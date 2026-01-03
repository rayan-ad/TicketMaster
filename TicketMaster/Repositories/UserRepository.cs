using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    public class UserRepository
    {
        public static List<User> Users = new()
        {
            new User
            {
                Id = 1,
                Name = "Admin Principal",
                Email = "admin@test.com",
                PasswordHash = "HASH_ADMIN", // juste symbolique pour l’instant
                Role = UserRole.Admin
            },
            new User
            {
                Id = 2,
                Name = "Organisateur Pro",
                Email = "orga@test.com",
                PasswordHash = "HASH_ORGA",
                Role = UserRole.Organisateur
            },
            new User
            {
                Id = 3,
                Name = "Client Démo",
                Email = "client@test.com",
                PasswordHash = "HASH_CLIENT",
                Role = UserRole.Client
            }
        };
    }
}
