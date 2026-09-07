using CameraReceiverKimi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CameraReceiverKimi.Services
{
    /// <summary>
    /// TCP-сервер, принимающий бинарные данные от Android-клиента.
    /// Протокол: [Int32 commentLength][comment UTF8][Int32 imageLength][image bytes]
    /// </summary>
    public class SocketServerService
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _listenTask;

        /// <summary>Событие: получено новое изображение</summary>
        public event EventHandler<ReceivedImageModel>? ImageReceived;

        /// <summary>Событие: произошла ошибка</summary>
        public event EventHandler<string>? ErrorOccurred;

        public async Task StartAsync(int port)
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            // Запускаем фоновую задачу прослушивания
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
            await Task.CompletedTask;  // Возвращаем управление сразу
        }

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Ожидаем подключения (неблокирующий с токеном отмены)
                    var clientTask = _listener!.AcceptTcpClientAsync();
                    var client = await clientTask.WaitAsync(ct);

                    // Обрабатываем клиента в отдельной задаче
                    _ = Task.Run(() => HandleClientAsync(client), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"Ошибка прослушивания: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
            var senderIp = endpoint?.Address.ToString() ?? "unknown";
            var senderPort = endpoint?.Port ?? 0;

            try
            {
                await using var stream = client.GetStream();

                // Читаем длину комментария (4 байта, Big-Endian)
                var commentLengthBytes = new byte[4];
                await ReadExactlyAsync(stream, commentLengthBytes);
                var commentLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(commentLengthBytes));

                // Читаем комментарий
                var commentBytes = new byte[commentLength];
                await ReadExactlyAsync(stream, commentBytes);
                var comment = System.Text.Encoding.UTF8.GetString(commentBytes);

                // Читаем длину изображения
                var imageLengthBytes = new byte[4];
                await ReadExactlyAsync(stream, imageLengthBytes);
                var imageLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(imageLengthBytes));

                // Читаем изображение
                var imageBytes = new byte[imageLength];
                await ReadExactlyAsync(stream, imageBytes);

                // Уведомляем подписчиков
                ImageReceived?.Invoke(this, new ReceivedImageModel
                {
                    Comment = comment,
                    ImageBytes = imageBytes,
                    SenderIp = senderIp,
                    SenderPort = senderPort
                });
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка обработки клиента {senderIp}: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        /// <summary>
        /// Гарантированно читает ровно count байт из потока.
        /// </summary>
        private static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(totalRead));
                if (read == 0) throw new IOException("Соединение разорвано при чтении данных");
                totalRead += read;
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listenTask?.Wait(TimeSpan.FromSeconds(2));
        }
    }
}
