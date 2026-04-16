import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HotelService } from './hotel.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private readonly hotelService: HotelService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.hotelService.authToken;

    const isAuthEndpoint =
      req.url.includes('/users/login') ||
      req.url.includes('/users/register') ||
      req.url.includes('/auth/login') ||
      req.url.includes('/auth/register');

    if (!token || isAuthEndpoint) {
      return next.handle(req);
    }

    const authRequest = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });

    return next.handle(authRequest);
  }
}
