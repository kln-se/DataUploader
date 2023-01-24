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
    /// Логика взаимодействия для SubstationTreeForm.xaml
    /// </summary>
    public partial class TreeViewForm : Window
    {
        private DataModel.Node _selectedTreeViewItem = null;
        private string _queryType = null;
        internal string SelectedParentName { get; set; }
        internal string SelectedChildName { get; set; }
        internal int? SelectedParentId { get; set; }
        internal int? SelectedChildId { get; set; }
        internal bool ItemsWereSelected { get; set; }

        internal TreeViewForm(string queryType)
        {
            InitializeComponent();

            btnAddChildItem.IsEnabled = false;
            btnDeleteItem.IsEnabled = false;
            btnApplySelectedItem.IsEnabled = false;
            
            _queryType = queryType;

            ItemsWereSelected = false;
            
            // Когда-то завершится в будущем, должно внезапно построиться дерево
            ConstructTreeAsync();
        }
        
        /// <summary>
        /// Создание дерева в интерфейсе:
        ///     - подстанция 1:
        ///         - трансформатор 1
        ///         - трнасформатор 2
        ///     - подстанция 2:
        ///         - ...
        /// </summary>
        private async void ConstructTreeAsync()
        {
            List<DataModel.Node> parentsList = await DataUploader.DataContext.GetParentsAndChilrenAsync(_queryType);

            if (parentsList != null)
            {
                // Добавляем узлы в TreeView
                foreach (DataModel.Node n in parentsList)
                {
                    treeView.Items.Add(n);
                }
            }
        }
        
        // ----------------------------------------------------------------------------------------
        // Обработка событий
        // ----------------------------------------------------------------------------------------
        
        /// <summary>
        /// Обработка события SelectedItemChanged в элементе TreeView tvSubstations
        /// </summary>
        private void SelectItem(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            btnAddParentItem.IsEnabled = true;
            btnDeleteItem.IsEnabled = true;
            try
            {
                _selectedTreeViewItem = treeView.SelectedItem as DataModel.Node;

                if (_selectedTreeViewItem.Parent == null)
                {
                    btnAddChildItem.IsEnabled = true;
                    tbSelectedItem.Text = _selectedTreeViewItem.Name;
                    btnApplySelectedItem.IsEnabled = false;
                }
                else
                {
                    btnAddChildItem.IsEnabled = false;
                    tbSelectedItem.Text = _selectedTreeViewItem.Parent.Name + " / " + _selectedTreeViewItem.Name;
                    btnApplySelectedItem.IsEnabled = true;
                }
            }
            catch
            {
                btnAddChildItem.IsEnabled = false;
                btnDeleteItem.IsEnabled = false;
                btnApplySelectedItem.IsEnabled = false;
                tbSelectedItem.Text = "Не выбрано";
                SelectedParentName = "Не выбрано";
                SelectedChildName = "Не выбрано";
                SelectedParentId = null;
                SelectedChildId = null;
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnAddRootItem
        /// </summary>
        private async void AddParentItemAsync(object sender, RoutedEventArgs e)
        {
            var ab = new ApplyBox();
            ab.Owner = this;
            var result = ab.ShowDialog();
            
            // false -> окно закрылось, можно считывать поля
            if (result == false)
            {
                string nodeName = ab.TypedNodeName;

                if (nodeName != null)
                {
                    int? insertedNodeId = await DataUploader.DataContext.InsertNodeAsync(_queryType, nodeName);
                    if (insertedNodeId != null)
                    {
                        treeView.Items.Add(new DataModel.Node() { Name = nodeName, Id = insertedNodeId, Parent = null });
                    }
                }
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnAddChildItem
        /// </summary>
        private async void AddChildItemAsync(object sender, RoutedEventArgs e)
        {
            var ab = new ApplyBox();
            ab.Owner = this;
            var result = ab.ShowDialog();

            // false -> окно закрылось, можно считывать поля
            if (result == false)
            {
                string nodeName = ab.TypedNodeName;

                if (nodeName != null)
                {
                    int? insertedNodeId = await DataUploader.DataContext.InsertNodeAsync(_queryType, nodeName, _selectedTreeViewItem.Id);
                    if (insertedNodeId != null)
                    {
                        _selectedTreeViewItem.Childs.Add(new DataModel.Node() { Name = nodeName, Id = insertedNodeId, Parent = _selectedTreeViewItem });
                    }
                }
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnDeleteItem
        /// </summary>
        private async void DeleteItemAsync(object sender, RoutedEventArgs e)
        {
            int? updatedRows = await DataUploader.DataContext.DeleteNodeAsync(_queryType, _selectedTreeViewItem);
            
            if (updatedRows != null)
            {
                // Если это корневой элемент
                if (_selectedTreeViewItem.Parent == null)
                {
                    treeView.Items.Remove(_selectedTreeViewItem);
                }
                // Дочерний элемент
                else
                {
                    _selectedTreeViewItem.Parent.Childs.Remove(_selectedTreeViewItem);
                }
                
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnApplySelectedItem
        /// </summary>
        private void ApplySelectedItem(object sender, RoutedEventArgs e)
        {
            var tvSelectedItem = treeView.SelectedItem as DataModel.Node;

            SelectedParentName = tvSelectedItem.Parent.Name;
            SelectedChildName = tvSelectedItem.Name;
            SelectedParentId = tvSelectedItem.Parent.Id;
            SelectedChildId = tvSelectedItem.Id;
            ItemsWereSelected = true;
            Close();
        }

        /// <summary>
        /// Обработка события Closing в элементе Window класса SubstationTreeForm.
        /// </summary>
        private void FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ItemsWereSelected)
            {
                SelectedParentName = "Не выбрано";
                SelectedChildName = "Не выбрано";
                SelectedParentId = null;
                SelectedChildId = null;
            }
        }
    }
}
