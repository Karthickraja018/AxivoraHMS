# Axivora Hospital Management System - Backend Architecture

## Project Structure

This project follows a clean, layered architecture pattern with the following structure:

```
Axivora/
??? Controllers/          # API Controllers
??? Services/            # Business Logic Layer
?   ??? Interfaces/      # Service Interfaces
??? DTOs/                # Data Transfer Objects
??? Models/              # Entity Models
??? Data/                # Database Context
??? Mappings/            # AutoMapper Profiles
??? Middleware/          # Custom Middleware
```

## Architecture Overview

### 1. **Controllers** (`Controllers/`)
RESTful API controllers that handle HTTP requests and responses.

- `PatientsController.cs` - Patient management endpoints
- `DoctorsController.cs` - Doctor management endpoints
- `AppointmentsController.cs` - Appointment scheduling endpoints
- `ConsultationsController.cs` - Medical consultation endpoints

### 2. **Services** (`Services/` & `Services/Interfaces/`)
Business logic layer implementing the repository pattern.

#### Interfaces:
- `IPatientService.cs` - Patient service contract
- `IDoctorService.cs` - Doctor service contract
- `IAppointmentService.cs` - Appointment service contract
- `IConsultationService.cs` - Consultation service contract

#### Implementations:
- `PatientService.cs` - Patient business logic
- `DoctorService.cs` - Doctor business logic
- `AppointmentService.cs` - Appointment business logic
- `ConsultationService.cs` - Consultation business logic

### 3. **DTOs** (`DTOs/`)
Data Transfer Objects for API requests and responses.

- `PatientDto.cs` - Patient data transfer objects
- `DoctorDto.cs` - Doctor data transfer objects
- `AppointmentDto.cs` - Appointment data transfer objects
- `ConsultationDto.cs` - Consultation data transfer objects
- `AddressDto.cs` - Address data transfer objects
- `DepartmentDto.cs` - Department data transfer objects
- `PatientAllergyDto.cs` - Patient allergy data transfer objects

Each DTO file contains:
- `{Entity}Dto` - Response DTO
- `Create{Entity}Dto` - Create request DTO
- `Update{Entity}Dto` - Update request DTO (where applicable)

### 4. **Models** (`Models/`)
Entity Framework Core entity models representing database tables.

Core entities include:
- User, Role, UserRole
- Patient, Doctor
- Address, Department
- Appointment, AppointmentStatus
- Consultation, Prescription
- PatientVital, PatientAllergy
- Medicine, LabTest, OrderedTest
- ICDCode, AuditLog

### 5. **Data** (`Data/`)
Database context and configurations.

- `AxivoraDbContext.cs` - EF Core DbContext with Fluent API configurations

### 6. **Mappings** (`Mappings/`)
AutoMapper configuration profiles.

- `MappingProfile.cs` - Mapping configurations between Models and DTOs

### 7. **Middleware** (`Middleware/`)
Custom middleware components.

- `GlobalExceptionHandlerMiddleware.cs` - Global exception handling

## Features

### Global Exception Handling
Centralized error handling middleware that:
- Catches all unhandled exceptions
- Logs errors
- Returns standardized error responses
- Maps exceptions to appropriate HTTP status codes

### AutoMapper Integration
- Automatic mapping between entities and DTOs
- Reduces boilerplate code
- Configured in `MappingProfile.cs`

### Dependency Injection
All services are registered with DI container in `Program.cs`:
```csharp
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
```

### Service Layer Pattern
- Services encapsulate business logic
- Controllers remain thin, delegating to services
- Easy to test and maintain

## API Endpoints

### Patients API (`/api/patients`)
- `GET /api/patients` - Get all patients
- `GET /api/patients/{id}` - Get patient by ID
- `GET /api/patients/mrn/{mrn}` - Get patient by MRN
- `GET /api/patients/search?searchTerm={term}` - Search patients
- `POST /api/patients` - Create new patient
- `PUT /api/patients/{id}` - Update patient
- `DELETE /api/patients/{id}` - Soft delete patient

### Doctors API (`/api/doctors`)
- `GET /api/doctors` - Get all doctors
- `GET /api/doctors/{id}` - Get doctor by ID
- `GET /api/doctors/department/{departmentId}` - Get doctors by department
- `POST /api/doctors` - Create new doctor
- `PUT /api/doctors/{id}` - Update doctor
- `DELETE /api/doctors/{id}` - Soft delete doctor

### Appointments API (`/api/appointments`)
- `GET /api/appointments` - Get all appointments
- `GET /api/appointments/{id}` - Get appointment by ID
- `GET /api/appointments/patient/{patientId}` - Get patient appointments
- `GET /api/appointments/doctor/{doctorId}` - Get doctor appointments
- `GET /api/appointments/date-range?startDate={start}&endDate={end}` - Get appointments by date range
- `POST /api/appointments` - Create new appointment
- `PUT /api/appointments/{id}` - Update appointment
- `DELETE /api/appointments/{id}` - Cancel appointment

### Consultations API (`/api/consultations`)
- `GET /api/consultations` - Get all consultations
- `GET /api/consultations/{id}` - Get consultation by ID
- `GET /api/consultations/appointment/{appointmentId}` - Get consultation by appointment
- `POST /api/consultations` - Create new consultation
- `PUT /api/consultations/{id}` - Update consultation
- `POST /api/consultations/{id}/prescriptions` - Add prescription to consultation
- `POST /api/consultations/{id}/lab-tests` - Add lab test to consultation

## Database

The application uses **SQL Server** with Entity Framework Core.

### Connection String
Configure in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=AxivoraDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### Migrations
To create and apply migrations:
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Best Practices Implemented

1. **Separation of Concerns** - Clear separation between layers
2. **Repository Pattern** - Services act as repositories
3. **DTO Pattern** - Separate models for API and database
4. **Dependency Injection** - Loose coupling between components
5. **Global Exception Handling** - Centralized error management
6. **Soft Deletes** - IsDeleted flag instead of hard deletes
7. **AutoMapper** - Automated object-to-object mapping
8. **Async/Await** - Asynchronous operations throughout
9. **REST API Standards** - Standard HTTP verbs and status codes
10. **Entity Framework Core** - Code-first approach with Fluent API

## Technology Stack

- **.NET 10** - Latest .NET framework
- **ASP.NET Core Web API** - RESTful API framework
- **Entity Framework Core 10** - ORM for database access
- **SQL Server** - Relational database
- **AutoMapper 12** - Object-to-object mapping
- **OpenAPI** - API documentation

## Running the Application

1. Update the connection string in `appsettings.json`
2. Run migrations: `dotnet ef database update`
3. Run the application: `dotnet run`
4. Access the API at `https://localhost:5001` or `http://localhost:5000`
5. View OpenAPI docs at `/openapi/v1.json` (in development mode)

## Future Enhancements

- [ ] Add authentication and authorization (JWT)
- [ ] Implement role-based access control
- [ ] Add input validation with FluentValidation
- [ ] Implement pagination for list endpoints
- [ ] Add caching layer (Redis)
- [ ] Implement audit logging
- [ ] Add comprehensive unit tests
- [ ] Add integration tests
- [ ] Implement rate limiting
- [ ] Add health checks
