using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DataUploader
{
    /// <summary>
    /// Содержит методы, которые используются в зависимости от расширения файла, подгруженного
    /// пользователем.
    /// </summary>
    internal class FileExtension
    {
        public static string DetermineExtension(string filePath)
        {
            int dotPosition = filePath.LastIndexOf(".");
            return filePath.Substring(dotPosition);
        }

        /// <summary>
        /// В зависимости от от расширения файла "закрашивает" тот или иной RadioButton
        /// </summary>
        public static void SetRadioButtonState(string extension, MainWindow mW)
        {
            switch (extension)
            {
                case ".zip":
                    mW.rbArchiveZip.IsChecked = true;
                    break;
                case ".7z":
                    mW.rbArchive7z.IsChecked = true;
                    break;
                case ".dtl":
                    mW.rbDtl.IsChecked = true;
                    break;
                case ".xls":
                    mW.rbXls.IsChecked = true;
                    break;
                case ".xlsx":
                    mW.rbXlsx.IsChecked = true;
                    break;
                default:
                    mW.rbArchiveZip.IsChecked = false;
                    mW.rbArchive7z.IsChecked = false;
                    mW.rbDtl.IsChecked = false;
                    mW.rbXlsx.IsChecked = false;
                    mW.rbXls.IsChecked = false;

                    // Когда пользователь выбирает файл, который программа не может определить...
                    // ...необходимо элементы интерфейса отключить
                    FileExtension.DisableUiElements(mW, "default", false);

                    break;
            }
        }

        /// <summary>
        /// В зависимости от расширения файла отключает элементы интерфейса
        /// </summary>
        public static void DisableUiElements(MainWindow mW, string extension, bool isTrue)
        {
            // Сначала всё включим
            EnableAllUiElements();

            // Затем нужное отключим
            Control[] uiGroupToBeDisabled = ReturnUiGroupToBeDisabled();
            for (int i = 0; i < uiGroupToBeDisabled.Length; i++)
            {
                uiGroupToBeDisabled[i].IsEnabled = isTrue;
            }

            /// <summary>
            /// В зависимости от расширения файла возвращает группу элементов интерфейса, которую
            /// надо сделать неактивной
            /// </summary>
            Control[] ReturnUiGroupToBeDisabled()
            {
                switch (extension)
                {
                    case ".zip":
                        // Group 1
                        return new Control[1] { mW.btnTest };
                    case ".7z":
                        // Group 2
                        return new Control[1] { mW.btnTest };
                    case ".dtl":
                        // Group 3
                        return new Control[3] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnExtract};
                    case ".xls":
                        // Group 4
                        return new Control[3] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnExtract};
                    case ".xlsx":
                        // Group 5
                        return new Control[3] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnExtract};
                    default:
                        // Group 6
                        return new Control[3] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnExtract};
                }
            }

            /// <summary>
            /// Возвращает группу элементов интерфейса, которую надо сделать активной
            /// (сначала включить все UI элементы, а потом уже нужные отключить)
            /// </summary>
            void EnableAllUiElements()
            {
                // Group 7
                Control[] uiGroupToBeEnabled = new Control[] {mW.tbDestinationPath,
                                                              mW.btnBrowseDestination,
                                                              mW.btnExtract};

                for (int i = 0; i < uiGroupToBeEnabled.Length; i++)
                {
                    uiGroupToBeEnabled[i].IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Функция, которая вызывается, если пользователь подгрузил ZIP-архив.
        /// </summary>
        /// <param name="filePath">Путь к архиву.</param>
        /// <param name="Destination">Путь к директории, в котороую необходимо извлесь архив</param>
        /// <returns>Возвращает false при успешном завершении, true при появлении ошибок.</returns>
        public static bool ExtractArchiveZip(string filePath, string Destination)
        {
            try
            {
                int encodingCode = System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage;
                System.IO.Compression.ZipFile.ExtractToDirectory(filePath, Destination, Encoding.GetEncoding(encodingCode));
                // Распаковка выполнена успешно
                return false;
            }
            catch (ArgumentException)
            {
                string messageBoxText = "Файл не выбран.";
                string caption = "Ошибка выбора файла";

                System.Windows.MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                // Распаковка не выполнена
                return true;
            }
            catch (System.IO.InvalidDataException)
            {
                string messageBoxText = "Выбранный файл не является допустимым ZIP-архивом.";
                string caption = "Ошибка типа файла";

                System.Windows.MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                return true;
            }
            catch (System.IO.IOException)
            {
                string messageBoxText = "Файл с таким именем уже был извлечен или существует.";
                string caption = "Ошибка извлечения файла";

                System.Windows.MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                return true;
            }
            catch (Exception ex)
            {
                string messageBoxText = "Ошибка: " + ex.Message;
                string caption = "Ошибка";

                System.Windows.MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                return true;
            }
        }

        /// <summary>
        /// Функция, которая вызывается, если пользователь подгрузил 7z-архив.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="Destination"></param>
        public void ExtractArchive7z(string filePath, string Destination)
        {
        //ToDo
        }
    }
}
