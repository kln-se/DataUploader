using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace DataUploader
{
    /// <summary>
    /// Логика взаимодействия для ExportBox.xaml
    /// </summary>
    public partial class ExportForm : Window
    {

        private readonly BackgroundWorker _bgw = new BackgroundWorker();

        // Чтобы засечь сколько прошло времени с момента старта операции
        private System.Timers.Timer _t = null;
        private DateTime _startTime;

        private DateTime _startDate;
        private DateTime _endDate;
        private List<DataModel.AvailibleFieldsInfo> _availibleFieldsInfoList;
        private int _selectedAveragingRange;
        private bool _exportToTemplateIsEnabled;
        private string _exportDestinationPath;
        private string _templateSourcePath;

        private int _selectedSheetNum;
        private int _selectedRowNum;
        private int _selectedColumnNum;

        public bool _exportIsCompleted = false;

        public ExportForm(DateTime startDate,
                         DateTime endDate,
                         int selectedAveragingRange,
                         List<DataModel.AvailibleFieldsInfo> availibleFieldsInfoList,
                         string exportDestinationPath,
                         bool exportToTemplateIsEnabled,
                         string templateSourcePath,
                         int selectedSheetNum,
                         int selectedRowNum,
                         int selectedColumnNum)
        {
            InitializeComponent();

            _startDate = startDate;
            _endDate = endDate;
            _selectedAveragingRange = selectedAveragingRange;
            _availibleFieldsInfoList = availibleFieldsInfoList;
            _exportDestinationPath = exportDestinationPath;
            _exportToTemplateIsEnabled = exportToTemplateIsEnabled;
            _templateSourcePath = templateSourcePath;
            _selectedSheetNum = selectedSheetNum;
            _selectedRowNum = selectedRowNum;
            _selectedColumnNum = selectedColumnNum;

            InitializeUIelements();
            InitializeBackgroundWorker();

            StartAsync();
        }

        private void InitializeUIelements()
        {
            btnRunExplorer.Visibility = Visibility.Hidden;
            lbProcessStatus.Visibility = Visibility.Hidden;
            pbProcessProgress.Value = 0;
        }

        // ----------------------------------------------------------------------------------------
        // Background worker
        // ----------------------------------------------------------------------------------------
        private void InitializeBackgroundWorker()
        {
            _bgw.WorkerReportsProgress = true;
            _bgw.WorkerSupportsCancellation = true;

            _bgw.DoWork += new DoWorkEventHandler(BgwDoWork);
            _bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BgwWorkCompleted);
            _bgw.ProgressChanged += new ProgressChangedEventHandler(ProgressChanged);
        }

        private void StartAsync()
        {
            if (!_bgw.IsBusy)
            {
                _bgw.RunWorkerAsync();
            }
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbProcessProgress.Value = e.ProgressPercentage;
            tbPercentCompleted.Text = ("Выполнено " + pbProcessProgress.Value.ToString() + "%");
        }

        private void BgwDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            // Запуск таймера
            SetTimer();

            worker.ReportProgress(0);

            // Список с экспортированными данными
            List<DataModel.ExportedMeasurement> exportedMeasurements = DataUploader.DataContext.GetDataForExport(_startDate, _endDate, _availibleFieldsInfoList, _selectedAveragingRange);

            // Словарь содержит данные о том, какие поля и в каком порядке отметил пользователь для экспорта
            Dictionary<int, (string, List<(int, string)>)> selectedFieldsInfoDict = CreateSelectedFieldsInfoDict(_availibleFieldsInfoList);

            // Выделим уникальные даты
            var distinctDates = exportedMeasurements.Select(x => x.datetime).Distinct().OrderBy(x => x).ToList();

            try
            {
                // Создадим файл для записи экспортируемых данных
                FileInfo fi = CreateExportFile(_exportToTemplateIsEnabled,
                                               _startDate,
                                               _endDate,
                                               _exportDestinationPath,
                                               _templateSourcePath,
                                               _selectedAveragingRange);


                using (ExcelPackage excelPackage = new ExcelPackage(fi))
                {
                    ExcelWorksheet ws = excelPackage.Workbook.Worksheets[_selectedSheetNum];

                    if (!_exportToTemplateIsEnabled)
                    {
                        // Запишем названия всех колонок
                        ws.Cells[_selectedRowNum, _selectedColumnNum].Value = "Дата";
                        ws.Cells[_selectedRowNum, _selectedColumnNum].Style.Font.Bold = true;
                        ws.Column(_selectedColumnNum).Width = 18;
                        for (int i = 1; i <= selectedFieldsInfoDict.Count; i++)
                        {
                            (string, List<(int, string)>) tempDictElement = selectedFieldsInfoDict.Values.ElementAt(i - 1);
                            for (int j = 1; j <= tempDictElement.Item2.Count; j++)
                            {
                                string tempFieldName = null;
                                switch (tempDictElement.Item2[j - 1].Item2)
                                {
                                    case "value":
                                        if (_selectedAveragingRange == 0) { tempFieldName = String.Format("{0}", tempDictElement.Item1); }
                                        else { tempFieldName = String.Format("{0} [{1}]", tempDictElement.Item1, "avg"); }
                                        break;
                                    case "value_min":
                                        tempFieldName = String.Format("{0} [{1}]", tempDictElement.Item1, "min");
                                        break;
                                    case "value_max":
                                        tempFieldName = String.Format("{0} [{1}]", tempDictElement.Item1, "max");
                                        break;
                                }
                                
                                ws.Cells[_selectedRowNum, _selectedColumnNum + tempDictElement.Item2[j - 1].Item1].Value = tempFieldName;
                                ws.Cells[_selectedRowNum, _selectedColumnNum + tempDictElement.Item2[j - 1].Item1].Style.Font.Bold = true;
                                ws.Column(_selectedColumnNum + tempDictElement.Item2[j - 1].Item1).Width = tempFieldName.Length;
                            }
                        }
                    }

                    // Сначала запишем столбец "Дата" со всеми уникальными датами
                    for (int i = 1; i <= distinctDates.Count; i++)
                    {
                        ws.Cells[_selectedRowNum + i, _selectedColumnNum].Value = distinctDates[i - 1].ToString("yyyy.MM.dd HH:mm:ss");
                        
                        // Проверка запроса отмены операции
                        if (worker.CancellationPending == true)
                        {
                            e.Cancel = true;
                            break;
                        }
                    }

                    // Затем записываем данные
                    for (int i = 0; i < exportedMeasurements.Count; i++)
                    {
                        for (int j = 0; j < distinctDates.Count; j++)
                        {
                            if (exportedMeasurements[i].datetime == distinctDates[j])
                            {
                                List<(int, string)> tempPair = selectedFieldsInfoDict[exportedMeasurements[i].field_id].Item2;
                                for (int k = 0; k < tempPair.Count; k++)
                                {
                                    var tempValue = typeof(DataModel.ExportedMeasurement).GetField(tempPair[k].Item2).GetValue(exportedMeasurements[i]);
                                    ws.Cells[_selectedRowNum + (j + 1), _selectedColumnNum + tempPair[k].Item1].Value = tempValue;
                                }
                                break;
                            }
                        }
                        worker.ReportProgress((int)((float)(i + 1) / exportedMeasurements.Count * 100));
                        if (i % 1000 == 0)
                        {
                            // Обновление элемента TextBlock tbRowsLeft 
                            DelegateShowRowsLeft(String.Format("Осталось строк: {0} из {1}", exportedMeasurements.Count - i - 1, exportedMeasurements.Count));
                        }
                        else if (i == exportedMeasurements.Count - 1)
                        {
                            // Обновление элемента TextBlock tbRowsLeft 
                            DelegateShowRowsLeft(String.Format("Осталось строк: {0} из {1}", exportedMeasurements.Count - i - 1, exportedMeasurements.Count));
                        }

                        // Проверка запроса отмены операции
                        if (worker.CancellationPending == true)
                        {
                            e.Cancel = true;
                            break;
                        }
                    }

                    excelPackage.SaveAs(fi);

                    _exportIsCompleted = true;
                }      
            }
            catch (Exception ex)
            {
                string messageBoxText = string.Format("{0}\n{1}\n{2}", ex.Message, ex.InnerException, ex.StackTrace);
                string caption = "Ошибка записи в файл";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
            }
        }

        private void BgwWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (e.Cancelled)
            {
                _t.Stop();
                _t.Dispose();
                Close();
            }
            else if (e.Error != null) // Если ошибка в BackgroundWorker
            {
                _t.Stop();
                _t.Dispose();

                string messageBoxText = string.Format("{0}\n{1}\n{2}", e.Error.Message, e.Error.InnerException, e.Error.StackTrace);
                string caption = "Ошибка";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                Close();
            }
            else if (_exportIsCompleted == true)
            {
                _t.Stop();
                _t.Dispose();

                lbProcessStatus.Visibility = Visibility.Visible;
                lbProcessStatus.Text = "Операция завершена";
                btnCancel.Visibility = Visibility.Collapsed;
                btnRunExplorer.Visibility = Visibility.Visible;

                tbPercentCompleted.Visibility = Visibility.Hidden;
                pbProcessProgress.Visibility = Visibility.Hidden;
                tbFileInProcess.Visibility = Visibility.Hidden;
                tbRowsLeft.Visibility = Visibility.Hidden;
                tbTimePassed.Visibility = Visibility.Hidden;
            }
            else
            {
                _t.Stop();
                _t.Dispose();
                Close();
            }
        }

        // ----------------------------------------------------------------------------------------
        // Обработка событий формы
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Обработка события Click в Button btnCancel.
        /// Отмена операции импорта.
        /// </summary>
        private void CancelOperation(object sender, RoutedEventArgs e)
        {
            if (_bgw.WorkerSupportsCancellation == true)
            {
                _bgw.CancelAsync();
            }
        }

        /// <summary>
        /// Обработка события Click в Button btnRunExplorer.
        /// </summary>
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", _exportDestinationPath);
            Close();
        }

        /// <summary>
        /// Обработка события Closing в элементе Window класса ProgressBarForm.
        /// </summary>
        private void FormClosing(object sender, CancelEventArgs e)
        {
            if (_bgw.IsBusy)
            {
                var r = MessageBox.Show("Вы уверены, что хотите прервать выполнение операции?",
                                        "Прервать операцию",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Warning,
                                        MessageBoxResult.Yes);

                if (r == MessageBoxResult.Yes)
                {
                    if (_bgw.WorkerSupportsCancellation == true)
                    {
                        _bgw.CancelAsync();
                    }
                }
                else // MessageBoxResult.No
                {
                    // Отменить закрытие окна
                    e.Cancel = true;
                }
            }
        }

        // ----------------------------------------------------------------------------------------
        // Вспомогательные функции
        // ----------------------------------------------------------------------------------------

        // Объявление делегата (указателя на метод) с именем InvokeDelegate - может указывать...
        // ...на любой метод, который, возвращает void и принимает входной параметр типа string
        private delegate void InvokeDelegate(string showString);

        // Объявление метода, на который будет указывать делегат
        private void DelegateShowTime(string time)
        {

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new InvokeDelegate(DelegateShowTime), time);
                return;
            }
            tbTimePassed.Text = time;
        }

        private void DelegateShowResultingFile(string resultingFile)
        {
            if (!Dispatcher.CheckAccess())
            {
                // Создание делегата InvokeDelegate и чтобы он указывал на DelegateShowCurrentFile
                InvokeDelegate invokeDelegate = new InvokeDelegate(DelegateShowResultingFile);

                Dispatcher.Invoke(invokeDelegate, resultingFile);
                return;
            }
            tbFileInProcess.Text = "Имя: " + resultingFile;
        }

        private void DelegateShowRowsLeft(string rowsLeft)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new InvokeDelegate(DelegateShowRowsLeft), rowsLeft);
                return;
            }
            tbRowsLeft.Text = string.Format("{0}", rowsLeft);
        }

        // Событие обновления текстоваго поля, показывающее значение таймера
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // Обновление TextBlock tbTimePassed из другого потока
            DelegateShowTime(string.Format("{0:hh\\:mm\\:ss}", (e.SignalTime - _startTime)));
        }

        private void SetTimer()
        {
            _startTime = DateTime.Now;

            // Интервал 1000 мс
            _t = new System.Timers.Timer(1000);

            _t.Elapsed += OnTimedEvent;
            _t.AutoReset = true;
            _t.Enabled = true;
        }

        /// <summary>
        /// Возвращает структуру, которая содержит какие поля были выбраны пользователем
        /// (id поля, выбрано avg/min/max, каким по счету было выбрано поле).
        /// </summary>
        public static Dictionary<int, (string, List<(int, string)>)> CreateSelectedFieldsInfoDict(List<DataModel.AvailibleFieldsInfo> availibleFieldsInfoList)
        {
            // Dictionary<id_поля, (Имя_поля, List<(порядковый_номер_выбора, чекбокс_avg_min_max)>)>
            var selectedFieldsInfoDict = new Dictionary<int, (string, List<(int, string)>)>();
            (int, string) tempPair;

            foreach (DataModel.AvailibleFieldsInfo afi in availibleFieldsInfoList)
            {
                if (afi.ValueIsChecked)
                {
                    tempPair = ((int)afi.ValueIsCheckedOrder, "value");
                    if (!selectedFieldsInfoDict.ContainsKey(afi.FieldId))
                    {
                        selectedFieldsInfoDict.Add(afi.FieldId, (afi.FieldName, new List<(int, string)>() { tempPair }));
                    }
                    else
                    {
                        selectedFieldsInfoDict[afi.FieldId].Item2.Add(tempPair);
                    }
                }
                if (afi.ValueMinIsChecked)
                {
                    tempPair = ((int)afi.ValueMinIsCheckedOrder, "value_min");
                    if (!selectedFieldsInfoDict.ContainsKey(afi.FieldId))
                    {
                        selectedFieldsInfoDict.Add(afi.FieldId, (afi.FieldName, new List<(int, string)>() { tempPair }));
                    }
                    else
                    {
                        selectedFieldsInfoDict[afi.FieldId].Item2.Add(tempPair);
                    }
                }
                if (afi.ValueMaxIsChecked)
                {
                    tempPair = ((int)afi.ValueMaxIsCheckedOrder, "value_max");
                    if (!selectedFieldsInfoDict.ContainsKey(afi.FieldId))
                    {
                        selectedFieldsInfoDict.Add(afi.FieldId, (afi.FieldName, new List<(int, string)>() { tempPair }));
                    }
                    else
                    {
                        selectedFieldsInfoDict[afi.FieldId].Item2.Add(tempPair);
                    }
                }
            }
            return selectedFieldsInfoDict;
        }

        /// <summary>
        /// Создает файл в формате *.xlsx куда буду записаны экспортируемые данные.
        /// </summary>
        private FileInfo CreateExportFile(bool exportToTemplateIsEnabled,
                                          DateTime startDate,
                                          DateTime endDate,
                                          string exportDestinationPath,
                                          string templateSourcePath,
                                          int selectedAveragingRange,
                                          int selectedSheetNum = 1,
                                          int selectedRowNum = 1,
                                          int selectedColumnNum = 1)
        {
            FileInfo fi = null;
            string resultingFileName = null;

            if (exportToTemplateIsEnabled && selectedAveragingRange != 0)
            {
                resultingFileName = String.Format("{0:yyyy\\.MM\\.dd} - {1:yyyy\\.MM\\.dd} {2:yyyy\\.MM\\.dd\\ HH\\-mm\\-ss} [AVG{3}] [template].xlsx",
                                                  startDate,
                                                  endDate,
                                                  DateTime.Now,
                                                  selectedAveragingRange);
            }
            else if (exportToTemplateIsEnabled && selectedAveragingRange == 0)
            {
                resultingFileName = String.Format("{0:yyyy\\.MM\\.dd} - {1:yyyy\\.MM\\.dd} {2:yyyy\\.MM\\.dd\\ HH\\-mm\\-ss} [RAW] [template].xlsx",
                                                  startDate,
                                                  endDate,
                                                  DateTime.Now);
            }
            else if (!exportToTemplateIsEnabled && selectedAveragingRange != 0)
            {
                resultingFileName = String.Format("{0:yyyy\\.MM\\.dd} - {1:yyyy\\.MM\\.dd} {2:yyyy\\.MM\\.dd\\ HH\\-mm\\-ss} [AVG{3}].xlsx",
                                                  startDate,
                                                  endDate,
                                                  DateTime.Now,
                                                  selectedAveragingRange);
            }
            else if (!exportToTemplateIsEnabled && selectedAveragingRange == 0)
            {
                resultingFileName = String.Format("{0:yyyy\\.MM\\.dd} - {1:yyyy\\.MM\\.dd} {2:yyyy\\.MM\\.dd\\ HH\\-mm\\-ss} [RAW].xlsx",
                                                  startDate,
                                                  endDate,
                                                  DateTime.Now);
            }

            // Запись в шаблонный *.xlsx файл
            if (exportToTemplateIsEnabled)
            {
                // Копируем шаблон в папку /temp
                File.Copy(templateSourcePath, Path.Combine(exportDestinationPath, Path.GetFileName(templateSourcePath)), true);
                fi = new FileInfo(Path.Combine(exportDestinationPath, Path.GetFileName(templateSourcePath)));
                // Переименовываем скопированный файл
                fi.MoveTo(Path.Combine(exportDestinationPath, resultingFileName));

                // Обновление элемента TextBlock tbFileInProcess 
                DelegateShowResultingFile(String.Format("{0} (в шаблон \"{1}\")", resultingFileName, Path.GetFileName(templateSourcePath)));
            }
            // Создаем новый *.xlsx файл
            else
            {
                fi = new FileInfo(Path.Combine(exportDestinationPath, resultingFileName));
                using (ExcelPackage excelPackage = new ExcelPackage(fi))
                {
                    excelPackage.Workbook.Worksheets.Add("Лист 1");
                    excelPackage.SaveAs(fi);
                }

                // Обновление элемента TextBlock tbFileInProcess 
                DelegateShowResultingFile(resultingFileName);

                _selectedSheetNum = selectedSheetNum;
                _selectedRowNum = selectedRowNum;
                _selectedColumnNum = selectedColumnNum;
            }
            return fi;
        }
    }
}
