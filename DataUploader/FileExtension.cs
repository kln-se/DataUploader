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
        public static void SetRadioButtonState(string extension, RadioButton rbArchive, RadioButton rbDtl, RadioButton rbXls, RadioButton rbXlsx)
        {
            switch (extension)
            {
                case ".zip":
                    rbArchive.IsChecked = true;
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
                    rbArchive.IsChecked = false;
                    rbDtl.IsChecked = false;
                    rbXlsx.IsChecked = false;
                    rbXlsx.IsChecked = false;
                    break;
            }
        }
    }
}
