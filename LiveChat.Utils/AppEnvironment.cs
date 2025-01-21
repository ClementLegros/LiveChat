using System;
using System.IO;
using System.Reflection;

namespace CalSup.Utilities
{
    public static class AppEnvironment
    {
        public static string AppName = "LiveChat";

        public static string ExeFilePath = Assembly.GetExecutingAssembly().Location;

        public static string WorkPath = Path.GetDirectoryName(ExeFilePath);

        public static string TempPath = Path.GetTempPath();

        private static DirectoryInfo _TempFolder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + @"Temp");

        public static DirectoryInfo TempFolder 
        {
            get
            {
                if (!_TempFolder.Exists)
                {
                    _TempFolder.Create();
                }

                return _TempFolder;
            }
        }

    }
}
