using System;
using System.IO;
using System.Security.Cryptography; // Для ProtectedData
using System.Text; // Для Encoding
using System.Text.Json; // Для сериализации/десериализации JSON

namespace CryptoPnLWidget
{
    // Класс для представления API-ключей
    public class ApiKeys
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
    }

    // Класс для сохранения и загрузки API-ключей
    public class ApiKeysStorage
    {
        // Имя файла, в котором будут храниться зашифрованные ключи
        private const string ApiKeysFileName = "bybit_api_keys.dat";

        // Энтропия для шифрования. Должна быть уникальной для вашего приложения.
        // Используется, чтобы предотвратить расшифровку данных, зашифрованных для другого приложения.
        private readonly byte[] _entropy = Encoding.UTF8.GetBytes("YourUniqueCryptoPnLWidgetEntropyStringHere");

        // Метод для получения полного пути к файлу с ключами
        private string GetApiKeysFilePath()
        {
            // Получаем путь к локальной папке данных приложения пользователя (например, C:\Users\YourUser\AppData\Local)
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Создаем подпапку для нашего приложения внутри AppData\Local
            string appSpecificFolder = Path.Combine(appDataFolder, "CryptoPnLWidget"); // "CryptoPnLWidget" - это имя вашей программы

            // Если папка не существует, создаем ее
            if (!Directory.Exists(appSpecificFolder))
            {
                Directory.CreateDirectory(appSpecificFolder);
            }

            // Возвращаем полный путь к файлу ключей
            return Path.Combine(appSpecificFolder, ApiKeysFileName);
        }

        /// <summary>
        /// Сохраняет API ключи, шифруя ApiSecret.
        /// </summary>
        /// <param name="keys">Объект ApiKeys, содержащий ключи для сохранения.</param>
        public void SaveApiKeys(ApiKeys keys)
        {
            if (keys == null || string.IsNullOrEmpty(keys.ApiKey) || string.IsNullOrEmpty(keys.ApiSecret))
            {
                throw new ArgumentException("API Key and Secret must be provided.");
            }

            try
            {
                // 1. Сериализуем объект ApiKeys в JSON-строку
                // System.Text.Json - это встроенный в .NET 8 высокопроизводительный сериализатор JSON
                string jsonKeys = JsonSerializer.Serialize(keys);

                // 2. Шифруем JSON-строку с использованием ProtectedData (DPAPI)
                // DataProtectionScope.CurrentUser означает, что данные может расшифровать только текущий пользователь
                // на текущем компьютере.
                byte[] encryptedData = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(jsonKeys), // Преобразуем JSON-строку в массив байтов
                    _entropy, // Дополнительная "соль" для шифрования
                    DataProtectionScope.CurrentUser); // Область защиты

                // 3. Сохраняем зашифрованные данные в файл
                File.WriteAllBytes(GetApiKeysFilePath(), encryptedData);
            }
            catch (Exception ex)
            {
                // В случае ошибки сохранения/шифрования выводим сообщение и перебрасываем исключение
                Console.WriteLine($"Ошибка при сохранении API ключей: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Загружает API ключи, расшифровывая ApiSecret.
        /// </summary>
        /// <returns>Объект ApiKeys. Если ключи не найдены или не удалось расшифровать, возвращает пустой объект ApiKeys.</returns>
        public ApiKeys LoadApiKeys()
        {
            string filePath = GetApiKeysFilePath();

            // Если файл с ключами не существует, возвращаем пустые ключи
            if (!File.Exists(filePath))
            {
                return new ApiKeys();
            }

            try
            {
                // 1. Читаем зашифрованные данные из файла
                byte[] encryptedData = File.ReadAllBytes(filePath);

                // 2. Расшифровываем данные
                byte[] decryptedData = ProtectedData.Unprotect(
                    encryptedData,
                    _entropy,
                    DataProtectionScope.CurrentUser);

                // 3. Десериализуем JSON-строку обратно в объект ApiKeys
                string jsonKeys = Encoding.UTF8.GetString(decryptedData);
                // Если десериализация по какой-то причине вернет null, создаем новый объект
                return JsonSerializer.Deserialize<ApiKeys>(jsonKeys) ?? new ApiKeys();
            }
            catch (Exception ex)
            {
                // В случае ошибки загрузки/расшифровки (например, файл поврежден, или был зашифрован на другом ПК)
                Console.WriteLine($"Ошибка при загрузке или расшифровке API ключей: {ex.Message}");
                // В таких случаях лучше удалить поврежденный файл, чтобы запросить ключи снова
                File.Delete(filePath);
                return new ApiKeys(); // Возвращаем пустые ключи
            }
        }

        /// <summary>
        /// Проверяет, установлены ли API ключи (т.е. сохранены и не пусты).
        /// </summary>
        public bool AreApiKeysSet()
        {
            var keys = LoadApiKeys();
            // Ключи считаются установленными, если оба поля (API Key и API Secret) не пусты
            return !string.IsNullOrEmpty(keys.ApiKey) && !string.IsNullOrEmpty(keys.ApiSecret);
        }
    }
}