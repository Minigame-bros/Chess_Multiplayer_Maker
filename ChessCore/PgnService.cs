using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChessCore
{
    public class PgnMetadata
    {
        public string Event { get; set; } = "Casual Game";
        public string Site { get; set; } = "ChessSolution";
        public string Date { get; set; } = DateTime.Now.ToString("yyyy.MM.dd");
        public string Round { get; set; } = "1";
        public string White { get; set; } = "White";
        public string Black { get; set; } = "Black";
        public string Result { get; set; } = "*";
        public string? Fen { get; set; }
        public Dictionary<string, string> AdditionalTags { get; } = new Dictionary<string, string>();
    }

    public class PgnMove
    {
        public int TurnNumber { get; set; }
        public PlayerColor Color { get; set; }
        public string San { get; set; } = string.Empty;
        public Position From { get; set; }
        public Position To { get; set; }
        public PieceType? Promotion { get; set; }
    }

    public static class PgnService
    {
        public static string GenerateSan(Game game, Position from, Position to, PieceType? promotion = null)
        {
            Piece? piece = game.Board[from];
            if (piece == null) return string.Empty;

            // 1. Castling
            if (piece.Type == PieceType.King && Math.Abs(to.Col - from.Col) == 2)
            {
                return to.Col > from.Col ? "O-O" : "O-O-O";
            }

            var sb = new StringBuilder();

            // 2. Piece Letter
            if (piece.Type != PieceType.Pawn)
            {
                sb.Append(piece.Type switch
                {
                    PieceType.King => 'K',
                    PieceType.Queen => 'Q',
                    PieceType.Rook => 'R',
                    PieceType.Bishop => 'B',
                    PieceType.Knight => 'N',
                    _ => '?'
                });

                // Disambiguation
                if (piece.Type != PieceType.King)
                {
                    var otherPieces = new List<Position>();
                    for (int r = 0; r < 8; r++)
                    {
                        for (int c = 0; c < 8; c++)
                        {
                            var pos = new Position(r, c);
                            if (pos == from) continue;
                            Piece? other = game.Board[pos];
                            if (other != null && other.Type == piece.Type && other.Color == piece.Color)
                            {
                                if (game.GetLegalMoves(other, pos).Contains(to))
                                {
                                    otherPieces.Add(pos);
                                }
                            }
                        }
                    }

                    if (otherPieces.Count > 0)
                    {
                        bool sameCol = otherPieces.Any(op => op.Col == from.Col);
                        bool sameRow = otherPieces.Any(op => op.Row == from.Row);

                        if (!sameCol)
                        {
                            sb.Append((char)('a' + from.Col));
                        }
                        else if (!sameRow)
                        {
                            sb.Append(8 - from.Row);
                        }
                        else
                        {
                            sb.Append($"{(char)('a' + from.Col)}{8 - from.Row}");
                        }
                    }
                }
            }

            // 3. Captures
            bool isCapture = game.Board[to] != null;
            if (piece.Type == PieceType.Pawn && to == game.Board.EnPassantTarget)
            {
                isCapture = true;
            }

            if (isCapture)
            {
                if (piece.Type == PieceType.Pawn)
                {
                    sb.Append((char)('a' + from.Col));
                }
                sb.Append('x');
            }

            // 4. Target Square
            sb.Append(to.ToAlgebraic());

            // 5. Promotion
            if (promotion.HasValue)
            {
                char promoChar = promotion.Value switch
                {
                    PieceType.Rook => 'R',
                    PieceType.Bishop => 'B',
                    PieceType.Knight => 'N',
                    _ => 'Q'
                };
                sb.Append($"={promoChar}");
            }

            return sb.ToString();
        }

        public static string GeneratePgn(Game game, IEnumerable<string> sanMoves, PgnMetadata? metadata = null)
        {
            metadata ??= new PgnMetadata();
            var sb = new StringBuilder();

            // Standard Seven Tag Roster
            sb.AppendLine($"[Event \"{EscapeTagValue(metadata.Event)}\"]");
            sb.AppendLine($"[Site \"{EscapeTagValue(metadata.Site)}\"]");
            sb.AppendLine($"[Date \"{EscapeTagValue(metadata.Date)}\"]");
            sb.AppendLine($"[Round \"{EscapeTagValue(metadata.Round)}\"]");
            sb.AppendLine($"[White \"{EscapeTagValue(metadata.White)}\"]");
            sb.AppendLine($"[Black \"{EscapeTagValue(metadata.Black)}\"]");
            sb.AppendLine($"[Result \"{EscapeTagValue(metadata.Result)}\"]");

            if (!string.IsNullOrEmpty(metadata.Fen) && metadata.Fen != FenService.StandardStartingFen)
            {
                sb.AppendLine("[SetUp \"1\"]");
                sb.AppendLine($"[FEN \"{EscapeTagValue(metadata.Fen)}\"]");
            }

            foreach (var kvp in metadata.AdditionalTags)
            {
                if (!string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
                {
                    sb.AppendLine($"[{kvp.Key} \"{EscapeTagValue(kvp.Value)}\"]");
                }
            }

            sb.AppendLine();

            // Format Movetext
            var moveList = sanMoves.ToList();
            var moveSb = new StringBuilder();
            int turnNumber = 1;

            for (int i = 0; i < moveList.Count; i += 2)
            {
                string whiteMove = moveList[i];
                string? blackMove = (i + 1 < moveList.Count) ? moveList[i + 1] : null;

                moveSb.Append($"{turnNumber}. {whiteMove} ");
                if (!string.IsNullOrEmpty(blackMove))
                {
                    moveSb.Append($"{blackMove} ");
                }
                turnNumber++;
            }

            if (!string.IsNullOrEmpty(metadata.Result))
            {
                moveSb.Append(metadata.Result);
            }

            sb.AppendLine(WrapText(moveSb.ToString().Trim(), 80));

            return sb.ToString().TrimEnd();
        }

        public static bool TryLoadPgn(string pgn, out Game? game, out List<PgnMove> moves, out PgnMetadata metadata, out string? errorMessage)
        {
            game = null;
            moves = new List<PgnMove>();
            metadata = new PgnMetadata();
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(pgn))
            {
                errorMessage = "Nội dung PGN trống.";
                return false;
            }

            // 1. Extract Tag Pairs
            var tagRegex = new Regex(@"\[\s*([A-Za-z0-9_]+)\s+""([^""]*)""\s*\]");
            var matches = tagRegex.Matches(pgn);
            foreach (Match match in matches)
            {
                string tag = match.Groups[1].Value;
                string val = match.Groups[2].Value;

                switch (tag.ToLowerInvariant())
                {
                    case "event": metadata.Event = val; break;
                    case "site": metadata.Site = val; break;
                    case "date": metadata.Date = val; break;
                    case "round": metadata.Round = val; break;
                    case "white": metadata.White = val; break;
                    case "black": metadata.Black = val; break;
                    case "result": metadata.Result = val; break;
                    case "fen": metadata.Fen = val; break;
                    default:
                        metadata.AdditionalTags[tag] = val;
                        break;
                }
            }

            // Remove tag pairs to get pure movetext
            string movetext = tagRegex.Replace(pgn, " ");

            // 2. Initialize Game
            if (!string.IsNullOrEmpty(metadata.Fen))
            {
                if (!FenService.TryParseFen(metadata.Fen, out game, out string? fenErr))
                {
                    errorMessage = $"Lỗi FEN trong PGN: {fenErr}";
                    return false;
                }
            }
            else
            {
                game = new Game();
            }

            // 3. Clean Movetext
            // Remove comments: { ... }
            movetext = Regex.Replace(movetext, @"\{[^}]*\}", " ");
            // Remove line comments: ; ...
            movetext = Regex.Replace(movetext, @";[^\r\n]*", " ");
            // Remove recursive variations: ( ... )
            while (movetext.Contains('(') && movetext.Contains(')'))
            {
                string before = movetext;
                movetext = Regex.Replace(movetext, @"\([^()]*\)", " ");
                if (movetext == before) break;
            }
            // Remove NAGs: $1, $2, etc.
            movetext = Regex.Replace(movetext, @"\$\d+", " ");
            // Remove move numbers: 1. or 1... or 12.
            movetext = Regex.Replace(movetext, @"\b\d+\s*\.+", " ");

            // 4. Tokenize Moves
            var rawTokens = movetext.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int moveIndex = 0;

            foreach (var token in rawTokens)
            {
                string cleanToken = token.Trim();
                if (string.IsNullOrEmpty(cleanToken)) continue;

                // Check termination markers
                if (cleanToken == "1-0" || cleanToken == "0-1" || cleanToken == "1/2-1/2" || cleanToken == "*")
                {
                    metadata.Result = cleanToken;
                    break;
                }

                // Resolve move
                if (!ResolveAndExecuteMove(game!, cleanToken, out PgnMove pgnMove, out string? resolveError))
                {
                    errorMessage = $"Không thể thực hiện nước đi thứ {moveIndex + 1} ('{cleanToken}'): {resolveError}";
                    return false;
                }

                moves.Add(pgnMove);
                moveIndex++;
            }

            return true;
        }

        private static bool ResolveAndExecuteMove(Game game, string san, out PgnMove pgnMove, out string? error)
        {
            pgnMove = new PgnMove();
            error = null;

            PlayerColor currentPlayer = game.CurrentPlayer;
            string rawSan = san;
            // Clean move string of check (+), checkmate (#), and annotations (!, ?)
            string cleaned = san.Trim().TrimEnd('+', '#', '!', '?');

            // 1. Castling
            if (cleaned.Equals("O-O", StringComparison.OrdinalIgnoreCase) || cleaned == "0-0")
            {
                int row = currentPlayer == PlayerColor.White ? 7 : 0;
                Position from = new Position(row, 4);
                Position to = new Position(row, 6);

                if (!game.GetLegalMoves(game.Board[from]!, from).Contains(to))
                {
                    error = "Nhập thành cánh vua không hợp lệ trong thế cờ này.";
                    return false;
                }

                game.MovePiece(from, to);
                pgnMove = new PgnMove
                {
                    TurnNumber = (game.FullmoveNumber),
                    Color = currentPlayer,
                    San = rawSan,
                    From = from,
                    To = to
                };
                return true;
            }

            if (cleaned.Equals("O-O-O", StringComparison.OrdinalIgnoreCase) || cleaned == "0-0-0")
            {
                int row = currentPlayer == PlayerColor.White ? 7 : 0;
                Position from = new Position(row, 4);
                Position to = new Position(row, 2);

                if (!game.GetLegalMoves(game.Board[from]!, from).Contains(to))
                {
                    error = "Nhập thành cánh hậu không hợp lệ trong thế cờ này.";
                    return false;
                }

                game.MovePiece(from, to);
                pgnMove = new PgnMove
                {
                    TurnNumber = (game.FullmoveNumber),
                    Color = currentPlayer,
                    San = rawSan,
                    From = from,
                    To = to
                };
                return true;
            }

            // 2. Promotion check
            PieceType? promotion = null;
            int promoIdx = cleaned.IndexOf('=');
            if (promoIdx >= 0 && promoIdx + 1 < cleaned.Length)
            {
                char promoChar = char.ToUpperInvariant(cleaned[promoIdx + 1]);
                promotion = promoChar switch
                {
                    'Q' => PieceType.Queen,
                    'R' => PieceType.Rook,
                    'B' => PieceType.Bishop,
                    'N' => PieceType.Knight,
                    _ => PieceType.Queen
                };
                cleaned = cleaned.Substring(0, promoIdx);
            }
            else if (cleaned.Length >= 3 && char.IsLetter(cleaned[^1]) && char.IsUpper(cleaned[^1]) && char.IsDigit(cleaned[^2]))
            {
                // Format like e8Q
                char promoChar = cleaned[^1];
                promotion = promoChar switch
                {
                    'Q' => PieceType.Queen,
                    'R' => PieceType.Rook,
                    'B' => PieceType.Bishop,
                    'N' => PieceType.Knight,
                    _ => null
                };
                if (promotion.HasValue)
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - 1);
                }
            }

            // 3. Target square
            if (cleaned.Length < 2)
            {
                error = $"Ký hiệu nước đi quá ngắn: '{san}'.";
                return false;
            }

            string targetSquareStr = cleaned.Substring(cleaned.Length - 2);
            if (!Position.TryFromAlgebraic(targetSquareStr, out Position targetSquare))
            {
                error = $"Không xác định được ô đích từ '{san}'.";
                return false;
            }

            // 4. Piece type and Disambiguation
            string prefix = cleaned.Substring(0, cleaned.Length - 2);
            PieceType pieceType = PieceType.Pawn;
            string disambig = "";

            if (prefix.Length > 0 && char.IsUpper(prefix[0]))
            {
                pieceType = prefix[0] switch
                {
                    'N' => PieceType.Knight,
                    'B' => PieceType.Bishop,
                    'R' => PieceType.Rook,
                    'Q' => PieceType.Queen,
                    'K' => PieceType.King,
                    _ => PieceType.Pawn
                };
                disambig = prefix.Substring(1).Replace("x", "");
            }
            else
            {
                pieceType = PieceType.Pawn;
                disambig = prefix.Replace("x", "");
            }

            // 5. Find matching legal move
            var candidates = new List<(Position from, Position to)>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var pos = new Position(r, c);
                    Piece? p = game.Board[pos];
                    if (p != null && p.Color == currentPlayer && p.Type == pieceType)
                    {
                        var legalMoves = game.GetLegalMoves(p, pos);
                        if (legalMoves.Contains(targetSquare))
                        {
                            // Check disambiguation
                            bool colMatch = true;
                            bool rowMatch = true;

                            foreach (char ch in disambig)
                            {
                                if (ch >= 'a' && ch <= 'h')
                                {
                                    if (c != (ch - 'a')) colMatch = false;
                                }
                                else if (ch >= '1' && ch <= '8')
                                {
                                    if (r != (8 - (ch - '0'))) rowMatch = false;
                                }
                            }

                            if (colMatch && rowMatch)
                            {
                                candidates.Add((pos, targetSquare));
                            }
                        }
                    }
                }
            }

            if (candidates.Count == 0)
            {
                error = $"Không có quân cờ {pieceType} nào có thể đi đến {targetSquareStr} theo ký hiệu '{san}'.";
                return false;
            }

            if (candidates.Count > 1)
            {
                error = $"Nước đi mập mờ, có {candidates.Count} quân có thể đi đến {targetSquareStr}.";
                return false;
            }

            var chosen = candidates[0];
            if (!game.MovePiece(chosen.from, chosen.to, promotion ?? PieceType.Queen))
            {
                error = $"Nước đi không hợp lệ theo luật cờ: {chosen.from.ToAlgebraic()} -> {chosen.to.ToAlgebraic()}.";
                return false;
            }

            pgnMove = new PgnMove
            {
                TurnNumber = game.FullmoveNumber,
                Color = currentPlayer,
                San = rawSan,
                From = chosen.from,
                To = chosen.to,
                Promotion = promotion
            };

            return true;
        }

        private static string EscapeTagValue(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string WrapText(string text, int lineLength)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= lineLength)
            {
                return text;
            }

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 > lineLength)
                {
                    sb.AppendLine(currentLine.ToString().TrimEnd());
                    currentLine.Clear();
                }

                currentLine.Append(word).Append(' ');
            }

            if (currentLine.Length > 0)
            {
                sb.Append(currentLine.ToString().TrimEnd());
            }

            return sb.ToString();
        }
    }
}
