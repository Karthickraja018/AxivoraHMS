-- ============================================================================
-- Axivora HMS - Master Seed Data Script
-- Description: Seed essential reference data for Axivora Hospital Management System
-- Version: 1.1 (Fixed subquery errors)
-- Created: 2026-03-04
-- Updated: 2026-03-04
-- ============================================================================

-- IMPORTANT: Run this script AFTER running database migrations
-- This script is idempotent - safe to run multiple times

USE AxivoraHMS;
GO

SET NOCOUNT ON;
GO

PRINT 'Starting Axivora HMS Seed Data Import...';
PRINT '=========================================';
PRINT '';

-- ============================================================================
-- 1. ROLES
-- ============================================================================
PRINT '1. Seeding Roles...';

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
BEGIN
    INSERT INTO Roles (RoleName) VALUES ('Admin');
    PRINT '   ? Admin role created';
END
ELSE
    PRINT '   - Admin role already exists';

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Doctor')
BEGIN
    INSERT INTO Roles (RoleName) VALUES ('Doctor');
    PRINT '   ? Doctor role created';
END
ELSE
    PRINT '   - Doctor role already exists';

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Patient')
BEGIN
    INSERT INTO Roles (RoleName) VALUES ('Patient');
    PRINT '   ? Patient role created';
END
ELSE
    PRINT '   - Patient role already exists';

PRINT '';

-- ============================================================================
-- 2. ADMIN USER
-- ============================================================================
PRINT '2. Seeding Admin User...';

-- Check if admin user exists
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@axivora.com')
BEGIN
    -- Create admin user
    INSERT INTO Users (Email, PasswordHash, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (
        'admin@axivora.com',
        'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=', -- SHA256 hash of "Admin123!"
        1,
        0,
        GETDATE(),
        GETDATE()
    );

    DECLARE @AdminUserId INT = SCOPE_IDENTITY();
    DECLARE @AdminRoleId INT;
    SELECT @AdminRoleId = RoleId FROM Roles WHERE RoleName = 'Admin';

    -- Assign admin role
    INSERT INTO UserRoles (UserId, RoleId)
    VALUES (@AdminUserId, @AdminRoleId);

    PRINT '   ? Admin user created (Email: admin@axivora.com, Password: Admin123!)';
END
ELSE
    PRINT '   - Admin user already exists';

PRINT '';

-- ============================================================================
-- 3. DEPARTMENTS
-- ============================================================================
PRINT '3. Seeding Departments...';

-- Medical Departments
DECLARE @DepartmentData TABLE (DepartmentName NVARCHAR(100));

INSERT INTO @DepartmentData (DepartmentName) VALUES
    ('Cardiology'),
    ('Neurology'),
    ('Orthopedics'),
    ('Pediatrics'),
    ('Obstetrics and Gynecology'),
    ('General Surgery'),
    ('Internal Medicine'),
    ('Dermatology'),
    ('Ophthalmology'),
    ('ENT (Ear, Nose, Throat)'),
    ('Psychiatry'),
    ('Radiology'),
    ('Anesthesiology'),
    ('Emergency Medicine'),
    ('Family Medicine'),
    ('Oncology'),
    ('Urology'),
    ('Nephrology'),
    ('Gastroenterology'),
    ('Pulmonology'),
    ('Endocrinology'),
    ('Rheumatology'),
    ('Hematology'),
    ('Infectious Disease'),
    ('Physical Medicine and Rehabilitation');

DECLARE @DeptCount INT = 0;
DECLARE @DeptName NVARCHAR(100);
DECLARE @TotalDepts INT;

DECLARE dept_cursor CURSOR FOR SELECT DepartmentName FROM @DepartmentData;
OPEN dept_cursor;
FETCH NEXT FROM dept_cursor INTO @DeptName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Departments WHERE DepartmentName = @DeptName)
    BEGIN
        INSERT INTO Departments (DepartmentName) VALUES (@DeptName);
        SET @DeptCount = @DeptCount + 1;
    END
    FETCH NEXT FROM dept_cursor INTO @DeptName;
END

CLOSE dept_cursor;
DEALLOCATE dept_cursor;

SELECT @TotalDepts = COUNT(*) FROM Departments;

PRINT '   ? ' + CAST(@DeptCount AS VARCHAR(10)) + ' departments created';
PRINT '   ? Total departments: ' + CAST(@TotalDepts AS VARCHAR(10));
PRINT '';

-- ============================================================================
-- 4. APPOINTMENT STATUSES
-- ============================================================================
PRINT '4. Seeding Appointment Statuses...';

DECLARE @AppointmentStatusData TABLE (StatusName NVARCHAR(50));

INSERT INTO @AppointmentStatusData (StatusName) VALUES
    ('Scheduled'),
    ('Confirmed'),
    ('Checked-In'),
    ('In Progress'),
    ('Completed'),
    ('Cancelled'),
    ('No-Show'),
    ('Rescheduled');

DECLARE @StatusCount INT = 0;
DECLARE @StatusName NVARCHAR(50);
DECLARE @TotalStatuses INT;

DECLARE status_cursor CURSOR FOR SELECT StatusName FROM @AppointmentStatusData;
OPEN status_cursor;
FETCH NEXT FROM status_cursor INTO @StatusName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM AppointmentStatus WHERE StatusName = @StatusName)
    BEGIN
        INSERT INTO AppointmentStatus (StatusName) VALUES (@StatusName);
        SET @StatusCount = @StatusCount + 1;
    END
    FETCH NEXT FROM status_cursor INTO @StatusName;
END

CLOSE status_cursor;
DEALLOCATE status_cursor;

SELECT @TotalStatuses = COUNT(*) FROM AppointmentStatus;

PRINT '   ? ' + CAST(@StatusCount AS VARCHAR(10)) + ' appointment statuses created';
PRINT '   ? Total statuses: ' + CAST(@TotalStatuses AS VARCHAR(10));
PRINT '';

-- ============================================================================
-- 5. ICD-10 CODES (Common Conditions)
-- ============================================================================
PRINT '5. Seeding ICD-10 Codes (Sample Common Conditions)...';

DECLARE @ICDCodeData TABLE (Code NVARCHAR(10), Description NVARCHAR(500));

INSERT INTO @ICDCodeData (Code, Description) VALUES
    -- Common Chronic Diseases
    ('I10', 'Essential (primary) hypertension'),
    ('E11.9', 'Type 2 diabetes mellitus without complications'),
    ('E78.5', 'Hyperlipidemia, unspecified'),
    ('J45.909', 'Unspecified asthma, uncomplicated'),
    ('I25.10', 'Atherosclerotic heart disease of native coronary artery without angina pectoris'),
    
    -- Acute Conditions
    ('J00', 'Acute nasopharyngitis [common cold]'),
    ('J06.9', 'Acute upper respiratory infection, unspecified'),
    ('J20.9', 'Acute bronchitis, unspecified'),
    ('A09', 'Infectious gastroenteritis and colitis, unspecified'),
    ('K21.9', 'Gastro-esophageal reflux disease without esophagitis'),
    
    -- Musculoskeletal
    ('M54.5', 'Low back pain'),
    ('M79.3', 'Panniculitis, unspecified'),
    ('M25.50', 'Pain in unspecified joint'),
    ('M19.90', 'Unspecified osteoarthritis, unspecified site'),
    
    -- Mental Health
    ('F41.9', 'Anxiety disorder, unspecified'),
    ('F32.9', 'Major depressive disorder, single episode, unspecified'),
    ('F33.9', 'Major depressive disorder, recurrent, unspecified'),
    
    -- Infectious Diseases
    ('A41.9', 'Sepsis, unspecified organism'),
    ('J18.9', 'Pneumonia, unspecified organism'),
    ('N39.0', 'Urinary tract infection, site not specified'),
    
    -- Cardiovascular
    ('I63.9', 'Cerebral infarction, unspecified'),
    ('I50.9', 'Heart failure, unspecified'),
    ('I48.91', 'Unspecified atrial fibrillation'),
    
    -- Endocrine
    ('E05.90', 'Thyrotoxicosis, unspecified without thyrotoxic crisis'),
    ('E03.9', 'Hypothyroidism, unspecified'),
    
    -- Respiratory
    ('J44.9', 'Chronic obstructive pulmonary disease, unspecified'),
    ('J96.00', 'Acute respiratory failure, unspecified whether with hypoxia or hypercapnia'),
    
    -- Gastrointestinal
    ('K29.70', 'Gastritis, unspecified, without bleeding'),
    ('K58.9', 'Irritable bowel syndrome without diarrhea'),
    ('K80.20', 'Calculus of gallbladder without cholecystitis without obstruction'),
    
    -- Renal
    ('N18.9', 'Chronic kidney disease, unspecified'),
    ('N17.9', 'Acute kidney failure, unspecified'),
    
    -- Dermatology
    ('L50.9', 'Urticaria, unspecified'),
    ('L70.0', 'Acne vulgaris'),
    ('L30.9', 'Dermatitis, unspecified'),
    
    -- Pediatric
    ('J03.90', 'Acute tonsillitis, unspecified'),
    ('A37.90', 'Whooping cough, unspecified species without pneumonia'),
    
    -- Obstetrics
    ('O80', 'Encounter for full-term uncomplicated delivery'),
    ('Z34.90', 'Encounter for supervision of normal pregnancy, unspecified, unspecified trimester'),
    
    -- General
    ('R50.9', 'Fever, unspecified'),
    ('R51.9', 'Headache, unspecified'),
    ('R05.9', 'Cough, unspecified'),
    ('R06.02', 'Shortness of breath'),
    ('R10.9', 'Unspecified abdominal pain'),
    ('R53.83', 'Other fatigue'),
    ('R07.9', 'Chest pain, unspecified'),
    ('N94.6', 'Dysmenorrhea, unspecified'),
    ('R42', 'Dizziness and giddiness'),
    ('R11.0', 'Nausea'),
    ('R19.7', 'Diarrhea, unspecified');

DECLARE @ICDCount INT = 0;
DECLARE @ICDCode NVARCHAR(10);
DECLARE @ICDDesc NVARCHAR(500);
DECLARE @TotalICDs INT;

DECLARE icd_cursor CURSOR FOR SELECT Code, Description FROM @ICDCodeData;
OPEN icd_cursor;
FETCH NEXT FROM icd_cursor INTO @ICDCode, @ICDDesc;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM ICDCodes WHERE Code = @ICDCode)
    BEGIN
        INSERT INTO ICDCodes (Code, Description) VALUES (@ICDCode, @ICDDesc);
        SET @ICDCount = @ICDCount + 1;
    END
    FETCH NEXT FROM icd_cursor INTO @ICDCode, @ICDDesc;
END

CLOSE icd_cursor;
DEALLOCATE icd_cursor;

SELECT @TotalICDs = COUNT(*) FROM ICDCodes;

PRINT '   ? ' + CAST(@ICDCount AS VARCHAR(10)) + ' ICD-10 codes created';
PRINT '   ? Total ICD codes: ' + CAST(@TotalICDs AS VARCHAR(10));
PRINT '';

-- ============================================================================
-- 6. MEDICINES (Common Medications)
-- ============================================================================
PRINT '6. Seeding Medicines (Common Medications)...';

DECLARE @MedicineData TABLE (MedicineName NVARCHAR(150));

INSERT INTO @MedicineData (MedicineName) VALUES
    -- Antibiotics
    ('Amoxicillin 500mg'),
    ('Azithromycin 250mg'),
    ('Ciprofloxacin 500mg'),
    ('Cephalexin 500mg'),
    ('Metronidazole 400mg'),
    ('Doxycycline 100mg'),
    
    -- Pain Relief & Anti-inflammatory
    ('Paracetamol 500mg'),
    ('Ibuprofen 400mg'),
    ('Diclofenac 50mg'),
    ('Naproxen 250mg'),
    ('Aspirin 75mg'),
    ('Tramadol 50mg'),
    
    -- Antihypertensives
    ('Amlodipine 5mg'),
    ('Losartan 50mg'),
    ('Atenolol 50mg'),
    ('Ramipril 5mg'),
    ('Hydrochlorothiazide 25mg'),
    
    -- Diabetes Medications
    ('Metformin 500mg'),
    ('Glimepiride 2mg'),
    ('Insulin Glargine 100 units/mL'),
    ('Sitagliptin 100mg'),
    
    -- Cardiovascular
    ('Atorvastatin 20mg'),
    ('Rosuvastatin 10mg'),
    ('Clopidogrel 75mg'),
    ('Digoxin 0.25mg'),
    
    -- Respiratory
    ('Salbutamol Inhaler 100mcg'),
    ('Budesonide Inhaler 200mcg'),
    ('Montelukast 10mg'),
    ('Cetirizine 10mg'),
    ('Loratadine 10mg'),
    
    -- Gastrointestinal
    ('Omeprazole 20mg'),
    ('Ranitidine 150mg'),
    ('Domperidone 10mg'),
    ('Loperamide 2mg'),
    ('Metoclopramide 10mg'),
    ('Pantoprazole 40mg'),
    
    -- Mental Health
    ('Sertraline 50mg'),
    ('Fluoxetine 20mg'),
    ('Escitalopram 10mg'),
    ('Alprazolam 0.5mg'),
    ('Lorazepam 1mg'),
    ('Quetiapine 25mg'),
    
    -- Thyroid
    ('Levothyroxine 50mcg'),
    ('Carbimazole 5mg'),
    
    -- Vitamins & Supplements
    ('Vitamin D3 1000 IU'),
    ('Calcium Carbonate 500mg'),
    ('Folic Acid 5mg'),
    ('Vitamin B Complex'),
    ('Iron (Ferrous Sulfate) 200mg'),
    ('Multivitamin'),
    
    -- Eye Drops
    ('Timolol Eye Drops 0.5%'),
    ('Latanoprost Eye Drops'),
    ('Artificial Tears'),
    
    -- Other Common Medications
    ('Prednisolone 5mg'),
    ('Furosemide 40mg'),
    ('Warfarin 5mg'),
    ('Simvastatin 20mg');

DECLARE @MedCount INT = 0;
DECLARE @MedName NVARCHAR(150);
DECLARE @TotalMeds INT;

DECLARE med_cursor CURSOR FOR SELECT MedicineName FROM @MedicineData;
OPEN med_cursor;
FETCH NEXT FROM med_cursor INTO @MedName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Medicines WHERE MedicineName = @MedName)
    BEGIN
        INSERT INTO Medicines (MedicineName) VALUES (@MedName);
        SET @MedCount = @MedCount + 1;
    END
    FETCH NEXT FROM med_cursor INTO @MedName;
END

CLOSE med_cursor;
DEALLOCATE med_cursor;

SELECT @TotalMeds = COUNT(*) FROM Medicines;

PRINT '   ? ' + CAST(@MedCount AS VARCHAR(10)) + ' medicines created';
PRINT '   ? Total medicines: ' + CAST(@TotalMeds AS VARCHAR(10));
PRINT '';

-- ============================================================================
-- 7. LAB TESTS (Common Laboratory Tests)
-- ============================================================================
PRINT '7. Seeding Lab Tests...';

DECLARE @LabTestData TABLE (TestName NVARCHAR(150));

INSERT INTO @LabTestData (TestName) VALUES
    -- Hematology
    ('Complete Blood Count (CBC)'),
    ('Hemoglobin (Hb)'),
    ('Platelet Count'),
    ('White Blood Cell Count (WBC)'),
    ('Erythrocyte Sedimentation Rate (ESR)'),
    ('Prothrombin Time (PT)'),
    ('Activated Partial Thromboplastin Time (APTT)'),
    ('Blood Group & Rh Typing'),
    
    -- Biochemistry
    ('Fasting Blood Sugar (FBS)'),
    ('Random Blood Sugar (RBS)'),
    ('HbA1c (Glycated Hemoglobin)'),
    ('Lipid Profile'),
    ('Liver Function Test (LFT)'),
    ('Kidney Function Test (KFT)'),
    ('Serum Creatinine'),
    ('Blood Urea Nitrogen (BUN)'),
    ('Serum Electrolytes (Na, K, Cl)'),
    ('Serum Calcium'),
    ('Serum Uric Acid'),
    ('Serum Bilirubin'),
    ('Serum Albumin'),
    ('Serum Protein'),
    ('Alkaline Phosphatase (ALP)'),
    ('SGOT (AST)'),
    ('SGPT (ALT)'),
    
    -- Hormones
    ('Thyroid Function Test (TFT)'),
    ('TSH (Thyroid Stimulating Hormone)'),
    ('Free T3'),
    ('Free T4'),
    ('Vitamin D (25-OH)'),
    ('Vitamin B12'),
    ('Serum Ferritin'),
    ('Testosterone'),
    ('Prolactin'),
    
    -- Cardiac Markers
    ('Troponin I/T'),
    ('CPK-MB'),
    ('NT-proBNP'),
    ('D-Dimer'),
    
    -- Urine Tests
    ('Urine Routine & Microscopy'),
    ('Urine Culture & Sensitivity'),
    ('24-Hour Urine Protein'),
    ('Microalbuminuria'),
    
    -- Microbiology
    ('Blood Culture & Sensitivity'),
    ('Sputum Culture'),
    ('Stool Routine & Microscopy'),
    ('Widal Test'),
    ('Malaria Antigen Test'),
    ('Dengue NS1 Antigen'),
    ('COVID-19 RT-PCR'),
    ('COVID-19 Rapid Antigen Test'),
    
    -- Immunology
    ('C-Reactive Protein (CRP)'),
    ('Rheumatoid Factor (RF)'),
    ('Anti-CCP Antibodies'),
    ('Antinuclear Antibodies (ANA)'),
    ('HIV 1 & 2 Antibodies'),
    ('Hepatitis B Surface Antigen (HBsAg)'),
    ('Hepatitis C Antibody (Anti-HCV)'),
    ('VDRL Test (Syphilis)'),
    
    -- Tumor Markers
    ('PSA (Prostate Specific Antigen)'),
    ('CEA (Carcinoembryonic Antigen)'),
    ('CA 19-9'),
    ('CA 125'),
    ('AFP (Alpha Fetoprotein)'),
    
    -- Other Tests
    ('Pregnancy Test (Urine/Blood)'),
    ('Stool Occult Blood Test'),
    ('Glucose Tolerance Test (GTT)'),
    ('Arterial Blood Gas (ABG)');

DECLARE @LabCount INT = 0;
DECLARE @LabName NVARCHAR(150);
DECLARE @TotalLabs INT;

DECLARE lab_cursor CURSOR FOR SELECT TestName FROM @LabTestData;
OPEN lab_cursor;
FETCH NEXT FROM lab_cursor INTO @LabName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM LabTests WHERE TestName = @LabName)
    BEGIN
        INSERT INTO LabTests (TestName) VALUES (@LabName);
        SET @LabCount = @LabCount + 1;
    END
    FETCH NEXT FROM lab_cursor INTO @LabName;
END

CLOSE lab_cursor;
DEALLOCATE lab_cursor;

SELECT @TotalLabs = COUNT(*) FROM LabTests;

PRINT '   ? ' + CAST(@LabCount AS VARCHAR(10)) + ' lab tests created';
PRINT '   ? Total lab tests: ' + CAST(@TotalLabs AS VARCHAR(10));
PRINT '';

-- ============================================================================
-- SUMMARY
-- ============================================================================
DECLARE @TotalRoles INT, @TotalAdmins INT;

SELECT @TotalRoles = COUNT(*) FROM Roles;
SELECT @TotalAdmins = COUNT(*) FROM Users WHERE Email LIKE '%admin%';
SELECT @TotalDepts = COUNT(*) FROM Departments;
SELECT @TotalStatuses = COUNT(*) FROM AppointmentStatus;
SELECT @TotalICDs = COUNT(*) FROM ICDCodes;
SELECT @TotalMeds = COUNT(*) FROM Medicines;
SELECT @TotalLabs = COUNT(*) FROM LabTests;

PRINT '';
PRINT '=========================================';
PRINT 'Seed Data Import Completed Successfully!';
PRINT '=========================================';
PRINT '';
PRINT 'Summary:';
PRINT '--------';
PRINT 'Roles:               ' + CAST(@TotalRoles AS VARCHAR(10));
PRINT 'Admin Users:         ' + CAST(@TotalAdmins AS VARCHAR(10));
PRINT 'Departments:         ' + CAST(@TotalDepts AS VARCHAR(10));
PRINT 'Appointment Statuses:' + CAST(@TotalStatuses AS VARCHAR(10));
PRINT 'ICD-10 Codes:        ' + CAST(@TotalICDs AS VARCHAR(10));
PRINT 'Medicines:           ' + CAST(@TotalMeds AS VARCHAR(10));
PRINT 'Lab Tests:           ' + CAST(@TotalLabs AS VARCHAR(10));
PRINT '';
PRINT 'Admin Credentials:';
PRINT '------------------';
PRINT 'Email:    admin@axivora.com';
PRINT 'Password: Admin123!';
PRINT '';
PRINT 'NOTE: Please change the admin password after first login!';
PRINT '';

SET NOCOUNT OFF;
GO
