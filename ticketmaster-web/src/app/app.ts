import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService, AuthResponse } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone : true,
  imports: [RouterOutlet, RouterLink, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  authService = inject(AuthService);
  router = inject(Router);
  currentUser: AuthResponse | null = null;
  showUserMenu = false;
  showEventsMenu = false;

  ngOnInit() {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  toggleUserMenu() {
    this.showUserMenu = !this.showUserMenu;
  }

  toggleEventsMenu() {
    this.showEventsMenu = !this.showEventsMenu;
  }

  closeEventsMenu() {
    this.showEventsMenu = false;
  }

  logout() {
    this.authService.logout();
    this.showUserMenu = false;
    this.router.navigate(['/']);  // Redirection vers l'accueil
  }
}
