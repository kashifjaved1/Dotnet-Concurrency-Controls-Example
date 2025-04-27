using Dotnet_Concurrency_Controls.Data;
using Dotnet_Concurrency_Controls.Data.Entities;
using Dotnet_Concurrency_Controls.Hubs;
using Dotnet_Concurrency_Controls.Services.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Dotnet_Concurrency_Controls.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBookingLockService _lockService;
        private readonly IHubContext<BookingHub> _hubContext;

        public BookingsController(
            ApplicationDbContext context,
            IBookingLockService lockService,
            IHubContext<BookingHub> hubContext)
        {
            _context = context;
            _lockService = lockService;
            _hubContext = hubContext;
        }

        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            return View(await _context.Bookings.ToListAsync());
        }

        // GET: Bookings/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GuestName,CheckInDate,CheckOutDate")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(booking);
        }

        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (!await _lockService.AcquireLock(booking.Id, User.Identity.Name))
            {
                await _hubContext.Clients.All.SendAsync("LockUpdated", booking.Id);
                return RedirectToAction(nameof(Index));
            }

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,GuestName,CheckInDate,CheckOutDate,RowVersion")] Booking booking)
        {
            try
            {
                _context.Update(booking);
                await _context.SaveChangesAsync();
                await _lockService.ReleaseLock(booking.Id, User.Identity.Name);
                await _hubContext.Clients.All.SendAsync("LockUpdated", booking.Id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.Single();
                await entry.ReloadAsync();
                ModelState.AddModelError("", "Conflict: Data was modified by another user");
                return View(await entry.GetDatabaseValuesAsync());
            }
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(m => m.Id == id);
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(m => m.Id == id);
            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
