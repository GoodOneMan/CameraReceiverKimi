using CameraReceiverKimi.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CameraReceiverKimi
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        /// <summary>
        /// Автоматически запускает сервер при загрузке окна.
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.StartServerCommand.ExecuteAsync(null);
        }

        /// <summary>
        /// Корректно освобождает ресурсы при закрытии.
        /// </summary>
        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.Dispose();
        }
    }
}