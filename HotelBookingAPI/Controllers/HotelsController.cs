using HotelBookingAPI.Data;
using HotelBookingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HotelsController : ControllerBase
    {
        private readonly HotelBookingDbContext _db;

        public HotelsController(HotelBookingDbContext db)
        {
            _db = db;
        }

        // GET: api/hotels — Get all hotels
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var hotels = await _db.Hotels.ToListAsync();
            return Ok(hotels);
        }

        // GET: api/hotels/5 — Get one hotel by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var hotel = await _db.Hotels.FindAsync(id);
            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            return Ok(hotel);
        }

        // GET: api/hotels/5/rooms — Get all rooms for a hotel
        [HttpGet("{id}/rooms")]
        public async Task<IActionResult> GetRooms(int id)
        {
            var exists = await _db.Hotels.AnyAsync(h => h.Id == id);
            if (!exists)
                return NotFound(new { message = "Hotel not found" });

            var rooms = await _db.Rooms
                .Where(r => r.HotelId == id)
                .Include(r => r.Facilities) // include amenities
                .ToListAsync();

            return Ok(rooms);
        }

        // POST: api/hotels — Create a new hotel
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Hotel hotel)
        {
            hotel.CreatedAt = DateTime.UtcNow;
            _db.Hotels.Add(hotel);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = hotel.Id }, hotel);
        }
    }
}
