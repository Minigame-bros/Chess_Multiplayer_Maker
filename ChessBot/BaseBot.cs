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

        private UciEngine _engine;
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
                string currentDir = baseDir;
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
            return await _engine.GetBestMoveAsync(game);
        }

        public abstract string GetSpeech(GameState state, Game game = null, BotMove? lastMove = null);

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
