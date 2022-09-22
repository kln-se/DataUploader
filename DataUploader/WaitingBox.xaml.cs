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

namespace DataUploader
{
    /// <summary>
    /// Логика взаимодействия для WaitingBox.xaml
    /// </summary>
    public partial class WaitingBox : Window
    {

        // Для выполнения операции извлечения архива в отдельном потоке
        private BackgroundWorker bgw = new BackgroundWorker();

        // Для возможности обратиться к процессу ивлечения/конвертирования заущенному внутри...
        //...данного экземпляра
        private FileExtension fE = null;

        public string filePath;
        public string destinationPath;
        public string selectedCsvEncoding;
        public string choosenFileFormat;
        public string showMilisec;

        bool IsFailedToComplete = false;

        public WaitingBox(string filePath,
                          string destinationPath,
                          string selectedCsvEncoding,
                          string choosenFileFormat,
                          string showMilisec)
        {
            InitializeComponent();

            this.filePath = filePath;
            this.destinationPath = destinationPath;
            this.selectedCsvEncoding = selectedCsvEncoding;
            this.choosenFileFormat = choosenFileFormat;
            this.showMilisec = showMilisec;

            UIelementsInitialization();

            InitializeBackgroundWorker();
            
            // Сделать кнопку "отмена" неактивной, т.к. при извлечении *.zip архива запускается...
            // ...местная функция, а не процесс, который можно убить
            if (System.IO.Path.GetExtension(filePath) == ".zip")
            {
                btnCancel.IsEnabled = false;
            }

            StartAsync();
        }

        private void InitializeBackgroundWorker()
        {
            bgw.WorkerReportsProgress = true;
            bgw.WorkerSupportsCancellation = true;

            bgw.DoWork += new DoWorkEventHandler(BgwDoWork);
            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BgwWorkCompleted);

        }

        private void UIelementsInitialization()
        {
            meWaitingGif.Source = new Uri(Directory.GetCurrentDirectory() + @"\Properties\wait.gif");
            btnRunExplorer.Visibility = Visibility.Hidden;
            btnErrorClose.Visibility = Visibility.Hidden;
            lbExtractStatus.Visibility = Visibility.Hidden;
        }

        private void StartAsync()
        {
            if (!bgw.IsBusy)
            {
                this.fE = new FileExtension();
                bgw.RunWorkerAsync();
            }
        }

        private void CancelOperation(object sender, RoutedEventArgs e)
        {
            if (bgw.WorkerSupportsCancellation == true)
            {
                bgw.CancelAsync();
                this.fE.KillProcess();
                IsFailedToComplete = true;
            }
        }

        private void BgwDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            //throw new InvalidOperationException(); //(!)Test
            string fileExtension = System.IO.Path.GetExtension(this.filePath);

            switch (fileExtension)
            {
                case ".zip":
                    //Control.Invoke((MethodInvoker)(() => control.Text = "new text"));
                    this.IsFailedToComplete = FileExtension.ExtractArchiveZip(this.filePath,
                                                                              this.destinationPath + "/" + System.IO.Path.GetFileNameWithoutExtension(filePath));
                    break;
                case ".7z":
                    this.IsFailedToComplete = fE.ExtractArchive7z(this.filePath,
                                                                  this.destinationPath + "/" + System.IO.Path.GetFileNameWithoutExtension(filePath));
                    break;
                case ".dtl":
                    this.IsFailedToComplete = fE.ConvertDtl(this.filePath,
                                                                       this.destinationPath,
                                                                       this.selectedCsvEncoding,
                                                                       this.choosenFileFormat,
                                                                       this.showMilisec);
                    break;
                case "":
                    System.Windows.MessageBox.Show("Файл не выбран.",
                                                   "Ошибка выбора файла",
                                                   MessageBoxButton.OK,
                                                   MessageBoxImage.Warning,
                                                   MessageBoxResult.Yes);
                    this.IsFailedToComplete = true;
                    break;
                default:
                    System.Windows.MessageBox.Show("Расширение выбранного файла не соответствует следующим форматам:\n- *.zip\n- *.7z\n- *.dtl",
                                                   "Ошибка",
                                                   MessageBoxButton.OK,
                                                   MessageBoxImage.Warning,
                                                   MessageBoxResult.Yes);
                    this.IsFailedToComplete = true;
                    break;
            }

            if (worker.CancellationPending == true)
            {
                e.Cancel = true;
            }
        }

        private void BgwWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                this.Close();
            }
            else if (e.Error != null) // Если ошибка в BackGroundWorker
            {
                meWaitingGif.Visibility = Visibility.Collapsed;
                lbExtractStatus.Visibility = Visibility.Visible;
                lbExtractStatus.Text = "Ошибка: " + e.Error.Message;
                lbExtractStatus.ToolTip = e.Error.Message;
                btnCancel.Visibility = Visibility.Collapsed;
                btnErrorClose.Visibility = Visibility.Visible;
            }
            else if (IsFailedToComplete == false)
            {
                meWaitingGif.Visibility = Visibility.Collapsed;
                lbExtractStatus.Visibility = Visibility.Visible;
                lbExtractStatus.Text = "Операция завершена";
                btnCancel.Visibility = Visibility.Collapsed;
                btnRunExplorer.Visibility = Visibility.Visible;
            }
            else
            {
                System.Windows.MessageBox.Show("Операция не завершена",
                                               "Ошибка",
                                               MessageBoxButton.OK,
                                               MessageBoxImage.Warning,
                                               MessageBoxResult.Yes);
                this.Close();
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
        /// Обработка события Click в Button btnErrorClose.
        /// </summary>
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Обработка события Click в Button btnRunExplorer. Открывает директорию, в которую
        /// был разархивирован архив.
        /// </summary>
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", this.destinationPath.Replace("/", "\\"));
            this.Close();
        }

        private void FormClosing(object sender, CancelEventArgs e)
        {
            if (bgw.IsBusy)
            {
                var r = System.Windows.MessageBox.Show("Вы уверены, что хотите прервать выполнение операции?",
                                                       "Прервать операцию",
                                                       MessageBoxButton.YesNo,
                                                       MessageBoxImage.Warning,
                                                       MessageBoxResult.Yes);
                if (r == MessageBoxResult.Yes)
                {
                    if (System.IO.Path.GetExtension(filePath) == ".zip")
                    {
                        // Потому что *.zip архив извлекаетсыя не сторонним процессом, который...
                        // ...можно убить, а местной функцией ExtractToDirectory
                        System.Windows.MessageBox.Show("Прерывание операции извлечения *.zip архива не поддерживается.",
                               "Ошибка прерывания операции",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning,
                               MessageBoxResult.Yes);
                        
                        // Отменить закрытие окна
                        e.Cancel = true;
                    }
                    else // Если выбранный файл не *.zip формата
                    {
                        if (bgw.WorkerSupportsCancellation == true)
                        {
                            bgw.CancelAsync();
                            this.fE.KillProcess();
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
