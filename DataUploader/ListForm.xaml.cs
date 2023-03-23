using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Логика взаимодействия для ListForm.xaml
    /// </summary>
    public partial class ListForm : Window
    {
        private int? _selectedPresetId;
        public int? ChoosenPresetId
        {
            get { return _selectedPresetId; }
        }
        private bool PresetIdWasSelected { get; set; }

        private ObservableCollection<DataModel.PresetInfo> _savedPresetNames = new ObservableCollection<DataModel.PresetInfo>();
        DataModel.PresetInfo _selectedListBoxItem = null;

        public ListForm()
        {
            InitializeComponent();

            btnSelectPresetName.IsEnabled = false;
            btnDeleteItem.IsEnabled = false;

            FillListBoxAsync();
        }

        /// <summary>
        /// Заполняет элемент ListBox lbPresetNames именами сохранённых в БД пресетов
        /// </summary>
        private async void FillListBoxAsync()
        {
            _savedPresetNames = await DataUploader.DataContext.GetFieldsPresetNamesAsync();
            // Привязываем список как источник данных для элемента ListBox lbPresetNames
            lbPresetNames.ItemsSource = _savedPresetNames;
        }

        /// <summary>
        /// Обработка события SelectionChanged в элементе ListBox lbPresetNames
        /// </summary>
        private void SelectPresetName(object sender, SelectionChangedEventArgs e)
        {
            btnSelectPresetName.IsEnabled = true;
            btnDeleteItem.IsEnabled = true;

            _selectedListBoxItem = lbPresetNames.SelectedItem as DataModel.PresetInfo;

        }

        /// <summary>
        /// Обработка события Click в элементе Button btnApplySelectedPresetName
        /// </summary>
        private void ApplySelectedPresetName(object sender, RoutedEventArgs e)
        {
            _selectedPresetId = _selectedListBoxItem.Id;
            PresetIdWasSelected = true;
            Close();
        }

        private async void DeleteSelectedPresetNameAsync(object sender, RoutedEventArgs e)
        {
            int? updatedRows = await DataUploader.DataContext.DeleteSavedPresetAsync(_selectedListBoxItem);
            if(updatedRows != null)
            {
                _savedPresetNames.Remove(_selectedListBoxItem);
                _selectedPresetId = null;
                btnSelectPresetName.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Удаление выбранного элемента из базы данных не выполнено.",
                "Ошибка удаления элемента",
                MessageBoxButton.OK,
                MessageBoxImage.Warning,
                MessageBoxResult.Yes);
            }
        }


        /// <summary>
        /// Обработка события Closing данного окна.
        /// </summary>
        private void FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!PresetIdWasSelected)
            {
                _selectedPresetId = null;
            }
        }
    }
}
