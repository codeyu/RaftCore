namespace RaftCore
{
    public interface INodeIdProvider
    {
        NodeId LocalNodeId { get; }
    }
}