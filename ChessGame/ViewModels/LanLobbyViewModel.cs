using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ChessGame.ViewModels
{
    public class RoomItem
    {
        public string Name { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public int Port { get; set; }
        public int PlayerCount { get; set; }
        public string DisplayText => $"{Name} - {PlayerCount}/2 Người chơi - {IpAddress}";
    }

    public class LanLobbyViewModel
    {
        public ObservableCollection<RoomItem> Rooms { get; } = new ObservableCollection<RoomItem>();

        public ICommand CreateRoomCommand { get; }
        public ICommand JoinRoomCommand { get; }
        public ICommand BackCommand { get; }

        public LanLobbyViewModel()
        {
            var discovery = MainViewModel.Instance.DiscoveryService;
            var network = MainViewModel.Instance.NetworkService;
            
            discovery.OnRoomDiscovered += (msg) => 
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    // Update or Add
                    var existing = System.Linq.Enumerable.FirstOrDefault(Rooms, r => r.IpAddress == msg.IpAddress && r.Port == msg.Port);
                    if (existing != null)
                    {
                        existing.PlayerCount = msg.PlayerCount;
                        existing.Name = msg.RoomName;
                    }
                    else
                    {
                        Rooms.Add(new RoomItem { Name = msg.RoomName, IpAddress = msg.IpAddress, Port = msg.Port, PlayerCount = msg.PlayerCount });
                    }
                });
            };
            
            discovery.StartListening();

            CreateRoomCommand = new RelayCommand(_ => 
            {
                discovery.StopListening();
                network.StartHost(11001);
                
                var roomInfo = new Models.UdpDiscoveryMessage 
                {
                    RoomName = $"Phòng của {MainViewModel.Instance.PlayerName}",
                    IpAddress = GetLocalIPAddress(),
                    Port = 11001,
                    PlayerCount = 1
                };
                discovery.StartBroadcasting(roomInfo);

                MainViewModel.Instance.IsMultiplayerHost = true;
                MainViewModel.Instance.IsMultiplayerClient = false;
                MainViewModel.Instance.NavigateTo(new GameRoomViewModel());
            });

            JoinRoomCommand = new RelayCommand(async param => 
            {
                if (param is RoomItem room)
                {
                    discovery.StopListening();
                    bool success = await network.ConnectAsync(room.IpAddress, room.Port);
                    if (success)
                    {
                        MainViewModel.Instance.IsMultiplayerHost = false;
                        MainViewModel.Instance.IsMultiplayerClient = true;
                        MainViewModel.Instance.NavigateTo(new GameRoomViewModel());
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Không thể kết nối đến máy chủ.");
                        discovery.StartListening(); // Resume listening if failed
                    }
                }
            });

            BackCommand = new RelayCommand(_ => 
            {
                discovery.StopListening();
                MainViewModel.Instance.NavigateTo(new MainMenuViewModel());
            });
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
    }
}
