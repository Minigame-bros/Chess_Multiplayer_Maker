using System.Collections.Generic;

namespace ChessCore
{
    public class Knight : Piece
    {
        public override PieceType Type => PieceType.Knight;
        public override PlayerColor Color { get; }

        public Knight(PlayerColor color)
        {
            Color = color;
        }

        public override Piece Copy()
        {
            Knight copy = new Knight(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        public override IEnumerable<Position> GetUnhinderedMoves(Position from, Board board)
        {
            Direction[] possibleMoves = new Direction[]
            {
                new Direction(-2, -1),
                new Direction(-2, 1),
                new Direction(2, -1),
                new Direction(2, 1),
                new Direction(-1, -2),
                new Direction(-1, 2),
                new Direction(1, -2),
                new Direction(1, 2)
            };

            foreach (Direction dir in possibleMoves)
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
        }
    }
}
