using System;

namespace ChessCore
{
    public struct Position : IEquatable<Position>
    {
        public int Row { get; }
        public int Col { get; }

        public Position(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public PlayerColor SquareColor()
        {
            if ((Row + Col) % 2 == 0)
            {
                return PlayerColor.White;
            }
            return PlayerColor.Black;
        }

        public override bool Equals(object? obj)
        {
            return obj is Position position && Equals(position);
        }

        public bool Equals(Position other)
        {
            return Row == other.Row && Col == other.Col;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Col);
        }

        public static bool operator ==(Position left, Position right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }

        public static Position operator +(Position pos, Direction dir)
        {
            return new Position(pos.Row + dir.RowDelta, pos.Col + dir.ColDelta);
        }

        public bool IsInsideBoard()
        {
            return Row >= 0 && Row < 8 && Col >= 0 && Col < 8;
        }

        public string ToAlgebraic()
        {
            return $"{(char)('a' + Col)}{8 - Row}";
        }

        public static bool TryFromAlgebraic(string s, out Position pos)
        {
            pos = default;
            if (string.IsNullOrWhiteSpace(s) || s.Length < 2) return false;
            char file = char.ToLowerInvariant(s[0]);
            char rank = s[1];
            if (file < 'a' || file > 'h' || rank < '1' || rank > '8') return false;
            pos = new Position(8 - (rank - '0'), file - 'a');
            return true;
        }

        public static Position FromAlgebraic(string s)
        {
            if (TryFromAlgebraic(s, out Position pos)) return pos;
            throw new ArgumentException($"Invalid algebraic coordinate: '{s}'");
        }
    }

    public class Direction
    {
        public readonly static Direction North = new Direction(-1, 0);
        public readonly static Direction South = new Direction(1, 0);
        public readonly static Direction East = new Direction(0, 1);
        public readonly static Direction West = new Direction(0, -1);
        public readonly static Direction NorthWest = North + West;
        public readonly static Direction NorthEast = North + East;
        public readonly static Direction SouthWest = South + West;
        public readonly static Direction SouthEast = South + East;

        public int RowDelta { get; }
        public int ColDelta { get; }

        public Direction(int rowDelta, int colDelta)
        {
            RowDelta = rowDelta;
            ColDelta = colDelta;
        }

        public static Direction operator +(Direction dir1, Direction dir2)
        {
            return new Direction(dir1.RowDelta + dir2.RowDelta, dir1.ColDelta + dir2.ColDelta);
        }

        public static Direction operator *(int scalar, Direction dir)
        {
            return new Direction(scalar * dir.RowDelta, scalar * dir.ColDelta);
        }
    }
}
