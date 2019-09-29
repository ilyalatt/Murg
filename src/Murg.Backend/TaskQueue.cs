using System;
using System.Threading;
using System.Threading.Tasks;

namespace Murg.Backend
{
    public sealed class TaskQueue
    {
        readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task<T> Put<T>(Func<Task<T>> func)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                return await func();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}