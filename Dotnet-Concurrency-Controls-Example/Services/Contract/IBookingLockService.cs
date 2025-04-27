namespace Dotnet_Concurrency_Controls.Services.Contract
{
    public interface IBookingLockService
    {
        Task<bool> AcquireLock(int bookingId, string userId);
        Task ReleaseLock(int bookingId, string userId);
        Task<bool> IsLocked(int bookingId);
    }
}
