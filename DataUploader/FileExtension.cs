using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DataUploader
{
    internal class FileExtension
    {
        public static string DetermineExtension(string filePath)
        {
            int dotPosition = filePath.LastIndexOf(".");
            return filePath.Substring(dotPosition);
        }

        /// <summary>
        /// В зависимости от от расширения файла "закрашивает" тот или иной RadioButton
        /// </summary>
        public static void SetRadioButtonState(string extension, MainWindow mW)
        {
            switch (extension)
            {
                case ".zip":
                    mW.rbArchiveZip.IsChecked = true;
                    break;
                case ".7z":
                    mW.rbArchive7z.IsChecked = true;
                    break;
                case ".dtl":
                    mW.rbDtl.IsChecked = true;
                    break;
                case ".xls":
                    mW.rbXls.IsChecked = true;
                    break;
                case ".xlsx":
                    mW.rbXlsx.IsChecked = true;
                    break;
                default:
                    mW.rbArchiveZip.IsChecked = false;
                    mW.rbArchive7z.IsChecked = false;
                    mW.rbDtl.IsChecked = false;
                    mW.rbXlsx.IsChecked = false;
                    mW.rbXls.IsChecked = false;

                    // Когда пользователь выбирает файл, который программа не может определить...
                    // ... необходимо элементы интерфейса отключить
                    FileExtension.DisableUiElements(mW, "default", false);

                    break;
            }
        }

        /// <summary>
        /// В зависимости от расширения файла отключает элементы интерфейса
        /// </summary>
        public static void DisableUiElements(MainWindow mW, string extension, bool isTrue)
        {
            // Сначала всё включим
            EnableAllUiElements();

            // Затем нужное отключим
            Control[] uiGroupToBeDisabled = ReturnUiGroupToBeDisabled();
            for (int i = 0; i < uiGroupToBeDisabled.Length; i++)
            {
                uiGroupToBeDisabled[i].IsEnabled = isTrue;
            }

            /// <summary>
            /// В зависимости от расширения файла возвращает группу элементов интерфейса, которую
            /// надо сделать неактивной
            /// </summary>
            Control[] ReturnUiGroupToBeDisabled()
            {
                switch (extension)
                {
                    case ".zip":
                        // Group 1
                        return new Control[1] { mW.btnTest };
                    case ".7z":
                        // Group 2
                        return new Control[1] { mW.btnTest };
                    case ".dtl":
                        // Group 3
                        return new Control[3] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnExtract};
                    case ".xls":
                        // Group 4
                        return new Control[3] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnExtract};
                    case ".xlsx":
                        // Group 5
                        return new Control[3] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnExtract};
                    default:
                        // Group 6
                        return new Control[3] {mW.tbDestinationPath,
                                               mW.btnBrowseDestination,
                                               mW.btnExtract};
                }
            }
            /// <summary>
            /// Возвращает группу элементов интерфейса, которую надо сделать активной
            /// (сначала включить все UI элементы, а потом уже нужные отключить)
            /// </summary>
            void EnableAllUiElements()
            {
                // Group 7
                Control[] uiGroupToBeEnabled = new Control[] {mW.tbDestinationPath,
                                                              mW.btnBrowseDestination,
                                                              mW.btnExtract};

                for (int i = 0; i < uiGroupToBeEnabled.Length; i++)
                {
                    uiGroupToBeEnabled[i].IsEnabled = true;
                }
            }
        }

    }
}
