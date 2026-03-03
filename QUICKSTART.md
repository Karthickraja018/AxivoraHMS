# Axivora HMS - Quick Start Guide

## Project Overview

Axivora is a comprehensive Hospital Management System backend built with .NET 10, featuring a clean architecture with AutoMapper, service layer, controllers, DTOs, and global exception handling.

## ? What Has Been Implemented

### ?? Project Structure
```
Axivora/
??? Controllers/
?   ??? PatientsController.cs
?   ??? DoctorsController.cs
?   ??? AppointmentsController.cs
?   ??? ConsultationsController.cs
??? Services/
?   ??? Interfaces/
?   ?   ??? IPatientService.cs
?   ?   ??? IDoctorService.cs
?   ?   ??? IAppointmentService.cs
?   ?   ??? IConsultationService.cs
?   ??? PatientService.cs
?   ??? DoctorService.cs
?   ??? AppointmentService.cs
?   ??? ConsultationService.cs
??? DTOs/
?   ??? PatientDto.cs
?   ??? DoctorDto.cs
?   ??? AppointmentDto.cs
?   ??? ConsultationDto.cs
?   ??? AddressDto.cs
?   ??? DepartmentDto.cs
?   ??? PatientAllergyDto.cs
??? Mappings/
?   ??? MappingProfile.cs
??? Middleware/
?   ??? GlobalExceptionHandlerMiddleware.cs
??? Models/ (Existing)
??? Data/ (Existing)
??? Program.cs (Updated)
```

### ?? Key Features Implemented

1. **Global Exception Handling**
   - Centralized error handling middleware
   - Standardized error responses
   - Automatic HTTP status code mapping
   - Comprehensive error logging

2. **AutoMapper Configuration**
   - Bidirectional mappings for all entities
   - Conditional mapping for update operations
   - Navigation property mapping

3. **Service Layer**
   - Interface-based design
   - Dependency injection ready
   - Business logic encapsulation
   - Async/await throughout

4. **RESTful Controllers**
   - Standard HTTP verbs (GET, POST, PUT, DELETE)
   - Proper status code returns
   - Route conventions
   - Action result types

5. **DTOs for API**
   - Request/Response separation
   - Create and Update DTOs
   - Nested object support

## ?? Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server (already configured as "Rakesh" server)
- Visual Studio 2022 or VS Code

### Step 1: Verify Database
Your database is already configured:
- Server: `Rakesh`
- Database: `AxivoraHMS`

### Step 2: Apply Migrations (if needed)
```bash
dotnet ef database update
```

### Step 3: Run the Application
```bash
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

## ?? Testing the API

### Example: Create a Patient

**Request:**
```http
POST /api/patients
Content-Type: application/json

{
  "fullName": "John Doe",
  "dateOfBirth": "1990-05-15",
  "gender": "Male",
  "phoneNumber": "1234567890",
  "bloodGroup": "O+",
  "emergencyContact": "9876543210",
  "addressId": 1,
  "userId": 1
}
```

**Response:**
```json
{
  "patientId": 1,
  "mrn": "MRN20250301000001",
  "fullName": "John Doe",
  "dateOfBirth": "1990-05-15",
  "gender": "Male",
  "phoneNumber": "1234567890",
  "bloodGroup": "O+",
  "emergencyContact": "9876543210",
  "address": { ... },
  "allergies": []
}
```

### Example: Get All Doctors

**Request:**
```http
GET /api/doctors
```

**Response:**
```json
[
  {
    "doctorId": 1,
    "licenseNumber": "LIC123456",
    "fullName": "Dr. Jane Smith",
    "qualification": "MD, MBBS",
    "experienceYears": 10,
    "isActive": true,
    "address": { ... },
    "departments": [ ... ]
  }
]
```

### Example: Create an Appointment

**Request:**
```http
POST /api/appointments
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 1,
  "appointmentStart": "2025-03-05T10:00:00",
  "appointmentEnd": "2025-03-05T10:30:00",
  "reason": "Regular checkup",
  "statusId": 1
}
```

### Example: Search Patients

**Request:**
```http
GET /api/patients/search?searchTerm=John
```

## ?? Service Layer Usage

All business logic is in the service layer. Example:

```csharp
public class SomeOtherService
{
    private readonly IPatientService _patientService;
    
    public SomeOtherService(IPatientService patientService)
    {
        _patientService = patientService;
    }
    
    public async Task DoSomething()
    {
        var patients = await _patientService.GetAllPatientsAsync();
        // Business logic here
    }
}
```

## ??? Error Handling

The global exception handler automatically catches and formats errors:

**Example Error Response:**
```json
{
  "statusCode": 404,
  "message": "Patient with ID 999 not found.",
  "details": "Patient with ID 999 not found."
}
```

**Exception Types Handled:**
- `KeyNotFoundException` ? 404 Not Found
- `ArgumentException` ? 400 Bad Request
- `InvalidOperationException` ? 400 Bad Request
- `UnauthorizedAccessException` ? 401 Unauthorized
- All others ? 500 Internal Server Error

## ?? Available Services

### IPatientService
- `GetAllPatientsAsync()`
- `GetPatientByIdAsync(int patientId)`
- `GetPatientByMRNAsync(string mrn)`
- `CreatePatientAsync(CreatePatientDto dto)`
- `UpdatePatientAsync(int patientId, UpdatePatientDto dto)`
- `DeletePatientAsync(int patientId)`
- `SearchPatientsAsync(string searchTerm)`

### IDoctorService
- `GetAllDoctorsAsync()`
- `GetDoctorByIdAsync(int doctorId)`
- `CreateDoctorAsync(CreateDoctorDto dto)`
- `UpdateDoctorAsync(int doctorId, UpdateDoctorDto dto)`
- `DeleteDoctorAsync(int doctorId)`
- `GetDoctorsByDepartmentAsync(int departmentId)`

### IAppointmentService
- `GetAllAppointmentsAsync()`
- `GetAppointmentByIdAsync(int appointmentId)`
- `GetAppointmentsByPatientIdAsync(int patientId)`
- `GetAppointmentsByDoctorIdAsync(int doctorId)`
- `CreateAppointmentAsync(CreateAppointmentDto dto)`
- `UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto dto)`
- `CancelAppointmentAsync(int appointmentId)`
- `GetAppointmentsByDateRangeAsync(DateTime start, DateTime end)`

### IConsultationService
- `GetAllConsultationsAsync()`
- `GetConsultationByIdAsync(int consultationId)`
- `GetConsultationByAppointmentIdAsync(int appointmentId)`
- `CreateConsultationAsync(CreateConsultationDto dto)`
- `UpdateConsultationAsync(int consultationId, CreateConsultationDto dto)`
- `AddPrescriptionAsync(int consultationId, CreatePrescriptionDto dto)`
- `AddLabTestAsync(int consultationId, CreateOrderedTestDto dto)`

## ?? AutoMapper Mappings

AutoMapper is configured to map between:
- `Patient` ? `PatientDto`
- `CreatePatientDto` ? `Patient`
- `UpdatePatientDto` ? `Patient`
- `Doctor` ? `DoctorDto`
- `CreateDoctorDto` ? `Doctor`
- `UpdateDoctorDto` ? `Doctor`
- `Appointment` ? `AppointmentDto`
- `Consultation` ? `ConsultationDto`
- And more...

## ?? Dependency Injection

All services are registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
```

## ?? Architecture Highlights

1. **Clean Architecture**: Clear separation of concerns
2. **Repository Pattern**: Services act as repositories
3. **DTO Pattern**: API models separate from database models
4. **Async/Await**: All operations are asynchronous
5. **Soft Deletes**: Using `IsDeleted` flag
6. **Auto-generated MRN**: For patients (format: MRN{date}{sequence})
7. **Appointment Conflict Check**: Prevents double-booking
8. **Navigation Properties**: Fully loaded with includes

## ?? Testing Endpoints

Use tools like:
- **Postman**
- **Thunder Client** (VS Code)
- **Swagger/OpenAPI** (available in development mode)
- **curl**

## ?? Database Schema

The system manages:
- **Users & Roles**: Authentication and authorization
- **Patients**: Demographics and medical records
- **Doctors**: Medical staff and specializations
- **Appointments**: Scheduling system
- **Consultations**: Medical visits and treatments
- **Prescriptions**: Medication orders
- **Lab Tests**: Laboratory orders and results
- **Audit Logs**: System activity tracking

## ?? Next Steps

1. **Test the APIs** using Postman or similar tools
2. **Add authentication** (JWT tokens)
3. **Implement authorization** (role-based access)
4. **Add validation** using FluentValidation
5. **Write unit tests** for services
6. **Add integration tests** for controllers
7. **Implement pagination** for large datasets
8. **Add caching** for frequently accessed data

## ?? Additional Resources

- See `README_ARCHITECTURE.md` for detailed architecture documentation
- Check individual controller/service files for specific implementation details
- Review `MappingProfile.cs` for AutoMapper configurations

## ? Build Status

**Status**: ? Build Successful

All components are properly integrated and the project compiles without errors!

---

**Happy Coding! ??**
