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
using System.Threading;
using System.IO;
using System.Timers;
using Path = System.IO.Path;

namespace DataUploader
{
    /// <summary>
    /// Логика взаимодействия для ImportBox.xaml
    /// </summary>
    public partial class ImportBox : Window
    {

        // Для выполнения операции конвертирования файлов в отдельном потоке
        private readonly BackgroundWorker _bgw = new BackgroundWorker();

        // Для возможности обратиться к процессу конвертирования, запущенному внутри...
        //...данного экземпляра
        private readonly FileExtension _fe = new FileExtension();

        // Чтобы засечь сколько прошло времени с момента старта операции
        private System.Timers.Timer _t = null;
        private DateTime _startTime;

        private string _folderPath;
        private int? _fileTypeId;
        private int? _transformerId;
        private string _phase;

        // Список, где будут храниться все файлы, которые подлежат импорту в текущей сессии импорта
        private List<DataModel.FileImportInfo> _fileImportInfoList = new List<DataModel.FileImportInfo>();

        //Queue<string> convertedFilesQueue = new Queue<string>();
        //ManualResetEvent conversionStatusEvent = new ManualResetEvent(false);
        //ManualResetEvent threadStatusEvent = new ManualResetEvent(false);

        public bool _isConverted = false;
        public bool _isImported = false;
        public bool _isCompleted = false;

        private string _errorMessage = null;

        public ImportBox(string folderPath,
                         int? fileTypeId,
                         int? transformerId,
                         string phase)
        {
            InitializeComponent();

            _folderPath = folderPath;
            _fileTypeId = fileTypeId;
            _transformerId = transformerId;
            _phase = phase;

            // Привязываем список кака источник данных для элемента ListView lvImportedFiles
            lvImportedFiles.ItemsSource = _fileImportInfoList;

            InitializeUIelements();
            InitializeBackgroundWorker();

            StartAsync();
        }

        private void InitializeBackgroundWorker()
        {
            _bgw.WorkerReportsProgress = true;
            _bgw.WorkerSupportsCancellation = true;

            _bgw.DoWork += new DoWorkEventHandler(BgwDoWork);
            _bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BgwWorkCompleted);
            _bgw.ProgressChanged += new ProgressChangedEventHandler(ProgressChanged);
        }

        private void SetTimer()
        {
            _startTime = DateTime.Now;

            // Интервал 1000 мс
            _t = new System.Timers.Timer(1000);

            _t.Elapsed += OnTimedEvent;
            _t.AutoReset = true;
            _t.Enabled = true;
        }

        private void InitializeUIelements()
        {
            btnClose.Visibility = Visibility.Hidden;
            lbImportStatus.Visibility = Visibility.Hidden;
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

            // Запуск таймера
            SetTimer();

            // Поиск *.dtl файлов в выбранной папке и вложенных папках
            string[] dtlFiles = Directory.GetFiles(_folderPath, "*.dtl", SearchOption.AllDirectories);

            for (int i = 0; i < dtlFiles.Length; i++)
            {
                var tempFileImportInfo = new DataModel.FileImportInfo()
                {
                    Id = i,
                    FilePath = dtlFiles[i],
                    FileName = Path.GetFileName(dtlFiles[i]),
                    ProcessStatus = null,
                    IntersectionsCount = "-"
                };

                // Этот список уже привязан как источник данных для элемента ListView lvImportedFiles
                _fileImportInfoList.Add(tempFileImportInfo);
            }

            // Определяем путь ко временной папке, куда будем копировать файлы для обработки
            string tempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp");

            // Создаём временную папку /temp
            string path = Directory.GetCurrentDirectory();
            DirectoryInfo dirInfo = new DirectoryInfo(tempDir);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            for (int i = 0; i < _fileImportInfoList.Count; i++)
            {               
                try
                {
                    // Файлы для обработки копируются во временную папку /temp
                    string tempFilePath = Path.Combine(tempDir, Path.GetFileName(dtlFiles[i]));
                    _fileImportInfoList[i].ProcessStatus = "⌛ (копирование)";
                    File.Copy(dtlFiles[i], tempFilePath, true);

                    _fileImportInfoList[i].ProcessStatus = "⌛ (конвертация)";
                    _isConverted = _fe.ConvertDtl(tempFilePath, tempDir, "ASCII", ".xlsx", "/t1");

                    // Отслеживаем запрос пользователем отмены операции
                    if (worker.CancellationPending == true)
                    {
                        _fileImportInfoList[i].ErrorMessage = "Конвертирование файла отменено";
                        e.Cancel = true;
                        break;
                    }

                    if (!_isConverted)
                    {
                        _fileImportInfoList[i].ProcessStatus = "✗";
                    }
                    else
                    {
                        _fileImportInfoList[i].ProcessStatus = "⌛ (импорт)";
                        _isImported = StartImportFile(Path.Combine(tempDir, Path.GetFileNameWithoutExtension(tempFilePath) + ".xlsx"), i);
                        if (_isImported)
                        {
                            _fileImportInfoList[i].ProcessStatus = "✓";
                        }
                        else
                        {
                            _fileImportInfoList[i].ProcessStatus = "✗";
                        }
                    }
                    
                    // Отслеживаем запрос пользователем отмены операции
                    if (worker.CancellationPending == true)
                    {
                        _fileImportInfoList[i].ErrorMessage = "Импорт файла отменен";
                        e.Cancel = true;
                        break;
                    }

                }
                catch (Exception ex)
                {
                    _fileImportInfoList[i].ProcessStatus = "✗";
                    _fileImportInfoList[i].ErrorMessage = ex.Message;
                }
                worker.ReportProgress((int)(((float)(i + 1) / (float)_fileImportInfoList.Count) * 100));
            }
            
            _isCompleted = true;

            // Удалим временные файлы (не папки) в папке /temp
            DirectoryInfo di = new DirectoryInfo(tempDir);
            foreach (FileInfo tempFile in di.GetFiles())
            {
                tempFile.Delete();
            }
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            tbPercentCompleted.Text = ("Выполнено " + e.ProgressPercentage.ToString() + "%");
            pbImportProgress.Value = e.ProgressPercentage;
        }

        private void BgwWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _t.Stop();
                _t.Dispose();

                lbImportStatus.Visibility = Visibility.Visible;
                lbImportStatus.Text = "Операция отменена";
                btnCancel.Visibility = Visibility.Hidden;
                btnClose.Visibility = Visibility.Visible;
            }
            else if (e.Error != null) // Если ошибка в BackgroundWorker
            {
                _t.Stop();
                _t.Dispose();

                lbImportStatus.Visibility = Visibility.Visible;
                lbImportStatus.Text = "Операция не завершена";
                btnCancel.Visibility = Visibility.Hidden;
                btnClose.Visibility = Visibility.Visible;

                MessageBox.Show(e.Error.Message,
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);

            }
            else // _isCompleted == true
            {
                _t.Stop();
                _t.Dispose();

                lbImportStatus.Visibility = Visibility.Visible;
                lbImportStatus.Text = "Операция завершена";
                btnCancel.Visibility = Visibility.Hidden;
                btnClose.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Нанинает цепочку действий для импорта содержимого файла.
        /// </summary>
        /// <returns>Импорт выполнен успешно (true), неуспешно (false)</returns>
        private bool StartImportFile(string filePath, int i)
        {
            _fileImportInfoList[i].ProcessStatus = "⌛ (Запрос полей в БД)";
            Dictionary<string, int> fieldsMap = DataUploader.DataContext.GetFields(_fileTypeId);
            Dictionary<string, int> fieldsMapComplemented = null;
            List<DataModel.Measurement> measurementsList = null;
            ulong? updatedRows = null;
            int? intersectionsCount = null;

            if (fieldsMap != null)
            {
                _fileImportInfoList[i].ProcessStatus = "⌛ (Сравнение заголовков *.xlsx ⇄ БД, уже имеющихся в БД)";
                fieldsMapComplemented = FileExtension.CheckMissingFields(filePath, (int)_fileTypeId, fieldsMap);
            }

            _fileImportInfoList[i].ProcessStatus = "⌛ (Парсинг *.xlsx файла)";
            measurementsList = FileExtension.ParseExcelFile(filePath, fieldsMapComplemented, (int)_transformerId, _phase, new TimeSpan(0, 15, 0));

            _fileImportInfoList[i].ProcessStatus = "⌛ (Вставка записей в БД во временную таблицу)";
            updatedRows = DataUploader.DataContext.InsertBinary(measurementsList);

            _fileImportInfoList[i].ProcessStatus = "⌛ (Проверка наложений в БД с существующими записями)";
            intersectionsCount = DataUploader.DataContext.СheckIntersections();
            _fileImportInfoList[i].IntersectionsCount = Convert.ToString(intersectionsCount);

            if (intersectionsCount == 0)
            {
                _fileImportInfoList[i].ProcessStatus = "⌛ (Перемещение записей в основную таблицу в БД)";
                return DataUploader.DataContext.TransferTempData(filePath);
            }
            else // intersectionsCount > 0
            {
                _fileImportInfoList[i].ProcessStatus = "⌛ (Перемещение записей в основную таблицу в БД с перезаписью обнаруженных наложений)";
                return DataUploader.DataContext.TransferTempData(filePath, true);
            }
        }

        // Событие обновления текстоваго поля, показывающее значение таймера
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // Обновление TextBlock tbTimePassed из другого потока
            DelegateShowTime(string.Format("{0:hh\\:mm\\:ss}", (e.SignalTime - _startTime)));
        }

        // Объявление делегата (указателя на метод) с именем InvokeDelegate - может указывать...
        // ...на любой метод, который, возвращает void и принимает входной параметр типа string
        private delegate void InvokeDelegate(string showString);
        
        // Объявление метода, на который будет указывать делегат
        private void DelegateShowTime(string time)
        {

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new InvokeDelegate(DelegateShowTime), time);
                return;
            }
            tbTimePassed.Text = time;
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
        /// Обработка события Closing в элементе Window класса ProgressBarForm.
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
                    if (_bgw.WorkerSupportsCancellation == true)
                    {
                        _bgw.CancelAsync();
                        _fe.KillProcess();
                        _isCompleted = false;
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
