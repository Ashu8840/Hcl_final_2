import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, map, of, tap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RoomSearchFilters {
  location?: string;
  checkIn?: string;
  checkOut?: string;
  minPrice?: number | null;
  maxPrice?: number | null;
  guests?: number | null;
  amenities?: string[];
}

@Injectable({
  providedIn: 'root',
})
export class HotelService {
  private readonly apiCandidates = [
    environment.apiBaseUrl,
    'http://localhost:5121/api',
    'https://localhost:7186/api',
    'http://localhost:5000/api',
  ].filter((value, index, array): value is string => Boolean(value) && array.indexOf(value) === index);

  private apiUrl = localStorage.getItem('apiBaseUrl') || this.apiCandidates[0];
  private readonly currentUserKey = 'currentUser';
  private readonly tokenKey = 'authToken';
  private readonly authStateSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  readonly authState$ = this.authStateSubject.asObservable();

  constructor(private http: HttpClient) {}

  // -------- AUTH STATE --------
  get isLoggedIn(): boolean {
    return this.hasValidToken();
  }

  get authToken(): string | null {
    if (!this.hasValidToken()) {
      return null;
    }

    return localStorage.getItem(this.tokenKey);
  }

  get currentUser(): any {
    const user = localStorage.getItem(this.currentUserKey);
    if (!user) {
      return null;
    }

    try {
      return JSON.parse(user);
    } catch {
      this.clearSession();
      this.authStateSubject.next(false);
      return null;
    }
  }

  get currentUserId(): number | null {
    const user = this.currentUser;
    return user?.id ? Number(user.id) : null;
  }

  logout() {
    this.clearSession();
    this.authStateSubject.next(false);
  }

  // -------- HOTELS --------
  getAllHotels(): Observable<any[]> {
    return this.withApiFallback((baseUrl) => this.http.get<any[]>(`${baseUrl}/hotels`)).pipe(
      map((data) => this.normalizeHotels(data)),
    );
  }

  getHotelById(id: number): Observable<any> {
    return this.withApiFallback((baseUrl) => this.http.get<any>(`${baseUrl}/hotels/${id}`)).pipe(
      map((data) => this.normalizeHotel(data)),
    );
  }

  getRoomsByHotel(hotelId: number): Observable<any[]> {
    return this.withApiFallback((baseUrl) =>
      this.http.get<any[]>(`${baseUrl}/hotels/${hotelId}/rooms`),
    ).pipe(map((data) => this.normalizeRooms(data)));
  }

  // -------- ROOMS --------
  getAllRooms(): Observable<any[]> {
    return this.withApiFallback((baseUrl) => this.http.get<any[]>(`${baseUrl}/rooms`)).pipe(
      map((data) => this.normalizeRooms(data)),
    );
  }

  searchRooms(filters: RoomSearchFilters): Observable<any[]> {
    let params = new HttpParams();

    if (filters.location?.trim()) {
      params = params.set('location', filters.location.trim());
    }

    if (filters.checkIn) {
      params = params.set('checkIn', filters.checkIn);
    }

    if (filters.checkOut) {
      params = params.set('checkOut', filters.checkOut);
    }

    if (typeof filters.minPrice === 'number') {
      params = params.set('minPrice', filters.minPrice.toString());
    }

    if (typeof filters.maxPrice === 'number') {
      params = params.set('maxPrice', filters.maxPrice.toString());
    }

    if (typeof filters.guests === 'number') {
      params = params.set('guests', filters.guests.toString());
    }

    if (filters.amenities && filters.amenities.length > 0) {
      params = params.set('amenities', filters.amenities.join(','));
    }

    return this.withApiFallback((baseUrl) =>
      this.http.get<any[]>(`${baseUrl}/rooms/search`, { params }),
    ).pipe(map((data) => this.normalizeRooms(data)));
  }

  getAvailableRooms(): Observable<any[]> {
    return this.withApiFallback((baseUrl) =>
      this.http.get<any[]>(`${baseUrl}/rooms/available`),
    ).pipe(map((data) => this.normalizeRooms(data)));
  }

  getRoomById(id: number): Observable<any> {
    return this.withApiFallback((baseUrl) => this.http.get<any>(`${baseUrl}/rooms/${id}`)).pipe(
      map((data) => this.normalizeRoom(data)),
    );
  }

  // -------- BOOKINGS --------
  getAllBookings(): Observable<any[]> {
    return this.withApiFallback((baseUrl) => this.http.get<any[]>(`${baseUrl}/bookings`)).pipe(
      map((data) => this.normalizeBookings(data)),
    );
  }

  getBookingsByUser(userId: number): Observable<any[]> {
    return this.withApiFallback((baseUrl) =>
      this.http.get<any[]>(`${baseUrl}/bookings/user/${userId}`),
    ).pipe(map((data) => this.normalizeBookings(data)));
  }

  getMyBookings(): Observable<any[]> {
    const userId = this.currentUserId;
    if (!userId) {
      return of([]);
    }

    return this.getBookingsByUser(userId);
  }

  createBooking(booking: any): Observable<any> {
    return this.withApiFallback((baseUrl) =>
      this.http.post<any>(`${baseUrl}/bookings`, booking),
    ).pipe(map((data) => this.normalizeBooking(data)));
  }

  cancelBooking(id: number): Observable<any> {
    return this.withApiFallback((baseUrl) =>
      this.http.put<any>(`${baseUrl}/bookings/${id}/cancel`, {}),
    );
  }

  // -------- USERS --------
  register(user: any): Observable<any> {
    return this.withApiFallback((baseUrl) =>
      this.http.post<any>(`${baseUrl}/auth/register`, user),
    ).pipe(
      map((response) => this.normalizeAuthResponse(response)),
      tap((response) => this.setSession(response)),
    );
  }

  login(credentials: any): Observable<any> {
    return this.withApiFallback((baseUrl) =>
      this.http.post<any>(`${baseUrl}/auth/login`, credentials),
    ).pipe(
      map((response) => this.normalizeAuthResponse(response)),
      tap((response) => this.setSession(response)),
    );
  }

  private setSession(response: any) {
    if (!response?.token) {
      return;
    }

    const sessionUser = {
      id: response.userId,
      name: response.name,
      email: response.email,
      role: response.role,
      expiresAtUtc: response.expiresAtUtc,
    };

    localStorage.setItem(this.currentUserKey, JSON.stringify(sessionUser));
    localStorage.setItem(this.tokenKey, response.token);
    this.authStateSubject.next(true);
  }

  private withApiFallback<T>(requestFactory: (baseUrl: string) => Observable<T>): Observable<T> {
    const orderedCandidates = this.getOrderedApiCandidates();
    return this.tryApiCandidates(orderedCandidates, 0, requestFactory);
  }

  private tryApiCandidates<T>(
    candidates: string[],
    index: number,
    requestFactory: (baseUrl: string) => Observable<T>,
  ): Observable<T> {
    const candidate = candidates[index];

    return requestFactory(candidate).pipe(
      tap(() => this.setActiveApiUrl(candidate)),
      catchError((error) => {
        if (index < candidates.length - 1 && this.shouldTryNextApi(error)) {
          return this.tryApiCandidates(candidates, index + 1, requestFactory);
        }

        return throwError(() => error);
      }),
    );
  }

  private getOrderedApiCandidates(): string[] {
    const uniqueCandidates = [
      this.apiUrl,
      ...this.apiCandidates.filter((url) => url !== this.apiUrl),
    ];
    return [...new Set(uniqueCandidates)];
  }

  private setActiveApiUrl(url: string) {
    this.apiUrl = url;
    localStorage.setItem('apiBaseUrl', url);
  }

  private shouldTryNextApi(error: unknown): boolean {
    if (!(error instanceof HttpErrorResponse)) {
      return false;
    }

    return error.status === 0 || error.status === 404 || error.status >= 500;
  }

  private normalizeHotels(input: any): any[] {
    return this.toArray(input).map((hotel) => this.normalizeHotel(hotel));
  }

  private normalizeRooms(input: any): any[] {
    return this.toArray(input).map((room) => this.normalizeRoom(room));
  }

  private normalizeBookings(input: any): any[] {
    return this.toArray(input).map((booking) => this.normalizeBooking(booking));
  }

  private normalizeAuthResponse(response: any) {
    return {
      message: response?.message ?? response?.Message ?? 'Success',
      token: response?.token ?? response?.Token ?? '',
      expiresAtUtc: response?.expiresAtUtc ?? response?.ExpiresAtUtc,
      userId: Number(response?.userId ?? response?.UserId ?? 0),
      name: response?.name ?? response?.Name ?? '',
      email: response?.email ?? response?.Email ?? '',
      role: response?.role ?? response?.Role ?? 'User',
    };
  }

  private normalizeHotel(hotel: any) {
    if (!hotel) {
      return null;
    }

    return {
      id: Number(hotel.id ?? hotel.Id ?? 0),
      name: hotel.name ?? hotel.Name ?? 'Hotel',
      location: hotel.location ?? hotel.Location ?? '',
      description: hotel.description ?? hotel.Description ?? '',
      createdAt: hotel.createdAt ?? hotel.CreatedAt,
    };
  }

  private normalizeRoom(room: any) {
    if (!room) {
      return null;
    }

    const facilitiesRaw = room.facilities ?? room.Facilities;

    return {
      id: Number(room.id ?? room.Id ?? 0),
      hotelId: Number(room.hotelId ?? room.HotelId ?? 0),
      roomNumber: room.roomNumber ?? room.RoomNumber ?? '-',
      pricePerNight: Number(room.pricePerNight ?? room.PricePerNight ?? 0),
      capacity: Number(room.capacity ?? room.Capacity ?? 0),
      isAvailable: Boolean(room.isAvailable ?? room.IsAvailable),
      hotel: this.normalizeHotel(room.hotel ?? room.Hotel),
      facilities: this.toArray(facilitiesRaw).map((facility) => this.normalizeFacility(facility)),
    };
  }

  private normalizeFacility(facility: any) {
    return {
      id: Number(facility?.id ?? facility?.Id ?? 0),
      name: facility?.name ?? facility?.Name ?? 'Amenity',
    };
  }

  private normalizeBooking(booking: any) {
    if (!booking) {
      return null;
    }

    return {
      id: Number(booking.id ?? booking.Id ?? 0),
      userId: Number(booking.userId ?? booking.UserId ?? 0),
      roomId: Number(booking.roomId ?? booking.RoomId ?? 0),
      checkInDate: booking.checkInDate ?? booking.CheckInDate,
      checkOutDate: booking.checkOutDate ?? booking.CheckOutDate,
      totalPrice: Number(booking.totalPrice ?? booking.TotalPrice ?? 0),
      status: booking.status ?? booking.Status ?? 'Confirmed',
      createdAt: booking.createdAt ?? booking.CreatedAt,
      room: this.normalizeRoom(booking.room ?? booking.Room),
      user: booking.user ?? booking.User,
    };
  }

  private toArray<T>(value: T[] | null | undefined): T[] {
    return Array.isArray(value) ? value : [];
  }

  private hasValidToken(): boolean {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) {
      return false;
    }

    const isExpired = this.isTokenExpired(token);
    if (isExpired) {
      this.clearSession();
      this.authStateSubject.next(false);
      return false;
    }

    return true;
  }

  private clearSession() {
    localStorage.removeItem(this.currentUserKey);
    localStorage.removeItem(this.tokenKey);
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload?.exp as number | undefined;

      if (!exp) {
        return true;
      }

      const expiresAtMs = exp * 1000;
      return Date.now() >= expiresAtMs;
    } catch {
      return true;
    }
  }
}
