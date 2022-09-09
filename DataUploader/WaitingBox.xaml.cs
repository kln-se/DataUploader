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

namespace DataUploader
{
    /// <summary>
    /// Логика взаимодействия для WaitingBox.xaml
    /// </summary>
    public partial class WaitingBox : Window
    {

        // Для выполнения операции извлечения архива в отдельном потоке
        private BackgroundWorker bgw = new BackgroundWorker();

        string filePath; 
        string destinationPath;
        bool IsFailedToExtract;

        public WaitingBox(string filePath, string destinationPath)
        {
                       
            InitializeComponent();          
            uiElementsInitialization();
            InitializeBackgroundWorker();
            StartAsync();

            this.filePath = filePath;
            this.destinationPath = destinationPath;
        }

        private void InitializeBackgroundWorker()
        {
            bgw.WorkerReportsProgress = true;
            bgw.WorkerSupportsCancellation = true;

            bgw.DoWork += new DoWorkEventHandler(BgwDoWork);
            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BgwWorkCompleted);

        }

        private void uiElementsInitialization()
        {
            meWaitingGif.Source = new Uri(Environment.CurrentDirectory + @"\Properties\wait.gif");
            btnRunExplorer.Visibility = Visibility.Hidden;
            btnErrorClose.Visibility = Visibility.Hidden;
            lbExtractStatus.Visibility = Visibility.Hidden;
        }

        private void StartAsync()
        {
            if (bgw.IsBusy != true)
            {
                bgw.RunWorkerAsync();
            }
        }

        private void CancelAsync(object sender, RoutedEventArgs e)
        {
            if (bgw.WorkerSupportsCancellation == true)
            {
                bgw.CancelAsync();
            }
        }

        private void BgwDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            //throw new InvalidOperationException(); //(!)Test
            if (worker.CancellationPending == true)
            {
                e.Cancel = true;
            }
            else
            {
                this.IsFailedToExtract = FileExtension.ExtractArchiveZip(filePath, destinationPath);
            }
        }

        private void BgwWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
            if (e.Cancelled == true)
            {
                this.Close();
            }
            else if (e.Error != null)
            {
                meWaitingGif.Visibility = Visibility.Collapsed;
                lbExtractStatus.Visibility = Visibility.Visible;
                lbExtractStatus.Text = "Ошибка: " + e.Error.Message;
                lbExtractStatus.ToolTip = e.Error.Message;
                btnCancel.Visibility = Visibility.Collapsed;
                btnErrorClose.Visibility = Visibility.Visible;
            }
            else if (IsFailedToExtract == false)
            {
                meWaitingGif.Visibility = Visibility.Collapsed;
                lbExtractStatus.Visibility = Visibility.Visible;
                lbExtractStatus.Text = "Архив извлечён";
                btnCancel.Visibility = Visibility.Collapsed;
                btnRunExplorer.Visibility = Visibility.Visible;
            }
            else
            {
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
    }
}
