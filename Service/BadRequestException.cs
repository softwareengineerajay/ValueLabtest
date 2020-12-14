using System;
using System.Runtime.Serialization;

namespace ValueLabtest.Service
{
    [Serializable]
    internal class BadRequestException : Exception
    {
        private string v;
        private object ex;

        public BadRequestException()
        {
        }

        public BadRequestException(string message) : base(message)
        {
        }

        public BadRequestException(string v, object ex)
        {
            this.v = v;
            this.ex = ex;
        }

        public BadRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BadRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}