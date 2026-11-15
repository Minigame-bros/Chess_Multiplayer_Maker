using System;
using System.Text;
using ChessCore;

namespace ChessBot
{
    public static class FenUtility
    {
        public static string GenerateFen(Game game)
        {
            return FenService.GenerateFen(game);
        }

        public static string PositionToUci(Position from, Position to, PieceType? promotion = null)
        {
            char fromCol = (char)('a' + from.Col);
            int fromRow = 8 - from.Row;
            char toCol = (char)('a' + to.Col);
            int toRow = 8 - to.Row;

            string uci = $"{fromCol}{fromRow}{toCol}{toRow}";

            if (promotion.HasValue)
            {
                uci += promotion.Value switch
                {
                    PieceType.Queen => "q",
                    PieceType.Rook => "r",
                    PieceType.Bishop => "b",
                    PieceType.Knight => "n",
                    _ => ""
                };
            }

            return uci;
        }

        public static (Position from, Position to, PieceType? promo) UciToMove(string uci)
        {
            if (uci.Length < 4) throw new ArgumentException("Invalid UCI string");

            int fromCol = uci[0] - 'a';
            int fromRow = 8 - (uci[1] - '0');
            int toCol = uci[2] - 'a';
            int toRow = 8 - (uci[3] - '0');

            PieceType? promo = null;
            if (uci.Length == 5)
            {
                promo = uci[4] switch
                {
                    'q' => PieceType.Queen,
                    'r' => PieceType.Rook,
                    'b' => PieceType.Bishop,
                    'n' => PieceType.Knight,
                    _ => null
                };
            }

            return (new Position(fromRow, fromCol), new Position(toRow, toCol), promo);
        }
    }
}
