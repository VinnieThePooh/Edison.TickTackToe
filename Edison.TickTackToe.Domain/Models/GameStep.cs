using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Edison.TickTackToe.Domain.Models
{
    public class GameStep
    {
        [Key]
        public int StepId { get; set; }

        [ForeignKey(nameof(ParentGame))]
        public int IdGame { get; set; }

        [ForeignKey(nameof(GameFigure))]
        public int IdFigure { get; set; }
        
        public bool? IsFinalStep { get; set; }
        public Game ParentGame { get; set; }

        public int XCoordinate  { get; set; }
        public int YCoordinate { get; set; }

        public GameFigure GameFigure { get; set; }
        public string PlayerName { get; set; }
    }
}