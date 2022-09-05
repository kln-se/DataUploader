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
        public MainWindow()
        {
            InitializeComponent();

            string currentDirectory = Directory.GetCurrentDirectory();
            tbDestinationPath.Text = currentDirectory;

            //FolderContent.ListContent(currentDirectory, lbFolderContent);
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
                FileExtension.SetRadioButtonState(fileExtension, rbArchive, rbDtl, rbXls, rbXlsx);
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
                dialog.SelectedPath = currentDirectory;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    tbDestinationPath.Text = dialog.SelectedPath;
                }
            }
        }
    }
}
