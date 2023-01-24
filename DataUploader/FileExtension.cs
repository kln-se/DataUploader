using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DataUploader
{
    
    /// <summary>
    /// Содержит методы разархивирования и конвертации, которые используются в зависимости от
    /// расширения файла, подгруженного пользователем
    /// </summary>
    internal class FileExtension
    {
        // Функции разархивации и конвертирования стартуют процесс, который будет лежать здесь...
        // ...чтобы его можно было "убить"
        private Process _p = null;
        private bool _pKilled = false;

        /// <summary>
        /// Функция, которая вызывается, если пользователь подгрузил ZIP-архив.
        /// </summary>
        internal static bool ExtractArchiveZip(string filePath, string destinationPath)
        {
            try
            {
                int encodingCode = System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage;
                System.IO.Compression.ZipFile.ExtractToDirectory(filePath,
                                                                 destinationPath,
                                                                 Encoding.GetEncoding(encodingCode));
                // Распаковка выполнена успешно isCompleted = true
                return true;
            }
            catch (ArgumentException)
            {
                string messageBoxText = "Файл не выбран или неверно указана директория.";
                string caption = "Ошибка выбора файла";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                // Конвертирование не выполнено
                return false;
            }
            catch (System.IO.InvalidDataException)
            {
                string messageBoxText = "Выбранный файл не является допустимым ZIP-архивом.";
                string caption = "Ошибка типа файла";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                return false;
            }
            catch (System.IO.IOException)
            {
                string messageBoxText = "Файл с таким именем уже был извлечен или существует.";
                string caption = "Ошибка извлечения файла";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                return false;
            }
            catch (Exception ex)
            {
                string messageBoxText = ex.Message;
                string caption = "Ошибка";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);

                return false;
            }
        }


        /// <summary>
        /// Функция, которая вызывается, если пользователь подгрузил 7z-архив.
        /// </summary>
        internal bool ExtractArchive7za(string filePath, string destinationPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    
                    // Необходимые файлы (7za.exe и т.д.) добавлены в ресурсы проекта (/Properties)
                    FileName = Path.Combine(Directory.GetCurrentDirectory(), "Properties", "7za.exe"),
                    Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", filePath, destinationPath)
                };

                _p = Process.Start(psi);
                _p.WaitForExit();
                
                if (_pKilled == true)
                {
                    // Конвертирование не выполнено isCompleted = false
                    return false;
                }
                else
                {
                    // Конвертирование выполнено успешно isCompleted = true
                    return true;
                }
            }
            catch (Exception ex)
            {
                string messageBoxText = ex.Message;
                string caption = "Ошибка";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                // Распаковка не выполнена
                return false;
            }
        }

        /// <summary>
        /// Функция, которая вызывается, если пользователь подгрузил *.dtl файл.
        /// </summary>
        internal bool ConvertDtl(string filePath,
                                 string destinationPath,
                                 string selectedCsvEncoding,
                                 string choosenFileFormat,
                                 string showMilisec)
        {
            destinationPath = Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(filePath) + choosenFileFormat);

            var psi = new ProcessStartInfo();
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            // Необходимые файлы (EasyConverter.exe.exe и т.д.) добавлены в ресурсы проекта (/Properties)
            psi.FileName = Path.Combine(Directory.GetCurrentDirectory(), "Properties", "EasyConverter.exe");

            if (choosenFileFormat == ".csv")
            {
                switch (selectedCsvEncoding)
                {
                    case "ASCII":
                        psi.Arguments = string.Format("/ca {0} {1} {2}", showMilisec, filePath, destinationPath);
                        break;
                    case "UTF-8":
                        psi.Arguments = string.Format("/c8 {0} {1} {2}", showMilisec, filePath, destinationPath);
                        break;
                    case "Unicode":
                        psi.Arguments = string.Format("/cu {0} {1} {2}", showMilisec, filePath, destinationPath);
                        break;
                }
            }
            else
            {
                psi.Arguments = string.Format("{0} {1} {2}", showMilisec, filePath, destinationPath);
            }

            // Старт процесса разархивирования или конвертирования
            _p = Process.Start(psi);
                
            _p.WaitForExit();

            if (_pKilled == true)
            {
                // Конвертирование не выполнено, операция была отменена isCompleted = false
                return false;
            }
            else
            {
                // Конвертирование выполнено успешно isCompleted = true
                _p = null;
                return true;
            }
        }

        /// <summary>
        /// Считывает из Excel поля, проверяет есть ли такое поле в словаре, если нету, то
        /// добавляет в БД в раздел соответствующего файла и в словарь
        /// </summary>
        internal static Dictionary<string, int> CheckMissingFields(string filePath, int fileId, Dictionary<string, int> fieldsMap)
        {
            FileInfo fi = new FileInfo(filePath);
            List<string> fieldNames = new List<string>();

            using (ExcelPackage excelPackage = new ExcelPackage(fi))
            {
                for (int i = 1; i < excelPackage.Workbook.Worksheets.Count + 1; i++)
                {
                    ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[i];

                    for (int j = 1; j < worksheet.Dimension.Columns + 1; j++)
                    {
                        string tempFieldName = worksheet.Cells[1, j].Value.ToString();
                        if (tempFieldName == "Date" || tempFieldName == "Time" || tempFieldName == "Millisecond")
                        {
                            continue;
                        }
                        else
                        {
                            if (!fieldsMap.ContainsKey(tempFieldName) &&
                                !fieldNames.Contains(tempFieldName))
                            {
                                fieldNames.Add(tempFieldName);
                            }
                        }
                    }
                }
                fieldsMap = DataContext.InsertFields(fieldNames, fieldsMap, fileId);
            }
            return fieldsMap;
        }

        /// <summary>
        /// Извлечение данных из *.xslx файла.
        /// </summary>
        internal static List<DataModel.Measurement> ParseExcelFile(string filePath, Dictionary<string, int> fieldsMap, int transformerId, string phase, TimeSpan averagingRange)
        {           
            DateTime? averagingStopDateTime = null;

            DateTime currentDateValue;
            TimeSpan currentTimeValue;
            int currentMillisecondValue;

            // Для расчёта среднего значения
            float cellValuesSum = 0;
            int cellCount = 0;

            List<DataModel.Measurement> measurementsList = new List<DataModel.Measurement>();

            FileInfo fi = new FileInfo(filePath);

            using (ExcelPackage excelPackage = new ExcelPackage(fi))
            {
                int dateColumnIndex = -1;
                int timeColumnIndex = -1;
                int milisecondColumnIndex = -1;

                // Определим какие столбцы содержат "Date", "Time" и "Millisecond"
                for (int columnIndex = 1; columnIndex < excelPackage.Workbook.Worksheets[1].Dimension.Columns + 1; columnIndex++)
                {
                    if (excelPackage.Workbook.Worksheets[1].Cells[1, columnIndex].Value.ToString() == "Millisecond") { milisecondColumnIndex = columnIndex; break; }
                    if (excelPackage.Workbook.Worksheets[1].Cells[1, columnIndex].Value.ToString() == "Date") { dateColumnIndex = columnIndex; continue; }
                    if (excelPackage.Workbook.Worksheets[1].Cells[1, columnIndex].Value.ToString() == "Time") { timeColumnIndex = columnIndex; }
                }

                // В каких-то файлах есть "Millisecond" а в каких-то нету, надо от этого стлобца отстроиться
                if (milisecondColumnIndex < 0) { milisecondColumnIndex = timeColumnIndex; }

                int worksheetCount = excelPackage.Workbook.Worksheets.Count;
                ExcelWorksheet firstWorksheet = excelPackage.Workbook.Worksheets[1];
                int columnCount = firstWorksheet.Dimension.Columns;

                // Начиная со столбца, следующего за "Millisecond"
                for (int c = milisecondColumnIndex + 1; c < columnCount + 1; c++)
                {
                    string tempFieldName = firstWorksheet.Cells[1, c].Value.ToString();
                    int tempFieldId = fieldsMap[tempFieldName];

                    for (int w = 1; w < worksheetCount + 1; w++)
                    {
                        ExcelWorksheet tempWorksheet = excelPackage.Workbook.Worksheets[w];
                        int rowCount = tempWorksheet.Dimension.Rows;

                        for (int r = 2; r < rowCount + 1; r++)
                        {

                            currentDateValue = Convert.ToDateTime(tempWorksheet.Cells[r, 1].Value).Date;
                            currentTimeValue = Convert.ToDateTime(tempWorksheet.Cells[r, 2].Value).TimeOfDay;
                            currentMillisecondValue = Convert.ToInt32(tempWorksheet.Cells[r, 3].Value);
                            currentDateValue += currentTimeValue;
                            currentDateValue = currentDateValue.AddMilliseconds(currentMillisecondValue);

                            if (averagingStopDateTime == null)
                            {
                                averagingStopDateTime = GetAveragingIntervalStopPoint(currentDateValue, averagingRange);
                            }

                            // Считываем значение ячейки
                            float tempFieldValue = Convert.ToSingle(tempWorksheet.Cells[r, c].Value);

                            if (currentDateValue < averagingStopDateTime)
                            {
                                cellValuesSum += tempFieldValue;
                                cellCount += 1;

                                // Если конец файла
                                if (w == worksheetCount &&
                                    r == rowCount)
                                {
                                    float averageFieldValue = cellValuesSum / cellCount;

                                    measurementsList.Add(new DataModel.Measurement((DateTime)averagingStopDateTime - averagingRange,
                                                                                    averageFieldValue,
                                                                                    tempFieldId,
                                                                                    transformerId,
                                                                                    phase));

                                    averagingStopDateTime = null;
                                }
                            }
                            else
                            {
                                float averageFieldValue = cellValuesSum / cellCount;

                                measurementsList.Add(new DataModel.Measurement((DateTime)averagingStopDateTime - averagingRange,
                                                                                averageFieldValue,
                                                                                tempFieldId,
                                                                                transformerId,
                                                                                phase));

                                averagingStopDateTime = GetAveragingIntervalStopPoint(currentDateValue, averagingRange);
                                cellValuesSum = tempFieldValue;
                                cellCount = 1;
                            }
                        }
                    }
                }
                return measurementsList;
            }
        }

        /// <summary>
        /// Функция, которая определяет начало интервала усреднения и возвращает конец
        /// интервала усреднения
        /// </summary>
        /// <param name="firstValueDate">дата самого первого измерения для определения границ
        /// усреднения</param>
        /// <param name="averagingRange">диапазаон усреднения (например, каждые 15 мин)</param>
        /// <returns></returns>
        internal static DateTime GetAveragingIntervalStopPoint(DateTime firstValueDate, TimeSpan averagingRange)
        {
            DateTime date = firstValueDate.Date;
            TimeSpan time = new TimeSpan(firstValueDate.Hour, 0, 0);

            // Принятое начальное значение диапазона усреднения. Время с которым сравниваем,
            // чтобы опеределить действительное
            DateTime startDateTime = date + time;

            while (firstValueDate >= startDateTime)
            {
                startDateTime += averagingRange;
            }
            DateTime averagingStopDateTime = startDateTime;

            return averagingStopDateTime;
        }

        /// <summary>
        /// Функция для преждевременной остановки запущенного процесса
        /// </summary>
        internal void KillProcess()
        {
            if (_p != null)
            {
                _p.Kill();
                _pKilled = true;
                _p = null;
            }
        }
    }
}