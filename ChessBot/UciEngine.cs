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

        public UciEngine(string executablePath, int elo)
        {
            _elo = elo;
            
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
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
            if (elo > 0)
            {
                bool isStockfish = executablePath.Contains("stockfish", StringComparison.OrdinalIgnoreCase);
                
                if (isStockfish)
                {
                    SendCommand("setoption name UCI_LimitStrength value true");
                    int mappedElo = Math.Max(1320, elo); 
                    SendCommand($"setoption name UCI_Elo value {mappedElo}");
                }
                else
                {
                    // For MadChess or others, we pass the exact Elo
                    SendCommand("setoption name UCI_LimitStrength value true");
                    SendCommand($"setoption name UCI_Elo value {elo}");
                }
                
                if (isStockfish && elo <= 1000)
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
            _stdin.WriteLine(cmd);
            _stdin.Flush();
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

            await Task.Run(() =>
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
            });

            if (bestMoveStr == null || bestMoveStr == "(none)")
            {
                throw new Exception("Engine failed to find a move.");
            }

            var tuple = FenUtility.UciToMove(bestMoveStr);
            return new BotMove(tuple.from, tuple.to, tuple.promo);
        }

        public void Dispose()
        {
            if (_process != null && !_process.HasExited)
            {
                SendCommand("quit");
                _process.WaitForExit(1000);
                if (!_process.HasExited) _process.Kill();
                _process.Dispose();
            }
        }
    }
}
