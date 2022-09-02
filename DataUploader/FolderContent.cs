using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DataUploader
{
    /// <summary>
    /// Вывод содержимого папки в элемент ListBox
    /// </summary>
    internal static class FolderContent
    {
        /// <summary>
        /// Вывод содержимого активной папки в элемент ListBox
        /// </summary>
        public static void ListContent(string currentDirectory, ListBox lbFolderContent)
        {
            string[] filePaths = Directory.GetFiles(currentDirectory);

            foreach (string path in filePaths)
            {
                lbFolderContent.Items.Add(ExtractFileName(path));
            }
        }
        /// <summary>
        /// Выделение имени файла из полного пути к этому файлу
        /// </summary>
        public static string ExtractFileName(string filePath)
        {
            int slashPosition = filePath.LastIndexOf("\\");
            return filePath.Substring(slashPosition + 1);
        }
    }
}
