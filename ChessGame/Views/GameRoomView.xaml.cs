using System.Windows.Controls;

namespace ChessGame.Views
{
    public partial class GameRoomView : UserControl
    {
        private System.Windows.Point _startPoint;
        private ViewModels.SquareViewModel _draggedSquare;
        private bool _isDragging = false;

        public GameRoomView()
        {
            InitializeComponent();
        }

        private void Square_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement fe && fe.DataContext is ViewModels.SquareViewModel squareVm)
            {
                if (!string.IsNullOrEmpty(squareVm.PieceText))
                {
                    _startPoint = e.GetPosition(BoardContainer);
                    _draggedSquare = squareVm;
                    _isDragging = false;
                }
                else
                {
                    _draggedSquare = null;
                }
            }
        }

        private void BoardContainer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && _draggedSquare != null)
            {
                System.Windows.Point currentPos = e.GetPosition(BoardContainer);
                if (!_isDragging)
                {
                    System.Windows.Vector diff = _startPoint - currentPos;
                    if (System.Math.Abs(diff.X) > System.Windows.SystemParameters.MinimumHorizontalDragDistance ||
                        System.Math.Abs(diff.Y) > System.Windows.SystemParameters.MinimumVerticalDragDistance)
                    {
                        _isDragging = true;
                        
                        if (this.DataContext is ViewModels.GameRoomViewModel vm)
                        {
                            vm.SelectForDrag(_draggedSquare);
                        }

                        _draggedSquare.IsDragging = true;
                        
                        // Wait for layout update so ActualWidth is correct
                        DragPieceText.Text = _draggedSquare.PieceText;
                        DragPieceText.Foreground = _draggedSquare.PieceColor;
                        DragCanvas.Visibility = System.Windows.Visibility.Visible;
                        DragPieceText.UpdateLayout();
                        
                        BoardContainer.CaptureMouse();
                    }
                }

                if (_isDragging)
                {
                    System.Windows.Controls.Canvas.SetLeft(DragPieceText, currentPos.X - (DragPieceText.ActualWidth / 2));
                    System.Windows.Controls.Canvas.SetTop(DragPieceText, currentPos.Y - (DragPieceText.ActualHeight / 2));
                }
            }
        }

        private void BoardContainer_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                BoardContainer.ReleaseMouseCapture();
                DragCanvas.Visibility = System.Windows.Visibility.Collapsed;
                if (_draggedSquare != null)
                {
                    _draggedSquare.IsDragging = false;
                }

                System.Windows.Point dropPos = e.GetPosition(BoardItemsControl);
                var element = BoardItemsControl.InputHitTest(dropPos) as System.Windows.FrameworkElement;
                
                ViewModels.SquareViewModel targetSquare = null;
                while (element != null)
                {
                    if (element.DataContext is ViewModels.SquareViewModel svm)
                    {
                        targetSquare = svm;
                        break;
                    }
                    element = System.Windows.Media.VisualTreeHelper.GetParent(element) as System.Windows.FrameworkElement;
                }

                if (targetSquare != null && _draggedSquare != null && _draggedSquare != targetSquare)
                {
                    if (this.DataContext is ViewModels.GameRoomViewModel vm)
                    {
                        vm.HandleDragDrop(_draggedSquare, targetSquare);
                    }
                }
                
                _draggedSquare = null;
                e.Handled = true;
            }
            else 
            {
                // Normal click (drag didn't start or we clicked an empty square)
                System.Windows.Point dropPos = e.GetPosition(BoardItemsControl);
                var element = BoardItemsControl.InputHitTest(dropPos) as System.Windows.FrameworkElement;
                ViewModels.SquareViewModel targetSquare = null;
                while (element != null)
                {
                    if (element.DataContext is ViewModels.SquareViewModel svm)
                    {
                        targetSquare = svm;
                        break;
                    }
                    element = System.Windows.Media.VisualTreeHelper.GetParent(element) as System.Windows.FrameworkElement;
                }

                if (targetSquare != null)
                {
                    if (this.DataContext is ViewModels.GameRoomViewModel vm)
                    {
                        vm.SquareClickCommand.Execute(targetSquare);
                    }
                }
                _draggedSquare = null;
                e.Handled = true;
            }
        }
    }
}
