using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataUploader
{
    public class DataModel
    {
        /// <summary>
        ///  Для записи данных об измерениях из *.xlsx файла в БД
        /// </summary>
        public struct ImportedMeasurement
        {
            public DateTime datetime;
            public float value;
            public float? value_min;
            public float? value_max;
            public int field_id;
            public int trans_id;
            public string phase;
            internal ImportedMeasurement(DateTime datetime, float value, float? value_min, float? value_max, int field_id, int trans_id, string phase)
            {
                this.datetime = datetime;
                this.value = value;
                this.value_min = value_min;
                this.value_max = value_max;
                this.field_id = field_id;
                this.trans_id = trans_id;
                this.phase = phase;
            }
        }

        /// <summary>
        ///  Для записи данных об измерениях из *.БД файла в *.xlsx файл
        /// </summary>
        public struct ExportedMeasurement
        {
            public DateTime datetime;
            public int field_id;
            public float value;
            public float? value_min;
            public float? value_max;

            internal ExportedMeasurement(DateTime datetime, int field_id, float value, float? value_min, float? value_max)
            {
                this.datetime = datetime;
                this.field_id = field_id;
                this.value = value;
                this.value_min = value_min;
                this.value_max = value_max;
            }
        }

        /// <summary>
            /// Класс, описывающий древовидную структуру для элемента TreeView tvSubstations
            /// </summary>
        public class Node
        {
            private ObservableCollection<Node> _childNodes = new ObservableCollection<Node>();

            public string Name { get; set; }
            public int? Id { get; set; }
            public Node Parent { get; set; }
            public ObservableCollection<Node> Childs
            {
                get { return _childNodes; }
            }
        }

        /// <summary>
        /// Класс, описывающий структуру данных о загруженном файле для элемента ListView lvUploadedFiles
        /// </summary>
        public class UploadedFile
        {
            public int? Id { get; set; }
            public string DateTime { get; set; }
            public string FileName { get; set; }
            public string Substation { get; set; }
            public string Transformer { get; set; }
            public string Phase { get; set; }
            public string Category { get; set; }
            public string FileType { get; set; }
            public int? AveragingRange { get; set; }
            public int? RecordsNum { get; set; }
        }

        /// <summary>
        /// Класс, описывающий структуру данных о состоянии импорта файла в БД для элемента ListView lvImportedFiles
        /// </summary>
        public class FileImportInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            private string _processStatus = null;
            private string _intersectionsCount = null;
            private string _errorMessage = null;

            public int? Id { get; set; }
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public string ProcessStatus
            {
                get { return _processStatus; }
                set { _processStatus = value; NotifyPropertyChanged(); }
            }
            public string IntersectionsCount
            {
                get { return _intersectionsCount; }
                set { _intersectionsCount = value; NotifyPropertyChanged(); }
            }
            public string ErrorMessage
            {
                get { return _errorMessage; }
                set { _errorMessage = value; NotifyPropertyChanged(); }
            }
        }

        /// <summary>
        /// Класс, описывающий структуру данных о полях, которые можно выбрать при экспорте данных
        /// </summary>
        public class AvailibleFieldsInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            private bool _valueIsChecked = false;
            private bool _valueMinIsChecked = false;
            private bool _valueMaxIsChecked = false;

            private int? _valueIsCheckedOrder = null;
            private int? _valueMinIsCheckedOrder = null;
            private int? _valueMaxIsCheckedOrder = null;

            public int FieldId { get; set; }
            public string FieldName { get; set; }
            public string FileName { get; set; }
            public string CategoryName { get; set; }
            public bool ValueIsChecked
            {
                get { return _valueIsChecked; }
                set { _valueIsChecked = value; NotifyPropertyChanged(); }
            }
            public bool ValueMinIsChecked
            {
                get { return _valueMinIsChecked; }
                set { _valueMinIsChecked = value; NotifyPropertyChanged(); }
            }
            public bool ValueMaxIsChecked
            {
                get { return _valueMaxIsChecked; }
                set { _valueMaxIsChecked = value; NotifyPropertyChanged(); }
            }
            public int? ValueIsCheckedOrder
            {
                get { return _valueIsCheckedOrder; }
                set { _valueIsCheckedOrder = value; NotifyPropertyChanged(); }
            }
            public int? ValueMinIsCheckedOrder
            {
                get { return _valueMinIsCheckedOrder; }
                set { _valueMinIsCheckedOrder = value; NotifyPropertyChanged(); }
            }
            public int? ValueMaxIsCheckedOrder
            {
                get { return _valueMaxIsCheckedOrder; }
                set { _valueMaxIsCheckedOrder = value; NotifyPropertyChanged(); }
            }
        }

        /// <summary>
        /// Класс, описывающий структуру данных для сохранения информации о пресете, полученной из БД 
        /// </summary>
        public class PresetInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
