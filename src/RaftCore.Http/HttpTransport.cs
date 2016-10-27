using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RaftCore.Transport;

namespace RaftCore.Http
{
    public class HttpTransport : ITransport
    {
        public IEnumerable<NodeStatus> RemoteNodes { get; }
        public IDisposable Listen(TransportRequestProcessingDelegate requestDelegate)
        {
            throw new NotImplementedException();
        }

        public Task<NodeId> Resolve(string address, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ITransportResponse> Send(NodeId target, ITransportRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}