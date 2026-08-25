using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChessGame.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private static MainViewModel? _instance;
        public static MainViewModel Instance => _instance ??= new MainViewModel();

        private object _currentViewModel;

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(); }
        }

        public string PlayerName { get; set; } = "Player";
        public bool IsMultiplayerHost { get; set; } = false;
        public bool IsMultiplayerClient { get; set; } = false;

        public Services.ChessNetworkService NetworkService { get; } = new Services.ChessNetworkService();
        public Services.LanDiscoveryService DiscoveryService { get; } = new Services.LanDiscoveryService();

        public MainViewModel()
        {
            _instance = this;
            _currentViewModel = new MainMenuViewModel();
        }

        public void NavigateTo(object viewModel)
        {
            CurrentViewModel = viewModel;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
