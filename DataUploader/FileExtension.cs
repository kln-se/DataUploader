using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DataUploader
{
    /// <summary>
    /// Содержит методы, которые используются в зависимости от расширения файла, подгруженного
    /// пользователем.
    /// </summary>
    internal class FileExtension
    {

        // Функции разархивации и конвертирования стартуют процесс, который будет лежать здесь...
        // ...чтобы его можно было "убить"
        private Process p = null;

        /// <summary>
        /// В зависимости от расширения файла "закрашивает" тот или иной RadioButton
        /// </summary>
        internal static void SetRadioButtonState(string fileExtension, MainWindow mW)
        {
            switch (fileExtension)
            {
                case ".zip":
                    mW.rbArchiveZip.IsChecked = true;
                    mW.imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/unpack_96.png"));
                    mW.btnStartProcess.ToolTip = "Извлечь содержимое архива";
                    break;
                case ".7z":
                    mW.rbArchive7z.IsChecked = true;
                    mW.imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/unpack_96.png"));
                    mW.btnStartProcess.ToolTip = "Извлечь содержимое архива";
                    break;
                case ".dtl":
                    mW.rbDtl.IsChecked = true;
                    mW.imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/convert_96.png"));
                    mW.btnStartProcess.ToolTip = "Конверитровать выбранный файл";
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
        internal static void DisableUiElements(MainWindow mW, string fileExtension, bool EnableUiElements)
        {
            // Сначала всё включим
            EnableAllUiElements();

            // Затем нужное отключим
            Control[] uiGroupToBeDisabled = ReturnUiGroupToBeDisabled();
            for (int i = 0; i < uiGroupToBeDisabled.Length; i++)
            {
                uiGroupToBeDisabled[i].IsEnabled = EnableUiElements;
            }

            /// <summary>
            /// В зависимости от расширения файла возвращает группу элементов интерфейса, которую
            /// надо сделать неактивной
            /// </summary>
            Control[] ReturnUiGroupToBeDisabled()
            {
                switch (fileExtension)
                {
                    case ".zip":
                        mW.imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/unpack_96.png"));
                        mW.btnStartProcess.ToolTip = "Извлечь содержимое архива";
                        // Group 1
                        return new Control[5] {mW.rbXlsxFileFormat,
                                               mW.rbXlsFileFormat,
                                               mW.rbCsvFileFormat,
                                               mW.cmbCsvEncoding,
                                               mW.chbCsvShowMilisec};
                    case ".7z":
                        mW.imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/unpack_96.png"));
                        mW.btnStartProcess.ToolTip = "Извлечь содержимое архива";
                        // Group 2
                        return new Control[5] {mW.rbXlsxFileFormat,
                                               mW.rbXlsFileFormat,
                                               mW.rbCsvFileFormat,
                                               mW.cmbCsvEncoding,
                                               mW.chbCsvShowMilisec};
                    case ".dtl":
                        mW.imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/convert_96.png"));
                        mW.btnStartProcess.ToolTip = "Конверитровать выбранный файл";
                        // Group 3
                        return new Control[0] { };
                    case ".xls":
                        // Group 4
                        return new Control[8] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnStartProcess,
                                               mW.rbXlsxFileFormat,
                                               mW.rbXlsFileFormat,
                                               mW.rbCsvFileFormat,
                                               mW.cmbCsvEncoding,
                                               mW.chbCsvShowMilisec};

                    case ".xlsx":
                        // Group 5
                        return new Control[8] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnStartProcess,
                                               mW.rbXlsxFileFormat,
                                               mW.rbXlsFileFormat,
                                               mW.rbCsvFileFormat,
                                               mW.cmbCsvEncoding,
                                               mW.chbCsvShowMilisec};
                    default:
                        mW.imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/unpack_96.png"));
                        mW.btnStartProcess.ToolTip = "Извлечь содержимое архива";
                        // Group 6
                        return new Control[8] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnStartProcess,
                                               mW.rbXlsxFileFormat,
                                               mW.rbXlsFileFormat,
                                               mW.rbCsvFileFormat,
                                               mW.cmbCsvEncoding,
                                               mW.chbCsvShowMilisec};
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
                                                              mW.btnStartProcess,
                                                              mW.rbXlsxFileFormat,
                                                              mW.rbXlsFileFormat,
                                                              mW.rbCsvFileFormat,
                                                              mW.cmbCsvEncoding,
                                                              mW.chbCsvShowMilisec};

                for (int i = 0; i < uiGroupToBeEnabled.Length; i++)
                {
                    uiGroupToBeEnabled[i].IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// В зависимости от того, что выбрано (File или Folder) в ComboBox cmbFileOrFolder
        /// отключает элементы интерфейса
        /// </summary>
        internal static void DisableUiElementsFF(MainWindow mW, string comboBoxSelection)
        {
            // Сначала всё включим
            EnableAllUiElements();

            // Затем нужное отключим
            Control[] uiGroupToBeDisabled = ReturnUiGroupToBeDisabled();
            for (int i = 0; i < uiGroupToBeDisabled.Length; i++)
            {
                uiGroupToBeDisabled[i].IsEnabled = false;
            }

            /// <summary>
            /// В зависимости от расширения файла возвращает группу элементов интерфейса, которую
            /// надо сделать неактивной
            /// </summary>
            Control[] ReturnUiGroupToBeDisabled()
            {
                switch (comboBoxSelection)
                {
                    case "Файл (для обработки единичного файла)":
                        mW.tbFileOrFolderPath.Text = "Файл не выбран";
                        mW.filePath = "";
                        mW.btnImportToDB.ToolTip = "Загрузить содержимое выбранного файла в базу данных.";
                        
                        DisableUiElements(mW, "default", false);
                        
                        // Group 1
                        return new Control[0] { };
                    
                    case "Директория (для пакетной обработки файлов в директории)":
                        mW.tbFileOrFolderPath.Text = "Директория не выбрана";
                        mW.filePath = "";
                        mW.btnImportToDB.ToolTip = "Загрузить содержимое файлов из выбранной директории в базу данных.";

                        mW.imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/convert_96.png"));
                        mW.btnStartProcess.ToolTip = "Начать пакетное конвертирование *.dtl файлов.";

                        mW.btnStartProcess.IsEnabled = true;
                        mW.btnBrowseDestination.IsEnabled = true;
                        mW.tbDestinationPath.IsEnabled = true;

                        mW.rbArchiveZip.IsChecked = false;
                        mW.rbArchive7z.IsChecked = false;
                        mW.rbDtl.IsChecked = false;
                        mW.rbXlsx.IsChecked = false;
                        mW.rbXls.IsChecked = false;

                        mW.rbXlsxFileFormat.IsEnabled = true;
                        mW.rbXlsFileFormat.IsEnabled = true;
                        mW.rbCsvFileFormat.IsEnabled = true;
                        mW.cmbCsvEncoding.IsEnabled = true;
                        mW.chbCsvShowMilisec.IsEnabled = true;

                        // Group 2
                        return new Control[5] {mW.rbArchiveZip,
                                               mW.rbArchive7z,
                                               mW.rbDtl,
                                               mW.rbXlsx,
                                               mW.rbXls};
                    default:
                        // Group 6
                        return new Control[0] { };
                }
            }

            /// <summary>
            /// Возвращает группу элементов интерфейса, которую надо сделать активной
            /// (сначала включить все UI элементы, а потом уже нужные отключить)
            /// </summary>
            void EnableAllUiElements()
            {
                // Group 7
                Control[] uiGroupToBeEnabled = new Control[] {mW.rbArchiveZip,
                                                              mW.rbArchive7z,
                                                              mW.rbDtl,
                                                              mW.rbXlsx,
                                                              mW.rbXls};

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
        /// <param name="destinationPath">Путь к директории, в котороую необходимо извлесь архив</param>
        /// <returns>Возвращает false при успешном завершении, true при появлении ошибок.</returns>
        internal static bool ExtractArchiveZip(string filePath, string destinationPath)
        {
            try
            {
                int encodingCode = System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage;
                System.IO.Compression.ZipFile.ExtractToDirectory(filePath, destinationPath, Encoding.GetEncoding(encodingCode));
                // Распаковка выполнена успешно
                return false;
            }
            catch (ArgumentException)
            {
                string messageBoxText = "Файл не выбран или неверно указана директория.";
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
        /// <param name="destinationPath"></param>
        internal bool ExtractArchive7z(string filePath, string destinationPath)
        {

            string filePathBackSlash = filePath.Replace("/", "\\");
            string destinationPathBackSlash = destinationPath.Replace("/", "\\");

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    // Необходимые файлы (7za.exe и т.д.) добавлены в ресурсы проекта (/Properties)
                    FileName = Directory.GetCurrentDirectory() + "\\Properties\\7za.exe",
                    Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", filePathBackSlash, destinationPathBackSlash)
                };

                this.p = Process.Start(psi);
                p.WaitForExit();
                // Распаковка выполнена успешно
                return false;
            }
            catch (Exception ex)
            {
                string messageBoxText = "Ошибка: " + ex.Message;
                string caption = "Ошибка";

                System.Windows.MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                // Распаковка не выполнена
                return true;
            }
        }
        internal bool ConvertDtl(string filePath,
                                        string destinationPath,
                                        string selectedCsvEncoding,
                                        string choosenFileFormat,
                                        string showMilisec)
        {

            string filePathBackSlash = filePath.Replace("/", "\\");
            string destinationPathBackSlash = (destinationPath +
                                               "/" +
                                               System.IO.Path.GetFileNameWithoutExtension(filePath) +
                                               choosenFileFormat
                                               ).Replace("/", "\\");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                // Необходимые файлы (EasyConverter.exe.exe и т.д.) добавлены в ресурсы проекта (/Properties)
                psi.FileName = Directory.GetCurrentDirectory() + "\\Properties\\EasyConverter.exe";

                if (choosenFileFormat == ".csv")
                {
                    switch (selectedCsvEncoding)
                    {
                        case "ASCII":
                            psi.Arguments = string.Format("/ca {0} {1} {2}", showMilisec, filePathBackSlash, destinationPathBackSlash);
                            break;
                        case "UTF-8":
                            psi.Arguments = string.Format("/c8 {0} {1} {2}", showMilisec, filePathBackSlash, destinationPathBackSlash);
                            break;
                        case "Unicode":
                            psi.Arguments = string.Format("/cu {0} {1} {2}", showMilisec, filePathBackSlash, destinationPathBackSlash);
                            break;
                    }
                }
                else
                {
                    psi.Arguments = string.Format("{0} {1} {2}", showMilisec, filePathBackSlash, destinationPathBackSlash);
                }

                // Старт процесса разархивирования или конвертирования
                this.p = Process.Start(psi);
                p.WaitForExit();
                // Конвертирование выполнено успешно
                return false;
            }
            catch (Exception ex)
            {
                string messageBoxText = "Ошибка: " + ex.Message;
                string caption = "Ошибка";

                System.Windows.MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                // Конвертирование не выполнено
                return true;
            }
        }

        /// <summary>
        /// Функция для остановки приватного процесса
        /// </summary>
        internal void KillProcess()
        {
            if (this.p != null)
            {
                this.p.Kill();
                this.p = null;
            }
        }
    }
}