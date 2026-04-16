using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelBookingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly HotelBookingDbContext _db;

        public BookingsController(HotelBookingDbContext db)
        {
            _db = db;
        }

        // GET: api/bookings — Get all bookings with room and user details
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _db.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Hotel)
                .Include(b => b.User)
                .ToListAsync();
            return Ok(bookings);
        }

        // GET: api/bookings/5 — Get a single booking
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var booking = await _db.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Hotel)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound(new { message = "Booking not found" });

            if (!IsAdmin() && booking.UserId != currentUserId.Value)
            {
                return Forbid();
            }

            return Ok(booking);
        }

        // GET: api/bookings/user/5 — Get all bookings for a user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            if (!IsAdmin() && currentUserId.Value != userId)
            {
                return Forbid();
            }

            var bookings = await _db.Bookings
                .Where(b => b.UserId == userId)
                .Include(b => b.Room)
                    .ThenInclude(r => r.Hotel)
                .ToListAsync();
            return Ok(bookings);
        }

        // POST: api/bookings — Create a new booking
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            // Check if room exists and is available
            var room = await _db.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == request.RoomId);

            if (room == null)
                return NotFound(new { message = "Room not found" });

            if (room.IsAvailable != true)
                return BadRequest(new { message = "Room is not available" });

            var checkInDate = request.CheckInDate.Date;
            var checkOutDate = request.CheckOutDate.Date;

            // Calculate total price based on number of nights
            var nights = (checkOutDate - checkInDate).Days;
            if (nights <= 0)
                return BadRequest(new { message = "Check-out must be after check-in" });

            var hasOverlappingBooking = await _db.Bookings.AnyAsync(b =>
                b.RoomId == request.RoomId &&
                b.Status == "Confirmed" &&
                b.CheckInDate < checkOutDate &&
                b.CheckOutDate > checkInDate);

            if (hasOverlappingBooking)
            {
                return Conflict(new { message = "Room is already booked for the selected dates" });
            }

            var booking = new Booking
            {
                UserId = currentUserId.Value,
                RoomId = request.RoomId,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                TotalPrice = room.PricePerNight * nights,
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            var createdBooking = await _db.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Hotel)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            return CreatedAtAction(nameof(GetById), new { id = booking.Id }, createdBooking);
        }

        // PUT: api/bookings/5/cancel — Cancel a booking
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound(new { message = "Booking not found" });

            if (!IsAdmin() && booking.UserId != currentUserId.Value)
            {
                return Forbid();
            }

            if (string.Equals(booking.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Booking is already cancelled" });
            }

            // Update booking status
            booking.Status = "Cancelled";

            await _db.SaveChangesAsync();
            return Ok(new { message = "Booking cancelled successfully" });
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }
    }
}
