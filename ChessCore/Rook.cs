using System.Collections.Generic;

namespace ChessCore
{
    public class Rook : Piece
    {
        public override PieceType Type => PieceType.Rook;
        public override PlayerColor Color { get; }

        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West
        };

        public Rook(PlayerColor color)
        {
            Color = color;
        }

        public override Piece Copy()
        {
            Rook copy = new Rook(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        public override IEnumerable<Position> GetUnhinderedMoves(Position from, Board board)
        {
            return MovePositionsInDirs(from, board, dirs);
        }
    }
}
