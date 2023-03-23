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

namespace DataUploader
{
    /// <summary>
    /// Логика взаимодействия для TypeNameBox.xaml
    /// </summary>
    public partial class TypeNameForm : Window
    {
        private string _typedName;
        public string TypedName
        {
            get { return _typedName; }
        }
        private bool NameWasApplied { get; set; }
        public TypeNameForm()
        {
            InitializeComponent();
            NameWasApplied = false;
        }

        private void ApplyNodeName(object sender, RoutedEventArgs e)
        {

            _typedName = tbTypedNodeName.Text;

            if (tbTypedNodeName.Text == "")
            {
                string messageBoxText = "Введите название!";
                string caption = "Ошибка ввода названия элемента";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                
                NameWasApplied = false;
            }
            else
            {
                NameWasApplied = true;
                Close();
            }
        }

        private void FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!NameWasApplied)
            {
                _typedName = null;
            }
        }
    }
}
