using System.Windows;
using System.Windows.Controls; // Для PasswordBox

namespace BybitWidget
{
    public partial class ApiSettingsWindow : Window
    {
        private readonly ApiKeysStorage _apiKeysStorage; // Экземпляр нашего хранилища ключей

        // Конструктор, который будет принимать экземпляр ApiKeysStorage
        public ApiSettingsWindow(ApiKeysStorage apiKeysStorage)
        {
            InitializeComponent();
            _apiKeysStorage = apiKeysStorage; // Инициализируем поле
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyTextBox.Text;
            string apiSecret = ApiSecretPasswordBox.Password; // Получаем текст из PasswordBox

            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
            {
                try
                {
                    // Создаем объект ApiKeys и сохраняем его через ApiKeysStorage
                    _apiKeysStorage.SaveApiKeys(new ApiKeys { ApiKey = apiKey, ApiSecret = apiSecret });
                    DialogResult = true; // Указываем, что данные были успешно сохранены
                    Close(); // Закрываем окно
                }
                catch (System.Exception ex)
                {
                    // Обработка ошибок при сохранении
                    System.Windows.MessageBox.Show($"Ошибка при сохранении ключей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Пожалуйста, введите оба ключа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Указываем, что пользователь отменил ввод
            Close(); // Закрываем окно
        }
    }
}