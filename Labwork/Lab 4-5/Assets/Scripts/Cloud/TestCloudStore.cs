#if UNITY_EDITOR || DEVELOPMENT_BUILD || WEEK7_DESKTOP_HARNESS
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CMP5402.Week7
{
    // In-memory service for a disposable development project. Drive it from the Unity main thread.
    public sealed class TestCloudStore : ICloudStore
    {
        public sealed class Gate
        {
            readonly TaskCompletionSource<bool> entered = new TaskCompletionSource<bool>();
            readonly TaskCompletionSource<bool> released = new TaskCompletionSource<bool>();
            public Task Entered => entered.Task;
            public void Release() => released.TrySetResult(true);

            internal Task WaitAsync()
            {
                entered.TrySetResult(true);
                return released.Task;
            }
        }

        readonly Dictionary<string, (string json, string writeLock)> items =
            new Dictionary<string, (string json, string writeLock)>();
        readonly Queue<Gate> reads = new Queue<Gate>();
        readonly Queue<Gate> writes = new Queue<Gate>();
        readonly List<(string key, string json, string writeLock)> writeCalls =
            new List<(string key, string json, string writeLock)>();
        int nextLock;

        public bool FailNextRead { get; set; }
        public bool FailNextWrite { get; set; }
        public int ReadCount { get; private set; }
        public int WriteCount => writeCalls.Count;
        public IReadOnlyList<(string key, string json, string writeLock)> WriteCalls => writeCalls;
        public int DeleteCount { get; private set; }

        public Gate HoldNextRead() => Enqueue(reads);
        public Gate HoldNextWrite() => Enqueue(writes);

        static Gate Enqueue(Queue<Gate> queue)
        {
            var gate = new Gate();
            queue.Enqueue(gate);
            return gate;
        }

        public string PeekJson(string key) => items.TryGetValue(key, out var item) ? item.json : null;

        // Simulate an independent device completing a write, without incrementing this client's call count.
        public string ExternalWrite(string key, string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            string writeLock = (++nextLock).ToString();
            items[key] = (json, writeLock);
            return writeLock;
        }

        public Task<string> SignInAsync() => Task.FromResult("week7-test-player");

        public async Task<(string json, string writeLock)> ReadAsync(string key)
        {
            ReadCount++;
            bool fail = FailNextRead;
            FailNextRead = false;
            items.TryGetValue(key, out var snapshot);
            if (reads.Count > 0) await reads.Dequeue().WaitAsync();
            if (fail) throw new IOException("Injected cloud read failure.");
            return snapshot;
        }

        public async Task<string> WriteAsync(string key, string json, string writeLock)
        {
            writeCalls.Add((key, json, writeLock));
            bool fail = FailNextWrite;
            FailNextWrite = false;
            if (writes.Count > 0) await writes.Dequeue().WaitAsync();
            if (fail) throw new IOException("Injected cloud write failure.");
            if (writeLock != null && (!items.TryGetValue(key, out var current) || current.writeLock != writeLock))
                throw new CloudStoreConflictException();
            return ExternalWrite(key, json);
        }

        public Task DeleteAsync(string key)
        {
            DeleteCount++;
            items.Remove(key);
            return Task.CompletedTask;
        }

        public Task DeleteAccountAsync()
        {
            items.Clear();
            return Task.CompletedTask;
        }
    }
}
#endif
