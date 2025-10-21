using System;
using System.Runtime.Serialization;

namespace Migration.Common.Log
{
    [Serializable]
    public class AbortMigrationException : Exception
    {

        public AbortMigrationException(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; private set; }
    }
}
