using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edison.TickTackToe.Domain.Infrastructure
{
    public enum StatusCode
    {
        Idle = 0,
        Playing = 1,
        Offline = 2
    }
}
