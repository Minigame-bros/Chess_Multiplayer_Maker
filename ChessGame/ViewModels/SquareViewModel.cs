using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ChessCore;

namespace ChessGame.ViewModels
{
    public class SquareViewModel : INotifyPropertyChanged
    {
        private Brush _background;
        private string _pieceText = "";
        private Brush _pieceColor = Brushes.Black;
        private bool _isSelected;
        private bool _isHighlighted;
        private Position _position;

        public Position Position 
        { 
            get => _position; 
            set { _position = value; OnPropertyChanged(); }
        }

        public Brush Background
        {
            get => _background;
            set { _background = value; OnPropertyChanged(); }
        }

        public string PieceText
        {
            get => _pieceText;
            set { _pieceText = value; OnPropertyChanged(); }
        }

        public Brush PieceColor
        {
            get => _pieceColor;
            set { _pieceColor = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set { _isHighlighted = value; OnPropertyChanged(); }
        }

        private bool _isLastMove;
        public bool IsLastMove
        {
            get => _isLastMove;
            set { _isLastMove = value; OnPropertyChanged(); }
        }

        public SquareViewModel(Position pos, Brush bg)
        {
            _position = pos;
            _background = bg;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
