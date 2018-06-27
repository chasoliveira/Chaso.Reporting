using System;
using System.Collections.Generic;

namespace Chaso.Reporting
{
    public class EngineErrorEventArgs : EventArgs
    {
        public IList<ErrorMessage> ErrorMessages { get; private set; }
        public EngineErrorEventArgs(IList<ErrorMessage> errorMessage)
        {
            ErrorMessages = errorMessage;
        }
    }

    public class ErrorMessage
    {
        public ErrorMessage(string message)
        {
            Message = message;
        }
        public ErrorMessage(string message, string stackTrace) : this(message)
        {
            StackTrace = stackTrace;
        }
        public string Message { get; private set; }
        public string StackTrace { get; private set; }
    }
}
