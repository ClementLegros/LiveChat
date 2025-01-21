using System.Collections.Generic;

namespace System.Excepts
{
    public static partial class Except
    {
        public static void Throw<TEx>(TEx exArg) where TEx : Exception
        {
            throw exArg;
        }

        public static void Throw(List<Exception> exArg)
        {
            throw new AggregateException(exArg);
        }
    }
}

