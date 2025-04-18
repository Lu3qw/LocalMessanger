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

        public LogService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task LogAsync(string message, string logLevel = "Info")
        {
            var log = new ServerLog
            {
                Message = message,
                LogLevel = logLevel,
                Timestamp = DateTime.UtcNow
            };

            await _dbContext.ServerLogs.AddAsync(log);
            await _dbContext.SaveChangesAsync();
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
        }
    }

}
