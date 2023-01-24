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
    internal class DataModel
    {
        /// <summary>
        ///  Для записи данных об измерениях из БД (таблицы measurements)
        /// </summary>
        internal struct Measurement
        {
            internal DateTime datetime;
            internal float value;
            internal int field_id;
            internal int trans_id;
            internal string phase;
            internal Measurement(DateTime datetime, float value, int field_id, int trans_id, string phase)
            {
                this.datetime = datetime;
                this.value = value;
                this.field_id = field_id;
                this.trans_id = trans_id;
                this.phase = phase;
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
            public int? RecordsNum { get; set; }
        }

        /// <summary>
        /// Класс, описывающий структуру данных о состоянии импорта файла в БД для элемента ListView lvImportedFiles
        /// </summary>
        internal class FileImportInfo : INotifyPropertyChanged
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
    }
}
