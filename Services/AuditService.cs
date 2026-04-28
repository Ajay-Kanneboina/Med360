using MediCore.Data;
using MediCore.Models;

namespace MediCore.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _db;

        public AuditService(AppDbContext db) => _db = db;

        public void Log(AuditLog entry)
        {
            entry.Timestamp = DateTime.Now;
            _db.AuditLogs.Add(entry);
            _db.SaveChanges();
        }
    }
}
