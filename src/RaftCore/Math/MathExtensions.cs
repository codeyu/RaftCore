namespace RaftCore.Math
{
    internal static class MathExtensions
    {
        internal static int GetMajorityCount(this int value)
        {
            if (value < 1)
            {
                return 1;
            }

            return (value/2) + 1;
        }
    }
}