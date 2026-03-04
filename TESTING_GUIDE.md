# ?? Quick Testing Guide - JWT Authentication

## Prerequisites
- API running on `https://localhost:5001`
- Postman or similar HTTP client
- User registered in the system

---

## ?? 1. Register a New User

### Patient Registration
```http
POST https://localhost:5001/api/auth/register
Content-Type: application/json

{
  "email": "patient@test.com",
  "password": "Patient123!",
  "role": "Patient"
}
```

### Admin Registration
```http
POST https://localhost:5001/api/auth/register
Content-Type: application/json

{
  "email": "admin@test.com",
  "password": "Admin123!",
  "role": "Admin"
}
```

### Doctor Registration
```http
POST https://localhost:5001/api/auth/register
Content-Type: application/json

{
  "email": "doctor@test.com",
  "password": "Doctor123!",
  "role": "Doctor"
}
```

**Expected Response:**
```json
{
  "userId": 1,
  "email": "patient@test.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJwYXRpZW50QHRlc3QuY29tIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiUGF0aWVudCIsInN1YiI6IjEiLCJlbWFpbCI6InBhdGllbnRAdGVzdC5jb20iLCJqdGkiOiJhNzhlZjg5Zi1jOGE1LTQ4MjctYWE4Ni1jZTUwZmIyNGM4YzkiLCJleHAiOjE3MDk0NjE4MjcsImlzcyI6IkF4aXZvcmFITVMiLCJhdWQiOiJBeGl2b3JhSE1TLVVzZXJzIn0.xyz123...",
  "role": "Patient",
  "emailVerified": false,
  "profileCompleted": false
}
```

**?? Save the `token` value for next steps!**

---

## ?? 2. Login (If Already Registered)

```http
POST https://localhost:5001/api/auth/login
Content-Type: application/json

{
  "email": "patient@test.com",
  "password": "Patient123!"
}
```

**Response:** Same as registration

---

## 3?? Complete Patient Profile

**?? Important:** Add the token to Authorization header!

```http
POST https://localhost:5001/api/patients/profile
Authorization: Bearer YOUR_TOKEN_HERE
Content-Type: application/json

{
  "fullName": "John Doe",
  "dateOfBirth": "1990-01-15",
  "gender": "Male",
  "phoneNumber": "1234567890",
  "bloodGroup": "O+",
  "emergencyContact": "9876543210",
  "address": {
    "addressLine1": "123 Main Street",
    "addressLine2": "Apt 4B",
    "city": "New York",
    "state": "NY",
    "postalCode": "10001",
    "country": "USA"
  }
}
```

---

## 4?? Get My Profile

```http
GET https://localhost:5001/api/patients/me
Authorization: Bearer YOUR_TOKEN_HERE
```

---

## ?? Testing Authorization

### Test 1: Patient tries to access Admin endpoint
```http
GET https://localhost:5001/api/patients
Authorization: Bearer PATIENT_TOKEN_HERE
```
**Expected:** `403 Forbidden`

### Test 2: Admin accesses Admin endpoint
```http
GET https://localhost:5001/api/patients
Authorization: Bearer ADMIN_TOKEN_HERE
```
**Expected:** `200 OK` with list of patients

### Test 3: Access without token
```http
GET https://localhost:5001/api/patients/me
```
**Expected:** `401 Unauthorized`

### Test 4: Access with invalid token
```http
GET https://localhost:5001/api/patients/me
Authorization: Bearer invalid_token
```
**Expected:** `401 Unauthorized`

---

## ?? Postman Setup

### Step 1: Save Token as Variable
1. After login/register, copy the token
2. Go to Postman Environment Variables
3. Create variable `jwt_token` = `YOUR_TOKEN`

### Step 2: Use Token in Requests
1. Go to "Authorization" tab
2. Select "Bearer Token"
3. Enter: `{{jwt_token}}`

### Step 3: Auto-save Token from Response
In login request, add to "Tests" tab:
```javascript
var jsonData = pm.response.json();
pm.environment.set("jwt_token", jsonData.token);
```

---

## ?? Common Endpoints to Test

### Patient Role:
```
? POST /api/patients/profile (complete profile)
? GET /api/patients/me (get own profile)
? PUT /api/patients/{id} (update own profile)
? GET /api/doctors (view doctors)
? POST /api/appointments (book appointment)
? GET /api/patients (admin only)
? POST /api/patients (admin only)
```

### Doctor Role:
```
? GET /api/patients/search (search patients)
? GET /api/appointments/doctor/{id} (view own appointments)
? POST /api/consultations (create consultation)
? POST /api/consultations/{id}/prescriptions (add prescription)
? GET /api/patients (admin only)
? DELETE /api/patients/{id} (admin only)
```

### Admin Role:
```
? Everything (full access)
? GET /api/patients
? POST /api/patients
? DELETE /api/patients/{id}
? POST /api/doctors
? DELETE /api/doctors/{id}
```

---

## ?? Debugging

### Token Not Working?
1. Check token format: `Bearer {token}`
2. Verify token not expired (60 minutes)
3. Check role matches endpoint requirements
4. Verify Authorization header is included

### Getting 401 Unauthorized?
- Token missing or invalid
- Token expired
- Wrong Authorization header format

### Getting 403 Forbidden?
- Token valid but user lacks permission
- Check role requirements for endpoint
- Login with correct role

---

## ?? Full Test Sequence

```
1. Register Patient ? Save token
2. Complete Patient Profile ? Use token
3. Get My Profile ? Use token
4. Register Admin ? Save new token
5. Get All Patients ? Use admin token (should work)
6. Get All Patients ? Use patient token (should fail 403)
7. Wait 61 minutes ? Try request (should fail 401 - expired)
```

---

## ?? Example cURL Commands

### Register:
```bash
curl -X POST "https://localhost:5001/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Pass123!","role":"Patient"}'
```

### Login:
```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Pass123!"}'
```

### Authenticated Request:
```bash
curl -X GET "https://localhost:5001/api/patients/me" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

---

## ? Success Checklist

- [ ] Can register new users
- [ ] Can login and receive token
- [ ] Can access protected endpoints with token
- [ ] Get 401 without token
- [ ] Get 403 with wrong role
- [ ] Patient can complete profile
- [ ] Patient can view own profile
- [ ] Admin can view all patients
- [ ] Patient cannot view all patients
- [ ] Expired token rejected

---

## ?? Common Issues

| Issue | Solution |
|-------|----------|
| "Invalid token" | Check Bearer prefix, token format |
| "Token expired" | Login again to get new token |
| "Access denied" | Check user role matches endpoint requirement |
| "Unauthorized" | Add Authorization header with Bearer token |
| Build errors | Run `dotnet restore` and `dotnet build` |

---

## ?? Happy Testing!

You now have:
- ? Secure JWT authentication
- ? Role-based authorization
- ? Token expiration
- ? No more X-User-Id vulnerability!

**Your API is secure!** ??
