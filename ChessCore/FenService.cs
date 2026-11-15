using System;
using System.Text;

namespace ChessCore
{
    public static class FenService
    {
        public const string StandardStartingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

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
                            typeChar = char.ToUpperInvariant(typeChar);
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
                    sb.Append('/');
                }
            }

            // 2. Active color
            sb.Append(game.CurrentPlayer == PlayerColor.White ? " w " : " b ");

            // 3. Castling availability
            var castlingSb = new StringBuilder();
            
            // White Castling
            var wk = board[7, 4];
            if (wk != null && wk.Type == PieceType.King && wk.Color == PlayerColor.White && !wk.HasMoved)
            {
                var wrK = board[7, 7];
                if (wrK != null && wrK.Type == PieceType.Rook && wrK.Color == PlayerColor.White && !wrK.HasMoved)
                {
                    castlingSb.Append('K');
                }
                
                var wrQ = board[7, 0];
                if (wrQ != null && wrQ.Type == PieceType.Rook && wrQ.Color == PlayerColor.White && !wrQ.HasMoved)
                {
                    castlingSb.Append('Q');
                }
            }

            // Black Castling
            var bk = board[0, 4];
            if (bk != null && bk.Type == PieceType.King && bk.Color == PlayerColor.Black && !bk.HasMoved)
            {
                var brK = board[0, 7];
                if (brK != null && brK.Type == PieceType.Rook && brK.Color == PlayerColor.Black && !brK.HasMoved)
                {
                    castlingSb.Append('k');
                }
                
                var brQ = board[0, 0];
                if (brQ != null && brQ.Type == PieceType.Rook && brQ.Color == PlayerColor.Black && !brQ.HasMoved)
                {
                    castlingSb.Append('q');
                }
            }

            if (castlingSb.Length == 0)
            {
                sb.Append('-');
            }
            else
            {
                sb.Append(castlingSb.ToString());
            }

            // 4. En passant target square
            sb.Append(' ');
            if (board.EnPassantTarget.HasValue)
            {
                sb.Append(board.EnPassantTarget.Value.ToAlgebraic());
            }
            else
            {
                sb.Append('-');
            }

            // 5. Halfmove clock
            sb.Append($" {game.HalfmoveClock}");

            // 6. Fullmove number
            sb.Append($" {game.FullmoveNumber}");

            return sb.ToString();
        }

        public static bool TryParseFen(string fen, out Game? game, out string? errorMessage)
        {
            game = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(fen))
            {
                errorMessage = "Chuỗi FEN không được để trống.";
                return false;
            }

            string[] parts = fen.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
            {
                errorMessage = "Định dạng FEN không hợp lệ.";
                return false;
            }

            string placement = parts[0];
            string[] ranks = placement.Split('/');
            if (ranks.Length != 8)
            {
                errorMessage = $"Bàn cờ phải có đúng 8 hàng (hiện có {ranks.Length}).";
                return false;
            }

            var board = Board.Empty();
            int whiteKings = 0, blackKings = 0;

            for (int r = 0; r < 8; r++)
            {
                int c = 0;
                foreach (char ch in ranks[r])
                {
                    if (char.IsDigit(ch))
                    {
                        c += ch - '0';
                    }
                    else
                    {
                        if (c >= 8)
                        {
                            errorMessage = $"Hàng {8 - r} chứa quá 8 ô.";
                            return false;
                        }

                        PlayerColor color = char.IsUpper(ch) ? PlayerColor.White : PlayerColor.Black;
                        char lower = char.ToLowerInvariant(ch);

                        Piece? piece = lower switch
                        {
                            'p' => new Pawn(color),
                            'n' => new Knight(color),
                            'b' => new Bishop(color),
                            'r' => new Rook(color),
                            'q' => new Queen(color),
                            'k' => new King(color),
                            _ => null
                        };

                        if (piece == null)
                        {
                            errorMessage = $"Ký tự quân cờ không hợp lệ '{ch}' tại hàng {8 - r}.";
                            return false;
                        }

                        if (piece is King)
                        {
                            if (color == PlayerColor.White) whiteKings++;
                            else blackKings++;
                        }

                        // Determine initial HasMoved
                        if (piece is Pawn)
                        {
                            if (color == PlayerColor.White && r != 6) piece.HasMoved = true;
                            if (color == PlayerColor.Black && r != 1) piece.HasMoved = true;
                        }
                        else
                        {
                            piece.HasMoved = true;
                        }

                        board[r, c] = piece;
                        c++;
                    }
                }

                if (c != 8)
                {
                    errorMessage = $"Hàng {8 - r} không đủ 8 ô (chỉ có {c} ô).";
                    return false;
                }
            }

            if (whiteKings != 1 || blackKings != 1)
            {
                errorMessage = $"Mỗi bên phải có đúng 1 quân Vua (Trắng: {whiteKings}, Đen: {blackKings}).";
                return false;
            }

            // 2. Active color
            PlayerColor activeColor = PlayerColor.White;
            if (parts.Length > 1)
            {
                if (parts[1].Equals("b", StringComparison.OrdinalIgnoreCase))
                {
                    activeColor = PlayerColor.Black;
                }
                else if (!parts[1].Equals("w", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = $"Lượt đi không hợp lệ: '{parts[1]}' (phải là 'w' hoặc 'b').";
                    return false;
                }
            }

            // 3. Castling availability
            string castling = parts.Length > 2 ? parts[2] : "-";
            var wk = board[7, 4];
            if (wk is King && wk.Color == PlayerColor.White)
            {
                bool canK = castling.Contains('K');
                bool canQ = castling.Contains('Q');

                if (canK || canQ)
                {
                    wk.HasMoved = false;
                }

                var wrK = board[7, 7];
                if (wrK is Rook && wrK.Color == PlayerColor.White && canK)
                {
                    wrK.HasMoved = false;
                }

                var wrQ = board[7, 0];
                if (wrQ is Rook && wrQ.Color == PlayerColor.White && canQ)
                {
                    wrQ.HasMoved = false;
                }
            }

            var bk = board[0, 4];
            if (bk is King && bk.Color == PlayerColor.Black)
            {
                bool canK = castling.Contains('k');
                bool canQ = castling.Contains('q');

                if (canK || canQ)
                {
                    bk.HasMoved = false;
                }

                var brK = board[0, 7];
                if (brK is Rook && brK.Color == PlayerColor.Black && canK)
                {
                    brK.HasMoved = false;
                }

                var brQ = board[0, 0];
                if (brQ is Rook && brQ.Color == PlayerColor.Black && canQ)
                {
                    brQ.HasMoved = false;
                }
            }

            // 4. En passant target square
            if (parts.Length > 3 && parts[3] != "-")
            {
                if (Position.TryFromAlgebraic(parts[3], out Position epPos))
                {
                    board.EnPassantTarget = epPos;
                }
            }

            // 5. Halfmove clock
            int halfmoveClock = 0;
            if (parts.Length > 4 && int.TryParse(parts[4], out int hm))
            {
                halfmoveClock = Math.Max(0, hm);
            }

            // 6. Fullmove number
            int fullmoveNumber = 1;
            if (parts.Length > 5 && int.TryParse(parts[5], out int fm))
            {
                fullmoveNumber = Math.Max(1, fm);
            }

            game = new Game(board, activeColor, halfmoveClock, fullmoveNumber);
            return true;
        }

        public static Game LoadGameFromFen(string fen)
        {
            if (TryParseFen(fen, out Game? game, out string? error))
            {
                return game!;
            }
            throw new ArgumentException(error ?? "Không thể phân tích chuỗi FEN");
        }
    }
}
