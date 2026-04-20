using MediCore.Models;

namespace MediCore.Services
{
    public interface IAppointmentService
    {
        List<Patient> GetAllPatients();
        List<User> GetAllDoctors();
        List<string> GetAvailableSlots(int doctorId, DateTime date);
        Appointment? BookAppointment(int patientId, int doctorId, DateTime date, string timeSlot, string? notes);
        List<Appointment> GetAllAppointments(string? status, string? search, DateTime? date);
        AppointmentStats GetStats();
        List<Appointment> GetDoctorAppointments(int doctorUserId, string? status);
        List<Appointment> GetPatientAppointments(int patientId);
        Appointment? UpdateStatus(int appointmentId, string newStatus);
        Appointment? CancelAppointment(int appointmentId, string reason);
        Appointment? Reschedule(int appointmentId, DateTime newDate, string newSlot);
        List<DoctorAvailability> GetDoctorAvailability(int doctorUserId);
        void SaveAvailability(int doctorUserId, List<DoctorAvailability> slots);
    }

    public class AppointmentStats
    {
        public int Total     { get; set; }
        public int Today     { get; set; }
        public int Scheduled { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
    }
}
