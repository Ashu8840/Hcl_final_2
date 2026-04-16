import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { HotelService } from '../services/hotel.service';

export const authGuard: CanActivateFn = (route, state) => {
  const hotelService = inject(HotelService);
  const router = inject(Router);

  if (hotelService.isLoggedIn) {
    return true;
  } else {
    // Redirect to login page if not authenticated
    router.navigate(['/auth/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }
};
