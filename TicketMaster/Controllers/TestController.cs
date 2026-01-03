using Microsoft.AspNetCore.Mvc;
using TicketMaster.Services;

namespace TicketMaster.Controllers
{
    /// <summary>
    /// Controller de test pour vérifier le hash BCrypt
    /// À SUPPRIMER en production !
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly DataAccess.TicketMasterContext _context;

        public TestController(IPasswordHasher passwordHasher, DataAccess.TicketMasterContext context)
        {
            _passwordHasher = passwordHasher;
            _context = context;
        }

        /// <summary>
        /// Teste si le hash BCrypt du seeding correspond à "password123"
        /// GET /api/Test/check-hash
        /// </summary>
        [HttpGet("check-hash")]
        public IActionResult CheckHash()
        {
            const string testPassword = "password123";
            const string seedHash = "$2a$11$bS6s6WHQI5a9/p66D.rWXe9841mxPwieaiKA.Kt6pn/TVgyzOMLCu";

            bool isValid = _passwordHasher.VerifyPassword(testPassword, seedHash);

            if (isValid)
            {
                return Ok(new
                {
                    success = true,
                    message = $"✓ LE HASH EST CORRECT ! Le mot de passe '{testPassword}' fonctionne.",
                    hash = seedHash,
                    password = testPassword
                });
            }
            else
            {
                string newHash = _passwordHasher.HashPassword(testPassword);

                return Ok(new
                {
                    success = false,
                    message = $"✗ LE HASH EST INCORRECT ! Le mot de passe '{testPassword}' ne fonctionne PAS.",
                    oldHash = seedHash,
                    newHash = newHash,
                    password = testPassword,
                    instructions = new[]
                    {
                        "1. Copiez le newHash ci-dessus",
                        "2. Remplacez la ligne 151 dans DataAccess/TicketMasterContext.cs",
                        "3. Exécutez : dotnet ef database drop --force",
                        "4. Exécutez : dotnet ef database update",
                        "5. Redémarrez le serveur"
                    }
                });
            }
        }

        /// <summary>
        /// Génère un nouveau hash pour un mot de passe donné
        /// GET /api/Test/generate-hash?password=votreMotDePasse
        /// </summary>
        [HttpGet("generate-hash")]
        public IActionResult GenerateHash([FromQuery] string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return BadRequest(new { message = "Le paramètre 'password' est requis." });
            }

            string hash = _passwordHasher.HashPassword(password);

            return Ok(new
            {
                password = password,
                hash = hash,
                usage = "Copiez ce hash dans TicketMasterContext.cs pour le seeding"
            });
        }

        /// <summary>
        /// Liste tous les utilisateurs de la base de données (sans les mots de passe)
        /// GET /api/Test/list-users
        /// </summary>
        [HttpGet("list-users")]
        public IActionResult ListUsers()
        {
            var users = _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    PasswordHashFirst20 = u.PasswordHash.Substring(0, Math.Min(20, u.PasswordHash.Length)) + "...",
                    HashValid = _passwordHasher.VerifyPassword("password123", u.PasswordHash)
                })
                .ToList();

            return Ok(new
            {
                count = users.Count,
                users = users
            });
        }

        /// <summary>
        /// RÉPARE les hash des utilisateurs de test
        /// POST /api/Test/fix-user-passwords
        /// </summary>
        [HttpPost("fix-user-passwords")]
        public IActionResult FixUserPasswords()
        {
            const string newPassword = "password123";
            var fixedUsers = new List<string>();

            // Récupérer tous les utilisateurs
            var users = _context.Users.ToList();

            foreach (var user in users)
            {
                // Générer un nouveau hash
                user.PasswordHash = _passwordHasher.HashPassword(newPassword);
                fixedUsers.Add($"{user.Email} (ID: {user.Id})");
            }

            _context.SaveChanges();

            return Ok(new
            {
                message = "Mots de passe réparés avec succès !",
                password = newPassword,
                usersFixed = fixedUsers.Count,
                users = fixedUsers
            });
        }
    }
}
