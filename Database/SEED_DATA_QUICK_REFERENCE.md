# ?? Seed Data Quick Reference - Axivora HMS

## ?? Tables Requiring Seed Data

| # | Table | Records | Priority | Why Needed |
|---|-------|---------|----------|------------|
| 1 | **Roles** | 3 | ?? CRITICAL | Authentication & authorization |
| 2 | **Users (Admin)** | 1 | ?? CRITICAL | System bootstrap & access |
| 3 | **Departments** | 25 | ?? CRITICAL | Doctor creation & assignment |
| 4 | **AppointmentStatus** | 8 | ?? CRITICAL | Appointment workflow states |
| 5 | **ICDCodes** | 48+ | ?? RECOMMENDED | Diagnosis coding (consultations) |
| 6 | **Medicines** | 60+ | ?? RECOMMENDED | Prescription management |
| 7 | **LabTests** | 68+ | ?? RECOMMENDED | Lab test ordering |

---

## ? Quick Import Commands

### Full Import (Recommended)
```sql
-- File: Database/SeedData.sql
-- Open in SSMS/Azure Data Studio and execute (F5)
```

### Command Line
```bash
sqlcmd -S localhost -d AxivoraHMS -i "Database/SeedData.sql"
```

---

## ?? What Gets Seeded

### Roles (3)
```
1. Admin
2. Doctor
3. Patient
```

### Admin User (1)
```
Email: admin@axivora.com
Password: Admin123!
```

### Departments (25)
```
Cardiology, Neurology, Orthopedics, Pediatrics, 
Obstetrics and Gynecology, General Surgery, 
Internal Medicine, Dermatology, Ophthalmology, 
ENT, Psychiatry, Radiology, Anesthesiology, 
Emergency Medicine, Family Medicine, Oncology, 
Urology, Nephrology, Gastroenterology, Pulmonology, 
Endocrinology, Rheumatology, Hematology, 
Infectious Disease, Physical Medicine
```

### Appointment Statuses (8)
```
1. Scheduled
2. Confirmed
3. Checked-In
4. In Progress
5. Completed
6. Cancelled
7. No-Show
8. Rescheduled
```

### ICD-10 Codes (48)
```
Common conditions:
- Chronic: Hypertension, Diabetes, Hyperlipidemia, Asthma
- Acute: Common cold, URI, Bronchitis, Gastroenteritis
- Musculoskeletal: Back pain, Osteoarthritis
- Mental: Anxiety, Depression
- Cardiovascular: MI, Heart failure, AFib
- And 35+ more...
```

### Medicines (60+)
```
Categories:
- Antibiotics (6)
- Pain Relief (6)
- Antihypertensives (5)
- Diabetes (4)
- Cardiovascular (4)
- Respiratory (5)
- GI (6)
- Mental Health (6)
- Vitamins (6)
- Others (12+)
```

### Lab Tests (68+)
```
Categories:
- Hematology (8)
- Biochemistry (18)
- Hormones (9)
- Cardiac Markers (4)
- Urine Tests (4)
- Microbiology (9)
- Immunology (8)
- Tumor Markers (5)
- Others (3)
```

---

## ? Verification (After Import)

```sql
-- Quick summary
SELECT 
    'Roles' AS TableName, COUNT(*) AS Count FROM Roles
UNION ALL SELECT 'Admins', COUNT(*) FROM Users WHERE Email LIKE '%admin%'
UNION ALL SELECT 'Departments', COUNT(*) FROM Departments
UNION ALL SELECT 'Statuses', COUNT(*) FROM AppointmentStatus
UNION ALL SELECT 'ICD Codes', COUNT(*) FROM ICDCodes
UNION ALL SELECT 'Medicines', COUNT(*) FROM Medicines
UNION ALL SELECT 'Lab Tests', COUNT(*) FROM LabTests;
```

**Expected Output:**
```
Roles:        3
Admins:       1
Departments: 25
Statuses:     8
ICD Codes:   48+
Medicines:   60+
Lab Tests:   68+
```

---

## ?? Dependencies

### To Create Doctor:
```
? Roles must exist
? Admin user must exist
? At least 1 Department must exist
```

### To Create Appointment:
```
? AppointmentStatus must exist
? Patient must exist
? Doctor must exist
```

### To Create Consultation:
```
? Appointment must exist
? ICDCode (optional but recommended)
```

### To Create Prescription:
```
? Consultation must exist
? Medicine must exist
```

### To Order Lab Test:
```
? Consultation must exist
? LabTest must exist
```

---

## ?? Admin Credentials

```
URL: https://localhost:7087/api/auth/login

Body:
{
  "email": "admin@axivora.com",
  "password": "Admin123!"
}

Response:
{
  "userId": 1,
  "email": "admin@axivora.com",
  "token": "eyJhbGc...",
  "role": "Admin",
  ...
}
```

?? **Change password after first login!**

---

## ?? Common Use Cases

### Use Case 1: Create Doctor
**Requires:** Departments seeded
```
POST /api/doctors
Body: {
  "email": "doctor@test.com",
  "departmentIds": [1]  ? Requires Departments
}
```

### Use Case 2: Book Appointment
**Requires:** AppointmentStatus seeded
```
POST /api/appointments
Body: {
  "statusId": 1  ? Requires AppointmentStatus
}
```

### Use Case 3: Add Diagnosis
**Requires:** ICD Codes seeded
```
POST /api/consultations
Body: {
  "icdId": 1  ? Requires ICDCodes
}
```

### Use Case 4: Prescribe Medicine
**Requires:** Medicines seeded
```
POST /api/consultations/{id}/prescriptions
Body: {
  "medicineId": 1  ? Requires Medicines
}
```

### Use Case 5: Order Lab Test
**Requires:** Lab Tests seeded
```
POST /api/consultations/{id}/lab-tests
Body: {
  "labTestId": 1  ? Requires LabTests
}
```

---

## ?? Minimal vs Full Seed

### Minimal (For Quick Testing)
```
? Roles (3)
? Admin (1)
? Departments (6 basic)
? Statuses (4 basic)
```
**Time:** 30 seconds  
**Use:** Quick API testing

### Full (Production Ready)
```
? Roles (3)
? Admin (1)
? Departments (25)
? Statuses (8)
? ICD Codes (48)
? Medicines (60+)
? Lab Tests (68+)
```
**Time:** 2 minutes  
**Use:** Complete system testing

---

## ?? Re-seed (If Needed)

### Clear and Re-import
```sql
-- 1. Delete reference data
DELETE FROM LabTests;
DELETE FROM Medicines;
DELETE FROM ICDCodes;
DELETE FROM AppointmentStatus;
DELETE FROM Departments;
-- Don't delete Roles/Users if you have real users!

-- 2. Reset identity
DBCC CHECKIDENT ('LabTests', RESEED, 0);
DBCC CHECKIDENT ('Medicines', RESEED, 0);
DBCC CHECKIDENT ('ICDCodes', RESEED, 0);
DBCC CHECKIDENT ('AppointmentStatus', RESEED, 0);
DBCC CHECKIDENT ('Departments', RESEED, 0);

-- 3. Re-run SeedData.sql
```

---

## ?? Files

| File | Purpose |
|------|---------|
| `Database/SeedData.sql` | Full automated seed script |
| `Database/SEED_DATA_README.md` | Detailed documentation |
| `SEED_DATA_QUICK_REFERENCE.md` | This file (quick ref) |

---

## ?? Ready to Test!

After seeding:

1. ? Login as admin
2. ? Create doctor (uses Departments)
3. ? Create patient
4. ? Create appointment (uses AppointmentStatus)
5. ? Create consultation (uses ICDCodes)
6. ? Add prescription (uses Medicines)
7. ? Order lab test (uses LabTests)

---

**Quick Help:**
- Full guide: `Database/SEED_DATA_README.md`
- Postman: `Axivora_Postman_Collection.json`
- Testing: `POSTMAN_TESTING_GUIDE.md`

---

**Created:** 2026-03-04  
**Version:** 1.0
