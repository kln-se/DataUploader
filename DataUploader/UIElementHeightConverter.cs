using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DataUploader
{
    // Класс, который используется для изменения размера ListView lvUploadedFiles.
    // c помощью него высота lvUploadedFiles будет на 25 pcx меньше, по сравнению с высотой элемента,
    // который берем за основу (см. XAML код в MainWindow)
    internal class UploadedFilesListViewHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double actualListViewHeight = (double)value - 25;
            if (actualListViewHeight <= 0)
            {
                return 0;
            }
            else
            {
                return actualListViewHeight;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Класс, который используется для изменения размера ListView lvImportedFiles.
    internal class ImportedFilesListViewHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double actualListViewHeight = (double)value - 100;
            if (actualListViewHeight <= 0)
            {
                return 0;
            }
            else
            {
                return actualListViewHeight;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Класс, который используется для изменения размера ListBox lbAvailibleFieldsInfo.
    internal class AvailibleFieldsInfoDataGridHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double actualListViewHeight = (double)value - 480;
            if (actualListViewHeight <= 0)
            {
                return 0;
            }
            else
            {
                return actualListViewHeight;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
