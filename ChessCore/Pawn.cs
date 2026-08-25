using System.Collections.Generic;

namespace ChessCore
{
    public class Pawn : Piece
    {
        public override PieceType Type => PieceType.Pawn;
        public override PlayerColor Color { get; }
        private readonly Direction forward;

        public Pawn(PlayerColor color)
        {
            Color = color;
            if (color == PlayerColor.White)
            {
                forward = Direction.North;
            }
            else if (color == PlayerColor.Black)
            {
                forward = Direction.South;
            }
            else
            {
                forward = new Direction(0, 0);
            }
        }

        public override Piece Copy()
        {
            Pawn copy = new Pawn(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        public override IEnumerable<Position> GetUnhinderedMoves(Position from, Board board)
        {
            Position oneStep = from + forward;
            if (oneStep.IsInsideBoard() && board.IsEmpty(oneStep))
            {
                yield return oneStep;

                Position twoSteps = oneStep + forward;
                if (!HasMoved && twoSteps.IsInsideBoard() && board.IsEmpty(twoSteps))
                {
                    yield return twoSteps;
                }
            }

            // Captures
            Position leftCapture = from + forward + Direction.West;
            if (leftCapture.IsInsideBoard())
            {
                if (!board.IsEmpty(leftCapture) && board[leftCapture]!.Color != Color)
                {
                    yield return leftCapture;
                }
                else if (board.EnPassantTarget == leftCapture)
                {
                    yield return leftCapture; // En Passant
                }
            }

            Position rightCapture = from + forward + Direction.East;
            if (rightCapture.IsInsideBoard())
            {
                if (!board.IsEmpty(rightCapture) && board[rightCapture]!.Color != Color)
                {
                    yield return rightCapture;
                }
                else if (board.EnPassantTarget == rightCapture)
                {
                    yield return rightCapture; // En Passant
                }
            }
        }
    }
}
