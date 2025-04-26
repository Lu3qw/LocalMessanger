
using LocalMessangerServer.EF;
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

        public async Task<bool> RegisterAsync(string username, string passwordHashFromClient)
        {
            if (_dbContext.Users.Any(u => u.Username == username))
            {
                await _logService.LogAsync($"Registration failed: '{username}' taken.", "Warning");
                return false;
            }

            _dbContext.Users.Add(new User
            {
                Username = username,
                PasswordHash = passwordHashFromClient,   
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
            await _logService.LogAsync($"User '{username}' registered.", "Info");
            return true;
        }

        public async Task<bool> LoginAsync(string username, string passwordHashFromClient)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                await _logService.LogAsync($"Login failed: '{username}' not found.", "Warning");
                return false;
            }

            if (user.PasswordHash != passwordHashFromClient)
            {
                await _logService.LogAsync($"Login failed: wrong password for '{username}'.", "Warning");
                return false;
            }

            await _logService.LogAsync($"User '{username}' logged in.", "Info");
            return true;
        }
    }
}
