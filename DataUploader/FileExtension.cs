using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public static void SetRadioButtonState(string extension,
                                               RadioButton rbArchiveZip,
                                               RadioButton rbArchive7z,
                                               RadioButton rbDtl,
                                               RadioButton rbXls,
                                               RadioButton rbXlsx)
        {
            switch (extension)
            {
                case ".zip":
                    rbArchiveZip.IsChecked = true;
                    break;
                case ".7z":
                    rbArchive7z.IsChecked = true;
                    break;
                case ".dtl":
                    rbDtl.IsChecked = true;
                    break;
                case ".xls":
                    rbXls.IsChecked = true;
                    break;
                case ".xlsx":
                    rbXlsx.IsChecked = true;
                    break;
                default:
                    rbArchiveZip.IsChecked = false;
                    rbArchive7z.IsChecked = false;
                    rbDtl.IsChecked = false;
                    rbXlsx.IsChecked = false;
                    rbXls.IsChecked = false;
                    break;
            }
        }
        /// <summary>
        /// В зависимости от расширения файла "закрашивает" включает/отключает элементы
        /// интерфейса
        /// </summary>
        public static void EnableUiElements()
        {
            // (!)ToDo
        }
    }
}
