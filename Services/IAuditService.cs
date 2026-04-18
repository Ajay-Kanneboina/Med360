namespace MediCore.Services
{
    public interface IAuditService
    {
        void Log(string actor, string action, string target, string category, int? userId = null);
    }
}
