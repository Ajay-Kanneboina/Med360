using MediCore.Models;

namespace MediCore.Services
{
    /// <summary>
    /// IPatientService — contract for all patient-portal business logic.
    /// </summary>
    public interface IPatientService
    {
        // ── Dashboard ──────────────────────────────────────────────────────
        /// <summary>Loads a patient with Complaints and Records for the dashboard.</summary>
        Patient? GetPatientForDashboard(int patientId);

        /// <summary>Returns KPI counts (open complaints, reviewed, record count).</summary>
        PatientDashboardStats GetDashboardStats(int patientId);

        // ── Records ────────────────────────────────────────────────────────
        /// <summary>Returns this patient's records with optional status and keyword filter.</summary>
        List<Record> GetMyRecords(int patientId, string? status, string? search);

        /// <summary>Returns total, active, and closed record counts for summary pills.</summary>
        PatientRecordStats GetRecordStats(int patientId);

        /// <summary>
        /// Returns a single record, verifying it belongs to this patient.
        /// Returns null if not found or ownership mismatch (security rule).
        /// </summary>
        Record? GetMyRecord(int recordId, int patientId);

        // ── Complaints ─────────────────────────────────────────────────────
        /// <summary>Returns all complaints for this patient, newest first.</summary>
        List<Complaint> GetMyComplaints(int patientId);

        /// <summary>
        /// Creates and saves a new complaint.
        /// Business rules: IsRead = false, Status = "Active", timestamps auto-set.
        /// </summary>
        Complaint SubmitComplaint(int patientId, string description, string? additionalNotes);

        /// <summary>
        /// Returns a complaint only if it belongs to this patient (ownership check).
        /// Returns null on mismatch — controller should return 404.
        /// </summary>
        Complaint? GetMyComplaint(int complaintId, int patientId);

        /// <summary>
        /// Updates a complaint's text and resets IsRead = false
        /// so the doctor is notified of the change.
        /// </summary>
        bool UpdateComplaint(int complaintId, int patientId,
                             string description, string? additionalNotes);

        // ── Profile ────────────────────────────────────────────────────────
        /// <summary>Returns the patient's own profile (demographics + history).</summary>
        Patient? GetMyProfile(int patientId);

        /// <summary>
        /// Updates the patient's own profile. Patients are now permitted to edit
        /// all fields about themselves — both contact info (phone, email, address,
        /// emergency contact) and clinical info (date of birth, gender, blood
        /// group, medical history).
        ///
        /// Caveat: allowing patients to self-edit clinical fields increases the
        /// risk of incorrect data (e.g. wrong blood group). In a production
        /// clinic, these typically remain admin-only — kept patient-editable
        /// here as requested. Consider adding an audit log entry for clinical
        /// changes so reception can verify them later.
        ///
        /// FullName is intentionally NOT editable (identity field).
        /// Returns false if the patient is not found.
        /// </summary>
        bool UpdateProfile(int patientId,
                           string?   phone,
                           string?   email,
                           string?   address,
                           string?   emergencyContact,
                           DateTime? dateOfBirth,
                           string?   gender,
                           string?   bloodGroup,
                           string?   medicalHistory);
    }

    /// <summary>KPI counts for the patient dashboard tiles.</summary>
    public class PatientDashboardStats
    {
        public int OpenCount     { get; set; }
        public int ReviewedCount { get; set; }
        public int RecordCount   { get; set; }
    }

    /// <summary>Record summary counts for the MyRecords summary pills.</summary>
    public class PatientRecordStats
    {
        public int Total  { get; set; }
        public int Active { get; set; }
        public int Closed { get; set; }
    }
}
