using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media.TextFormatting;
using System.IO;
using System.Windows.Threading;
using Path = System.IO.Path;
using System.Threading;

namespace DataUploader
{
    /// <summary>
    /// Логика взаимодействия для WaitingBox.xaml
    /// </summary>
    public partial class WaitingBox : Window
    {

        // Для выполнения операции извлечения архива, конвертирования файла в отдельном потоке
        private readonly BackgroundWorker _bgw = new BackgroundWorker();

        // Для возможности обратиться к процессу ивлечения/конвертирования, запущенному внутри...
        //...данного экземпляра
        private readonly FileExtension _fe = new FileExtension();

        private string _filePath;
        private string _destinationPath;
        private string _selectedCsvEncoding;
        private string _choosenOutputFileFormat;
        private string _showMilisec;
        private int? _fileTypeId;
        private int? _transformerId;
        private string _phase;

        // В каком режиме запускается это окно:
        // * по умолчанию null - при конвертации/разархивации файлов
        // * "import" - при импорте содержимого файла в базу данных
        private string _mode;

        private bool _isCompleted = false;
        private bool _importIsCompleted = false;

        //private delegate bool ThreadStart();

        public bool ImportIsCompleted
        {
            get { return _importIsCompleted; }
        }

        public WaitingBox(string filePath,
                          string destinationPath = null,
                          string selectedCsvEncoding = null,
                          string choosenOutputFileFormat = null,
                          string showMilisec = null,
                          int? fileTypeId = null,
                          int? transformerId = null,
                          string phase = null,
                          string mode = null)
        {
            InitializeComponent();

            _filePath = filePath;
            _destinationPath = destinationPath;
            _selectedCsvEncoding = selectedCsvEncoding;
            _choosenOutputFileFormat = choosenOutputFileFormat;
            _showMilisec = showMilisec;
            _fileTypeId = fileTypeId;
            _transformerId = transformerId;
            _phase = phase;
            _mode = mode;

            InitializeUIelements();
            InitializeBackgroundWorker();

            // Сделать кнопку "отмена" неактивной, т.к. при извлечении *.zip архива запускается...
            // ...функция, а не процесс, который можно убить
            if (System.IO.Path.GetExtension(filePath) == ".zip" || System.IO.Path.GetExtension(filePath) == ".xlsx")
            {
                btnCancel.IsEnabled = false;
            }

            StartAsync();
        }

        private void InitializeBackgroundWorker()
        {
            _bgw.WorkerSupportsCancellation = true;

            _bgw.DoWork += new DoWorkEventHandler(BgwDoWork);
            _bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BgwWorkCompleted);
        }

        private void InitializeUIelements()
        {
            meWaitingGif.Source = new Uri(Directory.GetCurrentDirectory() + @"\Properties\wait.gif");
            btnRunExplorer.Visibility = Visibility.Hidden;
            btnClose.Visibility = Visibility.Hidden;
            lbProcessStatus.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Нанинает цепочку действий для импорта содержимого файла.
        /// </summary>
        /// <returns>Импорт выполнен успешно (true), неуспешно (false)</returns>
        private bool StartImportFile(string filePath)
        {
            Dictionary<string, int> fieldsMap = DataUploader.DataContext.GetFields(_fileTypeId);
            Dictionary<string, int> fieldsMapComplemented = null;
            List<DataModel.Measurement> measurementsList = null;
            ulong? updatedRows = null;
            int? intersectionsCount = null;
            
            if (fieldsMap != null)
            {
                fieldsMapComplemented = FileExtension.CheckMissingFields(filePath, (int)_fileTypeId, fieldsMap);
            }

            measurementsList = FileExtension.ParseExcelFile(filePath, fieldsMapComplemented, (int)_transformerId, _phase, new TimeSpan(0, 15, 0));
            updatedRows = DataUploader.DataContext.InsertBinary(measurementsList);
            intersectionsCount = DataUploader.DataContext.СheckIntersections();
            
            if (intersectionsCount == 0)
            {
                return DataUploader.DataContext.TransferTempData(_filePath);
            }
            else if (intersectionsCount > 0)
            {
                var r = MessageBox.Show(string.Format("Найдено существующих записей: {0}\nПерезаписать значения?", intersectionsCount),
                                        "Найдены существующие записи",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Warning,
                                        MessageBoxResult.Yes);

                if (r == MessageBoxResult.Yes)
                {
                    return DataUploader.DataContext.TransferTempData(_filePath, true);
                }
                else
                {
                    return DataUploader.DataContext.TransferTempData(_filePath);
                }
            }
            else
            {
                return false;
            }
        }

        private void StartAsync()
        {
            if (!_bgw.IsBusy)
            {
                _bgw.RunWorkerAsync();
            }
        }

        private void BgwDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            string fileExtension = Path.GetExtension(_filePath);

            try
            {
                switch (fileExtension)
                {
                    case ".zip":
                        if (_mode == null)
                        {
                            _destinationPath = Path.Combine(_destinationPath, Path.GetFileNameWithoutExtension(_filePath));
                            //_isCompleted = new Thread(FileExtension.ExtractArchiveZip(_filePath, _destinationPath));
                            
                            _isCompleted = FileExtension.ExtractArchiveZip(_filePath, _destinationPath);
                        }
                        else
                        {
                            MessageBox.Show("Импорт архива не поддерживается.",
                                            "Ошибка",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Warning,
                                            MessageBoxResult.Yes);
                        }
                        break;

                    case ".7z":
                        if (_mode == null)
                        {
                            _destinationPath = Path.Combine(_destinationPath, Path.GetFileNameWithoutExtension(_filePath));
                            _isCompleted = _fe.ExtractArchive7za(_filePath,
                                                                 _destinationPath);
                        }
                        else
                        {
                            MessageBox.Show("Импорт архива не поддерживается.",
                                            "Ошибка",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Warning,
                                            MessageBoxResult.Yes);
                        }
                        break;

                    case ".dtl":


                        // Определяем путь ко временной папке, куда будем копировать файлы для обработки
                        string tempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp");

                        // Создаём временную папку /temp
                        string path = Directory.GetCurrentDirectory();
                        DirectoryInfo dirInfo = new DirectoryInfo(tempDir);
                        if (!dirInfo.Exists)
                        {
                            dirInfo.Create();
                        }

                        // Файлы для обработки копируются во временную папку /temp
                        string tempFilePath = Path.Combine(tempDir, Path.GetFileName(_filePath));
                        System.IO.File.Copy(_filePath, tempFilePath, true);

                        // конвертация/разархивации файла
                        if (_mode == null)
                        {
                            _isCompleted = _fe.ConvertDtl(tempFilePath,
                                                          _destinationPath,
                                                          _selectedCsvEncoding,
                                                          _choosenOutputFileFormat,
                                                          _showMilisec);
                        }
                        // импорт файла
                        else
                        {
                            bool isConverted = _fe.ConvertDtl(tempFilePath,
                                                              tempDir,
                                                              "ASCII",
                                                              ".xlsx",
                                                              "/t1");

                            if (isConverted)
                            {
                                _importIsCompleted = StartImportFile(Path.Combine(tempDir, Path.GetFileNameWithoutExtension(tempFilePath) + ".xlsx"));
                            }
                            else
                            {
                                _importIsCompleted = false;
                            }

                            // Удалим временные файлы (не папки) в папке /temp
                            DirectoryInfo di = new DirectoryInfo(tempDir);
                            foreach (FileInfo tempFile in di.GetFiles())
                            {
                                tempFile.Delete();
                            }
                        }
                        break;

                    case ".xlsx":
                        _importIsCompleted = StartImportFile(_filePath);
                        break;

                    default:
                        MessageBox.Show("Расширение выбранного файла не соответствует следующим форматам:\n- *.zip\n- *.7z\n- *.dtl\n- *.xlsx",
                                        "Ошибка",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning,
                                        MessageBoxResult.Yes);
                        break;
                }
            }
            catch (Exception ex)
            {
                string messageBoxText = ex.Message;
                string caption = "Ошибка";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
            }

            if (worker.CancellationPending == true)
            {
                e.Cancel = true;
            }
        }

        private void BgwWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Close();
            }
            else if (e.Error != null) // Если ошибка в BackgroundWorker
            {
                MessageBox.Show(e.Error.Message,
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);
                Close();
            }
            else if (_importIsCompleted == true)
            {
                meWaitingGif.Visibility = Visibility.Collapsed;
                lbProcessStatus.Visibility = Visibility.Visible;
                lbProcessStatus.Text = "Импорт в базу данных выполнен успешно!";
                btnCancel.Visibility = Visibility.Collapsed;
                btnClose.Visibility = Visibility.Visible;
                btnRunExplorer.Visibility = Visibility.Collapsed;
            }
            else if (_isCompleted == true)
            {
                meWaitingGif.Visibility = Visibility.Collapsed;
                lbProcessStatus.Visibility = Visibility.Visible;
                lbProcessStatus.Text = "Операция завершена";
                btnCancel.Visibility = Visibility.Collapsed;
                btnRunExplorer.Visibility = Visibility.Visible;
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Обработка события MediaEnded в MediaElement meWaitingGif.
        /// Выполняет перезапуск gif-анимации, чтобы она не прерывалась.
        /// </summary>
        private void GifEnded(object sender, RoutedEventArgs e)
        {
            meWaitingGif.Position = new TimeSpan(0, 0, 1);
        }

        /// <summary>
        /// Обработка события Click в Button btnCancel.
        /// Отмена операции импорта.
        /// </summary>
        private void CancelOperation(object sender, RoutedEventArgs e)
        {
            if (_bgw.WorkerSupportsCancellation == true)
            {
                _bgw.CancelAsync();
                _fe.KillProcess();
                _isCompleted = false;
            }
        }

        /// <summary>
        /// Обработка события Click в Button btnClose. Закрывает форму.
        /// </summary>
        private void CloseForm(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Обработка события Click в Button btnRunExplorer. Открывает директорию, в которую
        /// был разархивирован архив.
        /// </summary>
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", _destinationPath);
            Close();
        }

        /// <summary>
        /// Обработка события Closing в элементе Window класса WaitingBox.
        /// </summary>
        private void FormClosing(object sender, CancelEventArgs e)
        {
            if (_bgw.IsBusy)
            {
                var r = MessageBox.Show("Вы уверены, что хотите прервать выполнение операции?",
                                        "Прервать операцию",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Warning,
                                        MessageBoxResult.Yes);
                if (r == MessageBoxResult.Yes)
                {
                    if (System.IO.Path.GetExtension(_filePath) == ".zip")
                    {
                        // *.zip архив извлекаетсыя не сторонним процессом, который...
                        // ...можно убить, а местной функцией ExtractToDirectory
                        MessageBox.Show("Прерывание операции извлечения *.zip архива не поддерживается.",
                        "Ошибка прерывания операции",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.Yes);
                        
                        // Отменить закрытие окна
                        e.Cancel = true;
                    }
                    else if (System.IO.Path.GetExtension(_filePath) == ".xlsx")
                    {
                        // *.zip архив извлекаетсыя не сторонним процессом, который...
                        // ...можно убить, а местной функцией ExtractToDirectory
                        MessageBox.Show("Прерывание операции обработки *.xlsx файла не поддерживается.",
                        "Ошибка прерывания операции",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.Yes);

                        // Отменить закрытие окна
                        e.Cancel = true;
                    }
                    else // Если выбранный файл не *.zip формата
                    {
                        if (_bgw.WorkerSupportsCancellation == true)
                        {
                            _bgw.CancelAsync();
                            _fe.KillProcess();
                        }
                    }
                }
                else // MessageBoxResult.No
                {
                    // Отменить закрытие окна
                    e.Cancel = true;
                }
            }
        }
    }
}
