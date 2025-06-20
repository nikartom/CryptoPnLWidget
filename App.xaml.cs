using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System; // Для IServiceProvider
using System.Windows; // Для Application, StartupEventArgs, ExitEventArgs
using Bybit.Net.Clients;
using CryptoPnLWidget.API;
using CryptoPnLWidget.Services;
using CryptoPnLWidget.Services.Bybit;

namespace CryptoPnLWidget
{
    // Наследуемся от Application (как обычно)
    public partial class App : System.Windows.Application
    {
        // Объявляем хост, который будет управлять жизненным циклом и DI
        private readonly IHost _host;
        public static IServiceProvider? Services { get; private set; }

        // Конструктор App
        public App()
        {
            // Создаем хост. CreateDefaultBuilder() предоставляет базовую конфигурацию.
            _host = Host.CreateDefaultBuilder()
                // ConfigureServices - это то место, где мы регистрируем все наши классы,
                // которые могут быть инжектированы (переданы через конструкторы).
                .ConfigureServices((context, services) =>
                {
                    // Регистрируем ExchangeKeysManager как синглтон.
                    // Это означает, что будет создан только один экземпляр ExchangeKeysManager
                    // и он будет использоваться везде, где запрошен.
                    services.AddSingleton<CryptoPnLWidget.Services.ExchangeKeysManager>();

                    // Регистрируем ApiSettingsWindow как транзитный (transient).
                    // Это означает, что новый экземпляр ApiSettingsWindow будет создаваться
                    // каждый раз, когда он будет запрошен из DI-контейнера.
                    services.AddTransient<CryptoPnLWidget.API.ApiSettingsWindow>();

                    services.AddSingleton<CryptoPnLWidget.Services.PositionManager>();

                    // Регистрируем BybitRestClient как синглтон
                    services.AddSingleton<BybitRestClient>();

                    // Регистрируем BybitService как синглтон
                    services.AddSingleton<CryptoPnLWidget.Services.Bybit.BybitService>(provider => 
                        new CryptoPnLWidget.Services.Bybit.BybitService(provider.GetRequiredService<BybitRestClient>()));

                    // Регистрируем главное окно (MainWindow) как синглтон.
                    // Это означает, что MainWindow будет создан один раз.
                    services.AddSingleton<MainWindow>();
                })
                .Build(); // Строим хост
        }

        // Этот метод вызывается при запуске приложения
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Запускаем хост. Это инициализирует все зарегистрированные сервисы.
            await _host.StartAsync();

            // Инициализируем статическое свойство Services
            Services = _host.Services;

            // Теперь, когда хост запущен, мы можем получить MainWindow из его сервис-провайдера.
            // GetRequiredService<T>() запрашивает экземпляр T из DI-контейнера.
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();

            // Отображаем главное окно
            mainWindow.Show();

            // Вызываем базовую реализацию OnStartup
            base.OnStartup(e);
        }

        // Этот метод вызывается при завершении работы приложения
        protected override async void OnExit(ExitEventArgs e)
        {
            // Перед остановкой хоста, сохраняем историю PnL
            // Получаем PositionManager из сервисов и вызываем SaveHistory
            var positionManager = _host.Services.GetService<CryptoPnLWidget.Services.PositionManager>();
            if (positionManager != null)
            {
                positionManager.SaveHistory(); // <--- ДОБАВЛЕНО
            }

            using (_host)
            {
                await _host.StopAsync();
            }
            base.OnExit(e);
        }

        // Добавляем статическое свойство для доступа к сервис-провайдеру из любого места.
        // Это "Service Locator" паттерн, который иногда используется в WPF для упрощения доступа
        // к сервисам, когда DI не может быть использован напрямую (например, для создания окон).
        // В идеале старайтесь передавать зависимости через конструктор, но для окон это часто удобно.

    }
}