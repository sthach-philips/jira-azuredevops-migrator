using System;
using System.Runtime.Serialization;

namespace Migration.Common.Log
{
    [Serializable]
    public class AttachmentNotFoundException : Exception
    {

        public AttachmentNotFoundException(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; private set; }
    }
}
