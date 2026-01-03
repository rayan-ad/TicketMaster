import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

export interface AuthResponse {
  userId: number;
  name: string;
  email: string;
  role: string;
  token: string;
  expiresAt: string;
}

export interface RegisterDto {
  name: string;
  email: string;
  password: string;
  role?: number;
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface UpdateProfileDto {
  name: string;
  email: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = 'https://localhost:7287/api/Auth';
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'current_user';

  private currentUserSubject = new BehaviorSubject<AuthResponse | null>(this.getCurrentUser());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {}

  register(dto: RegisterDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/register`, dto)
      .pipe(tap(response => this.handleAuthSuccess(response)));
  }

  login(dto: LoginDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/login`, dto)
      .pipe(tap(response => this.handleAuthSuccess(response)));
  }

  updateProfile(dto: UpdateProfileDto): Observable<AuthResponse> {
    return this.http.put<AuthResponse>(`${this.API_URL}/profile`, dto)
      .pipe(tap(response => this.handleAuthSuccess(response)));
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getCurrentUser(): AuthResponse | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    return userJson ? JSON.parse(userJson) : null;
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    const user = this.getCurrentUser();

    if (!token || !user) {
      return false;
    }

    const expiresAt = new Date(user.expiresAt);
    return expiresAt > new Date();
  }

  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return user?.role === 'Admin';
  }

  isOrganisateur(): boolean {
    const user = this.getCurrentUser();
    return user?.role === 'Organisateur';
  }

  private handleAuthSuccess(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.USER_KEY, JSON.stringify(response));
    this.currentUserSubject.next(response);
  }
}
