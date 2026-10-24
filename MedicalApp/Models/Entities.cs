
// Svaka klasa = jedna tablica u PostgreSQL bazi.


using MedicalOrm.Attributes;

namespace MedicalApp.Models;


// PACIJENT - Patient

[Table("patients")]  // Mapira se na tablicu "patients"
public class Patient
{
    [PrimaryKey(autoIncrement: true)]        
    [Column("id")]
    public int Id { get; set; }

    [Column("first_name", IsNullable = false)] 
    public string FirstName { get; set; } = "";

    [Column("last_name", IsNullable = false)]
    public string LastName { get; set; } = "";

    [Column("oib", IsNullable = false, IsUnique = true, DbType = "CHAR(11)")] 
    public string Oib { get; set; } = "";

    [Column("date_of_birth", DbType = "DATE")]
    public DateOnly DateOfBirth { get; set; }

    [Column("gender", DbType = "CHAR(1)")]   
    public string Gender { get; set; } = "";

    [Column("address_residence")]          
    public string AddressResidence { get; set; } = "";

    [Column("address_domicile")]            
    public string AddressDomicile { get; set; } = "";

    
    [Navigate("id")] 
    public List<MedicalHistory>? MedicalHistories { get; set; }

    [Navigate("id")]
    public List<Prescription>? Prescriptions { get; set; }
}


// LIJEČNIK - Doctor

[Table("doctors")]
public class Doctor
{
    [PrimaryKey(autoIncrement: true)]
    [Column("id")]
    public int Id { get; set; }

    [Column("first_name", IsNullable = false)]
    public string FirstName { get; set; } = "";

    [Column("last_name", IsNullable = false)]
    public string LastName { get; set; } = "";

    [Column("specialization", IsNullable = false)]
    public string Specialization { get; set; } = "";

   
    [Navigate("doctor_id")]
    public List<Appointment>? Appointments { get; set; }
}


// MEDICINSKA POVIJEST - MedicalHistory

[Table("medical_histories")]
public class MedicalHistory
{
    [PrimaryKey(autoIncrement: true)]
    [Column("id")]
    public int Id { get; set; }

    [Column("patient_id", IsNullable = false)]    
    public int PatientId { get; set; }

    [Column("diagnosis", IsNullable = false)]    
    public string Diagnosis { get; set; } = "";

    [Column("start_date", DbType = "DATE")]
    public DateOnly StartDate { get; set; }

    [Column("end_date", DbType = "DATE")]          
    public DateOnly? EndDate { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

   
    [Navigate("patient_id")]
    public Patient? Patient { get; set; }
}


// LIJEK - Medication

[Table("medications")]
public class Medication
{
    [PrimaryKey(autoIncrement: true)]
    [Column("id")]
    public int Id { get; set; }

    [Column("name", IsNullable = false, IsUnique = true)]
    public string Name { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }
}


// PRESKRIPCIJA - Prescription

[Table("prescriptions")]
public class Prescription
{
    [PrimaryKey(autoIncrement: true)]
    [Column("id")]
    public int Id { get; set; }

    [Column("patient_id", IsNullable = false)]    
    public int PatientId { get; set; }

    [Column("medication_id", IsNullable = false)] 
    public int MedicationId { get; set; }

    [Column("doctor_id", IsNullable = false)]      
    public int DoctorId { get; set; }

    [Column("dose", IsNullable = false)]          
    public string Dose { get; set; } = "";

    [Column("frequency", IsNullable = false)]      
    public string Frequency { get; set; } = "";

    [Column("start_date", DbType = "DATE")]
    public DateOnly StartDate { get; set; }

    [Column("end_date", DbType = "DATE")]
    public DateOnly? EndDate { get; set; }

    [Column("condition_treated")]                
    public string? ConditionTreated { get; set; }

    
    [Navigate("patient_id")]
    public Patient? Patient { get; set; }

    [Navigate("medication_id")]
    public Medication? Medication { get; set; }
}


// SPECIJALISTIČKI PREGLED - Appointment

[Table("appointments")]
public class Appointment
{
    [PrimaryKey(autoIncrement: true)]
    [Column("id")]
    public int Id { get; set; }

    [Column("patient_id", IsNullable = false)]
    public int PatientId { get; set; }

    [Column("doctor_id", IsNullable = false)]     
    public int DoctorId { get; set; }

    
    [Column("appointment_type", IsNullable = false, DbType = "VARCHAR(10)")]
    public string AppointmentType { get; set; } = "";

    [Column("scheduled_at", DbType = "TIMESTAMP WITH TIME ZONE")]
    public DateTime ScheduledAt { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("status", Default = "'scheduled'")]    
    public string Status { get; set; } = "scheduled";

    
    [Navigate("patient_id")]
    public Patient? Patient { get; set; }

    [Navigate("doctor_id")]
    public Doctor? Doctor { get; set; }
}
