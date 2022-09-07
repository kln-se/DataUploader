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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Forms;

namespace DataUploader
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        string filePath = "";
        
        public MainWindow()
        {
            InitializeComponent();

            //string destinationPath = Directory.GetCurrentDirectory();
            string destinationPath = "D:/Sergei/TEMP/temp/test"; // (!)Test
            //string destinationPath = currentDirectory;

            tbDestinationPath.Text = destinationPath;

            //tbTest.Text = mW.ActualWidth.ToString(); // (!)Test
            //FolderContent.ListContent(currentDirectory, lbFolderContent); // (!)Test
            //System.Windows.MessageBox.Show(tbFilePath.Text); // (!)Test

            // При запуске программы файл не выбран, следовательно надо все соотвествующие...
            // ...элементы интерфейса отключить
            FileExtension.DisableUiElements(this, "default", false);
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnBrowseFile "Обзор..."
        /// </summary>
        private void ChooseFileDialog(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "All files (*.*)|*.*" +
                                    "|Excel files (*.xls)|*.xls" +
                                    "|Excel files (*.xlsx)|*.xlsx";
            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = FolderContent.ExtractFileName(openFileDialog.FileName);
                string fileExtension = FileExtension.DetermineExtension(openFileDialog.FileName);

                // Запись имени файла в TextBox tbFilePath
                tbFilePath.Text = fileName;
                FileExtension.SetRadioButtonState(fileExtension, this);
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnBrowseDestination "Обзор..."
        /// </summary>
        private void ChooseFolderDialog(object sender, RoutedEventArgs e)
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            {
                dialog.Description = "Выберете директорию для извлечения архива.";
                dialog.SelectedPath = currentDirectory;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    tbDestinationPath.Text = dialog.SelectedPath;
                }
            }
        }

        // Обработка событий Checked на RadioButton'ах
        private void CheckRbArchiveZip(object sender, RoutedEventArgs e)
        {
            FileExtension.DisableUiElements(this, ".zip", false);
        }

        private void CheckRbArchive7z(object sender, RoutedEventArgs e)
        {
            FileExtension.DisableUiElements(this, ".7z", false);
        }

        private void CheckRbDtl(object sender, RoutedEventArgs e)
        {
            FileExtension.DisableUiElements(this, ".dtl", false);
        }

        private void CheckRbXls(object sender, RoutedEventArgs e)
        {
            FileExtension.DisableUiElements(this, ".xls", false);
        }

        private void CheckRbXlsx(object sender, RoutedEventArgs e)
        {
            FileExtension.DisableUiElements(this, ".xlsx", false);
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnExtract.
        /// Извлекает содержимое архива в папку
        /// </summary>
        private void ExtractArchive(object sender, RoutedEventArgs e)
        {
            if (filePath == "")
            {
                FileExtension.DisableUiElements(this, "default", false);
            }
            //public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName);
        }
    }
}
