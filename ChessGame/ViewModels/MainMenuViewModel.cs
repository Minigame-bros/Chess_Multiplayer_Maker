using System.Windows.Input;

namespace ChessGame.ViewModels
{
    public class MainMenuViewModel
    {
        public string PlayerName
        {
            get => MainViewModel.Instance.PlayerName;
            set => MainViewModel.Instance.PlayerName = value;
        }

        public ICommand HotseatCommand { get; }
        public ICommand PlayBotCommand { get; }
        public ICommand LanCommand { get; }
        public ICommand PlayInternetCommand { get; }

        public MainMenuViewModel()
        {
            HotseatCommand = new RelayCommand(_ => 
            {
                MainViewModel.Instance.NavigateTo(new GameRoomViewModel());
            });

            PlayBotCommand = new RelayCommand(_ => 
            {
                MainViewModel.Instance.NavigateTo(new BotSelectionViewModel());
            });

            LanCommand = new RelayCommand(_ => 
            {
                MainViewModel.Instance.NavigateTo(new LanLobbyViewModel());
            });

            PlayInternetCommand = new RelayCommand(_ => 
            {
                MainViewModel.Instance.NavigateTo(new InternetLobbyViewModel());
            });
        }
    }
}
