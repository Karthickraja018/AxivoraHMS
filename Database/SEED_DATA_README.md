# ?? Database Seed Data Guide - Axivora HMS

## ?? Overview

This guide explains which tables require pre-seeded reference data and how to import them.

---

## ?? Tables Requiring Seed Data

### ? CRITICAL (Must Have Before Testing)

| Table | Description | Count | Required For |
|-------|-------------|-------|--------------|
| **Roles** | User role types | 3 | Authentication, Authorization |
| **Admin User** | System administrator | 1 | Initial system access |
| **Departments** | Medical departments | 25 | Doctor creation, Assignment |
| **AppointmentStatus** | Appointment states | 8 | Appointments workflow |

### ? IMPORTANT (Recommended for Functional System)

| Table | Description | Count | Required For |
|-------|-------------|-------|--------------|
| **ICDCodes** | ICD-10 diagnosis codes | 48+ | Consultations, Diagnoses |
| **Medicines** | Common medications | 60+ | Prescriptions |
| **LabTests** | Laboratory test types | 68+ | Ordered tests, Diagnostics |

### ? OPTIONAL (Can Add Later)

| Table | Description | Notes |
|-------|-------------|-------|
| **Addresses** | Created by users | Dynamic data |
| **Users** | Created via registration | Dynamic data |
| **Patients** | Created by users/admin | Dynamic data |
| **Doctors** | Created by admin | Dynamic data |
| **Appointments** | Created by users | Transactional data |
| **Consultations** | Created by doctors | Transactional data |

---

## ?? Quick Import

### Option 1: Full Automated Import (RECOMMENDED)

Run the comprehensive SQL script:

```sql
-- Execute from SQL Server Management Studio or Azure Data Studio
-- Make sure you're connected to the correct database

-- File: Database/SeedData.sql
```

**Steps:**
1. Open SQL Server Management Studio (SSMS) or Azure Data Studio
2. Connect to your SQL Server instance
3. Open file: `Database/SeedData.sql`
4. Execute (F5)
5. ? Done!

**What it does:**
- ? Creates 3 roles (Admin, Doctor, Patient)
- ? Creates admin user (admin@axivora.com / Admin123!)
- ? Seeds 25 medical departments
- ? Seeds 8 appointment statuses
- ? Seeds 48 common ICD-10 codes
- ? Seeds 60+ common medicines
- ? Seeds 68+ common lab tests
- ? Idempotent (safe to run multiple times)

---

### Option 2: Minimal Seed (Quick Testing)

If you only want to test quickly, run this minimal version:

```sql
-- Minimal seed data for testing

USE AxivoraHMS;
GO

-- 1. Roles
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
    INSERT INTO Roles (RoleName) VALUES ('Admin');

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Doctor')
    INSERT INTO Roles (RoleName) VALUES ('Doctor');

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Patient')
    INSERT INTO Roles (RoleName) VALUES ('Patient');

-- 2. Admin User
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@axivora.com')
BEGIN
    INSERT INTO Users (Email, PasswordHash, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('admin@axivora.com', 'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=', 1, 0, GETDATE(), GETDATE());
    
    DECLARE @AdminUserId INT = SCOPE_IDENTITY();
    DECLARE @AdminRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Admin');
    
    INSERT INTO UserRoles (UserId, RoleId) VALUES (@AdminUserId, @AdminRoleId);
END

-- 3. Critical Departments
INSERT INTO Departments (DepartmentName) 
SELECT * FROM (VALUES 
    ('Cardiology'),
    ('Neurology'),
    ('Orthopedics'),
    ('Pediatrics'),
    ('General Surgery'),
    ('Internal Medicine')
) AS Depts(DepartmentName)
WHERE NOT EXISTS (SELECT 1 FROM Departments WHERE DepartmentName = Depts.DepartmentName);

-- 4. Appointment Statuses
INSERT INTO AppointmentStatus (StatusName)
SELECT * FROM (VALUES 
    ('Scheduled'),
    ('Confirmed'),
    ('Completed'),
    ('Cancelled')
) AS Statuses(StatusName)
WHERE NOT EXISTS (SELECT 1 FROM AppointmentStatus WHERE StatusName = Statuses.StatusName);

PRINT 'Minimal seed data imported successfully!';
PRINT 'Admin: admin@axivora.com / Admin123!';
GO
```

---

## ?? Detailed Reference Data

### 1. Roles (3 records)

```
RoleId | RoleName
-------|----------
1      | Admin
2      | Doctor
3      | Patient
```

**Purpose:** Define user access levels and permissions

---

### 2. Admin User (1 record)

```
Email: admin@axivora.com
Password: Admin123!
Role: Admin
```

**Purpose:** Initial system administrator for bootstrapping

?? **SECURITY:** Change password after first login!

---

### 3. Departments (25 records)

Complete list seeded:

1. Cardiology
2. Neurology
3. Orthopedics
4. Pediatrics
5. Obstetrics and Gynecology
6. General Surgery
7. Internal Medicine
8. Dermatology
9. Ophthalmology
10. ENT (Ear, Nose, Throat)
11. Psychiatry
12. Radiology
13. Anesthesiology
14. Emergency Medicine
15. Family Medicine
16. Oncology
17. Urology
18. Nephrology
19. Gastroenterology
20. Pulmonology
21. Endocrinology
22. Rheumatology
23. Hematology
24. Infectious Disease
25. Physical Medicine and Rehabilitation

**Purpose:** Assign doctors to specializations

---

### 4. Appointment Statuses (8 records)

1. **Scheduled** - Initial booking
2. **Confirmed** - Patient/admin confirmed
3. **Checked-In** - Patient arrived
4. **In Progress** - Consultation ongoing
5. **Completed** - Consultation finished
6. **Cancelled** - Appointment cancelled
7. **No-Show** - Patient didn't arrive
8. **Rescheduled** - Moved to different time

**Purpose:** Track appointment lifecycle

---

### 5. ICD-10 Codes (48 common conditions)

Sample categories included:

#### Chronic Diseases
- I10 - Essential hypertension
- E11.9 - Type 2 diabetes mellitus
- E78.5 - Hyperlipidemia
- J45.909 - Asthma

#### Acute Conditions
- J00 - Common cold
- J06.9 - Upper respiratory infection
- A09 - Gastroenteritis

#### Musculoskeletal
- M54.5 - Low back pain
- M19.90 - Osteoarthritis

#### Mental Health
- F41.9 - Anxiety disorder
- F32.9 - Major depression

#### General Symptoms
- R50.9 - Fever
- R51.9 - Headache
- R07.9 - Chest pain

**Purpose:** Standard diagnosis coding for consultations

**Note:** You can add more ICD-10 codes as needed. The script includes 48 common ones.

---

### 6. Medicines (60+ common medications)

Categories included:

#### Antibiotics (6)
- Amoxicillin, Azithromycin, Ciprofloxacin, etc.

#### Pain Relief (6)
- Paracetamol, Ibuprofen, Diclofenac, etc.

#### Antihypertensives (5)
- Amlodipine, Losartan, Atenolol, etc.

#### Diabetes (4)
- Metformin, Glimepiride, Insulin, etc.

#### Cardiovascular (4)
- Atorvastatin, Rosuvastatin, Clopidogrel, etc.

#### Respiratory (5)
- Salbutamol, Budesonide, Montelukast, etc.

#### Gastrointestinal (6)
- Omeprazole, Ranitidine, Domperidone, etc.

#### Mental Health (6)
- Sertraline, Fluoxetine, Alprazolam, etc.

#### Vitamins & Supplements (6)
- Vitamin D3, Calcium, Folic Acid, etc.

**Purpose:** Prescription management

---

### 7. Lab Tests (68+ common tests)

Categories included:

#### Hematology (8 tests)
- Complete Blood Count (CBC)
- Hemoglobin, Platelet Count, etc.

#### Biochemistry (18 tests)
- Fasting Blood Sugar
- Liver Function Test (LFT)
- Kidney Function Test (KFT)
- Lipid Profile, etc.

#### Hormones (9 tests)
- Thyroid Function Test (TFT)
- TSH, Vitamin D, Vitamin B12, etc.

#### Cardiac Markers (4 tests)
- Troponin, CPK-MB, NT-proBNP, etc.

#### Urine Tests (4 tests)
- Urine Routine & Microscopy
- Urine Culture, etc.

#### Microbiology (9 tests)
- Blood Culture
- COVID-19 RT-PCR
- Malaria, Dengue tests, etc.

#### Immunology (8 tests)
- CRP, Rheumatoid Factor
- HIV, Hepatitis tests, etc.

#### Tumor Markers (5 tests)
- PSA, CEA, CA 125, etc.

**Purpose:** Laboratory test ordering and tracking

---

## ?? How to Run Seed Script

### Using SQL Server Management Studio (SSMS)

1. **Open SSMS**
2. **Connect** to your SQL Server instance
3. **File ? Open ? File**
4. Select `Database/SeedData.sql`
5. **Verify database**: Make sure you're connected to `AxivoraHMS`
6. **Execute**: Press `F5` or click Execute button
7. **Check output**: Review messages window for success

### Using Azure Data Studio

1. **Open Azure Data Studio**
2. **Connect** to your SQL Server
3. **File ? Open File**
4. Select `Database/SeedData.sql`
5. **Change database**: Select `AxivoraHMS` from dropdown
6. **Run**: Click Run button or press `F5`
7. **Review**: Check Results pane

### Using Command Line (sqlcmd)

```bash
sqlcmd -S localhost -d AxivoraHMS -i "Database/SeedData.sql"
```

Or with authentication:

```bash
sqlcmd -S localhost -U sa -P YourPassword -d AxivoraHMS -i "Database/SeedData.sql"
```

---

## ? Verification Queries

After running seed script, verify data:

```sql
-- Check Roles
SELECT * FROM Roles;
-- Expected: 3 rows (Admin, Doctor, Patient)

-- Check Admin User
SELECT u.Email, u.IsActive, r.RoleName 
FROM Users u 
JOIN UserRoles ur ON u.UserId = ur.UserId
JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.Email LIKE '%admin%';
-- Expected: admin@axivora.com with Admin role

-- Check Departments
SELECT COUNT(*) AS DepartmentCount FROM Departments;
-- Expected: 25

-- Check Appointment Statuses
SELECT COUNT(*) AS StatusCount FROM AppointmentStatus;
-- Expected: 8

-- Check ICD Codes
SELECT COUNT(*) AS ICDCount FROM ICDCodes;
-- Expected: 48+

-- Check Medicines
SELECT COUNT(*) AS MedicineCount FROM Medicines;
-- Expected: 60+

-- Check Lab Tests
SELECT COUNT(*) AS LabTestCount FROM LabTests;
-- Expected: 68+

-- Summary
SELECT 
    'Roles' AS TableName, COUNT(*) AS RecordCount FROM Roles
UNION ALL SELECT 'Admin Users', COUNT(*) FROM Users WHERE Email LIKE '%admin%'
UNION ALL SELECT 'Departments', COUNT(*) FROM Departments
UNION ALL SELECT 'Appointment Statuses', COUNT(*) FROM AppointmentStatus
UNION ALL SELECT 'ICD Codes', COUNT(*) FROM ICDCodes
UNION ALL SELECT 'Medicines', COUNT(*) FROM Medicines
UNION ALL SELECT 'Lab Tests', COUNT(*) FROM LabTests;
```

---

## ?? Testing Workflow After Seeding

### 1. Test Admin Login
```
Postman: Public / Authentication ? Login
Body: {
  "email": "admin@axivora.com",
  "password": "Admin123!"
}
Expected: 200 OK with token and role="Admin"
```

### 2. Create Doctor (Requires Department)
```
Postman: Admin Only ? Doctors ? Create Doctor
Requires: At least one department in database
departmentIds: [1] (Cardiology)
```

### 3. Test Appointments (Requires Status)
```
Create appointment with statusId: 1 (Scheduled)
```

### 4. Test Consultation (Requires ICD Code)
```
Create consultation with ICDId: 1 (Hypertension)
```

### 5. Test Prescription (Requires Medicine)
```
Add prescription with MedicineId: 1 (Amoxicillin)
```

---

## ?? Reset Seed Data (If Needed)

To delete all seed data and start fresh:

```sql
-- WARNING: This will delete ALL data!

-- Delete transactional data first (if any)
DELETE FROM Prescriptions;
DELETE FROM OrderedTests;
DELETE FROM PatientVitals;
DELETE FROM Consultations;
DELETE FROM Appointments;
DELETE FROM PatientAllergies;
DELETE FROM DoctorSchedules;
DELETE FROM DoctorDepartments;
DELETE FROM Doctors;
DELETE FROM Patients;
DELETE FROM UserRoles;
DELETE FROM Users;

-- Delete reference data
DELETE FROM LabTests;
DELETE FROM Medicines;
DELETE FROM ICDCodes;
DELETE FROM AppointmentStatus;
DELETE FROM Departments;
DELETE FROM Roles;

-- Reset identity seeds
DBCC CHECKIDENT ('LabTests', RESEED, 0);
DBCC CHECKIDENT ('Medicines', RESEED, 0);
DBCC CHECKIDENT ('ICDCodes', RESEED, 0);
DBCC CHECKIDENT ('AppointmentStatus', RESEED, 0);
DBCC CHECKIDENT ('Departments', RESEED, 0);
DBCC CHECKIDENT ('Roles', RESEED, 0);
DBCC CHECKIDENT ('Users', RESEED, 0);

-- Now re-run SeedData.sql
```

---

## ?? Adding More Data Later

### Add More Departments
```sql
INSERT INTO Departments (DepartmentName) VALUES ('Sports Medicine');
```

### Add More ICD Codes
```sql
INSERT INTO ICDCodes (Code, Description) 
VALUES ('I21.9', 'Acute myocardial infarction, unspecified');
```

### Add More Medicines
```sql
INSERT INTO Medicines (MedicineName) 
VALUES ('Lisinopril 10mg');
```

### Add More Lab Tests
```sql
INSERT INTO LabTests (TestName) 
VALUES ('MRI Scan');
```

---

## ?? Summary

### Required Seed Data Checklist

- [x] **Roles** (3) - CRITICAL
- [x] **Admin User** (1) - CRITICAL
- [x] **Departments** (25) - CRITICAL
- [x] **Appointment Statuses** (8) - CRITICAL
- [x] **ICD-10 Codes** (48) - RECOMMENDED
- [x] **Medicines** (60+) - RECOMMENDED
- [x] **Lab Tests** (68+) - RECOMMENDED

### Next Steps

1. ? Run `Database/SeedData.sql`
2. ? Verify data using verification queries
3. ? Test admin login in Postman
4. ? Create test doctor
5. ? Start testing full workflows

---

## ?? Related Files

- **Seed Script**: `Database/SeedData.sql`
- **Postman Collection**: `Axivora_Postman_Collection.json`
- **Testing Guide**: `POSTMAN_TESTING_GUIDE.md`
- **Quick Start**: `START_HERE.md`

---

**Created:** 2026-03-04  
**Version:** 1.0  
**Database:** AxivoraHMS  
