namespace System.Excepts
{
    public partial class Except
    {
        public static TSource Run<TSource>(this bool ok, Func<TSource> function) => function();

        public static TSource Run<TSource>(this TSource result, Func<TSource, TSource> function) => function(result);

        public static bool Run(Action function)
        {
            function();

            return true;
        }

        public static TSource Do<TSource>(this bool ok, Func<TSource> function) => function();

        public static TSource Do<TSource>(this TSource result, Func<TSource, TSource> function) => function(result);

        public static bool Do(Action function)
        {
            function();

            return true;
        }

        public static Exception Try(Action function)
        {
            if (ThreadIdToException.ContainsKey(ThreadId))
            {
                ThreadIdToException.Remove(ThreadId);
            }

            try
            {
                function();
            }
            catch (Exception ex)
            {
                ThreadIdToException.Add(ThreadId, ex);

                return ex;
            }

            return null;
        }

        public static TSource Try<TSource>(Func<TSource> function)
        {
            if (ThreadIdToException.ContainsKey(ThreadId))
            {
                ThreadIdToException.Remove(ThreadId);
            }

            try
            {
                return function();
            }
            catch (Exception ex)
            {
                ThreadIdToException.Add(ThreadId, ex);

                return default(TSource);
            }
        }
    }
}
