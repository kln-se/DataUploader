using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataUploader
{
    internal class FileExtension
    {
        public static string DetermineExtension(string filePath)
        {
            int dotPosition = filePath.LastIndexOf(".");
            return filePath.Substring(dotPosition);
        }
    }
}
