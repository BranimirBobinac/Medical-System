
// Demonstrira sve CRUD operacije 
//docker exec -it medical_postgres psql -U meduser -d medical_db

using MedicalApp.Models;


// KONFIGURACIJA - Connection string za PostgreSQL Docker kontejner
// Host = ime Docker kontejnera ili localhost
// Port = 5432 (default PostgreSQL port)

const string CONNECTION_STRING =
    "Host=localhost;Port=5432;Database=medical_db;Username=meduser;Password=medpass123;Include Error Detail=true";

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=================================================");
Console.WriteLine("     MEDICINSKI SUSTAV - Vlastiti ORM (C#)      ");
Console.WriteLine("=================================================");


using var db = new MedicalDbContext(CONNECTION_STRING);


db.InitializeSchema();


db.SeedDoctors();

Console.WriteLine();
Console.WriteLine("Konekcija na bazu uspješna! Pokretanje aplikacije...");
Console.WriteLine();


// GLAVNI IZBORNIK

bool running = true;
while (running)
{
    Console.WriteLine("\n===== GLAVNI IZBORNIK =====");
    Console.WriteLine("1. Upravljanje pacijentima");
    Console.WriteLine("2. Upravljanje medicinskom poviješću");
    Console.WriteLine("3. Upravljanje lijekovima");
    Console.WriteLine("4. Upravljanje receptima");
    Console.WriteLine("5. Upravljanje pregledima");
    Console.WriteLine("6. Prikaz liječnika");
    Console.WriteLine("0. Izlaz");
    Console.Write("\nOdabir: ");

    switch (Console.ReadLine())
    {
        case "1": ManagePatients(db); break;
        case "2": ManageMedicalHistory(db); break;
        case "3": ManageMedications(db); break;
        case "4": ManagePrescriptions(db); break;
        case "5": ManageAppointments(db); break;
        case "6": ShowDoctors(db); break;
        case "0": running = false; break;
        default: Console.WriteLine("Nepoznata opcija."); break;
    }
}

Console.WriteLine("Doviđenja!");


// UPRAVLJANJE PACIJENTIMA

static void ManagePatients(MedicalDbContext db)
{
    bool back = false;
    while (!back)
    {
        Console.WriteLine("\n--- PACIJENTI ---");
        Console.WriteLine("1. Prikaz svih pacijenata");
        Console.WriteLine("2. Dodaj pacijenta");
        Console.WriteLine("3. Uredi pacijenta (Change Tracking demo)");
        Console.WriteLine("4. Obriši pacijenta");
        Console.WriteLine("5. Pretraži pacijenta po prezimenu");
        Console.WriteLine("6. Pregled s eager loading (pacijent + povijest)");
        Console.WriteLine("0. Natrag");
        Console.Write("Odabir: ");

        switch (Console.ReadLine())
        {
            case "1":
                
                var patients = db.GetPatients();
                if (!patients.Any()) { Console.WriteLine("Nema pacijenata."); break; }
                Console.WriteLine($"\n{"ID",-5} {"Ime",-15} {"Prezime",-15} {"OIB",-12} {"Spol",-5} {"Datum rođenja",-15}");
                Console.WriteLine(new string('-', 70));
                foreach (var p in patients)
                    Console.WriteLine($"{p.Id,-5} {p.FirstName,-15} {p.LastName,-15} {p.Oib,-12} {p.Gender,-5} {p.DateOfBirth,-15}");
                break;

            case "2":
                
                var newP = new Patient();
                Console.Write("Ime: ");        newP.FirstName = Console.ReadLine()!;
                Console.Write("Prezime: ");    newP.LastName = Console.ReadLine()!;
                Console.Write("OIB (11 znakova): "); newP.Oib = Console.ReadLine()!;
                Console.Write("Datum rođenja (YYYY-MM-DD): ");
                newP.DateOfBirth = DateOnly.Parse(Console.ReadLine()!);
                Console.Write("Spol (M/Ž): "); newP.Gender = Console.ReadLine()!;
                Console.Write("Adresa boravišta: "); newP.AddressResidence = Console.ReadLine()!;
                Console.Write("Adresa prebivališta: "); newP.AddressDomicile = Console.ReadLine()!;

                var saved = db.AddPatient(newP);
                Console.WriteLine($"✓ Pacijent dodan s ID: {saved.Id}");
                break;

            case "3":
                
                
                Console.Write("ID pacijenta za uređivanje: ");
                if (!int.TryParse(Console.ReadLine(), out int editId)) break;

                var editP = db.GetPatient(editId);
                if (editP == null) { Console.WriteLine("Pacijent nije pronađen."); break; }

                Console.WriteLine($"Trenutna adresa boravišta: {editP.AddressResidence}");
                Console.Write("Nova adresa boravišta (Enter = bez promjene): ");
                var newAddr = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(newAddr))
                    editP.AddressResidence = newAddr;

                Console.WriteLine($"Trenutna adresa prebivališta: {editP.AddressDomicile}");
                Console.Write("Nova adresa prebivališta (Enter = bez promjene): ");
                var newDom = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(newDom))
                    editP.AddressDomicile = newDom;

                
                db.UpdatePatient(editP);
                Console.WriteLine("✓ Pacijent ažuriran (Change Tracking).");
                break;

            case "4":
               
                Console.Write("ID pacijenta za brisanje: ");
                if (!int.TryParse(Console.ReadLine(), out int delId)) break;
                var delP = db.GetPatient(delId);
                if (delP == null) { Console.WriteLine("Pacijent nije pronađen."); break; }
                db.RemovePatient(delP);
                Console.WriteLine("✓ Pacijent obrisan.");
                break;

            case "5":
                
                Console.Write("Prezime za pretragu: ");
                var query = Console.ReadLine()!;
                var found = db.SearchPatients(query);
                if (!found.Any()) { Console.WriteLine("Nema rezultata."); break; }
                foreach (var p in found)
                    Console.WriteLine($"  [{p.Id}] {p.FirstName} {p.LastName} - OIB: {p.Oib}");
                break;

            case "6":
                // EAGER LOADING
                Console.Write("ID pacijenta: ");
                if (!int.TryParse(Console.ReadLine(), out int eagerid)) break;
                var eagerP = db.GetPatient(eagerid);
                if (eagerP == null) { Console.WriteLine("Pacijent nije pronađen."); break; }

                Console.WriteLine($"\nPacijent: {eagerP.FirstName} {eagerP.LastName}");
                Console.WriteLine("Medicinska povijest:");
                var histories = db.GetPatientHistory(eagerid);
                if (!histories.Any()) { Console.WriteLine("  Nema zapisa."); break; }
                foreach (var h in histories)
                    Console.WriteLine($"  - {h.Diagnosis} ({h.StartDate} - {h.EndDate?.ToString() ?? "i dalje"})");
                break;

            case "0": back = true; break;
        }
    }
}


// MEDICINSKA POVIJEST

static void ManageMedicalHistory(MedicalDbContext db)
{
    bool back = false;
    while (!back)
    {
        Console.WriteLine("\n--- MEDICINSKA POVIJEST ---");
        Console.WriteLine("1. Prikaz povijesti pacijenta");
        Console.WriteLine("2. Dodaj zapis");
        Console.WriteLine("3. Uredi zapis");
        Console.WriteLine("4. Obriši zapis");
        Console.WriteLine("0. Natrag");
        Console.Write("Odabir: ");

        switch (Console.ReadLine())
        {
            case "1":
                Console.Write("ID pacijenta: ");
                if (!int.TryParse(Console.ReadLine(), out int pid)) break;
                var hist = db.GetPatientHistory(pid);
                if (!hist.Any()) { Console.WriteLine("Nema zapisa."); break; }
                Console.WriteLine($"\n{"ID",-5} {"Dijagnoza",-25} {"Od",-12} {"Do",-12} {"Napomene",-20}");
                Console.WriteLine(new string('-', 80));
                foreach (var h in hist)
                    Console.WriteLine($"{h.Id,-5} {h.Diagnosis,-25} {h.StartDate,-12} {h.EndDate?.ToString() ?? "traje",-12} {h.Notes ?? "",-20}");
                break;

            case "2":
                var newH = new MedicalHistory();
                Console.Write("ID pacijenta: ");
                newH.PatientId = int.Parse(Console.ReadLine()!);
                Console.Write("Dijagnoza: ");
                newH.Diagnosis = Console.ReadLine()!;
                Console.Write("Datum početka (YYYY-MM-DD): ");
                newH.StartDate = DateOnly.Parse(Console.ReadLine()!);
                Console.Write("Datum završetka (YYYY-MM-DD, Enter = traje): ");
                var endStr = Console.ReadLine();
                newH.EndDate = string.IsNullOrWhiteSpace(endStr) ? null : DateOnly.Parse(endStr);
                Console.Write("Napomene: ");
                newH.Notes = Console.ReadLine();

                var addedH = db.AddHistory(newH);
                Console.WriteLine($"✓ Zapis dodan s ID: {addedH.Id}");
                break;

            case "3":
                Console.Write("ID zapisa za uređivanje: ");
                if (!int.TryParse(Console.ReadLine(), out int hid)) break;
                var editH = db.GetById<MedicalHistory>(hid);
                if (editH == null) { Console.WriteLine("Zapis nije pronađen."); break; }

                Console.Write($"Nova dijagnoza ({editH.Diagnosis}): ");
                var nd = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(nd)) editH.Diagnosis = nd;

                Console.Write($"Datum završetka ({editH.EndDate?.ToString() ?? "traje"}): ");
                var ne = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(ne)) editH.EndDate = DateOnly.Parse(ne);

                db.UpdateHistory(editH);
                Console.WriteLine("✓ Zapis ažuriran.");
                break;

            case "4":
                Console.Write("ID zapisa za brisanje: ");
                if (!int.TryParse(Console.ReadLine(), out int dhid)) break;
                var delH = db.GetById<MedicalHistory>(dhid);
                if (delH == null) { Console.WriteLine("Zapis nije pronađen."); break; }
                db.RemoveHistory(delH);
                Console.WriteLine("✓ Zapis obrisan.");
                break;

            case "0": back = true; break;
        }
    }
}


// LIJEKOVI

static void ManageMedications(MedicalDbContext db)
{
    bool back = false;
    while (!back)
    {
        Console.WriteLine("\n--- LIJEKOVI ---");
        Console.WriteLine("1. Prikaz svih lijekova");
        Console.WriteLine("2. Dodaj lijek");
        Console.WriteLine("3. Uredi lijek");
        Console.WriteLine("4. Obriši lijek");
        Console.WriteLine("0. Natrag");
        Console.Write("Odabir: ");

        switch (Console.ReadLine())
        {
            case "1":
                var meds = db.GetMedications();
                if (!meds.Any()) { Console.WriteLine("Nema lijekova."); break; }
                Console.WriteLine($"\n{"ID",-5} {"Naziv",-25} {"Opis",-40}");
                Console.WriteLine(new string('-', 75));
                foreach (var m in meds)
                    Console.WriteLine($"{m.Id,-5} {m.Name,-25} {m.Description ?? "",-40}");
                break;

            case "2":
                var newM = new Medication();
                Console.Write("Naziv lijeka: "); newM.Name = Console.ReadLine()!;
                Console.Write("Opis: "); newM.Description = Console.ReadLine();
                var addedM = db.AddMedication(newM);
                Console.WriteLine($"✓ Lijek dodan s ID: {addedM.Id}");
                break;

            case "3":
                Console.Write("ID lijeka: ");
                if (!int.TryParse(Console.ReadLine(), out int mid)) break;
                var editM = db.GetById<Medication>(mid);
                if (editM == null) { Console.WriteLine("Lijek nije pronađen."); break; }
                Console.Write($"Novi naziv ({editM.Name}): ");
                var nn = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(nn)) editM.Name = nn;
                Console.Write($"Novi opis ({editM.Description}): ");
                var nd2 = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(nd2)) editM.Description = nd2;
                db.UpdateMedication(editM);
                Console.WriteLine("✓ Lijek ažuriran.");
                break;

            case "4":
                Console.Write("ID lijeka za brisanje: ");
                if (!int.TryParse(Console.ReadLine(), out int delmid)) break;
                var delM = db.GetById<Medication>(delmid);
                if (delM == null) { Console.WriteLine("Lijek nije pronađen."); break; }
                db.RemoveMedication(delM);
                Console.WriteLine("✓ Lijek obrisan.");
                break;

            case "0": back = true; break;
        }
    }
}


// RECEPTI / PRESKRIPCIJE

static void ManagePrescriptions(MedicalDbContext db)
{
    bool back = false;
    while (!back)
    {
        Console.WriteLine("\n--- RECEPTI ---");
        Console.WriteLine("1. Prikaz recepata pacijenta");
        Console.WriteLine("2. Dodaj recept");
        Console.WriteLine("3. Uredi recept");
        Console.WriteLine("4. Obriši recept");
        Console.WriteLine("0. Natrag");
        Console.Write("Odabir: ");

        switch (Console.ReadLine())
        {
            case "1":
                Console.Write("ID pacijenta: ");
                if (!int.TryParse(Console.ReadLine(), out int rpid)) break;
                var rxs = db.GetPatientPrescriptions(rpid);
                if (!rxs.Any()) { Console.WriteLine("Nema recepata."); break; }
                Console.WriteLine($"\n{"ID",-5} {"Lijek ID",-10} {"Doza",-15} {"Učestalost",-20} {"Od",-12} {"Stanje",-20}");
                Console.WriteLine(new string('-', 85));
                foreach (var r in rxs)
                    Console.WriteLine($"{r.Id,-5} {r.MedicationId,-10} {r.Dose,-15} {r.Frequency,-20} {r.StartDate,-12} {r.ConditionTreated ?? "",-20}");
                break;

            case "2":
                var newRx = new Prescription();
                Console.Write("ID pacijenta: "); newRx.PatientId = int.Parse(Console.ReadLine()!);
                Console.Write("ID lijeka: "); newRx.MedicationId = int.Parse(Console.ReadLine()!);
                Console.Write("ID liječnika: "); newRx.DoctorId = int.Parse(Console.ReadLine()!);
                Console.Write("Doza (npr. 500mg, 2 tablete): "); newRx.Dose = Console.ReadLine()!;
                Console.Write("Učestalost (npr. 3x dnevno): "); newRx.Frequency = Console.ReadLine()!;
                Console.Write("Datum početka (YYYY-MM-DD): "); newRx.StartDate = DateOnly.Parse(Console.ReadLine()!);
                Console.Write("Datum kraja (Enter = bez kraja): ");
                var rxEnd = Console.ReadLine();
                newRx.EndDate = string.IsNullOrWhiteSpace(rxEnd) ? null : DateOnly.Parse(rxEnd);
                Console.Write("Stanje zbog kojeg se pije: "); newRx.ConditionTreated = Console.ReadLine();

                var addRx = db.AddPrescription(newRx);
                Console.WriteLine($"✓ Recept dodan s ID: {addRx.Id}");
                break;

            case "3":
                Console.Write("ID recepta: ");
                if (!int.TryParse(Console.ReadLine(), out int rxid)) break;
                var editRx = db.GetById<Prescription>(rxid);
                if (editRx == null) { Console.WriteLine("Recept nije pronađen."); break; }
                Console.Write($"Nova doza ({editRx.Dose}): "); var nd3 = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(nd3)) editRx.Dose = nd3;
                Console.Write($"Nova učestalost ({editRx.Frequency}): "); var nf = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(nf)) editRx.Frequency = nf;
                db.UpdatePrescription(editRx);
                Console.WriteLine("✓ Recept ažuriran.");
                break;

            case "4":
                Console.Write("ID recepta za brisanje: ");
                if (!int.TryParse(Console.ReadLine(), out int drxid)) break;
                var delRx = db.GetById<Prescription>(drxid);
                if (delRx == null) { Console.WriteLine("Recept nije pronađen."); break; }
                db.RemovePrescription(delRx);
                Console.WriteLine("✓ Recept obrisan.");
                break;

            case "0": back = true; break;
        }
    }
}


// SPECIJALISTIČKI PREGLEDI

static void ManageAppointments(MedicalDbContext db)
{
    
    var validTypes = new[] { "CT", "MR", "ULTRA", "EKG", "ECHO", "OKO", "DERM", "DENTA", "MAMMO", "EEG" };
    bool back = false;

    while (!back)
    {
        Console.WriteLine("\n--- PREGLEDI ---");
        Console.WriteLine("1. Prikaz svih pregleda");
        Console.WriteLine("2. Pregledi pacijenta");
        Console.WriteLine("3. Zakaži pregled");
        Console.WriteLine("4. Ažuriraj status pregleda");
        Console.WriteLine("5. Obriši pregled");
        Console.WriteLine("0. Natrag");
        Console.Write("Odabir: ");

        switch (Console.ReadLine())
        {
            case "1":
                var all = db.GetAllAppointments();
                if (!all.Any()) { Console.WriteLine("Nema pregleda."); break; }
                PrintAppointments(all);
                break;

            case "2":
                Console.Write("ID pacijenta: ");
                if (!int.TryParse(Console.ReadLine(), out int appPid)) break;
                var pApps = db.GetPatientAppointments(appPid);
                if (!pApps.Any()) { Console.WriteLine("Nema pregleda za ovog pacijenta."); break; }
                PrintAppointments(pApps);
                break;

            case "3":
                var newApp = new Appointment();
                Console.Write("ID pacijenta: "); newApp.PatientId = int.Parse(Console.ReadLine()!);
                Console.Write("ID liječnika specijaliste: "); newApp.DoctorId = int.Parse(Console.ReadLine()!);
                Console.Write($"Vrsta pregleda ({string.Join(", ", validTypes)}): ");
                newApp.AppointmentType = Console.ReadLine()!.ToUpper();
                Console.Write("Termin (YYYY-MM-DD HH:MM): ");
                newApp.ScheduledAt = DateTime.Parse(Console.ReadLine()!);
                Console.Write("Napomene: "); newApp.Notes = Console.ReadLine();
                newApp.Status = "scheduled";

                var addApp = db.AddAppointment(newApp);
                Console.WriteLine($"✓ Pregled zakazan s ID: {addApp.Id}");
                break;

            case "4":
                Console.Write("ID pregleda: ");
                if (!int.TryParse(Console.ReadLine(), out int appId)) break;
                var editApp = db.GetById<Appointment>(appId);
                if (editApp == null) { Console.WriteLine("Pregled nije pronađen."); break; }
                Console.Write("Novi status (scheduled/completed/cancelled): ");
                var ns = Console.ReadLine()!;
                editApp.Status = ns;
                db.UpdateAppointment(editApp);
                Console.WriteLine("✓ Status ažuriran.");
                break;

            case "5":
                Console.Write("ID pregleda za brisanje: ");
                if (!int.TryParse(Console.ReadLine(), out int dappId)) break;
                var delApp = db.GetById<Appointment>(dappId);
                if (delApp == null) { Console.WriteLine("Pregled nije pronađen."); break; }
                db.RemoveAppointment(delApp);
                Console.WriteLine("✓ Pregled obrisan.");
                break;

            case "0": back = true; break;
        }
    }
}


// PRIKAZ LIJEČNIKA (samo čitanje)

static void ShowDoctors(MedicalDbContext db)
{
    var doctors = db.GetDoctors();
    Console.WriteLine($"\n{"ID",-5} {"Ime",-15} {"Prezime",-15} {"Specijalizacija",-20}");
    Console.WriteLine(new string('-', 60));
    foreach (var d in doctors)
        Console.WriteLine($"{d.Id,-5} {d.FirstName,-15} {d.LastName,-15} {d.Specialization,-20}");
}


static void PrintAppointments(List<Appointment> apps)
{
    Console.WriteLine($"\n{"ID",-5} {"Pac.",-6} {"Dokt.",-6} {"Vrsta",-8} {"Termin",-22} {"Status",-12} {"Napomene",-20}");
    Console.WriteLine(new string('-', 90));
    foreach (var a in apps)
        Console.WriteLine($"{a.Id,-5} {a.PatientId,-6} {a.DoctorId,-6} {a.AppointmentType,-8} {a.ScheduledAt:yyyy-MM-dd HH:mm,-22} {a.Status,-12} {a.Notes ?? "",-20}");
}
