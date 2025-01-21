using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Excepts
{
    public partial class Except
    {
        private static int FrameCount => new StackTrace(true).FrameCount;

        private static int ThreadId => Thread.CurrentThread.ManagedThreadId + FrameCount;

        private static Dictionary<int, Exception> ThreadIdToException = new Dictionary<int, Exception>();

        private static Dictionary<int, List<Exception>> ThreadIdToExceptions = new Dictionary<int, List<Exception>>();
    }

    public abstract class CheckException : Exception
    {
        public abstract dynamic ToTest { get; set; }

        public abstract bool Test();

        public new string Message;
    }

    public class NotNull : CheckException
    {
        public override dynamic ToTest { get; set; }

        public override bool Test() => ToTest != null;

        public new string Message = "The object is null";
    }

    public class StrictlyPositive : CheckException
    {
        public override dynamic ToTest { get; set; }

        public override bool Test() => ToTest > 0;

        public new string Message = "The value is not strictly positive";
    }

    public class NotBlank : CheckException
    {
        public override dynamic ToTest { get; set; }

        public override bool Test() => !string.IsNullOrEmpty(ToTest);

        public new string Message = "The value must not be blank";
    }

    public class NotEmpty : CheckException
    {
        public override dynamic ToTest { get; set; }

        public override bool Test() => ToTest.Count > 0;

        public new string Message = "The value must have at least one author";
    }
}
