# Axivora HMS — Appointments & Consultations System Documentation

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Authentication Flow](#2-authentication-flow)
3. [Profile Setup](#3-profile-setup)
4. [Appointment System](#4-appointment-system)
5. [Role-by-Role Appointment Capabilities](#5-role-by-role-appointment-capabilities)
6. [Consultation System](#6-consultation-system)
7. [Role-by-Role Consultation Capabilities](#7-role-by-role-consultation-capabilities)
8. [Complete End-to-End Flows](#8-complete-end-to-end-flows)
9. [Error Handling](#9-error-handling)
10. [Pagination](#10-pagination)
11. [Key Business Rules](#11-key-business-rules)
12. [Quick Reference — All Endpoints](#12-quick-reference--all-endpoints)

---

## 1. System Overview

Axivora HMS is a Hospital Management System REST API built with **ASP.NET Core (.NET 10)**. All endpoints are protected by **JWT Bearer authentication**. The system has three roles with strictly separated permissions.

### Roles

| Role | How Created | Scope |
|---|---|---|
| **Patient** | Self-registration via `POST /api/auth/register` | Own appointments, own consultations, own profile |
| **Doctor** | Admin creates via `POST /api/doctors` | Their appointments, their consultations, patient lookup |
| **Admin** | Seeded directly in the database | Full access to everything |

### Authentication Header

Every protected request must include:

```
Authorization: Bearer <jwt_token>
```

### Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core (.NET 10) |
| Database | SQL Server (Entity Framework Core) |
| Authentication | JWT Bearer + Refresh Token rotation |
| Object Mapping | AutoMapper |
| Password Hashing | BCrypt |
| Error Handling | Global Exception Handler Middleware |

---

## 2. Authentication Flow

### 2.1 Patient Self-Registration

```
POST /api/auth/register
```

**Request body:**
```json
{
  "email": "patient@example.com",
  "password": "SecurePass123!",
  "role": "Patient"
}
```

> **Note:** Only `"Patient"` is accepted for `role`. Sending `"Doctor"` or `"Admin"` returns `400 Bad Request`.

**Response:**
```json
{
  "userId": 2,
  "email": "patient@example.com",
  "token": "<jwt>",
  "refreshToken": "<refresh_token>",
  "tokenExpiresAt": "2026-05-10T12:00:00Z",
  "role": "Patient",
  "emailVerified": false,
  "profileCompleted": false
}
```

`profileCompleted: false` until the patient calls `POST /api/patients/me`.

---

### 2.2 Login (All Roles)

```
POST /api/auth/login
```

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response:** Same shape as registration. `profileCompleted` reflects whether the patient/doctor profile exists.

---

### 2.3 Token Refresh

```
POST /api/auth/refresh-token
```

**Request body:**
```json
{
  "refreshToken": "<existing_refresh_token>"
}
```

Old refresh token is **revoked** and a new JWT + refresh token pair is issued (token rotation). Using a revoked token returns `401 Unauthorized`.

---

### 2.4 Logout / Revoke Token

```
POST /api/auth/revoke-token
```

**Request body:**
```json
{
  "refreshToken": "<refresh_token>"
}
```

Marks the refresh token as revoked. The JWT itself remains valid until its natural expiry (`ClockSkew = TimeSpan.Zero`).

---

### 2.5 Password Reset

```
POST /api/auth/forgot-password       ? sends reset token (currently logs to console)
POST /api/auth/reset-password        ? applies new password using the token
```

---

## 3. Profile Setup

A user account alone is not enough to book appointments. A **patient profile** (with `PatientId`) and a **doctor profile** (with `DoctorId`) must exist first.

### 3.1 Patient — Complete Profile

After registration the patient has a `UserId` but **no `PatientId`**. Appointments require `PatientId`.

```
POST /api/patients/me          ? create profile (first time)
GET  /api/patients/me          ? read own profile
PUT  /api/patients/{id}        ? update own profile
```

**`POST /api/patients/me` request body:**
```json
{
  "fullName": "John Smith",
  "dateOfBirth": "1990-05-15",
  "gender": "Male",
  "phoneNumber": "0712345678",
  "bloodGroup": "O+",
  "emergencyContact": "0798765432",
  "address": {
    "addressLine1": "123 Main St",
    "city": "Nairobi",
    "country": "Kenya"
  }
}
```

An auto-generated **MRN** (Medical Record Number) is assigned on first creation.

---

### 3.2 Doctor — Profile Created by Admin

A doctor cannot self-register. An admin must call:

```
POST /api/doctors
```

**Request body:**
```json
{
  "email": "dr.smith@hospital.com",
  "password": "DoctorPass123!",
  "fullName": "Dr. Jane Smith",
  "licenseNumber": "LIC-2024-001",
  "qualification": "MBBS, MD",
  "experienceYears": 10,
  "departmentIds": [1, 3],
  "address": {
    "addressLine1": "Hospital Rd",
    "city": "Nairobi",
    "country": "Kenya"
  }
}
```

This creates a `User` account, assigns the `Doctor` role, and creates the `Doctor` profile in a **single database transaction**. If any step fails, the entire transaction is rolled back.

---

## 4. Appointment System

### 4.1 Data Model

```
Appointment
??? AppointmentId         (PK, auto-generated)
??? PatientId             (FK ? Patients, required)
??? DoctorId              (FK ? Doctors, required)
??? StatusId              (FK ? AppointmentStatuses, required)
??? AppointmentStart      (DateTime, required)
??? AppointmentEnd        (DateTime, required)
??? Reason                (string, max 500 chars, optional)
??? IsDeleted             (bool, soft delete flag)
??? CreatedAt             (DateTime, set on creation)
```

### 4.2 Appointment Status Lifecycle

Status values are seeded in the `AppointmentStatuses` table. The typical progression is:

```
Scheduled ? Confirmed ? Checked-In ? In Progress ? Completed
                                                  ? Cancelled
                                                  ? No-Show
                                                  ? Rescheduled
```

**All 8 seeded statuses:**

| StatusId | StatusName | Description |
|---|---|---|
| 1 | Scheduled | Initial booking state |
| 2 | Confirmed | Doctor or admin confirmed the slot |
| 3 | Checked-In | Patient has arrived at the facility |
| 4 | In Progress | Consultation is actively happening |
| 5 | Completed | Visit is finished |
| 6 | Cancelled | Appointment was cancelled |
| 7 | No-Show | Patient did not arrive |
| 8 | Rescheduled | Moved to a different time slot |

Status is updated using the **status name string** (not the integer ID):
```
PUT /api/appointments/{id}/status
Body: { "status": "Confirmed" }
```

---

### 4.3 Time Slot Conflict Detection

When creating an appointment, the service rejects bookings where the doctor already has an **overlapping** appointment:

```
Overlap is detected when:
  newStart >= existingStart AND newStart < existingEnd
  OR
  newEnd > existingStart AND newEnd <= existingEnd
```

**Response when conflict detected:**
```json
{
  "statusCode": 400,
  "message": "Doctor already has an appointment during this time slot."
}
```

---

### 4.4 Creating an Appointment

```
POST /api/appointments
```

**Request body:**
```json
{
  "patientId": 1,
  "doctorId": 2,
  "appointmentStart": "2026-03-20T09:00:00",
  "appointmentEnd": "2026-03-20T09:30:00",
  "reason": "Routine check-up",
  "statusId": 1
}
```

> Use `statusId: 1` ("Scheduled") for new bookings. The IDs come from the seeded `AppointmentStatuses` table.

**Response (`201 Created`):**
```json
{
  "appointmentId": 5,
  "patientId": 1,
  "patientName": "John Smith",
  "doctorId": 2,
  "doctorName": "Dr. Jane Smith",
  "appointmentStart": "2026-03-20T09:00:00",
  "appointmentEnd": "2026-03-20T09:30:00",
  "reason": "Routine check-up",
  "status": "Scheduled"
}
```

---

## 5. Role-by-Role Appointment Capabilities

### 5.1 Patient

| Action | Method | Endpoint | Notes |
|---|---|---|---|
| Book an appointment | `POST` | `/api/appointments` | Requires valid `patientId`, `doctorId`, `statusId` |
| View own appointments | `GET` | `/api/appointments/me` | Paginated; optional `?status=` filter |
| Filter by status name | `GET` | `/api/appointments/me?status=Confirmed` | Pass status name exactly; `?status=all` shows everything |
| View single appointment | `GET` | `/api/appointments/{id}` | Any authenticated user |
| Update appointment details | `PUT` | `/api/appointments/{id}` | Change times, reason, or `statusId` (by integer) |
| Cancel appointment | `DELETE` | `/api/appointments/{id}` | Soft delete — sets `IsDeleted = true` |

**View own appointments with status filter:**
```
GET /api/appointments/me?pageNumber=1&pageSize=10&status=Scheduled
GET /api/appointments/me?pageNumber=1&pageSize=10&status=all
```

---

### 5.2 Doctor

| Action | Method | Endpoint | Notes |
|---|---|---|---|
| View own appointments (all) | `GET` | `/api/appointments` | Role-aware — returns only this doctor's appointments |
| View own appointments (explicit) | `GET` | `/api/appointments/doctor/me` | Paginated; optional `?date=2026-03-20` filter |
| Filter by calendar day | `GET` | `/api/appointments/doctor/me?date=2026-03-20` | Returns appointments starting on that day |
| View another doctor's appointments | `GET` | `/api/appointments/doctor/{doctorId}` | Admin and Doctor roles |
| View appointment by ID | `GET` | `/api/appointments/{id}` | Any authenticated user |
| **Update appointment status** | `PUT` | `/api/appointments/{id}/status` | Doctor and Admin only — accepts status name string |
| Update appointment details | `PUT` | `/api/appointments/{id}` | Change times, reason |
| Cancel appointment | `DELETE` | `/api/appointments/{id}` | Soft delete |
| View by date range | `GET` | `/api/appointments/date-range?startDate=...&endDate=...` | Admin and Doctor |

**Update status example:**
```
PUT /api/appointments/1/status
Body: { "status": "Confirmed" }
```

---

### 5.3 Admin

| Action | Method | Endpoint | Notes |
|---|---|---|---|
| View ALL appointments | `GET` | `/api/appointments` | Full paginated list across all patients and doctors |
| View by date range | `GET` | `/api/appointments/date-range` | Cross-doctor/patient range search |
| All doctor actions | — | — | Admin inherits all doctor-level appointment permissions |

---

## 6. Consultation System

A **Consultation** is a clinical record linked to exactly **one appointment** (enforced by a unique index on `AppointmentId`). It can only be created after the appointment exists.

### 6.1 Data Model

```
Consultation
??? ConsultationId        (PK, auto-generated)
??? AppointmentId         (FK ? Appointments, UNIQUE — one consultation per appointment)
??? ICDId                 (FK ? ICDCodes, nullable)
??? ChiefComplaint        (string, max 1000 chars)
??? Examination           (string, max 1000 chars)
??? DiagnosisNotes        (string, max 500 chars)
??? TreatmentPlan         (string, max 1000 chars)
??? Notes                 (string, internal doctor notes, no length limit)
??? CreatedAt             (DateTime, set on creation)
??? Prescriptions[]       (one-to-many, cascade delete)
??? OrderedTests[]        (one-to-many, cascade delete)
```

### 6.2 Prescription Sub-Model

Each prescription belongs to one consultation and references the `Medicines` table:

```
Prescription
??? PrescriptionId        (PK)
??? ConsultationId        (FK ? Consultations)
??? MedicineId            (FK ? Medicines)
??? Dosage                (e.g. "500mg")
??? Frequency             (e.g. "Twice daily")
??? Route                 (e.g. "Oral")
??? DurationDays          (int, nullable)
??? Instructions          (e.g. "Take after meals")
```

### 6.3 Ordered Lab Test Sub-Model

Each ordered test belongs to one consultation and references the `LabTests` table:

```
OrderedTest
??? OrderedTestId         (PK)
??? ConsultationId        (FK ? Consultations)
??? LabTestId             (FK ? LabTests)
??? Status                (default: "Pending")
??? Result                (nullable, filled later)
??? ResultDate            (DateTime, nullable)
```

### 6.4 ICD-10 Codes

The `ICDCodes` table is pre-seeded with 48+ common diagnosis codes. The doctor links a consultation to a code via `ICDId`. The response returns the `Code` string (e.g. `"I10"` for hypertension).

---

## 7. Role-by-Role Consultation Capabilities

### 7.1 Patient

| Action | Method | Endpoint | Notes |
|---|---|---|---|
| View own consultations | `GET` | `/api/consultations/me` | Paginated list of all own consultations |
| View single consultation | `GET` | `/api/consultations/{id}` | Returns `403 Forbidden` if consultation belongs to another patient |
| View by appointment | `GET` | `/api/consultations/appointment/{appointmentId}` | Any authenticated user |

> Patients are **read-only**. They cannot create, update, add prescriptions, or order lab tests.

---

### 7.2 Doctor

| Action | Method | Endpoint | Notes |
|---|---|---|---|
| View own consultations | `GET` | `/api/consultations/doctor/me` | Paginated; all consultations where they are the treating doctor |
| View by appointment | `GET` | `/api/consultations/appointment/{appointmentId}` | Any authenticated user |
| View single consultation | `GET` | `/api/consultations/{id}` | Any authenticated user |
| **Create consultation** | `POST` | `/api/consultations` | One per appointment; duplicate returns `400` |
| **Update consultation** | `PUT` | `/api/consultations/{id}` | Safe update — `AppointmentId` is never overwritten |
| **Add prescription** | `POST` | `/api/consultations/{id}/prescriptions` | Appends a new prescription to the consultation |
| **Order lab test** | `POST` | `/api/consultations/{id}/lab-tests` | Appends a new test; status defaults to `"Pending"` |

---

**Create consultation example:**
```
POST /api/consultations
```
```json
{
  "appointmentId": 1,
  "chiefComplaint": "Persistent headache for 3 days",
  "examination": "BP 120/80, Temp 37.2°C, No neck stiffness",
  "diagnosisNotes": "Tension headache, likely stress-induced",
  "treatmentPlan": "Paracetamol 500mg, rest, hydration",
  "icdId": 14
}
```

**Update consultation example** — all fields are optional, `AppointmentId` is never accepted:
```
PUT /api/consultations/1
```
```json
{
  "chiefComplaint": "Headache improving",
  "diagnosisNotes": "Resolving — viral origin suspected",
  "treatmentPlan": "Continue paracetamol, add rest",
  "internalNotes": "Patient responding well — review in 3 days"
}
```

> `internalNotes` maps to the `Notes` field on the `Consultation` model. It is not returned in the `ConsultationDto` response (internal use only).

**Add prescription example:**
```
POST /api/consultations/1/prescriptions
```
```json
{
  "medicineId": 7,
  "dosage": "500mg",
  "frequency": "Twice daily",
  "route": "Oral",
  "durationDays": 5,
  "instructions": "Take after meals"
}
```

**Order lab test example:**
```
POST /api/consultations/1/lab-tests
```
```json
{
  "labTestId": 1
}
```
Returns the full consultation with the new test appended (`status: "Pending"`).

---

### 7.3 Admin

| Action | Method | Endpoint | Notes |
|---|---|---|---|
| View ALL consultations | `GET` | `/api/consultations` | Paginated, all patients and doctors |
| All doctor actions | — | — | Full create/update/prescribe/lab-test access |

---

## 8. Complete End-to-End Flows

### Flow A — Patient Books and Tracks an Appointment

```
Step 1 — Register
POST /api/auth/register
Body: { "email": "...", "password": "...", "role": "Patient" }
? Receive JWT (profileCompleted: false)

Step 2 — Complete Patient Profile
POST /api/patients/me
Body: { "fullName": "...", "dateOfBirth": "...", "gender": "...", ... }
? PatientId is now available

Step 3 — Browse Doctors (no auth required)
GET /api/doctors
GET /api/doctors/department/{departmentId}

Step 4 — Book an Appointment
POST /api/appointments
Body: { "patientId": 1, "doctorId": 2, "appointmentStart": "...", "appointmentEnd": "...", "statusId": 1 }
? AppointmentId returned (status: "Scheduled")

Step 5 — Check Appointment Status
GET /api/appointments/me?pageNumber=1&pageSize=10&status=all
? See current status; wait for doctor to confirm

Step 6 — View Consultation After Visit
GET /api/consultations/me?pageNumber=1&pageSize=10
GET /api/consultations/{consultationId}
? See prescriptions and lab test orders
```

---

### Flow B — Doctor Manages a Patient Visit

```
Step 1 — Login
POST /api/auth/login
Body: { "email": "dr.smith@hospital.com", "password": "..." }
? Receive JWT (role: "Doctor")

Step 2 — View Today's Schedule
GET /api/appointments/doctor/me?pageNumber=1&pageSize=10&date=2026-03-20
? See all appointments for the day, ordered by start time

Step 3 — Confirm Appointment
PUT /api/appointments/1/status
Body: { "status": "Confirmed" }

Step 4 — Patient Arrives
PUT /api/appointments/1/status
Body: { "status": "Checked-In" }

Step 5 — Start Consultation
PUT /api/appointments/1/status
Body: { "status": "In Progress" }

Step 6 — Create Consultation Record
POST /api/consultations
Body: { "appointmentId": 1, "chiefComplaint": "...", "examination": "...", ... }
? ConsultationId returned

Step 7 — Prescribe Medicine
POST /api/consultations/1/prescriptions
Body: { "medicineId": 7, "dosage": "500mg", "frequency": "Twice daily", ... }

Step 8 — Order Lab Tests
POST /api/consultations/1/lab-tests
Body: { "labTestId": 1 }

Step 9 — Update Consultation Notes
PUT /api/consultations/1
Body: { "diagnosisNotes": "Updated after tests", "internalNotes": "Monitor for 48hrs" }

Step 10 — Complete Visit
PUT /api/appointments/1/status
Body: { "status": "Completed" }

Step 11 — Review Past Consultations
GET /api/consultations/doctor/me?pageNumber=1&pageSize=10
```

---

### Flow C — Admin Oversees the System

```
Step 1 — Login
POST /api/auth/login
Body: { "email": "admin@axivora.com", "password": "Admin@123!" }

Step 2 — Onboard a New Doctor
POST /api/doctors
Body: { "email": "...", "password": "...", "fullName": "...", "licenseNumber": "...",
        "departmentIds": [1, 3], ... }

Step 3 — View All Appointments
GET /api/appointments?pageNumber=1&pageSize=20

Step 4 — View All Consultations
GET /api/consultations?pageNumber=1&pageSize=20

Step 5 — Date Range Report
GET /api/appointments/date-range?startDate=2026-03-01&endDate=2026-03-31

Step 6 — Manage Patients
GET /api/patients?pageNumber=1&pageSize=20
DELETE /api/patients/{id}        ? soft delete
```

---

## 9. Error Handling

All errors are returned by the `GlobalExceptionHandlerMiddleware` in a consistent JSON shape:

```json
{
  "statusCode": 404,
  "message": "Appointment with ID 99 not found.",
  "details": "Appointment with ID 99 not found."
}
```

### Exception Mapping

| Exception Type | HTTP Status | Typical Cause |
|---|---|---|
| `KeyNotFoundException` | `404 Not Found` | Record does not exist in the database |
| `InvalidOperationException` | `400 Bad Request` | Business rule violation (duplicate consultation, time slot conflict, invalid role for registration) |
| `UnauthorizedAccessException` | `401 Unauthorized` | Wrong password, expired or revoked token |
| Model validation failure | `400 Bad Request` | Missing required fields, out-of-range values |
| Role restriction | `403 Forbidden` | Authenticated but insufficient role |
| Any other exception | `500 Internal Server Error` | Unexpected database error, mapping failure |

---

## 10. Pagination

All paginated endpoints accept these query parameters:

| Parameter | Default | Max | Description |
|---|---|---|---|
| `pageNumber` | `1` | — | Page number to retrieve (1-based) |
| `pageSize` | `10` | `100` | Number of items per page |

**Paginated response shape:**
```json
{
  "items": [ ... ],
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasPrevious": false,
  "hasNext": true
}
```

**Example:**
```
GET /api/appointments/me?pageNumber=2&pageSize=5&status=all
```

---

## 11. Key Business Rules

| Rule | Detail |
|---|---|
| **One consultation per appointment** | Attempting `POST /api/consultations` with a duplicate `appointmentId` returns `400` |
| **Doctor time slot conflict** | `POST /api/appointments` rejects overlapping bookings for the same doctor |
| **Patient profile required** | Appointments need a valid `PatientId`; patient must call `POST /api/patients/me` first |
| **Doctor profile required** | A `DoctorId` must exist before bookings can be made against it |
| **Self-registration restricted** | Only `"Patient"` role is accepted at `POST /api/auth/register` |
| **Soft deletes only** | Patients, doctors, and appointments use `IsDeleted = true`; records are never physically removed |
| **Status update by name** | `PUT /api/appointments/{id}/status` accepts the status **name string** (`"Confirmed"`), not an integer ID |
| **Consultation update is safe** | `PUT /api/consultations/{id}` uses `UpdateConsultationDto` — `AppointmentId` is never overwritten |
| **Patient consultation privacy** | A patient calling `GET /api/consultations/{id}` receives `403 Forbidden` if the consultation belongs to another patient |
| **Token rotation** | Each refresh token can be used exactly once; using a revoked token returns `401` |
| **Doctor profile lookup** | `GetDoctorAppointmentsAsync` and `GetConsultationsByDoctorUserIdAsync` resolve `DoctorId` from the JWT `userId` claim — the doctor must have a profile row in the `Doctors` table |

---

## 12. Quick Reference — All Endpoints

### Auth

| Method | Endpoint | Role | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Patient self-registration |
| `POST` | `/api/auth/login` | Public | Login for all roles |
| `POST` | `/api/auth/refresh-token` | Public | Rotate JWT + refresh token |
| `POST` | `/api/auth/revoke-token` | Public | Logout / revoke refresh token |
| `POST` | `/api/auth/forgot-password` | Public | Request password reset |
| `POST` | `/api/auth/reset-password` | Public | Apply new password |

### Appointments

| Method | Endpoint | Role | Description |
|---|---|---|---|
| `GET` | `/api/appointments` | Admin, Doctor | All appointments (Admin) / own appointments (Doctor) |
| `GET` | `/api/appointments/{id}` | Any | Single appointment by ID |
| `GET` | `/api/appointments/me` | Patient | Own appointments with optional `?status=` filter |
| `GET` | `/api/appointments/doctor/me` | Doctor | Own appointments with optional `?date=` filter |
| `GET` | `/api/appointments/doctor/{doctorId}` | Admin, Doctor | Appointments for a specific doctor |
| `GET` | `/api/appointments/patient/{patientId}` | Any | Appointments for a specific patient |
| `GET` | `/api/appointments/date-range` | Admin, Doctor | Appointments within a date range |
| `POST` | `/api/appointments` | Any | Create a new appointment |
| `PUT` | `/api/appointments/{id}/status` | Admin, Doctor | Update appointment status by name |
| `PUT` | `/api/appointments/{id}` | Any | Update appointment details |
| `DELETE` | `/api/appointments/{id}` | Any | Soft-cancel an appointment |

### Consultations

| Method | Endpoint | Role | Description |
|---|---|---|---|
| `GET` | `/api/consultations` | Admin | All consultations |
| `GET` | `/api/consultations/{id}` | Any | Single consultation (patients see own only) |
| `GET` | `/api/consultations/me` | Patient | Own consultations paginated |
| `GET` | `/api/consultations/doctor/me` | Doctor | Own consultations paginated |
| `GET` | `/api/consultations/appointment/{appointmentId}` | Any | Consultation by appointment |
| `POST` | `/api/consultations` | Admin, Doctor | Create a new consultation |
| `PUT` | `/api/consultations/{id}` | Admin, Doctor | Update consultation details |
| `POST` | `/api/consultations/{id}/prescriptions` | Admin, Doctor | Add a prescription |
| `POST` | `/api/consultations/{id}/lab-tests` | Admin, Doctor | Order a lab test |

### Patients

| Method | Endpoint | Role | Description |
|---|---|---|---|
| `GET` | `/api/patients` | Admin | All patients paginated |
| `GET` | `/api/patients/{id}` | Admin, Doctor, Own | Patient by ID |
| `GET` | `/api/patients/me` | Patient | Own profile |
| `GET` | `/api/patients/mrn/{mrn}` | Admin, Doctor | Patient by MRN |
| `GET` | `/api/patients/search?searchTerm=` | Admin, Doctor | Search by name/MRN/phone |
| `POST` | `/api/patients/me` | Patient | Create/restore own profile |
| `POST` | `/api/patients` | Admin | Create patient with user account |
| `PUT` | `/api/patients/{id}` | Admin, Own | Update patient details |
| `DELETE` | `/api/patients/{id}` | Admin | Soft-delete patient |

### Doctors

| Method | Endpoint | Role | Description |
|---|---|---|---|
| `GET` | `/api/doctors` | Public | All doctors paginated |
| `GET` | `/api/doctors/{id}` | Public | Doctor by ID |
| `GET` | `/api/doctors/department/{departmentId}` | Public | Doctors by department |
| `POST` | `/api/doctors` | Admin | Create doctor with user account |
| `PUT` | `/api/doctors/{id}` | Admin | Update doctor details |
| `DELETE` | `/api/doctors/{id}` | Admin | Soft-delete doctor |

### Lab Tests

| Method | Endpoint | Role | Description |
|---|---|---|---|
| `PUT` | `/api/lab-tests/{orderedTestId}/result` | Admin, Doctor | Upload or update a lab test result |
| `GET` | `/api/lab-tests/patient/{patientId}` | Admin, Doctor | All lab results for a patient |
| `GET` | `/api/lab-tests/consultation/{consultationId}` | Admin, Doctor | All lab tests ordered during a consultation |

---

*Last updated: 2026 — Axivora HMS v1.0*
