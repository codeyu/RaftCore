using System;

namespace RaftCore
{
    public struct NodeId : IEquatable<NodeId>
    {
        public static readonly NodeId Null = new NodeId(null);

        private readonly string value;

        public NodeId(string value)
        {
            this.value = value;
        }

        public bool IsNull => value == null;

        public bool Equals(NodeId other)
        {
            return string.Equals(value, other.value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is NodeId && Equals((NodeId) obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(value);
        }

        public override string ToString()
        {
            return value;
        }

        public static bool operator ==(NodeId left, NodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NodeId left, NodeId right)
        {
            return !left.Equals(right);
        }
    }
}