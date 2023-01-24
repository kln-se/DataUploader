using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DataUploader
{
    internal static class AppConfig
    {
        
        private static string s_configPath;
        private static Configuration s_config;

        private static string s_login;
        private static string s_password;
        private static string s_host;
        private static string s_port;
        private static string s_databaseName;

        /// <summary>
        /// Получение App.config
        /// </summary>
        private static void GetConfig()
        {
            try
            {
                s_configPath = Assembly.GetExecutingAssembly().Location;
                s_config = ConfigurationManager.OpenExeConfiguration(s_configPath);
            }
            catch (Exception ex)
            {
                string messageBoxText = ex.Message;
                string caption = "Ошибка получения *.config файла";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
            }
        }

        /// <summary>
        /// Получение настроек подключения к базе данных из App.config
        /// </summary>
        public static string[] GetSettings()
        {
            GetConfig();

            // Чтение из App.config
            s_login = s_config.AppSettings.Settings["host"].Value;
            s_password = s_config.AppSettings.Settings["port"].Value;
            s_host = s_config.AppSettings.Settings["databaseName"].Value;
            s_port = s_config.AppSettings.Settings["login"].Value;
            s_databaseName = s_config.AppSettings.Settings["password"].Value;

            return new string[] { s_login, s_password, s_host, s_port, s_databaseName };
        }

        /// <summary>
        /// Сохранение настроек подключения к базе данных в App.config
        /// </summary>
        public static void SetSettings(string host,
                                       string port,
                                       string databaseName,
                                       string login,
                                       string password)

        {
            GetConfig();

            s_login = host;
            s_password = port;
            s_host = databaseName;
            s_port = login;
            s_databaseName = password;

            // Запись в App.config
            s_config.AppSettings.Settings["host"].Value = host;
            s_config.AppSettings.Settings["port"].Value = port;
            s_config.AppSettings.Settings["databaseName"].Value = databaseName;
            s_config.AppSettings.Settings["login"].Value = login;
            s_config.AppSettings.Settings["password"].Value = password;

            s_config.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
