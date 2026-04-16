import { ChangeDetectionStrategy, ChangeDetectorRef, Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HotelService } from '../../services/hotel.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  loginData = {
    email: '',
    password: '',
  };
  errorMessage = '';
  loading = false;
  returnUrl = '/';

  constructor(
    private hotelService: HotelService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
  ) {
    this.returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
  }

  onLogin() {
    this.loading = true;
    this.errorMessage = '';

    this.hotelService.login(this.loginData).subscribe({
      next: () => {
        this.loading = false;
        this.cdr.markForCheck();
        this.router.navigateByUrl(this.returnUrl);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Login failed. Please try again.';
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }
}
