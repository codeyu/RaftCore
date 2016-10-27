using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RaftCore.Protocol
{
    internal interface ICarryTermUpdate
    {
        long Term { get; }
    }

    internal static class CarryTermExtensions
    {
        
    }
}
