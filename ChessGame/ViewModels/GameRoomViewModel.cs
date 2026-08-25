using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using System.Collections.Generic;
using ChessCore;
using ChessGame.Models;

namespace ChessGame.ViewModels
{
    public enum GameStatus
    {
        Waiting,
        Playing,
        Finished
    }

    public class GameRoomViewModel : INotifyPropertyChanged
    {
        private Game _game;
        private ChessGame.Services.INetworkService _network => MainViewModel.Instance.NetworkService;
        private PlayerColor _myColor = PlayerColor.None; // None means Hotseat, Spectator = (PlayerColor)2 if we extended it, but we can just use IsSpectator
        private bool _isSpectator = false;
        private bool _isBoardRotated = false;
        private bool _isRematch = false;
        private int _playerCount = 1;
        
        private GameStatus _currentStatus = GameStatus.Waiting;
        public GameStatus CurrentStatus
        {
            get => _currentStatus;
            set
            {
                _currentStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStartGame));
                OnPropertyChanged(nameof(StatusText));
            }
        }
        
        public bool IsHost => MainViewModel.Instance.IsMultiplayerHost;
        public bool IsHotseatMode => !MainViewModel.Instance.IsMultiplayerHost && !MainViewModel.Instance.IsMultiplayerClient;
        public bool IsMultiplayerMode => !IsHotseatMode;
        
        public bool CanStartGame => (IsHost && _playerCount == 2 && CurrentStatus != GameStatus.Playing) || 
                                    (IsHotseatMode && CurrentStatus != GameStatus.Playing);
        public string StartButtonText => CurrentStatus == GameStatus.Finished ? "Chơi ván mới" : "Bắt đầu";
        private bool _isMyTurn
        {
            get
            {
                if (_isSpectator) return false;
                if (MainViewModel.Instance.IsMultiplayerHost || MainViewModel.Instance.IsMultiplayerClient)
                {
                    if (_myColor == PlayerColor.None) return false;
                    return _myColor == _game.CurrentPlayer;
                }
                return true;
            }
        }
        private Brush _lightSquareColor = new SolidColorBrush(Color.FromRgb(240, 217, 181)); // Classic light wood
        private Brush _darkSquareColor = new SolidColorBrush(Color.FromRgb(181, 136, 99));  // Classic dark wood
        private Brush _selectedColor = new SolidColorBrush(Color.FromArgb(128, 20, 200, 20)); // Semi-transparent green
        private Brush _highlightColor = new SolidColorBrush(Color.FromArgb(128, 200, 20, 20)); // Semi-transparent red

        private SquareViewModel? _selectedSquare;

        public ObservableCollection<SquareViewModel> Squares { get; } = new ObservableCollection<SquareViewModel>();
        
        public ObservableCollection<TurnRecord> MoveHistory { get; } = new ObservableCollection<TurnRecord>();
        private MoveRecord? _selectedHistoryMove;
        public MoveRecord? SelectedHistoryMove
        {
            get => _selectedHistoryMove;
            set
            {
                _selectedHistoryMove = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsViewingHistory));
                if (_selectedHistoryMove != null)
                {
                    SyncBoardToSnapshot(_selectedHistoryMove.BoardSnapshot, _selectedHistoryMove.NextTurn);
                }
                else
                {
                    SyncBoardToUI();
                }
            }
        }
        
        public bool IsViewingHistory => SelectedHistoryMove != null;

        public Brush LightSquareColor
        {
            get => _lightSquareColor;
            set { _lightSquareColor = value; UpdateBoardColors(); OnPropertyChanged(); }
        }

        public Brush DarkSquareColor
        {
            get => _darkSquareColor;
            set { _darkSquareColor = value; UpdateBoardColors(); OnPropertyChanged(); }
        }

        private int _selectedThemeIndex = 0;
        public int SelectedThemeIndex
        {
            get => _selectedThemeIndex;
            set
            {
                _selectedThemeIndex = value;
                OnPropertyChanged();
                ApplyTheme(value);
            }
        }

        private void ApplyTheme(int index)
        {
            switch (index)
            {
                case 0: // Classic Wood
                    LightSquareColor = new SolidColorBrush(Color.FromRgb(240, 217, 181));
                    DarkSquareColor = new SolidColorBrush(Color.FromRgb(181, 136, 99));
                    break;
                case 1: // Green/White
                    LightSquareColor = new SolidColorBrush(Color.FromRgb(238, 238, 210));
                    DarkSquareColor = new SolidColorBrush(Color.FromRgb(118, 150, 86));
                    break;
                case 2: // Gray/White
                    LightSquareColor = new SolidColorBrush(Color.FromRgb(232, 235, 239));
                    DarkSquareColor = new SolidColorBrush(Color.FromRgb(125, 135, 150));
                    break;
            }
        }

        private string _statusText = "Đang chờ bắt đầu...";
        public string StatusText
        {
            get
            {
                if (CurrentStatus == GameStatus.Waiting) return "Phòng đang chờ Bắt đầu...";
                if (CurrentStatus == GameStatus.Finished) return _statusText;
                return _statusText;
            }
            set { _statusText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> ChatMessages { get; } = new ObservableCollection<string>();
        
        private string _chatInput = "";
        public string ChatInput
        {
            get => _chatInput;
            set { _chatInput = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> ColumnLabels { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> RowLabels { get; } = new ObservableCollection<string>();

        public ICommand SquareClickCommand { get; }
        public ICommand SendChatCommand { get; }
        public ICommand LeaveRoomCommand { get; }
        public ICommand ReturnToCurrentGameCommand { get; }
        public ICommand StartGameCommand { get; }
        public ICommand ReviewMoveCommand { get; }

        public GameRoomViewModel()
        {
            _game = new Game();
            InitializeSquares();
            SyncBoardToUI();

            SquareClickCommand = new RelayCommand(OnSquareClicked);
            SendChatCommand = new RelayCommand(OnSendChat);
            LeaveRoomCommand = new RelayCommand(OnLeaveRoom);
            ReturnToCurrentGameCommand = new RelayCommand(OnReturnToCurrentGame);
            StartGameCommand = new RelayCommand(OnStartGame);
            ReviewMoveCommand = new RelayCommand(OnReviewMove);

            if (IsHotseatMode)
            {
                CurrentStatus = GameStatus.Playing;
                StatusText = "Lượt: Trắng";
            }
            else
            {
                SetupNetworking();
            }
        }

        private void OnLeaveRoom(object? parameter)
        {
            // Disconnect if we are in multiplayer
            if (MainViewModel.Instance.IsMultiplayerHost || MainViewModel.Instance.IsMultiplayerClient)
            {
                UnsubscribeNetworking();
                _network.StopAll();
                MainViewModel.Instance.IsMultiplayerHost = false;
                MainViewModel.Instance.IsMultiplayerClient = false;
            }
            MainViewModel.Instance.NavigateTo(new MainMenuViewModel());
        }

        private void OnReturnToCurrentGame(object? parameter)
        {
            SelectedHistoryMove = null;
        }

        private void OnReviewMove(object? parameter)
        {
            if (parameter is MoveRecord record)
            {
                SelectedHistoryMove = record;
            }
        }

        private void OnStartGame(object? parameter)
        {
            if (IsMultiplayerMode)
            {
                if (!IsHost) return;
                if (_playerCount < 2)
                {
                    ChatMessages.Add("[Hệ thống] Không đủ người chơi để bắt đầu.");
                    return;
                }
            }
            
            if (CurrentStatus == GameStatus.Finished)
            {
                if (_isRematch)
                {
                    _myColor = _myColor == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
                    _isBoardRotated = _myColor == PlayerColor.Black;
                }
                
                // Reset game for play again
                _game = new Game();
                MoveHistory.Clear();
                SelectedHistoryMove = null;
                InitializeSquares();
                SyncBoardToUI();
            }

            CurrentStatus = GameStatus.Playing;
            _isRematch = true; // Next time it will be a rematch unless opponent leaves
            OnPropertyChanged(nameof(StartButtonText));
            SyncBoardToUI();

            if (IsMultiplayerMode)
            {
                _ = _network.BroadcastMessageAsync(new NetworkMessage { Type = MessageType.StartGame, Payload = ((int)_myColor).ToString() });
                ChatMessages.Add("[Hệ thống] Trận đấu bắt đầu!");
            }
        }

        private void SetupNetworking()
        {
            if (MainViewModel.Instance.IsMultiplayerHost || MainViewModel.Instance.IsMultiplayerClient)
            {
                UnsubscribeNetworking(); // Ensure no duplicates
                _network.OnMessageReceived += Network_OnMessageReceived;
                _network.OnClientConnected += Network_OnClientConnected;
                _network.OnClientDisconnected += Network_OnClientDisconnected;
                
                if (MainViewModel.Instance.IsMultiplayerHost)
                {
                    // Đợi client tham gia mới random màu
                    ChatMessages.Add("[Hệ thống] Đã tạo phòng, chờ người chơi khác...");
                }
                else
                {
                    // I am client, send JoinRequest
                    var req = new JoinRequest { PlayerName = MainViewModel.Instance.PlayerName, IsSpectator = false };
                    _ = _network.SendMessageAsync(new NetworkMessage { Type = MessageType.JoinRequest, Payload = JsonSerializer.Serialize(req) });
                }
            }
        }

        private void UnsubscribeNetworking()
        {
            _network.OnMessageReceived -= Network_OnMessageReceived;
            _network.OnClientConnected -= Network_OnClientConnected;
            _network.OnClientDisconnected -= Network_OnClientDisconnected;
        }

        private void Network_OnClientConnected(object client)
        {
            _playerCount++;
        }

        private void Network_OnClientDisconnected(object client)
        {
            if (MainViewModel.Instance.IsMultiplayerClient)
            {
                // We are client, and host disconnected -> Host migration
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _network.StopAll();
                    MainViewModel.Instance.IsMultiplayerHost = true;
                    MainViewModel.Instance.IsMultiplayerClient = false;
                    
                    var tcpNetwork = _network as ChessGame.Services.ChessNetworkService;
                    tcpNetwork?.StartHost(11001);

                    var discovery = MainViewModel.Instance.DiscoveryService;
                    discovery.StopListening();
                    
                    var roomInfo = new Models.UdpDiscoveryMessage 
                    {
                        RoomName = $"Phòng của {MainViewModel.Instance.PlayerName}",
                        IpAddress = GetLocalIPAddress(),
                        Port = 11001,
                        PlayerCount = 1
                    };
                    discovery.StartBroadcasting(roomInfo);

                    _playerCount = 1;
                    CurrentStatus = GameStatus.Waiting;
                    _myColor = PlayerColor.White; // Reset to white as new host
                    _isBoardRotated = false;
                    _isRematch = false;
                    
                    _game = new Game();
                    MoveHistory.Clear();
                    SelectedHistoryMove = null;
                    InitializeSquares();
                    SyncBoardToUI();

                    OnPropertyChanged(nameof(IsHost));
                    OnPropertyChanged(nameof(CanStartGame));

                    ChatMessages.Add("[Hệ thống] Chủ phòng đã thoát. Bạn đã trở thành chủ phòng mới. Đang chờ người chơi...");
                });
            }
            else
            {
                _playerCount--;
                if (_playerCount < 2)
                {
                    _myColor = PlayerColor.None; // Reset color to re-randomize for new opponent
                    _isRematch = false;
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(CanStartGame));
                    if (CurrentStatus == GameStatus.Playing && _playerCount < 2)
                    {
                        CurrentStatus = GameStatus.Finished;
                        StatusText = "ĐỐI THỦ ĐÃ THOÁT! BẠN THẮNG!";
                        ChatMessages.Add("[Hệ thống] Đối thủ đã ngắt kết nối. Bạn được xử thắng.");
                        OnPropertyChanged(nameof(StartButtonText));
                    }
                    else
                    {
                        StatusText = "MỘT NGƯỜI CHƠI ĐÃ THOÁT!";
                        ChatMessages.Add("[Hệ thống] Một người chơi đã ngắt kết nối.");
                    }
                });
            }
        }

        private string GetLocalIPAddress()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        private void Network_OnMessageReceived(NetworkMessage msg, object? sender)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                if (msg.Type == MessageType.Chat)
                {
                    var chat = JsonSerializer.Deserialize<ChatMessage>(msg.Payload);
                    if (chat != null) ChatMessages.Add($"{chat.Sender}: {chat.Text}");
                }
                else if (msg.Type == MessageType.JoinRequest && _network.IsHost)
                {
                    var req = JsonSerializer.Deserialize<JoinRequest>(msg.Payload);
                    if (req != null)
                    {
                        int assignedColor = 2; 
                        if (!req.IsSpectator && _playerCount <= 2)
                        {
                            if (_myColor == PlayerColor.None)
                            {
                                _myColor = new System.Random().Next(2) == 0 ? PlayerColor.White : PlayerColor.Black;
                                _isBoardRotated = _myColor == PlayerColor.Black;
                                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                    InitializeSquares();
                                    SyncBoardToUI();
                                });
                            }
                            
                            assignedColor = _myColor == PlayerColor.White ? 1 : 0;
                            
                            OnPropertyChanged(nameof(CanStartGame));
                        }

                        var res = new JoinResponse { Success = true, AssignedColor = assignedColor };
                        _ = _network.SendMessageAsync(new NetworkMessage { Type = MessageType.JoinResponse, Payload = JsonSerializer.Serialize(res) }, sender);
                        ChatMessages.Add($"[Hệ thống] {req.PlayerName} đã tham gia {(assignedColor == 2 ? "với tư cách Khán giả" : "")}.");
                        SendGameStateSync(sender);
                    }
                }
                else if (msg.Type == MessageType.JoinResponse && !_network.IsHost)
                {
                    var res = JsonSerializer.Deserialize<JoinResponse>(msg.Payload);
                    if (res != null && res.Success)
                    {
                        if (res.AssignedColor == 2)
                        {
                            _isSpectator = true;
                            ChatMessages.Add($"[Hệ thống] Đã vào phòng với tư cách Khán giả.");
                        }
                        else
                        {
                            _myColor = res.AssignedColor == 0 ? PlayerColor.White : PlayerColor.Black;
                            if (_myColor == PlayerColor.Black)
                            {
                                _isBoardRotated = true;
                                InitializeSquares();
                                SyncBoardToUI();
                            }
                            ChatMessages.Add($"[Hệ thống] Đã vào phòng. Bạn cầm quân " + (_myColor == PlayerColor.White ? "Trắng" : "Đen"));
                        }
                    }
                }
                else if (msg.Type == MessageType.GameState && !_network.IsHost)
                {
                    var sync = JsonSerializer.Deserialize<GameStateSync>(msg.Payload);
                    if (sync != null)
                    {
                        for (int r = 0; r < 8; r++) for (int c = 0; c < 8; c++) _game.Board[r, c] = null;
                        foreach(var p in sync.Pieces)
                        {
                            Piece piece = (PieceType)p.Type switch
                            {
                                PieceType.Pawn => new Pawn((PlayerColor)p.Color),
                                PieceType.Rook => new Rook((PlayerColor)p.Color),
                                PieceType.Knight => new Knight((PlayerColor)p.Color),
                                PieceType.Bishop => new Bishop((PlayerColor)p.Color),
                                PieceType.Queen => new Queen((PlayerColor)p.Color),
                                PieceType.King => new King((PlayerColor)p.Color),
                                _ => new Pawn(PlayerColor.White)
                            };
                            piece.HasMoved = p.HasMoved;
                            _game.Board[p.Row, p.Col] = piece;
                        }
                        _game.ForceSetCurrentPlayer((PlayerColor)sync.CurrentPlayer);
                        if (!IsViewingHistory) SyncBoardToUI();
                    }
                }
                else if (msg.Type == MessageType.StartGame)
                {
                    if (int.TryParse(msg.Payload, out int hostColorInt))
                    {
                        var hostColor = (PlayerColor)hostColorInt;
                        _myColor = hostColor == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
                        _isBoardRotated = _myColor == PlayerColor.Black;
                    }

                    // Always reset when a new game starts
                    _game = new Game();
                    MoveHistory.Clear();
                    SelectedHistoryMove = null;
                    InitializeSquares();
                    SyncBoardToUI();
                    
                    CurrentStatus = GameStatus.Playing;
                    ChatMessages.Add("[Hệ thống] Trận đấu bắt đầu!");
                }
                else if (msg.Type == MessageType.Move)
                {
                    var move = JsonSerializer.Deserialize<MoveAction>(msg.Payload);
                    if (move != null)
                    {
                        var from = new Position(move.FromRow, move.FromCol);
                        var to = new Position(move.ToRow, move.ToCol);
                        
                        string san = GenerateSan(from, to);
                        _game.MovePiece(from, to);
                        RecordMove(san);

                        if (!IsViewingHistory) SyncBoardToUI();
                    }
                }
            });
        }

        private void OnSendChat(object? parameter)
        {
            if (!string.IsNullOrWhiteSpace(ChatInput))
            {
                var chat = new ChatMessage { Sender = MainViewModel.Instance.PlayerName, Text = ChatInput };
                ChatMessages.Add($"Tôi: {ChatInput}");
                
                if (MainViewModel.Instance.IsMultiplayerHost)
                {
                    _ = _network.BroadcastMessageAsync(new NetworkMessage { Type = MessageType.Chat, Payload = JsonSerializer.Serialize(chat) });
                }
                else if (MainViewModel.Instance.IsMultiplayerClient)
                {
                    _ = _network.SendMessageAsync(new NetworkMessage { Type = MessageType.Chat, Payload = JsonSerializer.Serialize(chat) });
                }
                ChatInput = "";
            }
        }

        private void InitializeSquares()
        {
            Squares.Clear();
            bool flipBoard = _isBoardRotated;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    int row = flipBoard ? 7 - r : r;
                    int col = flipBoard ? 7 - c : c;
                    var pos = new Position(row, col);
                    var bg = (pos.SquareColor() == PlayerColor.White) ? LightSquareColor : DarkSquareColor;
                    Squares.Add(new SquareViewModel(pos, bg));
                }
            }
            ColumnLabels.Clear();
            RowLabels.Clear();
            for (int i = 0; i < 8; i++)
            {
                ColumnLabels.Add(flipBoard ? ((char)('h' - i)).ToString() : ((char)('a' + i)).ToString());
                RowLabels.Add(flipBoard ? (i + 1).ToString() : (8 - i).ToString());
            }
        }

        private void UpdateBoardColors()
        {
            foreach (var sq in Squares)
            {
                sq.Background = (sq.Position.SquareColor() == PlayerColor.White) ? LightSquareColor : DarkSquareColor;
            }
        }

        private void SyncBoardToUI()
        {
            foreach (var sq in Squares)
            {
                Piece? p = _game.Board[sq.Position];
                if (p != null)
                {
                    sq.PieceText = GetPieceUnicode(p);
                    sq.PieceColor = (p.Color == PlayerColor.White) ? Brushes.White : Brushes.Black; 
                }
                else
                {
                    sq.PieceText = "";
                }
            }

            string turnColor = _game.CurrentPlayer == PlayerColor.White ? "Trắng" : "Đen";
            string status = $"Lượt: {turnColor}";

            if (_game.IsCheckmate) 
            {
                CurrentStatus = GameStatus.Finished;
                OnPropertyChanged(nameof(StartButtonText));
                status = $"HẾT CỜ! {(_game.CurrentPlayer == PlayerColor.White ? "ĐEN" : "TRẮNG")} THẮNG!";
            }
            else if (_game.IsStalemate) 
            {
                CurrentStatus = GameStatus.Finished;
                OnPropertyChanged(nameof(StartButtonText));
                status = "HÒA CỜ!";
            }
            else if (_game.IsCheck) status += " (ĐANG BỊ CHIẾU!)";
            
            StatusText = status;
        }
        
        private void SyncBoardToSnapshot(List<PieceDto> snapshot, PlayerColor nextTurn)
        {
            foreach (var sq in Squares)
            {
                var p = snapshot.FirstOrDefault(x => x.Row == sq.Position.Row && x.Col == sq.Position.Col);
                if (p != null)
                {
                    Piece mockPiece = new Pawn((PlayerColor)p.Color); // Type doesn't matter for GetPieceUnicode if we reconstruct it, but let's do it right
                    mockPiece = (PieceType)p.Type switch
                    {
                        PieceType.Pawn => new Pawn((PlayerColor)p.Color),
                        PieceType.Rook => new Rook((PlayerColor)p.Color),
                        PieceType.Knight => new Knight((PlayerColor)p.Color),
                        PieceType.Bishop => new Bishop((PlayerColor)p.Color),
                        PieceType.Queen => new Queen((PlayerColor)p.Color),
                        PieceType.King => new King((PlayerColor)p.Color),
                        _ => mockPiece
                    };
                    sq.PieceText = GetPieceUnicode(mockPiece);
                    sq.PieceColor = (p.Color == (int)PlayerColor.White) ? Brushes.White : Brushes.Black;
                }
                else
                {
                    sq.PieceText = "";
                }
                sq.IsSelected = false;
                sq.IsHighlighted = false;
            }
            
            StatusText = $"Đang xem lại nước đi cũ... Lượt: {(nextTurn == PlayerColor.White ? "Trắng" : "Đen")}";
        }

        private void OnSquareClicked(object? parameter)
        {
            if (_game.IsGameOver || !_isMyTurn || IsViewingHistory || CurrentStatus != GameStatus.Playing) return;

            if (parameter is SquareViewModel clickedSquare)
            {
                if (_selectedSquare == null)
                {
                    Piece? p = _game.Board[clickedSquare.Position];
                    if (p != null && p.Color == _game.CurrentPlayer && (_myColor == PlayerColor.None || p.Color == _myColor))
                    {
                        _selectedSquare = clickedSquare;
                        clickedSquare.IsSelected = true;
                        ShowValidMoves(p, clickedSquare.Position);
                    }
                }
                else
                {
                    if (_selectedSquare == clickedSquare) ClearSelection();
                    else if (clickedSquare.IsHighlighted)
                    {
                        var from = _selectedSquare.Position;
                        var to = clickedSquare.Position;
                        
                        string san = GenerateSan(from, to);

                        if (_game.MovePiece(from, to))
                        {
                            ClearSelection();
                            RecordMove(san);
                            SyncBoardToUI();
                            if (_myColor != PlayerColor.None)
                            {
                                var move = new MoveAction { FromRow = from.Row, FromCol = from.Col, ToRow = to.Row, ToCol = to.Col };
                                var msg = new NetworkMessage { Type = MessageType.Move, Payload = JsonSerializer.Serialize(move) };
                                if (MainViewModel.Instance.IsMultiplayerHost) _ = _network.BroadcastMessageAsync(msg);
                                else _ = _network.SendMessageAsync(msg);
                            }
                        }
                    }
                    else
                    {
                        ClearSelection();
                        Piece? p = _game.Board[clickedSquare.Position];
                        if (p != null && p.Color == _game.CurrentPlayer && (_myColor == PlayerColor.None || p.Color == _myColor))
                        {
                            _selectedSquare = clickedSquare;
                            clickedSquare.IsSelected = true;
                            ShowValidMoves(p, clickedSquare.Position);
                        }
                    }
                }
            }
        }

        private void ShowValidMoves(Piece piece, Position from)
        {
            var validMoves = _game.GetLegalMoves(piece, from);
            foreach (var move in validMoves)
            {
                var sq = Squares.FirstOrDefault(s => s.Position == move);
                if (sq != null) sq.IsHighlighted = true;
            }
        }

        private void ClearSelection()
        {
            if (_selectedSquare != null) { _selectedSquare.IsSelected = false; _selectedSquare = null; }
            foreach (var sq in Squares) sq.IsHighlighted = false;
        }

        private string GenerateSan(Position from, Position to)
        {
            Piece? p = _game.Board[from];
            if (p == null) return "";

            string san = "";
            if (p.Type != PieceType.Pawn)
            {
                san += p.Type switch
                {
                    PieceType.King => "K",
                    PieceType.Queen => "Q",
                    PieceType.Rook => "R",
                    PieceType.Bishop => "B",
                    PieceType.Knight => "N",
                    _ => ""
                };
            }

            bool isCapture = _game.Board[to] != null;
            // Also check for en passant capture
            if (p.Type == PieceType.Pawn && to == _game.Board.EnPassantTarget) isCapture = true;

            if (isCapture)
            {
                if (p.Type == PieceType.Pawn)
                {
                    san += (char)('a' + from.Col);
                }
                san += "x";
            }

            san += $"{(char)('a' + to.Col)}{8 - to.Row}";
            return san;
        }

        private void RecordMove(string san)
        {
            // After move is made, check if checkmate or check to append to san
            if (_game.IsCheckmate) san += "#";
            else if (_game.IsCheck) san += "+";

            var snapshot = new List<PieceDto>();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var p = _game.Board[r, c];
                    if (p != null)
                    {
                        snapshot.Add(new PieceDto { Row = r, Col = c, Type = (int)p.Type, Color = (int)p.Color, HasMoved = p.HasMoved });
                    }
                }
            }

            var record = new MoveRecord
            {
                MoveNumber = _game.HalfmoveClock, // Not strictly accurate, but we manage TurnRecord count
                SanNotation = san,
                BoardSnapshot = snapshot,
                NextTurn = _game.CurrentPlayer
            };

            PlayerColor movedColor = _game.CurrentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
            
            if (movedColor == PlayerColor.White)
            {
                MoveHistory.Add(new TurnRecord
                {
                    TurnNumber = MoveHistory.Count + 1,
                    WhiteMove = record
                });
            }
            else
            {
                var lastTurn = MoveHistory.LastOrDefault();
                if (lastTurn != null)
                {
                    lastTurn.BlackMove = record;
                    // Trigger property change to update UI
                    int idx = MoveHistory.IndexOf(lastTurn);
                    MoveHistory[idx] = new TurnRecord { TurnNumber = lastTurn.TurnNumber, WhiteMove = lastTurn.WhiteMove, BlackMove = lastTurn.BlackMove };
                }
            }
        }

        private void SendGameStateSync(object? targetClient = null)
        {
            var sync = new GameStateSync
            {
                CurrentPlayer = (int)_game.CurrentPlayer,
                Pieces = new List<PieceDto>()
            };

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var p = _game.Board[r, c];
                    if (p != null)
                    {
                        sync.Pieces.Add(new PieceDto { Row = r, Col = c, Type = (int)p.Type, Color = (int)p.Color, HasMoved = p.HasMoved });
                    }
                }
            }

            var msg = new NetworkMessage { Type = MessageType.GameState, Payload = JsonSerializer.Serialize(sync) };
            if (targetClient != null) _ = _network.SendMessageAsync(msg, targetClient);
            else _ = _network.BroadcastMessageAsync(msg);
        }

        private string GetPieceUnicode(Piece p)
        {
            // Using filled symbols for black, outlined for white is standard for unicode chess
            return p.Type switch
            {
                PieceType.King => p.Color == PlayerColor.White ? "♔" : "♚",
                PieceType.Queen => p.Color == PlayerColor.White ? "♕" : "♛",
                PieceType.Rook => p.Color == PlayerColor.White ? "♖" : "♜",
                PieceType.Bishop => p.Color == PlayerColor.White ? "♗" : "♝",
                PieceType.Knight => p.Color == PlayerColor.White ? "♘" : "♞",
                PieceType.Pawn => p.Color == PlayerColor.White ? "♙" : "♟",
                _ => ""
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
