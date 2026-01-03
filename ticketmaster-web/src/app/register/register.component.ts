import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, RegisterDto } from '../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  name = '';
  email = '';
  password = '';
  confirmPassword = '';
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

  register(): void {
    if (!this.name || !this.email || !this.password || !this.confirmPassword) {
      this.errorMessage = 'Veuillez remplir tous les champs.';
      return;
    }

    // Validation email avec regex
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.email)) {
      this.errorMessage = 'Format d\'email invalide.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Les mots de passe ne correspondent pas.';
      return;
    }

    if (this.password.length < 6) {
      this.errorMessage = 'Le mot de passe doit contenir au moins 6 caractères.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const dto: RegisterDto = {
      name: this.name,
      email: this.email,
      password: this.password,
      role: 2  // 2 = Client enum value
    };

    this.authService.register(dto).subscribe({
      next: (response) => {
        console.log('Inscription réussie:', response);
        this.router.navigate(['/']);
      },
      error: (error) => {
        console.error('Erreur d\'inscription:', error);
        this.errorMessage = error.error?.message || 'Une erreur est survenue lors de l\'inscription.';
        this.loading = false;
      },
      complete: () => {
        this.loading = false;
      }
    });
  }
}
