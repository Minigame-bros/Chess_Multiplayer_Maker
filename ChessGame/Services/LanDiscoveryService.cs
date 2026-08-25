using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChessGame.Models;

namespace ChessGame.Services
{
    public class LanDiscoveryService
    {
        private const int BroadcastPort = 11000;
        private UdpClient? _udpListener;
        private CancellationTokenSource? _listenCts;
        private CancellationTokenSource? _broadcastCts;

        public event Action<UdpDiscoveryMessage>? OnRoomDiscovered;

        public void StartListening()
        {
            StopListening();
            _listenCts = new CancellationTokenSource();
            _udpListener = new UdpClient(BroadcastPort);
            
            Task.Run(async () =>
            {
                try
                {
                    while (!_listenCts.Token.IsCancellationRequested)
                    {
                        var result = await _udpListener.ReceiveAsync();
                        string json = Encoding.UTF8.GetString(result.Buffer);
                        var msg = JsonSerializer.Deserialize<UdpDiscoveryMessage>(json);
                        if (msg != null)
                        {
                            OnRoomDiscovered?.Invoke(msg);
                        }
                    }
                }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }
            }, _listenCts.Token);
        }

        public void StopListening()
        {
            _listenCts?.Cancel();
            _udpListener?.Close();
            _udpListener = null;
        }

        public void StartBroadcasting(UdpDiscoveryMessage roomInfo)
        {
            StopBroadcasting();
            _broadcastCts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                using var udpBroadcaster = new UdpClient();
                udpBroadcaster.EnableBroadcast = true;
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);

                string json = JsonSerializer.Serialize(roomInfo);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                while (!_broadcastCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await udpBroadcaster.SendAsync(bytes, bytes.Length, endPoint);
                        await Task.Delay(2000, _broadcastCts.Token); // Broadcast every 2 seconds
                    }
                    catch (TaskCanceledException) { break; }
                    catch (Exception) { /* Handle transient errors silently in loop */ }
                }
            }, _broadcastCts.Token);
        }

        public void StopBroadcasting()
        {
            _broadcastCts?.Cancel();
        }

        public void StopAll()
        {
            StopListening();
            StopBroadcasting();
        }
    }
}
