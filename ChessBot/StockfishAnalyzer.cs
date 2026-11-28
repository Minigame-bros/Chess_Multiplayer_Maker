using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChessBot
{
    public sealed class EvaluationResult : EventArgs
    {
        public string Fen { get; }
        public int Depth { get; }
        public int? Centipawns { get; }
        public int? MateIn { get; }

        public EvaluationResult(string fen, int depth, int? centipawns, int? mateIn)
        {
            Fen = fen;
            Depth = depth;
            Centipawns = centipawns;
            MateIn = mateIn;
        }
    }

    /// <summary>
    /// Dedicated full-strength Stockfish analyzer for continuous position evaluation.
    /// Kept separate from UciEngine to avoid affecting bot gameplay logic.
    /// </summary>
    public sealed class StockfishAnalyzer : IDisposable
    {
        private Process? _process;
        private StreamWriter? _stdin;
        private StreamReader? _stdout;
        private readonly object _cmdLock = new object();
        private CancellationTokenSource? _readerCts;
        private Task? _readerTask;

        private bool _isDisposed;
        private bool _isSearching;
        private int _pendingStopCount;
        private string _currentFen = string.Empty;
        private bool _currentWhiteToMove = true;

        public event EventHandler<EvaluationResult>? EvaluationUpdated;

        public StockfishAnalyzer()
        {
            StartProcess();
        }

        private static string ResolveStockfishPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string enginePath = Path.Combine(baseDir, "Engine", "stockfish.exe");

            if (!File.Exists(enginePath))
            {
                string? currentDir = baseDir;
                while (!string.IsNullOrEmpty(currentDir))
                {
                    string potentialPath = Path.Combine(currentDir, "ChessBot", "Engine", "stockfish.exe");
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
                throw new FileNotFoundException($"Cannot find stockfish.exe at {enginePath}.");
            }

            return enginePath;
        }

        private void StartProcess()
        {
            string executablePath = ResolveStockfishPath();

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

            SendCommand("uci");
            WaitForToken("uciok", 5000);

            int threads = Math.Clamp(Environment.ProcessorCount / 2, 1, 4);
            SendCommand("setoption name UCI_LimitStrength value false");
            SendCommand($"setoption name Threads value {threads}");
            SendCommand("setoption name Hash value 64");
            SendCommand("setoption name MultiPV value 1");

            SendCommand("isready");
            WaitForToken("readyok", 5000);

            _readerCts = new CancellationTokenSource();
            _readerTask = Task.Run(() => ReadOutputLoop(_readerCts.Token));
        }

        private void WaitForToken(string token, int timeoutMs)
        {
            if (_stdout == null) return;

            var waitTask = Task.Run(() =>
            {
                try
                {
                    string? line;
                    while ((line = _stdout.ReadLine()) != null)
                    {
                        if (line.Contains(token, StringComparison.Ordinal))
                            return true;
                    }
                }
                catch { }
                return false;
            });

            if (!waitTask.Wait(timeoutMs) || !waitTask.Result)
            {
                throw new TimeoutException($"StockfishAnalyzer failed to respond with '{token}' within {timeoutMs}ms.");
            }
        }

        private void SendCommand(string command)
        {
            if (_stdin == null || _process == null || _process.HasExited) return;
            try
            {
                _stdin.WriteLine(command);
                _stdin.Flush();
            }
            catch { }
        }

        public void AnalyzeFen(string fen, int maxDepth = 22)
        {
            if (_isDisposed || string.IsNullOrWhiteSpace(fen)) return;

            lock (_cmdLock)
            {
                if (_isDisposed) return;

                if (_isSearching)
                {
                    _pendingStopCount++;
                    SendCommand("stop");
                    _isSearching = false;
                }

                _currentFen = fen;
                _currentWhiteToMove = IsWhiteToMoveFromFen(fen);
                _isSearching = true;

                SendCommand($"position fen {fen}");
                SendCommand($"go depth {maxDepth}");
            }
        }

        public void Stop()
        {
            lock (_cmdLock)
            {
                if (_isDisposed || !_isSearching) return;
                _pendingStopCount++;
                SendCommand("stop");
                _isSearching = false;
            }
        }

        private static bool IsWhiteToMoveFromFen(string fen)
        {
            var parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length < 2 || parts[1] == "w";
        }

        private void ReadOutputLoop(CancellationToken ct)
        {
            try
            {
                string? line;
                while (!ct.IsCancellationRequested && _stdout != null && (line = _stdout.ReadLine()) != null)
                {
                    if (line.StartsWith("bestmove", StringComparison.Ordinal))
                    {
                        lock (_cmdLock)
                        {
                            if (_pendingStopCount > 0)
                            {
                                _pendingStopCount--;
                            }
                            else
                            {
                                _isSearching = false;
                            }
                        }
                        continue;
                    }

                    if (!line.StartsWith("info ", StringComparison.Ordinal))
                        continue;

                    string fenSnapshot;
                    bool whiteToMoveSnapshot;

                    lock (_cmdLock)
                    {
                        if (_pendingStopCount > 0 || !_isSearching)
                            continue;

                        fenSnapshot = _currentFen;
                        whiteToMoveSnapshot = _currentWhiteToMove;
                    }

                    if (TryParseInfoLine(line, fenSnapshot, whiteToMoveSnapshot, out var result))
                    {
                        EvaluationUpdated?.Invoke(this, result!);
                    }
                }
            }
            catch { }
        }

        private static bool TryParseInfoLine(string line, string fen, bool whiteToMove, out EvaluationResult? result)
        {
            result = null;

            // Skip aspiration window bounds so the bar only updates on exact evaluations
            if (line.Contains(" lowerbound", StringComparison.Ordinal) ||
                line.Contains(" upperbound", StringComparison.Ordinal))
            {
                return false;
            }

            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int depth = 0;
            int? cp = null;
            int? mate = null;

            for (int i = 1; i < tokens.Length; i++)
            {
                if (tokens[i] == "depth" && i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out int d))
                {
                    depth = d;
                    i++;
                }
                else if (tokens[i] == "score" && i + 2 < tokens.Length)
                {
                    if (tokens[i + 1] == "cp" && int.TryParse(tokens[i + 2], out int cpVal))
                    {
                        cp = whiteToMove ? cpVal : -cpVal;
                        i += 2;
                    }
                    else if (tokens[i + 1] == "mate" && int.TryParse(tokens[i + 2], out int mateVal))
                    {
                        mate = whiteToMove ? mateVal : -mateVal;
                        i += 2;
                    }
                }
            }

            if (depth <= 0 || (!cp.HasValue && !mate.HasValue))
                return false;

            result = new EvaluationResult(fen, depth, cp, mate);
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            lock (_cmdLock)
            {
                if (_isDisposed) return;
                _isDisposed = true;
                _isSearching = false;
                try
                {
                    SendCommand("stop");
                    SendCommand("quit");
                }
                catch { }
            }

            try { _readerCts?.Cancel(); } catch { }

            try
            {
                if (_process != null && !_process.HasExited)
                {
                    if (!_process.WaitForExit(300))
                    {
                        _process.Kill();
                    }
                }
            }
            catch { }

            _readerCts?.Dispose();
            _stdin?.Dispose();
            _stdout?.Dispose();
            _process?.Dispose();
        }
    }
}
