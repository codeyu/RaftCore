using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RaftCore.Transport
{
    internal static class TransportExtensions
    {
        internal static async Task<IReadOnlyDictionary<NodeId,ITransportResponse>>  Broadcast(this ITransport transport,IEnumerable<NodeId> nodeIds, ITransportRequest request,CancellationToken cancellationToken)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (nodeIds == null) throw new ArgumentNullException(nameof(nodeIds));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var sendTasks   = nodeIds.Select(n => transport.Send(n, request, cancellationToken)).ToList();

            var result      = new Dictionary<NodeId,ITransportResponse>();
            var responses   = await Task.WhenAll(sendTasks);

            for (var i = 0; i < nodeIds.Count(); ++i)
            {
                result[nodeIds.ElementAt(i)] = responses[i];
            }

            return result;
        }

        internal static void Broadcast<TResponse>(this ITransport transport,IEnumerable<NodeId> nodeIds, ITransportRequest request,Action<NodeId,TResponse> successFunc,Action<NodeId,Exception> failureFunc,CancellationToken cancellationToken)
            where TResponse : ITransportResponse
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (nodeIds == null) throw new ArgumentNullException(nameof(nodeIds));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (successFunc == null) throw new ArgumentNullException(nameof(successFunc));
            if (failureFunc == null) throw new ArgumentNullException(nameof(failureFunc));

            foreach (var nodeId in nodeIds)
            {
                var localNodeId = nodeId;

                transport.Send(localNodeId, request, cancellationToken).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        failureFunc.Invoke(localNodeId,t.Exception);
                    }
                    else
                    {
                        successFunc.Invoke(localNodeId,(TResponse)t.Result);
                    }
                }, cancellationToken);
            }
        }
    }
}