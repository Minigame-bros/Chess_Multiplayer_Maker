using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ChessGame.Models;

namespace ChessGame.ViewModels
{
    public class InternetLobbyViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<RoomInfo> AvailableRooms { get; } = new ObservableCollection<RoomInfo>();
        
        private RoomInfo? _selectedRoom;
        public RoomInfo? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                _selectedRoom = value;
                OnPropertyChanged();
            }
        }

        private string _serverUrl = "https://localhost:5001";
        public string ServerUrl
        {
            get => _serverUrl;
            set { _serverUrl = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CreateRoomCommand { get; }
        public ICommand JoinRoomCommand { get; }
        public ICommand BackCommand { get; }

        public InternetLobbyViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await FetchRoomsAsync());
            
            CreateRoomCommand = new RelayCommand(async _ => 
            {
                var signalRNetwork = new Services.SignalRNetworkService();
                MainViewModel.Instance.NetworkService = signalRNetwork;

                string roomId = Guid.NewGuid().ToString().Substring(0, 8);
                await signalRNetwork.StartHostAsync($"{ServerUrl}/chesshub", roomId, MainViewModel.Instance.PlayerName);

                MainViewModel.Instance.IsMultiplayerHost = true;
                MainViewModel.Instance.IsMultiplayerClient = false;
                MainViewModel.Instance.NavigateTo(new GameRoomViewModel());
            });

            JoinRoomCommand = new RelayCommand(async param => 
            {
                if (param is RoomInfo room)
                {
                    var signalRNetwork = new Services.SignalRNetworkService();
                    MainViewModel.Instance.NetworkService = signalRNetwork;

                    bool success = await signalRNetwork.ConnectAsync($"{ServerUrl}/chesshub", room.RoomId, MainViewModel.Instance.PlayerName);
                    if (success)
                    {
                        MainViewModel.Instance.IsMultiplayerHost = false;
                        MainViewModel.Instance.IsMultiplayerClient = true;
                        MainViewModel.Instance.NavigateTo(new GameRoomViewModel());
                    }
                }
            });

            BackCommand = new RelayCommand(_ => 
            {
                MainViewModel.Instance.NavigateTo(new MainMenuViewModel());
            });
            
            _ = FetchRoomsAsync();
        }

        private async Task FetchRoomsAsync()
        {
            try
            {
                using var client = new HttpClient();
                var rooms = await client.GetFromJsonAsync<RoomInfo[]>($"{ServerUrl}/api/room");
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableRooms.Clear();
                    if (rooms != null)
                    {
                        foreach (var room in rooms)
                        {
                            AvailableRooms.Add(room);
                        }
                    }
                });
            }
            catch
            {
                // Ignore connection errors during fetch
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
