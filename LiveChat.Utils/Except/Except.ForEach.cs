using System.Collections.Generic;

namespace System.Excepts
{
    public partial class Except
    {
        public static List<Exception> ForEach(IEnumerable<dynamic> list, Action<dynamic> function)
        {
            if (ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                ThreadIdToExceptions.Remove(ThreadId);
            }

            List<Exception> exceptions = new List<Exception>();

            foreach (dynamic obj in list)
            {
                try
                {
                    function(obj);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                try
                {
                    ThreadIdToExceptions.Add(ThreadId, exceptions);
                }
                catch
                {
                    ThreadIdToExceptions[ThreadId] = exceptions;
                }

                return exceptions;
            }

            return null;
        }

        public static List<Exception> ForEach<TSource>(IEnumerable<TSource> list, Action<TSource> function)
        {
            if (ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                ThreadIdToExceptions.Remove(ThreadId);
            }

            List<Exception> exceptions = new List<Exception>();

            foreach (TSource obj in list)
            {
                try
                {
                    function(obj);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                try
                {
                    ThreadIdToExceptions.Add(ThreadId, exceptions);
                }
                catch
                {
                    ThreadIdToExceptions[ThreadId] = exceptions;
                }

                return exceptions;
            }

            return null;
        }

        public static TSource ForEach<TSource>(TSource list, Func<TSource, TSource> function) where TSource : List<int>, new()
        {
            if (ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                ThreadIdToExceptions.Remove(ThreadId);
            }

            TSource results = new TSource();

            List<Exception> exceptions = new List<Exception>();

            foreach (int obj in list)
            {
                try
                {
                    results.AddRange(function(list));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                try
                {
                    ThreadIdToExceptions.Add(ThreadId, exceptions);
                }
                catch
                {
                    ThreadIdToExceptions[ThreadId] = exceptions;
                }
            }

            return results;
        }

        public static List<TSource> ForEach<TArg, TSource>(IEnumerable<TArg> list, Func<TArg, TSource> function)
        {
            if (ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                ThreadIdToExceptions.Remove(ThreadId);
            }

            List<TSource> results = new List<TSource>();

            List<Exception> exceptions = new List<Exception>();

            foreach (TArg obj in list)
            {
                try
                {
                    results.Add(function(obj));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                try
                {
                    ThreadIdToExceptions.Add(ThreadId, exceptions);
                }
                catch
                {
                    ThreadIdToExceptions[ThreadId] = exceptions;
                }
            }

            return results;
        }

        public static List<TSource> ForEach<TArg, TSource>(IEnumerable<TArg> list, Func<TArg, List<TSource>> function)
        {
            if (ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                ThreadIdToExceptions.Remove(ThreadId);
            }

            List<TSource> results = new List<TSource>();

            List<Exception> exceptions = new List<Exception>();

            foreach (TArg obj in list)
            {
                try
                {
                    results.AddRange(function(obj));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                try
                {
                    ThreadIdToExceptions.Add(ThreadId, exceptions);
                }
                catch
                {
                    ThreadIdToExceptions[ThreadId] = exceptions;
                }
            }

            return results;
        }

        public static IEnumerable<TSource> ForEachTry<TSource>(this IEnumerable<TSource> list, Action<TSource> function)
        {
            if (ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                ThreadIdToExceptions.Remove(ThreadId);
            }

            List<Exception> exceptions = new List<Exception>();

            foreach (TSource obj in list)
            {
                try
                {
                    function(obj);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                try
                {
                    ThreadIdToExceptions.Add(ThreadId, exceptions);
                }
                catch
                {
                    ThreadIdToExceptions[ThreadId] = exceptions;
                }
            }

            return list;
        }

        public static List<TSource> ForEachTry<TSource>(this List<TSource> list, Func<TSource, TSource> function)
        {
            if (ThreadIdToExceptions.ContainsKey(ThreadId))
            {
                ThreadIdToExceptions.Remove(ThreadId);
            }

            List<TSource> newList = new List<TSource>();

            List<Exception> exceptions = new List<Exception>();

            foreach (TSource obj in list)
            {
                try
                {
                    newList.Add(function(obj));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                try
                {
                    ThreadIdToExceptions.Add(ThreadId, exceptions);
                }
                catch
                {
                    ThreadIdToExceptions[ThreadId] = exceptions;
                }
            }

            return newList;
        }
    }
}
