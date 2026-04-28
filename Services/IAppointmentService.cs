using MediCore.Models;

namespace MediCore.Services
{
    public interface IAppointmentService
    {
        List<Patient> GetAllPatients();
        List<User> GetAllDoctors();
        List<string> GetAvailableSlots(int doctorId, DateTime date);

        Appointment? BookAppointment(Appointment data);

        List<Appointment> GetAllAppointments(string? status, string? search, DateTime? date);
        AppointmentStats  GetStats();
        Appointment? UpdateStatus(int appointmentId, string newStatus);
        Appointment? CancelAppointment(int appointmentId, string reason);

        List<Appointment> GetDoctorAppointments(int doctorUserId, string? status);
        List<DoctorAvailability> GetDoctorAvailability(int doctorUserId);
        void SaveAvailability(int doctorUserId, List<DoctorAvailability> slots);
        List<Appointment> GetPatientAppointments(int patientId);

        AppointmentRequest SendRequest(int patientId, AppointmentRequest data);

        List<AppointmentRequest> GetPendingRequests();
        List<AppointmentRequest> GetPatientRequests(int patientId);

        bool MarkRequestHandled(int requestId);

        int GetPendingRequestCount();
    }

    public class AppointmentStats
    {
        public int Total { get; set; }
        public int Today { get; set; }
        public int Scheduled { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
    }
}
