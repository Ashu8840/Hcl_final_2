using HotelBookingAPI.Data;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly HotelBookingDbContext _db;

        public RoomsController(HotelBookingDbContext db)
        {
            _db = db;
        }

        // GET: api/rooms — Get all rooms with their hotel info
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rooms = await _db.Rooms
                .Include(r => r.Hotel)
                .Include(r => r.Facilities)
                .ToListAsync();
            return Ok(rooms);
        }

        // GET: api/rooms/5 — Get one room by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var room = await _db.Rooms
                .Include(r => r.Hotel)
                .Include(r => r.Facilities)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound(new { message = "Room not found" });

            return Ok(room);
        }

        // GET: api/rooms/available — Get only available rooms
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable()
        {
            var rooms = await _db.Rooms
                .Where(r => r.IsAvailable == true)
                .Include(r => r.Hotel)
                .Include(r => r.Facilities)
                .ToListAsync();
            return Ok(rooms);
        }

        // GET: api/rooms/search — Filter rooms by location, dates, price, capacity, amenities
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? location,
            [FromQuery] DateTime? checkIn,
            [FromQuery] DateTime? checkOut,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int? guests,
            [FromQuery] string? amenities)
        {
            if (checkIn.HasValue != checkOut.HasValue)
            {
                return BadRequest(new { message = "Both check-in and check-out dates are required when filtering by dates" });
            }

            if (checkIn.HasValue && checkOut <= checkIn)
            {
                return BadRequest(new { message = "Check-out must be after check-in" });
            }

            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            {
                return BadRequest(new { message = "Minimum price cannot be greater than maximum price" });
            }

            var query = _db.Rooms
                .Include(r => r.Hotel)
                .Include(r => r.Facilities)
                .Where(r => r.IsAvailable == true)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(location))
            {
                var locationPattern = $"%{location.Trim()}%";
                query = query.Where(r => r.Hotel.Location != null && EF.Functions.Like(r.Hotel.Location, locationPattern));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(r => r.PricePerNight >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(r => r.PricePerNight <= maxPrice.Value);
            }

            if (guests.HasValue && guests.Value > 0)
            {
                query = query.Where(r => r.Capacity >= guests.Value);
            }

            var amenityList = amenities?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.ToLowerInvariant())
                .Distinct()
                .ToList() ?? new List<string>();

            foreach (var amenity in amenityList)
            {
                query = query.Where(r => r.Facilities.Any(f => f.Name.ToLower() == amenity));
            }

            if (checkIn.HasValue && checkOut.HasValue)
            {
                var startDate = checkIn.Value.Date;
                var endDate = checkOut.Value.Date;

                query = query.Where(r => !_db.Bookings.Any(b =>
                    b.RoomId == r.Id &&
                    b.Status == "Confirmed" &&
                    b.CheckInDate < endDate &&
                    b.CheckOutDate > startDate));
            }

            var rooms = await query.ToListAsync();
            return Ok(rooms);
        }
    }
}
