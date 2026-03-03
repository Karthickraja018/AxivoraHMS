# API Endpoints Reference

## Authentication Endpoints

### 1. Register User
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "password": "SecureP@ss123",
  "confirmPassword": "SecureP@ss123",
  "role": "Patient"
}

Response 201:
{
  "userId": 1,
  "email": "john.doe@example.com",
  "token": "temporary-token-here",
  "role": "Patient",
  "emailVerified": false,
  "profileCompleted": false
}
```

### 2. Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "password": "SecureP@ss123"
}

Response 200:
{
  "userId": 1,
  "email": "john.doe@example.com",
  "token": "temporary-token-here",
  "role": "Patient",
  "emailVerified": true,
  "profileCompleted": true
}
```

### 3. Verify Email
```http
POST /api/auth/verify-email?email=john.doe@example.com&code=123456

Response 200:
{
  "message": "Email verified successfully"
}
```

### 4. Forgot Password
```http
POST /api/auth/forgot-password
Content-Type: application/json

{
  "email": "john.doe@example.com"
}

Response 200:
{
  "message": "Password reset link sent to your email"
}
```

### 5. Reset Password
```http
POST /api/auth/reset-password
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "resetToken": "reset-token-from-email",
  "newPassword": "NewP@ss123",
  "confirmPassword": "NewP@ss123"
}

Response 200:
{
  "message": "Password reset successfully"
}
```

---

## Patient Endpoints

### 6. Complete Patient Profile (After Registration)
```http
POST /api/patients/profile
Content-Type: application/json
X-User-Id: 1  # TEMPORARY - will be replaced with JWT token

{
  "fullName": "John Doe",
  "dateOfBirth": "1990-05-15",
  "gender": "Male",
  "phoneNumber": "+1234567890",
  "bloodGroup": "O+",
  "emergencyContact": "+0987654321",
  "address": {
    "addressLine1": "123 Main St",
    "addressLine2": "Apt 4B",
    "city": "New York",
    "state": "NY",
    "postalCode": "10001",
    "country": "USA"
  }
}

Response 201:
{
  "patientId": 1,
  "mrn": "MRN20260303000001",
  "fullName": "John Doe",
  "dateOfBirth": "1990-05-15",
  "gender": "Male",
  "phoneNumber": "+1234567890",
  "bloodGroup": "O+",
  "emergencyContact": "+0987654321",
  "address": {
    "addressId": 1,
    "addressLine1": "123 Main St",
    "addressLine2": "Apt 4B",
    "city": "New York",
    "state": "NY",
    "postalCode": "10001",
    "country": "USA"
  },
  "allergies": []
}
```

### 7. Create Patient (Admin Only)
```http
POST /api/patients/admin
Content-Type: application/json

{
  "email": "patient@example.com",
  "password": "TempP@ss123",
  "fullName": "Jane Smith",
  "dateOfBirth": "1985-03-20",
  "gender": "Female",
  "phoneNumber": "+1234567890",
  "bloodGroup": "A+",
  "emergencyContact": "+0987654321",
  "address": {
    "addressLine1": "456 Oak Ave",
    "city": "Los Angeles",
    "state": "CA",
    "postalCode": "90001",
    "country": "USA"
  }
}

Response 201: (Same as above)
```

### 8. Get All Patients
```http
GET /api/patients

Response 200:
[
  {
    "patientId": 1,
    "mrn": "MRN20260303000001",
    "fullName": "John Doe",
    ...
  }
]
```

### 9. Get Patient by ID
```http
GET /api/patients/1

Response 200:
{
  "patientId": 1,
  "mrn": "MRN20260303000001",
  ...
}
```

### 10. Get Patient by MRN
```http
GET /api/patients/mrn/MRN20260303000001

Response 200:
{
  "patientId": 1,
  "mrn": "MRN20260303000001",
  ...
}
```

### 11. Search Patients
```http
GET /api/patients/search?searchTerm=John

Response 200:
[
  {
    "patientId": 1,
    "fullName": "John Doe",
    ...
  }
]
```

### 12. Update Patient
```http
PUT /api/patients/1
Content-Type: application/json

{
  "fullName": "John Smith",
  "phoneNumber": "+1987654321",
  "bloodGroup": "O+",
  "emergencyContact": "+1234567890",
  "addressId": 2
}

Response 200: (Updated patient object)
```

### 13. Delete Patient (Soft Delete)
```http
DELETE /api/patients/1

Response 204: No Content
```

---

## Production-Ready User Flow

### New Patient Registration Flow

**Step 1: User registers**
```
POST /api/auth/register
?
Receives: userId, token, emailVerified=false, profileCompleted=false
```

**Step 2: User verifies email (optional)**
```
POST /api/auth/verify-email?email=...&code=...
?
Email verified
```

**Step 3: User completes profile**
```
POST /api/patients/profile
Headers: X-User-Id: {userId}  (temporary - will use JWT)
?
Receives: patientId, MRN, full profile
```

**Step 4: User is ready to use the system**
- Book appointments
- View medical records
- Access test results

---

## Authentication Notes

### Current Implementation (Development)
- ?? **Temporary token**: Base64 encoded string (INSECURE)
- ?? **SHA256 password hashing**: Not recommended for production
- ?? **X-User-Id header**: Temporary authentication bypass

### TODO for Production
1. ? Install `BCrypt.Net-Next` for password hashing
2. ? Install `Microsoft.AspNetCore.Authentication.JwtBearer` for JWT
3. ? Implement proper JWT token generation with secret key
4. ? Add `[Authorize]` attributes to protected endpoints
5. ? Implement email verification service
6. ? Implement password reset with expiring tokens
7. ? Add refresh token support
8. ? Add rate limiting on auth endpoints

---

## Error Responses

All endpoints return consistent error formats:

```json
// 400 Bad Request
{
  "message": "Validation error message"
}

// 401 Unauthorized
{
  "message": "Invalid email or password"
}

// 404 Not Found
{
  "message": "Patient with ID 123 not found"
}

// 500 Internal Server Error
{
  "message": "An error occurred",
  "details": "Stack trace (only in Development)"
}
```

---

## Testing with Swagger

1. Start the application
2. Navigate to `https://localhost:7087/swagger`
3. Test endpoints in this order:
   - Register ? Login ? Complete Profile
   - Or use Admin endpoint to create patient directly

---

## Security Checklist

- [ ] Replace SHA256 with BCrypt for passwords
- [ ] Implement proper JWT tokens
- [ ] Add [Authorize] attributes
- [ ] Enable HTTPS only
- [ ] Add rate limiting
- [ ] Implement CORS policy
- [ ] Add request logging
- [ ] Enable data encryption for sensitive fields
- [ ] Implement audit logging
- [ ] Add input sanitization
- [ ] Enable SQL injection protection (EF Core handles this)
