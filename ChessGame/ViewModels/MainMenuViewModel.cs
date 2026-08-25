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
        public ICommand LanCommand { get; }

        public MainMenuViewModel()
        {
            HotseatCommand = new RelayCommand(_ => 
            {
                MainViewModel.Instance.NavigateTo(new GameRoomViewModel());
            });

            LanCommand = new RelayCommand(_ => 
            {
                MainViewModel.Instance.NavigateTo(new LanLobbyViewModel());
            });
        }
    }
}
