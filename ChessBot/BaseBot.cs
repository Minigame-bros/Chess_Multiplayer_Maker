using System;
using System.IO;
using System.Threading.Tasks;
using ChessCore;

namespace ChessBot
{
    public abstract class BaseBot : IChessBot, IDisposable
    {
        public abstract string Name { get; }
        public abstract string AvatarPath { get; }
        public abstract string Description { get; }
        public abstract int Elo { get; }

        protected static readonly Random Rnd = new Random();
        protected string _lastSpeech = "";

        private UciEngine? _engine;
        private bool _isDisposed;

        protected BaseBot()
        {
        }

        private void InitializeEngine()
        {
            if (_engine != null) return;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string engineName = Elo < 1320 ? "MadChess.exe" : "stockfish.exe";
            string enginePath = Path.Combine(baseDir, "Engine", engineName);
            
            if (!File.Exists(enginePath))
            {
                // Fallback for development
                string? currentDir = baseDir;
                while (!string.IsNullOrEmpty(currentDir))
                {
                    string potentialPath = Path.Combine(currentDir, "ChessBot", "Engine", engineName);
                    if (File.Exists(potentialPath))
                    {
                        enginePath = potentialPath;
                        break;
                    }
                    currentDir = Path.GetDirectoryName(currentDir);
                }
            }

            if (!File.Exists(enginePath))
            {
                throw new FileNotFoundException($"Cannot find {engineName} at {enginePath}. Please make sure it is downloaded and placed in the Engine folder.");
            }

            _engine = new UciEngine(enginePath, Elo);
        }

        public async Task<BotMove> CalculateMoveAsync(Game game)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(BaseBot));
            if (_engine == null) InitializeEngine();
            return await _engine!.GetBestMoveAsync(game);
        }

        public abstract string GetSpeech(GameState state, Game? game = null, BotMove? lastMove = null);

        protected string PickRandom(string[] lines)
        {
            if (lines == null || lines.Length == 0) return "...";
            if (lines.Length == 1) return lines[0];

            int idx = Rnd.Next(lines.Length);
            if (lines[idx] == _lastSpeech)
            {
                idx = (idx + 1 + Rnd.Next(lines.Length - 1)) % lines.Length;
            }

            _lastSpeech = lines[idx];
            return _lastSpeech;
        }

        protected int GetMaterialBalance(Game? game, PlayerColor botColor = PlayerColor.Black)
        {
            if (game == null) return 0;

            int botScore = 0;
            int oppScore = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var p = game.Board[r, c];
                    if (p != null)
                    {
                        int val = p.Type switch
                        {
                            PieceType.Queen => 9,
                            PieceType.Rook => 5,
                            PieceType.Bishop => 3,
                            PieceType.Knight => 3,
                            PieceType.Pawn => 1,
                            _ => 0
                        };

                        if (p.Color == botColor) botScore += val;
                        else botScore -= val;
                    }
                }
            }

            return botScore;
        }

        protected bool IsEndgame(Game? game)
        {
            if (game == null) return false;

            int totalMaterial = 0;
            int queenCount = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var p = game.Board[r, c];
                    if (p != null)
                    {
                        if (p.Type == PieceType.Queen) queenCount++;
                        int val = p.Type switch
                        {
                            PieceType.Queen => 9,
                            PieceType.Rook => 5,
                            PieceType.Bishop => 3,
                            PieceType.Knight => 3,
                            PieceType.Pawn => 1,
                            _ => 0
                        };
                        totalMaterial += val;
                    }
                }
            }

            return queenCount == 0 || totalMaterial <= 26;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _engine?.Dispose();
                _engine = null;
                _isDisposed = true;
            }
        }
    }
}
