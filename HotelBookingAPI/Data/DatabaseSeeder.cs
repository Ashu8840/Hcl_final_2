using HotelBookingAPI.Models;
using HotelBookingAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAPI.Data;

public static class DatabaseSeeder
{
    private const int TargetHotels = 12;
    private const int TargetFacilities = 12;
    private const int TargetRooms = 15;
    private const int TargetUsers = 12;
    private const int TargetBookings = 12;

    public static async Task SeedAsync(
        HotelBookingDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

        var hotels = await SeedHotelsAsync(db, cancellationToken);
        var facilities = await SeedFacilitiesAsync(db, cancellationToken);
        var users = await SeedUsersAsync(db, configuration, cancellationToken);
        var rooms = await SeedRoomsAsync(db, hotels, cancellationToken);

        await SeedRoomFacilitiesAsync(db, facilities, cancellationToken);
        await SeedBookingsAsync(db, users, rooms, cancellationToken);

        logger.LogInformation(
            "Seed data counts => Hotels: {Hotels}, Rooms: {Rooms}, Facilities: {Facilities}, Users: {Users}, Bookings: {Bookings}",
            await db.Hotels.CountAsync(cancellationToken),
            await db.Rooms.CountAsync(cancellationToken),
            await db.Facilities.CountAsync(cancellationToken),
            await db.Users.CountAsync(cancellationToken),
            await db.Bookings.CountAsync(cancellationToken));
    }

    private static async Task<List<Hotel>> SeedHotelsAsync(HotelBookingDbContext db, CancellationToken cancellationToken)
    {
        var hotelSeeds = new (string Name, string Location, string Description)[]
        {
            ("Sunset Grand", "Mumbai", "Luxury stay near marine drive"),
            ("Royal Orchid", "Delhi", "Business and family friendly hotel"),
            ("Blue Lagoon", "Goa", "Beachfront resort with sea view"),
            ("Hilltop Haven", "Manali", "Scenic mountain retreat"),
            ("Urban Nest", "Bengaluru", "Modern city hotel"),
            ("Amber Residency", "Jaipur", "Heritage themed boutique hotel"),
            ("Lakeview Palace", "Udaipur", "Lakeside rooms and fine dining"),
            ("Cedar Point", "Shimla", "Cozy stay with pine valley views"),
            ("Palm Breeze", "Kochi", "Comfort hotel near waterfront"),
            ("Coral Heights", "Chennai", "Premium rooms near city center"),
            ("Silver Birch", "Pune", "Affordable comfort for long stays"),
            ("Maple Crown", "Hyderabad", "Executive hotel with conference halls"),
            ("Golden Horizon", "Ahmedabad", "Family hotel with rooftop lounge"),
            ("Riverstone Inn", "Rishikesh", "Relaxed riverside accommodation"),
            ("Cloudnine Suites", "Mysuru", "Spacious suites with amenities")
        };

        var existingHotels = await db.Hotels.OrderBy(h => h.Id).ToListAsync(cancellationToken);
        var required = Math.Max(0, TargetHotels - existingHotels.Count);

        for (var i = 0; i < required; i++)
        {
            var index = existingHotels.Count + i;
            var seed = hotelSeeds[index % hotelSeeds.Length];

            db.Hotels.Add(new Hotel
            {
                Name = seed.Name,
                Location = seed.Location,
                Description = seed.Description,
                CreatedAt = DateTime.UtcNow.AddDays(-(index + 1))
            });
        }

        if (required > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return await db.Hotels.OrderBy(h => h.Id).ToListAsync(cancellationToken);
    }

    private static async Task<List<Facility>> SeedFacilitiesAsync(HotelBookingDbContext db, CancellationToken cancellationToken)
    {
        var facilityNames = new[]
        {
            "Free WiFi",
            "Air Conditioning",
            "Breakfast Included",
            "Swimming Pool",
            "Gym",
            "Parking",
            "Room Service",
            "Airport Shuttle",
            "Laundry",
            "Restaurant",
            "Spa",
            "Conference Hall",
            "Balcony",
            "Mini Bar",
            "Pet Friendly"
        };

        var existingNames = await db.Facilities
            .Select(f => f.Name)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        var currentCount = existingNames.Count;

        foreach (var facilityName in facilityNames)
        {
            if (currentCount >= TargetFacilities)
            {
                break;
            }

            if (existingSet.Contains(facilityName))
            {
                continue;
            }

            db.Facilities.Add(new Facility { Name = facilityName });
            existingSet.Add(facilityName);
            currentCount++;
        }

        while (currentCount < TargetFacilities)
        {
            var generatedName = $"Facility {currentCount + 1}";
            if (existingSet.Contains(generatedName))
            {
                currentCount++;
                continue;
            }

            db.Facilities.Add(new Facility { Name = generatedName });
            existingSet.Add(generatedName);
            currentCount++;
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return await db.Facilities.OrderBy(f => f.Id).ToListAsync(cancellationToken);
    }

    private static async Task<List<User>> SeedUsersAsync(
        HotelBookingDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var adminEmails = configuration.GetSection("Jwt:AdminEmails").Get<string[]>() ?? Array.Empty<string>();
        var adminEmail = adminEmails.FirstOrDefault() ?? "admin@hotelbook.com";

        var userSeeds = new List<(string Name, string Email, string Password)>
        {
            ("Admin User", adminEmail, "Admin@123"),
            ("Ayush Guest", "guest1@hotelbook.com", "Password@123"),
            ("Riya Sharma", "guest2@hotelbook.com", "Password@123"),
            ("Arjun Singh", "guest3@hotelbook.com", "Password@123"),
            ("Neha Verma", "guest4@hotelbook.com", "Password@123"),
            ("Karan Mehta", "guest5@hotelbook.com", "Password@123"),
            ("Sneha Rao", "guest6@hotelbook.com", "Password@123"),
            ("Vivek Nair", "guest7@hotelbook.com", "Password@123"),
            ("Pooja Das", "guest8@hotelbook.com", "Password@123"),
            ("Rahul Jain", "guest9@hotelbook.com", "Password@123"),
            ("Isha Kapoor", "guest10@hotelbook.com", "Password@123"),
            ("Sahil Khan", "guest11@hotelbook.com", "Password@123"),
            ("Meera Bhat", "guest12@hotelbook.com", "Password@123"),
            ("Nitin Roy", "guest13@hotelbook.com", "Password@123"),
            ("Ananya Paul", "guest14@hotelbook.com", "Password@123")
        };

        var existingEmails = await db.Users
            .Select(u => u.Email)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existingEmails, StringComparer.OrdinalIgnoreCase);
        var currentCount = existingEmails.Count;

        foreach (var userSeed in userSeeds)
        {
            if (currentCount >= TargetUsers)
            {
                break;
            }

            if (existingSet.Contains(userSeed.Email))
            {
                continue;
            }

            db.Users.Add(new User
            {
                Name = userSeed.Name,
                Email = userSeed.Email.ToLowerInvariant(),
                PasswordHash = PasswordSecurity.HashPassword(userSeed.Password),
                CreatedAt = DateTime.UtcNow.AddDays(-(currentCount + 1))
            });

            existingSet.Add(userSeed.Email);
            currentCount++;
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return await db.Users.OrderBy(u => u.Id).ToListAsync(cancellationToken);
    }

    private static async Task<List<Room>> SeedRoomsAsync(
        HotelBookingDbContext db,
        IReadOnlyList<Hotel> hotels,
        CancellationToken cancellationToken)
    {
        if (hotels.Count == 0)
        {
            return await db.Rooms.OrderBy(r => r.Id).ToListAsync(cancellationToken);
        }

        var existingCount = await db.Rooms.CountAsync(cancellationToken);
        var required = Math.Max(0, TargetRooms - existingCount);

        for (var i = 0; i < required; i++)
        {
            var index = existingCount + i;
            var hotel = hotels[index % hotels.Count];

            db.Rooms.Add(new Room
            {
                HotelId = hotel.Id,
                RoomNumber = $"R-{hotel.Id:D2}-{index + 1:D3}",
                PricePerNight = 2200 + (index % 8) * 650,
                Capacity = 2 + (index % 4),
                IsAvailable = index % 5 != 0
            });
        }

        if (required > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return await db.Rooms.OrderBy(r => r.Id).ToListAsync(cancellationToken);
    }

    private static async Task SeedRoomFacilitiesAsync(
        HotelBookingDbContext db,
        IReadOnlyList<Facility> facilities,
        CancellationToken cancellationToken)
    {
        if (facilities.Count == 0)
        {
            return;
        }

        var rooms = await db.Rooms
            .Include(r => r.Facilities)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        if (rooms.Count == 0)
        {
            return;
        }

        var changed = false;

        foreach (var room in rooms)
        {
            const int targetFacilitiesPerRoom = 3;
            if (room.Facilities.Count >= targetFacilitiesPerRoom)
            {
                continue;
            }

            var startIndex = room.Id % facilities.Count;

            for (var offset = 0; offset < facilities.Count && room.Facilities.Count < targetFacilitiesPerRoom; offset++)
            {
                var facility = facilities[(startIndex + offset) % facilities.Count];
                if (room.Facilities.Any(f => f.Id == facility.Id))
                {
                    continue;
                }

                room.Facilities.Add(facility);
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedBookingsAsync(
        HotelBookingDbContext db,
        IReadOnlyList<User> users,
        IReadOnlyList<Room> rooms,
        CancellationToken cancellationToken)
    {
        if (users.Count == 0 || rooms.Count == 0)
        {
            return;
        }

        var existingCount = await db.Bookings.CountAsync(cancellationToken);
        var required = Math.Max(0, TargetBookings - existingCount);

        for (var i = 0; i < required; i++)
        {
            var index = existingCount + i;
            var user = users[index % users.Count];
            var room = rooms[(index * 2) % rooms.Count];

            var checkIn = DateTime.UtcNow.Date.AddDays(1 + index * 2);
            var nights = 1 + (index % 4);
            var checkOut = checkIn.AddDays(nights);
            var status = index % 4 == 0 ? "Cancelled" : "Confirmed";

            db.Bookings.Add(new Booking
            {
                UserId = user.Id,
                RoomId = room.Id,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                TotalPrice = room.PricePerNight * nights,
                Status = status,
                CreatedAt = DateTime.UtcNow.AddHours(-(index + 1))
            });
        }

        if (required > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}