# ?? Database Seed Data - Summary

## ? Files Created

1. **`Database/SeedData.sql`** - Complete automated seed script (2,500+ lines)
2. **`Database/SEED_DATA_README.md`** - Comprehensive documentation
3. **`Database/SEED_DATA_QUICK_REFERENCE.md`** - Quick reference card

---

## ?? What Was Created

### Comprehensive SQL Seed Script

**File:** `Database/SeedData.sql`

**Features:**
- ? Idempotent (safe to run multiple times)
- ? Checks for existing data before inserting
- ? Provides detailed progress output
- ? Includes summary at the end
- ? Production-ready quality

**Data Seeded:**

| Category | Items | Count | Description |
|----------|-------|-------|-------------|
| **Roles** | System roles | 3 | Admin, Doctor, Patient |
| **Admin User** | Bootstrap account | 1 | admin@axivora.com / Admin123! |
| **Departments** | Medical specialties | 25 | All major medical departments |
| **Appointment Statuses** | Workflow states | 8 | Full appointment lifecycle |
| **ICD-10 Codes** | Diagnosis codes | 48 | Common medical conditions |
| **Medicines** | Medications | 60+ | Common drugs by category |
| **Lab Tests** | Laboratory tests | 68+ | All major test categories |
| **TOTAL** | Reference records | **213+** | Complete reference data |

---

## ?? Tables Seeded

### CRITICAL (Must Seed)

1. **Roles** (3 records)
   - Admin, Doctor, Patient
   - Required for: Authentication & Authorization

2. **Users - Admin** (1 record)
   - Email: admin@axivora.com
   - Password: Admin123!
   - Required for: System bootstrap

3. **Departments** (25 records)
   - All major medical specialties
   - Required for: Doctor creation & assignment

4. **AppointmentStatus** (8 records)
   - Scheduled, Confirmed, Checked-In, In Progress, Completed, Cancelled, No-Show, Rescheduled
   - Required for: Appointment workflow

### RECOMMENDED (For Full Functionality)

5. **ICDCodes** (48 records)
   - ICD-10 diagnosis codes
   - Categories: Chronic diseases, Acute conditions, Musculoskeletal, Mental health, Cardiovascular, etc.
   - Required for: Consultations, Diagnoses

6. **Medicines** (60+ records)
   - Categories: Antibiotics, Pain relief, Antihypertensives, Diabetes, Cardiovascular, Respiratory, GI, Mental health, Vitamins
   - Required for: Prescriptions

7. **LabTests** (68+ records)
   - Categories: Hematology, Biochemistry, Hormones, Cardiac markers, Urine tests, Microbiology, Immunology, Tumor markers
   - Required for: Lab test ordering

---

## ?? How to Use

### Quick Start (3 Steps)

1. **Open SQL Management Tool**
   - SQL Server Management Studio (SSMS), or
   - Azure Data Studio, or
   - sqlcmd command line

2. **Execute Seed Script**
   ```sql
   -- Open and run: Database/SeedData.sql
   ```

3. **Verify Success**
   ```sql
   SELECT 'Roles' AS TableName, COUNT(*) AS Count FROM Roles
   UNION ALL SELECT 'Departments', COUNT(*) FROM Departments
   UNION ALL SELECT 'Statuses', COUNT(*) FROM AppointmentStatus
   UNION ALL SELECT 'ICD Codes', COUNT(*) FROM ICDCodes
   UNION ALL SELECT 'Medicines', COUNT(*) FROM Medicines
   UNION ALL SELECT 'Lab Tests', COUNT(*) FROM LabTests;
   ```

**Expected Output:**
```
Roles:        3
Departments: 25
Statuses:     8
ICD Codes:   48
Medicines:   60+
Lab Tests:   68+
```

---

## ?? Seed Data Breakdown

### Roles (3)
```
1. Admin    - Full system access
2. Doctor   - Medical staff access
3. Patient  - Patient portal access
```

### Departments (25)
```
Medical Specialties:
?? Cardiology
?? Neurology
?? Orthopedics
?? Pediatrics
?? Obstetrics and Gynecology
?? General Surgery
?? Internal Medicine
?? Dermatology
?? Ophthalmology
?? ENT
?? Psychiatry
?? Radiology
?? Anesthesiology
?? Emergency Medicine
?? Family Medicine
?? Oncology
?? Urology
?? Nephrology
?? Gastroenterology
?? Pulmonology
?? Endocrinology
?? Rheumatology
?? Hematology
?? Infectious Disease
?? Physical Medicine and Rehabilitation
```

### Appointment Statuses (8)
```
Workflow States:
1. Scheduled     ? Initial booking
2. Confirmed     ? Patient confirmed
3. Checked-In    ? Patient arrived
4. In Progress   ? Consultation active
5. Completed     ? Consultation done
6. Cancelled     ? Appointment cancelled
7. No-Show       ? Patient didn't arrive
8. Rescheduled   ? Moved to new time
```

### ICD-10 Codes (48)
```
Categories:
?? Chronic Diseases (5)
?  ?? I10 - Hypertension
?  ?? E11.9 - Type 2 Diabetes
?  ?? E78.5 - Hyperlipidemia
?  ?? J45.909 - Asthma
?  ?? I25.10 - Coronary artery disease
?? Acute Conditions (5)
?  ?? J00 - Common cold
?  ?? J06.9 - URI
?  ?? J20.9 - Acute bronchitis
?  ?? A09 - Gastroenteritis
?  ?? K21.9 - GERD
?? Musculoskeletal (4)
?? Mental Health (3)
?? Infectious Diseases (3)
?? Cardiovascular (3)
?? Endocrine (2)
?? Respiratory (2)
?? Gastrointestinal (3)
?? Renal (2)
?? Dermatology (3)
?? Pediatric (2)
?? Obstetrics (2)
?? General Symptoms (9)
```

### Medicines (60+)
```
By Category:
?? Antibiotics (6)
?  ?? Amoxicillin 500mg
?  ?? Azithromycin 250mg
?  ?? Ciprofloxacin 500mg
?? Pain Relief (6)
?  ?? Paracetamol 500mg
?  ?? Ibuprofen 400mg
?  ?? Diclofenac 50mg
?? Antihypertensives (5)
?  ?? Amlodipine 5mg
?  ?? Losartan 50mg
?  ?? Atenolol 50mg
?? Diabetes (4)
?  ?? Metformin 500mg
?  ?? Glimepiride 2mg
?  ?? Insulin Glargine
?? Cardiovascular (4)
?? Respiratory (5)
?? Gastrointestinal (6)
?? Mental Health (6)
?? Thyroid (2)
?? Vitamins (6)
?? Eye Drops (3)
?? Other (7)
```

### Lab Tests (68+)
```
By Category:
?? Hematology (8)
?  ?? Complete Blood Count (CBC)
?  ?? Hemoglobin
?  ?? Platelet Count
?? Biochemistry (18)
?  ?? Fasting Blood Sugar
?  ?? Liver Function Test
?  ?? Kidney Function Test
?  ?? Lipid Profile
?? Hormones (9)
?  ?? Thyroid Function Test
?  ?? TSH
?  ?? Vitamin D
?? Cardiac Markers (4)
?? Urine Tests (4)
?? Microbiology (9)
?? Immunology (8)
?? Tumor Markers (5)
?? Other (3)
```

---

## ?? Admin Account Details

**Created by seed script:**

```
Email:    admin@axivora.com
Password: Admin123!
Role:     Admin
Status:   Active
```

**First Login:**
```
POST https://localhost:7087/api/auth/login
Content-Type: application/json

{
  "email": "admin@axivora.com",
  "password": "Admin123!"
}
```

**Response:**
```json
{
  "userId": 1,
  "email": "admin@axivora.com",
  "token": "eyJhbGc...",
  "role": "Admin",
  "emailVerified": false,
  "profileCompleted": false
}
```

?? **IMPORTANT:** Change the password after first login!

---

## ?? Use Cases Enabled

### After seeding, you can:

? **Admin Functions**
- Login as admin
- Create doctors (requires Departments)
- Create patients
- Manage all users

? **Doctor Creation**
- Assign to departments
- Set specializations
- Configure schedules

? **Appointment Management**
- Book appointments
- Set status (Scheduled, Confirmed, etc.)
- Track workflow

? **Consultations**
- Record patient visits
- Add diagnoses (uses ICD codes)
- Take notes

? **Prescriptions**
- Add medications (uses Medicines)
- Set dosage & frequency
- Track duration

? **Lab Tests**
- Order tests (uses LabTests)
- Track status (Pending, Completed)
- Record results

---

## ? Verification Steps

### 1. Check Data Counts
```sql
SELECT 
    'Roles' AS TableName, COUNT(*) AS Count FROM Roles
UNION ALL SELECT 'Admin Users', COUNT(*) FROM Users WHERE Email LIKE '%admin%'
UNION ALL SELECT 'Departments', COUNT(*) FROM Departments
UNION ALL SELECT 'Appointment Statuses', COUNT(*) FROM AppointmentStatus
UNION ALL SELECT 'ICD-10 Codes', COUNT(*) FROM ICDCodes
UNION ALL SELECT 'Medicines', COUNT(*) FROM Medicines
UNION ALL SELECT 'Lab Tests', COUNT(*) FROM LabTests;
```

### 2. Test Admin Login (Postman)
```
Folder: ?? Public / Authentication ? Login
Body: { "email": "admin@axivora.com", "password": "Admin123!" }
Expected: 200 OK with token and role="Admin"
```

### 3. Create Test Doctor (Postman)
```
Folder: ?? Admin Only ? Doctors ? Create Doctor
Body: { ..., "departmentIds": [1] }
Expected: 201 Created (uses seeded Department)
```

### 4. Create Test Appointment
```
Body: { ..., "statusId": 1 }
Expected: 201 Created (uses seeded AppointmentStatus)
```

---

## ?? File Structure

```
Axivora/
??? Database/
?   ??? SeedData.sql                      ? Main seed script (RUN THIS!)
?   ??? SEED_DATA_README.md               ? Detailed documentation
?   ??? SEED_DATA_QUICK_REFERENCE.md      ? Quick reference
?   ??? SEED_DATA_SUMMARY.md              ? This file
??? Axivora_Postman_Collection.json       ? API testing
??? POSTMAN_TESTING_GUIDE.md              ? Testing guide
??? START_HERE.md                          ? Quick start
```

---

## ?? Maintenance

### Adding More Data

**Add Department:**
```sql
INSERT INTO Departments (DepartmentName) VALUES ('Sports Medicine');
```

**Add ICD Code:**
```sql
INSERT INTO ICDCodes (Code, Description) 
VALUES ('I21.9', 'Acute myocardial infarction');
```

**Add Medicine:**
```sql
INSERT INTO Medicines (MedicineName) 
VALUES ('Lisinopril 10mg');
```

**Add Lab Test:**
```sql
INSERT INTO LabTests (TestName) 
VALUES ('MRI Brain Scan');
```

### Re-seeding

If you need to clear and re-import:

```sql
-- Clear reference data (WARNING: Deletes data!)
DELETE FROM LabTests;
DELETE FROM Medicines;
DELETE FROM ICDCodes;
DELETE FROM AppointmentStatus;
DELETE FROM Departments;

-- Reset identity counters
DBCC CHECKIDENT ('LabTests', RESEED, 0);
DBCC CHECKIDENT ('Medicines', RESEED, 0);
DBCC CHECKIDENT ('ICDCodes', RESEED, 0);
DBCC CHECKIDENT ('AppointmentStatus', RESEED, 0);
DBCC CHECKIDENT ('Departments', RESEED, 0);

-- Re-run SeedData.sql
```

---

## ?? Benefits

### Automated & Consistent
- ? One-click import
- ? Idempotent (safe to re-run)
- ? No manual data entry
- ? Production-ready data

### Comprehensive Coverage
- ? All reference tables populated
- ? Real-world medical data
- ? Complete workflow support
- ? 213+ reference records

### Developer Friendly
- ? Detailed progress output
- ? Error handling
- ? Verification queries
- ? Well documented

### Testing Ready
- ? Admin account included
- ? All dependencies met
- ? Postman collection compatible
- ? Full API testing enabled

---

## ?? Statistics

```
Total Files Created:     3
Total SQL Lines:      2,500+
Total Records Seeded:  213+

Breakdown:
?? Roles:               3
?? Admin User:          1
?? Departments:        25
?? Statuses:            8
?? ICD Codes:          48
?? Medicines:          60
?? Lab Tests:          68
```

---

## ?? Quick Start

### 3-Step Setup

1. **Run Seed Script**
   ```
   Open Database/SeedData.sql in SSMS
   Press F5 to execute
   ```

2. **Verify Import**
   ```sql
   SELECT COUNT(*) FROM Departments; -- Should be 25
   ```

3. **Test Login**
   ```
   Postman: Login with admin@axivora.com / Admin123!
   ```

---

## ?? Related Documentation

| Document | Purpose |
|----------|---------|
| `Database/SeedData.sql` | **Main seed script - RUN THIS!** |
| `Database/SEED_DATA_README.md` | Detailed guide with examples |
| `Database/SEED_DATA_QUICK_REFERENCE.md` | Quick reference card |
| `Axivora_Postman_Collection.json` | API testing collection |
| `POSTMAN_TESTING_GUIDE.md` | Complete testing guide |
| `START_HERE.md` | Overall quick start |

---

## ? Summary

You now have:

1. ? **Complete seed data script** ready to run
2. ? **213+ reference records** covering all essential tables
3. ? **Admin account** for immediate system access
4. ? **Comprehensive documentation** with examples
5. ? **Production-ready data** for real-world scenarios

**Next Steps:**
1. Run `Database/SeedData.sql`
2. Login as admin in Postman
3. Start testing API endpoints
4. Create doctors, patients, appointments!

---

**Created:** 2026-03-04  
**Version:** 1.0  
**Database:** AxivoraHMS  
**Status:** Production Ready ?
