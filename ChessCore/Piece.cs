using System.Collections.Generic;

namespace ChessCore
{
    public abstract class Piece
    {
        public abstract PieceType Type { get; }
        public abstract PlayerColor Color { get; }
        public bool HasMoved { get; set; } = false;

        public abstract Piece Copy();

        public abstract IEnumerable<Position> GetUnhinderedMoves(Position from, Board board);

        public virtual bool CanMoveTo(Position pos, Board board)
        {
            Piece? pieceInTarget = board[pos];
            if (pieceInTarget != null && pieceInTarget.Color == this.Color)
            {
                return false; // Cannot capture own piece
            }
            return true;
        }

        protected IEnumerable<Position> MovePositionsInDir(Position from, Board board, Direction dir)
        {
            for (Position pos = from + dir; pos.IsInsideBoard(); pos += dir)
            {
                if (board.IsEmpty(pos))
                {
                    yield return pos;
                }
                else
                {
                    Piece? piece = board[pos];
                    if (piece != null && piece.Color != Color)
                    {
                        yield return pos;
                    }
                    yield break;
                }
            }
        }

        protected IEnumerable<Position> MovePositionsInDirs(Position from, Board board, Direction[] dirs)
        {
            foreach (Direction dir in dirs)
            {
                foreach (Position pos in MovePositionsInDir(from, board, dir))
                {
                    yield return pos;
                }
            }
        }
    }
}
