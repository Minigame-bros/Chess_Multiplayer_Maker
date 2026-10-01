using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ChessCore;

namespace ChessBot
{
    public class UciEngine : IDisposable
    {
        private Process _process;
        private StreamWriter _stdin;
        private StreamReader _stdout;
        private int _elo;
        private string _executablePath;

        public UciEngine(string executablePath, int elo)
        {
            _elo = elo;
            _executablePath = executablePath;
            StartProcess();
        }

        private void StartProcess()
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _executablePath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            _process.Start();
            _stdin = _process.StandardInput;
            _stdout = _process.StandardOutput;

            // Initialize UCI
            SendCommand("uci");
            WaitFor("uciok");

            // Configure Elo
            if (_elo > 0)
            {
                bool isStockfish = _executablePath.Contains("stockfish", StringComparison.OrdinalIgnoreCase);
                
                if (isStockfish)
                {
                    SendCommand("setoption name UCI_LimitStrength value true");
                    int mappedElo = Math.Max(1320, _elo); 
                    SendCommand($"setoption name UCI_Elo value {mappedElo}");
                }
                else
                {
                    // For MadChess or others, we pass the exact Elo
                    SendCommand("setoption name UCI_LimitStrength value true");
                    SendCommand($"setoption name UCI_Elo value {_elo}");
                }
                
                if (isStockfish && _elo <= 1000)
                {
                    SendCommand("setoption name Skill Level value 0");
                    SendCommand("setoption name MultiPV value 1");
                }
            }

            SendCommand("isready");
            WaitFor("readyok");
        }

        private void SendCommand(string cmd)
        {
            if (_stdin == null || _process == null || _process.HasExited) return;
            try
            {
                _stdin.WriteLine(cmd);
                _stdin.Flush();
            }
            catch { }
        }

        private void WaitFor(string token)
        {
            string line;
            while ((line = _stdout.ReadLine()) != null)
            {
                if (line.Contains(token)) break;
            }
        }

        public async Task<BotMove> GetBestMoveAsync(Game game)
        {
            string fen = FenUtility.GenerateFen(game);
            SendCommand($"position fen {fen}");
            
            int timeMs = _elo <= 1000 ? 500 : (_elo <= 1600 ? 1000 : 2000);
            
            // Add a fake delay for lower level bots to pretend they are "thinking"
            if (_elo <= 1600)
            {
                int delayMs = new Random().Next(1000, 2500);
                await Task.Delay(delayMs);
            }
            
            SendCommand($"go movetime {timeMs}");

            string bestMoveStr = null;

            var readTask = Task.Run(() =>
            {
                try
                {
                    string line;
                    while ((line = _stdout.ReadLine()) != null)
                    {
                        if (line.StartsWith("bestmove"))
                        {
                            var parts = line.Split(' ');
                            if (parts.Length >= 2)
                            {
                                bestMoveStr = parts[1];
                            }
                            break;
                        }
                    }
                }
                catch { }
            });

            // Wait for 25 seconds max for the engine to respond
            var timeoutTask = Task.Delay(25000);
            var completedTask = await Task.WhenAny(readTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timeout occurred, send stop command just in case engine is still thinking
                SendCommand("stop");
                
                // Wait another 2 seconds for bestmove
                var stopTimeoutTask = Task.Delay(2000);
                var finalTask = await Task.WhenAny(readTask, stopTimeoutTask);
                
                if (finalTask == stopTimeoutTask)
                {
                    // Engine is hung. Restart it and force a random move.
                    RestartEngine();
                    return GetRandomMove(game);
                }
            }

            if (bestMoveStr == null || bestMoveStr == "(none)")
            {
                return GetRandomMove(game);
            }

            var tuple = FenUtility.UciToMove(bestMoveStr);
            return new BotMove(tuple.from, tuple.to, tuple.promo);
        }

        private BotMove GetRandomMove(Game game)
        {
            var r = new Random();
            var possibleMoves = new System.Collections.Generic.List<BotMove>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var pos = new Position(row, col);
                    var piece = game.Board[pos];
                    if (piece != null && piece.Color == game.CurrentPlayer)
                    {
                        var moves = game.GetLegalMoves(piece, pos);
                        foreach (var to in moves)
                        {
                            PieceType? promo = null;
                            if (game.IsPromotionMove(pos, to))
                            {
                                promo = PieceType.Queen; // Default to Queen for random move
                            }
                            possibleMoves.Add(new BotMove(pos, to, promo));
                        }
                    }
                }
            }

            if (possibleMoves.Count > 0)
            {
                return possibleMoves[r.Next(possibleMoves.Count)];
            }

            throw new Exception("No legal moves available.");
        }

        private void RestartEngine()
        {
            Dispose();
            StartProcess();
        }

        public void Dispose()
        {
            if (_process != null && !_process.HasExited)
            {
                try
                {
                    SendCommand("quit");
                    _process.WaitForExit(1000);
                    if (!_process.HasExited) _process.Kill();
                }
                catch { }
                finally
                {
                    _process.Dispose();
                    _process = null;
                }
            }
        }
    }
}
