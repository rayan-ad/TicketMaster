import { Routes } from '@angular/router';
import { EventDetails } from './event-details/event-details';
import { Home } from './home/home';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { MyReservationsComponent } from './my-reservations/my-reservations.component';
import { MyTicketsComponent } from './my-tickets/my-tickets.component';
import { ProfileComponent } from './profile/profile.component';
import { SearchResultsComponent } from './search-results/search-results.component';
import { AdminDashboardComponent } from './admin-dashboard/admin-dashboard.component';
import { VenueEvents } from './venue-events/venue-events';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
    { path: '', component: Home },
    { path: 'search', component: SearchResultsComponent },
    { path: 'event/:id', component: EventDetails },
    { path: 'venue/:id/events', component: VenueEvents },
    { path: 'login', component: LoginComponent },
    { path: 'register', component: RegisterComponent },
    { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
    { path: 'admin', component: AdminDashboardComponent, canActivate: [authGuard] },
    { path: 'my-reservations', component: MyReservationsComponent, canActivate: [authGuard] },
    { path: 'my-tickets', component: MyTicketsComponent, canActivate: [authGuard] },
];
