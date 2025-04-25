using LocalMessangerServer.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalMessangerServer
{
    public class LogService
    {
        private readonly AppDbContext _dbContext;
        private readonly Action<string>? _logToUI;

        public LogService(AppDbContext dbContext, Action<string>? logToUI = null)
        {
            _dbContext = dbContext;
            _logToUI = logToUI;
        }

        public async Task LogAsync(string message, string logLevel = "Info")
        {
            var log = new ServerLog
            {
                Message = message,
                LogLevel = logLevel,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.ServerLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            _logToUI?.Invoke($"[{logLevel}] {message} {DateTime.Now}");
        }

        public void Log(string message, string logLevel = "Info")
        {
            var log = new ServerLog
            {
                Message = message,
                LogLevel = logLevel,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.ServerLogs.Add(log);
            _dbContext.SaveChanges();

            _logToUI?.Invoke($"[{logLevel}] {message} {DateTime.Now}");
        }
    }
}
