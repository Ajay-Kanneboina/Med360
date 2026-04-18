using MediCore.Data;
using MediCore.Models;

namespace MediCore.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _db;

        public AuditService(AppDbContext db) => _db = db;

        public void Log(string actor, string action, string target, string category, int? userId = null)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId    = userId,
                Actor     = actor,
                Action    = action,
                Target    = target,
                Category  = category,
                Timestamp = DateTime.Now
            });
            _db.SaveChanges();
        }
    }
}
