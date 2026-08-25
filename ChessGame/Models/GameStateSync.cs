using System.Collections.Generic;

namespace ChessGame.Models
{
    public class PieceDto
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int Type { get; set; } // 0: Pawn, 1: Rook, 2: Knight, 3: Bishop, 4: Queen, 5: King
        public int Color { get; set; } // 0: White, 1: Black
        public bool HasMoved { get; set; }
    }

    public class GameStateSync
    {
        public List<PieceDto> Pieces { get; set; } = new List<PieceDto>();
        public int CurrentPlayer { get; set; }
    }
}
