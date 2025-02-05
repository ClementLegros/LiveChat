using System;

namespace LiveChat.Utilities
{
    public class DefaultException : Exception
    {
        public DefaultException() : base()
        {
            Logger.Error(base.StackTrace);

            Logger.Error(base.Message);

            Logger.StackTrace();
        }

        public DefaultException(string message) : base(message)
        {
            Logger.Error(base.StackTrace);

            Logger.Error(base.Message);

            Logger.StackTrace();
        }

        public DefaultException(Exception ex) : base(ex.Message)
        {
            Logger.Error(ex.StackTrace);

            Logger.Error(ex.Message);

            Logger.StackTrace();
        }
    }
}
