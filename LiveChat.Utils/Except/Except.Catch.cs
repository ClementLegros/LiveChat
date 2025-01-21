using System.Collections.Generic;

namespace System.Excepts
{
    public static partial class Except
    {
        public static TSource Catch<TSource, TEx>(this TSource result, TSource newResult) where TEx : Exception
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(TEx) != typeof(Exception) && typeof(TEx) != ex.GetType())
            {
                return result;
            }

            ThreadIdToException.Remove(ThreadId);

            return newResult;
        }

        public static TSource Catch<TSource, TEx>(this TSource result, Action<TEx> function) where TEx : Exception
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(TEx) != typeof(Exception) && typeof(TEx) != ex.GetType())
            {
                return result;
            }

            function((TEx)ex);

            ThreadIdToException.Remove(ThreadId);

            return default(TSource);
        }

        // public static TSource CatchReturn<TSource, TEx>(this TSource result, Func<TEx, TSource> function) where TEx : Exception
        // { 
        //     if (!ThreadIdToException.ContainsKey(ThreadId))
        //     {
        //         return result;
        //     }

        //     var ex = ThreadIdToException[ThreadId];

        //     if(typeof(TEx) != typeof(Exception) && typeof(TEx) != ex.GetType())
        //     {
        //         return result;
        //     }

        //     var res = function((TEx)ex);

        //     ThreadIdToException.Remove(ThreadId);

        //     return res;
        // } 

        public static TSource Catch<TSource>(this TSource result)
        {
            ThreadIdToException.Remove(ThreadId);

            return result;
        }

        public static TSource CatchReturn<TSource>(this TSource result, Func<Exception, TSource> function)
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var res = function(ThreadIdToException[ThreadId]);

            ThreadIdToException.Remove(ThreadId);

            return res;
        }

        public static TSource Catch<TSource>(this TSource result, Action<Exception> function)
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            function(ThreadIdToException[ThreadId]);

            ThreadIdToException.Remove(ThreadId);

            return default(TSource);
        }

#if NET8_0
        public static List<TSource> CatchAll<TSource>(this List<TSource> list, Action<List<Exception>> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return list;
            }

            function(ThreadIdToExceptions[ThreadId]);

            return list;
        }
#endif

        public static IEnumerable<TSource> CatchAll<TSource>(this IEnumerable<TSource> list, Action<List<Exception>> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return list;
            }

            function(ThreadIdToExceptions[ThreadId]);

            return list;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static int Catch<T>(this int result, Func<Exception, int> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            var res = function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return res;
        }

        public static int Catch<T>(this int result, Action<Exception> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return default(int);
        }

        public static int Catch<T>(this int result, int newResult) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            ThreadIdToException.Remove(ThreadId);

            return newResult;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static long Catch<T>(this long result, Func<Exception, long> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            var res = function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return res;
        }

        public static long Catch<T>(this long result, Action<Exception> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return default(long);
        }

        public static long Catch<T>(this long result, long newResult) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            ThreadIdToException.Remove(ThreadId);

            return newResult;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static float Catch<T>(this float result, Func<Exception, float> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            var res = function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return res;
        }

        public static float Catch<T>(this float result, Action<Exception> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return default(float);
        }

        public static float Catch<T>(this float result, float newResult) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            ThreadIdToException.Remove(ThreadId);

            return newResult;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static double Catch<T>(this double result, Func<Exception, double> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            var res = function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return res;
        }

        public static double Catch<T>(this double result, Action<Exception> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return default(double);
        }

        public static double Catch<T>(this double result, double newResult) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            ThreadIdToException.Remove(ThreadId);

            return newResult;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static bool Catch<T>(this bool result, Func<Exception, bool> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            var res = function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return res;
        }

        public static bool Catch<T>(this bool result, Action<Exception> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return default(bool);
        }

        public static bool Catch<T>(this bool result, bool newResult) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            ThreadIdToException.Remove(ThreadId);

            return newResult;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static char Catch<T>(this char result, Func<Exception, char> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            var res = function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return res;
        }

        public static char Catch<T>(this char result, Action<Exception> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return default(char);
        }

        public static char Catch<T>(this char result, char newResult) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            ThreadIdToException.Remove(ThreadId);

            return newResult;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static string Catch<T>(this string result, Func<Exception, string> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            var res = function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return res;
        }

        public static string Catch<T>(this string result, Action<Exception> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return default(string);
        }

        public static string Catch<T>(this string result, string newResult) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            ThreadIdToException.Remove(ThreadId);

            return newResult;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static object Catch<T>(this object result, Func<Exception, object> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            var res = function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return res;
        }

        public static object Catch<T>(this object result, Action<Exception> function) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            function((T)ex);

            ThreadIdToException.Remove(ThreadId);

            return default(object);
        }

        public static object Catch<T>(this object result, object newResult) where T : Exception, new()
        {
            if (!ThreadIdToException.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToException[ThreadId];

            if (typeof(T) != typeof(Exception) && typeof(T) != ex.GetType())
            {
                return result;
            }

            ThreadIdToException.Remove(ThreadId);

            return newResult;
        }
    }
}

namespace System.Excepts
{
    public static partial class Except
    {
        public static List<Exception> CatchAll<T>(this List<Exception> result, Action<T> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToExceptions[ThreadId];

            function((T)Activator.CreateInstance(typeof(T), ex));

            return result;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static List<int> CatchAll<T>(this List<int> result, Action<T> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToExceptions[ThreadId];

            function((T)Activator.CreateInstance(typeof(T), ex));

            return result;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static List<long> CatchAll<T>(this List<long> result, Action<T> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToExceptions[ThreadId];

            function((T)Activator.CreateInstance(typeof(T), ex));

            return result;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static List<float> CatchAll<T>(this List<float> result, Action<T> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToExceptions[ThreadId];

            function((T)Activator.CreateInstance(typeof(T), ex));

            return result;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static List<double> CatchAll<T>(this List<double> result, Action<T> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToExceptions[ThreadId];

            function((T)Activator.CreateInstance(typeof(T), ex));

            return result;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static List<bool> CatchAll<T>(this List<bool> result, Action<T> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToExceptions[ThreadId];

            function((T)Activator.CreateInstance(typeof(T), ex));

            return result;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static List<char> CatchAll<T>(this List<char> result, Action<T> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToExceptions[ThreadId];

            function((T)Activator.CreateInstance(typeof(T), ex));

            return result;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static List<string> CatchAll<T>(this List<string> result, Action<T> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToExceptions[ThreadId];

            function((T)Activator.CreateInstance(typeof(T), ex));

            return result;
        }
    }
}

// Generated
namespace System.Excepts
{
    public static partial class Except
    {
        public static List<object> CatchAll<T>(this List<object> result, Action<T> function)
        {
            if (!ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                return result;
            }

            var ex = ThreadIdToExceptions[ThreadId];

            function((T)Activator.CreateInstance(typeof(T), ex));

            return result;
        }
    }
}
