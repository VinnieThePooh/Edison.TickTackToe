using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Edison.TickTackToe.Domain.Models
{
  public class GameFigure
    {
      #region Constructors

      public GameFigure()
      {
          GameSteps = new List<GameStep>();
      }

      #endregion

      public int FigureId { get; set; }
      public string Name { get; set; }

      public virtual List<GameStep> GameSteps { get; set; }
    }
}
