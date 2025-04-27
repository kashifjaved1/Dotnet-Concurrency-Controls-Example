using Dotnet_Concurrency_Controls.Services.Contract;
using System.Collections.Concurrent;

namespace Dotnet_Concurrency_Controls.Services.Implementation
{
    public class SemaphoreLockService : IBookingLockService
    {
        private static readonly ConcurrentDictionary<int, (SemaphoreSlim, string)> _locks = new();

        public async Task<bool> AcquireLock(int bookingId, string userId)
        {
            var entry = _locks.GetOrAdd(bookingId, _ => (new SemaphoreSlim(1, 1), null));

            if (await entry.Item1.WaitAsync(TimeSpan.Zero))
            {
                _locks[bookingId] = (entry.Item1, userId);
                return true;
            }

            return false;
        }

        public Task ReleaseLock(int bookingId, string userId)
        {
            if (_locks.TryGetValue(bookingId, out var entry) && entry.Item2 == userId)
            {
                entry.Item1.Release();
                _locks.TryRemove(bookingId, out _);
            }
            return Task.CompletedTask;
        }

        public Task<bool> IsLocked(int bookingId)
        {
            return Task.FromResult(_locks.ContainsKey(bookingId));
        }
    }
}

