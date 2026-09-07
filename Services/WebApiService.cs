using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace CameraReceiverKimi.Services
{
    /// <summary>
    /// Локальный HTTP-сервер на Kestrel.
    /// Предоставляет endpoint /api/status для проверки работоспособности.
    /// </summary>
public class WebApiService
    {
        private WebApplication? _app;
        private Task? _runTask;

        public async Task StartAsync(int port)
        {
            var builder = WebApplication.CreateBuilder();

            // Настраиваем Kestrel на указанный порт
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(port);
            });

            // Отключаем логирование запросов в консоль (чтобы не мешало WPF)
            //builder.Logging.ClearProviders();

            _app = builder.Build();

            // Minimal API endpoint — статус сервера
            _app.MapGet("/api/status", () => Results.Ok(new
            {
                status = "running",
                timestamp = DateTime.Now,
                tcpPort = 5000
            }));

            // Запускаем в фоне
            _runTask = _app.RunAsync();
            await Task.CompletedTask;
        }

        public void Stop()
        {
            _app?.StopAsync().Wait(TimeSpan.FromSeconds(5));
        }
    }
}