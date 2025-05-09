
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


        public void BanUser(string username)
        {
            using (var db = new AppDbContext())
            {
                if (db.BannedUsers.Any(b => b.Username == username)) return;
                db.BannedUsers.Add(new BannedUser
                {
                    Username = username,
                    BanDate = DateTime.Now
                });
                db.SaveChanges();
            }
        }

        public void UnbanUser(string username)
        {
            using (var db = new AppDbContext())
            {
                var ban = db.BannedUsers.FirstOrDefault(b => b.Username == username);
                if (ban != null)
                {
                    db.BannedUsers.Remove(ban);
                    db.SaveChanges();
                }
            }
        }

        public bool IsUserBanned(string username)
        {
            using (var db = new AppDbContext())
            {
                var ban = db.BannedUsers.FirstOrDefault(b => b.Username == username);
                if (ban == null) return false;

                if (ban.BanDate.AddMonths(1) < DateTime.Now)
                {
                    db.BannedUsers.Remove(ban);
                    var user = db.Users.FirstOrDefault(u => u.Username == username);
                    if (user != null) db.Users.Remove(user);
                    db.SaveChanges();
                    return false;
                }
                return true;
            }
        }

    }
}
