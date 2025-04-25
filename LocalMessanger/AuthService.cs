using LocalMessangerServer.EF;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LocalMessangerServer
{
    public class AuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly LogService _logService;

        public AuthService(AppDbContext dbContext, LogService logService)
        {
            _dbContext = dbContext;
            _logService = logService;
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            if (_dbContext.Users.Any(u => u.Username == username))
            {
                await _logService.LogAsync($"Registration failed: Username '{username}' is already taken.", "Warning");
                return false;
            }

            var user = new User
            {
                Username = username,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            await _logService.LogAsync($"User '{username}' registered successfully.", "Info");
            return true;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                await _logService.LogAsync($"Login failed: User '{username}' not found.", "Warning");
                return false;
            }

            var isValid = VerifyPassword(password, user.PasswordHash);
            if (!isValid)
            {
                await _logService.LogAsync($"Login failed: Invalid password for user '{username}'.", "Warning");
                return false;
            }

            await _logService.LogAsync($"User '{username}' logged in successfully.", "Info");
            return true;
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
