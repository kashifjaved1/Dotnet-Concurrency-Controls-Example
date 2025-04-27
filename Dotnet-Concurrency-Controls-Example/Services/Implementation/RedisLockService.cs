using Dotnet_Concurrency_Controls.Services.Contract;
using StackExchange.Redis;

namespace Dotnet_Concurrency_Controls.Services.Implementation
{

    public class RedisLockService : IBookingLockService
    {
        private readonly IDatabase _redis;
        private readonly TimeSpan _lockDuration = TimeSpan.FromMinutes(5);

        public RedisLockService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public async Task<bool> AcquireLock(int bookingId, string userId)
        {
            var lockKey = $"booking:{bookingId}:lock";
            return await _redis.StringSetAsync(
                lockKey,
                userId,
                _lockDuration,
                When.NotExists);
        }

        public async Task ReleaseLock(int bookingId, string userId)
        {
            var lockKey = $"booking:{bookingId}:lock";
            var currentOwner = await _redis.StringGetAsync(lockKey);
            if (currentOwner == userId)
            {
                await _redis.KeyDeleteAsync(lockKey);
            }
        }

        public async Task<bool> IsLocked(int bookingId)
        {
            var lockKey = $"booking:{bookingId}:lock";
            return await _redis.KeyExistsAsync(lockKey);
        }
    }
}
