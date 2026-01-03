/**
 * Configuration d'environnement pour la production
 * A modifier avec les vraies URLs de production
 */
export const environment = {
  production: true,
  apiUrl: 'https://your-production-api.com/api',
  signalRUrl: 'https://your-production-api.com/hubs/seat',

  // Pagination
  defaultPageSize: 9,
  usersPageSize: 10,

  // Timeouts (millisecondes)
  toastDuration: 3000,
  toastDurationError: 6000,
  pdfRenderDelay: 100,

  // Reservation
  reservationExpirationMinutes: 15,

  // Zoom configuration
  minZoom: 0.6,
  maxZoom: 2.2,
  defaultZoom: 1,

  // PDF configuration
  pdfScale: 2,
  qrCodeSize: 200
};
