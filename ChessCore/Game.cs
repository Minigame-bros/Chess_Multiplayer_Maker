using System.Collections.Generic;
using System.Linq;

namespace ChessCore
{
    public class Game
    {
        public Board Board { get; }
        public PlayerColor CurrentPlayer { get; private set; }
        public bool IsCheck { get; private set; }
        public bool IsCheckmate { get; private set; }
        public bool IsStalemate { get; private set; }
        public bool IsGameOver => IsCheckmate || IsStalemate;

        public Game()
        {
            Board = Board.Initial();
            CurrentPlayer = PlayerColor.White;
        }

        public void ForceSetCurrentPlayer(PlayerColor color)
        {
            CurrentPlayer = color;
        }

        public bool MovePiece(Position from, Position to)
        {
            Piece? piece = Board[from];
            if (piece == null || piece.Color != CurrentPlayer)
            {
                return false;
            }

            var legalMoves = GetLegalMoves(piece, from);
            if (!legalMoves.Contains(to))
            {
                return false;
            }

            // Execute move
            Board[to] = piece;
            Board[from] = null;
            
            // Check for En Passant Capture
            if (piece.Type == PieceType.Pawn && to == Board.EnPassantTarget)
            {
                // Remove captured pawn
                Position capturedPawnPos = new Position(from.Row, to.Col);
                Board[capturedPawnPos] = null;
            }

            // Check for Castling (King moves 2 squares)
            if (piece.Type == PieceType.King && System.Math.Abs(to.Col - from.Col) == 2)
            {
                int direction = to.Col > from.Col ? 1 : -1;
                Position rookFrom = new Position(from.Row, direction == 1 ? 7 : 0);
                Position rookTo = new Position(from.Row, to.Col - direction);
                
                Piece? rook = Board[rookFrom];
                if (rook != null)
                {
                    Board[rookTo] = rook;
                    Board[rookFrom] = null;
                    rook.HasMoved = true;
                }
            }

            // Set EnPassantTarget for next turn
            Board.EnPassantTarget = null;
            if (piece.Type == PieceType.Pawn && System.Math.Abs(to.Row - from.Row) == 2)
            {
                // The square skipped over
                Board.EnPassantTarget = new Position((from.Row + to.Row) / 2, from.Col);
            }

            piece.HasMoved = true;

            // Promotion (Auto-promote to Queen for now)
            if (piece.Type == PieceType.Pawn)
            {
                if ((piece.Color == PlayerColor.White && to.Row == 0) ||
                    (piece.Color == PlayerColor.Black && to.Row == 7))
                {
                    Board[to] = new Queen(piece.Color);
                }
            }

            // Switch turns
            CurrentPlayer = CurrentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
            
            // Check if the new player is in check
            IsCheck = IsPlayerInCheck(CurrentPlayer, Board);

            // Check if game is over (no legal moves)
            if (!HasLegalMoves(CurrentPlayer))
            {
                if (IsCheck)
                {
                    IsCheckmate = true;
                }
                else
                {
                    IsStalemate = true;
                }
            }

            return true;
        }

        private bool HasLegalMoves(PlayerColor color)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var pos = new Position(r, c);
                    Piece? p = Board[pos];
                    if (p != null && p.Color == color)
                    {
                        var legalMoves = GetLegalMoves(p, pos);
                        if (legalMoves.Any())
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public IEnumerable<Position> GetLegalMoves(Piece piece, Position from)
        {
            var moves = piece.GetUnhinderedMoves(from, Board);
            foreach (var to in moves)
            {
                // Simulate move
                Piece? captured = Board[to];
                Piece? enPassantCaptured = null;
                Position? enPassantCapturedPos = null;

                if (piece.Type == PieceType.Pawn && to == Board.EnPassantTarget)
                {
                    enPassantCapturedPos = new Position(from.Row, to.Col);
                    enPassantCaptured = Board[enPassantCapturedPos.Value];
                    Board[enPassantCapturedPos.Value] = null;
                }

                Board[to] = piece;
                Board[from] = null;

                bool isCheck = IsPlayerInCheck(piece.Color, Board);

                // Undo move
                Board[from] = piece;
                Board[to] = captured;
                
                if (enPassantCapturedPos.HasValue)
                {
                    Board[enPassantCapturedPos.Value] = enPassantCaptured;
                }

                if (!isCheck)
                {
                    // Special checks for castling
                    if (piece.Type == PieceType.King && System.Math.Abs(to.Col - from.Col) == 2)
                    {
                        if (IsPlayerInCheck(piece.Color, Board)) continue; // Cannot castle out of check
                        
                        // Check if passing square is under attack
                        int direction = to.Col > from.Col ? 1 : -1;
                        Position passingSquare = new Position(from.Row, from.Col + direction);
                        
                        // Simulate move to passing square
                        Board[passingSquare] = piece;
                        Board[from] = null;
                        bool isPassingCheck = IsPlayerInCheck(piece.Color, Board);
                        Board[from] = piece;
                        Board[passingSquare] = null;

                        if (isPassingCheck) continue; // Cannot castle through check
                    }

                    yield return to;
                }
            }
        }

        public bool IsPlayerInCheck(PlayerColor color, Board board)
        {
            Position? kingPos = null;

            // Find King
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var pos = new Position(r, c);
                    Piece? p = board[pos];
                    if (p != null && p.Color == color && p.Type == PieceType.King)
                    {
                        kingPos = pos;
                        break;
                    }
                }
                if (kingPos != null) break;
            }

            if (kingPos == null) return false; // Should not happen in a valid game

            // Check if any opponent piece can attack king
            PlayerColor opponentColor = color == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var pos = new Position(r, c);
                    Piece? p = board[pos];
                    if (p != null && p.Color == opponentColor)
                    {
                        var opponentMoves = p.GetUnhinderedMoves(pos, board);
                        if (opponentMoves.Contains(kingPos.Value))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
