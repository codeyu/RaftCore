using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RaftCore.Transport
{
    public interface ITransportRequest
    {
        
    }

    public interface ITransportResponse
    {
        
    }

    public delegate ITransportResponse TransportRequestProcessingDelegate(NodeId sender,ITransportRequest request);

    public interface ITransport
    {
        IEnumerable<NodeId>         Nodes { get; }
            
        IDisposable                 Listen(TransportRequestProcessingDelegate requestDelegate);

        Task<NodeId>                Resolve(string address,CancellationToken cancellationToken);

        Task<ITransportResponse>    Send(NodeId target, ITransportRequest request, CancellationToken cancellationToken);
    }
}