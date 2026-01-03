import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  user: any = null;

  // Edit mode
  editMode = false;
  editName = '';
  editEmail = '';

  // Loading states
  saving = false;

  // Notifications
  showNotification = false;
  notificationMessage = '';
  notificationType: 'success' | 'error' = 'success';

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.user = user;
      if (user) {
        this.editName = user.name;
        this.editEmail = user.email;
      }
    });
  }

  toggleEditMode(): void {
    this.editMode = !this.editMode;
    if (!this.editMode) {
      // Reset values if cancelled
      this.editName = this.user.name;
      this.editEmail = this.user.email;
    }
  }

  saveProfile(): void {
    if (!this.editName.trim() || !this.editEmail.trim()) {
      this.showToast('Veuillez remplir tous les champs.', 'error');
      return;
    }

    this.saving = true;

    const updateDto = {
      name: this.editName.trim(),
      email: this.editEmail.trim()
    };

    this.authService.updateProfile(updateDto).subscribe({
      next: (response) => {
        this.editMode = false;
        this.saving = false;
        this.showToast('Profil mis à jour avec succès!', 'success');
        // Le user sera automatiquement mis à jour via currentUser$ observable
      },
      error: (err) => {
        console.error('Erreur lors de la mise à jour du profil:', err);
        const message = err.error?.message || 'Erreur lors de la mise à jour du profil.';
        this.showToast(message, 'error');
        this.saving = false;
      }
    });
  }

  private showToast(message: string, type: 'success' | 'error'): void {
    this.notificationMessage = message;
    this.notificationType = type;
    this.showNotification = true;

    setTimeout(() => {
      this.showNotification = false;
    }, 3000);
  }
}
