using System;
using System.Threading.Tasks;

namespace CMP5402.Week7
{
    public interface ICloudStore
    {
        Task<string> SignInAsync();
        // Null JSON means absent. Throw when the service could not answer.
        Task<(string json, string writeLock)> ReadAsync(string key);
        Task<string> WriteAsync(string key, string json, string writeLock);
        Task DeleteAsync(string key);
        Task DeleteAccountAsync();
    }

    public sealed class CloudStoreConflictException : Exception
    {
        public CloudStoreConflictException(Exception inner = null)
            : base("The cloud write lock is stale.", inner) { }
    }
}
