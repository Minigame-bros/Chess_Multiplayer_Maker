using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChessCore;

namespace ChessGame.Services
{
    public static class PieceImageService
    {
        private static readonly Dictionary<(PieceType, PlayerColor), ImageSource> _cache = new();
        private static bool _initialized = false;
        private static readonly object _lock = new();

        public static void Initialize()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                LoadPieces();
                _initialized = true;
            }
        }

        private static void LoadPieces()
        {
            PieceType[] types = [PieceType.King, PieceType.Queen, PieceType.Bishop, PieceType.Knight, PieceType.Rook, PieceType.Pawn];
            PlayerColor[] colors = [PlayerColor.White, PlayerColor.Black];

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string piecesDir = Path.Combine(baseDir, "Assets", "Image", "Pieces");

            foreach (var color in colors)
            {
                foreach (var type in types)
                {
                    string fileName = $"{color}_{type}.png";
                    string filePath = Path.Combine(piecesDir, fileName);

                    ImageSource? source = null;

                    if (File.Exists(filePath))
                    {
                        try
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.UriSource = new Uri(filePath, UriKind.Absolute);
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                            bmp.Freeze();
                            source = bmp;
                        }
                        catch
                        {
                            source = null;
                        }
                    }

                    if (source == null)
                    {
                        try
                        {
                            var uri = new Uri($"pack://application:,,,/Assets/Image/Pieces/{fileName}", UriKind.RelativeOrAbsolute);
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.UriSource = uri;
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                            bmp.Freeze();
                            source = bmp;
                        }
                        catch
                        {
                            source = null;
                        }
                    }

                    if (source != null)
                    {
                        _cache[(type, color)] = source;
                    }
                }
            }

            // Fallback: If individual piece files could not be loaded, slice directly from Chess_Piece.png
            if (_cache.Count < 12)
            {
                SliceFromSpriteSheet(baseDir);
            }
        }

        private static void SliceFromSpriteSheet(string baseDir)
        {
            string spritePath = Path.Combine(baseDir, "Assets", "Image", "Chess_Piece.png");
            BitmapSource? spriteSheet = null;

            if (File.Exists(spritePath))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(spritePath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                spriteSheet = bmp;
            }
            else
            {
                try
                {
                    var uri = new Uri("pack://application:,,,/Assets/Image/Chess_Piece.png", UriKind.RelativeOrAbsolute);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = uri;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    spriteSheet = bmp;
                }
                catch
                {
                    spriteSheet = null;
                }
            }

            if (spriteSheet == null) return;

            // Bounding boxes in Chess_Piece.png (735x269)
            var rects = new Dictionary<(PieceType, PlayerColor), Int32Rect>
            {
                { (PieceType.King, PlayerColor.White), new Int32Rect(34, 35, 91, 92) },
                { (PieceType.Queen, PlayerColor.White), new Int32Rect(147, 34, 102, 92) },
                { (PieceType.Bishop, PlayerColor.White), new Int32Rect(271, 34, 90, 91) },
                { (PieceType.Knight, PlayerColor.White), new Int32Rect(389, 38, 88, 88) },
                { (PieceType.Rook, PlayerColor.White), new Int32Rect(515, 43, 75, 83) },
                { (PieceType.Pawn, PlayerColor.White), new Int32Rect(637, 43, 65, 84) },

                { (PieceType.King, PlayerColor.Black), new Int32Rect(34, 153, 91, 92) },
                { (PieceType.Queen, PlayerColor.Black), new Int32Rect(147, 153, 102, 95) },
                { (PieceType.Bishop, PlayerColor.Black), new Int32Rect(271, 152, 90, 91) },
                { (PieceType.Knight, PlayerColor.Black), new Int32Rect(389, 156, 88, 88) },
                { (PieceType.Rook, PlayerColor.Black), new Int32Rect(515, 161, 75, 83) },
                { (PieceType.Pawn, PlayerColor.Black), new Int32Rect(637, 161, 65, 84) }
            };

            foreach (var kvp in rects)
            {
                if (!_cache.ContainsKey(kvp.Key))
                {
                    try
                    {
                        var cropped = new CroppedBitmap(spriteSheet, kvp.Value);
                        cropped.Freeze();
                        _cache[kvp.Key] = cropped;
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }
        }

        public static ImageSource? GetPieceImage(PieceType type, PlayerColor color)
        {
            if (type == PieceType.None || color == PlayerColor.None) return null;

            if (!_initialized)
            {
                Initialize();
            }

            if (_cache.TryGetValue((type, color), out var img))
            {
                return img;
            }

            return null;
        }

        public static ImageSource? GetPieceImage(Piece? piece)
        {
            if (piece == null) return null;
            return GetPieceImage(piece.Type, piece.Color);
        }
    }
}
