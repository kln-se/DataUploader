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
    public partial class WaitingForm : Window
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
        private bool _averagingIsEnabled;
        private int _selectedAveragingRange;

        // Для реализации отмены работы bgw без необходимости передачи экземпляра bgw в функции
        private BackgroundWorker _worker = null;
        private DoWorkEventArgs _e = null;

        // В каком режиме запускается это окно:
        // * по умолчанию null - при конвертации/разархивации файлов
        // * "import" - при импорте содержимого файла в базу данных
        private string _mode;

        public bool _isConverted = false;
        private bool _isCompleted = false;
        private bool _importIsCompleted = false;

        public bool ImportIsCompleted
        {
            get { return _importIsCompleted; }
        }

        public WaitingForm(string filePath,
                          string destinationPath = null,
                          string selectedCsvEncoding = null,
                          string choosenOutputFileFormat = null,
                          string showMilisec = null,
                          int? fileTypeId = null,
                          int? transformerId = null,
                          string phase = null,
                          string mode = null,
                          bool averagingIsEnabled = true,
                          int selectedAveragingRange = 30)
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
            _averagingIsEnabled = averagingIsEnabled;
            _selectedAveragingRange = selectedAveragingRange;

            InitializeUIelements();
            InitializeBackgroundWorker();

            // Сделать кнопку "отмена" неактивной, т.к. при извлечении *.zip архива запускается...
            // ...функция, а не процесс, который можно убить
            if (System.IO.Path.GetExtension(filePath) == ".zip")
            {
                btnCancel.IsEnabled = false;
            }

            StartAsync();
        }

        private void InitializeUIelements()
        {
            meWaitingGif.Source = new Uri(Directory.GetCurrentDirectory() + @"\Properties\wait.gif");
            btnRunExplorer.Visibility = Visibility.Hidden;
            btnClose.Visibility = Visibility.Hidden;
            tbProcessStatus.Visibility = Visibility.Hidden;
        }

        // ----------------------------------------------------------------------------------------
        // Background worker
        // ----------------------------------------------------------------------------------------

        private void InitializeBackgroundWorker()
        {
            _bgw.WorkerSupportsCancellation = true;

            _bgw.DoWork += new DoWorkEventHandler(BgwDoWork);
            _bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BgwWorkCompleted);
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
            _worker = sender as BackgroundWorker;
            _e = e;

            string fileExtension = Path.GetExtension(_filePath);

            try
            {
                switch (fileExtension)
                {
                    case ".zip":
                        if (_mode == null)
                        {
                            _destinationPath = Path.Combine(_destinationPath, Path.GetFileNameWithoutExtension(_filePath));

                            DelegateShowProcessStage("⌛ (Извлечение *.zip архива)");
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
                            DelegateShowProcessStage("⌛ (Извлечение *.7z архива)");
                            _isCompleted = _fe.ExtractArchive7za(_filePath, _destinationPath);
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

                    case ".xlsx":
                        _importIsCompleted = StartImportFile(_filePath);
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
                        DelegateShowProcessStage("⌛ (Копирование *.dtl файла во временную папку)");
                        File.Copy(_filePath, tempFilePath, true);

                        // Конвертация/Разархивация файла
                        if (_mode == null)
                        {
                            DelegateShowProcessStage("⌛ (Конвертация *.dtl файла)");
                            _isCompleted = _fe.ConvertDtl(tempFilePath,
                                                          _destinationPath,
                                                          _selectedCsvEncoding,
                                                          _choosenOutputFileFormat,
                                                          _showMilisec);
                        }
                        // Импорт файла
                        else
                        {
                            DelegateShowProcessStage("⌛ (Конвертация *.dtl файла)");
                            _isConverted = _fe.ConvertDtl(tempFilePath,
                                                          tempDir,
                                                          "ASCII",
                                                          ".xlsx",
                                                          "/t1");

                            if (_isConverted)
                            {
                                DelegateShowProcessStage("⌛ (Импорт сконвертированного *.xlsx файла)");
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

                    default:
                        MessageBox.Show("Расширение выбранного файла не соответствует следующим форматам:\n- *.zip\n- *.7z\n- *.dtl\n- *.xlsx",
                                        "Ошибка",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning,
                                        MessageBoxResult.Yes);
                        break;
                }
                // Отслеживаем запрос пользователем отмены операции
                IsCancelled();
            }
            catch (Exception ex)
            {
                string messageBoxText = string.Format("{0}\n{1}\n{2}", ex.Message, ex.InnerException, ex.StackTrace);
                string caption = "Ошибка";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
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
                string messageBoxText = string.Format("{0}\n{1}\n{2}", e.Error.Message, e.Error.InnerException, e.Error.StackTrace);
                string caption = "Ошибка";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                Close();
            }
            else if (_importIsCompleted == true)
            {
                meWaitingGif.Visibility = Visibility.Hidden;
                tbProcessStage.Visibility = Visibility.Hidden;
                tbProcessStatus.Visibility = Visibility.Visible;
                tbProcessStatus.Text = "Импорт в базу данных выполнен успешно!";
                btnCancel.Visibility = Visibility.Collapsed;
                btnClose.Visibility = Visibility.Visible;
                btnRunExplorer.Visibility = Visibility.Collapsed;
            }
            else if (_isCompleted == true)
            {
                meWaitingGif.Visibility = Visibility.Hidden;
                tbProcessStage.Visibility = Visibility.Hidden;
                tbProcessStatus.Visibility = Visibility.Visible;
                tbProcessStatus.Text = "Операция завершена";
                btnCancel.Visibility = Visibility.Collapsed;
                btnRunExplorer.Visibility = Visibility.Visible;
            }
            else
            {
                Close();
            }
        }

        // ----------------------------------------------------------------------------------------
        // Обработка событий формы
        // ----------------------------------------------------------------------------------------

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
                _importIsCompleted = false;
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
                        MessageBox.Show("Прерывание операции извлечения *.zip архива не поддерживается. В случае закрытия окна операция будет закончена в фоновом режиме.",
                        "Предупреждение прерывания операции",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.Yes);

                        // Применить закрытие окна
                        e.Cancel = false;
                    }
                    else // Если выбранный файл не *.zip формата
                    {
                        if (_bgw.WorkerSupportsCancellation == true)
                        {
                            _bgw.CancelAsync();
                            _fe.KillProcess();
                            _isCompleted = false;
                            _importIsCompleted = false;
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

        // ----------------------------------------------------------------------------------------
        // Вспомогательные функции
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Нанинает цепочку действий для импорта содержимого файла.
        /// </summary>
        /// <returns>Импорт выполнен успешно (true), неуспешно (false)</returns>
        private bool StartImportFile(string filePath)
        {
            DelegateShowProcessStage("⌛ (Запрос полей в БД)");
            Dictionary<string, int> fieldsMap = DataUploader.DataContext.GetFields(_fileTypeId);

            // Отслеживаем запрос пользователем отмены операции
            if (IsCancelled()) { return false; };

            Dictionary<string, int> fieldsMapComplemented = null;
            DelegateShowProcessStage("⌛ (Сравнение заголовков *.xlsx ⇄ БД, уже имеющихся в БД)");
            fieldsMapComplemented = FileExtension.CheckMissingFields(filePath, (int)_fileTypeId, fieldsMap);

            // Отслеживаем запрос пользователем отмены операции
            if (IsCancelled()) { return false; };

            List<DataModel.ImportedMeasurement> measurementsList = null;
            DelegateShowProcessStage("⌛ (Парсинг *.xlsx файла)");
            measurementsList = FileExtension.ParseExcelFile(filePath, fieldsMapComplemented, (int)_transformerId, _phase, new TimeSpan(0, _selectedAveragingRange, 0), _averagingIsEnabled);

            // Отслеживаем запрос пользователем отмены операции
            if (IsCancelled()) { return false; };

            ulong? updatedRows = null;
            DelegateShowProcessStage("⌛ (Вставка записей в БД во временную таблицу)");
            updatedRows = DataUploader.DataContext.InsertBinary(measurementsList, _selectedAveragingRange, _averagingIsEnabled);

            // Отслеживаем запрос пользователем отмены операции
            if (IsCancelled()) { DataUploader.DataContext.TruncateTempTableAsync(); return false; }

            int? intersectionsCount = null;
            DelegateShowProcessStage("⌛ (Проверка наложений в БД с существующими записями)");
            intersectionsCount = DataUploader.DataContext.СheckIntersections();

            // Отслеживаем запрос пользователем отмены операции
            if (IsCancelled()) { DataUploader.DataContext.TruncateTempTableAsync(); return false; }

            DelegateShowProcessStage("⌛ (Перемещение записей в основную таблицу в БД)");
            int? updatedRowsAfterTransferTempData = null;
            if (intersectionsCount == 0)
            {
                updatedRowsAfterTransferTempData = DataUploader.DataContext.TransferTempData(_filePath);
                if (updatedRowsAfterTransferTempData != null) { return true; } else { return false; }
            }
            else
            {
                var r = MessageBox.Show(string.Format("Найдено существующих записей: {0}\nПерезаписать значения?", intersectionsCount),
                                        "Найдены существующие записи",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Warning,
                                        MessageBoxResult.Yes);

                if (r == MessageBoxResult.Yes)
                {
                    updatedRowsAfterTransferTempData = DataUploader.DataContext.TransferTempData(_filePath, true);
                    if (updatedRowsAfterTransferTempData != null) { return true; } else { return false; }
                }
                else
                {
                    updatedRowsAfterTransferTempData = DataUploader.DataContext.TransferTempData(_filePath);
                    if (updatedRowsAfterTransferTempData != null) { return true; } else { return false; }
                }
            }
        }

        // Объявление делегата (указателя на метод) с именем InvokeDelegate - может указывать...
        // ...на любой метод, который, возвращает void и принимает входной параметр типа string
        private delegate void InvokeDelegate(string showString);

        // Объявление метода, на который будет указывать делегат
        private void DelegateShowProcessStage(string processStage)
        {

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new InvokeDelegate(DelegateShowProcessStage), processStage);
                return;
            }
            tbProcessStage.Text = processStage;
        }

        // Отслеживает запрос пользователем отмены операции
        private bool IsCancelled()
        {
            if (_worker.CancellationPending == true)
            {
                _e.Cancel = true;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
