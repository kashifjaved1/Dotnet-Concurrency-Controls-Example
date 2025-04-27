using Microsoft.AspNetCore.SignalR;

namespace Dotnet_Concurrency_Controls.Hubs
{
    public class BookingHub : Hub
    {
        public async Task SubscribeToLock(int bookingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
            await Clients.Group($"booking-{bookingId}")
                .SendAsync("LockUpdated", bookingId);
        }
    }
}
