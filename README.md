Hotel Booking System (Full-Stack .NET + Angular)

A modern, scalable, and secure **Hotel Booking Web Application** built using **ASP.NET Core Web API, Angular, and MySQL**.
This system allows users to **search hotels, filter rooms, and book seamlessly**, while ensuring efficient backend operations and real-time availability updates.


# Project Overview

The Hotel Booking System is designed to solve real-world problems like:

*  Manual booking inefficiencies
*  Lack of real-time room availability
*  Poor filtering & search experience
*  Security concerns in booking systems

 This system provides a **centralized, secure, and user-friendly platform** for hotel discovery and booking.

---

 
# Key Features

# User Features

* User Registration & Login (JWT Authentication)
* Search hotels by location
* Filter rooms by facilities (WiFi, AC, etc.)
*  Select check-in & check-out dates
*  View available rooms in real-time
*  Book rooms instantly
*  Booking history (extendable)

---

# Tech Stack

Backend

* ASP.NET Core Web API (.NET 8/10)
* Entity Framework Core
* MySQL
* JWT Authentication
* Swagger (OpenAPI)

# Frontend

* Angular (Standalone Components)
* TypeScript
* HTML, CSS
* Angular Forms & HttpClient

---

Database Design

Main Entities:

* Users
* Hotels
* Rooms
* Bookings
* Facilities
* RoomFacilities (Many-to-Many)

Relationships:

* One Hotel → Many Rooms
* One User → Many Bookings
* One Room → Many Bookings
* Many Rooms ↔ Many Facilities

---

System Flow (Booking Process)

1. User logs in / registers
2. Searches hotels by location
3. Applies filters (optional)
4. Views available rooms
5. Selects room & dates
6. System checks availability
7. Booking is confirmed
8. Room availability updated automatically

---

API Endpoints (Sample)

### Auth

* POST /api/auth/login
* POST /api/auth/register

###  Hotels

* GET /api/hotels
* GET /api/hotels?location=city

###  Rooms

* GET /api/rooms
* GET /api/rooms/{id}

### Bookings

* POST /api/bookings
* GET /api/bookings/user/{userId}

---

##  Security Features

* JWT-based authentication
* Role-based authorization (User/Admin)
* Secure password hashing
* API protection using middleware

---

## Core Functional Logic

*  Room availability is checked before booking
*  Booking stored in database with status = **Confirmed**
*  Room availability updated (IsAvailable = false)
*  Prevents double booking

---

## Unique Selling Points (Hackathon Highlight)

*  Real-time room availability logic
*  Advanced filtering using facilities (JOIN operations)
*  Clean modular architecture
*  Production-ready authentication system
*  Scalable backend design

---

##  Future Enhancements (Stretch Features)

* Email confirmation with booking details
* Payment Gateway Integration (Stripe/Razorpay)
* Offers, coupons & loyalty rewards
* Admin dashboard (analytics & reports)
* Notifications system
* Mobile responsive UI

---

##  Testing

* Swagger UI for API testing

---

## Project Structure


Backend/
 ├── Controllers/
 ├── Models/
 ├── Services/
 ├── Data/
 ├── DTOs/

Frontend/
 ├── Components/
 ├── Services/
 ├── Models/
 ├── Pages/


---

##  Setup Instructions

###  Backend

1. Open project in Visual Studio
2. Configure appsettings.json (DB connection)
3. Run migrations
4. Press F5 to run API
5. Open Swagger

### Frontend

bash
cd frontend
npm install
ng serve


---

## Hackathon Impact

This project demonstrates:

* Real-world **problem-solving approach**
* Strong **full-stack development skills**
* Clean **database design & architecture**
* Practical **API security implementation**




