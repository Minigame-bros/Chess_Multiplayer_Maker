using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChessCore;

namespace ChessGame.Models
{
    public class MoveRecord : INotifyPropertyChanged
    {
        private bool _isSelected;

        public int MoveNumber { get; set; }
        public string SanNotation { get; set; } = string.Empty;

        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }

        // Snapshot of the pieces at this point in time
        public List<PieceDto> BoardSnapshot { get; set; } = new List<PieceDto>();

        // We can optionally store whether it was White's or Black's turn after this move
        public PlayerColor NextTurn { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string DisplayText => $"{MoveNumber}. {SanNotation}";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

