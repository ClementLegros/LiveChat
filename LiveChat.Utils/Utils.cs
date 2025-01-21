using System;
using System.IO;
using System.Excepts;
using System.Globalization;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalSup.Utilities
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

        public static double GetInterpolatedValue(Dictionary<int, double> values, int index)
        {
            if (values.ContainsKey(index))
            {
                return values[index];
            }

            int lowerKey = int.MinValue;
            int upperKey = int.MaxValue;

            foreach (int key in values.Keys)
            {
                if (key < index && key > lowerKey)
                {
                    lowerKey = key;
                }
                if (key > index && key < upperKey)
                {
                    upperKey = key;
                }
            }

            if (lowerKey == int.MinValue || upperKey == int.MaxValue)
            {
                throw new ArgumentException("Interpolation point is out of the range of the data");
            }

            double lowerValue = values[lowerKey];
            double upperValue = values[upperKey];

            return lowerValue + (upperValue - lowerValue) * (index - lowerKey) / (upperKey - lowerKey);
        }

        public static T[,] TransposeMatrix<T>(T[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int columns = matrix.GetLength(1);

            T[,] result = new T[columns, rows];

            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    result[c, r] = matrix[r, c];
                }
            }

            return result;
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

        public static string GetCaseByRc(string rc)
        {
            Logger.Enter();

            string rstabCase = "";

            switch(rc)
            {
                case "RC-1":
                    rstabCase = "OAB";
                    break;
                case "RC-2":
                    rstabCase = "C";
                    break;
                case "RC-3":
                    rstabCase = "D";
                    break;
                case "RC-4":
                    rstabCase = "ELUN";
                    break;
                case "RC-5":
                    rstabCase = "ELUF";
                    break;
                case "RC-6":
                    rstabCase = "STIFF";
                    break;
                default:
                    Logger.Leave();
                    throw new Exception("Error when finding the case by rc");
            }

            return rstabCase;
        }
    }
}
