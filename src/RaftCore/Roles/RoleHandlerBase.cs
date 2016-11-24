using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RaftCore.Commands;
using RaftCore.Messages;
using RaftCore.Queries;

namespace RaftCore.Roles
{
    public abstract class RoleHandlerBase : IRoleHandler
    {
        public delegate Message RequestHandlerDelegate(RoleContext context, NodeId sender, Message request);
        public delegate void    ResponseHandlerDelegate(RoleContext context,NodeId sender, object state,Message response,Exception exception);

        private readonly Dictionary<Type,RequestHandlerDelegate>    requestHandlers     = new Dictionary<Type, RequestHandlerDelegate>();
        private readonly  Dictionary<Type,ResponseHandlerDelegate>  responseHandlers    = new Dictionary<Type, ResponseHandlerDelegate>();

        protected RoleHandlerBase(ILogger logger,NodeRole roleHandled)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            RoleHandled = roleHandled;
            Log         = logger;
        }

        protected ILogger   Log { get; }

        public NodeRole     RoleHandled { get; }

        public abstract void OnEnter(RoleContext context, NodeRole newRole);
      
        public Message OnRequest(RoleContext context, NodeId sender, Message request)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (request == null) throw new ArgumentNullException(nameof(request));

            RequestHandlerDelegate requestHandler = null;

            if (requestHandlers.TryGetValue(request.GetType(), out requestHandler))
            {
                var response = requestHandler.Invoke(context, sender, request);
                response.Term = context.PersistentState.CurrentTerm;

                OnAllServerRules(context,request,response);

                return response;
            }

            Log.LogWarning("No handler for request type {request}, in role {role}",request.GetType(),RoleHandled);
            return null;
        }

        public void OnResponse(RoleContext context, NodeId sender, object state, Message response, Exception exception)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (response == null) throw new ArgumentNullException(nameof(response));

            ResponseHandlerDelegate responseHandler = null;

            if (responseHandlers.TryGetValue(response.GetType(), out responseHandler))
            {
                responseHandler.Invoke(context,sender,state,response,exception);

                OnAllServerRules(context,null,response);
            }

            Log.LogWarning("No handler for response type {response}, in role {role}",response.GetType(),RoleHandled);
        }

        public abstract Task<IList<object>> OnQueries(RoleContext context, IEnumerable<IQuery> queries,QueryConsistency queryConsistency);
        public abstract Task OnCommands(RoleContext context, IEnumerable<ICommand> commands);
        public abstract void OnTimeout(RoleContext context);

        public abstract void OnHeartbeat(RoleContext context);

        public abstract void OnExit(RoleContext context, NodeRole newRole);
     
        protected void HandleRequest<TRequest>(Func<RoleContext, NodeId, TRequest, Message> handler)
            where TRequest : Message
        {
            this.requestHandlers[typeof(TRequest)] = (context, sender, request) => handler.Invoke(context,sender,(TRequest)request); 
        }

        protected void HandleResponse<TRequest, TResponse>(Action<RoleContext, NodeId, object, TResponse, Exception> handler)
            where TRequest: Message
            where TResponse: Message
        {
            this.responseHandlers[typeof(TResponse)] = (context, sender, state, response, exception) => handler.Invoke(context,sender,state,(TResponse)response,exception);
        }

        private void OnAllServerRules(RoleContext context, Message request, Message response)
        {
            // Apply any outstanding commits:
            while (context.VolatileState.CommitIndex > context.VolatileState.LastAppliedIndex)
            {
                Log.LogInformation("Applying log entry with index {index} to state machine",context.VolatileState.LastAppliedIndex + 1);

                context.VolatileState.LastAppliedIndex++;

                context.PersistentState.ApplyLogEntry(context.VolatileState.LastAppliedIndex);
            }

            // Update terms:
            var maxTerm = Math.Max(request.Term, response.Term);

            if (maxTerm > context.PersistentState.CurrentTerm)
            {
                Log.LogInformation("Term is out of date, updating from {oldTerm} to {term}",context.PersistentState.CurrentTerm,maxTerm);

                context.PersistentState.CurrentTerm = maxTerm;
                context.ConvertTo(NodeRole.Follower);
            }
        }
    }
}