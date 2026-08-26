using System.Collections.Generic;
using ChessCore;

namespace ChessGame.Models
{
    public class MoveRecord
    {
        public int MoveNumber { get; set; }
        public string SanNotation { get; set; } = string.Empty;

        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }

        // Snapshot of the pieces at this point in time
        public List<PieceDto> BoardSnapshot { get; set; } = new List<PieceDto>();

        // We can optionally store whether it was White's or Black's turn after this move
        public PlayerColor NextTurn { get; set; }
        
        public string DisplayText => $"{MoveNumber}. {SanNotation}";
    }
}
