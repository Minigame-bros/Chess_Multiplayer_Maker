using System;
using System.Windows;

namespace ChessGame
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show($"Lỗi không xử lý được:\n\n{e.Exception.GetType().Name}: {e.Exception.Message}\n\nStack trace:\n{e.Exception.StackTrace}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show($"Lỗi nghiêm trọng:\n\n{ex?.GetType().Name}: {ex?.Message}\n\nStack trace:\n{ex?.StackTrace}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }
    }
}
