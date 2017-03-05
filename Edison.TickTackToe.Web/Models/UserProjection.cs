using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Edison.TickTackToe.Web.Models
{
    public class UserProjection
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
    }
}