using System;
using System.Collections.Generic;
using System.IO;
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
        public string selectedCsvEncoding;
        public string selectedFileOrFolder;
        public string choosenInputFileFormat;
        public string choosenOutputFileFormat;
        public string showMilisec = "/t0";

        public string selectedSubstation;
        public string selectedTransformer;
        public int? selectedCategoryId = null;
        public int? selectedFileId = null;
        public int? selectedSubstationId = null;
        public int? selectedTransformerId = null;
        public string selectedPhase = null;

        private bool _sourceWasSelected = false;
        private bool _substationWasSelected = false;
        private bool _categoryWasSelected = false;

        private string _dialogSelectedPath = null;

        public MainWindow()
        {
            InitializeComponent();

            destinationPath = Directory.GetCurrentDirectory() + "\\temp";
            
            tbDestinationPath.Text = destinationPath;

            // В ComboBox cmbFileOrFolder выбрать "Файл (для обработки единичного файла *.zip, *.7z, *.dtl, *.xlsx)"
            cmbFileOrFolder.SelectedItem = cmbFileOrFolder.Items[0];
            cmbCsvEncoding.SelectedItem = cmbCsvEncoding.Items[0];
            cmbPhase.SelectedItem = cmbPhase.Items[0];

            // При запуске программы надо оставить элементы интерфейса, соотвествующие...
            // ...выбранному пункту в ComboBox cmbFileOrFolder
            DisableUiElementsFF(selectedFileOrFolder);

            // При запуске программы файл не выбран, следовательно надо все соотвествующие...
            // ...элементы интерфейса отключить
            DisableUiElements("default", false);

            // Считать настройки подключения из App.cpnfig
            InitializeConnectionSettings();

            // Заполняет журнал информацией о загруженных в БД файлах
            ConstructUploadedFilesLogAsync();
        }

        private void InitializeConnectionSettings()
        {
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
                    choosenInputFileFormat = ".zip";
                    rbArchive.IsChecked = true;
                    DisableUiElements(choosenInputFileFormat, false);
                    break;

                case ".7z":
                    choosenInputFileFormat = ".7z";
                    rbArchive.IsChecked = true;
                    DisableUiElements(choosenInputFileFormat, false);
                    break;

                case ".dtl":
                    choosenInputFileFormat = ".dtl";
                    rbDtl.IsChecked = true;
                    DisableUiElements(choosenInputFileFormat, false);
                    break;

                case ".xlsx":
                    choosenInputFileFormat = ".xlsx";
                    rbXlsx.IsChecked = true;
                    DisableUiElements(choosenInputFileFormat, false);
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
                        btnStartProcess.ToolTip = "";
                        btnImportToDB.ToolTip = "";
                        // Group 5
                        return new Control[9] {tbDestinationPath,
                                               btnBrowseDestination,
                                               btnStartProcess,
                                               rbXlsxFileFormat,
                                               rbXlsFileFormat,
                                               rbCsvFileFormat,
                                               cmbCsvEncoding,
                                               chbCsvShowMilisec,
                                               btnImportToDB};
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

                        btnStartProcess.ToolTip = "Начать пакетное конвертирование *.dtl файлов.";

                        rbXlsxFileFormat.IsEnabled = true;
                        rbXlsFileFormat.IsEnabled = true;
                        rbCsvFileFormat.IsEnabled = true;
                        cmbCsvEncoding.IsEnabled = true;
                        chbCsvShowMilisec.IsEnabled = true;

                        btnStartProcess.IsEnabled = false;
                        btnBrowseDestination.IsEnabled = false;
                        tbDestinationPath.IsEnabled = false;
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

            if (selectedFileOrFolder == "file")
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Readable files|*.zip;*.7z;*.xlsx;*.dtl|" +
                             "Archives|*.zip;*.7z|" +
                             "All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileName);
                    string fileExtension = Path.GetExtension(openFileDialog.FileName);

                    SetRadioButtonState(fileExtension);

                    // Запись имени файла в TextBox tbFilePath
                    tbFileOrFolderPath.Text = fileName;
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
                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                {
                    dialog.Description = "Выберете директорию для обработки файлов, содержащихся в ней.";
                    if (_dialogSelectedPath == null)
                    {
                        dialog.SelectedPath = Directory.GetCurrentDirectory();
                    }
                    else
                    {
                        dialog.SelectedPath = _dialogSelectedPath;
                    }
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        tbFileOrFolderPath.Text = dialog.SelectedPath;
                        folderPath = dialog.SelectedPath;
                        _dialogSelectedPath = dialog.SelectedPath;

                        btnStartProcess.IsEnabled = true;
                        btnBrowseDestination.IsEnabled = true;
                        tbDestinationPath.IsEnabled = true;

                        _sourceWasSelected = true;
                        Enable_btnImportToDB(_sourceWasSelected, _substationWasSelected, _categoryWasSelected);
                    }
                }
            }
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnBrowseDestination "Обзор...".
        /// </summary>
        private void ChooseDestinationFolderDialog(object sender, RoutedEventArgs e)
        {

            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            {
                dialog.Description = "Выберете директорию для сохранения обработанных файлов.";
                dialog.SelectedPath = destinationPath;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    tbDestinationPath.Text = dialog.SelectedPath;
                    destinationPath = dialog.SelectedPath;
                }
            }
        }

        /// <summary>
        /// Обработка события TextChanged в элементе TextBox tbDestinationPath.
        /// Извлекает содержимое архива в папку
        /// </summary>
        private void ChangedByInput(object sender, TextChangedEventArgs e)
        {
            destinationPath = tbDestinationPath.Text;
        }

        // Обработка событий Checked на элементах RadioButton группы outputFileFormat
        private void CheckXlsxFileFormat(object sender, RoutedEventArgs e)
        {
            choosenOutputFileFormat = ".xlsx";
        }

        private void CheckXlsFileFormat(object sender, RoutedEventArgs e)
        {
            choosenOutputFileFormat = ".xls";
        }

        private void CheckCsvFileFormat(object sender, RoutedEventArgs e)
        {
            choosenOutputFileFormat = ".csv";
        }

        // Обработка событий Checked/Unchecked в элементе CheckBox chbCsvShowMilisec
        private void CheckShowMilisec(object sender, RoutedEventArgs e)
        {
            showMilisec = "/t1";
        }

        private void UncheckShowMilisec(object sender, RoutedEventArgs e)
        {
            showMilisec = "/t0";
        }

        /// <summary>
        /// Обработка события Click в элементе Button btnStartProcess.
        /// Извлекает содержимое архива в папку или запускает процесс конвертирования *.dtl файла.
        /// </summary>
        private void ProcessFileOrFolder(object sender, RoutedEventArgs e)
        {
            if (selectedFileOrFolder == "file")
            {
                var wb = new WaitingBox(filePath,
                                        destinationPath,
                                        selectedCsvEncoding,
                                        choosenOutputFileFormat,
                                        showMilisec);
                wb.Owner = this;
                wb.ShowDialog();
            }
            else // == "folder"
            {
                // Поиск *.dtl файлов в выбранной папке и вложенных папках
                string[] dtlFiles = Directory.GetFiles(folderPath, "*.dtl", SearchOption.AllDirectories);

                if (dtlFiles.Length != 0)
                {

                    var pbf = new ProgressBarForm(folderPath,
                                                  destinationPath,
                                                  selectedCsvEncoding,
                                                  choosenOutputFileFormat,
                                                  showMilisec);
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
        }

        /// <summary>
        /// Обработка события SelectionChanged в элементе ComboBox cmbFileOrFolder.
        /// </summary>
        private void FileOrFolderSelected(object sender, SelectionChangedEventArgs e)
        {
            // Запрос того, что выбрано в элементе ComboBox cmbFileOrFolder
            selectedFileOrFolder = (cmbFileOrFolder.SelectedItem as ComboBoxItem).Tag.ToString();
            DisableUiElementsFF(selectedFileOrFolder);
        }

        /// <summary>
        /// Обработка события SelectionChanged в элементе ComboBox cmbCsvEncoding.
        /// </summary>
        private void EncodingSelected(object sender, SelectionChangedEventArgs e)
        {
            // Запрос того, что выбрано в элементе ComboBox cmbCsvEncoding
            selectedCsvEncoding = (cmbCsvEncoding.SelectedItem as ComboBoxItem).Content.ToString();
        }

        /// <summary>
        /// Обработка события SelectionChanged в элементе ComboBox cmbPhase.
        /// </summary>
        private void PhaseSelected(object sender, SelectionChangedEventArgs e)
        {
            // Запрос того, что выбрано в элементе ComboBox cmbPhase
            selectedPhase = (cmbPhase.SelectedItem as ComboBoxItem).Content.ToString();
        }


        /// <summary>
        /// Обработка события Click в элементе Button "Подключиться...".
        /// </summary>
        private async void ConnectToDbAsync(object sender, RoutedEventArgs e)
        {
            tbConnectionStatus.Text = "...";
            tbConnectionStatus.Text = await DataUploader.DataContext.TestConnectionAsync();
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
                selectedCategoryId = tvf.SelectedParentId;
                selectedFileId = tvf.SelectedChildId;

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
                selectedSubstationId = tvf.SelectedParentId;
                selectedTransformerId = tvf.SelectedChildId;

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
            if (selectedFileOrFolder == "file")
            {
                var wb = new WaitingBox(filePath: filePath,
                                        fileTypeId: selectedFileId,
                                        transformerId: selectedTransformerId,
                                        phase: selectedPhase,
                                        mode: "import");
                wb.Owner = this;
                wb.ShowDialog();

                if (wb.ImportIsCompleted == true)
                {
                    ConstructUploadedFilesLogAsync();
                }
            }
            else // == "folder"
            {
                // Поиск *.dtl файлов в выбранной папке и вложенных папках
                string[] dtlFiles = Directory.GetFiles(folderPath, "*.dtl", SearchOption.AllDirectories);

                if (dtlFiles.Length != 0)
                {
                    var ib = new ImportBox(folderPath,
                                           selectedFileId,
                                           selectedTransformerId,
                                           selectedPhase);

                    ib.Owner = this;
                    ib.ShowDialog();
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
        private async void UpdateTableAsync(object sender, RoutedEventArgs e)
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
    }
}
