using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Edison.TickTackToe.Domain.Models
{
   public class Game
    {

       #region Constructors

       public Game()
       {
           GameSteps = new List<GameStep>();
       }

       #endregion

       public int GameId { get; set; }

       [ForeignKey(nameof(PlayerInitiator))]
       public int? IdPlayerInitiator { get; set; }       
       public string WinnerUserName { get; set; }
       public string OpponentUserName { get; set; }
       public int FieldSize { get; set; }
       public DateTime GameBeginningDate { get; set; }
       public DateTime? GameEndingDate { get; set; }
       
       public UserProfile PlayerInitiator { get; set; }
       public virtual List<GameStep> GameSteps { get; set; }
    }
}
