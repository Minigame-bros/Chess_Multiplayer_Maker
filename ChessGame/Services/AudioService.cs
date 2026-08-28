using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ChessGame.Services
{
    public class AudioService : IDisposable
    {
        private static readonly AudioService _instance = new AudioService();
        public static AudioService Instance => _instance;

        private readonly Dictionary<string, MediaPlayer> _players = new Dictionary<string, MediaPlayer>();
        private MediaPlayer _keepAlivePlayer;

        private AudioService()
        {
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", "ChessDotCom");

            // Lệnh gọi MediaPlayer phải chạy trên luồng giao diện chính (UI Thread) để Windows Media Foundation quản lý
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadSound("Move", Path.Combine(basePath, "di_chuyen_quan_chess_dot_com.wav"));
                    LoadSound("Capture", Path.Combine(basePath, "an_quan_chess_dot_com.wav"));
                    LoadSound("Check", Path.Combine(basePath, "chieu_chesss_dot_com.wav"));
                    LoadSound("GameStart", Path.Combine(basePath, "bat_dau_game_chess_dot_com.wav"));
                    LoadSound("GameEnd", Path.Combine(basePath, "ket_thuc_tran_dau_chess_dot_com.wav"));
                    LoadSound("Castle", Path.Combine(basePath, "nhap_thanh_chess_dot_com.wav"));
                    LoadSound("InvalidMove", Path.Combine(basePath, "nuoc_di_khong_hop_le_chess_dot_com.wav"));
                    LoadSound("Promotion", Path.Combine(basePath, "phong_cap_chess_dot_com.wav"));

                    // HACK: Khởi tạo MediaPlayer chạy ngầm liên tục để chống sleep driver âm thanh
                    try
                    {
                        _keepAlivePlayer = new MediaPlayer();
                        // Để volume = 0.01 (1%) để hệ điều hành không tối ưu hóa ngắt luồng, nhưng tai người gần như không nghe thấy
                        _keepAlivePlayer.Volume = 0.01;
                        _keepAlivePlayer.Open(new Uri(Path.Combine(basePath, "di_chuyen_quan_chess_dot_com.wav"), UriKind.Absolute));
                        
                        _keepAlivePlayer.MediaEnded += (s, e) =>
                        {
                            _keepAlivePlayer.Position = TimeSpan.Zero;
                            _keepAlivePlayer.Play();
                        };
                        
                        _keepAlivePlayer.Play();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioService] KeepAlive player failed: {ex.Message}");
                    }
                });
            }
        }

        private void LoadSound(string key, string path)
        {
            if (File.Exists(path))
            {
                var player = new MediaPlayer();
                player.Open(new Uri(path, UriKind.Absolute));
                _players[key] = player;
            }
        }

        public void PlaySound(string key)
        {
            if (Application.Current?.Dispatcher != null)
            {
                // Đẩy lệnh Play vào hàng đợi của UI Thread (BeginInvoke để không block luồng hiện tại)
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_players.TryGetValue(key, out var player))
                    {
                        // Reset về 0 và phát lại ngay lập tức
                        player.Stop();
                        player.Position = TimeSpan.Zero;
                        player.Play();
                    }
                }));
            }
        }

        public void PlayMoveSound() => PlaySound("Move");
        public void PlayCaptureSound() => PlaySound("Capture");
        public void PlayCheckSound() => PlaySound("Check");
        public void PlayGameStartSound() => PlaySound("GameStart");
        public void PlayGameEndSound() => PlaySound("GameEnd");
        public void PlayCastleSound() => PlaySound("Castle");
        public void PlayInvalidMoveSound() => PlaySound("InvalidMove");
        public void PlayPromotionSound() => PlaySound("Promotion");

        public void Dispose()
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var player in _players.Values)
                    {
                        player.Close();
                    }
                    _players.Clear();
                });
            }
        }
    }
}
