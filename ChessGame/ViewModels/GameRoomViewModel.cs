using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using System.Collections.Generic;
using System.Windows.Threading;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using ChessCore;
using ChessGame.Models;
using ChessBot;

namespace ChessGame.ViewModels
{
    public enum GameStatus
    {
        Waiting,
        Playing,
        Finished
    }

    public enum BotSideChoice
    {
        White,
        Random,
        Black
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
        private DispatcherTimer? _gameTimer;
        private IChessBot? _bot;
        private bool _isBotThinking = false;
        
        public string MyName { get; private set; } = "Player 1";
        public string OpponentName { get; private set; } = "Player 2";
        
        private string _myNameDisplay = "";
        public string MyNameDisplay { get => _myNameDisplay; set { _myNameDisplay = value; OnPropertyChanged(); } }
        private string _opponentNameDisplay = "";
        public string OpponentNameDisplay { get => _opponentNameDisplay; set { _opponentNameDisplay = value; OnPropertyChanged(); } }

        private int _gameTimeMinutes = 10;
        public int GameTimeMinutes
        {
            get => _gameTimeMinutes;
            set
            {
                if (value < 0) value = 0;
                if (value > 30) value = 30;
                _gameTimeMinutes = value;
                OnPropertyChanged();
                if (CurrentStatus == GameStatus.Waiting)
                {
                    UpdatePreGameTimePreview();
                }
            }
        }

        private int _timeIncrementSeconds = 0;
        public int TimeIncrementSeconds
        {
            get => _timeIncrementSeconds;
            set
            {
                if (value < 0) value = 0;
                if (value > 60) value = 60;
                _timeIncrementSeconds = value;
                OnPropertyChanged();
            }
        }

        private TimeSpan _whiteTime;
        private TimeSpan _blackTime;
        private bool _isTimeInfinite = false;

        private string _myTimeDisplay = "";
        public string MyTimeDisplay { get => _myTimeDisplay; set { _myTimeDisplay = value; OnPropertyChanged(); } }

        private string _opponentTimeDisplay = "";
        public string OpponentTimeDisplay { get => _opponentTimeDisplay; set { _opponentTimeDisplay = value; OnPropertyChanged(); } }
        
        private string _myCapturedPiecesDisplay = "";
        public string MyCapturedPiecesDisplay { get => _myCapturedPiecesDisplay; set { _myCapturedPiecesDisplay = value; OnPropertyChanged(); } }

        private string _opponentCapturedPiecesDisplay = "";
        public string OpponentCapturedPiecesDisplay { get => _opponentCapturedPiecesDisplay; set { _opponentCapturedPiecesDisplay = value; OnPropertyChanged(); } }
        
        private GameStatus _currentStatus = GameStatus.Waiting;
        public GameStatus CurrentStatus
        {
            get => _currentStatus;
            set
            {
                _currentStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStartGame));
                OnPropertyChanged(nameof(CanConfigureBotMatch));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsGameOver));
                OnPropertyChanged(nameof(CanResignOrDraw));
                OnPropertyChanged(nameof(CanImport));
                OnPropertyChanged(nameof(IsImportDisabled));
                OnPropertyChanged(nameof(ImportDisabledReason));
            }
        }
        
        private bool _isAnalysisMode = false;
        public bool IsAnalysisMode
        {
            get => _isAnalysisMode;
            private set
            {
                _isAnalysisMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHotseatMode));
                OnPropertyChanged(nameof(IsBotMode));
                OnPropertyChanged(nameof(IsMultiplayerMode));
                OnPropertyChanged(nameof(CanImport));
                OnPropertyChanged(nameof(IsImportDisabled));
                OnPropertyChanged(nameof(ImportDisabledReason));
                OnPropertyChanged(nameof(CanStartGame));
                OnPropertyChanged(nameof(CanConfigureBotMatch));
                OnPropertyChanged(nameof(_isMyTurn));
            }
        }

        private StockfishAnalyzer? _analyzer;
        private string _lastAnalyzedFen = string.Empty;
        private double _evalBarWhiteRatio = 0.5;
        public double EvalBarWhiteRatio
        {
            get => _evalBarWhiteRatio;
            private set
            {
                _evalBarWhiteRatio = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EvalBarBottomRatio));
                OnPropertyChanged(nameof(EvalScoreAtBottom));
            }
        }

        public double EvalBarBottomRatio => Math.Clamp(_isBoardRotated ? 1.0 - _evalBarWhiteRatio : _evalBarWhiteRatio, 0.0, 1.0);
        public bool EvalScoreAtBottom => EvalBarBottomRatio >= 0.5;

        private static readonly Brush EvalWhiteBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240));
        private static readonly Brush EvalBlackBrush = new SolidColorBrush(Color.FromRgb(48, 46, 43));

        public Brush EvalBarBottomBrush => _isBoardRotated ? EvalBlackBrush : EvalWhiteBrush;
        public Brush EvalBarTopBrush => _isBoardRotated ? EvalWhiteBrush : EvalBlackBrush;

        private string _evalScoreText = "0.0";
        public string EvalScoreText
        {
            get => _evalScoreText;
            private set { _evalScoreText = value; OnPropertyChanged(); }
        }

        private string _evalDepthText = "depth=0";
        public string EvalDepthText
        {
            get => _evalDepthText;
            private set { _evalDepthText = value; OnPropertyChanged(); }
        }

        public bool IsHost => MainViewModel.Instance.IsMultiplayerHost && !_isAnalysisMode;
        public bool IsHotseatMode => !MainViewModel.Instance.IsMultiplayerHost && !MainViewModel.Instance.IsMultiplayerClient && _bot == null && !_isAnalysisMode;
        public bool IsMultiplayerMode => (MainViewModel.Instance.IsMultiplayerHost || MainViewModel.Instance.IsMultiplayerClient) && !_isAnalysisMode;
        public bool IsBotMode => _bot != null && !_isAnalysisMode;

        public bool CanImport
        {
            get
            {
                if (IsAnalysisMode) return true;
                if (IsMultiplayerMode) return false;
                if (IsBotMode && CurrentStatus == GameStatus.Playing) return false;
                return true;
            }
        }

        public bool IsImportDisabled => !CanImport;

        public string ImportDisabledReason
        {
            get
            {
                if (IsMultiplayerMode)
                {
                    return "🔒 Chức năng nạp thế cờ (Import) bị khóa trong chế độ Chơi Mạng để tránh lỗi bất đồng bộ và gian lận. Bạn có thể bấm 'Chuyển sang Phân tích' để thử nghiệm độc lập.";
                }
                if (IsBotMode && CurrentStatus == GameStatus.Playing)
                {
                    return "🔒 Không thể nạp thế cờ khi trận đấu với Bot đang diễn ra để chống gian lận. Bạn có thể kết thúc ván đấu hoặc bấm 'Chuyển sang Phân tích' để thử nghiệm tự do.";
                }
                return string.Empty;
            }
        }
        
        private string _botSpeechText = "";
        public string BotSpeechText { get => _botSpeechText; set { _botSpeechText = value; OnPropertyChanged(); } }
        
        public IChessBot? Bot => _bot;
        
        private BotSideChoice _selectedBotSide = BotSideChoice.White;
        public BotSideChoice SelectedBotSide
        {
            get => _selectedBotSide;
            set
            {
                if (_selectedBotSide == value) return;
                _selectedBotSide = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBotSideWhite));
                OnPropertyChanged(nameof(IsBotSideRandom));
                OnPropertyChanged(nameof(IsBotSideBlack));

                if (IsBotMode && CurrentStatus == GameStatus.Waiting)
                {
                    _myColor = value == BotSideChoice.Black ? PlayerColor.Black : PlayerColor.White;
                    _isBoardRotated = value == BotSideChoice.Black;
                    OnPropertyChanged(nameof(IsBoardRotated));
                    InitializeSquares();
                    SyncBoardToUI();
                    UpdatePreGameTimePreview();
                }
            }
        }

        public bool IsBotSideWhite
        {
            get => SelectedBotSide == BotSideChoice.White;
            set { if (value) SelectedBotSide = BotSideChoice.White; }
        }

        public bool IsBotSideRandom
        {
            get => SelectedBotSide == BotSideChoice.Random;
            set { if (value) SelectedBotSide = BotSideChoice.Random; }
        }

        public bool IsBotSideBlack
        {
            get => SelectedBotSide == BotSideChoice.Black;
            set { if (value) SelectedBotSide = BotSideChoice.Black; }
        }

        public bool CanConfigureBotMatch => IsBotMode && CurrentStatus != GameStatus.Playing;

        public bool CanStartGame => !IsAnalysisMode && ((IsHost && _playerCount == 2 && CurrentStatus != GameStatus.Playing) || 
                                    (IsHotseatMode && CurrentStatus != GameStatus.Playing) ||
                                    (IsBotMode && CurrentStatus != GameStatus.Playing));
        public string StartButtonText => CurrentStatus == GameStatus.Finished ? "Chơi ván mới" : "Bắt đầu";
        public bool IsGameOver => CurrentStatus == GameStatus.Finished;

        private bool _isMyTurn
        {
            get
            {
                if (IsAnalysisMode) return true;
                if (_isSpectator) return false;
                if (IsBotMode)
                {
                    return _myColor == _game.CurrentPlayer && !_isBotThinking;
                }
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

        private Position? _lastMoveFrom;
        private Position? _lastMoveTo;

        private SquareViewModel? _selectedSquare;

        private List<PieceDto> _initialBoardSnapshot = new List<PieceDto>();
        private bool _isViewingInitialPosition = false;

        public bool IsBoardRotated => _isBoardRotated;
        public bool CanResignOrDraw => CurrentStatus == GameStatus.Playing && !_isSpectator;

        public ObservableCollection<SquareViewModel> Squares { get; } = new ObservableCollection<SquareViewModel>();
        
        public ObservableCollection<TurnRecord> MoveHistory { get; } = new ObservableCollection<TurnRecord>();
        private MoveRecord? _selectedHistoryMove;
        public MoveRecord? SelectedHistoryMove
        {
            get => _selectedHistoryMove;
            set
            {
                if (_selectedHistoryMove != null)
                {
                    _selectedHistoryMove.IsSelected = false;
                }
                _selectedHistoryMove = value;
                if (_selectedHistoryMove != null)
                {
                    _isViewingInitialPosition = false;
                    _selectedHistoryMove.IsSelected = true;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsViewingHistory));
                if (_selectedHistoryMove != null)
                {
                    SyncBoardToSnapshot(_selectedHistoryMove.BoardSnapshot, _selectedHistoryMove.NextTurn);
                }
                else if (_isViewingInitialPosition)
                {
                    SyncBoardToSnapshot(_initialBoardSnapshot, PlayerColor.White);
                }
                else
                {
                    SyncBoardToUI();
                }
            }
        }
        
        public bool IsViewingHistory => SelectedHistoryMove != null || _isViewingInitialPosition;

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
                if (CurrentStatus == GameStatus.Waiting)
                    return IsBotMode ? "Chọn màu quân & thời gian rồi bấm Bắt đầu" : "Phòng đang chờ Bắt đầu...";
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

        private bool _isPromotionDialogVisible = false;
        public bool IsPromotionDialogVisible
        {
            get => _isPromotionDialogVisible;
            set { _isPromotionDialogVisible = value; OnPropertyChanged(); }
        }

        private Brush _promotionPieceColor = Brushes.White;
        public Brush PromotionPieceColor
        {
            get => _promotionPieceColor;
            set { _promotionPieceColor = value; OnPropertyChanged(); }
        }

        private ImageSource? _promotionQueenImage;
        public ImageSource? PromotionQueenImage
        {
            get => _promotionQueenImage;
            set { _promotionQueenImage = value; OnPropertyChanged(); }
        }

        private ImageSource? _promotionRookImage;
        public ImageSource? PromotionRookImage
        {
            get => _promotionRookImage;
            set { _promotionRookImage = value; OnPropertyChanged(); }
        }

        private ImageSource? _promotionBishopImage;
        public ImageSource? PromotionBishopImage
        {
            get => _promotionBishopImage;
            set { _promotionBishopImage = value; OnPropertyChanged(); }
        }

        private ImageSource? _promotionKnightImage;
        public ImageSource? PromotionKnightImage
        {
            get => _promotionKnightImage;
            set { _promotionKnightImage = value; OnPropertyChanged(); }
        }

        private Position? _pendingPromotionFrom;
        private Position? _pendingPromotionTo;

        private bool _isFenPgnDialogVisible = false;
        public bool IsFenPgnDialogVisible
        {
            get => _isFenPgnDialogVisible;
            set { _isFenPgnDialogVisible = value; OnPropertyChanged(); }
        }

        private string _importText = "";
        public string ImportText
        {
            get => _importText;
            set { _importText = value; OnPropertyChanged(); }
        }

        private string _importErrorText = "";
        public string ImportErrorText
        {
            get => _importErrorText;
            set
            {
                _importErrorText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasImportError));
            }
        }
        public bool HasImportError => !string.IsNullOrEmpty(ImportErrorText);

        private int _activeFenPgnTab = 0;
        public int ActiveFenPgnTab
        {
            get => _activeFenPgnTab;
            set { _activeFenPgnTab = value; OnPropertyChanged(); }
        }

        public string CurrentFen => FenService.GenerateFen(_game);
        public string CurrentPgn => PgnService.GeneratePgn(_game, GetAllSanMoves(), BuildCurrentPgnMetadata());

        public ICommand SquareClickCommand { get; }
        public ICommand SendChatCommand { get; }
        public ICommand LeaveRoomCommand { get; }
        public ICommand ReturnToCurrentGameCommand { get; }
        public ICommand StartGameCommand { get; }
        public ICommand SetTimeControlCommand { get; }
        public ICommand ReviewMoveCommand { get; }
        public ICommand PromoteCommand { get; }
        public ICommand CopyFenCommand { get; }
        public ICommand CopyPgnCommand { get; }
        public ICommand SavePgnFileCommand { get; }
        public ICommand LoadPgnFromFileCommand { get; }
        public ICommand OpenFenPgnDialogCommand { get; }
        public ICommand CloseFenPgnDialogCommand { get; }
        public ICommand ApplyImportCommand { get; }
        public ICommand SwitchToAnalysisModeCommand { get; }
        public ICommand PlayBotMoveCommand { get; }
        public ICommand ResetAnalysisBoardCommand { get; }
        public ICommand FirstMoveCommand { get; }
        public ICommand PreviousMoveCommand { get; }
        public ICommand NextMoveCommand { get; }
        public ICommand LastMoveCommand { get; }
        public ICommand FlipBoardCommand { get; }
        public ICommand ResignCommand { get; }
        public ICommand OfferDrawCommand { get; }

        public GameRoomViewModel()
        {
            _game = new Game();
            InitializeSquares();
            SyncBoardToUI();
            _initialBoardSnapshot = CreateBoardSnapshot();

            SquareClickCommand = new RelayCommand(OnSquareClicked);
            SendChatCommand = new RelayCommand(OnSendChat);
            LeaveRoomCommand = new RelayCommand(OnLeaveRoom);
            ReturnToCurrentGameCommand = new RelayCommand(OnReturnToCurrentGame);
            StartGameCommand = new RelayCommand(OnStartGame);
            SetTimeControlCommand = new RelayCommand(param =>
            {
                if (param is string s)
                {
                    var parts = s.Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int mins) && int.TryParse(parts[1], out int inc))
                    {
                        GameTimeMinutes = mins;
                        TimeIncrementSeconds = inc;
                    }
                }
            });
            ReviewMoveCommand = new RelayCommand(OnReviewMove);
            PromoteCommand = new RelayCommand(OnPromote);
            CopyFenCommand = new RelayCommand(OnCopyFen);
            CopyPgnCommand = new RelayCommand(OnCopyPgn);
            SavePgnFileCommand = new RelayCommand(OnSavePgnFile);
            LoadPgnFromFileCommand = new RelayCommand(OnLoadPgnFromFile);
            OpenFenPgnDialogCommand = new RelayCommand(OnOpenFenPgnDialog);
            CloseFenPgnDialogCommand = new RelayCommand(OnCloseFenPgnDialog);
            ApplyImportCommand = new RelayCommand(OnApplyImport);
            SwitchToAnalysisModeCommand = new RelayCommand(OnSwitchToAnalysisMode);
            PlayBotMoveCommand = new RelayCommand(OnPlayBotMove);
            ResetAnalysisBoardCommand = new RelayCommand(OnResetAnalysisBoard);
            FirstMoveCommand = new RelayCommand(OnFirstMove);
            PreviousMoveCommand = new RelayCommand(OnPreviousMove);
            NextMoveCommand = new RelayCommand(OnNextMove);
            LastMoveCommand = new RelayCommand(OnLastMove);
            FlipBoardCommand = new RelayCommand(OnFlipBoard);
            ResignCommand = new RelayCommand(OnResign);
            OfferDrawCommand = new RelayCommand(OnOfferDraw);

            MyName = MainViewModel.Instance.PlayerName;
            MyNameDisplay = MyName;
            OpponentNameDisplay = "Đang chờ...";

            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(100);
            _gameTimer.Tick += GameTimer_Tick;

            if (IsHotseatMode && !IsAnalysisMode)
            {
                CurrentStatus = GameStatus.Playing;
                StatusText = "Lượt: Trắng";
                Services.AudioService.Instance.PlayGameStartSound();
            }
            else if (!IsAnalysisMode)
            {
                SetupNetworking();
            }
        }

        public GameRoomViewModel(bool isAnalysisMode) : this()
        {
            if (isAnalysisMode)
            {
                OnSwitchToAnalysisMode(null);
            }
        }

        public GameRoomViewModel(IChessBot bot) : this()
        {
            _bot = bot;
            _myColor = PlayerColor.White;
            OpponentName = bot.Name;
            OpponentNameDisplay = $"{bot.Name} (Elo: {bot.Elo})";
            BotSpeechText = bot.GetSpeech(GameState.Start, _game);
            
            CurrentStatus = GameStatus.Waiting;
            OnPropertyChanged(nameof(IsBotMode));
            OnPropertyChanged(nameof(CanStartGame));
            OnPropertyChanged(nameof(CanConfigureBotMatch));
            OnPropertyChanged(nameof(StatusText));
            UpdatePreGameTimePreview();
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
            DisposeAnalyzer();
            MainViewModel.Instance.NavigateTo(new MainMenuViewModel());
        }

        private void OnReturnToCurrentGame(object? parameter)
        {
            _isViewingInitialPosition = false;
            SelectedHistoryMove = null;
        }

        private void OnFirstMove(object? parameter)
        {
            var moves = GetAllMoveRecords();
            if (moves.Count == 0) return;
            if (SelectedHistoryMove != null) SelectedHistoryMove.IsSelected = false;
            _selectedHistoryMove = null;
            _isViewingInitialPosition = true;
            OnPropertyChanged(nameof(SelectedHistoryMove));
            OnPropertyChanged(nameof(IsViewingHistory));
            SyncBoardToSnapshot(_initialBoardSnapshot, PlayerColor.White);
        }

        private void OnPreviousMove(object? parameter)
        {
            var moves = GetAllMoveRecords();
            if (moves.Count == 0) return;

            if (_isViewingInitialPosition)
            {
                return; // Already at the beginning
            }

            if (SelectedHistoryMove == null)
            {
                _isViewingInitialPosition = false;
                SelectedHistoryMove = moves.LastOrDefault();
                return;
            }

            int idx = moves.IndexOf(SelectedHistoryMove);
            if (idx > 0)
            {
                _isViewingInitialPosition = false;
                SelectedHistoryMove = moves[idx - 1];
            }
            else if (idx == 0)
            {
                if (SelectedHistoryMove != null) SelectedHistoryMove.IsSelected = false;
                _selectedHistoryMove = null;
                _isViewingInitialPosition = true;
                OnPropertyChanged(nameof(SelectedHistoryMove));
                OnPropertyChanged(nameof(IsViewingHistory));
                SyncBoardToSnapshot(_initialBoardSnapshot, PlayerColor.White);
            }
        }

        private void OnNextMove(object? parameter)
        {
            var moves = GetAllMoveRecords();
            if (moves.Count == 0) return;

            if (_isViewingInitialPosition)
            {
                _isViewingInitialPosition = false;
                SelectedHistoryMove = moves.FirstOrDefault();
                return;
            }

            if (SelectedHistoryMove == null)
            {
                return; // Already at live position
            }

            int idx = moves.IndexOf(SelectedHistoryMove);
            if (idx >= 0 && idx < moves.Count - 1)
            {
                _isViewingInitialPosition = false;
                SelectedHistoryMove = moves[idx + 1];
            }
            else
            {
                _isViewingInitialPosition = false;
                SelectedHistoryMove = null;
            }
        }

        private void OnLastMove(object? parameter)
        {
            _isViewingInitialPosition = false;
            SelectedHistoryMove = null;
        }

        private void OnFlipBoard(object? parameter)
        {
            _isBoardRotated = !_isBoardRotated;
            OnPropertyChanged(nameof(IsBoardRotated));
            OnPropertyChanged(nameof(EvalBarBottomRatio));
            OnPropertyChanged(nameof(EvalBarBottomBrush));
            OnPropertyChanged(nameof(EvalBarTopBrush));
            OnPropertyChanged(nameof(EvalScoreAtBottom));
            InitializeSquares();
            if (SelectedHistoryMove != null)
            {
                SyncBoardToSnapshot(SelectedHistoryMove.BoardSnapshot, SelectedHistoryMove.NextTurn);
            }
            else if (_isViewingInitialPosition)
            {
                SyncBoardToSnapshot(_initialBoardSnapshot, PlayerColor.White);
            }
            else
            {
                SyncBoardToUI();
            }
        }

        private void OnResign(object? parameter)
        {
            if (CurrentStatus != GameStatus.Playing) return;

            var result = System.Windows.MessageBox.Show(
                "Bạn có chắc chắn muốn đầu hàng ván cờ này không?",
                "Xác nhận đầu hàng",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            CurrentStatus = GameStatus.Finished;
            StopTimers();
            OnPropertyChanged(nameof(StartButtonText));
            OnPropertyChanged(nameof(IsGameOver));
            OnPropertyChanged(nameof(CanResignOrDraw));

            if (IsMultiplayerMode)
            {
                _ = _network.BroadcastMessageAsync(new NetworkMessage { Type = MessageType.Resign, Payload = MainViewModel.Instance.PlayerName });
                StatusText = "BẠN ĐÃ ĐẦU HÀNG. Đối thủ thắng!";
                ChatMessages.Add("[Hệ thống] Bạn đã xin đầu hàng.");
            }
            else if (IsBotMode)
            {
                StatusText = "BẠN ĐÃ ĐẦU HÀNG. Bot đã thắng!";
                if (_bot != null) BotSpeechText = _bot.GetSpeech(GameState.PlayerResigned, _game);
            }
            else
            {
                string loser = _game.CurrentPlayer == PlayerColor.White ? "Trắng" : "Đen";
                string winner = _game.CurrentPlayer == PlayerColor.White ? "ĐEN" : "TRẮNG";
                StatusText = $"{loser} đã đầu hàng. {winner} THẮNG!";
            }
        }

        private void OnOfferDraw(object? parameter)
        {
            if (CurrentStatus != GameStatus.Playing) return;

            if (IsBotMode)
            {
                var drawDecision = System.Windows.MessageBox.Show(
                    "Bạn muốn đề nghị hòa ván cờ với Bot?",
                    "Đề nghị hòa",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (drawDecision != System.Windows.MessageBoxResult.Yes) return;

                int whiteScore = 0, blackScore = 0;
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        var p = _game.Board[r, c];
                        if (p != null)
                        {
                            int val = p.Type switch { PieceType.Queen => 9, PieceType.Rook => 5, PieceType.Bishop => 3, PieceType.Knight => 3, PieceType.Pawn => 1, _ => 0 };
                            if (p.Color == PlayerColor.White) whiteScore += val;
                            else blackScore += val;
                        }
                    }
                }

                bool botAccepts = Math.Abs(whiteScore - blackScore) <= 2 && MoveHistory.Count >= 10;
                if (botAccepts)
                {
                    CurrentStatus = GameStatus.Finished;
                    StopTimers();
                    StatusText = "HÒA CỜ! Bot đã chấp nhận đề nghị hòa.";
                    if (_bot != null) BotSpeechText = _bot.GetSpeech(GameState.Draw, _game);
                    OnPropertyChanged(nameof(StartButtonText));
                    OnPropertyChanged(nameof(IsGameOver));
                    OnPropertyChanged(nameof(CanResignOrDraw));
                    System.Windows.MessageBox.Show("Bot đã đồng ý hòa ván cờ!", "Hòa cờ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    StatusText = "Bot từ chối đề nghị hòa và muốn tiếp tục thi đấu!";
                    if (_bot != null) BotSpeechText = _bot.GetSpeech(GameState.DrawRefused, _game);
                    System.Windows.MessageBox.Show("Bot từ chối lời đề nghị hòa và muốn đánh tiếp!", "Từ chối hòa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            else if (IsHotseatMode)
            {
                var result = System.Windows.MessageBox.Show(
                    "Cả hai bên có đồng ý kết thúc ván cờ với kết quả Hòa không?",
                    "Thỏa thuận hòa cờ",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    CurrentStatus = GameStatus.Finished;
                    StopTimers();
                    StatusText = "HÒA CỜ (Thỏa thuận đôi bên)!";
                    OnPropertyChanged(nameof(StartButtonText));
                    OnPropertyChanged(nameof(IsGameOver));
                    OnPropertyChanged(nameof(CanResignOrDraw));
                }
            }
            else if (IsMultiplayerMode)
            {
                var result = System.Windows.MessageBox.Show(
                    "Gửi lời mời cầu hòa tới đối thủ?",
                    "Cầu hòa",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _ = _network.BroadcastMessageAsync(new NetworkMessage { Type = MessageType.OfferDraw, Payload = MainViewModel.Instance.PlayerName });
                    ChatMessages.Add("[Hệ thống] Bạn đã gửi lời đề nghị hòa cờ tới đối thủ.");
                }
            }
        }

        private List<MoveRecord> GetAllMoveRecords()
        {
            var list = new List<MoveRecord>();
            foreach (var turn in MoveHistory)
            {
                if (turn.WhiteMove != null) list.Add(turn.WhiteMove);
                if (turn.BlackMove != null) list.Add(turn.BlackMove);
            }
            return list;
        }

        private List<PieceDto> CreateBoardSnapshot()
        {
            var snapshot = new List<PieceDto>();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var p = _game.Board[r, c];
                    if (p != null)
                    {
                        snapshot.Add(new PieceDto
                        {
                            Row = r,
                            Col = c,
                            Type = (int)p.Type,
                            Color = (int)p.Color,
                            HasMoved = p.HasMoved
                        });
                    }
                }
            }
            return snapshot;
        }

        private void OnPromote(object? parameter)
        {
            if (parameter is string pieceTypeStr && int.TryParse(pieceTypeStr, out int pieceTypeInt))
            {
                PieceType promotionType = (PieceType)pieceTypeInt;
                IsPromotionDialogVisible = false;

                if (_pendingPromotionFrom.HasValue && _pendingPromotionTo.HasValue)
                {
                    var from = _pendingPromotionFrom.Value;
                    var to = _pendingPromotionTo.Value;
                    
                    string san = GenerateSan(from, to);
                    string promoChar = promotionType switch
                    {
                        PieceType.Rook => "R",
                        PieceType.Bishop => "B",
                        PieceType.Knight => "N",
                        _ => "Q"
                    };
                    san += "=" + promoChar;

                    Piece? targetPiece = _game.Board[to];
                    Piece? movingPiece = _game.Board[from];
                    if (movingPiece?.Type == PieceType.Pawn && to == _game.Board.EnPassantTarget)
                    {
                        targetPiece = new Pawn(PlayerColor.Black);
                    }
                    bool isCastle = movingPiece?.Type == PieceType.King && Math.Abs(from.Col - to.Col) == 2;

                    if (_game.MovePiece(from, to, promotionType))
                    {
                        ClearSelection();
                        HandleTimeIncrement(PlayerColor.White == _game.CurrentPlayer ? PlayerColor.Black : PlayerColor.White);
                        RecordMove(san, from, to);
                        SyncBoardToUI();

                        if (!IsAnalysisMode && (_myColor != PlayerColor.None || MainViewModel.Instance.IsMultiplayerHost || MainViewModel.Instance.IsMultiplayerClient))
                        {
                            var move = new MoveAction { FromRow = from.Row, FromCol = from.Col, ToRow = to.Row, ToCol = to.Col, PromotionPieceType = (int)promotionType };
                            var msg = new NetworkMessage { Type = MessageType.Move, Payload = JsonSerializer.Serialize(move) };
                            if (MainViewModel.Instance.IsMultiplayerHost) _ = _network.BroadcastMessageAsync(msg);
                            else _ = _network.SendMessageAsync(msg);
                        }
                        
                        if (!IsAnalysisMode && IsBotMode && CurrentStatus == GameStatus.Playing)
                        {
                            var playerMove = new BotMove(from, to, promotionType, targetPiece?.Type, movingPiece?.Type ?? PieceType.None, _game.IsCheck, isCastle, _game.FullmoveNumber);
                            if (targetPiece != null)
                            {
                                BotSpeechText = _bot!.GetSpeech(GameState.Captured, _game, playerMove);
                            }
                            else if (_game.IsCheck)
                            {
                                BotSpeechText = _bot!.GetSpeech(GameState.Checked, _game, playerMove);
                            }
                            else
                            {
                                BotSpeechText = _bot!.GetSpeech(GameState.Promotion, _game, playerMove);
                            }
                            _ = HandleBotTurnAsync();
                        }
                    }
                    _pendingPromotionFrom = null;
                    _pendingPromotionTo = null;
                }
            }
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
                if (_isRematch && !IsBotMode)
                {
                    _myColor = _myColor == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
                    _isBoardRotated = _myColor == PlayerColor.Black;
                }
                
                // Reset game for play again
                _game = new Game();
                MoveHistory.Clear();
                SelectedHistoryMove = null;
                _isViewingInitialPosition = false;
                InitializeSquares();
                SyncBoardToUI();
                _initialBoardSnapshot = CreateBoardSnapshot();
            }

            if (IsBotMode)
            {
                _myColor = SelectedBotSide switch
                {
                    BotSideChoice.White => PlayerColor.White,
                    BotSideChoice.Black => PlayerColor.Black,
                    BotSideChoice.Random => Random.Shared.Next(2) == 0 ? PlayerColor.White : PlayerColor.Black,
                    _ => PlayerColor.White
                };
                _isBoardRotated = _myColor == PlayerColor.Black;
            }

            CurrentStatus = GameStatus.Playing;
            _isRematch = true; // Next time it will be a rematch unless opponent leaves
            OnPropertyChanged(nameof(StartButtonText));
            OnPropertyChanged(nameof(CanResignOrDraw));
            OnPropertyChanged(nameof(IsBoardRotated));
            InitializeSquares();
            SyncBoardToUI();
            Services.AudioService.Instance.PlayGameStartSound();

            if (IsMultiplayerMode)
            {
                var payload = new StartGamePayload 
                { 
                    HostColor = (int)_myColor,
                    GameTimeMinutes = GameTimeMinutes,
                    TimeIncrementSeconds = TimeIncrementSeconds
                };
                _ = _network.BroadcastMessageAsync(new NetworkMessage { Type = MessageType.StartGame, Payload = JsonSerializer.Serialize(payload) });
                ChatMessages.Add("[Hệ thống] Trận đấu bắt đầu!");
            }
            else if (IsBotMode)
            {
                StatusText = _game.CurrentPlayer == PlayerColor.White ? "Lượt: Trắng" : "Lượt: Đen";
                BotSpeechText = _bot!.GetSpeech(GameState.Start, _game);
                StartTimers();
                if (_game.CurrentPlayer != _myColor)
                {
                    _ = HandleBotTurnAsync();
                }
                return;
            }
            StartTimers();
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

                        var res = new JoinResponse { Success = true, AssignedColor = assignedColor, HostName = MainViewModel.Instance.PlayerName };
                        _ = _network.SendMessageAsync(new NetworkMessage { Type = MessageType.JoinResponse, Payload = JsonSerializer.Serialize(res) }, sender);
                        OpponentName = req.PlayerName;
                        OpponentNameDisplay = OpponentName;
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
                            OpponentName = res.HostName;
                            OpponentNameDisplay = OpponentName;
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
                    var payload = JsonSerializer.Deserialize<StartGamePayload>(msg.Payload);
                    if (payload != null)
                    {
                        var hostColor = (PlayerColor)payload.HostColor;
                        _myColor = hostColor == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
                        _isBoardRotated = _myColor == PlayerColor.Black;
                        GameTimeMinutes = payload.GameTimeMinutes;
                        TimeIncrementSeconds = payload.TimeIncrementSeconds;
                    }

                    // Always reset when a new game starts
                    _game = new Game();
                    MoveHistory.Clear();
                    SelectedHistoryMove = null;
                    _isViewingInitialPosition = false;
                    InitializeSquares();
                    SyncBoardToUI();
                    _initialBoardSnapshot = CreateBoardSnapshot();
                    
                    CurrentStatus = GameStatus.Playing;
                    OnPropertyChanged(nameof(CanResignOrDraw));
                    OnPropertyChanged(nameof(IsBoardRotated));
                    ChatMessages.Add("[Hệ thống] Trận đấu bắt đầu!");
                    Services.AudioService.Instance.PlayGameStartSound();
                    StartTimers();
                }
                else if (msg.Type == MessageType.Move)
                {
                    var move = JsonSerializer.Deserialize<MoveAction>(msg.Payload);
                    if (move != null)
                    {
                        var from = new Position(move.FromRow, move.FromCol);
                        var to = new Position(move.ToRow, move.ToCol);
                        var promotionType = (PieceType)move.PromotionPieceType;
                        
                        string san = GenerateSan(from, to);
                        if (_game.IsPromotionMove(from, to))
                        {
                             string promoChar = promotionType switch
                             {
                                 PieceType.Rook => "R",
                                 PieceType.Bishop => "B",
                                 PieceType.Knight => "N",
                                 _ => "Q"
                             };
                             san += "=" + promoChar;
                        }

                        _game.MovePiece(from, to, promotionType);
                        HandleTimeIncrement(PlayerColor.White == _game.CurrentPlayer ? PlayerColor.Black : PlayerColor.White);
                        RecordMove(san, from, to);

                        if (!IsViewingHistory) SyncBoardToUI();
                    }
                }
                else if (msg.Type == MessageType.Resign)
                {
                    CurrentStatus = GameStatus.Finished;
                    StopTimers();
                    StatusText = $"{OpponentName} ĐÃ ĐẦU HÀNG. BẠN CHIẾN THẮNG!";
                    ChatMessages.Add($"[Hệ thống] {OpponentName} đã xin đầu hàng. Bạn đã chiến thắng!");
                    OnPropertyChanged(nameof(StartButtonText));
                    OnPropertyChanged(nameof(IsGameOver));
                    OnPropertyChanged(nameof(CanResignOrDraw));
                }
                else if (msg.Type == MessageType.OfferDraw)
                {
                    var drawChoice = System.Windows.MessageBox.Show(
                        $"{OpponentName} muốn xin hòa cờ. Bạn có đồng ý hòa không?",
                        "Lời mời hòa cờ",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);

                    if (drawChoice == System.Windows.MessageBoxResult.Yes)
                    {
                        _ = _network.BroadcastMessageAsync(new NetworkMessage { Type = MessageType.AcceptDraw });
                        CurrentStatus = GameStatus.Finished;
                        StopTimers();
                        StatusText = "HÒA CỜ (Thỏa thuận đôi bên)!";
                        ChatMessages.Add("[Hệ thống] Bạn đã đồng ý hòa ván cờ.");
                        OnPropertyChanged(nameof(StartButtonText));
                        OnPropertyChanged(nameof(IsGameOver));
                        OnPropertyChanged(nameof(CanResignOrDraw));
                    }
                    else
                    {
                        _ = _network.BroadcastMessageAsync(new NetworkMessage { Type = MessageType.DeclineDraw });
                        ChatMessages.Add("[Hệ thống] Bạn đã từ chối lời mời hòa cờ.");
                    }
                }
                else if (msg.Type == MessageType.AcceptDraw)
                {
                    CurrentStatus = GameStatus.Finished;
                    StopTimers();
                    StatusText = "HÒA CỜ! Đối thủ đã đồng ý hòa.";
                    ChatMessages.Add($"[Hệ thống] {OpponentName} đã chấp nhận đề nghị hòa cờ.");
                    OnPropertyChanged(nameof(StartButtonText));
                    OnPropertyChanged(nameof(IsGameOver));
                    OnPropertyChanged(nameof(CanResignOrDraw));
                }
                else if (msg.Type == MessageType.DeclineDraw)
                {
                    ChatMessages.Add($"[Hệ thống] {OpponentName} đã từ chối lời đề nghị hòa cờ.");
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
                    sq.PieceImage = Services.PieceImageService.GetPieceImage(p);
                }
                else
                {
                    sq.PieceText = "";
                    sq.PieceImage = null;
                }
                
                sq.IsLastMove = (sq.Position == _lastMoveFrom || sq.Position == _lastMoveTo);
            }

            string turnColor = _game.CurrentPlayer == PlayerColor.White ? "Trắng" : "Đen";
            string status = $"Lượt: {turnColor}";

            if (_game.IsCheckmate) 
            {
                CurrentStatus = GameStatus.Finished;
                OnPropertyChanged(nameof(StartButtonText));
                status = $"HẾT CỜ! {(_game.CurrentPlayer == PlayerColor.White ? "ĐEN" : "TRẮNG")} THẮNG!";
                if (IsBotMode) BotSpeechText = _game.CurrentPlayer == _myColor ? _bot!.GetSpeech(GameState.Won, _game) : _bot!.GetSpeech(GameState.Lost, _game);
            }
            else if (_game.IsStalemate) 
            {
                CurrentStatus = GameStatus.Finished;
                OnPropertyChanged(nameof(StartButtonText));
                status = "HÒA CỜ!";
                if (IsBotMode) BotSpeechText = _bot!.GetSpeech(GameState.Draw, _game);
            }
            else if (_game.IsCheck) 
            {
                status += " (ĐANG BỊ CHIẾU!)";
                if (IsBotMode && _game.CurrentPlayer != _myColor) BotSpeechText = _bot!.GetSpeech(GameState.Checked, _game);
            }
            
            StatusText = status;
            UpdateCapturedPiecesDisplay();
            OnPropertyChanged(nameof(CurrentFen));
            OnPropertyChanged(nameof(CurrentPgn));
            TriggerEvaluationForCurrentGame();
        }
        
        private void UpdateCapturedPiecesDisplay()
        {
            var whitePieces = new Dictionary<PieceType, int>();
            var blackPieces = new Dictionary<PieceType, int>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var p = _game.Board[r, c];
                    if (p != null)
                    {
                        var dict = p.Color == PlayerColor.White ? whitePieces : blackPieces;
                        if (!dict.ContainsKey(p.Type)) dict[p.Type] = 0;
                        dict[p.Type]++;
                    }
                }
            }

            string GetCapturedString(PlayerColor color)
            {
                var current = color == PlayerColor.White ? whitePieces : blackPieces;
                int pawns = 8 - current.GetValueOrDefault(PieceType.Pawn, 0);
                int knights = 2 - current.GetValueOrDefault(PieceType.Knight, 0);
                int bishops = 2 - current.GetValueOrDefault(PieceType.Bishop, 0);
                int rooks = 2 - current.GetValueOrDefault(PieceType.Rook, 0);
                int queens = 1 - current.GetValueOrDefault(PieceType.Queen, 0);

                if (queens < 0) { pawns += queens; queens = 0; }
                if (rooks < 0) { pawns += rooks; rooks = 0; }
                if (bishops < 0) { pawns += bishops; bishops = 0; }
                if (knights < 0) { pawns += knights; knights = 0; }
                if (pawns < 0) pawns = 0;

                string s = "";
                char pChar = color == PlayerColor.White ? '♙' : '♟';
                char nChar = color == PlayerColor.White ? '♘' : '♞';
                char bChar = color == PlayerColor.White ? '♗' : '♝';
                char rChar = color == PlayerColor.White ? '♖' : '♜';
                char qChar = color == PlayerColor.White ? '♕' : '♛';

                s += new string(pChar, pawns);
                s += new string(nChar, knights);
                s += new string(bChar, bishops);
                s += new string(rChar, rooks);
                s += new string(qChar, queens);
                
                return s;
            }

            int GetMaterial(Dictionary<PieceType, int> current)
            {
                return current.GetValueOrDefault(PieceType.Pawn, 0) * 1 +
                       current.GetValueOrDefault(PieceType.Knight, 0) * 3 +
                       current.GetValueOrDefault(PieceType.Bishop, 0) * 3 +
                       current.GetValueOrDefault(PieceType.Rook, 0) * 5 +
                       current.GetValueOrDefault(PieceType.Queen, 0) * 9;
            }

            string whiteCaptured = GetCapturedString(PlayerColor.White);
            string blackCaptured = GetCapturedString(PlayerColor.Black);
            
            int whiteScore = GetMaterial(whitePieces);
            int blackScore = GetMaterial(blackPieces);

            string GetDisplay(string capturedStr, int myScore, int opScore)
            {
                int diff = myScore - opScore;
                if (diff > 0)
                {
                    return string.IsNullOrEmpty(capturedStr) ? $"+{diff}" : $"{capturedStr} +{diff}";
                }
                return capturedStr;
            }

            if (_myColor == PlayerColor.White)
            {
                MyCapturedPiecesDisplay = GetDisplay(blackCaptured, whiteScore, blackScore);
                OpponentCapturedPiecesDisplay = GetDisplay(whiteCaptured, blackScore, whiteScore);
            }
            else if (_myColor == PlayerColor.Black)
            {
                MyCapturedPiecesDisplay = GetDisplay(whiteCaptured, blackScore, whiteScore);
                OpponentCapturedPiecesDisplay = GetDisplay(blackCaptured, whiteScore, blackScore);
            }
            else 
            {
                if (_isBoardRotated)
                {
                    MyCapturedPiecesDisplay = GetDisplay(whiteCaptured, blackScore, whiteScore);
                    OpponentCapturedPiecesDisplay = GetDisplay(blackCaptured, whiteScore, blackScore);
                }
                else
                {
                    MyCapturedPiecesDisplay = GetDisplay(blackCaptured, whiteScore, blackScore);
                    OpponentCapturedPiecesDisplay = GetDisplay(whiteCaptured, blackScore, whiteScore);
                }
            }
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
                    sq.PieceImage = Services.PieceImageService.GetPieceImage((PieceType)p.Type, (PlayerColor)p.Color);
                }
                else
                {
                    sq.PieceText = "";
                    sq.PieceImage = null;
                }
                sq.IsSelected = false;
                sq.IsHighlighted = false;
                
                if (SelectedHistoryMove != null)
                {
                    sq.IsLastMove = (sq.Position.Row == SelectedHistoryMove.FromRow && sq.Position.Col == SelectedHistoryMove.FromCol) ||
                                    (sq.Position.Row == SelectedHistoryMove.ToRow && sq.Position.Col == SelectedHistoryMove.ToCol);
                }
                else
                {
                    sq.IsLastMove = false;
                }
            }
            
            StatusText = $"Đang xem lại nước đi cũ... Lượt: {(nextTurn == PlayerColor.White ? "Trắng" : "Đen")}";
            TriggerEvaluationForSnapshot(snapshot, nextTurn);
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

                        if (_game.IsPromotionMove(from, to))
                        {
                            _pendingPromotionFrom = from;
                            _pendingPromotionTo = to;
                            var promoColor = _game.CurrentPlayer;
                            PromotionPieceColor = promoColor == PlayerColor.White ? Brushes.White : Brushes.Black;
                            PromotionQueenImage = Services.PieceImageService.GetPieceImage(PieceType.Queen, promoColor);
                            PromotionRookImage = Services.PieceImageService.GetPieceImage(PieceType.Rook, promoColor);
                            PromotionBishopImage = Services.PieceImageService.GetPieceImage(PieceType.Bishop, promoColor);
                            PromotionKnightImage = Services.PieceImageService.GetPieceImage(PieceType.Knight, promoColor);
                            IsPromotionDialogVisible = true;
                            return;
                        }
                        
                        string san = GenerateSan(from, to);

                        Piece? movingPiece = _game.Board[from];
                        Piece? targetPiece = _game.Board[to];
                        if (movingPiece?.Type == PieceType.Pawn && to == _game.Board.EnPassantTarget)
                        {
                            targetPiece = new Pawn(PlayerColor.Black);
                        }
                        bool isCastle = movingPiece?.Type == PieceType.King && Math.Abs(from.Col - to.Col) == 2;

                        if (_game.MovePiece(from, to))
                        {
                            ClearSelection();
                            HandleTimeIncrement(PlayerColor.White == _game.CurrentPlayer ? PlayerColor.Black : PlayerColor.White);
                            RecordMove(san, from, to);
                            SyncBoardToUI();
                            if (!IsAnalysisMode && _myColor != PlayerColor.None && !IsBotMode)
                            {
                                var move = new MoveAction { FromRow = from.Row, FromCol = from.Col, ToRow = to.Row, ToCol = to.Col, PromotionPieceType = 5 };
                                var msg = new NetworkMessage { Type = MessageType.Move, Payload = JsonSerializer.Serialize(move) };
                                if (MainViewModel.Instance.IsMultiplayerHost) _ = _network.BroadcastMessageAsync(msg);
                                else _ = _network.SendMessageAsync(msg);
                            }
                            
                            if (!IsAnalysisMode && IsBotMode && CurrentStatus == GameStatus.Playing)
                            {
                                var playerMove = new BotMove(from, to, null, targetPiece?.Type, movingPiece?.Type ?? PieceType.None, _game.IsCheck, isCastle, _game.FullmoveNumber);
                                if (targetPiece != null)
                                {
                                    BotSpeechText = _bot!.GetSpeech(GameState.Captured, _game, playerMove);
                                }
                                else if (_game.IsCheck)
                                {
                                    BotSpeechText = _bot!.GetSpeech(GameState.Checked, _game, playerMove);
                                }
                                else if (isCastle)
                                {
                                    BotSpeechText = _bot!.GetSpeech(GameState.Castled, _game, playerMove);
                                }
                                _ = HandleBotTurnAsync();
                            }
                        }
                        else
                        {
                            Services.AudioService.Instance.PlayInvalidMoveSound();
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

        public void HandleDragDrop(SquareViewModel source, SquareViewModel target)
        {
            if (_game.IsGameOver || !_isMyTurn || IsViewingHistory || CurrentStatus != GameStatus.Playing) return;

            Piece? p = _game.Board[source.Position];
            if (p != null && p.Color == _game.CurrentPlayer && (_myColor == PlayerColor.None || p.Color == _myColor))
            {
                var validMoves = _game.GetLegalMoves(p, source.Position);
                if (validMoves.Contains(target.Position))
                {
                    ClearSelection();
                    _selectedSquare = source;
                    target.IsHighlighted = true; 
                    OnSquareClicked(target);
                }
            }
        }

        public void SelectForDrag(SquareViewModel square)
        {
            if (_game.IsGameOver || !_isMyTurn || IsViewingHistory || CurrentStatus != GameStatus.Playing) return;

            Piece? p = _game.Board[square.Position];
            if (p != null && p.Color == _game.CurrentPlayer && (_myColor == PlayerColor.None || p.Color == _myColor))
            {
                if (_selectedSquare != square)
                {
                    ClearSelection();
                    _selectedSquare = square;
                    square.IsSelected = true;
                    ShowValidMoves(p, square.Position);
                }
            }
        }

        private async Task HandleBotTurnAsync()
        {
            if (_bot == null || _game.CurrentPlayer == _myColor || CurrentStatus != GameStatus.Playing) return;

            _isBotThinking = true;
            OnPropertyChanged(nameof(_isMyTurn));
            
            if (string.IsNullOrEmpty(BotSpeechText) || MoveHistory.Count <= 1 || Random.Shared.Next(100) < 30)
            {
                BotSpeechText = _bot.GetSpeech(GameState.Thinking, _game);
            }

            try
            {
                var move = await _bot.CalculateMoveAsync(_game);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (CurrentStatus != GameStatus.Playing) return;
                    
                    string san = GenerateSan(move.From, move.To);
                    if (move.Promotion.HasValue)
                    {
                        string promoChar = move.Promotion.Value switch
                        {
                            PieceType.Rook => "R",
                            PieceType.Bishop => "B",
                            PieceType.Knight => "N",
                            _ => "Q"
                        };
                        san += "=" + promoChar;
                    }
                    
                    Piece? movingPiece = _game.Board[move.From];
                    Piece? capturedPiece = _game.Board[move.To];
                    if (movingPiece?.Type == PieceType.Pawn && move.To == _game.Board.EnPassantTarget)
                    {
                        capturedPiece = new Pawn(PlayerColor.White);
                    }
                    bool isCastle = movingPiece?.Type == PieceType.King && Math.Abs(move.From.Col - move.To.Col) == 2;
                    
                    _game.MovePiece(move.From, move.To, move.Promotion ?? PieceType.Queen);
                    HandleTimeIncrement(PlayerColor.White == _game.CurrentPlayer ? PlayerColor.Black : PlayerColor.White);
                    RecordMove(san, move.From, move.To);
                    SyncBoardToUI();

                    if (CurrentStatus == GameStatus.Playing)
                    {
                        var executedMove = new BotMove(move.From, move.To, move.Promotion, capturedPiece?.Type, movingPiece?.Type ?? PieceType.None, _game.IsCheck, isCastle, _game.FullmoveNumber);

                        if (_game.IsCheck)
                        {
                            BotSpeechText = _bot.GetSpeech(GameState.BotDeliveredCheck, _game, executedMove);
                        }
                        else if (capturedPiece != null)
                        {
                            BotSpeechText = _bot.GetSpeech(GameState.BotCaptured, _game, executedMove);
                        }
                        else
                        {
                            BotSpeechText = _bot.GetSpeech(GameState.Moved, _game, executedMove);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusText = "Lỗi Bot: " + ex.Message;
                    CurrentStatus = GameStatus.Finished;
                });
            }
            finally
            {
                _isBotThinking = false;
                System.Windows.Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(nameof(_isMyTurn)));
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

            if (p.Type == PieceType.King && System.Math.Abs(to.Col - from.Col) == 2)
            {
                return to.Col > from.Col ? "O-O" : "O-O-O";
            }

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

                if (p.Type != PieceType.King)
                {
                    var otherPieces = new List<Position>();
                    for (int r = 0; r < 8; r++)
                    {
                        for (int c = 0; c < 8; c++)
                        {
                            var pos = new Position(r, c);
                            if (pos == from) continue;
                            Piece? other = _game.Board[pos];
                            if (other != null && other.Type == p.Type && other.Color == p.Color)
                            {
                                if (_game.GetLegalMoves(other, pos).Contains(to))
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
                            san += (char)('a' + from.Col);
                        }
                        else if (!sameRow)
                        {
                            san += (8 - from.Row).ToString();
                        }
                        else
                        {
                            san += $"{(char)('a' + from.Col)}{8 - from.Row}";
                        }
                    }
                }
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

        private void RecordMove(string san, Position from, Position to)
        {
            _lastMoveFrom = from;
            _lastMoveTo = to;
            
            // After move is made, check if checkmate or check to append to san
            if (_game.IsCheckmate) san += "#";
            else if (_game.IsCheck) san += "+";

            if (san.Contains("#") || _game.IsStalemate) Services.AudioService.Instance.PlayGameEndSound();
            else if (san.Contains("+")) Services.AudioService.Instance.PlayCheckSound();
            else if (san.Contains("=")) Services.AudioService.Instance.PlayPromotionSound();
            else if (san.Contains("O-O")) Services.AudioService.Instance.PlayCastleSound();
            else if (san.Contains("x")) Services.AudioService.Instance.PlayCaptureSound();
            else Services.AudioService.Instance.PlayMoveSound();

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
                FromRow = from.Row,
                FromCol = from.Col,
                ToRow = to.Row,
                ToCol = to.Col,
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

            OnPropertyChanged(nameof(CurrentFen));
            OnPropertyChanged(nameof(CurrentPgn));
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

        private DateTime _lastTickTime;

        private void UpdatePreGameTimePreview()
        {
            if (GameTimeMinutes == 0)
            {
                _isTimeInfinite = true;
                MyTimeDisplay = "∞";
                OpponentTimeDisplay = "∞";
            }
            else
            {
                _isTimeInfinite = false;
                _whiteTime = TimeSpan.FromMinutes(GameTimeMinutes);
                _blackTime = TimeSpan.FromMinutes(GameTimeMinutes);
                UpdateTimeDisplay();
            }
        }

        private void StartTimers()
        {
            if (GameTimeMinutes == 0)
            {
                _isTimeInfinite = true;
                MyTimeDisplay = "∞";
                OpponentTimeDisplay = "∞";
            }
            else
            {
                _isTimeInfinite = false;
                _whiteTime = TimeSpan.FromMinutes(GameTimeMinutes);
                _blackTime = TimeSpan.FromMinutes(GameTimeMinutes);
                UpdateTimeDisplay();
                _lastTickTime = DateTime.UtcNow;
                _gameTimer?.Start();
            }
        }

        private void StopTimers()
        {
            _gameTimer?.Stop();
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            if (CurrentStatus != GameStatus.Playing || _isTimeInfinite || IsViewingHistory) return;

            DateTime now = DateTime.UtcNow;
            TimeSpan elapsed = now - _lastTickTime;
            _lastTickTime = now;

            if (_game.CurrentPlayer == PlayerColor.White)
            {
                _whiteTime = _whiteTime.Subtract(elapsed);
                if (_whiteTime.TotalMilliseconds <= 0)
                {
                    _whiteTime = TimeSpan.Zero;
                    HandleTimeOut(PlayerColor.White);
                }
            }
            else
            {
                _blackTime = _blackTime.Subtract(elapsed);
                if (_blackTime.TotalMilliseconds <= 0)
                {
                    _blackTime = TimeSpan.Zero;
                    HandleTimeOut(PlayerColor.Black);
                }
            }
            UpdateTimeDisplay();
        }

        private void HandleTimeOut(PlayerColor timedOutPlayer)
        {
            _gameTimer?.Stop();
            CurrentStatus = GameStatus.Finished;
            OnPropertyChanged(nameof(StartButtonText));
            
            string winner = timedOutPlayer == PlayerColor.White ? "ĐEN" : "TRẮNG";
            StatusText = $"HẾT THỜI GIAN! {winner} THẮNG!";
            Services.AudioService.Instance.PlayGameEndSound();

            if (IsBotMode && _bot != null)
            {
                BotSpeechText = timedOutPlayer == _myColor
                    ? _bot.GetSpeech(GameState.TimeoutWon, _game)
                    : _bot.GetSpeech(GameState.TimeoutLost, _game);
            }
        }

        private void HandleTimeIncrement(PlayerColor justMovedPlayer)
        {
            if (_isTimeInfinite || CurrentStatus != GameStatus.Playing) return;

            _lastTickTime = DateTime.UtcNow;

            if (TimeIncrementSeconds > 0)
            {
                if (justMovedPlayer == PlayerColor.White)
                {
                    _whiteTime = _whiteTime.Add(TimeSpan.FromSeconds(TimeIncrementSeconds));
                }
                else
                {
                    _blackTime = _blackTime.Add(TimeSpan.FromSeconds(TimeIncrementSeconds));
                }
            }
            UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            if (_isTimeInfinite) return;

            string FormatTime(TimeSpan t) => t.Minutes > 0 ? $"{t.Minutes:D2}:{t.Seconds:D2}" : $"{t.Seconds:D2}.{t.Milliseconds / 100:D1}";

            string whiteDisplay = FormatTime(_whiteTime);
            string blackDisplay = FormatTime(_blackTime);

            if (_myColor == PlayerColor.Black)
            {
                MyTimeDisplay = blackDisplay;
                OpponentTimeDisplay = whiteDisplay;
            }
            else
            {
                MyTimeDisplay = whiteDisplay;
                OpponentTimeDisplay = blackDisplay;
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

        private PgnMetadata BuildCurrentPgnMetadata()
        {
            string whitePlayer = _myColor == PlayerColor.Black ? OpponentNameDisplay : MyNameDisplay;
            string blackPlayer = _myColor == PlayerColor.Black ? MyNameDisplay : OpponentNameDisplay;
            if (IsHotseatMode)
            {
                whitePlayer = "Trắng";
                blackPlayer = "Đen";
            }

            string result = "*";
            if (_game.IsCheckmate)
            {
                result = _game.CurrentPlayer == PlayerColor.White ? "0-1" : "1-0";
            }
            else if (_game.IsStalemate || _game.DrawReason != DrawReason.None)
            {
                result = "1/2-1/2";
            }

            return new PgnMetadata
            {
                Event = IsBotMode ? $"Trận đấu với Bot {OpponentNameDisplay}" : (IsMultiplayerMode ? "Trận đấu mạng (Multiplayer)" : "Đấu trên một máy (Hotseat)"),
                Site = "ChessSolution",
                Date = DateTime.Now.ToString("yyyy.MM.dd"),
                Round = "1",
                White = string.IsNullOrWhiteSpace(whitePlayer) ? "Trắng" : whitePlayer,
                Black = string.IsNullOrWhiteSpace(blackPlayer) ? "Đen" : blackPlayer,
                Result = result
            };
        }

        private List<string> GetAllSanMoves()
        {
            var list = new List<string>();
            foreach (var turn in MoveHistory)
            {
                if (turn.WhiteMove != null && !string.IsNullOrEmpty(turn.WhiteMove.SanNotation))
                {
                    list.Add(turn.WhiteMove.SanNotation);
                }
                if (turn.BlackMove != null && !string.IsNullOrEmpty(turn.BlackMove.SanNotation))
                {
                    list.Add(turn.BlackMove.SanNotation);
                }
            }
            return list;
        }

        private void OnCopyFen(object? parameter)
        {
            try
            {
                string fen = CurrentFen;
                System.Windows.Clipboard.SetText(fen);
                StatusText = "Đã sao chép FEN vào Clipboard!";
            }
            catch (Exception ex)
            {
                StatusText = "Lỗi khi sao chép FEN: " + ex.Message;
            }
        }

        private void OnCopyPgn(object? parameter)
        {
            try
            {
                string pgn = CurrentPgn;
                System.Windows.Clipboard.SetText(pgn);
                StatusText = "Đã sao chép PGN vào Clipboard!";
            }
            catch (Exception ex)
            {
                StatusText = "Lỗi khi sao chép PGN: " + ex.Message;
            }
        }

        private void OnSavePgnFile(object? parameter)
        {
            try
            {
                var saveDlg = new SaveFileDialog
                {
                    Title = "Lưu biên bản ván cờ (PGN)",
                    Filter = "Chess PGN Files (*.pgn)|*.pgn|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = ".pgn",
                    FileName = $"chess_game_{DateTime.Now:yyyyMMdd_HHmmss}.pgn"
                };

                if (saveDlg.ShowDialog() == true)
                {
                    string pgn = CurrentPgn;
                    File.WriteAllText(saveDlg.FileName, pgn, System.Text.Encoding.UTF8);
                    StatusText = $"Đã lưu file PGN: {Path.GetFileName(saveDlg.FileName)}";
                }
            }
            catch (Exception ex)
            {
                StatusText = "Lỗi khi lưu file PGN: " + ex.Message;
            }
        }

        private void OnLoadPgnFromFile(object? parameter)
        {
            try
            {
                var openDlg = new OpenFileDialog
                {
                    Title = "Chọn file PGN hoặc FEN",
                    Filter = "Chess Files (*.pgn;*.fen;*.txt)|*.pgn;*.fen;*.txt|All Files (*.*)|*.*"
                };

                if (openDlg.ShowDialog() == true)
                {
                    string content = File.ReadAllText(openDlg.FileName, System.Text.Encoding.UTF8);
                    ImportText = content;
                    ActiveFenPgnTab = 1;
                    OnApplyImport(null);
                }
            }
            catch (Exception ex)
            {
                ImportErrorText = "Lỗi khi đọc file: " + ex.Message;
            }
        }

        private void OnOpenFenPgnDialog(object? parameter)
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int tabIndex))
            {
                ActiveFenPgnTab = tabIndex;
            }
            ImportErrorText = "";
            OnPropertyChanged(nameof(CanImport));
            OnPropertyChanged(nameof(IsImportDisabled));
            OnPropertyChanged(nameof(ImportDisabledReason));
            OnPropertyChanged(nameof(CurrentFen));
            OnPropertyChanged(nameof(CurrentPgn));
            IsFenPgnDialogVisible = true;
        }

        private void OnCloseFenPgnDialog(object? parameter)
        {
            IsFenPgnDialogVisible = false;
        }

        private void OnSwitchToAnalysisMode(object? parameter)
        {
            if (MainViewModel.Instance.IsMultiplayerHost || MainViewModel.Instance.IsMultiplayerClient)
            {
                UnsubscribeNetworking();
                _network.StopAll();
                MainViewModel.Instance.IsMultiplayerHost = false;
                MainViewModel.Instance.IsMultiplayerClient = false;
            }

            _gameTimer?.Stop();
            _isTimeInfinite = true;
            _myColor = PlayerColor.None; // Cho phép đi cả Trắng lẫn Đen
            _isSpectator = false;
            IsAnalysisMode = true;

            CurrentStatus = GameStatus.Playing;
            MyNameDisplay = "Phân tích viên";
            OpponentNameDisplay = "Bàn cờ Phân tích / Tự do";
            MyTimeDisplay = "∞";
            OpponentTimeDisplay = "∞";

            ImportErrorText = "";
            OnPropertyChanged(nameof(CanImport));
            OnPropertyChanged(nameof(IsImportDisabled));
            OnPropertyChanged(nameof(ImportDisabledReason));
            OnPropertyChanged(nameof(CanStartGame));
            OnPropertyChanged(nameof(IsGameOver));
            OnPropertyChanged(nameof(_isMyTurn));
            OnPropertyChanged(nameof(CurrentFen));
            OnPropertyChanged(nameof(CurrentPgn));

            StatusText = $"Chế độ Phân tích: Lượt {(_game.CurrentPlayer == PlayerColor.White ? "Trắng" : "Đen")}";
            ChatMessages.Add("[Hệ thống] Đã chuyển sang Chế độ Phân tích thế cờ. Bạn có thể tự do thử nghiệm cho cả 2 bên hoặc nạp FEN/PGN.");
            EnsureAnalyzerInitialized();
            TriggerEvaluationForCurrentGame();
        }

        private void OnResetAnalysisBoard(object? parameter)
        {
            _game = new Game();
            MoveHistory.Clear();
            SelectedHistoryMove = null;
            _isViewingInitialPosition = false;
            _lastMoveFrom = null;
            _lastMoveTo = null;
            ClearSelection();
            InitializeSquares();
            SyncBoardToUI();
            _initialBoardSnapshot = CreateBoardSnapshot();
            StatusText = "Bàn cờ phân tích đã được đặt lại ban đầu.";
        }

        private async void OnPlayBotMove(object? parameter)
        {
            if (_isBotThinking || _game.IsGameOver) return;

            if (_bot == null)
            {
                _bot = new ChessBot.Bots.QuanVua.QuanVuaBot();
            }

            _isBotThinking = true;
            OnPropertyChanged(nameof(_isMyTurn));
            StatusText = "Bot đang phân tích và tính toán nước đi...";

            try
            {
                var move = await _bot.CalculateMoveAsync(_game);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    string san = GenerateSan(move.From, move.To);
                    if (move.Promotion.HasValue)
                    {
                        string promoChar = move.Promotion.Value switch
                        {
                            PieceType.Rook => "R",
                            PieceType.Bishop => "B",
                            PieceType.Knight => "N",
                            _ => "Q"
                        };
                        san += "=" + promoChar;
                    }

                    _game.MovePiece(move.From, move.To, move.Promotion ?? PieceType.Queen);
                    RecordMove(san, move.From, move.To);
                    SyncBoardToUI();
                    StatusText = $"Bot gợi ý: {san} (Lượt tiếp theo: {(_game.CurrentPlayer == PlayerColor.White ? "Trắng" : "Đen")})";
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusText = "Lỗi khi Bot phân tích: " + ex.Message;
                });
            }
            finally
            {
                _isBotThinking = false;
                System.Windows.Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(nameof(_isMyTurn)));
            }
        }

        private void OnApplyImport(object? parameter)
        {
            ImportErrorText = "";
            if (!CanImport)
            {
                ImportErrorText = ImportDisabledReason;
                return;
            }

            string input = (ImportText ?? "").Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                ImportErrorText = "Vui lòng nhập hoặc dán mã FEN / PGN.";
                return;
            }

            // Detect if input is PGN or FEN
            bool looksLikePgn = input.Contains('[') || (Regex.IsMatch(input, @"\b1\s*\.") && !input.Contains('/'));
            string? pgnError = null;
            string? fallbackPgnErr = null;

            if (looksLikePgn)
            {
                if (PgnService.TryLoadPgn(input, out var loadedGame, out var pgnMoves, out var metadata, out pgnError))
                {
                    ApplyLoadedPgn(loadedGame!, pgnMoves, metadata);
                    return;
                }
            }

            // Try FEN
            if (FenService.TryParseFen(input, out var loadedFenGame, out var fenError))
            {
                ApplyLoadedFen(loadedFenGame!);
                return;
            }

            // If FEN failed, maybe it's raw PGN without tags
            if (!looksLikePgn && PgnService.TryLoadPgn(input, out var fallbackPgnGame, out var fallbackPgnMoves, out var fallbackMeta, out fallbackPgnErr))
            {
                ApplyLoadedPgn(fallbackPgnGame!, fallbackPgnMoves, fallbackMeta);
                return;
            }

            ImportErrorText = fenError ?? pgnError ?? fallbackPgnErr ?? "Không nhận diện được định dạng FEN hoặc PGN hợp lệ.";
        }

        private void ApplyLoadedFen(Game newGame)
        {
            _game = newGame;
            MoveHistory.Clear();
            SelectedHistoryMove = null;
            _isViewingInitialPosition = false;
            _lastMoveFrom = null;
            _lastMoveTo = null;

            InitializeSquares();
            SyncBoardToUI();
            _initialBoardSnapshot = CreateBoardSnapshot();

            IsFenPgnDialogVisible = false;
            StatusText = "Đã nạp thế cờ từ FEN thành công!";
            Services.AudioService.Instance.PlayGameStartSound();

            if (IsBotMode && !IsAnalysisMode)
            {
                BotSpeechText = _bot!.GetSpeech(GameState.Start, _game);
                if (_game.CurrentPlayer != _myColor && CurrentStatus == GameStatus.Playing)
                {
                    _ = HandleBotTurnAsync();
                }
            }
        }

        private void ApplyLoadedPgn(Game loadedGame, List<PgnMove> moves, PgnMetadata metadata)
        {
            Game replayGame = !string.IsNullOrEmpty(metadata.Fen) 
                ? FenService.LoadGameFromFen(metadata.Fen) 
                : new Game();

            MoveHistory.Clear();
            SelectedHistoryMove = null;
            _isViewingInitialPosition = false;

            TurnRecord? currentTurn = null;

            foreach (var m in moves)
            {
                replayGame.MovePiece(m.From, m.To, m.Promotion ?? PieceType.Queen);

                var snapshot = new List<PieceDto>();
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        var p = replayGame.Board[r, c];
                        if (p != null)
                        {
                            snapshot.Add(new PieceDto
                            {
                                Row = r,
                                Col = c,
                                Type = (int)p.Type,
                                Color = (int)p.Color,
                                HasMoved = p.HasMoved
                            });
                        }
                    }
                }

                var record = new MoveRecord
                {
                    MoveNumber = m.TurnNumber,
                    SanNotation = m.San,
                    FromRow = m.From.Row,
                    FromCol = m.From.Col,
                    ToRow = m.To.Row,
                    ToCol = m.To.Col,
                    BoardSnapshot = snapshot,
                    NextTurn = replayGame.CurrentPlayer
                };

                if (m.Color == PlayerColor.White)
                {
                    currentTurn = new TurnRecord
                    {
                        TurnNumber = MoveHistory.Count + 1,
                        WhiteMove = record
                    };
                    MoveHistory.Add(currentTurn);
                }
                else
                {
                    if (currentTurn == null)
                    {
                        currentTurn = new TurnRecord
                        {
                            TurnNumber = MoveHistory.Count + 1,
                            BlackMove = record
                        };
                        MoveHistory.Add(currentTurn);
                    }
                    else
                    {
                        currentTurn.BlackMove = record;
                        int idx = MoveHistory.IndexOf(currentTurn);
                        MoveHistory[idx] = new TurnRecord
                        {
                            TurnNumber = currentTurn.TurnNumber,
                            WhiteMove = currentTurn.WhiteMove,
                            BlackMove = currentTurn.BlackMove
                        };
                        currentTurn = null;
                    }
                }
            }

            _game = loadedGame;
            if (moves.Count > 0)
            {
                var last = moves.Last();
                _lastMoveFrom = last.From;
                _lastMoveTo = last.To;
            }
            else
            {
                _lastMoveFrom = null;
                _lastMoveTo = null;
            }

            InitializeSquares();
            SyncBoardToUI();
            _initialBoardSnapshot = CreateBoardSnapshot();

            IsFenPgnDialogVisible = false;
            StatusText = $"Đã nạp ván cờ PGN ({moves.Count} nước đi)!";
            Services.AudioService.Instance.PlayGameStartSound();
        }

        private void EnsureAnalyzerInitialized()
        {
            if (_analyzer != null) return;
            try
            {
                _analyzer = new StockfishAnalyzer();
                _analyzer.EvaluationUpdated += Analyzer_EvaluationUpdated;
            }
            catch (Exception ex)
            {
                ChatMessages.Add($"[Hệ thống] Không thể khởi động Stockfish phân tích: {ex.Message}");
            }
        }

        private void DisposeAnalyzer()
        {
            if (_analyzer == null) return;
            _analyzer.EvaluationUpdated -= Analyzer_EvaluationUpdated;
            _analyzer.Dispose();
            _analyzer = null;
        }

        private void TriggerEvaluationForCurrentGame()
        {
            if (!IsAnalysisMode || IsViewingHistory) return;

            if (_game.IsCheckmate)
            {
                _analyzer?.Stop();
                bool whiteWon = _game.CurrentPlayer == PlayerColor.Black;
                EvalBarWhiteRatio = whiteWon ? 1.0 : 0.0;
                EvalScoreText = whiteWon ? "1-0" : "0-1";
                EvalDepthText = "Checkmate";
                _lastAnalyzedFen = string.Empty;
                return;
            }

            if (_game.IsStalemate || _game.DrawReason != DrawReason.None)
            {
                _analyzer?.Stop();
                EvalBarWhiteRatio = 0.5;
                EvalScoreText = "0.0";
                EvalDepthText = "Draw";
                _lastAnalyzedFen = string.Empty;
                return;
            }

            EnsureAnalyzerInitialized();
            string fen = FenService.GenerateFen(_game);
            _lastAnalyzedFen = fen;
            _analyzer?.AnalyzeFen(fen);
        }

        private void TriggerEvaluationForSnapshot(List<PieceDto> snapshot, PlayerColor nextTurn)
        {
            if (!IsAnalysisMode) return;

            EnsureAnalyzerInitialized();
            string fen = BuildFenFromSnapshot(snapshot, nextTurn);
            _lastAnalyzedFen = fen;
            _analyzer?.AnalyzeFen(fen);
        }

        private static string BuildFenFromSnapshot(List<PieceDto> snapshot, PlayerColor nextTurn)
        {
            var board = new char[8, 8];
            foreach (var p in snapshot)
            {
                if (p.Row < 0 || p.Row > 7 || p.Col < 0 || p.Col > 7) continue;
                char c = (PieceType)p.Type switch
                {
                    PieceType.Pawn => 'p',
                    PieceType.Rook => 'r',
                    PieceType.Knight => 'n',
                    PieceType.Bishop => 'b',
                    PieceType.Queen => 'q',
                    PieceType.King => 'k',
                    _ => ' '
                };
                if (c != ' ' && p.Color == (int)PlayerColor.White)
                    c = char.ToUpperInvariant(c);
                board[p.Row, p.Col] = c;
            }

            var sb = new System.Text.StringBuilder();
            for (int r = 0; r < 8; r++)
            {
                int empty = 0;
                for (int c = 0; c < 8; c++)
                {
                    char ch = board[r, c];
                    if (ch == '\0' || ch == ' ')
                    {
                        empty++;
                    }
                    else
                    {
                        if (empty > 0) { sb.Append(empty); empty = 0; }
                        sb.Append(ch);
                    }
                }
                if (empty > 0) sb.Append(empty);
                if (r < 7) sb.Append('/');
            }

            sb.Append(nextTurn == PlayerColor.White ? " w " : " b ");
            sb.Append("- - 0 1");
            return sb.ToString();
        }

        private void Analyzer_EvaluationUpdated(object? sender, EvaluationResult e)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                if (!IsAnalysisMode || e.Fen != _lastAnalyzedFen) return;

                double whiteRatio;
                string scoreText;

                if (e.MateIn.HasValue)
                {
                    int mate = e.MateIn.Value;
                    whiteRatio = mate > 0 ? 1.0 : 0.0;
                    scoreText = mate > 0 ? $"M{mate}" : $"-M{Math.Abs(mate)}";
                }
                else
                {
                    int cp = e.Centipawns ?? 0;
                    // Sigmoid mapping matching Chess.com / Lichess style eval bar
                    whiteRatio = 1.0 / (1.0 + Math.Exp(-0.004 * cp));
                    double pawns = cp / 100.0;
                    scoreText = Math.Abs(pawns) < 0.05
                        ? "0.0"
                        : (pawns > 0 ? $"+{pawns:0.0}" : $"{pawns:0.0}");
                }

                EvalBarWhiteRatio = whiteRatio;
                EvalScoreText = scoreText;
                EvalDepthText = $"depth={e.Depth} | Stockfish";
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
