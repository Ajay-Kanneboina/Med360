namespace MediCore.Services
{
    public interface IAuditService
    {
        // Call this every time something important happens in the system.
        // actor   = who did it (person's name from session)
        // action  = what happened ("Medical Record Added", "Login", etc.)
        // target  = what was affected ("Patient — john@email.com")
        // category = "User", "Record", or "Complaint"
        void Log(string actor, string action, string target, string category);
    }
}
