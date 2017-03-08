using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Ajax.Utilities;

namespace Edison.TickTackToe.Web.Infrastructure
{
    public enum GameState
    {
        Undefined =0,
        NoughtsVictory,
        CrossesVictory,
        Draw 
    }
}