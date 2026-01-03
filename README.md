# TicketMaster - Système de Réservation de Billets

Application complète de gestion et réservation de billets pour événements (concerts, matchs sportifs, spectacles, etc.) avec interface en temps réel.

## Technologies Utilisées

### Backend
- **ASP.NET Core 8.0** - Framework Web API
- **Entity Framework Core** - ORM pour accès base de données
- **SQL Server** - Base de données relationnelle
- **SignalR** - Communication en temps réel
- **JWT** - Authentification et autorisation
- **BCrypt** - Hashage des mots de passe

### Frontend
- **Angular 18+** - Framework SPA
- **TypeScript** - Langage de développement
- **Tailwind CSS** - Framework CSS
- **SignalR Client** - Client temps réel
- **jsPDF + html2canvas** - Génération de PDF
- **RxJS** - Programmation réactive

---

## Architecture du Projet

```
TicketMaster/
├── TicketMaster/                    # Backend ASP.NET Core
│   ├── Controllers/                 # Endpoints API REST
│   ├── Services/                    # Logique métier
│   ├── Repositories/                # Accès données
│   ├── Models/                      # Entités de base de données
│   ├── DTOs/                        # Objets de transfert
│   ├── Common/                      # Constantes et utilitaires
│   ├── DataAccess/                  # DbContext et configuration EF
│   ├── Hubs/                        # Hubs SignalR
│   └── Enum/                        # Énumérations
│
└── ticketmaster-web/                # Frontend Angular
    └── src/app/
        ├── admin-dashboard/         # Interface administrateur
        ├── event-details/           # Page détails événement + réservation
        ├── home/                    # Page d'accueil
        ├── login/                   # Authentification
        ├── my-reservations/         # Réservations utilisateur
        ├── profile/                 # Profil utilisateur
        ├── search-results/          # Résultats de recherche
        ├── venue-events/            # Événements par lieu
        └── services/                # Services Angular (Auth, SignalR, etc.)
```

---

## Fonctionnalités Principales

### Gestion des Utilisateurs
- Inscription et connexion avec JWT
- 3 rôles: Client, Organisateur, Admin
- Profil utilisateur modifiable
- Dashboard administrateur pour gestion utilisateurs

### Gestion des Événements
- CRUD complet des événements (Admin/Organisateur)
- Recherche par nom, type, date
- Filtrage par type d'événement
- Pagination des résultats
- Upload d'images d'événements

### Gestion des Venues (Lieux)
- CRUD complet des venues (Admin/Organisateur)
- Génération automatique des sièges
- Zones de prix personnalisables
- Capacité configurable

### Système de Réservation
- Sélection interactive des sièges
- Réservations temporaires (15 minutes)
- Expiration automatique des réservations
- Mise à jour en temps réel de l'état des sièges (SignalR)
- Persistance de la sélection (LocalStorage)

### Paiement
- Paiement fictif (Card, PayPal, Bancontact)
- Génération automatique de tickets avec QR codes
- Téléchargement PDF des billets
- Facture récapitulative

### Temps Réel
- Mise à jour instantanée de l'état des sièges
- Notification des réservations/paiements
- Synchronisation multi-utilisateurs

---

## Installation et Configuration

### Prérequis

- **Backend:**
  - .NET 8.0 SDK ou supérieur
  - SQL Server (LocalDB ou instance complète)
  - Visual Studio 2022 ou VS Code

- **Frontend:**
  - Node.js 18+ et npm
  - Angular CLI 18+

### Étape 1: Configuration Backend

1. **Cloner le repository**
   ```bash
   cd TicketMaster/TicketMaster
   ```

2. **Configurer la base de données**

   Ouvrez `appsettings.json` et modifiez la connection string si nécessaire:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TicketMasterDb;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Restaurer les packages NuGet**
   ```bash
   dotnet restore
   ```

4. **Créer la base de données**

   Les migrations sont déjà créées. Appliquez-les:
   ```bash
   dotnet ef database update
   ```

   Cela va:
   - Créer la base de données `TicketMasterDb`
   - Créer toutes les tables
   - Insérer les données de test (seed data)

5. **Lancer l'API**
   ```bash
   dotnet run
   ```

   L'API sera accessible sur:
   - HTTPS: `https://localhost:7287`
   - HTTP: `http://localhost:5135`

### Étape 2: Configuration Frontend

1. **Aller dans le dossier frontend**
   ```bash
   cd ../ticketmaster-web
   ```

2. **Installer les dépendances**
   ```bash
   npm install
   ```

3. **Vérifier la configuration**

   Ouvrez `src/environments/environment.ts` et vérifiez les URLs:
   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'https://localhost:7287/api',
     signalRUrl: 'https://localhost:7287/hubs/seat'
   };
   ```

4. **Lancer l'application**
   ```bash
   ng serve
   ```

   L'application sera accessible sur: `http://localhost:4200`

---

## Utilisation

### Comptes de Test

Après le seed de la base de données, vous aurez accès à:

| Email | Mot de passe | Rôle |
|-------|--------------|------|
| admin@test.com | password123 | Admin |
| organizer@test.com | password123 | Organisateur |
| client@test.com | password123 | Client |

### Workflow Utilisateur Standard

1. **Inscription/Connexion**
   - Créer un compte ou se connecter
   - Le token JWT est stocké dans localStorage

2. **Recherche d'événements**
   - Page d'accueil affiche tous les événements
   - Utiliser la barre de recherche ou filtres

3. **Réservation**
   - Cliquer sur un événement
   - Sélectionner les sièges sur la carte interactive
   - Cliquer sur "Réserver" (crée une réservation temporaire de 15 min)

4. **Paiement**
   - Remplir les informations de paiement (fictif)
   - Confirmer le paiement
   - Télécharger les billets PDF avec QR codes

5. **Mes Réservations**
   - Voir toutes les réservations (En attente, Payées, Annulées)
   - Annuler une réservation en attente
   - Télécharger les billets

### Workflow Administrateur/Organisateur

1. **Dashboard Admin**
   - Accès via menu utilisateur → "Dashboard"
   - Statistiques globales

2. **Gestion des Événements**
   - Créer un événement (nom, date, type, venue, image)
   - Modifier/Supprimer un événement
   - Voir les statistiques de remplissage

3. **Gestion des Venues**
   - Créer un venue avec génération automatique de sièges
   - Définir les zones de prix
   - Modifier la capacité

4. **Gestion des Utilisateurs (Admin uniquement)**
   - Voir tous les utilisateurs
   - Changer les rôles
   - Supprimer des utilisateurs

---

## API Documentation (Swagger)

### Accéder à Swagger

Une fois l'API lancée, ouvrez votre navigateur:

**URL Swagger:** `https://localhost:7287/swagger`

### Utiliser Swagger pour Tester l'API

#### 1. Authentification

Pour tester les endpoints protégés:

1. **S'authentifier**
   - Allez dans `POST /api/Auth/login`
   - Cliquez sur "Try it out"
   - Entrez:
     ```json
     {
       "email": "admin@test.com",
       "password": "password123"
     }
     ```
   - Cliquez sur "Execute"
   - Copiez le `token` dans la réponse

2. **Ajouter le token**
   - Cliquez sur le bouton "Authorize" (cadenas en haut à droite)
   - Entrez: `Bearer [VOTRE_TOKEN]`
   - Exemple: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
   - Cliquez sur "Authorize"

#### 2. Endpoints Principaux

**Events**
- `GET /api/Event` - Liste paginée des événements
- `GET /api/Event/{id}` - Détails d'un événement avec stats
- `POST /api/Event` - Créer un événement (Admin/Organisateur)
- `PUT /api/Event/{id}` - Modifier un événement (Admin/Organisateur)
- `DELETE /api/Event/{id}` - Supprimer un événement (Admin)
- `GET /api/Event/{id}/seats` - Sièges d'un événement avec états

**Reservations**
- `POST /api/Reservation` - Créer une réservation
- `GET /api/Reservation/my` - Mes réservations
- `DELETE /api/Reservation/{id}` - Annuler une réservation

**Payment**
- `POST /api/Payment/process` - Traiter un paiement

**Venues**
- `GET /api/Venue` - Liste des venues
- `POST /api/Venue` - Créer un venue (Admin/Organisateur)

**Auth**
- `POST /api/Auth/register` - Inscription
- `POST /api/Auth/login` - Connexion
- `GET /api/Auth/me` - Infos utilisateur actuel

#### 3. Tester un Flow Complet

**Exemple: Réserver un billet**

1. **Login**
   ```
   POST /api/Auth/login
   Body: { "email": "client@ticketmaster.com", "password": "Client123!" }
   → Copier le token
   ```

2. **Voir les événements**
   ```
   GET /api/Event
   → Noter un eventId
   ```

3. **Voir les sièges disponibles**
   ```
   GET /api/Event/{eventId}/seats
   → Noter des seatIds libres (state: "Free")
   ```

4. **Créer une réservation**
   ```
   POST /api/Reservation
   Headers: Authorization: Bearer [TOKEN]
   Body: {
     "eventId": 1,
     "seatIds": [5, 6, 7]
   }
   → Noter le reservationId
   ```

5. **Payer la réservation**
   ```
   POST /api/Payment/process
   Headers: Authorization: Bearer [TOKEN]
   Body: {
     "reservationId": 1,
     "paymentMethod": "Card",
     "cardNumber": "1234567890123456",
     "cardName": "John Doe",
     "cardExpiry": "12/25",
     "cardCVV": "123"
   }
   → La réservation passe en statut "Paid" et les billets sont générés
   ```

---

## Structure de la Base de Données

### Tables Principales

**Users**
- Id, Name, Email, PasswordHash, Role

**Events**
- Id, Name, Date, Type, Description, VenueId, ImageEvent

**Venues**
- Id, Name, Address, City, Capacity

**Seats**
- Id, VenueId, Row, Number, PricingZoneId

**PricingZones**
- Id, VenueId, Name, Price, Color

**SeatReservationState**
- Id, EventId, SeatId, State (Free/ReservedTemp/Paid), UserId, ReservedAt, ExpiresAt

**Reservations**
- Id, UserId, EventId, Status (Pending/Paid/Cancelled), TotalAmount, CreatedAt, ExpiresAt

**ReservationSeats** (table de liaison)
- ReservationId, SeatId, PriceAtBooking

**Payments**
- Id, ReservationId, Amount, Method, Reference, Status, CreatedAt, ConfirmedAt

**Tickets**
- Id, ReservationId, SeatId, TicketNumber, QrCodeData, QrCodeUrl, GeneratedAt, IsUsed

### Diagramme ERD (Simplifié)

```
Users ─┬─< Reservations >─┬─ Events
       │                   │
       └─< SeatReservationState
                           │
                          Seats ─< Venues
                           │
                      PricingZones
```

---

## Patterns et Bonnes Pratiques

### Backend

**Architecture en Couches**
```
Controller → Service → Repository → Database
```

- **Controllers:** Gestion HTTP uniquement (codes status, validation ModelState)
- **Services:** Logique métier, calculs, orchestration
- **Repositories:** Accès données CRUD
- **Unit of Work:** Gestion des transactions

**DTOs (Data Transfer Objects)**
- Séparation entités/DTOs pour éviter références circulaires
- Propriétés calculées (stats) dans les DTOs
- Validation avec DataAnnotations

**Constantes Centralisées**
- `AppConstants.cs` pour toutes les magic numbers/strings
- Facilite la maintenance
- Configuration centralisée

### Frontend

**Services Angular**
- `AuthService` - Gestion authentification JWT
- `ReservationService` - API réservations
- `SignalRService` - Connexion temps réel

**State Management**
- LocalStorage pour persistance de session
- Subjects RxJS pour communication entre composants

**Composants Standalone**
- Angular 18+ avec composants standalone
- Pas de modules, imports directs

---

## Variables d'Environnement

### Backend (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "[VOTRE_CONNECTION_STRING]"
  },
  "Jwt": {
    "Key": "[VOTRE_CLE_SECRETE]",
    "Issuer": "TicketMasterAPI",
    "Audience": "TicketMasterClient",
    "ExpirationDays": 7
  },
  "AllowedHosts": "*"
}
```

### Frontend (environment.ts)

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7287/api',
  signalRUrl: 'https://localhost:7287/hubs/seat',
  defaultPageSize: 9,
  reservationExpirationMinutes: 15
};
```

---

## Troubleshooting

### Backend

**Erreur: Database connection failed**
- Vérifier que SQL Server est démarré
- Vérifier la connection string dans `appsettings.json`
- Essayer: `dotnet ef database update --force`

**Erreur: CORS policy**
- Vérifier que l'origine frontend est dans `Program.cs`:
  ```csharp
  builder.Services.AddCors(options =>
  {
      options.AddPolicy("AllowAngularApp", policy =>
      {
          policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
      });
  });
  ```

**Erreur: Unauthorized 401**
- Vérifier que le token JWT est valide
- Vérifier que le header `Authorization: Bearer [TOKEN]` est bien envoyé
- Vérifier que l'utilisateur a le bon rôle pour l'endpoint

### Frontend

**Erreur: Cannot connect to API**
- Vérifier que le backend tourne sur `https://localhost:7287`
- Vérifier `environment.ts`
- Désactiver le bloqueur HTTPS dans le navigateur (dev uniquement)

**Erreur: SignalR connection failed**
- Vérifier que SignalR est configuré dans `Program.cs`
- Vérifier la console navigateur pour erreurs WebSocket
- S'assurer que CORS autorise les connexions WebSocket

**PDF ne se génère pas**
- Vérifier la console pour erreurs CORS
- Les QR codes doivent être chargés avant génération PDF
- Timeout peut être nécessaire pour le rendu DOM

---

## Déploiement en Production

### Backend

1. **Publier l'application**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Configurer appsettings.Production.json**
   - Connection string vers DB production
   - Clé JWT sécurisée (générer une nouvelle)
   - Désactiver Swagger: `app.UseSwagger()` seulement si Development

3. **Déployer sur:**
   - Azure App Service
   - IIS
   - Docker container

### Frontend

1. **Build de production**
   ```bash
   ng build --configuration production
   ```

2. **Configurer environment.prod.ts**
   - Mettre l'URL de l'API production
   - Désactiver les logs de debug

3. **Déployer sur:**
   - Azure Static Web Apps
   - Netlify
   - Vercel
   - IIS (servir le dossier dist/)

---

## Scripts Utiles

### Backend

```bash
# Restaurer les packages
dotnet restore

# Compiler
dotnet build

# Lancer l'API
dotnet run

# Créer une migration
dotnet ef migrations add NomMigration

# Appliquer les migrations
dotnet ef database update

# Supprimer la dernière migration
dotnet ef migrations remove

# Recréer la DB from scratch
dotnet ef database drop
dotnet ef database update
```

### Frontend

```bash
# Installer les dépendances
npm install

# Lancer en dev
ng serve

# Build de production
ng build --prod

# Lancer les tests
ng test

# Générer un composant
ng generate component nom-composant

# Générer un service
ng generate service nom-service
```

---

## Contributeurs

- Développement: Rayan
- Framework: ASP.NET Core + Angular
- Année: 2025

---

## License

Ce projet est développé dans un cadre académique (BAC 3 - HELB PRIGOGINE).

---

## Support

Pour toute question ou problème:
1. Consulter la documentation Swagger: `https://localhost:7287/swagger`
2. Vérifier les logs backend dans la console
3. Vérifier la console navigateur (F12) pour le frontend
4. Vérifier que toutes les dépendances sont installées

---

**Dernière mise à jour:** Janvier 2025
