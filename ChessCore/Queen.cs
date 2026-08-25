using System.Collections.Generic;

namespace ChessCore
{
    public class Queen : Piece
    {
        public override PieceType Type => PieceType.Queen;
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

        public Queen(PlayerColor color)
        {
            Color = color;
        }

        public override Piece Copy()
        {
            Queen copy = new Queen(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        public override IEnumerable<Position> GetUnhinderedMoves(Position from, Board board)
        {
            return MovePositionsInDirs(from, board, dirs);
        }
    }
}
