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
                tbFilePath.Text = FolderContent.ExtractFileName(openFileDialog.FileName);
                tbTest.Text = FileExtension.DetermineExtension(openFileDialog.FileName);
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnBrowseDestination "Обзор..."
        /// </summary>
        private void ChooseDestinationDialog(object sender, RoutedEventArgs e)
        {

            string currentDirectory = Directory.GetCurrentDirectory();
            /*
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.InitialDirectory = currentDirectory;
            dialog.Title = "Выберете папку";
            dialog.Filter = "Папка|*.this.directory";
            dialog.FileName = ""; // Filename will then be "select.this.directory"
            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                // Remove fake filename from resulting path
                path = path.Replace("\\select.this.directory", "");
                path = path.Replace(".this.directory", "");
                // If user has changed the filename, create the new directory
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                tbDestinationPath.Text = path;
            }*/
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            {
                dialog.SelectedPath = currentDirectory;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            }
        }
    }
}
