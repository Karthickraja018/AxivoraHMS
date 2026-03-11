# Axivora HMS — REST API Documentation

> **Version:** 1.3 · **Platform:** .NET 10 / C# 14 · **Auth:** JWT Bearer HS256

---

## Table of Contents

1. [Overview](#1-overview)
2. [Authentication & Token Lifecycle](#2-authentication--token-lifecycle)
3. [Roles & Access Control](#3-roles--access-control)
4. [Global Error Handling](#4-global-error-handling)
5. [Pagination](#5-pagination)
6. [Auth Endpoints](#6-auth-endpoints----apiauth)
7. [Doctor Endpoints](#7-doctor-endpoints----apidoctors)
8. [Patient Endpoints](#8-patient-endpoints----apipatients)
9. [Appointment Endpoints](#9-appointment-endpoints----apiappointments)
10. [Consultation Endpoints](#10-consultation-endpoints----apiconsultations)
11. [Lab Test Endpoints](#11-lab-test-endpoints----apilab-tests)
12. [Doctor Schedule Endpoints](#12-doctor-schedule-endpoints)
13. [Medical History Endpoints](#13-medical-history-endpoints)
14. [Feedback Endpoints](#14-feedback-endpoints----apifeedback)
15. [Admin Report Endpoints](#15-admin-report-endpoints----apiadminreports)
16. [Medicine Catalogue Endpoints](#16-medicine-catalogue-endpoints----apimedicines)
17. [Appointment Status State Machine](#17-appointment-status-state-machine)
18. [Data Schemas](#18-data-schemas)
19. [Quick Reference](#19-quick-reference)

---

## 1. Overview

Axivora HMS is a Hospital Management System REST API built with ASP.NET Core (.NET 10).  
Endpoints cover: authentication, patient management, doctor management, appointment scheduling
(with schedule-aware booking validation and rescheduling), clinical consultations (prescriptions + lab-test orders),
lab test result management, doctor availability scheduling, patient feedback, admin reporting, and full patient medical history.

**Base URL**
```
https://<host>/api
```

**Tech Stack**

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / C# 14 |
| Database | SQL Server — Entity Framework Core 10 |
| Authentication | JWT Bearer — HS256 |
| Object Mapping | AutoMapper 13 |
| API Docs (dev) | Swagger / OpenAPI |

---

## 2. Authentication & Token Lifecycle

Include the JWT in every protected request:

```
Authorization: Bearer <token>
```

### Token Properties

| Property | Value |
|---|---|
| Issuer | `AxivoraHMS` |
| Audience | `AxivoraHMS-Users` |
| Access Token Expiry | 60 minutes (`JwtSettings:ExpiryMinutes`) |
| Refresh Token Expiry | 7 days (`JwtSettings:RefreshTokenExpiryDays`) |
| Algorithm | HS256 (HMAC-SHA256) |
| Refresh Token Storage | Database — `RefreshTokens` table (max 512 chars) |
| Rotation Strategy | Old token revoked on every refresh call |

### Token Lifecycle

1. `POST /api/auth/login` (or `/register`) — returns `token` **and** `refreshToken`.
2. When the access token expires ? `POST /api/auth/refresh-token` with the `refreshToken`.
3. Server revokes the old refresh token and returns a fresh pair.
4. To log out a specific device ? `POST /api/auth/revoke-token` with the `refreshToken`.
5. A revoked or expired refresh token returns **401 Unauthorized**.

### RefreshToken Model

| Field | Type | Description |
|---|---|---|
| `id` | `int` | Primary key |
| `userId` | `int` | Owning user ID |
| `token` | `string` | Cryptographically random 64-byte Base64 value |
| `expiresAt` | `DateTime` | UTC expiry |
| `createdAt` | `DateTime` | UTC creation timestamp |
| `isRevoked` | `bool` | `true` once revoked by rotation or explicit call |
| `revokedAt` | `DateTime?` | UTC revocation timestamp (`null` if still active) |

---

## 3. Roles & Access Control

| Role | Description | How Created |
|---|---|---|
| `Patient` | Self-registered end users | `POST /api/auth/register` |
| `Doctor` | Medical staff | `POST /api/doctors` (Admin only) |
| `LabTechnician` | Lab staff | Admin / manual DB insert |
| `Admin` | Full system access | Database seed / manual insert |

---

## 4. Global Error Handling

All unhandled exceptions are caught by `GlobalExceptionHandlerMiddleware`:

```json
{ "statusCode": 404, "message": "...", "details": "..." }
```

| Exception | HTTP Status |
|---|---|
| `KeyNotFoundException` | 404 Not Found |
| `ArgumentException` | 400 Bad Request |
| `InvalidOperationException` | 400 Bad Request |
| `UnauthorizedAccessException` | 401 Unauthorized |
| Any other | 500 Internal Server Error |

---

## 5. Pagination

Paginated endpoints accept `pageNumber` (default `1`) and `pageSize` (default `10`, max `100`).

**Response envelope:**
```json
{
  "items": [...],
  "totalCount": 50,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasPrevious": false,
  "hasNext": true
}
```

---

## 6. Auth Endpoints — `/api/auth`

---

### `POST /api/auth/register`
**Auth:** Public

Register a new Patient user. Only the `Patient` role is accepted for self-registration.

| Field | Type | Required | Description |
|---|---|---|---|
| `email` | `string` | Yes | Valid email (max 150 chars) |
| `password` | `string` | Yes | Min 8 chars — uppercase + lowercase + digit + special char |
| `confirmPassword` | `string` | Yes | Must match `password` |
| `role` | `string` | No | Always `"Patient"` — any other value is rejected |

**Example**
```json
{
  "email": "user@example.com",
  "password": "P@ssword1",
  "confirmPassword": "P@ssword1",
  "role": "Patient"
}
```

| Status | Description |
|---|---|
| 201 Created | Returns `AuthResponseDto` (includes `token` + `refreshToken`) |
| 400 Bad Request | Validation error, email already registered, or non-Patient role |

---

### `POST /api/auth/login`
**Auth:** Public

| Field | Type | Required | Description |
|---|---|---|---|
| `email` | `string` | Yes | Registered email |
| `password` | `string` | Yes | Account password |

**Example**
```json
{ "email": "user@example.com", "password": "P@ssword1" }
```

| Status | Description |
|---|---|
| 200 OK | Returns `AuthResponseDto` (includes `token` + `refreshToken` + `tokenExpiresAt`) |
| 400 Bad Request | Validation error |
| 401 Unauthorized | Invalid email or password, or account disabled |

---

### `POST /api/auth/verify-email`
**Auth:** Public · Query params: `email`, `code`

| Status | Description |
|---|---|
| 200 OK | `{ "message": "Email verified successfully" }` |
| 400 Bad Request | Invalid verification code |
| 404 Not Found | User not found |

---

### `POST /api/auth/forgot-password`
**Auth:** Public · Body: `{ "email": "user@example.com" }`

| Status | Description |
|---|---|
| 200 OK | Reset link sent to email |
| 404 Not Found | Email not registered |

---

### `POST /api/auth/reset-password`
**Auth:** Public

| Field | Type | Required | Description |
|---|---|---|---|
| `email` | `string` | Yes | Registered email |
| `resetToken` | `string` | Yes | Token received via email |
| `newPassword` | `string` | Yes | Same complexity rules as registration |
| `confirmPassword` | `string` | Yes | Must match `newPassword` |

| Status | Description |
|---|---|
| 200 OK | Password reset successfully |
| 400 Bad Request | Invalid / expired token or validation error |
| 404 Not Found | User not found |

---

### `POST /api/auth/refresh-token`
**Auth:** Public

Issues a new JWT access token and refresh token using a valid, non-revoked refresh token.
Implements **token rotation** — the supplied token is immediately revoked.

**Request Body:** `{ "refreshToken": "<base64-token>" }`

| Status | Description |
|---|---|
| 200 OK | Returns full `AuthResponseDto` with new `token`, `refreshToken`, `tokenExpiresAt` |
| 400 Bad Request | `refreshToken` field missing |
| 401 Unauthorized | Token not found, already revoked, or expired |

> **Note:** Always replace the stored refresh token with the one returned — the old token is invalidated immediately.

---

### `POST /api/auth/revoke-token`
**Auth:** Public

Revokes a refresh token, logging out that session. The JWT remains valid until it expires naturally.

**Request Body:** `{ "refreshToken": "<base64-token>" }`

| Status | Description |
|---|---|
| 200 OK | `{ "message": "Token revoked successfully." }` |
| 400 Bad Request | Token not found or already revoked |

---

## 7. Doctor Endpoints — `/api/doctors`

---

### `GET /api/doctors`
**Auth:** Public · Query: `pageNumber`, `pageSize`

Returns `PaginationResponse<DoctorDto>`.

---

### `GET /api/doctors/{id}`
**Auth:** Public

| Status | Description |
|---|---|
| 200 OK | Returns `DoctorDto` |
| 404 Not Found | Doctor not found |

---

### `GET /api/doctors/department/{departmentId}`
**Auth:** Public

Returns `IEnumerable<DoctorDto>` for all doctors in the department.

---

### `POST /api/doctors`
**Auth:** Required — Admin only

Creates a doctor with a linked user account in one transaction.

| Field | Type | Required | Description |
|---|---|---|---|
| `email` | `string` | Yes | Unique email (max 150 chars) |
| `password` | `string` | Yes | Min 8 chars |
| `licenseNumber` | `string` | Yes | Unique medical license (max 100 chars) |
| `fullName` | `string` | Yes | Doctor's full name (max 150 chars) |
| `qualification` | `string` | No | e.g. MBBS, MD (max 150 chars) |
| `experienceYears` | `int` | No | Range 0–100 |
| `address` | `CreateAddressDto` | No | See [AddressDto](#addressdto--createaddressdto) |
| `departmentIds` | `int[]` | Yes | At least one valid department ID |

| Status | Description |
|---|---|
| 201 Created | Returns `DoctorDto` |
| 400 Bad Request | Email/license already exists, or invalid department |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |

---

### `PUT /api/doctors/{id}`
**Auth:** Required — Admin only

| Field | Type | Required | Description |
|---|---|---|---|
| `fullName` | `string` | No | Updated full name |
| `qualification` | `string` | No | Updated qualification |
| `experienceYears` | `int` | No | Range 0–100 |
| `addressId` | `int` | No | Reference to existing address |
| `isActive` | `bool` | No | Active / inactive flag |

| Status | Description |
|---|---|
| 200 OK | Returns updated `DoctorDto` |
| 404 Not Found | Doctor not found |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |

---

### `DELETE /api/doctors/{id}`
**Auth:** Required — Admin only

Soft-delete (`IsDeleted = true`, `IsActive = false`).

| Status | Description |
|---|---|
| 204 No Content | Deleted successfully |
| 404 Not Found | Doctor not found |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |

---

## 8. Patient Endpoints — `/api/patients`

---

### `GET /api/patients`
**Auth:** Required — Admin only · Query: `pageNumber`, `pageSize`

Returns `PaginationResponse<PatientDto>`.

---

### `GET /api/patients/{id}`
**Auth:** Required — Admin / Doctor / record owner

> **Ownership rule:** Non-Admin/Doctor callers may only access their own record (verified via `UserId` JWT claim).

| Status | Description |
|---|---|
| 200 OK | Returns `PatientDto` |
| 403 Forbidden | Caller is not owner, Admin, or Doctor |
| 404 Not Found | Patient not found |

---

### `GET /api/patients/mrn/{mrn}`
**Auth:** Required — Admin / Doctor

| Status | Description |
|---|---|
| 200 OK | Returns `PatientDto` |
| 404 Not Found | MRN not found |

---

### `GET /api/patients/search`
**Auth:** Required — Admin / Doctor · Query: `searchTerm`

Returns `IEnumerable<PatientDto>` (empty list for blank/null term).

---

### `GET /api/patients/me`
**Auth:** Required — Patient only

Returns the authenticated patient's own profile.

| Status | Description |
|---|---|
| 200 OK | Returns `PatientDto` |
| 404 Not Found | Profile not yet created |

---

### `POST /api/patients/me`
**Auth:** Required — Patient only

Creates (or restores a soft-deleted) patient profile for the authenticated user.

| Field | Type | Required | Description |
|---|---|---|---|
| `fullName` | `string` | Yes | Max 150 chars |
| `dateOfBirth` | `DateOnly` | Yes | ISO-8601 e.g. `1990-05-15` |
| `gender` | `string` | Yes | e.g. Male / Female / Other (max 20 chars) |
| `phoneNumber` | `string` | Yes | Max 20 chars |
| `bloodGroup` | `string` | No | e.g. O+ (max 10 chars) |
| `emergencyContact` | `string` | No | Max 20 chars |
| `address` | `CreateAddressDto` | Yes | See [AddressDto](#addressdto--createaddressdto) |

| Status | Description |
|---|---|
| 201 Created | Returns `PatientDto` |
| 400 Bad Request | Validation error |
| 401 Unauthorized | Token missing or UserId not found in claims |

---

### `POST /api/patients`
**Auth:** Required — Admin only

Creates a patient together with a new user account in one transaction.

| Field | Type | Required |
|---|---|---|
| `email` | `string` | Yes |
| `password` | `string` | Yes |
| `fullName` | `string` | Yes |
| `dateOfBirth` | `DateOnly` | Yes |
| `gender` | `string` | Yes |
| `phoneNumber` | `string` | No |
| `bloodGroup` | `string` | No |
| `emergencyContact` | `string` | No |
| `address` | `CreateAddressDto` | Yes |

| Status | Description |
|---|---|
| 201 Created | Returns `PatientDto` |
| 400 Bad Request | Email already exists or validation error |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |

---

### `PUT /api/patients/{id}`
**Auth:** Required — Admin or record owner

| Field | Type | Description |
|---|---|---|
| `fullName` | `string` | Max 150 chars |
| `phoneNumber` | `string` | Max 20 chars |
| `bloodGroup` | `string` | Max 10 chars |
| `emergencyContact` | `string` | Max 20 chars |
| `addressId` | `int` | Reference to existing address |

| Status | Description |
|---|---|
| 200 OK | Returns updated `PatientDto` |
| 403 Forbidden | Not authorized for this record |
| 404 Not Found | Patient not found |

---

### `DELETE /api/patients/{id}`
**Auth:** Required — Admin only

Soft-delete (`IsDeleted = true`).

---

## 9. Appointment Endpoints — `/api/appointments`

> **Booking validation:** When creating an appointment the service verifies:
> 1. No overlapping appointment exists for the same doctor in the requested time window.
> 2. The time window falls within an **active** `DoctorSchedule` entry for that day-of-week (uses .NET `DayOfWeek` — `0 = Sunday … 6 = Saturday`).
>
> **Ownership** for `GET /{id}`, `PUT /{id}`, `PATCH /{id}/reschedule`, and `DELETE /{id}`:
> - **Admin** — always allowed.
> - **Doctor** — must be the appointment's assigned doctor (`UserId` ? `DoctorId`).
> - **Patient** — must be the appointment's patient (`UserId` ? `PatientId`).
>
> **Audit:** When a Doctor or Admin creates an appointment an entry is written to `AuditLogs`.

---

### `GET /api/appointments`
**Auth:** Required — Admin / Doctor · Query: `pageNumber`, `pageSize`

- **Admin** — returns all appointments (paginated, ordered by `AppointmentStart` desc).
- **Doctor** — automatically scoped to **their own appointments** (resolved from JWT `UserId`), ordered by `AppointmentStart` asc.

Returns `PaginationResponse<AppointmentDto>`.

---

### `GET /api/appointments/{id}`
**Auth:** Required — any authenticated user (ownership enforced)

| Status | Description |
|---|---|
| 200 OK | Returns `AppointmentDto` |
| 403 Forbidden | Caller does not own the appointment |
| 404 Not Found | Appointment not found |

---

### `GET /api/appointments/patient/{patientId}`
**Auth:** Required — any authenticated user

> **Patient restriction:** A Patient caller is forbidden unless `patientId` matches their own `PatientId`.

Returns `IEnumerable<AppointmentDto>`.

---

### `GET /api/appointments/doctor/me`
**Auth:** Required — Doctor only · Query: `pageNumber`, `pageSize`, `date` (optional `DateTime` UTC)

Returns the authenticated doctor's own appointments, ordered by `AppointmentStart` asc.  
Pass `date` to filter to a single calendar day.

Returns `PaginationResponse<AppointmentDto>`.

---

### `GET /api/appointments/doctor/{doctorId}`
**Auth:** Required — Admin / Doctor

Returns `IEnumerable<AppointmentDto>` for all non-deleted appointments for the given doctor.

---

### `GET /api/appointments/date-range`
**Auth:** Required — Admin / Doctor · Query: `startDate` (DateTime UTC), `endDate` (DateTime UTC)

Returns `IEnumerable<AppointmentDto>` where `AppointmentStart` is within the range (inclusive).

---

### `GET /api/appointments/me`
**Auth:** Required — Patient only · Query: `pageNumber`, `pageSize`, `status` (optional string)

Returns the authenticated patient's own appointments, ordered by `AppointmentStart` desc.  
Pass `status` to filter by status name (e.g. `"Scheduled"`). Pass `"all"` or omit to return every status.

Returns `PaginationResponse<AppointmentDto>`.

---

### `POST /api/appointments`
**Auth:** Required — any authenticated user

> **Patient shortcut:** When the caller is a Patient, `patientId` in the body is **ignored** — the service resolves the patient from the JWT `UserId` claim automatically.

| Field | Type | Required | Description |
|---|---|---|---|
| `patientId` | `int` | Yes* | *Ignored for Patient callers |
| `doctorId` | `int` | Yes | Existing doctor ID |
| `appointmentStart` | `DateTime` | Yes | UTC start time |
| `appointmentEnd` | `DateTime` | Yes | UTC end time |
| `reason` | `string` | No | Max 500 chars |
| `statusId` | `int` | Yes | e.g. `1` = Scheduled |

**Example**
```json
{
  "patientId": 1,
  "doctorId": 2,
  "appointmentStart": "2025-07-10T09:00:00",
  "appointmentEnd":   "2025-07-10T09:30:00",
  "reason": "Annual check-up",
  "statusId": 1
}
```

| Status | Description |
|---|---|
| 201 Created | Returns `AppointmentDto`; `Location` ? `GET /api/appointments/{id}` |
| 400 Bad Request | Time slot conflict, outside doctor's schedule, or validation error |
| 404 Not Found | Doctor, Patient, or StatusId not found |
| 401 Unauthorized | Token missing |

---

### `PATCH /api/appointments/{id}/reschedule`
**Auth:** Required — any authenticated user (ownership enforced)

Moves an existing appointment to a new time window without changing any other fields.

> **Allowed statuses:** Only appointments whose current status is **not** `Completed` or `Cancelled` can be rescheduled.  
> **Slot conflict check:** The new window must not overlap any other non-deleted appointment for the same doctor.  
> **Patient restriction:** A Patient caller may only reschedule their own appointment.

| Field | Type | Required | Description |
|---|---|---|---|
| `appointmentStart` | `DateTime` | Yes | New UTC start time |
| `appointmentEnd` | `DateTime` | Yes | New UTC end time (must be after start — validated by `IValidatableObject`) |

**Example**
```json
{
  "appointmentStart": "2025-07-12T10:00:00",
  "appointmentEnd":   "2025-07-12T10:30:00"
}
```

| Status | Description |
|---|---|
| 200 OK | Returns updated `AppointmentDto` |
| 400 Bad Request | `appointmentEnd` ? `appointmentStart` |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Caller does not own the appointment |
| 404 Not Found | Appointment not found or soft-deleted |
| 409 Conflict | Doctor already has an overlapping appointment in the requested window |

---

### `PUT /api/appointments/{id}/status`
**Auth:** Required — Admin / Doctor only

Transitions the appointment to a new named status. Validated against the [status state machine](#17-appointment-status-state-machine).

**Request Body:** `{ "status": "Confirmed" }`

| Status | Description |
|---|---|
| 200 OK | Returns updated `AppointmentDto` |
| 400 Bad Request | Transition not permitted for this role or from this state |
| 404 Not Found | Appointment or target status not found |

---

### `PUT /api/appointments/{id}`
**Auth:** Required — any authenticated user (ownership enforced)

Updates appointment fields. If `statusId` is included the transition is validated against the state machine.

| Field | Type | Description |
|---|---|---|
| `appointmentStart` | `DateTime?` | New UTC start time |
| `appointmentEnd` | `DateTime?` | New UTC end time |
| `reason` | `string` | Updated reason |
| `statusId` | `int?` | New status ID — validated via state machine |

| Status | Description |
|---|---|
| 200 OK | Returns updated `AppointmentDto` |
| 400 Bad Request | Status transition not allowed |
| 403 Forbidden | Caller does not own the appointment |
| 404 Not Found | Appointment not found |

---

### `DELETE /api/appointments/{id}`
**Auth:** Required — any authenticated user (ownership enforced)

Soft-deletes (`IsDeleted = true`) the appointment.

| Status | Description |
|---|---|
| 204 No Content | Cancelled successfully |
| 403 Forbidden | Caller does not own the appointment |
| 404 Not Found | Appointment not found |

---

## 10. Consultation Endpoints — `/api/consultations`

> **Controller-level auth:** Doctor, Admin, or Patient.  
> Write endpoints (POST, PUT) are Doctor / Admin only.  
> **Status rule on create:** Linked appointment must have status `Checked-In`, `In Progress`, or `Completed`.  
> **Doctor ownership on create:** A Doctor caller can only create a consultation for their own appointment.  
> **Duplicate guard:** Only one consultation per appointment is allowed.  
> **`appointmentId` is immutable** after creation — PUT updates preserve the original value.  
> **Prescription duplicate guard:** The same medicine cannot be prescribed twice in one consultation.

---

### `GET /api/consultations`
**Auth:** Required — Admin only · Query: `pageNumber`, `pageSize`

Returns `PaginationResponse<ConsultationDto>` ordered by `CreatedAt` desc.

---

### `GET /api/consultations/doctor/me`
**Auth:** Required — Doctor only · Query: `pageNumber`, `pageSize`

Returns the authenticated doctor's own consultations, ordered by `CreatedAt` desc.

Returns `PaginationResponse<ConsultationDto>`.

---

### `GET /api/consultations/{id}`
**Auth:** Required — Doctor / Admin / Patient

> **Patient restriction:** A Patient caller is forbidden unless the consultation's linked appointment belongs to their own patient record.

| Status | Description |
|---|---|
| 200 OK | Returns `ConsultationDto` |
| 403 Forbidden | Patient caller does not own this consultation |
| 404 Not Found | Consultation not found |

---

### `GET /api/consultations/appointment/{appointmentId}`
**Auth:** Required — Doctor / Admin / Patient

| Status | Description |
|---|---|
| 200 OK | Returns `ConsultationDto` |
| 404 Not Found | No consultation found for this appointment |

---

### `GET /api/consultations/me`
**Auth:** Required — Patient only · Query: `pageNumber`, `pageSize`

Returns the authenticated patient's own consultations, ordered by `CreatedAt` desc.

Returns `PaginationResponse<ConsultationDto>`.

---

### `POST /api/consultations`
**Auth:** Required — Doctor / Admin

| Field | Type | Required | Description |
|---|---|---|---|
| `appointmentId` | `int` | Yes | Must be `Checked-In`, `In Progress`, or `Completed` |
| `chiefComplaint` | `string` | No | Max 1000 chars |
| `examination` | `string` | No | Max 1000 chars |
| `diagnosisNotes` | `string` | No | Max 500 chars |
| `treatmentPlan` | `string` | No | Max 1000 chars |
| `icdId` | `int` | No | ICD-10 code reference ID |

**Example**
```json
{
  "appointmentId": 5,
  "chiefComplaint": "Headache and fever",
  "examination":    "BP 120/80, Temp 38.5 C",
  "diagnosisNotes": "Viral fever",
  "treatmentPlan":  "Rest, fluids, paracetamol",
  "icdId": 12
}
```

| Status | Description |
|---|---|
| 201 Created | Returns `ConsultationDto`; `Location` ? `GET /api/consultations/{id}` |
| 400 Bad Request | Appointment status invalid, or consultation already exists |
| 403 Forbidden | Doctor caller is not the appointment's assigned doctor |

---

### `PUT /api/consultations/{id}`
**Auth:** Required — Doctor / Admin

`appointmentId` is **read-only** — any value sent in the body is silently ignored.

| Field | Type | Description |
|---|---|---|
| `chiefComplaint` | `string` | Updated complaint |
| `examination` | `string` | Updated examination notes |
| `diagnosisNotes` | `string` | Updated diagnosis |
| `treatmentPlan` | `string` | Updated treatment plan |
| `icdId` | `int?` | Updated ICD-10 reference |

| Status | Description |
|---|---|
| 200 OK | Returns updated `ConsultationDto` |
| 404 Not Found | Consultation not found |

---

### `POST /api/consultations/{id}/prescriptions`
**Auth:** Required — Doctor / Admin

> Same `medicineId` cannot appear twice in the same consultation.

| Field | Type | Required | Description |
|---|---|---|---|
| `medicineId` | `int` | Yes | Existing Medicine catalog ID |
| `dosage` | `string` | No | Max 50 chars |
| `frequency` | `string` | No | Max 50 chars |
| `route` | `string` | No | Max 50 chars |
| `durationDays` | `int` | No | Number of days |
| `instructions` | `string` | No | Max 200 chars |

| Status | Description |
|---|---|
| 200 OK | Returns updated `ConsultationDto` including new prescription |
| 400 Bad Request | Medicine already prescribed in this consultation |
| 404 Not Found | Consultation not found |

---

### `POST /api/consultations/{id}/lab-tests`
**Auth:** Required — Doctor / Admin

| Field | Type | Required | Description |
|---|---|---|---|
| `labTestId` | `int` | Yes | Existing LabTest catalog ID |

Ordered test is created with `Status = "Pending"`.

| Status | Description |
|---|---|
| 200 OK | Returns updated `ConsultationDto` including new ordered test |
| 404 Not Found | Consultation not found |

---

## 11. Lab Test Endpoints — `/api/lab-tests`

---

### `PUT /api/lab-tests/{orderedTestId}/result`
**Auth:** Required — Admin / Doctor

Uploads or updates the result for an ordered test and sets `ResultDate` to `UtcNow`.

| Field | Type | Required | Description |
|---|---|---|---|
| `result` | `string` | Yes | Result text / value (max 2000 chars) |

**Example:** `{ "result": "Haemoglobin: 13.5 g/dL — Normal" }`

| Status | Description |
|---|---|
| 200 OK | Returns `LabResultDto` with updated `result` and `resultDate` |
| 400 Bad Request | `result` field missing |
| 404 Not Found | OrderedTest not found |
| 403 Forbidden | Not Admin or Doctor |

---

### `GET /api/lab-tests/patient/{patientId}`
**Auth:** Required — Admin / Doctor

Returns all lab test results for a patient across all consultations.

Returns `IEnumerable<LabResultDto>`.

---

### `GET /api/lab-tests/consultation/{consultationId}`
**Auth:** Required — Admin / Doctor

Returns all lab tests ordered during a specific consultation.

Returns `IEnumerable<LabResultDto>`.

---

### `GET /api/lab-tests/catalogue`
**Auth:** Required — Admin / Doctor / Patient · Query: `search` (optional), `pageNumber` (default `1`), `pageSize` (default `20`, max `100`)

Queries the `LabTests` **catalogue** table (not ordered test results). Search is a case-insensitive partial match on `TestName`. Results are sorted alphabetically.

Returns `PaginationResponse<LabTestCatalogueDto>`.

---

### `GET /api/lab-tests/catalogue/{id}`
**Auth:** Required — Admin / Doctor / Patient

Returns a single lab test catalogue entry by `LabTestId`.

| Status | Description |
|---|---|
| 200 OK | Returns `LabTestCatalogueDto` |
| 404 Not Found | No lab test with the given ID |

---

## 12. Doctor Schedule Endpoints

Routes are nested under `/api/doctors`.

> **Overlap guard (create & update):** A new or updated schedule cannot overlap an existing **active** schedule for the same doctor on the same day-of-week.  
> **Time validation:** `EndTime` must be after `StartTime`.  
> **Doctor ownership:** A Doctor caller can only update/delete their **own** schedule slots (matched by JWT `UserId` ? `DoctorId`). Admin bypasses this check.

---

### `POST /api/doctors/{doctorId}/schedule`
**Auth:** Required — Admin / Doctor

| Field | Type | Required | Description |
|---|---|---|---|
| `dayOfWeek` | `int` | Yes | `0 = Sunday … 6 = Saturday` |
| `startTime` | `TimeSpan` | Yes | e.g. `"09:00:00"` |
| `endTime` | `TimeSpan` | Yes | e.g. `"17:00:00"` (must be > `startTime`) |
| `slotDurationMinutes` | `int` | No | Range 5–120, default `15` |

**Example**
```json
{
  "dayOfWeek": 1,
  "startTime": "09:00:00",
  "endTime": "17:00:00",
  "slotDurationMinutes": 30
}
```

| Status | Description |
|---|---|
| 201 Created | Returns `DoctorScheduleDto` with `generatedSlots` |
| 400 Bad Request | `endTime ? startTime` or overlap with existing schedule |
| 404 Not Found | Doctor not found |
| 403 Forbidden | Not Admin or Doctor |

---

### `GET /api/doctors/{doctorId}/schedule`
**Auth:** Public

Returns all schedule slots ordered by `DayOfWeek` then `StartTime`.

Returns `IEnumerable<DoctorScheduleDto>` (each includes `generatedSlots`).

| Status | Description |
|---|---|
| 200 OK | Returns schedule list |
| 404 Not Found | Doctor not found |

---

### `PUT /api/doctors/schedule/{scheduleId}`
**Auth:** Required — Admin / Doctor (own schedule only)

All fields are optional — only provided fields are updated.

| Field | Type | Description |
|---|---|---|
| `dayOfWeek` | `int?` | New day (0–6) |
| `startTime` | `TimeSpan?` | New start time |
| `endTime` | `TimeSpan?` | New end time |
| `slotDurationMinutes` | `int?` | Range 5–120 |
| `isActive` | `bool?` | Enable / disable this slot |

| Status | Description |
|---|---|
| 200 OK | Returns updated `DoctorScheduleDto` |
| 400 Bad Request | `endTime ? startTime` or would create overlap |
| 403 Forbidden | Doctor caller does not own this schedule |
| 404 Not Found | Schedule not found |

---

### `DELETE /api/doctors/schedule/{scheduleId}`
**Auth:** Required — Admin / Doctor (own schedule only)

Hard-deletes the schedule slot.

| Status | Description |
|---|---|
| 204 No Content | Deleted successfully |
| 403 Forbidden | Doctor caller does not own this schedule |
| 404 Not Found | Schedule not found |

---

## 13. Medical History Endpoints

Routes are nested under `/api/patients`.

---

### `GET /api/patients/{patientId}/medical-history`
**Auth:** Required — Admin / Doctor

Returns a full chronological medical history including all visits, consultations, prescriptions, and lab test results.

| Status | Description |
|---|---|
| 200 OK | Returns `MedicalHistoryDto` |
| 404 Not Found | Patient not found |
| 403 Forbidden | Not Admin or Doctor |

---

### `GET /api/patients/me/medical-history`
**Auth:** Required — Patient only

Returns the authenticated patient's own medical history. `UserId` is resolved from the JWT claim.

| Status | Description |
|---|---|
| 200 OK | Returns `MedicalHistoryDto` |
| 404 Not Found | Patient profile not found |

---

## 14. Feedback Endpoints — `/api/feedback`

> **One feedback per consultation** — enforced by a UNIQUE constraint on `ConsultationId`.  
> Feedback can only be submitted for consultations whose linked appointment has status **`Completed`**.  
> Only the submitting Patient may edit or delete their own feedback (Admin can delete any).

---

### `POST /api/feedback`
**Auth:** Required — Patient only

Submit feedback for a completed consultation.

| Field | Type | Required | Description |
|---|---|---|---|
| `consultationId` | `int` | Yes | The completed consultation to rate |
| `rating` | `int` | Yes | 1 (Very Poor) to 5 (Excellent) |
| `comment` | `string` | No | Optional free text (max 1000 chars) |

**Example**
```json
{
  "consultationId": 12,
  "rating": 5,
  "comment": "Very professional and thorough."
}
```

| Status | Description |
|---|---|
| 201 Created | Returns `SessionFeedbackDto`; `Location` ? `GET /api/feedback/consultation/{consultationId}` |
| 400 Bad Request | Validation error |
| 403 Forbidden | Consultation does not belong to the caller's patient record |
| 404 Not Found | Consultation or patient profile not found |
| 409 Conflict | Feedback already submitted for this consultation, or appointment is not `Completed` |

---

### `PUT /api/feedback/{feedbackId}`
**Auth:** Required — Patient only (own feedback)

Edit the `rating` and/or `comment` of existing feedback. Sets `isEdited = true` and `updatedAt = UtcNow`.

| Field | Type | Required | Description |
|---|---|---|---|
| `rating` | `int?` | No | 1–5 |
| `comment` | `string?` | No | Max 1000 chars |

| Status | Description |
|---|---|
| 200 OK | Returns updated `SessionFeedbackDto` |
| 400 Bad Request | Validation error |
| 403 Forbidden | Caller did not submit this feedback |
| 404 Not Found | Feedback not found |

---

### `GET /api/feedback/consultation/{consultationId}`
**Auth:** Required — Patient / Doctor / Admin

| Caller | Access |
|---|---|
| **Admin** | Unrestricted |
| **Doctor** | Only if the consultation belongs to their own appointment |
| **Patient** | Only if they submitted the feedback |

| Status | Description |
|---|---|
| 200 OK | Returns `SessionFeedbackDto` |
| 403 Forbidden | Ownership check failed |
| 404 Not Found | No feedback for this consultation |

---

### `GET /api/feedback/doctor/{doctorId}`
**Auth:** Required — Admin / Doctor (own only)

Returns all feedback for a doctor's consultations, ordered by `CreatedAt` desc.

> A Doctor caller is restricted to their own `DoctorId` (matched via JWT `UserId`).

Returns `IEnumerable<SessionFeedbackDto>`.

| Status | Description |
|---|---|
| 200 OK | Returns feedback list |
| 403 Forbidden | Doctor caller does not own this doctor ID |

---

### `GET /api/feedback/patient/{patientId}`
**Auth:** Required — Admin / Patient (own only)

Returns all feedback submitted by a patient, ordered by `CreatedAt` desc.

> A Patient caller is restricted to their own `PatientId` (matched via JWT `UserId`).

Returns `IEnumerable<SessionFeedbackDto>`.

| Status | Description |
|---|---|
| 200 OK | Returns feedback list |
| 403 Forbidden | Patient caller does not own this patient ID |

---

### `DELETE /api/feedback/{feedbackId}`
**Auth:** Required — Patient (own) / Admin

Hard-deletes the feedback record.

| Status | Description |
|---|---|
| 204 No Content | Deleted successfully |
| 403 Forbidden | Patient caller did not submit this feedback |
| 404 Not Found | Feedback not found |

---

## 15. Admin Report Endpoints — `/api/admin/reports`

> **All endpoints require the `Admin` role.**  
> Reports are backed by pre-built SQL Server views (`vw_AppointmentReport`, `vw_DoctorWorkloadReport`).

---

### `GET /api/admin/reports/appointments`
**Auth:** Required — Admin only

Returns a paginated appointment report sourced from `vw_AppointmentReport`.  
Results are ordered by `AppointmentStart` desc.

**Query Parameters (`ReportFilterDto`)**

| Parameter | Type | Description |
|---|---|---|
| `from` | `DateTime?` | Inclusive UTC lower bound on `AppointmentStart` |
| `to` | `DateTime?` | Inclusive UTC upper bound on `AppointmentStart` |
| `status` | `string?` | Exact match on status name (e.g. `Scheduled`, `Completed`, `Cancelled`) |
| `doctorId` | `int?` | Restrict to a single doctor |
| `pageNumber` | `int` | 1-based, default `1` |
| `pageSize` | `int` | 1–100, default `20` |

Returns `PaginationResponse<AppointmentReportDto>`.

| Status | Description |
|---|---|
| 200 OK | Paginated report returned |
| 400 Bad Request | Filter validation error |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |

---

### `GET /api/admin/reports/doctors`
**Auth:** Required — Admin only · Query: `from` (DateTime? UTC), `to` (DateTime? UTC)

Returns a workload summary for every active doctor from `vw_DoctorWorkloadReport`.  
When `from`/`to` are provided only doctors with at least one appointment in that window are included.  
Results are ordered alphabetically by doctor name.

Returns `IEnumerable<DoctorWorkloadDto>`.

| Status | Description |
|---|---|
| 200 OK | Workload report returned |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |

---

## 16. Medicine Catalogue Endpoints — `/api/medicines`

> All endpoints require any valid JWT (Doctor, Admin, or Patient).

---

### `GET /api/medicines`
**Auth:** Required — any authenticated user · Query: `search` (optional), `pageNumber` (default `1`), `pageSize` (default `20`, max `100`)

Returns a paginated, alphabetically sorted list of medicines from the catalogue.  
`search` is a case-insensitive partial match on `MedicineName` (e.g. `"para"` matches `"Paracetamol 500mg"`).

Returns `PaginationResponse<MedicineDto>`.

| Status | Description |
|---|---|
| 200 OK | Paginated medicine list returned |
| 401 Unauthorized | Token missing |

---

### `GET /api/medicines/{id}`
**Auth:** Required — any authenticated user

Returns a single medicine by `MedicineId`.

| Status | Description |
|---|---|
| 200 OK | Returns `MedicineDto` |
| 401 Unauthorized | Token missing |
| 404 Not Found | Medicine not found |

---

## 17. Appointment Status State Machine

Terminal states (`Completed`, `Cancelled`, `No-Show`) cannot be left once entered.

| Transition | Patient | Doctor | Admin |
|---|:---:|:---:|:---:|
| `Scheduled` ? `Confirmed` | ? | ? | ? |
| `Confirmed` ? `Checked-In` | ? | ? | ? |
| `Checked-In` ? `In Progress` | ? | ? | ? |
| `In Progress` ? `Completed` | ? | ? | ? |
| Any non-terminal ? `Cancelled` | ? | ? | ? |
| Any non-terminal ? `No-Show` | ? | ? | ? |
| Any non-terminal ? `Rescheduled` | ? | ? | ? |

> Transitions not listed above are rejected with `400 Bad Request`.  
> Attempting to leave a terminal state also returns `400 Bad Request`.

---

## 18. Data Schemas

### `AuthResponseDto`

| Field | Type | Description |
|---|---|---|
| `userId` | `int` | User primary key |
| `email` | `string` | Email address |
| `token` | `string` | JWT access token (60 min) |
| `refreshToken` | `string` | Refresh token — store securely |
| `tokenExpiresAt` | `DateTime` | UTC expiry of the access token |
| `role` | `string` | `Patient` \| `Doctor` \| `LabTechnician` \| `Admin` |
| `emailVerified` | `bool` | Whether email is verified |
| `profileCompleted` | `bool` | Whether patient/doctor profile is complete |

---

### `AppointmentDto`

| Field | Type | Description |
|---|---|---|
| `appointmentId` | `int` | Primary key |
| `patientId` | `int` | Linked patient ID |
| `patientName` | `string` | Patient's full name |
| `doctorId` | `int` | Linked doctor ID |
| `doctorName` | `string` | Doctor's full name |
| `appointmentStart` | `DateTime` | UTC start time |
| `appointmentEnd` | `DateTime` | UTC end time |
| `reason` | `string` | Reason for visit |
| `status` | `string` | `Scheduled` \| `Confirmed` \| `Checked-In` \| `In Progress` \| `Completed` \| `Cancelled` \| `No-Show` \| `Rescheduled` |

---

### `RescheduleAppointmentDto`

| Field | Type | Required | Description |
|---|---|---|---|
| `appointmentStart` | `DateTime` | Yes | New UTC start time |
| `appointmentEnd` | `DateTime` | Yes | New UTC end time — must be after `appointmentStart` |

---

### `ConsultationDto`

| Field | Type | Description |
|---|---|---|
| `consultationId` | `int` | Primary key |
| `appointmentId` | `int` | Linked appointment ID (immutable) |
| `patientId` | `int` | Resolved patient ID |
| `chiefComplaint` | `string?` | Chief complaint |
| `examination` | `string?` | Examination notes |
| `diagnosisNotes` | `string?` | Diagnosis |
| `treatmentPlan` | `string?` | Treatment plan |
| `icdCode` | `string?` | ICD-10 code text |
| `createdAt` | `DateTime` | UTC creation timestamp |
| `prescriptions` | `List<PrescriptionDto>` | Linked prescriptions |
| `orderedTests` | `List<OrderedTestDto>` | Linked ordered lab tests |

---

### `SessionFeedbackDto`

| Field | Type | Description |
|---|---|---|
| `feedbackId` | `int` | Primary key |
| `consultationId` | `int` | Linked consultation ID |
| `patientId` | `int` | Submitting patient ID |
| `patientName` | `string` | Patient's full name |
| `doctorId` | `int` | Doctor linked through the consultation's appointment |
| `doctorName` | `string` | Doctor's full name |
| `rating` | `int` | 1–5 numeric rating |
| `ratingLabel` | `string` | `Very Poor` \| `Poor` \| `Average` \| `Good` \| `Excellent` |
| `comment` | `string?` | Optional free-text comment (max 1000 chars) |
| `createdAt` | `DateTime` | UTC submission timestamp |
| `isEdited` | `bool` | `true` if the patient has edited their feedback |
| `updatedAt` | `DateTime?` | UTC timestamp of last edit |

---

### `AppointmentReportDto`

| Field | Type | Description |
|---|---|---|
| `appointmentId` | `int` | Primary key |
| `appointmentStart` | `DateTime` | UTC start |
| `appointmentEnd` | `DateTime` | UTC end |
| `patientName` | `string` | Patient's full name |
| `patientPhone` | `string?` | Patient's phone number |
| `mrn` | `string` | Patient's Medical Record Number |
| `doctorName` | `string` | Attending doctor's full name |
| `departmentName` | `string?` | Doctor's department |
| `statusName` | `string` | Appointment status |
| `reason` | `string?` | Visit reason |
| `hasConsultation` | `bool` | Whether a consultation record exists |

---

### `DoctorWorkloadDto`

| Field | Type | Description |
|---|---|---|
| `doctorId` | `int` | Doctor primary key |
| `doctorName` | `string` | Doctor's full name |
| `qualification` | `string?` | Academic qualification |
| `departmentName` | `string?` | Department |
| `totalAppointments` | `int` | All non-deleted appointments |
| `completedAppointments` | `int` | Appointments with `Completed` status |
| `cancelledAppointments` | `int` | Appointments with `Cancelled` status |
| `totalConsultations` | `int` | Consultation records linked to this doctor |

---

### `MedicineDto`

| Field | Type | Description |
|---|---|---|
| `medicineId` | `int` | Primary key |
| `medicineName` | `string` | Full name including strength (e.g. `Paracetamol 500mg`) |

---

### `LabTestCatalogueDto`

| Field | Type | Description |
|---|---|---|
| `labTestId` | `int` | Primary key |
| `testName` | `string` | Full test name (e.g. `Complete Blood Count (CBC)`) |

---

### `LabResultDto`

| Field | Type | Description |
|---|---|---|
| `orderedTestId` | `int` | OrderedTest primary key |
| `consultationId` | `int` | Linked consultation ID |
| `labTestId` | `int` | Catalog lab test ID |
| `testName` | `string` | Lab test name |
| `status` | `string` | `Pending` \| `Completed` |
| `result` | `string?` | Result text uploaded by Doctor/Admin |
| `resultDate` | `DateTime?` | UTC date/time of upload |
| `patientId` | `int` | Linked patient ID |
| `patientName` | `string` | Patient's full name |

---

### `DoctorScheduleDto`

| Field | Type | Description |
|---|---|---|
| `scheduleId` | `int` | Primary key |
| `doctorId` | `int` | Linked doctor ID |
| `doctorName` | `string` | Doctor's full name |
| `dayOfWeek` | `int` | `0 = Sunday … 6 = Saturday` |
| `dayName` | `string` | e.g. `"Monday"` |
| `startTime` | `TimeSpan` | e.g. `09:00:00` |
| `endTime` | `TimeSpan` | e.g. `17:00:00` |
| `slotDurationMinutes` | `int` | Duration per appointment slot |
| `isActive` | `bool` | Whether this slot is active |
| `generatedSlots` | `List<string>` | e.g. `["09:00 - 09:30", "09:30 - 10:00", …]` |

---

### `MedicalHistoryDto`

| Field | Type | Description |
|---|---|---|
| `patientId` | `int` | Patient primary key |
| `patientName` | `string` | Full name |
| `mrn` | `string` | Medical Record Number |
| `dateOfBirth` | `DateOnly` | Date of birth |
| `gender` | `string?` | Gender |
| `bloodGroup` | `string?` | Blood group |
| `allergies` | `List<string>` | Allergy names |
| `visits` | `List<MedicalVisitDto>` | Chronological visits |

**`MedicalVisitDto`** (nested in `visits`)

| Field | Type | Description |
|---|---|---|
| `appointmentId` | `int` | Primary key |
| `appointmentStart` | `DateTime` | UTC start |
| `appointmentEnd` | `DateTime` | UTC end |
| `reason` | `string?` | Visit reason |
| `status` | `string` | Appointment status |
| `doctorName` | `string` | Attending doctor |
| `consultation` | `MedicalConsultationDto?` | `null` if not yet created |

**`MedicalConsultationDto`** (nested in `consultation`)

| Field | Type | Description |
|---|---|---|
| `consultationId` | `int` | Primary key |
| `chiefComplaint` | `string?` | Chief complaint |
| `examination` | `string?` | Examination notes |
| `diagnosisNotes` | `string?` | Diagnosis |
| `treatmentPlan` | `string?` | Treatment plan |
| `notes` | `string?` | General notes |
| `icdCode` | `string?` | ICD-10 code |
| `createdAt` | `DateTime` | UTC creation timestamp |
| `prescriptions` | `List<PrescriptionDto>` | Prescriptions |
| `labTests` | `List<LabResultDto>` | Lab tests with results |

---

### `PrescriptionDto`

| Field | Type | Description |
|---|---|---|
| `prescriptionId` | `int` | Primary key |
| `medicineName` | `string` | Medicine name from catalog |
| `dosage` | `string?` | Dose e.g. `500 mg` |
| `frequency` | `string?` | e.g. `Twice daily` |
| `route` | `string?` | e.g. `Oral` |
| `durationDays` | `int?` | Number of days |
| `instructions` | `string?` | Patient instructions |

---

### `DoctorDto`

| Field | Type | Description |
|---|---|---|
| `doctorId` | `int` | Primary key |
| `licenseNumber` | `string` | Unique medical license |
| `fullName` | `string` | Full name |
| `qualification` | `string?` | Academic qualifications |
| `experienceYears` | `int?` | Years of experience |
| `isActive` | `bool` | Active flag |
| `address` | `AddressDto?` | Linked address |
| `departments` | `List<DepartmentDto>` | Assigned departments |

---

### `PatientDto`

| Field | Type | Description |
|---|---|---|
| `patientId` | `int` | Primary key |
| `userId` | `int` | Linked user account ID |
| `mrn` | `string` | System-generated MRN |
| `fullName` | `string` | Full name |
| `dateOfBirth` | `DateOnly` | Date of birth |
| `gender` | `string?` | Gender |
| `phoneNumber` | `string?` | Contact phone |
| `bloodGroup` | `string?` | Blood group |
| `emergencyContact` | `string?` | Emergency phone |
| `address` | `AddressDto?` | Linked address |
| `allergies` | `List<PatientAllergyDto>?` | Known allergies |

---

### `AddressDto / CreateAddressDto`

| Field | Type | In CreateDto | Description |
|---|---|---|---|
| `addressId` | `int` | No | Read-only |
| `addressLine1` | `string` | Yes | Street / house number |
| `addressLine2` | `string?` | Yes | Apartment (optional) |
| `city` | `string` | Yes | City |
| `state` | `string` | Yes | State / province |
| `postalCode` | `string?` | Yes | ZIP / postal code |
| `country` | `string` | Yes | Country |

---

### `DepartmentDto`

| Field | Type | Description |
|---|---|---|
| `departmentId` | `int` | Primary key |
| `departmentName` | `string` | e.g. Cardiology |

---

## 19. Quick Reference

| Method | Route | Auth | Role(s) |
|---|---|---|---|
| POST | `/api/auth/register` | No | — |
| POST | `/api/auth/login` | No | — |
| POST | `/api/auth/verify-email` | No | — |
| POST | `/api/auth/forgot-password` | No | — |
| POST | `/api/auth/reset-password` | No | — |
| POST | `/api/auth/refresh-token` | No | — |
| POST | `/api/auth/revoke-token` | No | — |
| GET | `/api/doctors` | No | — |
| GET | `/api/doctors/{id}` | No | — |
| GET | `/api/doctors/department/{departmentId}` | No | — |
| POST | `/api/doctors` | Yes | Admin |
| PUT | `/api/doctors/{id}` | Yes | Admin |
| DELETE | `/api/doctors/{id}` | Yes | Admin |
| GET | `/api/patients` | Yes | Admin |
| GET | `/api/patients/{id}` | Yes | Admin / Doctor / Owner |
| GET | `/api/patients/mrn/{mrn}` | Yes | Admin / Doctor |
| GET | `/api/patients/search` | Yes | Admin / Doctor |
| GET | `/api/patients/me` | Yes | Patient |
| POST | `/api/patients/me` | Yes | Patient |
| POST | `/api/patients` | Yes | Admin |
| PUT | `/api/patients/{id}` | Yes | Admin / Owner |
| DELETE | `/api/patients/{id}` | Yes | Admin |
| GET | `/api/appointments` | Yes | Admin / Doctor |
| GET | `/api/appointments/{id}` | Yes | Any (ownership enforced) |
| GET | `/api/appointments/patient/{patientId}` | Yes | Any (Patient: own only) |
| GET | `/api/appointments/doctor/me` | Yes | Doctor |
| GET | `/api/appointments/doctor/{doctorId}` | Yes | Admin / Doctor |
| GET | `/api/appointments/date-range` | Yes | Admin / Doctor |
| GET | `/api/appointments/me` | Yes | Patient |
| POST | `/api/appointments` | Yes | Any |
| PATCH | `/api/appointments/{id}/reschedule` | Yes | Any (ownership enforced) |
| PUT | `/api/appointments/{id}/status` | Yes | Admin / Doctor |
| PUT | `/api/appointments/{id}` | Yes | Any (ownership enforced) |
| DELETE | `/api/appointments/{id}` | Yes | Any (ownership enforced) |
| GET | `/api/consultations` | Yes | Admin |
| GET | `/api/consultations/doctor/me` | Yes | Doctor |
| GET | `/api/consultations/{id}` | Yes | Doctor / Admin / Patient (own) |
| GET | `/api/consultations/appointment/{appointmentId}` | Yes | Doctor / Admin / Patient |
| GET | `/api/consultations/me` | Yes | Patient |
| POST | `/api/consultations` | Yes | Doctor / Admin |
| PUT | `/api/consultations/{id}` | Yes | Doctor / Admin |
| POST | `/api/consultations/{id}/prescriptions` | Yes | Doctor / Admin |
| POST | `/api/consultations/{id}/lab-tests` | Yes | Doctor / Admin |
| PUT | `/api/lab-tests/{orderedTestId}/result` | Yes | Admin / Doctor |
| GET | `/api/lab-tests/patient/{patientId}` | Yes | Admin / Doctor |
| GET | `/api/lab-tests/consultation/{consultationId}` | Yes | Admin / Doctor |
| GET | `/api/lab-tests/catalogue` | Yes | Admin / Doctor / Patient |
| GET | `/api/lab-tests/catalogue/{id}` | Yes | Admin / Doctor / Patient |
| POST | `/api/doctors/{doctorId}/schedule` | Yes | Admin / Doctor |
| GET | `/api/doctors/{doctorId}/schedule` | No | — |
| PUT | `/api/doctors/schedule/{scheduleId}` | Yes | Admin / Doctor (own) |
| DELETE | `/api/doctors/schedule/{scheduleId}` | Yes | Admin / Doctor (own) |
| GET | `/api/patients/{patientId}/medical-history` | Yes | Admin / Doctor |
| GET | `/api/patients/me/medical-history` | Yes | Patient |
| POST | `/api/feedback` | Yes | Patient |
| PUT | `/api/feedback/{feedbackId}` | Yes | Patient (own) |
| GET | `/api/feedback/consultation/{consultationId}` | Yes | Patient (own) / Doctor (own) / Admin |
| GET | `/api/feedback/doctor/{doctorId}` | Yes | Admin / Doctor (own) |
| GET | `/api/feedback/patient/{patientId}` | Yes | Admin / Patient (own) |
| DELETE | `/api/feedback/{feedbackId}` | Yes | Patient (own) / Admin |
| GET | `/api/admin/reports/appointments` | Yes | Admin |
| GET | `/api/admin/reports/doctors` | Yes | Admin |
| GET | `/api/medicines` | Yes | Any |
| GET | `/api/medicines/{id}` | Yes | Any |

---

*Generated for Axivora HMS · .NET 10 / C# 14 · https://github.com/Karthickraja018/AxivoraHMS*
