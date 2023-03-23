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
    public partial class ImportForm : Window
    {
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
        private bool _averagingIsEnabled;
        private int _selectedAveragingRange;
        private bool _overwriteIsEnabled;

        // Для реализации отмены работы bgw без необходимости передачи экземпляра bgw в функции
        private BackgroundWorker _worker = null;
        private DoWorkEventArgs _e = null;

        // Список, где будут храниться все файлы, которые подлежат импорту в текущей сессии импорта
        private List<DataModel.FileImportInfo> _fileImportInfoList = new List<DataModel.FileImportInfo>();
        private int _importMsgCount = 0;

        public bool _isConverted = false;
        public bool _isImported = false;
        public bool _isCompleted = false;

        public ImportForm(string folderPath,
                         int? fileTypeId,
                         int? transformerId,
                         string phase,
                         bool averagingIsEnabled,
                         bool overwriteIsEnabled,
                         int selectedAveragingRange)
        {
            InitializeComponent();

            _folderPath = folderPath;
            _fileTypeId = fileTypeId;
            _transformerId = transformerId;
            _phase = phase;
            _averagingIsEnabled = averagingIsEnabled;
            _selectedAveragingRange = selectedAveragingRange;
            _overwriteIsEnabled = overwriteIsEnabled;

            // Привязываем список как источник данных для элемента ListView lvImportedFiles
            lvImportedFiles.ItemsSource = _fileImportInfoList;

            InitializeUIelements();
            InitializeBackgroundWorker();

            StartAsync();
        }

        private void InitializeUIelements()
        {
            btnClose.Visibility = Visibility.Hidden;
            lbImportStatus.Visibility = Visibility.Hidden;
        }

        // ----------------------------------------------------------------------------------------
        // Background worker
        // ----------------------------------------------------------------------------------------

        private void InitializeBackgroundWorker()
        {
            _bgw.WorkerReportsProgress = true;
            _bgw.WorkerSupportsCancellation = true;

            _bgw.DoWork += new DoWorkEventHandler(BgwDoWork);
            _bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BgwWorkCompleted);
            _bgw.ProgressChanged += new ProgressChangedEventHandler(ProgressChanged);
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

            _worker.ReportProgress(0);
            for (int i = 0; i < _fileImportInfoList.Count; i++)
            {               
                try
                {
                    // Файлы для обработки копируются во временную папку /temp
                    string tempFilePath = Path.Combine(tempDir, Path.GetFileName(dtlFiles[i]));
                    _fileImportInfoList[i].ProcessStatus = "⌛ (Копирование *.dtl файла во временную папку)";
                    File.Copy(dtlFiles[i], tempFilePath, true);

                    _fileImportInfoList[i].ProcessStatus = "⌛ (Конвертация *.dtl файла)";
                    _isConverted = _fe.ConvertDtl(tempFilePath, tempDir, "ASCII", ".xlsx", "/t1");

                    // Отслеживаем запрос пользователем отмены операции
                    if (IsCancelled(i, "Конвертирование файла отменено")) { break; }

                    if (!_isConverted)
                    {
                        _fileImportInfoList[i].ProcessStatus = "✗";
                    }
                    // Импорт
                    else
                    {
                        tempFilePath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(tempFilePath) + ".xlsx");

                        _fileImportInfoList[i].ProcessStatus = "⌛ (Запрос полей в БД)";
                        Dictionary<string, int> fieldsMap = DataUploader.DataContext.GetFields(_fileTypeId);

                        // Отслеживаем запрос пользователем отмены операции
                        if (IsCancelled(i, "Импрот файла отменен")) { break; }

                        Dictionary<string, int> fieldsMapComplemented = null;
                        _fileImportInfoList[i].ProcessStatus = "⌛ (Сравнение заголовков *.xlsx ⇄ БД, уже имеющихся в БД)";
                        fieldsMapComplemented = FileExtension.CheckMissingFields(tempFilePath, (int)_fileTypeId, fieldsMap);

                        // Отслеживаем запрос пользователем отмены операции
                        if (IsCancelled(i, "Импрот файла отменен")) { break; }

                        List<DataModel.ImportedMeasurement> measurementsList = null;
                        _fileImportInfoList[i].ProcessStatus = "⌛ (Парсинг *.xlsx файла)";
                        measurementsList = FileExtension.ParseExcelFile(tempFilePath, fieldsMapComplemented, (int)_transformerId, _phase, new TimeSpan(0, _selectedAveragingRange, 0), _averagingIsEnabled);

                        // Отслеживаем запрос пользователем отмены операции
                        if (IsCancelled(i, "Импрот файла отменен")) { break; }

                        ulong? updatedRowsAfterInsertBinary = null;
                        _fileImportInfoList[i].ProcessStatus = "⌛ (Вставка записей в БД во временную таблицу)";
                        updatedRowsAfterInsertBinary = DataUploader.DataContext.InsertBinary(measurementsList, _selectedAveragingRange, _averagingIsEnabled);

                        // Отслеживаем запрос пользователем отмены операции
                        if (IsCancelled(i, "Импрот файла отменен")) { DataUploader.DataContext.TruncateTempTableAsync(); break; }

                        int? intersectionsCount = null;
                        _fileImportInfoList[i].ProcessStatus = "⌛ (Проверка наложений в БД с существующими записями)";
                        intersectionsCount = DataUploader.DataContext.СheckIntersections();

                        // Отслеживаем запрос пользователем отмены операции
                        if (IsCancelled(i, "Импрот файла отменен")) { DataUploader.DataContext.TruncateTempTableAsync(); break; }

                        int? updatedRowsAfterTransferTempData = null;
                        if (intersectionsCount == 0)
                        {
                            _fileImportInfoList[i].IntersectionsCount = Convert.ToString(intersectionsCount);
                            _fileImportInfoList[i].ProcessStatus = "⌛ (Перемещение записей в основную таблицу в БД)";
                            updatedRowsAfterTransferTempData = DataUploader.DataContext.TransferTempData(tempFilePath);
                            if (updatedRowsAfterTransferTempData != null) { _isImported = true; } else { _isImported = false; }
                        }
                        else // intersectionsCount > 0
                        {
                            if (_overwriteIsEnabled)
                            {
                                _fileImportInfoList[i].IntersectionsCount = Convert.ToString(intersectionsCount);
                                _fileImportInfoList[i].ProcessStatus = "⌛ (Перемещение записей в основную таблицу в БД с перезаписью повторяющихся данных)";
                            }
                            else
                            {
                                _fileImportInfoList[i].IntersectionsCount = Convert.ToString(intersectionsCount);
                                _fileImportInfoList[i].ProcessStatus = "⌛ (Перемещение записей в основную таблицу в БД с пропуском повторяющихся данных)";
                            }
                            updatedRowsAfterTransferTempData = DataUploader.DataContext.TransferTempData(tempFilePath, _overwriteIsEnabled);
                            if (updatedRowsAfterTransferTempData != null) { _isImported = true; } else { _isImported = false; }
                            _fileImportInfoList[i].IntersectionsCount = String.Format("{1} из {0} перезаписано", intersectionsCount, updatedRowsAfterTransferTempData); 
                        }

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
                    if (IsCancelled(i, "Импорт файла отменен")) { break; }

                }
                catch (Exception ex)
                {
                    _fileImportInfoList[i].ProcessStatus = "✗";
                    _fileImportInfoList[i].ErrorMessage = String.Format("{0}\n{1}\n{2}", ex.Message, ex.InnerException, ex.StackTrace);
                    _importMsgCount++;

                    if (!_isImported) { DataUploader.DataContext.TruncateTempTableAsync(); }
                }
                _worker.ReportProgress(i + 1);
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
            pbImportProgress.Value = (int)(((float)(e.ProgressPercentage) / (float)_fileImportInfoList.Count) * 100);
            tbPercentCompleted.Text = String.Format("Выполнено {0}% ({1} из {2})", pbImportProgress.Value.ToString(), e.ProgressPercentage, _fileImportInfoList.Count); 
        }

        private void BgwWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _t.Stop();
                _t.Dispose();

                lbImportStatus.Visibility = Visibility.Visible;
                lbImportStatus.Text = String.Format("Операция отменена: сообщений {0}", _importMsgCount);
                btnCancel.Visibility = Visibility.Hidden;
                btnClose.Visibility = Visibility.Visible;
            }
            else if (e.Error != null) // Если ошибка в BackgroundWorker
            {
                _t.Stop();
                _t.Dispose();

                lbImportStatus.Visibility = Visibility.Visible;
                lbImportStatus.Text = String.Format("Операция не завершена: сообщений {0}", _importMsgCount);
                btnCancel.Visibility = Visibility.Hidden;
                btnClose.Visibility = Visibility.Visible;

                string messageBoxText = String.Format("{0}\n{1}\n{2}", e.Error.Message, e.Error.InnerException, e.Error.StackTrace);
                string caption = "Ошибка";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
            }
            else // _isCompleted == true
            {
                _t.Stop();
                _t.Dispose();

                lbImportStatus.Visibility = Visibility.Visible;
                lbImportStatus.Text = String.Format("Операция завершена: сообщений {0}", _importMsgCount);
                btnCancel.Visibility = Visibility.Hidden;
                btnClose.Visibility = Visibility.Visible;
            }
        }

        // ----------------------------------------------------------------------------------------
        // Обработка событий формы
        // ----------------------------------------------------------------------------------------

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

        // ----------------------------------------------------------------------------------------
        // Вспомогательные функции
        // ----------------------------------------------------------------------------------------

        // Событие обновления текстоваго поля, показывающее значение таймера
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // Обновление TextBlock tbTimePassed из другого потока
            DelegateShowTime(String.Format("{0:hh\\:mm\\:ss}", (e.SignalTime - _startTime)));
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

        private void SetTimer()
        {
            _startTime = DateTime.Now;

            // Интервал 1000 мс
            _t = new System.Timers.Timer(1000);

            _t.Elapsed += OnTimedEvent;
            _t.AutoReset = true;
            _t.Enabled = true;
        }

        // Отслеживает запрос пользователем отмены операции
        private bool IsCancelled(int i, string message)
        {
            if (_worker.CancellationPending == true)
            {
                _fileImportInfoList[i].ErrorMessage = message;
                _importMsgCount++;
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
