
// konkretna implementacija DbContext-a za medicinski sustav



using MedicalOrm.Core;
using MedicalApp.Models;

namespace MedicalApp.Models;

public class MedicalDbContext : DbContext
{
    
    public MedicalDbContext(string connectionString) : base(connectionString) { }

   
    
    public void InitializeSchema()
    {
        Console.WriteLine("[DB] Inicijalizacija sheme baze podataka...");

        // RunMigration provjerava postoji li tablica i ažurira je po potrebi
        RunMigration<Doctor>();        
        RunMigration<Patient>();        
        RunMigration<MedicalHistory>(); 
        RunMigration<Medication>();    
        RunMigration<Prescription>();  
        RunMigration<Appointment>();    

        Console.WriteLine("[DB] Shema inicijalizirana.");
    }

    
    // Dodaj liječnike samo pri prvom pokretanju
    
    public void SeedDoctors()
    {
        var existing = GetAll<Doctor>();
        if (existing.Count > 0)
        {
            Console.WriteLine("[DB] Liječnici već postoje, preskačem seed.");
            return;
        }

        Console.WriteLine("[DB] Dodajem početne liječnike...");

        
        var doctors = new[]
        {
            new Doctor { FirstName = "Ana",    LastName = "Kovač",   Specialization = "Kardiologija" },
            new Doctor { FirstName = "Marko",  LastName = "Blažić",  Specialization = "Neurologija" },
            new Doctor { FirstName = "Petra",  LastName = "Horvat",  Specialization = "Dermatologija" },
            new Doctor { FirstName = "Ivan",   LastName = "Perić",   Specialization = "Oftalmologija" },
            new Doctor { FirstName = "Maja",   LastName = "Jurić",   Specialization = "Radiologija" },
            new Doctor { FirstName = "Tomislav",LastName = "Šimić",  Specialization = "Stomatologija" },
            new Doctor { FirstName = "Lucija", LastName = "Babić",   Specialization = "Ginekologija" },
        };

        foreach (var doc in doctors)
            Insert(doc);

        Console.WriteLine($"[DB] Dodano {doctors.Length} liječnika.");
    }

    

    // PATIENTS
    public List<Patient> GetPatients() => GetAll<Patient>();
    public Patient? GetPatient(int id) => GetById<Patient>(id);
    public Patient AddPatient(Patient p) => Insert(p);
    public void UpdatePatient(Patient p) => SaveChanges(p);
    public void RemovePatient(Patient p) => Delete(p);
    public List<Patient> SearchPatients(string query)
        => Where<Patient>(p => p.LastName == query || p.FirstName == query);

    // DOCTORS 
    public List<Doctor> GetDoctors() => GetAll<Doctor>();
    public Doctor? GetDoctor(int id) => GetById<Doctor>(id);

    // MEDICAL HISTORY 
    public List<MedicalHistory> GetPatientHistory(int patientId)
        => Where<MedicalHistory>(h => h.PatientId == patientId);
    public MedicalHistory AddHistory(MedicalHistory h) => Insert(h);
    public void UpdateHistory(MedicalHistory h) => SaveChanges(h);
    public void RemoveHistory(MedicalHistory h) => Delete(h);

    // MEDICATIONS 
    public List<Medication> GetMedications() => GetAll<Medication>();
    public Medication AddMedication(Medication m) => Insert(m);
    public void UpdateMedication(Medication m) => SaveChanges(m);
    public void RemoveMedication(Medication m) => Delete(m);

    // PRESCRIPTIONS 
    public List<Prescription> GetPatientPrescriptions(int patientId)
        => Where<Prescription>(p => p.PatientId == patientId);
    public Prescription AddPrescription(Prescription p) => Insert(p);
    public void UpdatePrescription(Prescription p) => SaveChanges(p);
    public void RemovePrescription(Prescription p) => Delete(p);

    // APPOINTMENTS 
    public List<Appointment> GetAllAppointments() => GetAll<Appointment>();
    public List<Appointment> GetPatientAppointments(int patientId)
        => Where<Appointment>(a => a.PatientId == patientId);
    public Appointment AddAppointment(Appointment a) => Insert(a);
    public void UpdateAppointment(Appointment a) => SaveChanges(a);
    public void RemoveAppointment(Appointment a) => Delete(a);
}
