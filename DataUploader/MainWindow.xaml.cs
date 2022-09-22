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
using System.IO.Compression;
using System.ComponentModel;

namespace DataUploader
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public string filePath;
        public string folderPath;
        public string destinationPath;
        public string selectedCsvEncoding;
        public string selectedFileOrFolder;
        public string choosenInputFileFormat;
        public string choosenOutputFileFormat;
        public string showMilisec;

        public MainWindow()
        {
            InitializeComponent();

            this.filePath = "";
            //this.destinationPath = "D:/Workspaces/VisualStudio/Source/Repos/DataUploader/DataUploader/bin/Debug/test"; // (!)Test
            this.destinationPath = "D:/Sergei/TEMP/temp/test"; // (!)Test
            //this.destinationPath = Directory.GetCurrentDirectory();
            tbDestinationPath.Text = destinationPath;

            // В ComboBox cmbFileOrFolder выбрать "Файл (для обработки единичного файла)"
            cmbFileOrFolder.SelectedItem = cmbFileOrFolder.Items[0];
            // Запрос того, что выбрано в элементе ComboBox cmbFileOrFolder
            this.selectedFileOrFolder = (cmbFileOrFolder.SelectedItem as ComboBoxItem).Content.ToString();
            // При запуске программы надо оставить элементы интерфейса, соотвествующие...
            // выбранному пункту в ComboBox cmbFileOrFolder
            FileExtension.DisableUiElementsFF(this, this.selectedFileOrFolder);

            // При запуске программы файл не выбран, следовательно надо все соотвествующие...
            // ...элементы интерфейса отключить
            FileExtension.DisableUiElements(this, "default", false);
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnBrowseFile "Обзор..."
        /// </summary>
        private void ChooseFileOrFolderDialog(object sender, RoutedEventArgs e)
        {
            // Запрос того, что выбрано в элементе ComboBox cmbFileOrFolder
            this.selectedFileOrFolder = (cmbFileOrFolder.SelectedItem as ComboBoxItem).Content.ToString();
            if (this.selectedFileOrFolder == "Файл (для обработки единичного файла)")
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Filter = "All files (*.*)|*.*" +
                                        "|Excel files (*.xls)|*.xls" +
                                        "|Excel files (*.xlsx)|*.xlsx";
                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    string fileExtension = System.IO.Path.GetExtension(openFileDialog.FileName);

                    // Запись имени файла в TextBox tbFilePath
                    tbFileOrFolderPath.Text = fileName;
                    this.filePath = openFileDialog.FileName;
                    FileExtension.SetRadioButtonState(fileExtension, this);
                }
            }
            else // == "Директория (для пакетной обработки файлов в директории)"
            {
                string currentDirectory = Directory.GetCurrentDirectory();

                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                {
                    dialog.Description = "Выберете директорию для обработки файлов, содержащихся в ней.";
                    dialog.SelectedPath = currentDirectory;
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        tbFileOrFolderPath.Text = dialog.SelectedPath;
                        this.folderPath = dialog.SelectedPath;
                    }
                }
            }

        }

        /// <summary>
        /// Обработка события Click в элементе Button btnBrowseDestination "Обзор..."
        /// </summary>
        private void ChooseDestinationFolderDialog(object sender, RoutedEventArgs e)
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            {
                dialog.Description = "Выберете директорию для сохранения обработанных файлов.";
                dialog.SelectedPath = currentDirectory;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    tbDestinationPath.Text = dialog.SelectedPath;
                    this.destinationPath = dialog.SelectedPath;
                }
            }
        }

        // Обработка событий Checked на RadioButton'ах группы fileFormat
        private void CheckRbArchiveZip(object sender, RoutedEventArgs e)
        {
            this.choosenInputFileFormat = ".zip";
            FileExtension.DisableUiElements(this, this.choosenInputFileFormat, false);
        }

        private void CheckRbArchive7z(object sender, RoutedEventArgs e)
        {
            this.choosenInputFileFormat = ".7z";
            FileExtension.DisableUiElements(this, this.choosenInputFileFormat, false);
        }

        private void CheckRbDtl(object sender, RoutedEventArgs e)
        {
            this.choosenInputFileFormat = ".dtl";
            FileExtension.DisableUiElements(this, this.choosenInputFileFormat, false);
        }

        private void CheckRbXls(object sender, RoutedEventArgs e)
        {
            this.choosenInputFileFormat = ".xls";
            FileExtension.DisableUiElements(this, this.choosenInputFileFormat, false);
        }

        private void CheckRbXlsx(object sender, RoutedEventArgs e)
        {
            this.choosenInputFileFormat = ".xlsx";
            FileExtension.DisableUiElements(this, this.choosenInputFileFormat, false);
        }

        /// <summary>
        /// Обработка события TextChanged в элементе TextBox tbDestinationPath.
        /// Извлекает содержимое архива в папку
        /// </summary>
        private void ChangedByInput(object sender, TextChangedEventArgs e)
        {
            destinationPath = tbDestinationPath.Text;
        }

        // Обработка событий Checked на RadioButton'ах группы outputFileFormat
        private void CheckXlsxFileFormat(object sender, RoutedEventArgs e)
        {
            this.choosenOutputFileFormat = ".xlsx";
        }

        private void CheckXlsFileFormat(object sender, RoutedEventArgs e)
        {
            this.choosenOutputFileFormat = ".xls";
        }

        private void CheckCsvFileFormat(object sender, RoutedEventArgs e)
        {
            this.choosenOutputFileFormat = ".csv";
        }

        // Обработка событий Checked/Unchecked в элементе CheckBox chbCsvShowMilisec
        private void CheckShowMilisec(object sender, RoutedEventArgs e)
        {
            this.showMilisec = "/t1";
        }

        private void UncheckShowMilisec(object sender, RoutedEventArgs e)
        {
            this.showMilisec = "/t0";
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnStartProcess.
        /// Извлекает содержимое архива в папку или запускает процесс конвертирования *.dtl файла.
        /// </summary>
        private void ProcessFileOrFolder(object sender, RoutedEventArgs e)
        {
            // Запрос того, что выбрано в элементе ComboBox cmbFileOrFolder
            this.selectedFileOrFolder = (cmbFileOrFolder.SelectedItem as ComboBoxItem).Content.ToString();

            if (this.selectedFileOrFolder == "Файл (для обработки единичного файла)")
            {
                ProcessFile();
            }
            else // == "Директория (для пакетной обработки файлов в директории)"
            {
                this.destinationPath = "D:/Sergei/TEMP/temp/test/dtls"; // (!)Test
                this.folderPath = "D:/Sergei/TEMP/temp/test/dtls"; // (!)Test

                string[] filesInFolder = Directory.GetFiles(this.folderPath);
                bool dtlFilesNotfound = true;

                for (int i = 0; i < filesInFolder.Length; i++)
                {
                    if (System.IO.Path.GetExtension(filesInFolder[i]) == ".dtl")
                    {
                        dtlFilesNotfound = false;
                        // ToDo

                    }
                }
                if (dtlFilesNotfound)
                {
                    System.Windows.MessageBox.Show("В выранной директории не обнаружено файлов формата *.dtl",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning,
                               MessageBoxResult.Yes);
                }
            }

            /// <summary>
            /// Запуск процесса обработки одного файла
            /// /// </summary>
            void ProcessFile()
            {
                // Запрос того, что выбрано в элементе ComboBox cmbCsvEncoding
                this.selectedCsvEncoding = (cmbCsvEncoding.SelectedItem as ComboBoxItem).Content.ToString();

                if (System.IO.Path.GetExtension(filePath) == this.choosenInputFileFormat)
                {
                    WaitingBox wb = new WaitingBox(this.filePath,
                                                   this.destinationPath,
                                                   this.selectedCsvEncoding,
                                                   this.choosenOutputFileFormat,
                                                   this.showMilisec);
                    wb.ShowDialog();
                    // Удалить экземпляр, т.к. больше не нужен
                    wb = null;
                }
                else
                {
                    System.Windows.MessageBox.Show("Файл не выбран или явное расширение выбранного файла не соответствует формату, выбранному пользователем.",
                                   "Ошибка",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning,
                                   MessageBoxResult.Yes);
                }
            }
        }

        private void FileOrFolderSelected(object sender, SelectionChangedEventArgs e)
        {
            // Запрос того, что выбрано в элементе ComboBox cmbFileOrFolder
            this.selectedFileOrFolder = (cmbFileOrFolder.SelectedItem as ComboBoxItem).Content.ToString();
            FileExtension.DisableUiElementsFF(this, this.selectedFileOrFolder);
        }
    }
}
