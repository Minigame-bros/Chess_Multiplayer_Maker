using System.Windows.Controls;

namespace ChessGame.Views
{
    public partial class GameRoomView : UserControl
    {
        private System.Windows.Point _startPoint;
        private ViewModels.SquareViewModel _draggedSquare;
        private bool _isDragging = false;
        
        private ViewModels.SquareViewModel _rightDragStartSquare;
        private System.Windows.Shapes.Path _tempArrow;
        private bool _isRightDragging = false;

        public GameRoomView()
        {
            InitializeComponent();
        }

        private void BoardContainer_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ClearMarkings();
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
            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed && _rightDragStartSquare != null)
            {
                var hoveredSquare = GetSquareFromPosition(e.GetPosition(BoardItemsControl));
                if (hoveredSquare != null && hoveredSquare != _rightDragStartSquare)
                {
                    _isRightDragging = true;
                }

                if (_isRightDragging)
                {
                    if (_tempArrow != null)
                        MarkingsCanvas.Children.Remove(_tempArrow);
                        
                    System.Windows.Point startPos = GetSquareCenter(_rightDragStartSquare);
                    System.Windows.Point currentPos = e.GetPosition(MarkingsCanvas);
                    
                    if (hoveredSquare != null && hoveredSquare != _rightDragStartSquare)
                    {
                        currentPos = GetSquareCenter(hoveredSquare);
                    }
                    
                    _tempArrow = CreateArrow(startPos, currentPos, 0.8);
                    if (_tempArrow != null)
                        MarkingsCanvas.Children.Add(_tempArrow);
                }
            }
            else if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && _draggedSquare != null)
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

        private void ClearMarkings()
        {
            MarkingsCanvas.Children.Clear();
            if (this.DataContext is ViewModels.GameRoomViewModel vm)
            {
                foreach (var square in vm.Squares)
                {
                    square.IsRightClicked = false;
                }
            }
        }

        private ViewModels.SquareViewModel GetSquareFromPosition(System.Windows.Point pos)
        {
            var element = BoardItemsControl.InputHitTest(pos) as System.Windows.FrameworkElement;
            while (element != null)
            {
                if (element.DataContext is ViewModels.SquareViewModel svm)
                    return svm;
                element = System.Windows.Media.VisualTreeHelper.GetParent(element) as System.Windows.FrameworkElement;
            }
            return null;
        }

        private System.Windows.Point GetSquareCenter(ViewModels.SquareViewModel square)
        {
            var container = BoardItemsControl.ItemContainerGenerator.ContainerFromItem(square) as System.Windows.FrameworkElement;
            if (container != null)
            {
                System.Windows.Point center = new System.Windows.Point(container.ActualWidth / 2, container.ActualHeight / 2);
                return container.TranslatePoint(center, MarkingsCanvas);
            }
            return new System.Windows.Point(0, 0);
        }

        private System.Windows.Shapes.Path CreateArrow(System.Windows.Point start, System.Windows.Point end, double opacity = 0.8)
        {
            var geometryGroup = new System.Windows.Media.GeometryGroup();
            
            System.Windows.Vector v = end - start;
            double length = v.Length;
            if (length < 10) return null;
            
            v.Normalize();
            
            double arrowHeadLength = 25;
            System.Windows.Point lineEnd = end - v * (arrowHeadLength * 0.8);
            
            var line = new System.Windows.Media.LineGeometry(start, lineEnd);
            geometryGroup.Children.Add(line);
            
            double arrowHeadAngle = System.Math.PI / 6;
            double angle = System.Math.Atan2(v.Y, v.X);
            
            System.Windows.Point p1 = new System.Windows.Point(
                end.X - arrowHeadLength * System.Math.Cos(angle - arrowHeadAngle),
                end.Y - arrowHeadLength * System.Math.Sin(angle - arrowHeadAngle));
                
            System.Windows.Point p2 = new System.Windows.Point(
                end.X - arrowHeadLength * System.Math.Cos(angle + arrowHeadAngle),
                end.Y - arrowHeadLength * System.Math.Sin(angle + arrowHeadAngle));
                
            var pathFigure = new System.Windows.Media.PathFigure();
            pathFigure.StartPoint = end;
            pathFigure.Segments.Add(new System.Windows.Media.LineSegment(p1, true));
            pathFigure.Segments.Add(new System.Windows.Media.LineSegment(p2, true));
            pathFigure.IsClosed = true;
            
            var pathGeometry = new System.Windows.Media.PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            geometryGroup.Children.Add(pathGeometry);
            
            var path = new System.Windows.Shapes.Path
            {
                Data = geometryGroup,
                Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(255 * opacity), 235, 151, 38)),
                StrokeThickness = 14,
                StrokeEndLineCap = System.Windows.Media.PenLineCap.Round,
                StrokeStartLineCap = System.Windows.Media.PenLineCap.Round,
                StrokeLineJoin = System.Windows.Media.PenLineJoin.Round,
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(255 * opacity), 235, 151, 38)),
                IsHitTestVisible = false
            };
            
            return path;
        }

        private void BoardContainer_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var square = GetSquareFromPosition(e.GetPosition(BoardItemsControl));
            if (square != null)
            {
                _rightDragStartSquare = square;
                _isRightDragging = false;
                BoardContainer.CaptureMouse();
                e.Handled = true;
            }
        }

        private void BoardContainer_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_rightDragStartSquare != null)
            {
                BoardContainer.ReleaseMouseCapture();
                
                if (_tempArrow != null)
                {
                    MarkingsCanvas.Children.Remove(_tempArrow);
                    _tempArrow = null;
                }
                
                var dropSquare = GetSquareFromPosition(e.GetPosition(BoardItemsControl));
                if (dropSquare != null)
                {
                    if (dropSquare == _rightDragStartSquare)
                    {
                        dropSquare.IsRightClicked = !dropSquare.IsRightClicked;
                    }
                    else if (_isRightDragging && dropSquare != _rightDragStartSquare)
                    {
                        string tag = $"{_rightDragStartSquare.Position.Row},{_rightDragStartSquare.Position.Col}-{dropSquare.Position.Row},{dropSquare.Position.Col}";
                        System.Windows.Shapes.Path existing = null;
                        foreach (System.Windows.UIElement child in MarkingsCanvas.Children)
                        {
                            if (child is System.Windows.Shapes.Path p && p.Tag as string == tag)
                            {
                                existing = p;
                                break;
                            }
                        }
                        
                        if (existing != null)
                        {
                            MarkingsCanvas.Children.Remove(existing);
                        }
                        else
                        {
                            var arrow = CreateArrow(GetSquareCenter(_rightDragStartSquare), GetSquareCenter(dropSquare), 0.8);
                            if (arrow != null)
                            {
                                arrow.Tag = tag;
                                MarkingsCanvas.Children.Add(arrow);
                            }
                        }
                    }
                }
                
                _rightDragStartSquare = null;
                _isRightDragging = false;
                e.Handled = true;
            }
        }
    }
}
