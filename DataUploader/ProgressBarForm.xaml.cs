using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using Path = System.IO.Path;

namespace DataUploader
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class ProgressBarForm : Window
    {
        // Для выполнения операции конвертирования файлов в отдельном потоке
        private readonly BackgroundWorker _bgw = new BackgroundWorker();

        // Для возможности обратиться к процессу конвертирования, запущенному внутри...
        //...данного экземпляра
        private readonly FileExtension _fe = new FileExtension();

        // Чтобы засечь сколько прошло времени с момента старта операции
        private System.Threading.Timer _t = null;

        private TimeSpan _startTime;

        private string _folderPath;
        private string _destinationPath;
        private string _selectedCsvEncoding;
        private string _choosenOutputFileFormat;
        private string _showMilisec;

        public bool _isCompleted = false;

        public ProgressBarForm(string folderPath,
                               string destinationPath,
                               string selectedCsvEncoding,
                               string choosenOutputFileFormat,
                               string showMilisec)
        {
            InitializeComponent();

            _folderPath = folderPath;
            _destinationPath = destinationPath;
            _selectedCsvEncoding = selectedCsvEncoding;
            _choosenOutputFileFormat = choosenOutputFileFormat;
            _showMilisec = showMilisec;

            UIelementsInitialization();

            InitializeBackgroundWorker();

            InitializeTimer();

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
        private void InitializeTimer()
        {
            _startTime = new TimeSpan(0, 0, 0);
        }

        private void UIelementsInitialization()
        {
            btnRunExplorer.Visibility = Visibility.Hidden;
            lbProcessStatus.Visibility = Visibility.Hidden;
            pbProcessProgress.Value = 0;
        }
        void TimerCallback(object state)
        {
            this._startTime += new TimeSpan(0,0,1);
            // Обновление TextBlock tbTimePassed из другого потока
            DelegateShowTime(string.Format("{0:hh\\:mm\\:ss}", (_startTime)));
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

            // Поиск *.dtl файлов в выбранной папке и вложенных папках
            string[] dtlFiles = Directory.GetFiles(_folderPath, "*.dtl", SearchOption.AllDirectories);

            // Запуск таймера
            _t = new System.Threading.Timer(TimerCallback, null, 0, 1000);
                
            // Обновление TextBlock tbFilesLeft из другого потока 
            DelegateShowFilesLeft(dtlFiles.Length.ToString() + " из " + dtlFiles.Length.ToString());

            // Определяем путь ко временной папке, куда будем копировать файлы для обработки
            string tempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp");

            // Создаём временную папку /temp
            string path = Directory.GetCurrentDirectory();
            DirectoryInfo dirInfo = new DirectoryInfo(tempDir);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            // Обработка каждого найденного *.dtl файла в папке
            for (int i = 0; i < dtlFiles.Length; i++)
            {

                // Файлы для обработки копируются во временную папку /temp
                string tempFilePath = Path.Combine(tempDir, Path.GetFileName(dtlFiles[i]));
                System.IO.File.Copy(dtlFiles[i], tempFilePath, true);

                // Вызов делегата для обновление TextBlock tbFileInProcess из другого потока   
                DelegateShowCurrentFile(dtlFiles[i]);

                try
                {
                    _isCompleted = _fe.ConvertDtl(tempFilePath,
                                   _destinationPath,
                                   _selectedCsvEncoding,
                                   _choosenOutputFileFormat,
                                   _showMilisec);
                }
                catch (Exception ex)
                {
                    string messageBoxText = ex.Message;
                    string caption = "Ошибка конвертации файла";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                }

                // Обновление TextBlock tbFilesLeft из другого потока   
                DelegateShowFilesLeft((dtlFiles.Length - i - 1).ToString() + " из " + dtlFiles.Length.ToString());

                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }

                worker.ReportProgress( (int)(((float)(i + 1) / (float)dtlFiles.Length) * 100) );
            }
        }

        // Объявление делегата (указателя на метод) с именем InvokeDelegate - может указывать...
        // ...на любой метод, который, возвращает void и принимает входной параметр типа string
        private delegate void InvokeDelegate(string showString);

        // Объявление метода, на который будет указывать делегат
        private void DelegateShowCurrentFile(string currentFilePath)
        {
            if (!Dispatcher.CheckAccess())
            {
                // Создание делегата InvokeDelegate и чтобы он указывал на DelegateShowCurrentFile
                InvokeDelegate invokeDelegate = new InvokeDelegate(DelegateShowCurrentFile);

                Dispatcher.Invoke(invokeDelegate, currentFilePath);
                return;
            }
            tbFileInProcess.Text = "Имя: " + System.IO.Path.GetFileName(currentFilePath);
        }

        private void DelegateShowFilesLeft(string filesLeft)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new InvokeDelegate(DelegateShowFilesLeft), filesLeft);
                return;
            }
            tbFilesLeft.Text = string.Format("Осталось элементов: {0}", filesLeft);
        }

        private void DelegateShowTime(string time)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new InvokeDelegate(DelegateShowTime), time);
                return;
            }
            tbTimePassed.Text = time;
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            tbPercentCompleted.Text = ("Выполнено " + e.ProgressPercentage.ToString() + "%");
            pbProcessProgress.Value = e.ProgressPercentage;
        }

        private void BgwWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (e.Cancelled)
            {
                _t.Dispose();
                Close();
            }
            else if (e.Error != null) // Если ошибка в BackgroundWorker
            {
                _t.Dispose();

                string messageBoxText = e.Error.Message;
                string caption = "Ошибка";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                
                Close();
            }
            else if (_isCompleted == true)
            {
                _t.Dispose();

                lbProcessStatus.Visibility = Visibility.Visible;
                lbProcessStatus.Text = "Операция завершена";
                btnCancel.Visibility = Visibility.Collapsed;
                btnRunExplorer.Visibility = Visibility.Visible;

                tbPercentCompleted.Visibility = Visibility.Collapsed;
                pbProcessProgress.Visibility = Visibility.Collapsed;
                tbFileInProcess.Visibility = Visibility.Collapsed;
                tbFilesLeft.Visibility = Visibility.Collapsed;
                tbTimePassed.Visibility = Visibility.Collapsed;
            }
            else
            {
                _t.Dispose();
                Close();
            }
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
            }
        }

        /// <summary>
        /// Обработка события Click в Button btnRunExplorer.
        /// </summary>
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", _destinationPath.Replace("/", "\\"));
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
