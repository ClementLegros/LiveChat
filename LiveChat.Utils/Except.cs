using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveChat.Utilities.Excepts
{
    public static class Except
    {
        public static void Check(bool ok)
        {
            if (!ok)
            {
                throw new DefaultException();
            }
        }

        public static void Check(bool ok, string message)
        {
            if (!ok)
            {
                throw new DefaultException(message);
            }
        }

        public static void Check(bool ok, Exception exception)
        {
            if (!ok)
            {
                throw exception;
            }
        }

        public static void Check<T>(bool ok) where T : Exception, new()
        {
            if (!ok)
            {
                throw new T();
            }
        }

        public static void Check<T>(bool ok, string message) where T : Exception
        {
            if (!ok)
            {
                throw (T)Activator.CreateInstance(typeof(T), message);
            }
        }

        public static void Check(bool[] conditions)
        {
            foreach (bool ok in conditions)
            {
                if (!ok)
                {
                    throw new DefaultException();
                }
            }
        }

        public static void Check(bool[] conditions, Exception exception)
        {
            foreach (bool ok in conditions)
            {
                if (!ok)
                {
                    throw exception;
                }
            }
        }

        public static void Check<T>(bool[] conditions) where T : Exception, new()
        {
            foreach (bool ok in conditions)
            {
                if (!ok)
                {
                    throw new T();
                }
            }
        }

        public static void Check<T>(bool[] conditions, string message) where T : Exception
        {
            foreach (bool ok in conditions)
            {
                if (!ok)
                {
                    throw (T)Activator.CreateInstance(typeof(T), message);
                }
            }
        }

        public static Exception Try(Action function)
        { 
            try
            {
                function();
            }
            catch (Exception e)
            {
                return e;
            }

            return null;
        } 

        public static Exception Try(Action function, Action<Exception> catchHandler)
        {
            try
            {
                function();
            }
            catch (Exception e)
            {
                catchHandler(e);

                return e;
            }

            return null;
        }

        public static void Catch(this Exception e, Action<Exception> function)
        {
            if (e == null)
            {
                return;
            }

            function(e);
        }

        public static void Catch(this Exception e, Action function)
        {
            if (e == null)
            {
                return;
            }

            function();
        }

        public static void Throw(this Exception e)
        {
            if (e == null)
            {
                return;
            }

            throw e;
        }

        public static bool Try(Action function, List<Exception> ListOfExceptions)
        {
            Check(ListOfExceptions != null, "The given list of exceptions is null");

            try
            {
                function();
            }
            catch (Exception e)
            {
                ListOfExceptions.Add(e);

                return false;
            }

            return true;
        }

        public static bool Try(Action function, ref List<Exception> ListOfExceptions)
        {
            Check(ListOfExceptions != null, "The given list of exceptions is null");

            try
            {
                function();
            }
            catch (Exception e)
            {
                ListOfExceptions.Add(e);

                return false;
            }

            return true;
        }

        public static bool Try(Action function, ref List<object> ListOfExceptions)
        {
            Check(ListOfExceptions != null, "The given list of exceptions is null");

            try
            {
                function();
            }
            catch (Exception e)
            {
                ListOfExceptions.Add(e);

                return false;
            }

            return true;
        }

        public static List<Exception> ForEach<TSource>(IEnumerable<TSource> list, Action<TSource> function)
        {
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
                return exceptions;
            }

            return null;
        }

        public static void CatchAll(this List<Exception> exceptions, Action<List<Exception>> function)
        {
            if (exceptions == null)
            {
                return;
            }

            function(exceptions);
        }

        public static void ThrowAll(this List<Exception> exceptions)
        {
            if (exceptions == null)
            {
                return;
            }

            throw new AggregateException(exceptions);
        }
    }

    public static class Except<E> where E : Exception
    {
        public static void Check(bool ok)
        {
            if (!ok)
            {
                // https://stackoverflow.com/questions/31348525/create-instance-of-a-parameterized-generic-object-with-all-parameters-set-to-nul

                var nullParameters = typeof(E).GetConstructors().Single().GetParameters().Select(p => (object)null).ToArray();

                throw (E)Activator.CreateInstance(typeof(E), nullParameters);
            }
        }

        public static void Check(bool ok, Exception exception)
        {
            if (!ok)
            {
                throw exception;
            }
        }

        public static void Check<O>(bool ok, string message)
        {
            if (!ok)
            {
                throw (E)Activator.CreateInstance(typeof(E), message);
            }
        }

        public static void Check(bool ok, object obj)
        {
            if (!ok)
            {
                throw (E)Activator.CreateInstance(typeof(E), obj);
            }
        }

        public static void Check(bool ok, params object[] obj)
        {
            if (!ok)
            {
                throw (E)Activator.CreateInstance(typeof(E), obj);
            }
        }

        public static void Check<O>(bool ok)
        {
            if (!ok)
            {
                throw (E)Activator.CreateInstance(typeof(E), typeof(O));
            }
        }

        public static void Check(bool[] conditions)
        {
            foreach (bool ok in conditions)
            {
                if (!ok)
                {
                    var nullParameters = typeof(E).GetConstructors().Single().GetParameters().Select(p => (object)null).ToArray();

                    throw (E)Activator.CreateInstance(typeof(E), nullParameters);
                }
            }
        }

        public static void Check(bool[] conditions, Exception exception)
        {
            foreach (bool ok in conditions)
            {
                if (!ok)
                {
                    throw exception;
                }
            }
        }

        public static void Check(bool[] conditions, string message)
        {
            foreach (bool ok in conditions)
            {
                if (!ok)
                {
                    throw (E)Activator.CreateInstance(typeof(E), message);
                }
            }
        }

        public static void Check<O>(bool[] conditions)
        {
            foreach (bool ok in conditions)
            {
                if (!ok)
                {
                    throw (E)Activator.CreateInstance(typeof(E), typeof(O));
                }
            }
        }

        public static bool Try(Action function, List<E> ListOfExceptions)
        {
            Check(ListOfExceptions != null, "The given list of exceptions is null");

            try
            {
                function();
            }
            catch (Exception e)
            {
                ListOfExceptions.Add((E)e);

                return false;
            }

            return true;
        }
    }
}
