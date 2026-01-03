using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketMaster.DataAccess;
using TicketMaster.DTOs;
using TicketMaster.Models;
using TicketMaster.Services;

namespace TicketMaster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TicketMasterContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;

        public AuthController(
            TicketMasterContext context,
            IPasswordHasher passwordHasher,
            IJwtService jwtService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Inscrit un nouvel utilisateur.
        /// POST /api/Auth/register
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Vérifier si l'email existe déjà
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (existingUser != null)
            {
                return BadRequest(new { message = "Cet email est déjà utilisé." });
            }

            // Créer le nouvel utilisateur
            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email.ToLower(),
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Générer le token JWT
            var token = _jwtService.GenerateToken(user);

            var response = new AuthResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            return Ok(response);
        }

        /// <summary>
        /// Connecte un utilisateur existant.
        /// POST /api/Auth/login
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Trouver l'utilisateur par email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null)
            {
                return Unauthorized(new { message = "Email ou mot de passe incorrect." });
            }

            // Vérifier le mot de passe
            if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Email ou mot de passe incorrect." });
            }

            // Générer le token JWT
            var token = _jwtService.GenerateToken(user);

            var response = new AuthResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            return Ok(response);
        }

        /// <summary>
        /// Met à jour le profil de l'utilisateur connecté.
        /// PUT /api/Auth/profile
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<AuthResponseDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Récupérer l'ID de l'utilisateur depuis le token JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Utilisateur non authentifié." });
            }

            // Trouver l'utilisateur
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Utilisateur introuvable." });
            }

            // Vérifier si le nouvel email existe déjà (sauf si c'est le même)
            if (dto.Email.ToLower() != user.Email.ToLower())
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

                if (existingUser != null)
                {
                    return BadRequest(new { message = "Cet email est déjà utilisé par un autre utilisateur." });
                }
            }

            // Mettre à jour les informations
            user.Name = dto.Name;
            user.Email = dto.Email.ToLower();

            await _context.SaveChangesAsync();

            // Générer un nouveau token avec les infos à jour
            var token = _jwtService.GenerateToken(user);

            var response = new AuthResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            return Ok(response);
        }

        /// <summary>
        /// Liste tous les utilisateurs avec pagination (Admin seulement)
        /// GET /api/Auth/users
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetAllUsers(int pageNumber = 1, int pageSize = 10)
        {
            var allUsers = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    Role = u.Role.ToString()
                })
                .OrderBy(u => u.Id)
                .ToListAsync();

            var totalCount = allUsers.Count;
            var items = allUsers.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            var result = new PaginatedResult<object>
            {
                Items = items.Cast<object>().ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }

        /// <summary>
        /// Compte le nombre total d'utilisateurs (Admin seulement)
        /// GET /api/Auth/users/count
        /// </summary>
        [HttpGet("users/count")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetUserCount()
        {
            var count = await _context.Users.CountAsync();
            return Ok(new { count });
        }

        /// <summary>
        /// Met à jour le rôle d'un utilisateur (Admin seulement)
        /// PUT /api/Auth/users/{id}/role
        /// </summary>
        [HttpPut("users/{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Utilisateur introuvable." });
            }

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rôle mis à jour avec succès.", user = new { user.Id, user.Name, user.Email, Role = user.Role.ToString() } });
        }

        /// <summary>
        /// Supprime un utilisateur (Admin seulement)
        /// DELETE /api/Auth/users/{id}
        /// </summary>
        [HttpDelete("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Utilisateur introuvable." });
            }

            // Empêcher la suppression de soi-même
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id == currentUserId)
            {
                return BadRequest(new { message = "Vous ne pouvez pas supprimer votre propre compte." });
            }

            // Vérifier si l'utilisateur a des réservations
            var hasReservations = await _context.Reservations.AnyAsync(r => r.UserId == id);
            if (hasReservations)
            {
                return BadRequest(new { message = $"Impossible de supprimer {user.Name}. Cet utilisateur a des réservations existantes." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Utilisateur {user.Name} supprimé avec succès." });
        }
    }
}
