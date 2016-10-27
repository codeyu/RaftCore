using System;

namespace RaftCore
{
    public enum NodeStatusType
    {
        Unknown = 0,
        Healthy,
        Unhealthy,
    }

    public struct NodeStatus
    {
        public NodeId           Id { get; }

        public NodeStatusType   Status { get; }

        public DateTimeOffset   LastUpdatedOn { get; }
    }
}