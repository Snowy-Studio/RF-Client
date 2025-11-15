using System;

namespace Localization.SevenZip
{
    public class SevenZipException : Exception
    {
        public SevenZipException()
        {
        }

        public SevenZipException(string message) : base(message)
        {
        }

        public SevenZipException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}