# Medical System – Custom ORM in C#

A console-based medical records application built on top of a **custom, hand-written ORM** (Object-Relational Mapper) for PostgreSQL. Instead of using an existing framework such as Entity Framework, the project implements its own ORM layer using C# reflection, custom attributes and expression trees, and then uses it to manage patients, doctors, medical histories, medications, prescriptions and specialist appointments.

> The application's user interface (menus and prompts) is in Croatian.

---

## Project Structure

```
projekt1/
├── MedicalSystem.sln            # Visual Studio solution
├── docker-compose.yml           # PostgreSQL database container
├── MedicalOrm/                  # Class library – the custom ORM
│   ├── Attributes/
│   │   └── OrmAttributes.cs     # [Table], [Column], [PrimaryKey], [ForeignKey], [Navigate], [Ignore]
│   └── Core/
│       ├── DbContext.cs         # Base context: connection, CRUD, change tracking, migrations
│       └── ExpressionToSqlVisitor.cs  # Translates C# lambda expressions into SQL WHERE clauses
└── MedicalApp/                  # Console application that uses the ORM
    ├── Program.cs               # Interactive console menus
    └── Models/
        ├── Entities.cs          # Entity classes mapped to database tables
        └── MedicalDbContext.cs  # Concrete DbContext for the medical domain
```

---

## Features

### Custom ORM (`MedicalOrm`)

- **Attribute-based mapping** – classes and properties are mapped to tables and columns using custom attributes:
  - `[Table("patients")]` – table name
  - `[Column("first_name", IsNullable = false, IsUnique = true, DbType = "CHAR(11)", Default = "...")]` – column name and constraints
  - `[PrimaryKey(autoIncrement: true)]` – primary key (`SERIAL` when auto-incremented)
  - `[Navigate("doctor_id")]` – navigation property for related entities
  - `[Ignore]` – property is not persisted
- **Code-first schema creation** – `EnsureCreated<T>()` reads the attributes via reflection and generates `CREATE TABLE IF NOT EXISTS` statements.
- **Simple migrations** – `RunMigration<T>()` compares the C# class with the existing table (via `information_schema`), creates missing tables and adds missing columns with `ALTER TABLE ... ADD COLUMN`. Applied migrations are logged in a `__migrations` table.
- **CRUD operations** – `Insert`, `GetAll`, `GetById`, `SaveChanges` (update) and `Delete`, all using parameterized queries. `Insert` uses `RETURNING` to populate the generated primary key.
- **LINQ-style filtering** – `Where<T>(x => x.LastName == "Horvat")` converts a lambda expression into a parameterized SQL `WHERE` clause using a custom `ExpressionVisitor`. Supported operators: `==`, `!=`, `>`, `>=`, `<`, `<=`, `&&`, `||`.
- **Change tracking** – loaded entities are snapshotted; on `SaveChanges` only the modified columns are included in the `UPDATE` statement. Untracked entities fall back to a full update.
- **Eager loading** – `Include<T, TRelated>(id, x => x.Navigation)` loads an entity together with a related entity referenced by a foreign key.
- **Automatic type mapping** – C# types (`int`, `long`, `string`, `bool`, `DateTime`, `DateOnly`, `decimal`, `double`, `float`) are mapped to PostgreSQL types; property names default to `snake_case` column names.

### Medical Application (`MedicalApp`)

An interactive console menu with the following modules:

| Module | Operations |
|---|---|
| **Patients** | List, add, edit addresses (change-tracking demo), delete, search by first/last name, view patient with medical history |
| **Medical history** | List a patient's records, add, edit diagnosis / end date, delete |
| **Medications** | List, add, edit, delete |
| **Prescriptions** | List a patient's prescriptions, add (patient, medication, doctor, dose, frequency, dates, condition treated), edit dose / frequency, delete |
| **Appointments** | List all or per patient, schedule a specialist exam (CT, MR, ULTRA, EKG, ECHO, OKO, DERM, DENTA, MAMMO, EEG), update status (`scheduled` / `completed` / `cancelled`), delete |
| **Doctors** | Read-only list of doctors |

On startup the application automatically creates/migrates the schema and seeds seven doctors with different specializations (only on the first run).

### Data Model

| Table | Description |
|---|---|
| `patients` | Personal data: name, OIB (unique, 11 characters), date of birth, gender, residence and domicile address |
| `doctors` | Name and specialization |
| `medical_histories` | Diagnoses per patient with start/end date and notes |
| `medications` | Medication catalogue (unique name, description) |
| `prescriptions` | Links patient, medication and doctor; dose, frequency, duration and condition treated |
| `appointments` | Specialist appointments: patient, doctor, type, scheduled time, status, notes |

---

## Technologies Used

- **C# 12 / .NET 8** – application and ORM (uses primary constructors, nullable reference types, implicit usings, `DateOnly`)
- **PostgreSQL 16** (Alpine image) – relational database
- **Npgsql 8.0.3** – ADO.NET data provider for PostgreSQL
- **Docker & Docker Compose** – running the database in a container with a persistent named volume
- **Reflection, custom attributes and Expression Trees** (`System.Reflection`, `System.Linq.Expressions`) – core building blocks of the ORM
- **Visual Studio 2022** – development environment (solution file included)

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine with Docker Compose)

### 1. Start the database

From the project root (the folder containing `docker-compose.yml`):

```bash
docker compose up -d
```

This starts a PostgreSQL 16 container named `medical_postgres` on port `5432` with:

| Setting | Value |
|---|---|
| Database | `medical_db` |
| User | `meduser` |
| Password | `medpass123` |

Data is persisted in the `postgres_data` Docker volume.

### 2. Run the application

Using the .NET CLI:

```bash
dotnet run --project MedicalApp
```

Or open `MedicalSystem.sln` in Visual Studio, set **MedicalApp** as the startup project and press **F5**.

On the first run the application creates all tables, seeds the doctors and then shows the main menu.

### 3. (Optional) Inspect the database

```bash
docker exec -it medical_postgres psql -U meduser -d medical_db
```

Useful commands inside `psql`: `\dt` (list tables), `SELECT * FROM patients;`, `SELECT * FROM __migrations;`.

### Configuration

The connection string is defined at the top of `MedicalApp/Program.cs`:

```csharp
const string CONNECTION_STRING =
    "Host=localhost;Port=5432;Database=medical_db;Username=meduser;Password=medpass123;Include Error Detail=true";
```

If you change the credentials or port in `docker-compose.yml`, update this string accordingly.

### Stopping the database

```bash
docker compose down        # stop the container, keep data
docker compose down -v     # stop the container and delete all data
```
