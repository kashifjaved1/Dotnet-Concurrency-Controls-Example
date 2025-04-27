using Dotnet_Concurrency_Controls.Data;
using Dotnet_Concurrency_Controls.Data.Entities;
using Dotnet_Concurrency_Controls.Services.Contract;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Dotnet_Concurrency_Controls.Enums;

namespace Dotnet_Concurrency_Controls.Services.Implementation
{
    public class SqlLockService : IBookingLockService
    {
        private readonly ApplicationDbContext _context;
        private readonly TimeSpan _lockDuration = TimeSpan.FromMinutes(5);

        public SqlLockService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AcquireLock(int bookingId, string userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

            try
            {
                var booking = await _context.Bookings
                    .Where(x => x.Id == bookingId)
                    .FirstOrDefaultAsync();

                if (booking?.LockExpiry > DateTime.UtcNow) return false;

                booking.LockedBy = userId;
                booking.LockExpiry = DateTime.UtcNow.Add(_lockDuration);
                booking.LockType = nameof(LockTypes.SQL);

                _context.Update(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task ReleaseLock(int bookingId, string userId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking?.LockedBy == userId)
            {
                booking.LockedBy = null;
                booking.LockExpiry = null;

                _context.Update(booking);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsLocked(int bookingId)
        {
            return await _context.Bookings
                .AnyAsync(b => b.Id == bookingId &&
                              b.LockExpiry > DateTime.UtcNow);
        }
    }
}
