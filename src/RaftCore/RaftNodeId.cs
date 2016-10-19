using System;

namespace RaftCore
{
    public struct RaftNodeId : IEquatable<RaftNodeId>
    {
        private readonly string value;

        public RaftNodeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            this.value = value;
        }

        public bool Equals(RaftNodeId other)
        {
            return string.Equals(value, other.value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RaftNodeId && Equals((RaftNodeId) obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(value);
        }

        public override string ToString()
        {
            return value;
        }

        public static bool operator ==(RaftNodeId left, RaftNodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RaftNodeId left, RaftNodeId right)
        {
            return !left.Equals(right);
        }
    }
}