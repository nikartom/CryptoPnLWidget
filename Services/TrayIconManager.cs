using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace CryptoPnLWidget.Services
{
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon? _trayIcon;
        private readonly Window _mainWindow;
        private readonly CryptoPnLWidget.Services.ThemeManager _themeManager;

        public TrayIconManager(Window mainWindow, CryptoPnLWidget.Services.ThemeManager themeManager)
        {
            _mainWindow = mainWindow;
            _themeManager = themeManager;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon();

            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("CryptoPnLWidget.vdhyq-ubma4-001.ico"))
                {
                    if (stream != null)
                        _trayIcon.Icon = new Icon(stream);
                    else
                        throw new Exception("Не удалось найти ресурс иконки в сборке");
                }
            }
            catch (Exception ex)
            {
                _trayIcon.Icon = SystemIcons.Application;
                CryptoPnLWidget.Services.UIManager.RaiseGlobalError($"Ошибка при загрузке иконки: {ex.Message}. Используется стандартная иконка.");
            }

            _trayIcon.Text = "Crypto PnL Widget";
            _trayIcon.Visible = true;

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            var settingsItem = new ToolStripMenuItem("Настройки");
            settingsItem.Click += (s, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var settingsWindow = new SettingsWindow(_themeManager);
                    settingsWindow.Owner = _mainWindow;
                    settingsWindow.ShowDialog();
                });
            };
            contextMenu.Items.Add(settingsItem);

            var exitItem = new ToolStripMenuItem("Выход");
            exitItem.Click += (s, e) =>
            {
                _trayIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            };
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;

            // Handle single click on tray icon
            _trayIcon.Click += (s, e) =>
            {
                // Проверяем, что это левый клик, чтобы избежать конфликтов с контекстным меню
                if (((MouseEventArgs)e).Button == MouseButtons.Left)
                {
                    if (_mainWindow.Visibility == Visibility.Hidden)
                    {
                        _mainWindow.Show();
                        _mainWindow.WindowState = WindowState.Normal;
                        _mainWindow.Activate();
                    }
                    else
                    {
                        _mainWindow.Hide();
                    }
                }
            };
        }

        public void Hide()
        {
            _trayIcon?.Dispose();
        }

        public void Dispose()
        {
            _trayIcon?.Dispose();
        }
    }
} 