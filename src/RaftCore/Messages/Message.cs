namespace RaftCore.Messages
{
    public abstract class Message
    {
        public long Term { get; set; }
    }
}