using CameraReceiverKimi.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using CameraReceiverKimi.Services;

namespace CameraReceiverKimi.ViewModels
{
    /// <summary>
    /// Главная ViewModel. Связывает UI с TCP-сервером через события.
    /// Наследуем от ObservableObject (CommunityToolkit.Mvvm) — автоматическая генерация INPC.
    /// </summary>
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly SocketServerService _socketServer;
        private readonly WebApiService _webApi;

        [ObservableProperty]
        private BitmapImage? _currentImage;

        [ObservableProperty]
        private string _commentText = "Ожидание подключения...";

        [ObservableProperty]
        private string _connectionInfo = "Не подключено";

        [ObservableProperty]
        private bool _isServerRunning;

        public MainViewModel()
        {
            // Инициализируем сервисы
            _socketServer = new SocketServerService();
            _webApi = new WebApiService();

            // Подписываемся на событие получения изображения
            _socketServer.ImageReceived += OnImageReceived;
            _socketServer.ErrorOccurred += OnErrorOccurred;
        }

        /// <summary>
        /// Запускает TCP-сервер и Web API при старте приложения.
        /// </summary>
        [RelayCommand]
        private async Task StartServerAsync()
        {
            try
            {
                // Запускаем TCP Socket сервер (порт 5000)
                await _socketServer.StartAsync(5000);

                // Запускаем ASP.NET Core Minimal API (порт 5001) для служебных нужд
                await _webApi.StartAsync(5001);

                IsServerRunning = true;
                ConnectionInfo = $"TCP: 0.0.0.0:5000 | API: 0.0.0.0:5001";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска сервера: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик события получения нового изображения.
        /// Выполняется в потоке сокета — обязательно используем Dispatcher для UI.
        /// </summary>
        private void OnImageReceived(object? sender, ReceivedImageModel data)
        {
            // Dispatcher.Invoke — маршаллинг в главный поток WPF
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Создаём BitmapImage из потока байтов
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new System.IO.MemoryStream(data.ImageBytes);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;  // Загружаем сразу, поток можно закрыть
                    bitmap.EndInit();
                    bitmap.Freeze();  // Делаем потокобезопасным для привязки

                    CurrentImage = bitmap;
                    CommentText = string.IsNullOrWhiteSpace(data.Comment)
                        ? "Без комментария"
                        : data.Comment;
                    ConnectionInfo = $"Последний отправитель: {data.SenderIp}:{data.SenderPort}";
                }
                catch (Exception ex)
                {
                    CommentText = $"Ошибка отображения: {ex.Message}";
                }
            });
        }

        private void OnErrorOccurred(object? sender, string error)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CommentText = $"Ошибка: {error}";
            });
        }

        public void Dispose()
        {
            _socketServer.ImageReceived -= OnImageReceived;
            _socketServer.ErrorOccurred -= OnErrorOccurred;
            _socketServer.Stop();
            _webApi.Stop();
        }
    }
}
