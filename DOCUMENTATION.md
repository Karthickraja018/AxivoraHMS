# Axivora HMS — REST API Documentation

**Version:** 1.5

---

## Table of Contents

1. [Overview](#1-overview)
2. [Authentication & Token Lifecycle](#2-authentication--token-lifecycle)
3. [Roles & Access Control](#3-roles--access-control)
4. [Global Error Handling](#4-global-error-handling)
5. [Pagination](#5-pagination)
6. [Slot-Based Booking Architecture](#6-slot-based-booking-architecture)
7. [Auth Endpoints](#7-auth-endpoints----apiauth)
8. [Doctor Endpoints](#8-doctor-endpoints----apidoctors)
9. [Patient Endpoints](#9-patient-endpoints----apipatients)
10. [Department Endpoints](#10-department-endpoints)
11. [ICD Code Endpoints](#11-icd-code-endpoints)
12. [Admin User Management](#12-admin-user-management)
13. [Patient Vital Endpoints](#13-patient-vital-endpoints)
14. [Appointment Endpoints](#14-appointment-endpoints----apiappointments)
15. [Doctor Availability & Slot Endpoints](#15-doctor-availability--slot-endpoints)
16. [Consultation Endpoints](#16-consultation-endpoints----apiconsultations)
17. [Lab Test Endpoints](#17-lab-test-endpoints----apilab-tests)
18. [Medical History Endpoints](#18-medical-history-endpoints)
19. [Feedback Endpoints](#19-feedback-endpoints----apifeedback)
20. [Admin Report Endpoints](#20-admin-report-endpoints----apiadminreports)
21. [Medicine Catalogue Endpoints](#21-medicine-catalogue-endpoints----apimedicines)
22. [Appointment Status State Machine](#22-appointment-status-state-machine)
23. [Data Schemas](#23-data-schemas)
24. [Quick Reference](#24-quick-reference)

---

## 1. Overview

Axivora HMS is a Hospital Management System REST API built with ASP.NET Core (.NET 10).

The appointment booking system was fully redesigned around **pre-generated availability slots**:
doctors define weekly recurring templates ? a nightly background service materialises
`DoctorAvailabilityDay` records ? slots are generated lazily on first access ? patients
book by referencing a `SlotId`.

**Base URL**
```
https://<host>/api
```

**Tech Stack**

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / C# 14 |
| Database | SQL Server — Entity Framework Core 10 |
| Auth | JWT Bearer HS256 |
| Object mapping | AutoMapper 13 |
| Concurrency | Optimistic via `[Timestamp]` `RowVersion` on `AppointmentSlot` and `Appointment` |
| Idempotency | `Idempotency-Key` request header, stored in `IdempotencyRecords` table |
| Background jobs | `AvailabilityGenerationBackgroundService` — daily at midnight UTC |
| API docs (dev) | Swagger / OpenAPI |

---

## 2. Authentication & Token Lifecycle

```
Authorization: Bearer <token>
```

### Token Properties

| Property | Value |
|---|---|
| Issuer | `AxivoraHMS` |
| Audience | `AxivoraHMS-Users` |
| Access Token Expiry | 60 min (`JwtSettings:ExpiryMinutes`) |
| Refresh Token Expiry | 7 days (`JwtSettings:RefreshTokenExpiryDays`) |
| Algorithm | HS256 |
| Refresh Token Storage | `RefreshTokens` table (max 512 chars) |
| Rotation | Old token revoked on every refresh call |

### Token Lifecycle

1. `POST /api/auth/login` (or `/register`) — returns `token` and `refreshToken`.
2. Access token expires ? `POST /api/auth/refresh-token` with `refreshToken`.
3. Server revokes old token and returns a fresh pair.
4. Per-device logout ? `POST /api/auth/revoke-token`.
5. Revoked/expired refresh token ? **401 Unauthorized**.

### RefreshToken Model

| Field | Type | Description |
|---|---|---|
| `id` | `int` | Primary key |
| `userId` | `int` | Owning user |
| `token` | `string` | 64-byte cryptographically random Base64 |
| `expiresAt` | `DateTime` | UTC expiry |
| `createdAt` | `DateTime` | UTC creation |
| `isRevoked` | `bool` | `true` after rotation or explicit revoke |
| `revokedAt` | `DateTime?` | UTC revocation time |

---

## 3. Roles & Access Control

| Role | Description | How Created |
|---|---|---|
| `Patient` | Self-registered end users | `POST /api/auth/register` |
| `Doctor` | Medical staff | `POST /api/doctors` (Admin only) |
| `LabTechnician` | Lab staff | Admin / manual DB insert |
| `Admin` | Full system access | DB seed / manual insert |

### Resource-Based Ownership (`OwnershipAuthorizationHandler`)

For `AppointmentDto` resources the `ResourceOwner` policy applies:

| Caller role | Access granted when |
|---|---|
| `Admin` | Always |
| `Doctor` | `appointment.DoctorId` matches caller's `DoctorId` (resolved from JWT `UserId`) |
| `Patient` | `appointment.PatientId` matches caller's `PatientId` (resolved from JWT `UserId`) |

---

## 4. Global Error Handling

`GlobalExceptionHandlerMiddleware` catches all unhandled exceptions:

```json
{ "statusCode": 404, "message": "...", "details": "..." }
```

| Exception | HTTP Status | Notes |
|---|---|---|
| `KeyNotFoundException` | 404 Not Found | |
| `ArgumentException` | 400 Bad Request | |
| `InvalidOperationException` | 400 Bad Request | Also used for 409 cases surfaced directly by controllers |
| `UnauthorizedAccessException` | **403 Forbidden** | Ownership / permissions violation (authenticated but not authorised for the resource) |
| Any other | 500 Internal Server Error | |

> **401 Unauthorized** is produced exclusively by the JWT middleware for missing or invalid tokens.

---

## 5. Pagination

```
GET /api/appointments?pageNumber=2&pageSize=20
```

**Response envelope:**
```json
{
  "items": [...],
  "totalCount": 50,
  "pageNumber": 2,
  "pageSize": 20,
  "totalPages": 3,
  "hasPrevious": true,
  "hasNext": true
}
```

---

## 6. Slot-Based Booking Architecture

The appointment booking flow replaced direct `AppointmentStart`/`AppointmentEnd` booking with a **pre-generated slot model**:

```
DoctorAvailabilityTemplate
        ? (nightly background service)
        ?
DoctorAvailabilityDay          ? one row per doctor-date
        ? (lazy, on first slot request)
        ?
AppointmentSlot                ? one row per bookable window
        ? (patient books by SlotId)
        ?
Appointment  (SlotId FK)
```

### Generation Pipeline

| Step | Actor | Detail |
|---|---|---|
| 1. Template created | Admin / Doctor | Defines a recurring weekly window with `DayOfWeek`, `StartTime`, `EndTime`, `SlotDurationMinutes`, `EffectiveFromDate` |
| 2. Day records generated | `AvailabilityGenerationBackgroundService` | Runs daily at midnight UTC; looks 30 days ahead; idempotent — skips existing days |
| 3. Slot records generated | `SlotService.EnsureSlotsGeneratedAsync` | Called lazily the **first time** a date's slots are requested; never regenerates existing slots |
| 4. Patient books | `POST /api/appointments` | Supplies `slotId` + optional `reason`; slot transitions `Available ? Booked` atomically inside a DB transaction |

### Concurrency & Idempotency

| Concern | Mechanism |
|---|---|
| **Race condition** | Slot availability is re-validated **inside** the DB transaction — no window between check and update |
| **Optimistic concurrency** | `[Timestamp] RowVersion` on `AppointmentSlot` and `Appointment`; concurrent writers ? `DbUpdateConcurrencyException` ? 409 Conflict |
| **Duplicate booking (retry)** | Supply `Idempotency-Key: <uuid>` header on `POST /api/appointments`; stored response is returned unchanged on retry |

### Slot Status Values

| Status | Meaning |
|---|---|
| `Available` | Ready to book |
| `Booked` | Linked to an appointment |
| `Blocked` | Manually blocked or covered by Leave/Holiday |
| `Cancelled` | Previously booked, now released via admin |

### Availability Day Status Values

| Status | Slot effect |
|---|---|
| `Open` | Slots remain as generated |
| `Closed` | All `Available` slots ? `Blocked` |
| `Leave` | All `Available` slots ? `Blocked` |
| `Holiday` | All `Available` slots ? `Blocked` |
| `NoSchedule` | No `DoctorAvailabilityDay` record exists for this date |

---

## 7. Auth Endpoints — `/api/auth`

### `POST /api/auth/register`
**Auth:** Public

| Field | Type | Required | Description |
|---|---|---|---|
| `email` | `string` | Yes | Valid email (max 150 chars) |
| `password` | `string` | Yes | ?8 chars — uppercase + lowercase + digit + special char |
| `confirmPassword` | `string` | Yes | Must match `password` |
| `role` | `string` | No | Always `"Patient"` — any other value is rejected |

| Status | Description |
|---|---|
| 201 Created | Returns `AuthResponseDto` |
| 400 Bad Request | Validation error, email already registered, or non-Patient role |

---

### `POST /api/auth/login`
**Auth:** Public

| Field | Type | Required |
|---|---|---|
| `email` | `string` | Yes |
| `password` | `string` | Yes |

| Status | Description |
|---|---|
| 200 OK | Returns `AuthResponseDto` |
| 401 Unauthorized | Invalid credentials or account disabled |

---

### `POST /api/auth/verify-email`
**Auth:** Public · Query: `email`, `code`

| Status | Description |
|---|---|
| 200 OK | Email verified |
| 400 Bad Request | Invalid code |
| 404 Not Found | User not found |

---

### `POST /api/auth/forgot-password`
**Auth:** Public · Body: `{ "email": "..." }`

| Status | Description |
|---|---|
| 200 OK | Reset link sent |
| 404 Not Found | Email not registered |

---

### `POST /api/auth/reset-password`
**Auth:** Public

| Field | Type | Required |
|---|---|---|
| `email` | `string` | Yes |
| `resetToken` | `string` | Yes |
| `newPassword` | `string` | Yes |
| `confirmPassword` | `string` | Yes |

| Status | Description |
|---|---|
| 200 OK | Password reset |
| 400 Bad Request | Invalid token or validation error |
| 404 Not Found | User not found |

---

### `POST /api/auth/refresh-token`
**Auth:** Public · Body: `{ "refreshToken": "..." }`

Implements token rotation — supplied token is immediately revoked.

| Status | Description |
|---|---|
| 200 OK | Returns full `AuthResponseDto` |
| 400 Bad Request | Field missing |
| 401 Unauthorized | Token not found, revoked, or expired |

---

### `POST /api/auth/revoke-token`
**Auth:** Public · Body: `{ "refreshToken": "..." }`

| Status | Description |
|---|---|
| 200 OK | `{ "message": "Token revoked successfully." }` |
| 400 Bad Request | Token not found or already revoked |

---

## 8. Doctor Endpoints — `/api/doctors`

### `GET /api/doctors`
**Auth:** Public · Query: `pageNumber`, `pageSize` ? `PaginationResponse<DoctorDto>`

### `GET /api/doctors/{id}`
**Auth:** Public ? `DoctorDto` · 404 if not found

### `GET /api/doctors/department/{departmentId}`
**Auth:** Public ? `IEnumerable<DoctorDto>`

### `POST /api/doctors`
**Auth:** Required — Admin only

| Field | Type | Required | Description |
|---|---|---|---|
| `email` | `string` | Yes | Unique email |
| `password` | `string` | Yes | |
| `licenseNumber` | `string` | Yes | Unique (max 100) |
| `fullName` | `string` | Yes | Max 150 |
| `qualification` | `string` | No | |
| `experienceYears` | `int` | No | 0–100 |
| `address` | `CreateAddressDto` | No | |
| `departmentIds` | `int[]` | Yes | ?1 valid ID |

201 Created · 400 · 401 · 403

### `PUT /api/doctors/{id}`
**Auth:** Required — Admin only ? `DoctorDto` · 404 · 401 · 403

### `DELETE /api/doctors/{id}`
**Auth:** Required — Admin only · Soft-delete · 204 · 404 · 401 · 403

---

## 9. Patient Endpoints — `/api/patients`

### `GET /api/patients`
**Auth:** Admin only · Query: `pageNumber`, `pageSize` ? `PaginationResponse<PatientDto>`

### `GET /api/patients/{id}`
**Auth:** Admin / Doctor / record owner (ownership via `UserId` claim) ? `PatientDto`

### `GET /api/patients/mrn/{mrn}`
**Auth:** Admin / Doctor ? `PatientDto`

### `GET /api/patients/search`
**Auth:** Admin / Doctor · Query: `searchTerm` ? `IEnumerable<PatientDto>`

### `GET /api/patients/me`
**Auth:** Patient only ? own `PatientDto`

### `POST /api/patients/me`
**Auth:** Patient only — creates or restores soft-deleted profile

| Field | Type | Required |
|---|---|---|
| `fullName` | `string` | Yes |
| `dateOfBirth` | `DateOnly` | Yes |
| `gender` | `string` | Yes |
| `phoneNumber` | `string` | Yes |
| `bloodGroup` | `string` | No |
| `emergencyContact` | `string` | No |
| `address` | `CreateAddressDto` | Yes |

201 Created

### `POST /api/patients`
**Auth:** Admin only — creates patient with user account · 201

### `PUT /api/patients/{id}`
**Auth:** Admin or record owner · 200 / 403 / 404

### `DELETE /api/patients/{id}`
**Auth:** Admin only · soft-delete · 204

---

## 10. Department Endpoints

Base route: `/api/departments`

> **Read access:** Any authenticated user.
> **Write access:** Admin only.
> **Soft-delete:** `DELETE` sets `IsActive = false` — the department record is preserved.
> **Duplicate name guard:** `POST` and `PUT` reject a `departmentName` that already exists. `PUT` excludes the current record from the check.

---

### `GET /api/departments`
**Auth:** Required — any authenticated user

Returns a paginated list of departments. Query: `pageNumber`, `pageSize` ? `PaginationResponse<DepartmentDto>`

| Status | Description |
|---|---|
| 200 OK | Paginated department list |
| 401 Unauthorized | Token missing |

---

### `GET /api/departments/{id}`
**Auth:** Required — any authenticated user

| Status | Description |
|---|---|
| 200 OK | Returns `DepartmentDto` |
| 401 Unauthorized | Token missing |
| 404 Not Found | Department not found |

---

### `POST /api/departments`
**Auth:** Required — Admin only

| Field | Type | Required | Description |
|---|---|---|---|
| `departmentName` | `string` | Yes | Max 100 chars; must be unique |
| `description` | `string?` | No | Max 500 chars |
| `isActive` | `bool` | No | Defaults to `true` |

| Status | Description |
|---|---|
| 201 Created | Returns `DepartmentDto`; `Location` points to `GET /api/departments/{id}` |
| 400 Bad Request | Validation error or duplicate department name |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |

---

### `PUT /api/departments/{id}`
**Auth:** Required — Admin only

| Field | Type | Required | Description |
|---|---|---|---|
| `departmentName` | `string` | Yes | Max 100 chars; must be unique (excludes self) |
| `description` | `string?` | No | Max 500 chars |
| `isActive` | `bool` | Yes | Set to `false` to deactivate |

| Status | Description |
|---|---|
| 200 OK | Returns updated `DepartmentDto` |
| 400 Bad Request | Validation error or duplicate department name |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |
| 404 Not Found | Department not found |

---

### `DELETE /api/departments/{id}`
**Auth:** Required — Admin only

Soft-deactivates the department (`IsActive = false`). The record is preserved.

| Status | Description |
|---|---|
| 204 No Content | Deactivated |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |
| 404 Not Found | Department not found |

---

## 11. ICD Code Endpoints

Base route: `/api/icd-codes`

> **Auth:** Admin, Doctor, or Patient.
> **Read-only:** No write endpoints are exposed.
> **`?search=` shorthand:** When supplied, the same value is applied to both `code` and `description` filters simultaneously (OR logic). Use `?code=` and `?description=` independently for field-specific filtering.

---

### `GET /api/icd-codes`
**Auth:** Required — Admin / Doctor / Patient

Returns a paginated list of ICD-10 codes with optional filtering.

| Query Parameter | Type | Description |
|---|---|---|
| `search` | `string?` | OR match across both `Code` and `Description` fields |
| `code` | `string?` | Partial match on ICD Code field only |
| `description` | `string?` | Partial match on Description field only |
| `pageNumber` | `int` | Default 1 |
| `pageSize` | `int` | Default value from `PaginationParams` |

Returns `PaginationResponse<ICDCodeDto>`.

| Status | Description |
|---|---|
| 200 OK | Paginated ICD code list |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Role not Admin / Doctor / Patient |

---

## 12. Admin User Management

Base route: `/api/admin/users`

> **Auth:** Admin only for all endpoints.
> **No hard-delete:** Accounts are toggled via `PATCH` endpoints — no `DELETE` is exposed.
> **`UpdatedAt` tracking:** Both `disable` and `enable` set `UpdatedAt = DateTime.UtcNow` on the user record.

---

### `GET /api/admin/users`
**Auth:** Required — Admin only

Returns a paginated, filterable list of all user accounts.

| Query Parameter | Type | Description |
|---|---|---|
| `email` | `string?` | Partial match on email |
| `role` | `string?` | Exact role name: `Admin`, `Doctor`, `Patient`, `LabTechnician` |
| `isActive` | `bool?` | Filter by active / inactive status |
| `pageNumber` | `int` | Default 1 |
| `pageSize` | `int` | Default value from `PaginationParams` |

Returns `PaginationResponse<AdminUserDto>`.

| Status | Description |
|---|---|
| 200 OK | Paginated user list |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |

---

### `GET /api/admin/users/{id}`
**Auth:** Required — Admin only

| Status | Description |
|---|---|
| 200 OK | Returns `AdminUserDto` |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |
| 404 Not Found | User not found |

---

### `PATCH /api/admin/users/{id}/disable`
**Auth:** Required — Admin only

Sets `IsActive = false` and `UpdatedAt = UtcNow` on the user account. A disabled account receives 401 on login.

| Status | Description |
|---|---|
| 200 OK | Returns updated `AdminUserDto` with `isActive = false` |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |
| 404 Not Found | User not found |

---

### `PATCH /api/admin/users/{id}/enable`
**Auth:** Required — Admin only

Sets `IsActive = true` and `UpdatedAt = UtcNow` on the user account.

| Status | Description |
|---|---|
| 200 OK | Returns updated `AdminUserDto` with `isActive = true` |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Admin |
| 404 Not Found | User not found |

---

## 13. Patient Vital Endpoints

Base route: `/api/patients/{patientId}/vitals`

> **Nested route:** All endpoints are scoped to a patient via `{patientId}`.
> **Read access:** Doctor and Admin can access any patient's vitals. A Patient caller can only view their own — enforced by `AuthorizePatientAccessAsync` (compares JWT `UserId` with `patient.UserId`).
> **Write access:** Doctor and Admin only.
> **`RecordedAt`:** Auto-set to `DateTime.UtcNow` on create — not supplied by the client.
> **Hard delete:** `DELETE` permanently removes the vital record.
> **Patient-exists guard:** Every operation validates `patientId` exists (404 if not).

---

### `GET /api/patients/{patientId}/vitals`
**Auth:** Required — Doctor / Admin / own Patient

Returns paginated vital records for the specified patient. Query: `pageNumber`, `pageSize` ? `PaginationResponse<PatientVitalDto>`

| Status | Description |
|---|---|
| 200 OK | Paginated vital list |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Patient caller accessing another patient's vitals |
| 404 Not Found | Patient not found |

---

### `GET /api/patients/{patientId}/vitals/{vitalId}`
**Auth:** Required — Doctor / Admin / own Patient

| Status | Description |
|---|---|
| 200 OK | Returns `PatientVitalDto` |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Patient caller accessing another patient's vitals |
| 404 Not Found | Patient not found, vital not found, or vital does not belong to patient |

---

### `POST /api/patients/{patientId}/vitals`
**Auth:** Required — Doctor / Admin only

Records a new vital entry. `RecordedAt` is auto-set to `UtcNow` — do not supply it in the request body. All measurement fields are optional.

| Field | Type | Constraints | Description |
|---|---|---|---|
| `height` | `decimal?` | 0.01 – 300 | Height in cm |
| `weight` | `decimal?` | 0.01 – 700 | Weight in kg |
| `bloodPressure` | `string?` | Max 20 chars | e.g. `"120/80"` |
| `heartRate` | `int?` | 1 – 300 | Beats per minute |
| `temperature` | `decimal?` | 30 – 45 | Body temperature in degrees C |

| Status | Description |
|---|---|
| 201 Created | Returns `PatientVitalDto`; `Location` points to `GET /api/patients/{patientId}/vitals/{vitalId}` |
| 400 Bad Request | Field value out of range |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Doctor or Admin |
| 404 Not Found | Patient not found |

---

### `PUT /api/patients/{patientId}/vitals/{vitalId}`
**Auth:** Required — Doctor / Admin only

Updates an existing vital record. All fields are optional.

| Field | Type | Constraints |
|---|---|---|
| `height` | `decimal?` | 0.01 – 300 cm |
| `weight` | `decimal?` | 0.01 – 700 kg |
| `bloodPressure` | `string?` | Max 20 chars |
| `heartRate` | `int?` | 1 – 300 bpm |
| `temperature` | `decimal?` | 30 – 45 degrees C |

| Status | Description |
|---|---|
| 200 OK | Returns updated `PatientVitalDto` |
| 400 Bad Request | Field value out of range |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Doctor or Admin |
| 404 Not Found | Patient not found, or vital does not belong to patient |

---

### `DELETE /api/patients/{patientId}/vitals/{vitalId}`
**Auth:** Required — Doctor / Admin only

Permanently deletes the vital record (hard delete).

| Status | Description |
|---|---|
| 204 No Content | Deleted |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not Doctor or Admin |
| 404 Not Found | Patient not found, or vital does not belong to patient |

---

## 14. Appointment Endpoints

> The appointment controller was **completely rewritten**. `POST` now requires a `slotId` instead of raw `AppointmentStart`/`AppointmentEnd`. `PUT` no longer exists — use `PATCH /{id}/status` for status changes. `PATCH /{id}/reschedule` now takes a `newSlotId` instead of new timestamps.

---

### `POST /api/appointments`
**Auth:** Required — Patient only

Books a pre-generated slot. Creates an `Appointment` and atomically transitions the `AppointmentSlot` from `Available` to `Booked` inside a single DB transaction.

**Idempotency:** Supply `Idempotency-Key: <uuid>` header to make the request safe to retry.
- First call: executes booking, stores the response against the key.
- Subsequent calls with the **same key**: returns the stored response — no duplicate appointment created.
- Response is **200 OK** (not 201) on a replayed idempotent response.

| Field | Type | Required | Description |
|---|---|---|---|
| `slotId` | `int` | Yes | ID of an `Available` slot |
| `reason` | `string` | No | Max 500 chars |

**Example**
```json
{ "slotId": 42, "reason": "Annual check-up" }
```

| Status | Description |
|---|---|
| 201 Created | Appointment created; `Location` ? `GET /api/appointments/{id}` |
| 200 OK | Idempotent replay — stored response returned |
| 400 Bad Request | Validation error |
| 404 Not Found | `slotId` not found or patient profile missing |
| 409 Conflict | Slot is no longer `Available` (already booked or concurrent race condition) |
| 401 Unauthorized | Token missing |
| 403 Forbidden | Not a Patient |

---

### `GET /api/appointments`
**Auth:** Required — Admin / Doctor · Query: `pageNumber`, `pageSize`

- **Admin** — all appointments, ordered by `AppointmentStart` desc.
- **Doctor** — scoped to their own appointments (resolved from JWT `UserId`), ordered `AppointmentStart` asc.

Returns `PaginationResponse<AppointmentDto>`.

---

### `GET /api/appointments/{id}`
**Auth:** Required — any (ownership enforced via `OwnershipAuthorizationHandler`)

| Status | Description |
|---|---|
| 200 OK | Returns `AppointmentDto` |
| 403 Forbidden | Caller does not own the appointment |
| 404 Not Found | Not found |

---

### `GET /api/appointments/me`
**Auth:** Required — Patient only · Query: `pageNumber`, `pageSize`, `status` (optional)

Ordered by `AppointmentStart` desc. Pass `"all"` or omit `status` to return all statuses.

Returns `PaginationResponse<AppointmentDto>`.

---

### `GET /api/appointments/doctor/me`
**Auth:** Required — Doctor only · Query: `pageNumber`, `pageSize`, `date` (optional `DateTime` UTC)

Ordered by `AppointmentStart` asc. Pass `date` to filter to a single calendar day.

Returns `PaginationResponse<AppointmentDto>`.

---

### `PATCH /api/appointments/{id}/reschedule`
**Auth:** Required — any authenticated user (ownership enforced)

Moves an appointment to a **different available slot**. Both the old slot release and the new slot booking are wrapped in a single DB transaction.

> **Business rules:**
> - Appointment must not be `Completed` or `Cancelled`.
> - `newSlotId` must be `Available`; otherwise 409.
> - Patient callers restricted to own appointment.
> - Old slot ? `Available`; new slot ? `Booked`; `appointment.SlotId`, `AppointmentStart`, and `AppointmentEnd` all updated atomically.
> - `DbUpdateConcurrencyException` on the new slot ? 409.

| Field | Type | Required | Description |
|---|---|---|---|
| `newSlotId` | `int` | Yes | ID of a different `Available` slot |

**Example**
```json
{ "newSlotId": 87 }
```

| Status | Description |
|---|---|
| 200 OK | Returns updated `AppointmentDto` |
| 400 Bad Request | Validation error |
| 403 Forbidden | Caller does not own this appointment |
| 404 Not Found | Appointment or `newSlotId` not found |
| 409 Conflict | New slot is not `Available` (concurrent race condition) |

---

### `PATCH /api/appointments/{id}/status`
**Auth:** Required — Admin / Doctor only

[state machine](#22-appointment-status-state-machine)

**Request Body:** `{ "status": "Confirmed" }`

| Status | Description |
|---|---|
| 200 OK | Returns updated `AppointmentDto` |
| 400 Bad Request | Transition not permitted for role or current state |
| 404 Not Found | Appointment or status not found |

---

### `DELETE /api/appointments/{id}`
**Auth:** Required — any authenticated user (ownership enforced)

Soft-deletes the appointment and **atomically releases the linked slot** back to `Available`. Also sets appointment status to `Cancelled`.

All three operations (slot release, soft-delete, status update) are wrapped in a single DB transaction.

| Status | Description |
|---|---|
| 204 No Content | Cancelled and slot released |
| 403 Forbidden | Caller does not own this appointment |
| 404 Not Found | Not found |

---

## 15. Doctor Availability & Slot Endpoints

Routes are all under

---

### Availability Templates

Templates define the **recurring weekly schedule** from which `DoctorAvailabilityDay` records are generated.

#### `POST /api/doctors/{doctorId}/availability-template`
**Auth:** Required — Admin / Doctor

| Field | Type | Required | Description |
|---|---|---|---|
| `dayOfWeek` | `int` | Yes | `0 = Sunday … 6 = Saturday` |
| `startTime` | `TimeSpan` | Yes | e.g. `"09:00:00"` |
| `endTime` | `TimeSpan` | Yes | Must be after `startTime` |
| `slotDurationMinutes` | `int` | No | 5–120, default `15` |
| `effectiveFromDate` | `DateOnly` | Yes | Template active from this date |
| `effectiveToDate` | `DateOnly?` | No | Template expires after this date; must be after `effectiveFromDate` |

**Validation (via `IValidatableObject`):**
- `endTime > startTime`
- `effectiveToDate > effectiveFromDate` (when provided)

**Example**
```json
{
  "dayOfWeek": 1,
  "startTime": "09:00:00",
  "endTime": "17:00:00",
  "slotDurationMinutes": 30,
  "effectiveFromDate": "2025-07-01"
}
```

| Status | Description |
|---|---|
| 201 Created | Returns `AvailabilityTemplateDto` |
| 400 Bad Request | Validation error or `endTime ? startTime` |
| 404 Not Found | Doctor not found |
| 403 Forbidden | Not Admin or Doctor |

---

#### `GET /api/doctors/{doctorId}/availability-template`
**Auth:** Public

Returns all templates for a doctor ordered by `DayOfWeek` then `StartTime`.

Returns `IEnumerable<AvailabilityTemplateDto>`.

| Status | Description |
|---|---|
| 200 OK | Template list |
| 404 Not Found | Doctor not found |

---

#### `PATCH /api/doctors/availability-template/{templateId}`
**Auth:** Required — Admin / Doctor

Updates `isActive` and/or `effectiveToDate` only. Time fields are immutable after creation.

| Field | Type | Description |
|---|---|---|
| `isActive` | `bool?` | Deactivate (false) to stop generating new days |
| `effectiveToDate` | `DateOnly?` | Set an expiry date; must be after `effectiveFromDate` |

| Status | Description |
|---|---|
| 200 OK | Returns updated `AvailabilityTemplateDto` |
| 400 Bad Request | `effectiveToDate ? effectiveFromDate` |
| 404 Not Found | Template not found |

---

#### `DELETE /api/doctors/availability-template/{templateId}`
**Auth:** Required — Admin / Doctor

Soft-delete: sets `isActive = false`. Existing `DoctorAvailabilityDay` records are preserved for historical integrity.

| Status | Description |
|---|---|
| 204 No Content | Deactivated |
| 404 Not Found | Template not found |

---

### Availability Days

`DoctorAvailabilityDay` records are materialised by the background service from active templates.

#### `GET /api/doctors/{doctorId}/availability-days`
**Auth:** Public

Returns all availability day records for a doctor.

Returns `IEnumerable<AvailabilityDayDto>`.

---

#### `PATCH /api/doctors/availability-day/{dayId}`
**Auth:** Required — Admin / Doctor

Updates the status of a specific day. **Side effects on slots:**

| New status | Slot effect |
|---|---|
| `Leave` / `Holiday` / `Closed` | All `Available` slots ? `Blocked` |
| `Open` (from non-Open) | All `Blocked` slots ? `Available` |

| Field | Type | Required | Description |
|---|---|---|---|
| `status` | `string` | Yes | `Open` \| `Closed` \| `Leave` \| `Holiday` |

| Status | Description |
|---|---|
| 200 OK | Returns updated `AvailabilityDayDto` |
| 400 Bad Request | Invalid status value |
| 404 Not Found | Day not found |

---

### Calendar

#### `GET /api/doctors/{doctorId}/calendar`
**Auth:** Public · Query: `from` (`DateOnly`), `to` (`DateOnly`)

Returns an aggregated daily summary over a date range. Days with no schedule show `dayStatus = "NoSchedule"`.

Returns `IEnumerable<DoctorCalendarDayDto>`.

| Status | Description |
|---|---|
| 200 OK | Calendar list |
| 400 Bad Request | `to < from` |

---

### Patient Availability Preview

#### `GET /api/doctors/{doctorId}/availability`
**Auth:** Public · Query: `from` (`DateOnly`), `to` (`DateOnly`)

Returns per-day available slot counts — designed for patient booking UIs (date picker / availability grid).

Returns `IEnumerable<PatientAvailabilityPreviewDto>`.

| Status | Description |
|---|---|
| 200 OK | Preview list (0 available slots shown for closed/leave days) |
| 400 Bad Request | `to < from` |

---

### Doctor Leave

#### `POST /api/doctors/{doctorId}/leave`
**Auth:** Required — Admin / Doctor

Marks all `DoctorAvailabilityDay` records in the `[from, to]` range as `Leave` and blocks all `Available` slots in those days.

| Field | Type | Required | Description |
|---|---|---|---|
| `from` | `DateOnly` | Yes | Inclusive start of leave |
| `to` | `DateOnly` | Yes | Inclusive end (must be ? `from`) |
| `reason` | `string?` | No | Max 500 chars (logged only) |

| Status | Description |
|---|---|
| 204 No Content | Leave applied |
| 400 Bad Request | `to < from` or validation error |
| 404 Not Found | No availability days found for this doctor in the given range |

---

### Slots

#### `GET /api/doctors/{doctorId}/slots`
**Auth:** Public · Query: `date` (`DateOnly`, required)

Returns all `Available` slots for a doctor on the given date. If the `DoctorAvailabilityDay` exists but slots have not been generated yet, generation is triggered **on demand** (lazy generation — idempotent).

Returns `IEnumerable<SlotDto>`.

| Status | Description |
|---|---|
| 200 OK | Available slot list (empty array if no open slots) |
| 400 Bad Request | `date` missing |

---

#### `GET /api/slots/{slotId}`
**Auth:** Public

Returns full detail for a single slot.

Returns `SlotDetailDto`.

| Status | Description |
|---|---|
| 200 OK | Returns `SlotDetailDto` |
| 404 Not Found | Slot not found |

---

#### `PATCH /api/slots/{slotId}`
**Auth:** Required — Admin only

Manually override a slot's status.

| Field | Type | Required | Allowed values |
|---|---|---|---|
| `status` | `string` | Yes | `Available` \| `Booked` \| `Blocked` \| `Cancelled` |

| Status | Description |
|---|---|
| 200 OK | Returns updated `SlotDetailDto` |
| 400 Bad Request | Invalid status value |
| 404 Not Found | Slot not found |
| 403 Forbidden | Not Admin |

---

## 16. Consultation Endpoints

> **Controller-level auth:** Doctor, Admin, or Patient.  
> Write endpoints (POST, PUT) are Doctor / Admin only.  
> **Status rule on create:** Linked appointment must have status `Checked-In`, `In Progress`, or `Completed`.  
> **Doctor ownership on create:** A Doctor caller can only create a consultation for their own appointment.  
> **Duplicate guard:** Only one consultation per appointment.  
> **`appointmentId` is immutable** after creation.  
> **Prescription duplicate guard:** Same `medicineId` cannot appear twice in one consultation.

---

### `GET /api/consultations`
**Auth:** Admin only · Query: `pageNumber`, `pageSize` ? `PaginationResponse<ConsultationDto>` ordered by `CreatedAt` desc

### `GET /api/consultations/doctor/me`
**Auth:** Doctor only · Query: `pageNumber`, `pageSize` ? `PaginationResponse<ConsultationDto>`

### `GET /api/consultations/{id}`
**Auth:** Doctor / Admin / Patient (Patient: own only)

### `GET /api/consultations/appointment/{appointmentId}`
**Auth:** Doctor / Admin / Patient

### `GET /api/consultations/me`
**Auth:** Patient only · Query: `pageNumber`, `pageSize` ? `PaginationResponse<ConsultationDto>`

### `POST /api/consultations`
**Auth:** Doctor / Admin

| Field | Type | Required | Description |
|---|---|---|---|
| `appointmentId` | `int` | Yes | Must be `Checked-In`, `In Progress`, or `Completed` |
| `chiefComplaint` | `string` | No | Max 1000 |
| `examination` | `string` | No | Max 1000 |
| `diagnosisNotes` | `string` | No | Max 500 |
| `treatmentPlan` | `string` | No | Max 1000 |
| `icdId` | `int` | No | ICD-10 code reference |

201 Created / 400 / 403

### `PUT /api/consultations/{id}`
**Auth:** Doctor / Admin — `appointmentId` is read-only · 200 / 404

### `POST /api/consultations/{id}/prescriptions`
**Auth:** Doctor / Admin — duplicate `medicineId` rejected (400) · 200 / 404

| Field | Type | Required |
|---|---|---|
| `medicineId` | `int` | Yes |
| `dosage` | `string` | No |
| `frequency` | `string` | No |
| `route` | `string` | No |
| `durationDays` | `int` | No |
| `instructions` | `string` | No |

### `POST /api/consultations/{id}/lab-tests`
**Auth:** Doctor / Admin — ordered test created with `Status = "Pending"` · 200 / 404

| Field | Type | Required |
|---|---|---|
| `labTestId` | `int` | Yes |

---

## 17. Lab Test Endpoints

### `PUT /api/lab-tests/{orderedTestId}/result`
**Auth:** Admin / Doctor · Sets `ResultDate = UtcNow`

| Field | Type | Required |
|---|---|---|
| `result` | `string` | Yes (max 2000) |

200 / 400 / 403 / 404

### `GET /api/lab-tests/patient/{patientId}`
**Auth:** Admin / Doctor ? `IEnumerable<LabResultDto>`

### `GET /api/lab-tests/consultation/{consultationId}`
**Auth:** Admin / Doctor ? `IEnumerable<LabResultDto>`

### `GET /api/lab-tests/catalogue`
**Auth:** Any authenticated user · Query: `search`, `pageNumber` (default 1), `pageSize` (default 20, max 100)

Case-insensitive partial match on `TestName`, sorted alphabetically ? `PaginationResponse<LabTestCatalogueDto>`

### `GET /api/lab-tests/catalogue/{id}`
**Auth:** Any authenticated user ? `LabTestCatalogueDto` · 404 if not found

---

## 18. Medical History Endpoints

### `GET /api/patients
**Auth:** Admin / Doctor ? `MedicalHistoryDto` · 404 · 403

### `GET /api/patients/me/medical-history`
**Auth:** Patient only ? own `MedicalHistoryDto` · 404

---

## 19. Feedback Endpoints

> One feedback per consultation (UNIQUE constraint).  
> Only allowed for appointments with status `Completed`.  
> Only the submitting Patient may edit/delete (Admin may also delete).

### `POST /api/feedback`
**Auth:** Patient only

| Field | Type | Required | Description |
|---|---|---|---|
| `consultationId` | `int` | Yes | Must be linked to a `Completed` appointment |
| `rating` | `int` | Yes | 1 (Very Poor) – 5 (Excellent) |
| `comment` | `string` | No | Max 1000 chars |

201 Created / 400 / 403 / 404 / 409 (duplicate or non-completed)

### `PUT /api/feedback/{feedbackId}`
**Auth:** Patient (own) — sets `isEdited = true`, `updatedAt = UtcNow`

| Field | Type |
|---|---|
| `rating` | `int?` (1–5) |
| `comment` | `string?` |

200 / 400 / 403 / 404

### `GET /api/feedback/consultation/{consultationId}`
**Auth:** Patient (own) / Doctor (own consultation) / Admin ? `SessionFeedbackDto`

### `GET /api/feedback/doctor/{doctorId}`
**Auth:** Admin / Doctor (own) ? `IEnumerable<SessionFeedbackDto>` ordered `CreatedAt` desc

### `GET /api/feedback/patient/{patientId}`
**Auth:** Admin / Patient (own) ? `IEnumerable<SessionFeedbackDto>` ordered `CreatedAt` desc

### `DELETE /api/feedback/{feedbackId}`
**Auth:** Patient (own) / Admin · hard-delete · 204 / 403 / 404

---

## 20. Admin Report Endpoints

> All endpoints require the `Admin` role. Backed by SQL Server views.

### `GET /api/admin/reports/appointments`
**Auth:** Admin only · Sourced from `vw_AppointmentReport`

**Query (`ReportFilterDto`)**

| Parameter | Type | Description |
|---|---|---|
| `from` | `DateTime?` | Inclusive UTC lower bound on `AppointmentStart` |
| `to` | `DateTime?` | Inclusive UTC upper bound |
| `status` | `string?` | Exact match on status name |
| `doctorId` | `int?` | Restrict to one doctor |
| `pageNumber` | `int` | Default 1 |
| `pageSize` | `int` | 1–100, default 20 |

Returns `PaginationResponse<AppointmentReportDto>`.

### `GET /api/admin/reports/doctors`
**Auth:** Admin only · Sourced from `vw_DoctorWorkloadReport` · Query: `from`, `to`

Returns `IEnumerable<DoctorWorkloadDto>` ordered alphabetically by doctor name.

---

## 21. Medicine Catalogue Endpoints

> Any valid JWT (Doctor / Admin / Patient).

### `GET /api/medicines`
Query: `search`, `pageNumber` (default 1), `pageSize` (default 20, max 100) ? `PaginationResponse<MedicineDto>`

### `GET /api/medicines/{id}`
? `MedicineDto` · 404 if not found

---

## 22. Appointment Status State Machine

Terminal states

| Transition | Patient | Doctor | Admin |
|---|:---:|:---:|:---:|
| `Scheduled` ? `Confirmed` | ? | ? | ? |
| `Confirmed` ? `Checked-In` | ? | ? | ? |
| `Checked-In` ? `In Progress` | ? | ? | ? |
| `In Progress` ? `Completed` | ? | ? | ? |
| Any non-terminal ? `Cancelled` | ? | ? | ? |
| Any non-terminal ? `No-Show` | ? | ? | ? |
| Any non-terminal ? `Rescheduled` | ? | ? | ? |

> Unlisted transitions ? 400 Bad Request.  
> Leaving a terminal state ? 400 Bad Request.

---

## 23. Data Schemas

### `AuthResponseDto`

| Field | Type | Description |
|---|---|---|
| `userId` | `int` | User primary key |
| `email` | `string` | Email |
| `token` | `string` | JWT access token (60 min) |
| `refreshToken` | `string` | Refresh token |
| `tokenExpiresAt` | `DateTime` | UTC access token expiry |
| `role` | `string` | `Patient` \| `Doctor` \| `LabTechnician` \| `Admin` |
| `emailVerified` | `bool` | |
| `profileCompleted` | `bool` | Whether profile setup is done |

---

### `AppointmentDto`

| Field | Type | Description |
|---|---|---|
| `appointmentId` | `int` | Primary key |
| `patientId` | `int` | |
| `patientName` | `string` | |
| `doctorId` | `int` | |
| `doctorName` | `string` | |
| `slotId` | `int?` | Linked `AppointmentSlot` ID (`null` for legacy appointments) |
| `appointmentStart` | `DateTime` | UTC start (copied from slot) |
| `appointmentEnd` | `DateTime` | UTC end (copied from slot) |
| `reason` | `string?` | |
| `status` | `string` | `Scheduled` \| `Confirmed` \| `Checked-In` \| `In Progress` \| `Completed` \| `Cancelled` \| `No-Show` \| `Rescheduled` |

---

### `CreateAppointmentDto`

| Field | Type | Required | Description |
|---|---|---|---|
| `slotId` | `int` | Yes | ID of an `Available` slot |
| `reason` | `string?` | No | Max 500 chars |

---

### `RescheduleAppointmentDto`

| Field | Type | Required | Description |
|---|---|---|---|
| `newSlotId` | `int` | Yes | ID of a different `Available` slot |

---

### `AvailabilityTemplateDto`

| Field | Type | Description |
|---|---|---|
| `id` | `int` | Primary key |
| `doctorId` | `int` | |
| `doctorName` | `string` | |
| `dayOfWeek` | `int` | 0–6 |
| `dayName` | `string` | e.g. `"Monday"` |
| `startTime` | `TimeSpan` | |
| `endTime` | `TimeSpan` | |
| `slotDurationMinutes` | `int` | |
| `effectiveFromDate` | `DateOnly` | |
| `effectiveToDate` | `DateOnly?` | |
| `isActive` | `bool` | |
| `createdAt` | `DateTime` | UTC |

---

### `AvailabilityDayDto`

| Field | Type | Description |
|---|---|---|
| `id` | `int` | Primary key |
| `doctorId` | `int` | |
| `date` | `DateOnly` | Calendar date |
| `startTime` | `TimeSpan` | |
| `endTime` | `TimeSpan` | |
| `slotDurationMinutes` | `int` | |
| `status` | `string` | `Open` \| `Closed` \| `Leave` \| `Holiday` |
| `sourceTemplateId` | `int?` | Template that generated this day |
| `totalSlots` | `int` | |
| `availableSlots` | `int` | |

---

### `SlotDto`

| Field | Type | Description |
|---|---|---|
| `id` | `int` | Primary key |
| `doctorId` | `int` | |
| `availabilityDayId` | `int` | Parent day |
| `slotStart` | `DateTime` | UTC |
| `slotEnd` | `DateTime` | UTC |
| `status` | `string` | `Available` \| `Booked` \| `Blocked` \| `Cancelled` |
| `appointmentId` | `int?` | Set once booked |

---

### `SlotDetailDto`

| Field | Type | Description |
|---|---|---|
| `slotId` | `int` | Primary key |
| `doctorId` | `int` | |
| `slotStart` | `DateTime` | UTC |
| `slotEnd` | `DateTime` | UTC |
| `status` | `string` | |
| `appointmentId` | `int?` | |

---

### `DoctorCalendarDayDto`

| Field | Type | Description |
|---|---|---|
| `date` | `DateOnly` | Calendar date |
| `dayStatus` | `string` | `Open` \| `Closed` \| `Leave` \| `Holiday` \| `NoSchedule` |
| `totalSlots` | `int` | |
| `availableSlots` | `int` | |
| `bookedSlots` | `int` | |

---

### `PatientAvailabilityPreviewDto`

| Field | Type | Description |
|---|---|---|
| `date` | `DateOnly` | Calendar date |
| `availableSlots` | `int` | Count of `Available` slots |

---

### `SessionFeedbackDto`

| Field | Type | Description |
|---|---|---|
| `feedbackId` | `int` | |
| `consultationId` | `int` | |
| `patientId` | `int` | |
| `patientName` | `string` | |
| `doctorId` | `int` | |
| `doctorName` | `string` | |
| `rating` | `int` | 1–5 |
| `ratingLabel` | `string` | `Very Poor` \| `Poor` \| `Average` \| `Good` \| `Excellent` |
| `comment` | `string?` | Max 1000 chars |
| `createdAt` | `DateTime` | UTC |
| `isEdited` | `bool` | |
| `updatedAt` | `DateTime?` | UTC of last edit |

---

### `AppointmentReportDto`

| Field | Type | Description |
|---|---|---|
| `appointmentId` | `int` | |
| `appointmentStart` | `DateTime` | |
| `appointmentEnd` | `DateTime` | |
| `patientName` | `string` | |
| `patientPhone` | `string?` | |
| `mrn` | `string` | |
| `doctorName` | `string` | |
| `departmentName` | `string?` | |
| `statusName` | `string` | |
| `reason` | `string?` | |
| `hasConsultation` | `bool` | |

---

### `DoctorWorkloadDto`

| Field | Type | Description |
|---|---|---|
| `doctorId` | `int` | |
| `doctorName` | `string` | |
| `qualification` | `string?` | |
| `departmentName` | `string?` | |
| `totalAppointments` | `int` | |
| `completedAppointments` | `int` | |
| `cancelledAppointments` | `int` | |
| `totalConsultations` | `int` | |

---

### `ConsultationDto`

| Field | Type | Description |
|---|---|---|
| `consultationId` | `int` | |
| `appointmentId` | `int` | Immutable after creation |
| `patientId` | `int` | |
| `chiefComplaint` | `string?` | |
| `examination` | `string?` | |
| `diagnosisNotes` | `string?` | |
| `treatmentPlan` | `string?` | |
| `icdCode` | `string?` | |
| `createdAt` | `DateTime` | |
| `prescriptions` | `List<PrescriptionDto>` | |
| `orderedTests` | `List<OrderedTestDto>` | |

---

### `MedicalHistoryDto`

| Field | Type | Description |
|---|---|---|
| `patientId` | `int` | |
| `patientName` | `string` | |
| `mrn` | `string` | |
| `dateOfBirth` | `DateOnly` | |
| `gender` | `string?` | |
| `bloodGroup` | `string?` | |
| `allergies` | `List<string>` | |
| `visits` | `List<MedicalVisitDto>` | |

---

### `MedicineDto`

| Field | Type | Description |
|---|---|---|
| `medicineId` | `int` | |
| `medicineName` | `string` | e.g. `Paracetamol 500mg` |

---

### `LabTestCatalogueDto`

| Field | Type | Description |
|---|---|---|
| `labTestId` | `int` | |
| `testName` | `string` | e.g. `Complete Blood Count (CBC)` |

---

### `LabResultDto`

| Field | Type | Description |
|---|---|---|
| `orderedTestId` | `int` | |
| `consultationId` | `int` | |
| `labTestId` | `int` | |
| `testName` | `string` | |
| `status` | `string` | `Pending` \| `Completed` |
| `result` | `string?` | |
| `resultDate` | `DateTime?` | |
| `patientId` | `int` | |
| `patientName` | `string` | |

---

### `DoctorDto`

| Field | Type | Description |
|---|---|---|
| `doctorId` | `int` | |
| `licenseNumber` | `string` | |
| `fullName` | `string` | |
| `qualification` | `string?` | |
| `experienceYears` | `int?` | |
| `isActive` | `bool` | |
| `address` | `AddressDto?` | |
| `departments` | `List<DepartmentDto>` | |

---

### `PatientDto`

| Field | Type | Description |
|---|---|---|
| `patientId` | `int` | |
| `userId` | `int` | |
| `mrn` | `string` | |
| `fullName` | `string` | |
| `dateOfBirth` | `DateOnly` | |
| `gender` | `string?` | |
| `phoneNumber` | `string?` | |
| `bloodGroup` | `string?` | |
| `emergencyContact` | `string?` | |
| `address` | `AddressDto?` | |
| `allergies` | `List<PatientAllergyDto>?` | |

---

### `DepartmentDto`

| Field | Type | Description |
|---|---|---|
| `departmentId` | `int` | Primary key |
| `departmentName` | `string` | Max 100 chars |
| `description` | `string?` | Max 500 chars |
| `isActive` | `bool` | |

---

### `CreateDepartmentDto`

| Field | Type | Required | Constraints |
|---|---|---|---|
| `departmentName` | `string` | Yes | Max 100 chars; must be unique |
| `description` | `string?` | No | Max 500 chars |
| `isActive` | `bool` | No | Defaults to `true` |

---

### `UpdateDepartmentDto`

| Field | Type | Required | Constraints |
|---|---|---|---|
| `departmentName` | `string` | Yes | Max 100 chars; must be unique (excludes self) |
| `description` | `string?` | No | Max 500 chars |
| `isActive` | `bool` | Yes | |

---

### `ICDCodeDto`

| Field | Type | Description |
|---|---|---|
| `id` | `int` | Primary key |
| `code` | `string` | ICD-10 code (e.g. `J06.9`) |
| `description` | `string` | Code description |

---

### `AdminUserDto`

| Field | Type | Description |
|---|---|---|
| `id` | `int` | User primary key |
| `email` | `string` | |
| `role` | `string?` | `Admin` \| `Doctor` \| `Patient` \| `LabTechnician` |
| `isActive` | `bool` | |
| `createdAt` | `DateTime` | UTC creation time |

---

### `PatientVitalDto`

| Field | Type | Description |
|---|---|---|
| `vitalId` | `int` | Primary key |
| `patientId` | `int` | Owning patient |
| `height` | `decimal?` | cm (0.01 – 300) |
| `weight` | `decimal?` | kg (0.01 – 700) |
| `bloodPressure` | `string?` | e.g. `"120/80"` (max 20 chars) |
| `heartRate` | `int?` | bpm (1 – 300) |
| `temperature` | `decimal?` | degrees C (30 – 45) |
| `recordedAt` | `DateTime` | UTC — auto-set on create |

---

## 24. Quick Reference

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
| POST | `/api/appointments` | Yes | Patient |
| GET | `/api/appointments` | Yes | Admin / Doctor |
| GET | `/api/appointments/{id}` | Yes | Any (ownership enforced) |
| GET | `/api/appointments/me` | Yes | Patient |
| GET | `/api/appointments/doctor/me` | Yes | Doctor |
| PATCH | `/api/appointments/{id}/reschedule` | Yes | Any (ownership enforced) |
| PATCH | `/api/appointments/{id}/status` | Yes | Admin / Doctor |
| DELETE | `/api/appointments/{id}` | Yes | Any (ownership enforced) |
| POST | `/api/doctors/{doctorId}/availability-template` | Yes | Admin / Doctor |
| GET | `/api/doctors/{doctorId}/availability-template` | No | — |
| PATCH | `/api/doctors/availability-template/{templateId}` | Yes | Admin / Doctor |
| DELETE | `/api/doctors/availability-template/{templateId}` | Yes | Admin / Doctor |
| GET | `/api/doctors/{doctorId}/availability-days` | No | — |
| PATCH | `/api/doctors/availability-day/{dayId}` | Yes | Admin / Doctor |
| GET | `/api/doctors/{doctorId}/calendar` | No | — |
| GET | `/api/doctors/{doctorId}/availability` | No | — |
| POST | `/api/doctors/{doctorId}/leave` | Yes | Admin / Doctor |
| GET | `/api/doctors/{doctorId}/slots` | No | — |
| GET | `/api/slots/{slotId}` | No | — |
| PATCH | `/api/slots/{slotId}` | Yes | Admin |
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
| GET | `/api/lab-tests/catalogue` | Yes | Any |
| GET | `/api/lab-tests/catalogue/{id}` | Yes | Any |
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
| GET | `/api/departments` | Yes | Any |
| GET | `/api/departments/{id}` | Yes | Any |
| POST | `/api/departments` | Yes | Admin |
| PUT | `/api/departments/{id}` | Yes | Admin |
| DELETE | `/api/departments/{id}` | Yes | Admin |
| GET | `/api/icd-codes` | Yes | Admin / Doctor / Patient |
| GET | `/api/admin/users` | Yes | Admin |
| GET | `/api/admin/users/{id}` | Yes | Admin |
| PATCH | `/api/admin/users/{id}/disable` | Yes | Admin |
| PATCH | `/api/admin/users/{id}/enable` | Yes | Admin |
| GET | `/api/patients/{patientId}/vitals` | Yes | Doctor / Admin / Owner |
| GET | `/api/patients/{patientId}/vitals/{vitalId}` | Yes | Doctor / Admin / Owner |
| POST | `/api/patients/{patientId}/vitals` | Yes | Doctor / Admin |
| PUT | `/api/patients/{patientId}/vitals/{vitalId}` | Yes | Doctor / Admin |
| DELETE | `/api/patients/{patientId}/vitals/{vitalId}` | Yes | Doctor / Admin |

---

*Generated for Axivora HMS · .NET 10 / C# 14 · https://github.com/Karthickraja018/AxivoraHMS*
