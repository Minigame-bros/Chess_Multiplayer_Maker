using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using ChessCore;
using ChessGame.Models;

namespace ChessGame.ViewModels
{
    public class GameRoomViewModel : INotifyPropertyChanged
    {
        private Game _game;
        private ChessGame.Services.ChessNetworkService _network => MainViewModel.Instance.NetworkService;
        private PlayerColor _myColor = PlayerColor.None; // None means Hotseat, Spectator = (PlayerColor)2 if we extended it, but we can just use IsSpectator
        private bool _isSpectator = false;
        private int _playerCount = 1;
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

        private string _statusText = "Lượt: Trắng";
        public string StatusText
        {
            get => _statusText;
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

        public GameRoomViewModel()
        {
            _game = new Game();
            InitializeSquares();
            SyncBoardToUI();

            SquareClickCommand = new RelayCommand(OnSquareClicked);
            SendChatCommand = new RelayCommand(OnSendChat);

            SetupNetworking();
        }

        private void SetupNetworking()
        {
            if (MainViewModel.Instance.IsMultiplayerHost || MainViewModel.Instance.IsMultiplayerClient)
            {
                _network.OnMessageReceived += Network_OnMessageReceived;
                _network.OnClientConnected += Network_OnClientConnected;
                
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

        private void Network_OnClientConnected(System.Net.Sockets.TcpClient client)
        {
            // Do nothing yet, wait for JoinRequest
        }

        private void Network_OnMessageReceived(NetworkMessage msg, System.Net.Sockets.TcpClient? client)
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
                        bool assignAsSpectator = req.IsSpectator || _playerCount >= 2;
                        if (!assignAsSpectator)
                        {
                            _playerCount++;
                            // Randomize color when the first opponent joins
                            _myColor = (new System.Random().Next(2) == 0) ? PlayerColor.White : PlayerColor.Black;
                            InitializeSquares(); // To rotate board if needed
                            SyncBoardToUI(); 
                        }

                        ChatMessages.Add($"[Hệ thống] {req.PlayerName} đã tham gia {(assignAsSpectator ? "với tư cách Khán giả" : "")}.");
                        
                        var res = new JoinResponse 
                        { 
                            Success = true, 
                            AssignedColor = assignAsSpectator ? 2 : (_myColor == PlayerColor.White ? 1 : 0)
                        };
                        _ = _network.SendMessageAsync(new NetworkMessage { Type = MessageType.JoinResponse, Payload = JsonSerializer.Serialize(res) }, client);

                        // Sync State
                        var sync = new GameStateSync { CurrentPlayer = _game.CurrentPlayer == PlayerColor.White ? 0 : 1 };
                        for (int r = 0; r < 8; r++)
                        {
                            for (int c = 0; c < 8; c++)
                            {
                                var p = _game.Board[r, c];
                                if (p != null)
                                {
                                    sync.Pieces.Add(new PieceDto 
                                    {
                                        Row = r, Col = c, HasMoved = p.HasMoved,
                                        Color = p.Color == PlayerColor.White ? 0 : 1,
                                        Type = (int)p.Type
                                    });
                                }
                            }
                        }
                        _ = _network.SendMessageAsync(new NetworkMessage { Type = MessageType.GameState, Payload = JsonSerializer.Serialize(sync) }, client);
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
                            InitializeSquares();
                            SyncBoardToUI(); // To rotate board if needed
                            ChatMessages.Add($"[Hệ thống] Đã vào phòng. Bạn cầm quân " + (_myColor == PlayerColor.White ? "Trắng" : "Đen"));
                        }
                    }
                }
                else if (msg.Type == MessageType.GameState && !_network.IsHost)
                {
                    var sync = JsonSerializer.Deserialize<GameStateSync>(msg.Payload);
                    if (sync != null)
                    {
                        // Clear board
                        for (int r = 0; r < 8; r++) for (int c = 0; c < 8; c++) _game.Board[r, c] = null;
                        
                        foreach(var p in sync.Pieces)
                        {
                            Piece piece = (PieceType)p.Type switch
                            {
                                PieceType.Pawn => new Pawn(p.Color == 0 ? PlayerColor.White : PlayerColor.Black),
                                PieceType.Rook => new Rook(p.Color == 0 ? PlayerColor.White : PlayerColor.Black),
                                PieceType.Knight => new Knight(p.Color == 0 ? PlayerColor.White : PlayerColor.Black),
                                PieceType.Bishop => new Bishop(p.Color == 0 ? PlayerColor.White : PlayerColor.Black),
                                PieceType.Queen => new Queen(p.Color == 0 ? PlayerColor.White : PlayerColor.Black),
                                PieceType.King => new King(p.Color == 0 ? PlayerColor.White : PlayerColor.Black),
                                _ => new Pawn(PlayerColor.White)
                            };
                            piece.HasMoved = p.HasMoved;
                            _game.Board[p.Row, p.Col] = piece;
                        }
                        
                        _game.ForceSetCurrentPlayer(sync.CurrentPlayer == 0 ? PlayerColor.White : PlayerColor.Black);
                        SyncBoardToUI();
                    }
                }
                else if (msg.Type == MessageType.Move)
                {
                    var move = JsonSerializer.Deserialize<MoveAction>(msg.Payload);
                    if (move != null)
                    {
                        Position from = new Position(move.FromRow, move.FromCol);
                        Position to = new Position(move.ToRow, move.ToCol);
                        _game.MovePiece(from, to);
                        SyncBoardToUI();
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
            bool flipBoard = _myColor == PlayerColor.Black;

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

            // Sync Labels
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
                string winner = _game.CurrentPlayer == PlayerColor.White ? "ĐEN" : "TRẮNG";
                status = $"HẾT CỜ! {winner} THẮNG!";
            }
            else if (_game.IsStalemate)
            {
                status = "HÒA CỜ! (Stalemate)";
            }
            else if (_game.IsCheck)
            {
                status += " (ĐANG BỊ CHIẾU!)";
            }
            StatusText = status;
        }

        private void OnSquareClicked(object? parameter)
        {
            if (_game.IsGameOver) return;
            if (!_isMyTurn) return; // Cannot play if not your turn in multiplayer

            if (parameter is SquareViewModel clickedSquare)
            {
                if (_selectedSquare == null)
                {
                    // Select piece
                    Piece? p = _game.Board[clickedSquare.Position];
                    if (p != null && p.Color == _game.CurrentPlayer)
                    {
                        // In multiplayer, only select your own color pieces
                        if (_myColor != PlayerColor.None && p.Color != _myColor) return;

                        _selectedSquare = clickedSquare;
                        clickedSquare.IsSelected = true;
                        ShowValidMoves(p, clickedSquare.Position);
                    }
                }
                else
                {
                    // Move or Deselect
                    if (_selectedSquare == clickedSquare)
                    {
                        ClearSelection();
                    }
                    else
                    {
                        if (clickedSquare.IsHighlighted) // Valid move
                        {
                            var from = _selectedSquare.Position;
                            var to = clickedSquare.Position;
                            bool success = _game.MovePiece(from, to);
                            if (success)
                            {
                                ClearSelection();
                                SyncBoardToUI();

                                // Send move to network
                                if (_myColor != PlayerColor.None)
                                {
                                    var move = new MoveAction { FromRow = from.Row, FromCol = from.Col, ToRow = to.Row, ToCol = to.Col };
                                    var msg = new NetworkMessage { Type = MessageType.Move, Payload = JsonSerializer.Serialize(move) };
                                    if (MainViewModel.Instance.IsMultiplayerHost)
                                    {
                                        _ = _network.BroadcastMessageAsync(msg);
                                    }
                                    else
                                    {
                                        _ = _network.SendMessageAsync(msg);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Change selection if clicking another own piece
                            ClearSelection();
                            Piece? p = _game.Board[clickedSquare.Position];
                            if (p != null && p.Color == _game.CurrentPlayer)
                            {
                                if (_myColor != PlayerColor.None && p.Color != _myColor) return;
                                
                                _selectedSquare = clickedSquare;
                                clickedSquare.IsSelected = true;
                                ShowValidMoves(p, clickedSquare.Position);
                            }
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
                if (sq != null)
                {
                    sq.IsHighlighted = true;
                }
            }
        }

        private void ClearSelection()
        {
            if (_selectedSquare != null)
            {
                _selectedSquare.IsSelected = false;
                _selectedSquare = null;
            }

            foreach (var sq in Squares)
            {
                sq.IsHighlighted = false;
            }
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
