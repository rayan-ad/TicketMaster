import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, LoginDto } from '../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  email = '';
  password = '';
  errorMessage = '';
  loading = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }
  }

  login(): void {
    if (!this.email || !this.password) {
      this.errorMessage = 'Veuillez remplir tous les champs.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const dto: LoginDto = {
      email: this.email,
      password: this.password
    };

    this.authService.login(dto).subscribe({
      next: (response) => {
        console.log('Connexion réussie:', response);
        this.router.navigate(['/']);
      },
      error: (error) => {
        console.error('Erreur de connexion:', error);
        this.errorMessage = error.error?.message || 'Email ou mot de passe incorrect.';
        this.loading = false;
      },
      complete: () => {
        this.loading = false;
      }
    });
  }
}
