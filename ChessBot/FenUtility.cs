using System;
using System.Text;
using ChessCore;

namespace ChessBot
{
    public static class FenUtility
    {
        public static string GenerateFen(Game game)
        {
            var board = game.Board;
            var sb = new StringBuilder();

            // 1. Piece placement
            for (int r = 0; r < 8; r++)
            {
                int emptyCount = 0;
                for (int c = 0; c < 8; c++)
                {
                    var piece = board[r, c];
                    if (piece == null)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            sb.Append(emptyCount);
                            emptyCount = 0;
                        }

                        char typeChar = piece.Type switch
                        {
                            PieceType.Pawn => 'p',
                            PieceType.Knight => 'n',
                            PieceType.Bishop => 'b',
                            PieceType.Rook => 'r',
                            PieceType.Queen => 'q',
                            PieceType.King => 'k',
                            _ => '?'
                        };

                        if (piece.Color == PlayerColor.White)
                        {
                            typeChar = char.ToUpper(typeChar);
                        }

                        sb.Append(typeChar);
                    }
                }
                if (emptyCount > 0)
                {
                    sb.Append(emptyCount);
                }
                if (r < 7)
                {
                    sb.Append("/");
                }
            }

            // 2. Active color
            sb.Append(game.CurrentPlayer == PlayerColor.White ? " w " : " b ");

            // 3. Castling availability
            bool canCastle = false;
            
            // White Castling
            var wk = board[7, 4];
            if (wk != null && wk.Type == PieceType.King && wk.Color == PlayerColor.White && !wk.HasMoved)
            {
                var wrK = board[7, 7];
                if (wrK != null && wrK.Type == PieceType.Rook && !wrK.HasMoved) { sb.Append("K"); canCastle = true; }
                
                var wrQ = board[7, 0];
                if (wrQ != null && wrQ.Type == PieceType.Rook && !wrQ.HasMoved) { sb.Append("Q"); canCastle = true; }
            }

            // Black Castling
            var bk = board[0, 4];
            if (bk != null && bk.Type == PieceType.King && bk.Color == PlayerColor.Black && !bk.HasMoved)
            {
                var brK = board[0, 7];
                if (brK != null && brK.Type == PieceType.Rook && !brK.HasMoved) { sb.Append("k"); canCastle = true; }
                
                var brQ = board[0, 0];
                if (brQ != null && brQ.Type == PieceType.Rook && !brQ.HasMoved) { sb.Append("q"); canCastle = true; }
            }

            if (!canCastle) sb.Append("-");

            // 4. En passant target square
            sb.Append(" ");
            if (board.EnPassantTarget.HasValue)
            {
                var pos = board.EnPassantTarget.Value;
                char f = (char)('a' + pos.Col);
                int rank = 8 - pos.Row;
                sb.Append($"{f}{rank}");
            }
            else
            {
                sb.Append("-");
            }

            // 5. Halfmove clock
            sb.Append($" {game.HalfmoveClock}");

            // 6. Fullmove number (Stockfish doesn't care much for search, but let's just put 1)
            sb.Append(" 1");

            return sb.ToString();
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
