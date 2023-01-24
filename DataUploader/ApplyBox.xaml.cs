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
    /// Логика взаимодействия для ApplyBox.xaml
    /// </summary>
    public partial class ApplyBox : Window
    {

        internal string TypedNodeName { get; set; }
        internal bool NodeNameWasApplied { get; set; }
        public ApplyBox()
        {
            InitializeComponent();
            NodeNameWasApplied = false;
        }

        private void ApplyNodeName(object sender, RoutedEventArgs e)
        {
  
            TypedNodeName = tbTypedNodeName.Text;

            if (tbTypedNodeName.Text == "")
            {
                string messageBoxText = "Введите название узла.";
                string caption = "Ошибка ввода названия узла";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                
                NodeNameWasApplied = false;
            }
            else
            {
                NodeNameWasApplied = true;
                Close();
            }
        }

        private void FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!NodeNameWasApplied)
            {
                TypedNodeName = null;
            }
        }
    }
}
