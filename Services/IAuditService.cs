using MediCore.Models;

namespace MediCore.Services
{
    public interface IAuditService
    {
        void Log(AuditLog entry);
    }
}
