using System.Collections.Generic;

namespace ChessCore
{
    public class King : Piece
    {
        public override PieceType Type => PieceType.King;
        public override PlayerColor Color { get; }

        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West,
            Direction.NorthEast,
            Direction.NorthWest,
            Direction.SouthEast,
            Direction.SouthWest
        };

        public King(PlayerColor color)
        {
            Color = color;
        }

        public override Piece Copy()
        {
            King copy = new King(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        public override IEnumerable<Position> GetUnhinderedMoves(Position from, Board board)
        {
            foreach (Direction dir in dirs)
            {
                Position to = from + dir;
                if (to.IsInsideBoard())
                {
                    if (board.IsEmpty(to) || board[to]!.Color != Color)
                    {
                        yield return to;
                    }
                }
            }

            // Castling
            if (!HasMoved)
            {
                // Kingside castling
                if (CanCastle(board, Direction.East, 2))
                {
                    yield return from + new Direction(0, 2);
                }
                
                // Queenside castling
                if (CanCastle(board, Direction.West, 3))
                {
                    yield return from + new Direction(0, -2);
                }
            }
        }

        private bool CanCastle(Board board, Direction dir, int emptySquaresRequired)
        {
            Position from = new Position(Color == PlayerColor.White ? 7 : 0, 4);
            
            // Check if squares between king and rook are empty
            for (int i = 1; i <= emptySquaresRequired; i++)
            {
                Position p = from + (i * dir);
                if (!board.IsEmpty(p)) return false;
            }

            // Check if rook is at the expected position and hasn't moved
            Position rookPos = from + ((emptySquaresRequired + 1) * dir);
            Piece? rook = board[rookPos];
            if (rook != null && rook.Type == PieceType.Rook && rook.Color == Color && !rook.HasMoved)
            {
                return true;
            }

            return false;
        }
    }
}
