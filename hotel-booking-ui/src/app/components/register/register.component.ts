import { ChangeDetectionStrategy, ChangeDetectorRef, Component } from '@angular/core';
import { Router } from '@angular/router';
import { HotelService } from '../../services/hotel.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  registerData = {
    name: '',
    email: '',
    password: '',
  };
  errorMessage = '';
  loading = false;

  constructor(
    private hotelService: HotelService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  onRegister() {
    this.loading = true;
    this.errorMessage = '';

    this.hotelService.register(this.registerData).subscribe({
      next: () => {
        this.loading = false;
        this.cdr.markForCheck();
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Registration failed. Try a different email.';
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }
}
