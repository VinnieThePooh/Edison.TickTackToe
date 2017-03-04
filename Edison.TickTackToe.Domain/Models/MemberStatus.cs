using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Edison.TickTackToe.Domain.Models
{
   public class MemberStatus
    {
       #region Constructors
       public MemberStatus()
       {
           Members = new List<Member>();
       }

       #endregion

       public int StatusId { get; set; }
       public string Name { get; set; }
    
       public virtual List<Member> Members { get; set; }
    }
}
