import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { HotelService } from './services/hotel.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'hotel-booking-ui';
  private authStateSubscription?: Subscription;

  constructor(
    public hotelService: HotelService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.authStateSubscription = this.hotelService.authState$.subscribe(() => {
      this.cdr.markForCheck();
    });
  }

  ngOnDestroy(): void {
    this.authStateSubscription?.unsubscribe();
  }

  onLogout() {
    this.hotelService.logout();
    this.cdr.markForCheck();
    this.router.navigate(['/auth/login']);
  }
}
