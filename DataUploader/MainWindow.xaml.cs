using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DataUploader
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public string filePath = "";
        public string folderPath = "";
        public string destinationPath;
        private string _selectedFileOrFolder;
        private string _selectedCsvEncoding;
        private int _selectedAveragingRange;
        private string _choosenInputFileFormat;
        private string _choosenOutputFileFormat;
        private string _showMilisec = "/t0";
        private bool _averagingIsEnabled;
        private bool _overwriteIsEnabled;
        private bool _exportToTemplateIsEnabled;

        private int? _selectedCategoryId = null;
        private int? _selectedFileId = null;
        private int? _selectedSubstationId = null;
        private int? _selectedTransformerId = null;
        private string _selectedPhase = null;

        private DateTime? _startDate = null;
        private DateTime? _endDate = null;
        private DateTime? _approvedStartDate = null;
        private DateTime? _approvedEndDate = null;
        public string exportDestinationPath;
        private DataModel.Node _selectedTreeViewItem = null;
        public string templateSourcePath;
        private int? _selectedAvailibleAveragingRange;
        private List<DataModel.AvailibleFieldsInfo> _availibleFieldsInfoList = null;
        private int _selectedSheetNum;
        private int _selectedRowNum;
        private int _selectedColumnNum;
        private int _fieldsOrderCounter = 0;
        private DataGridCellInfo _activeCheckBoxCellAtEdit;

        private bool _sourceWasSelected = false;
        private bool _substationWasSelected = false;
        private bool _categoryWasSelected = false;

        //HACK [#1] Для ликвидации двойного срабатывания события
        private bool __resetIntervalDatesWasFired = false;

        public MainWindow()
        {
            InitializeComponent();

            destinationPath = Directory.GetCurrentDirectory() + "\\temp";
            exportDestinationPath = Directory.GetCurrentDirectory() + "\\temp";
            templateSourcePath = Directory.GetCurrentDirectory() + "\\temp\\template\\template.xlsx";

            tbDestinationPath.Text = destinationPath;
            tbExportDestinationPath.Text = exportDestinationPath;
            tbTemplatePath.Text = templateSourcePath;

            // В ComboBox cmbFileOrFolder выбрать "Файл (для обработки единичного файла *.zip, *.7z, *.dtl, *.xlsx)"
            cmbFileOrFolder.SelectedItem = cmbFileOrFolder.Items[0];
            cmbCsvEncoding.SelectedItem = cmbCsvEncoding.Items[0];
            cmbPhase.SelectedItem = cmbPhase.Items[0];

            chbUploadDataWithAveraging.IsChecked = true;
            chbOverwriteData.IsChecked = true;
            chbExportToTemplate.IsChecked = true;

            tbSheetNum.Text = "3";
            tbRowNum.Text = "1";
            tbColumnNum.Text = "1";

            // При запуске программы надо оставить элементы интерфейса, соотвествующие...
            // ...выбранному пункту в ComboBox cmbFileOrFolder
            DisableUiElementsFF(_selectedFileOrFolder);

            // При запуске программы файл не выбран, следовательно надо все соотвествующие...
            // ...элементы интерфейса отключить
            DisableUiElements("default", false);

            // Считать настройки подключения из App.cpnfig
            InitializeAppConfigSettings();

            // Заполняет журнал информацией о загруженных в БД файлах
            ConstructUploadedFilesLogAsync();
        }

        private void InitializeAppConfigSettings()
        {
            try { cmbAveragingRange.SelectedItem = cmbAveragingRange.Items[Convert.ToInt32(AppConfig.GetAveragingRange())]; }
            catch { cmbAveragingRange.SelectedItem = cmbAveragingRange.Items[6]; }

            string[] connectionSettings = AppConfig.GetSettings();

            tbServerIP.Text = connectionSettings[0];
            tbServerPort.Text = connectionSettings[1];
            tbDatabaseName.Text = connectionSettings[2];
            tbLogin.Text = connectionSettings[3];
            tbPassword.Password = connectionSettings[4];

            tbServerIP.Background = Brushes.White;
            tbServerPort.Background = Brushes.White;
            tbDatabaseName.Background = Brushes.White;
            tbLogin.Background = Brushes.White;
            tbPassword.Background = Brushes.White;

            btnTestConnection.IsEnabled = true;
        }

        /// <summary>
        /// В зависимости от расширения файла "закрашивает" тот или иной RadioButton.
        /// </summary>
        private void SetRadioButtonState(string fileExtension)
        {
            switch (fileExtension)
            {
                case ".zip":
                    _choosenInputFileFormat = ".zip";
                    rbArchive.IsChecked = true;
                    DisableUiElements(_choosenInputFileFormat, false);
                    break;

                case ".7z":
                    _choosenInputFileFormat = ".7z";
                    rbArchive.IsChecked = true;
                    DisableUiElements(_choosenInputFileFormat, false);
                    break;

                case ".dtl":
                    _choosenInputFileFormat = ".dtl";
                    rbDtl.IsChecked = true;
                    DisableUiElements(_choosenInputFileFormat, false);
                    break;

                case ".xlsx":
                    _choosenInputFileFormat = ".xlsx";
                    rbXlsx.IsChecked = true;
                    DisableUiElements(_choosenInputFileFormat, false);
                    break;

                default:
                    rbArchive.IsChecked = false;
                    rbDtl.IsChecked = false;
                    rbXlsx.IsChecked = false;

                    // Когда пользователь выбирает файл, который программа не может определить...
                    // ...необходимо элементы интерфейса отключить
                    DisableUiElements("default", false);
                    break;
            }
        }

        /// <summary>
        /// В зависимости от расширения файла отключает элементы интерфейса.
        /// </summary>
        private void DisableUiElements(string fileExtension, bool enableUiElements)
        {
            // Сначала всё включим
            EnableAllUiElements();

            // Затем нужное отключим
            Control[] uiGroupToBeDisabled = ReturnUiGroupToBeDisabled();
            for (int i = 0; i < uiGroupToBeDisabled.Length; i++)
            {
                uiGroupToBeDisabled[i].IsEnabled = enableUiElements;
            }

            /// <summary>
            /// В зависимости от расширения файла возвращает группу элементов интерфейса, которую
            /// надо сделать неактивной
            /// </summary>
            Control[] ReturnUiGroupToBeDisabled()
            {
                switch (fileExtension)
                {
                    case ".zip":
                        imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/unpack_96.png"));
                        btnStartProcess.ToolTip = "Извлечь содержимое *.zip архива.";
                        btnImportToDB.ToolTip = "Загрузить содержимое *.dtl файлов из *.zip архива в базу данных.";
                        // Group 1
                        return new Control[5] {rbXlsxFileFormat,
                                               rbXlsFileFormat,
                                               rbCsvFileFormat,
                                               cmbCsvEncoding,
                                               chbCsvShowMilisec};
                    case ".7z":
                        imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/unpack_96.png"));
                        btnStartProcess.ToolTip = "Извлечь содержимое *.7z архива.";
                        btnImportToDB.ToolTip = "Загрузить содержимое *.dtl файлов из *.7z архива в базу данных.";
                        // Group 2
                        return new Control[5] {rbXlsxFileFormat,
                                               rbXlsFileFormat,
                                               rbCsvFileFormat,
                                               cmbCsvEncoding,
                                               chbCsvShowMilisec};

                    case ".dtl":
                        imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/convert_96.png"));
                        btnStartProcess.ToolTip = "Конверитровать выбранный *.dtl файл.";
                        btnImportToDB.ToolTip = "Загрузить содержимое *.dtl файла в базу данных.";
                        // Group 3
                        return new Control[0] { };

                    case ".xlsx":
                        btnImportToDB.ToolTip = "Загрузить содержимое *.xlsx файла в базу данных.";
                        // Group 4
                        return new Control[8] {tbDestinationPath,
                                               btnBrowseDestination,
                                               btnStartProcess,
                                               rbXlsxFileFormat,
                                               rbXlsFileFormat,
                                               rbCsvFileFormat,
                                               cmbCsvEncoding,
                                               chbCsvShowMilisec};
                    default:
                        imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/unpack_96.png"));
                        btnStartProcess.ToolTip = "Операция не определена. Файл не выбран или имеет недопустимое расширение.";
                        btnImportToDB.ToolTip = "";
                        // Group 5
                        return new Control[8] {rbXlsxFileFormat,
                                               rbXlsFileFormat,
                                               rbCsvFileFormat,
                                               cmbCsvEncoding,
                                               chbCsvShowMilisec,
                                               btnImportToDB,
                                               btnLoadFieldsPreset,
                                               btnSaveFieldsPreset};
                }
            }

            /// <summary>
            /// Возвращает группу элементов интерфейса, которую надо сделать активной
            /// (сначала включить все UI элементы, а потом уже нужные отключить)
            /// </summary>
            void EnableAllUiElements()
            {
                // Group 7
                Control[] uiGroupToBeEnabled = new Control[] {tbDestinationPath,
                                                              btnBrowseDestination,
                                                              btnStartProcess,
                                                              rbXlsxFileFormat,
                                                              rbXlsFileFormat,
                                                              rbCsvFileFormat,
                                                              cmbCsvEncoding,
                                                              chbCsvShowMilisec};

                for (int i = 0; i < uiGroupToBeEnabled.Length; i++)
                {
                    uiGroupToBeEnabled[i].IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// В зависимости от того, что выбрано (File или Folder) в ComboBox cmbFileOrFolder
        /// отключает элементы интерфейса.
        /// </summary>
        private void DisableUiElementsFF(string comboBoxSelection)
        {
            // Сначала всё включим
            EnableAllUiElements();

            // Затем нужное отключим
            Control[] uiGroupToBeDisabled = ReturnUiGroupToBeDisabled();
            for (int i = 0; i < uiGroupToBeDisabled.Length; i++)
            {
                uiGroupToBeDisabled[i].IsEnabled = false;
            }

            /// <summary>
            /// В зависимости от расширения файла возвращает группу элементов интерфейса, которую
            /// надо сделать неактивной
            /// </summary>
            Control[] ReturnUiGroupToBeDisabled()
            {
                switch (comboBoxSelection)
                {
                    case "file":
                        tbFileOrFolderPath.Text = "Файл не выбран";
                        filePath = "";
                        btnImportToDB.ToolTip = "Загрузить содержимое выбранного файла в базу данных.";

                        rbArchive.IsChecked = false;
                        rbDtl.IsChecked = false;
                        rbXlsx.IsChecked = false;
                        chbOverwriteData.IsEnabled = false;

                        DisableUiElements("default", false);

                        // Group 1
                        return new Control[0] { };

                    case "folder":
                        tbFileOrFolderPath.Text = "Директория не выбрана";
                        filePath = "";
                        btnImportToDB.ToolTip = "Загрузить содержимое *.dtl файлов из выбранной директории в базу данных.";

                        imgExtractBtn.Source = new BitmapImage(new Uri("pack://application:,,,/Properties/convert_96.png"));

                        rbArchive.IsChecked = false;
                        rbDtl.IsChecked = true;
                        rbXlsx.IsChecked = false;
                        chbOverwriteData.IsEnabled = true;

                        btnStartProcess.ToolTip = "Начать пакетное конвертирование *.dtl файлов.";

                        rbXlsxFileFormat.IsEnabled = true;
                        rbXlsFileFormat.IsEnabled = true;
                        rbCsvFileFormat.IsEnabled = true;
                        cmbCsvEncoding.IsEnabled = true;
                        chbCsvShowMilisec.IsEnabled = true;

                        btnImportToDB.IsEnabled = false;

                        // Group 2
                        return new Control[3] {rbArchive,
                                               rbDtl,
                                               rbXlsx};
                    default:
                        // Group 6
                        return new Control[0] { };
                }
            }

            /// <summary>
            /// Возвращает группу элементов интерфейса, которую надо сделать активной
            /// (сначала включить все UI элементы, а потом уже нужные отключить)
            /// </summary>
            void EnableAllUiElements()
            {
                // Group 7
                Control[] uiGroupToBeEnabled = new Control[] {rbArchive,
                                                              rbDtl,
                                                              rbXlsx};

                for (int i = 0; i < uiGroupToBeEnabled.Length; i++)
                {
                    uiGroupToBeEnabled[i].IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Делвет кнопку btnImportToDB активной если выбран файл/папка (источни), а также
        /// подстанция и трансформатор, к которым относится источник.
        /// </summary>
        private void Enable_btnImportToDB(bool sourceWasSelected, bool itemsWereSelected, bool categoryWasSelected)
        {
            if (sourceWasSelected && itemsWereSelected && categoryWasSelected)
            {
                btnImportToDB.IsEnabled = true;
            }
            else
            {
                btnImportToDB.IsEnabled = false;
            }
        }

        /// <summary>
        /// Функция заполняет список lvUploadedFiles загруженными в БД файлами (на вкладке "Журнал") 
        /// </summary>
        private async void ConstructUploadedFilesLogAsync()
        {
            List<DataModel.UploadedFile> uploadedFilesList = await DataUploader.DataContext.GetUploadedFilesAsync();

            if (uploadedFilesList != null)
            {
                // Очистим список
                lvUploadedFiles.Items.Clear();

                // Добавляем строки в журнал файлов, загруженных в БД
                foreach (DataModel.UploadedFile uf in uploadedFilesList)
                {
                    lvUploadedFiles.Items.Add(uf);
                }
            }
        }

        /// <summary>
        /// Создание дерева в интерфейсе:
        ///     - подстанция 1:
        ///         - трансформатор 1
        ///             - фаза
        ///             - фаза
        ///     - ...
        /// Функция заполняет элемент TreeView tvSubstTransPhase доступными данными
        /// из таблицы measurements в БД (на вкладке "Экспорт данных") 
        /// </summary>
        private async void ConstructSubstTransPhaseTreeAsync(DateTime startDate, DateTime endDate)
        {
            List<DataModel.Node> availibleSbstTransPhaseList = await DataUploader.DataContext.GetSubstTransPhaseAsync(startDate, endDate);

            if (availibleSbstTransPhaseList != null)
            {
                // Очистим список
                tvSubstTransPhase.Items.Clear();

                // Добавляем строки в журнал файлов, загруженных в БД
                foreach (DataModel.Node n in availibleSbstTransPhaseList)
                {
                    tvSubstTransPhase.Items.Add(n);
                }
            }
            else
            {
                tvSubstTransPhase.Items.Clear();
            }
        }

        // ----------------------------------------------------------------------------------------
        // Обработка событий
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Обработка события Click в элементе Button btnBrowseFile "Обзор...".
        /// </summary>
        private void ChooseFileOrFolderDialog(object sender, RoutedEventArgs e)
        {
            _sourceWasSelected = false; // сброс значения
            Enable_btnImportToDB(_sourceWasSelected, _substationWasSelected, _categoryWasSelected);

            if (_selectedFileOrFolder == "file")
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Default|*.zip;*.7z;*.xlsx;*.dtl|" +
                             "Archives|*.zip;*.7z|" +
                             "All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileExtension = Path.GetExtension(openFileDialog.FileName);

                    SetRadioButtonState(fileExtension);

                    // Запись имени файла в TextBox tbFilePath
                    tbFileOrFolderPath.Text = openFileDialog.FileName;
                    filePath = openFileDialog.FileName;

                    if (fileExtension != "")
                    {
                        _sourceWasSelected = true;
                    }

                    Enable_btnImportToDB(_sourceWasSelected, _substationWasSelected, _categoryWasSelected);
                }
                else
                {
                    tbFileOrFolderPath.Text = "Файл не выбран";
                    _sourceWasSelected = false;
                    Enable_btnImportToDB(_sourceWasSelected, _substationWasSelected, _categoryWasSelected);
                }
            }
            else // == "Директория (для пакетной обработки файлов в директории)"
            {
                VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog
                {
                    SelectedPath = "",
                    ShowNewFolderButton = true,

                };
                if (folderBrowserDialog.ShowDialog() == true)
                {
                    folderPath = folderBrowserDialog.SelectedPath;
                    tbFileOrFolderPath.Text = folderBrowserDialog.SelectedPath;

                    _sourceWasSelected = true;
                    Enable_btnImportToDB(_sourceWasSelected, _substationWasSelected, _categoryWasSelected);
                }
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnBrowseDestination "Обзор...".
        /// </summary>
        private void ChooseDestinationFolderDialog(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog
            {
                SelectedPath = "",
                ShowNewFolderButton = true,
            };
            if (folderBrowserDialog.ShowDialog() == true)
            {
                destinationPath = folderBrowserDialog.SelectedPath;
                tbDestinationPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnBrowseExportDestination "Обзор...".
        /// </summary>
        private void ChooseExportDestinationFolderDialog(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog
            {
                SelectedPath = "",
                ShowNewFolderButton = true,
            };
            if (folderBrowserDialog.ShowDialog() == true)
            {
                exportDestinationPath = folderBrowserDialog.SelectedPath;
                tbExportDestinationPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnBrowseTemplateDestination "Обзор...".
        /// </summary>
        private void ChooseTemplateDestinationPathDialog(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Default|*.xlsx|" +
                         "All files (*.*)|*.*",
                InitialDirectory = Directory.GetCurrentDirectory() + "\\temp\\template\\"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                templateSourcePath = openFileDialog.FileName;
                tbTemplatePath.Text = openFileDialog.FileName;
            }
        }

        /// <summary>
        /// Обработка события TextChanged в элементе TextBox tbFileOrFolderPath.
        /// Если пользователь вручную поменял путь в элементе TextBox tbFileOrFolderPath
        /// </summary>
        private void SourcePathChangedByInput(object sender, TextChangedEventArgs e)
        {
            if (_selectedFileOrFolder == "file")
            {
                filePath = tbFileOrFolderPath.Text;
            }
            else
            {
                folderPath = tbFileOrFolderPath.Text;
            }
        }

        /// <summary>
        /// Обработка события TextChanged в элементе TextBox tbDestinationPath.
        /// Если пользователь вручную поменял путь в элементе TextBox tbDestinationPath
        /// </summary>
        private void DestinationPathChangedByInput(object sender, TextChangedEventArgs e)
        {
            destinationPath = tbDestinationPath.Text;
        }

        /// <summary>
        /// Обработка события TextChanged в элементе TextBox tbExportDestinationPath.
        /// Если пользователь вручную поменял путь в элементе TextBox tbExportDestinationPath
        /// </summary>
        private void ExportDestinationPathChangedByInput(object sender, TextChangedEventArgs e)
        {
            exportDestinationPath = tbExportDestinationPath.Text;
        }

        /// <summary>
        /// Обработка события TextChanged в элементе TextBox tbTemplatePath.
        /// Если пользователь вручную поменял путь в элементе TextBox tbTemplatePath
        /// </summary>
        private void TemplateSourcePathChangedByInput(object sender, TextChangedEventArgs e)
        {
            templateSourcePath = tbTemplatePath.Text;
        }

        /// <summary>
        /// Обработка события TextChanged в элементе TextBox tbSheetNum.
        /// </summary>
        private void SheetNumChangedByInput(object sender, TextChangedEventArgs e)
        {
            if (!Int32.TryParse(tbSheetNum.Text, out _selectedSheetNum))
            {
                MessageBox.Show("Введенное значение не является целочисленным.\nВведите целочисленное значение.\n\nУстановлено значение по умолчанию.",
                                "Ошибка ввода",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);
                tbSheetNum.Text = "3";
            }
            else if (_selectedSheetNum == 0)
            {

                MessageBox.Show("Введите значение отличное от 0.\nНумерация листов Excel-книги начинается с 1.\n\nУстановлено значение по умолчанию.",
                                "Ошибка ввода",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);
                tbSheetNum.Text = "3";
            }
        }

        /// <summary>
        /// Обработка события TextChanged в элементе TextBox tbRowNum.
        /// </summary>
        private void RowNumChangedByInput(object sender, TextChangedEventArgs e)
        {
            if (!Int32.TryParse(tbRowNum.Text, out _selectedRowNum))
            {
                MessageBox.Show("Введенное значение не является целочисленным.\nВведите целочисленное значение.\n\nУстановлено значение по умолчанию.",
                                "Ошибка ввода",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);
                tbRowNum.Text = "1";
            }
            else if (_selectedRowNum == 0)
            {

                MessageBox.Show("Введите значение отличное от 0.\nНумерация строк Excel-книги начинается с 1.\n\nУстановлено значение по умолчанию.",
                                "Ошибка ввода",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);
                tbRowNum.Text = "1";
            }
        }

        /// <summary>
        /// Обработка события TextChanged в элементе TextBox tbColumnNum.
        /// </summary>
        private void ColumnNumChangedByInput(object sender, TextChangedEventArgs e)
        {
            if (!Int32.TryParse(tbColumnNum.Text, out _selectedColumnNum))
            {
                MessageBox.Show("Введенное значение не является целочисленным.\nВведите целочисленное значение.\n\nУстановлено значение по умолчанию.",
                                "Ошибка ввода",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);
                tbColumnNum.Text = "1";
            }
            else if (_selectedColumnNum == 0)
            {

                MessageBox.Show("Введите значение отличное от 0.\nНумерация столбцов Excel-книги начинается с 1.\n\nУстановлено значение по умолчанию.",
                                "Ошибка ввода",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);
                tbColumnNum.Text = "1";
            }
        }

        // Обработка событий Checked на элементах RadioButton группы outputFileFormat
        private void CheckXlsxFileFormat(object sender, RoutedEventArgs e)
        {
            _choosenOutputFileFormat = ".xlsx";
        }

        private void CheckXlsFileFormat(object sender, RoutedEventArgs e)
        {
            _choosenOutputFileFormat = ".xls";
        }

        private void CheckCsvFileFormat(object sender, RoutedEventArgs e)
        {
            _choosenOutputFileFormat = ".csv";
        }

        // Обработка событий Checked/Unchecked в элементе CheckBox chbCsvShowMilisec
        private void CheckShowMilisec(object sender, RoutedEventArgs e)
        {
            _showMilisec = "/t1";
        }

        private void UncheckShowMilisec(object sender, RoutedEventArgs e)
        {
            _showMilisec = "/t0";
        }

        // Обработка событий Checked/Unchecked в элементе CheckBox chbUploadDataWithAveraging
        private void CheckUploadDataWithAveraging(object sender, RoutedEventArgs e)
        {
            _averagingIsEnabled = true;
            cmbAveragingRange.IsEnabled = true;
            imgRawDataWarning.Visibility = Visibility.Hidden;
        }

        private void UncheckUploadDataWithAveraging(object sender, RoutedEventArgs e)
        {
            _averagingIsEnabled = false;
            cmbAveragingRange.IsEnabled = false;
            imgRawDataWarning.Visibility = Visibility.Visible;
        }

        // Обработка событий Checked/Unchecked в элементе CheckBox chbchbOverwriteData
        private void CheckOverwriteData(object sender, RoutedEventArgs e)
        {
            _overwriteIsEnabled = true;
        }

        private void UncheckOverwriteData(object sender, RoutedEventArgs e)
        {
            _overwriteIsEnabled = false;
        }

        // Обработка событий Checked/Unchecked в элементе CheckBox chbExportToTemplate
        private void CheckExportToTemplate(object sender, RoutedEventArgs e)
        {
            tbSheetNum.IsEnabled = true;
            tbRowNum.IsEnabled = true;
            tbColumnNum.IsEnabled = true;
            tbTemplatePath.IsEnabled = true;
            btnBrowseTemplate.IsEnabled = true;
            _exportToTemplateIsEnabled = true;
        }

        private void UncheckExportToTemplate(object sender, RoutedEventArgs e)
        {
            tbSheetNum.IsEnabled = false;
            tbRowNum.IsEnabled = false;
            tbColumnNum.IsEnabled = false;
            tbTemplatePath.IsEnabled = false;
            btnBrowseTemplate.IsEnabled = false;
            _exportToTemplateIsEnabled = false;
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnStartProcess.
        /// Извлекает содержимое архива в папку или запускает процесс конвертирования *.dtl файла.
        /// </summary>
        private void ProcessFileOrFolder(object sender, RoutedEventArgs e)
        {
            if (_selectedFileOrFolder == "file")
            {
                if (File.Exists(filePath))
                {
                    var wf = new WaitingForm(filePath,
                                             destinationPath,
                                             _selectedCsvEncoding,
                                             _choosenOutputFileFormat,
                                             _showMilisec);
                    wf.Owner = this;
                    wf.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Выбранного файла не существует. Возможно, неверно указан путь к файлу.",
                                    "Ошибка выбора файла",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning,
                                    MessageBoxResult.Yes);
                }
            }
            else // == "folder"
            {
                if (Directory.Exists(folderPath))
                {
                    // Поиск *.dtl файлов в выбранной папке и вложенных папках
                    string[] dtlFiles = Directory.GetFiles(folderPath, "*.dtl", SearchOption.AllDirectories);

                    if (dtlFiles.Length != 0)
                    {

                        var pbf = new ProgressBarForm(folderPath,
                                                      destinationPath,
                                                      _selectedCsvEncoding,
                                                      _choosenOutputFileFormat,
                                                      _showMilisec);
                        pbf.Owner = this;
                        pbf.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("Выбранная директория не содержит *.dtl файлы.",
                        "Ошибка выбора директории",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.Yes);
                    }
                }
                else
                {
                    MessageBox.Show("Выбранной директории не существует. Возможно, неверно указан путь к директории.",
                                    "Ошибка выбора директории",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning,
                                    MessageBoxResult.Yes);
                }
            }
        }

        /// <summary>
        /// Обработка события SelectionChanged в элементе ComboBox cmbFileOrFolder.
        /// </summary>
        private void FileOrFolderSelected(object sender, SelectionChangedEventArgs e)
        {
            // Запрос того, что выбрано в элементе ComboBox cmbFileOrFolder
            _selectedFileOrFolder = (cmbFileOrFolder.SelectedItem as ComboBoxItem).Tag.ToString();
            DisableUiElementsFF(_selectedFileOrFolder);
        }

        /// <summary>
        /// Обработка события SelectionChanged в элементе ComboBox cmbCsvEncoding.
        /// </summary>
        private void EncodingSelected(object sender, SelectionChangedEventArgs e)
        {
            // Запрос того, что выбрано в элементе ComboBox cmbCsvEncoding
            _selectedCsvEncoding = (cmbCsvEncoding.SelectedItem as ComboBoxItem).Content.ToString();
        }

        /// <summary>
        /// Обработка события SelectionChanged в элементе ComboBox cmbPhase.
        /// </summary>
        private void PhaseSelected(object sender, SelectionChangedEventArgs e)
        {
            // Запрос того, что выбрано в элементе ComboBox cmbPhase
            _selectedPhase = (cmbPhase.SelectedItem as ComboBoxItem).Content.ToString();
        }

        /// <summary>
        /// Обработка события SelectionChanged в элементе ComboBox cmbAveragingRange.
        /// </summary>
        private void AveragingRangeSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedAveragingRange = Convert.ToInt32((cmbAveragingRange.SelectedItem as ComboBoxItem).Tag);
            AppConfig.SetAveragingRange((cmbAveragingRange.SelectedItem as ComboBoxItem).Uid);
        }

        /// <summary>
        /// Обработка события Click в элементе Button "Подключиться...".
        /// </summary>
        private async void ConnectToDbAsync(object sender, RoutedEventArgs e)
        {
            tbConnectionStatus.Text = "⏳";
            tbConnectionStatus.ToolTip = "Выполнение операции";
            tbConnectionStatus.Text = await DataUploader.DataContext.TestConnectionAsync();
            tbConnectionStatus.ToolTip = null;
        }

        /// <summary>
        /// Обработка события Click в элементе Button "Сохранить настройки...".
        /// </summary>
        private void SaveConnectionSettings(object sender, RoutedEventArgs e)
        {
            AppConfig.SetSettings(tbServerIP.Text,
                                  tbServerPort.Text,
                                  tbDatabaseName.Text,
                                  tbLogin.Text,
                                  tbPassword.Password);

            tbServerIP.Background = Brushes.White;
            tbServerPort.Background = Brushes.White;
            tbDatabaseName.Background = Brushes.White;
            tbLogin.Background = Brushes.White;
            tbPassword.Background = Brushes.White;

            btnTestConnection.IsEnabled = true;
        }

        // Обработка событий TextChanged в элементах TextBox
        private void ServerIpChanged(object sender, TextChangedEventArgs e)
        {
            tbServerIP.Background = new SolidColorBrush(Color.FromRgb(250, 255, 189));
            btnTestConnection.IsEnabled = false;
        }

        private void DatabaseNameChanged(object sender, TextChangedEventArgs e)
        {
            tbDatabaseName.Background = new SolidColorBrush(Color.FromRgb(250, 255, 189));
            btnTestConnection.IsEnabled = false;
        }

        private void ServerPortChanged(object sender, TextChangedEventArgs e)
        {
            tbServerPort.Background = new SolidColorBrush(Color.FromRgb(250, 255, 189));
            btnTestConnection.IsEnabled = false;
        }

        private void LoginChanged(object sender, TextChangedEventArgs e)
        {
            tbLogin.Background = new SolidColorBrush(Color.FromRgb(250, 255, 189));
            btnTestConnection.IsEnabled = false;
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            tbPassword.Background = new SolidColorBrush(Color.FromRgb(250, 255, 189));
            btnTestConnection.IsEnabled = false;
        }

        /// <summary>
        /// Обработка события Click в элементе Button "Выбрать...".
        /// По нажатию кнопки открывается форма с деревом категорий и типов файлов в БД
        /// </summary>
        private void OpenCategoryTreeForm(object sender, RoutedEventArgs e)
        {
            var tvf = new TreeViewForm("categories_and_files");
            tvf.Owner = this;
            var result = tvf.ShowDialog();

            // false -> окно закрылось, можно считывать поля
            if (result == false)
            {
                tbSelectedCategory.Text = tvf.SelectedParentName;
                tbSelectedFileType.Text = tvf.SelectedChildName;
                _selectedCategoryId = tvf.SelectedParentId;
                _selectedFileId = tvf.SelectedChildId;

                _categoryWasSelected = tvf.ItemsWereSelected;
                Enable_btnImportToDB(_sourceWasSelected, _substationWasSelected, _categoryWasSelected);
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button "Выбрать...".
        /// По нажатию кнопки открывается форма с деревом подстанций и трансформаторов в БД
        /// </summary>
        private void OpenSubstationTreeForm(object sender, RoutedEventArgs e)
        {
            var tvf = new TreeViewForm("substations_and_transformers");
            tvf.Owner = this;
            var result = tvf.ShowDialog();

            // false -> окно закрылось, можно считывать поля
            if (result == false)
            {
                tbSelectedSubstation.Text = tvf.SelectedParentName;
                tbSelectedTransformer.Text = tvf.SelectedChildName;
                _selectedSubstationId = tvf.SelectedParentId;
                _selectedTransformerId = tvf.SelectedChildId;

                _substationWasSelected = tvf.ItemsWereSelected;
                Enable_btnImportToDB(_sourceWasSelected, _substationWasSelected, _categoryWasSelected);
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnImportToDB.
        /// Импорт содержимого выбранного файла / файлов в папке в БД
        /// </summary>
        private void ImportToDB(object sender, RoutedEventArgs e)
        {
            if (_selectedFileOrFolder == "file")
            {
                if (File.Exists(filePath))
                {
                    var wf = new WaitingForm(filePath: filePath,
                                             fileTypeId: _selectedFileId,
                                             transformerId: _selectedTransformerId,
                                             phase: _selectedPhase,
                                             mode: "import",
                                             averagingIsEnabled: _averagingIsEnabled,
                                             selectedAveragingRange: _selectedAveragingRange);
                    wf.Owner = this;
                    wf.ShowDialog();

                    if (wf.ImportIsCompleted == true)
                    {
                        ConstructUploadedFilesLogAsync();
                    }
                }
                else
                {
                    MessageBox.Show("Выбранного файла не существует. Возможно, неверно указан путь к файлу.",
                                    "Ошибка выбора файла",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning,
                                    MessageBoxResult.Yes);
                }
            }
            else // == "folder"
            {
                if (Directory.Exists(folderPath))
                {
                    // Поиск *.dtl файлов в выбранной папке и вложенных папках
                    string[] dtlFiles = Directory.GetFiles(folderPath, "*.dtl", SearchOption.AllDirectories);

                    if (dtlFiles.Length != 0)
                    {
                        var impf = new ImportForm(folderPath,
                                                  _selectedFileId,
                                                  _selectedTransformerId,
                                                  _selectedPhase,
                                                  _averagingIsEnabled,
                                                  _overwriteIsEnabled,
                                                  _selectedAveragingRange);

                        impf.Owner = this;
                        impf.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("Выбранная директория не содержит *.dtl файлы.",
                        "Ошибка выбора директории",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.Yes);
                    }
                }
                else
                {
                    MessageBox.Show("Выбранной директории не существует. Возможно, неверно указан путь к директории.",
                                    "Ошибка выбора директории",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning,
                                    MessageBoxResult.Yes);
                }
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnDeleteItem.
        /// Удалить загруженный файл из БД
        /// </summary>
        private async void DeleteUploadedFileAsync(object sender, RoutedEventArgs e)
        {
            var selectedListViewItem = lvUploadedFiles.SelectedItem as DataModel.UploadedFile;

            if (selectedListViewItem != null)
            {
                var r = MessageBox.Show("Вы уверены, что хотите удалить из базы данных содержимое указанного файла?",
                "Удаление загруженного файла",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.Yes);

                if (r == MessageBoxResult.Yes)
                {
                    int? updatedRows = await DataUploader.DataContext.DeleteUploadedFileAsync(selectedListViewItem);

                    if (updatedRows != null)
                    {
                        lvUploadedFiles.Items.Remove(selectedListViewItem);
                    }
                }
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnUpdateTable.
        /// Обновляет таблицу-журнал загруженных файлов
        /// </summary>
        private async void UpdateLogTableAsync(object sender, RoutedEventArgs e)
        {
            // Очистим список
            lvUploadedFiles.Items.Clear();

            btnUpdateTable.IsEnabled = false;
            List<DataModel.UploadedFile> uploadedFilesList = await DataUploader.DataContext.GetUploadedFilesAsync();

            if (uploadedFilesList != null)
            {
                // Добавляем строки в журнал файлов, загруженных в БД
                foreach (DataModel.UploadedFile uf in uploadedFilesList)
                {
                    lvUploadedFiles.Items.Add(uf);
                }
            }

            btnUpdateTable.IsEnabled = true;
        }

        /// <summary>
        /// Обработка события SelectedDateChanged в элементе DatePicker dpStartDate.
        /// </summar
        private void StartDateSelected(object sender, SelectionChangedEventArgs e)
        {
            if (__resetIntervalDatesWasFired)
            {
                //HACK [#1] При смене даты в элементе dpStartDate через свойство ".SelectedDate"
                // событие SelectedDateChanged срабатывает дважды, поэтому отпишемся от обновления
                // на время, чтобы оно не сработало 2-ой раз
                dpStartDate.SelectedDateChanged -= StartDateSelected;
            }

            _startDate = dpStartDate.SelectedDate;
            if (_endDate != null && _endDate > _startDate)
            {
                ConstructSubstTransPhaseTreeAsync((DateTime)_startDate, (DateTime)_endDate);
                _approvedStartDate = _startDate;
                _approvedEndDate = _endDate;
            }
            else
            {
                tvSubstTransPhase.Items.Clear();
                _approvedStartDate = null;
                _approvedEndDate = null;
            }
        }

        /// <summary>
        /// Обработка события SelectedDateChanged в элементе DatePicker dpEndDate.
        /// </summar
        private void EndDateSelected(object sender, SelectionChangedEventArgs e)
        {
            if (__resetIntervalDatesWasFired)
            {
                //HACK [#1] При смене даты в элементе dpEndDate через свойство ".SelectedDate"
                // событие SelectedDateChanged срабатывает дважды, поэтому отпишемся от обновления
                // на время, чтобы оно не сработало 2-ой раз
                dpEndDate.SelectedDateChanged -= EndDateSelected;
            }

            _endDate = dpEndDate.SelectedDate;
            if (_startDate != null && _startDate < _endDate)
            {
                ConstructSubstTransPhaseTreeAsync((DateTime)_startDate, (DateTime)_endDate);
                _approvedStartDate = _startDate;
                _approvedEndDate = _endDate;
            }
            else
            {
                tvSubstTransPhase.Items.Clear();
                _approvedStartDate = null;
                _approvedEndDate = null;
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnResetIntervalDate
        /// Сброс дат (значений) в элементах DatePicker dpStartDate и dpEndDate к max и min,
        /// которые есть в БД
        /// </summary>
        private async void ResetIntervalDates(object sender, RoutedEventArgs e)
        {
            List<DateTime> minMaxDates = await DataUploader.DataContext.GetMinMaxDates();
            __resetIntervalDatesWasFired = true;

            if (minMaxDates == null || minMaxDates.Count == 0)
            {
                dpStartDate.SelectedDate = null;
                dpEndDate.SelectedDate = null;
                _approvedStartDate = null;
                _approvedEndDate = null;
            }
            else
            {
                __resetIntervalDatesWasFired = true;
                dpStartDate.SelectedDate = minMaxDates[0];
                dpEndDate.SelectedDate = minMaxDates[1]; 
                __resetIntervalDatesWasFired = false;
                //HACK [#1] Снова подпишемся на эти события, от которых на время отписались
                dpStartDate.SelectedDateChanged += StartDateSelected;
                dpEndDate.SelectedDateChanged += EndDateSelected;
            }
        }

        /// <summary>
        /// Обработка события SelectedItemChanged в элементе TreeView tvSubstTransPhase
        /// </summary>
        private async void SelectTreeItem(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedTreeViewItem = tvSubstTransPhase.SelectedItem as DataModel.Node;

            if (_selectedTreeViewItem != null)
            {
                _fieldsOrderCounter = 0;
                // Только у последнего элемента Childs.Count == 0
                if (_selectedTreeViewItem.Childs.Count == 0)
                {
                    _selectedAvailibleAveragingRange = (int)_selectedTreeViewItem.Id;
                    _availibleFieldsInfoList = await DataUploader.DataContext.GetAvailibleFieldsInfoAsync((int)_selectedTreeViewItem.Parent.Parent.Parent.Id,
                                                                                                          (int)_selectedTreeViewItem.Parent.Parent.Id,
                                                                                                          _selectedTreeViewItem.Parent.Name,
                                                                                                          (int)_selectedTreeViewItem.Id);
                    btnLoadFieldsPreset.IsEnabled = true;
                    btnSaveFieldsPreset.IsEnabled = true;
                    // Привязываем список кака источник данных для элемента DataGrid dgAvailibleFieldsInfo
                    dgAvailibleFieldsInfo.ItemsSource = _availibleFieldsInfoList;

                    // Если усреднение нет, то скрыть колонки MIN и MAX
                    if (_selectedAvailibleAveragingRange == 0)
                    {
                        btnLoadFieldsPreset.IsEnabled = false;
                        btnSaveFieldsPreset.IsEnabled = false;
                        dgAvailibleFieldsInfo.Columns[4].Header = "RAW";
                        dgAvailibleFieldsInfo.Columns[5].Visibility = Visibility.Hidden;
                        dgAvailibleFieldsInfo.Columns[6].Visibility = Visibility.Hidden;
                        dgAvailibleFieldsInfo.Columns[7].Header = "RAW☑";
                        dgAvailibleFieldsInfo.Columns[8].Visibility = Visibility.Hidden;
                        dgAvailibleFieldsInfo.Columns[9].Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        btnLoadFieldsPreset.IsEnabled = true;
                        btnSaveFieldsPreset.IsEnabled = true;
                        dgAvailibleFieldsInfo.Columns[4].Header = "AVG";
                        dgAvailibleFieldsInfo.Columns[5].Visibility = Visibility.Visible;
                        dgAvailibleFieldsInfo.Columns[6].Visibility = Visibility.Visible;
                        dgAvailibleFieldsInfo.Columns[7].Header = "AVG☑";
                        dgAvailibleFieldsInfo.Columns[8].Visibility = Visibility.Visible;
                        dgAvailibleFieldsInfo.Columns[9].Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    _selectedAvailibleAveragingRange = null;
                    btnLoadFieldsPreset.IsEnabled = false;
                    btnSaveFieldsPreset.IsEnabled = false;
                    // Очистим список
                    dgAvailibleFieldsInfo.ItemsSource = null;
                }
            }
            else
            {
                _selectedAvailibleAveragingRange = null;
                btnLoadFieldsPreset.IsEnabled = false;
                btnSaveFieldsPreset.IsEnabled = false;
                // Очистим список
                dgAvailibleFieldsInfo.ItemsSource = null;
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnExportFromDB.
        /// Экспорт данных из БД в файл формата *.xlsx
        /// </summary>
        private void ExportFromDB(object sender, RoutedEventArgs e)
        {
            if (_selectedAvailibleAveragingRange == null)
            {
                MessageBox.Show("Интервал усреднения для выгружаемых данных не выбран.\n" +
                                "Выберите интервал усреднения среди доступного оборудования.",
                                "Ошибка выбора парметров выгрузки",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);
            }
            else if (_exportToTemplateIsEnabled && !File.Exists(templateSourcePath) ||
                    ((_exportToTemplateIsEnabled && File.Exists(templateSourcePath) && !(Path.GetExtension(templateSourcePath) == ".xlsx"))))
            {
                MessageBox.Show("Файл шаблона в формате *.xlsx не найден. " +
                                "Проверьте расположение файла шаблона в параметрах экспорта данных или создайте файл template.xlsx по указанному пути.",
                                "Ошибка выбора файла шаблона",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);
            }
            else
            {
                bool atLeastOneFieldWasChoosen = false;
                foreach (DataModel.AvailibleFieldsInfo afi in _availibleFieldsInfoList)
                {
                    if (afi.ValueIsChecked || afi.ValueMinIsChecked || afi.ValueMaxIsChecked) { atLeastOneFieldWasChoosen = true; }
                }

                if (!atLeastOneFieldWasChoosen)
                {
                    MessageBox.Show("Экспортируемые поля не выбраны.",
                                    "Ошибка выбора полей для экспорта",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning,
                                    MessageBoxResult.Yes);
                }
                else
                {
                    var ef = new ExportForm((DateTime)_approvedStartDate,
                                           (DateTime)_approvedEndDate,
                                           (int)_selectedAvailibleAveragingRange,
                                           _availibleFieldsInfoList,
                                           exportDestinationPath,
                                           _exportToTemplateIsEnabled,
                                           templateSourcePath,
                                           _selectedSheetNum,
                                           _selectedRowNum,
                                           _selectedColumnNum);
                    ef.Owner = this;
                    ef.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnSaveCheckedPreset.
        /// Сохраняет пресет отмеченных для экспорта полей в БД.
        /// </summary>
        private void SaveCheckedPreset(object sender, RoutedEventArgs e)
        {
            bool atLeastOneFieldWasChoosen = false;
            foreach (DataModel.AvailibleFieldsInfo afi in _availibleFieldsInfoList)
            {
                if (afi.ValueIsChecked || afi.ValueMinIsChecked || afi.ValueMaxIsChecked) { atLeastOneFieldWasChoosen = true; }
            }

            if (!atLeastOneFieldWasChoosen)
            {
                MessageBox.Show("Экспортируемые поля не выбраны.",
                                "Ошибка выбора полей для экспорта",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning,
                                MessageBoxResult.Yes);
            }
            else
            {
                string typedName = null;
                try
                {
                    var tnf = new TypeNameForm();
                    tnf.Owner = this;
                    var result = tnf.ShowDialog();

                    // false -> окно закрылось, можно считывать поля
                    if (result == false)
                    {
                        typedName = tnf.TypedName;
                    }

                    if (typedName != null)
                    {
                        int? updatedRows = DataUploader.DataContext.SaveSelectedFieldsPreset(_availibleFieldsInfoList, typedName);

                        MessageBox.Show("Сохрание пресета экспортируемых полей выполнено успешно.",
                                        "Сохранение пресета экспортируемых полей.",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information,
                                        MessageBoxResult.Yes);
                    }
                }
                catch (Exception ex)
                {
                    string messageBoxText = String.Format("{0}\n{1}\n{2}", ex.Message, ex.InnerException, ex.StackTrace);
                    string caption = "Ошибка";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                }
            }
        }


        /// <summary>
        /// Обработка события Click в элементе Button btnLoadCheckedPreset.
        /// Отмечает экспортируемые поля соглвсно сохранённому пресету.
        /// </summary>
        private void LoadCheckedPreset(object sender, RoutedEventArgs e)
        {
            var lf = new ListForm();
            lf.Owner = this;
            var result = lf.ShowDialog();

            // false -> окно закрылось, можно считывать поля
            int? choosenPresetId = null;
            if (result == false)
            {
                choosenPresetId = lf.ChoosenPresetId;
            }

            List<DataModel.AvailibleFieldsInfo> retrievedFieldsInfo = null;
            Dictionary<int, (string, List<(int, string)>)> retrievedPreset = null;
            var mapListIndexToFieldId = new Dictionary<int, int>();

            if (choosenPresetId != null)
            {
                retrievedFieldsInfo = DataUploader.DataContext.GetFieldsPresetContent((int)choosenPresetId);
                retrievedPreset = ExportForm.CreateSelectedFieldsInfoDict(retrievedFieldsInfo);

                for (int i = 0; i < _availibleFieldsInfoList.Count; i++)
                {
                    // Сброс значений
                    _availibleFieldsInfoList[i].ValueIsCheckedOrder =
                        _availibleFieldsInfoList[i].ValueMinIsCheckedOrder =
                        _availibleFieldsInfoList[i].ValueMaxIsCheckedOrder = null;

                    _availibleFieldsInfoList[i].ValueIsChecked =
                        _availibleFieldsInfoList[i].ValueMinIsChecked =
                        _availibleFieldsInfoList[i].ValueMaxIsChecked = false;

                    // Составим карту какой id  лежит в каком элементе списка
                    mapListIndexToFieldId[_availibleFieldsInfoList[i].FieldId] = i;
                }

                // Разворачиваем пресет на полях _availibleFieldsInfoList
                _fieldsOrderCounter = 0;
                for (int i = 0; i < retrievedPreset.Count; i++)
                {
                    int tempDictKey = retrievedPreset.Keys.ElementAt(i);
                    List<(int, string)> tempDictValue = retrievedPreset.Values.ElementAt(i).Item2;

                    for (int j = 0; j < tempDictValue.Count; j++)
                    {
                        // После загрузки пресета _fieldsOrderCounter должен быть равен максимальному порпядковому номеру из пресета
                        if (tempDictValue[j].Item1 > _fieldsOrderCounter) { _fieldsOrderCounter = tempDictValue[j].Item1; }

                        var tempIndex = mapListIndexToFieldId[tempDictKey];
                        if (tempDictValue[j].Item2 == "value")
                        {
                            _availibleFieldsInfoList[tempIndex].ValueIsChecked = true;
                            _availibleFieldsInfoList[tempIndex].ValueIsCheckedOrder = tempDictValue[j].Item1;
                        }
                        if (tempDictValue[j].Item2 == "value_min")
                        {
                            _availibleFieldsInfoList[tempIndex].ValueMinIsChecked = true;
                            _availibleFieldsInfoList[tempIndex].ValueMinIsCheckedOrder = tempDictValue[j].Item1;
                        }
                        if (tempDictValue[j].Item2 == "value_max")
                        {
                            _availibleFieldsInfoList[tempIndex].ValueMaxIsChecked = true;
                            _availibleFieldsInfoList[tempIndex].ValueMaxIsCheckedOrder = tempDictValue[j].Item1;
                        }
                    }
                    dgAvailibleFieldsInfo.ItemsSource = _availibleFieldsInfoList;
                }
            }
        }

        /// <summary>
        /// Обработка события BeginningEdit в элементе DataGrid dgAvailibleFieldsInfo.
        /// Для отслеживания последовательности выбора полей для экспорта.
        /// </summary>
        private void DataGridCurrentCheckBoxCellEditBeginning(object sender, DataGridBeginningEditEventArgs e)
        {
            _activeCheckBoxCellAtEdit = dgAvailibleFieldsInfo.CurrentCell;
        }

        /// <summary>
        /// Обработка события CellEditEnding в элементе DataGrid dgAvailibleFieldsInfo.
        /// Для отслеживания последовательности выбора полей для экспорта.
        /// </summary>
        private void DataGridCurrentCheckBoxCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var activeCheckBoxCellAtEdit = _activeCheckBoxCellAtEdit.Item as DataModel.AvailibleFieldsInfo;

            bool checkboxCellStateBeforeEdit = false;
            bool checkboxCellStateEdited = (bool)(e.EditingElement as CheckBox).IsChecked;

            // Алгоритм расстановки чисел в какой последовательности выбраны поля
            switch (Convert.ToString(_activeCheckBoxCellAtEdit.Column.Header))
            {
                default: // Т.к. столбец может называться "AVG" или "RAW"
                    checkboxCellStateBeforeEdit = activeCheckBoxCellAtEdit.ValueIsChecked;
                    if (checkboxCellStateEdited != checkboxCellStateBeforeEdit && checkboxCellStateBeforeEdit == false)
                    {
                        _fieldsOrderCounter++;
                        activeCheckBoxCellAtEdit.ValueIsCheckedOrder = _fieldsOrderCounter;
                    }
                    else
                    {
                        if (activeCheckBoxCellAtEdit.ValueIsCheckedOrder != null)
                        {
                            foreach (var listItem in _availibleFieldsInfoList)
                            {
                                if (listItem.ValueIsCheckedOrder > activeCheckBoxCellAtEdit.ValueIsCheckedOrder) { listItem.ValueIsCheckedOrder--; }
                                if (listItem.ValueMinIsCheckedOrder > activeCheckBoxCellAtEdit.ValueIsCheckedOrder) { listItem.ValueMinIsCheckedOrder--; }
                                if (listItem.ValueMaxIsCheckedOrder > activeCheckBoxCellAtEdit.ValueIsCheckedOrder) { listItem.ValueMaxIsCheckedOrder--; }
                            }
                            _fieldsOrderCounter--;
                            activeCheckBoxCellAtEdit.ValueIsCheckedOrder = null;
                        }
                    }
                    break;

                case "MIN":
                    checkboxCellStateBeforeEdit = activeCheckBoxCellAtEdit.ValueMinIsChecked;
                    if (checkboxCellStateEdited != checkboxCellStateBeforeEdit && checkboxCellStateBeforeEdit == false)
                    {
                        _fieldsOrderCounter++;
                        activeCheckBoxCellAtEdit.ValueMinIsCheckedOrder = _fieldsOrderCounter;
                    }
                    else
                    {
                        if (activeCheckBoxCellAtEdit.ValueMinIsCheckedOrder != null)
                        {
                            foreach (var listItem in _availibleFieldsInfoList)
                            {
                                if (listItem.ValueIsCheckedOrder > activeCheckBoxCellAtEdit.ValueMinIsCheckedOrder) { listItem.ValueIsCheckedOrder--; }
                                if (listItem.ValueMinIsCheckedOrder > activeCheckBoxCellAtEdit.ValueMinIsCheckedOrder) { listItem.ValueMinIsCheckedOrder--; }
                                if (listItem.ValueMaxIsCheckedOrder > activeCheckBoxCellAtEdit.ValueMinIsCheckedOrder) { listItem.ValueMaxIsCheckedOrder--; }
                            }
                            _fieldsOrderCounter--;
                            activeCheckBoxCellAtEdit.ValueMinIsCheckedOrder = null;
                        }
                    }
                    break;

                case "MAX":
                    checkboxCellStateBeforeEdit = activeCheckBoxCellAtEdit.ValueMaxIsChecked;
                    if (checkboxCellStateEdited != checkboxCellStateBeforeEdit && checkboxCellStateBeforeEdit == false)
                    {
                        _fieldsOrderCounter++;
                        activeCheckBoxCellAtEdit.ValueMaxIsCheckedOrder = _fieldsOrderCounter;
                    }
                    else
                    {
                        if (activeCheckBoxCellAtEdit.ValueMaxIsCheckedOrder != null)
                        {
                            foreach (var listItem in _availibleFieldsInfoList)
                            {
                                if (listItem.ValueIsCheckedOrder > activeCheckBoxCellAtEdit.ValueMaxIsCheckedOrder) { listItem.ValueIsCheckedOrder--; }
                                if (listItem.ValueMinIsCheckedOrder > activeCheckBoxCellAtEdit.ValueMaxIsCheckedOrder) { listItem.ValueMinIsCheckedOrder--; }
                                if (listItem.ValueMaxIsCheckedOrder > activeCheckBoxCellAtEdit.ValueMaxIsCheckedOrder) { listItem.ValueMaxIsCheckedOrder--; }
                            }
                            _fieldsOrderCounter--;
                            activeCheckBoxCellAtEdit.ValueMaxIsCheckedOrder = null;
                        }
                    }
                    break;
            }
        }
    }
}
