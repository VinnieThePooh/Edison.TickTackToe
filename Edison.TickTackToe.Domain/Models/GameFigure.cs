using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edison.TickTackToe.Domain.Models
{
  public class GameFigure
    {
      [Key]
      public int FigureId { get; set; }
      public string FigureName { get; set; }
    }
}
