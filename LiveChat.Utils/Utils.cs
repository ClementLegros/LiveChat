using System;
using System.IO;
using System.Excepts;
using System.Globalization;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
namespace LiveChat.Utilities
{
    public static class Utils
    {
        private static DirectoryInfo _TempFolder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "Temp");

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

        public static string GetFileType(byte[] fileBytes)
        {
            // Check for common file headers
            if (fileBytes.Length > 4)
            {
                // JPEG
                if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
                    return "jpg";
                // PNG
                if (fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47)
                    return "png";
                // GIF
                if (fileBytes[0] == 0x47 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46)
                    return "gif";
                // MP4
                if (fileBytes[4] == 0x66 && fileBytes[5] == 0x74 && fileBytes[6] == 0x79 && fileBytes[7] == 0x70)
                    return "mp4";
            }
            return "unknown";
        }

        public static bool IsValidFileType(string fileType)
        {
            var allowedTypes = new[] { "jpg", "png", "gif", "mp4" };
            return allowedTypes.Contains(fileType.ToLower());
        }


        public static double SafeParseDouble(string toParse)
        {
            if (toParse.Contains("EX"))
            {
                toParse = toParse.Replace("EX", "E");
            }

            NumberStyles style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol | NumberStyles.Float | NumberStyles.AllowExponent | NumberStyles.Any;

            IFormatProvider culture = CultureInfo.CreateSpecificCulture("en-US");

            bool isParsed = double.TryParse(toParse, style, culture, out double parsedValue);

            return isParsed ? parsedValue : 0;
        }

        public static int SafeParseInt(string toParse)
        {
            NumberStyles style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;

            IFormatProvider culture = CultureInfo.CreateSpecificCulture("en-US");

            bool isParsed = int.TryParse(toParse, style, culture, out int parsedValue);

            return isParsed ? parsedValue : 0;
        }

        public static bool SafeParseBool(string toParse)
        {
            bool isParsed = bool.TryParse(toParse, out bool parsedValue);

            return isParsed ? parsedValue : false;
        }

        public static T GetBySubstring<T>(string stringToSearch) where T : IConvertible
        {
            Except.Check(typeof(T).IsEnum);

            foreach (object enumValue in Enum.GetValues(typeof(T)))
            {
                if (stringToSearch.Contains(enumValue.ToString()) || enumValue.ToString().Contains(stringToSearch))
                {
                    return (T)Convert.ChangeType(enumValue, typeof(T));
                }
            }

            return (T)Convert.ChangeType(0, typeof(T));
        }

        public static string ToJson(object objet)
        {
            return JsonConvert.SerializeObject(objet, Formatting.Indented);
        }

        public static T GetByKeySubString<T>(string subKey, IDictionary dict)
        {
            foreach (string key in dict.Keys)
            {
                if (subKey.Contains(key) || key.Contains(subKey))
                {
                    return (T)dict[key];
                }
            }

            return default(T);
        }

        public static T SafeGetByIndex<T>(string key, IDictionary dict)
        {
            try
            {
                return (T)dict[key];
            }
            catch
            {
                return default(T);
            }
        }

        public static Action<Exception> ExceptionHandler = delegate (Exception e)
        {
            Logger.Error(e.Message);

            Logger.Error(e.StackTrace);
        };

        public static Action<List<Exception>> ExceptionsHandler = delegate (List<Exception> exceptions)
        {
            foreach (Exception e in exceptions)
            {
                Logger.Error(e.Message);

                Logger.Error(e.StackTrace);
            }
        };

        public static void AddEx(this List<Exception> list, Exception e)
        {
            if (e.GetType().GetProperty("InnerExceptions") == null && e.InnerException?.GetType().GetProperty("InnerExceptions") == null)
            {
                list.Add(e);

                return;
            }

            if (e.GetType().GetProperty("InnerExceptions") != null)
            {
                foreach (Exception ex in ((AggregateException)e).InnerExceptions)
                {
                    list.Add(ex);
                }
            }

            if (e.InnerException?.GetType().GetProperty("InnerExceptions") != null)
            {
                foreach (Exception ex in ((AggregateException)e.InnerException).InnerExceptions)
                {
                    list.Add(ex);
                }

            }
        }
    }
}
