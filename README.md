# Inclusive Hostel Management System — Setup Guide

## Quick Start (5 steps)

### 1. Configure the backend

Edit `Backend/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HostelManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "REPLACE_WITH_32_CHAR_MINIMUM_SECRET_KEY_STRING",
    "Issuer": "HostelMS",
    "Audience": "HostelMS-Users",
    "ExpiryMinutes": 1440
  },
  "PaystackSettings": {
    "SecretKey": "sk_test_YOUR_KEY_HERE",
    "PublicKey": "pk_test_YOUR_KEY_HERE",
    "WebhookSecret": "sk_test_YOUR_KEY_HERE"
  },
  "AllowedOrigins": ["http://localhost:5500", "http://127.0.0.1:5500"]
}
```

### 2. Run the backend

```bash
cd Backend/
dotnet restore
dotnet ef migrations add Init --output-dir Migrations
dotnet ef database update
dotnet run
```

API starts at: http://localhost:5000
Swagger UI:    http://localhost:5000/swagger

This automatically seeds one admin account:
- Email:    admin@hostel.edu.ng
- Password: Admin@12345

### 3. Serve the frontend

```bash
cd frontend/
npx serve . -p 5500
# OR: python -m http.server 5500
```

Open: http://localhost:5500/login.html

### 4. Set up Paystack webhooks (for payment → allocation flow)

For local testing, expose your API with ngrok:
```bash
ngrok http 5000
```

In your Paystack dashboard → Settings → Webhooks:
- Set URL to: https://YOUR_NGROK_ID.ngrok.io/api/webhook/paystack

Also update `Backend/Services/PaymentService.cs` line ~47:
```csharp
callback_url = "http://localhost:5500/payment.html"
```

### 5. That's it

---

## API Endpoints Reference

| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| POST | /api/auth/register | None | Register student |
| POST | /api/auth/login | None | Login |
| GET | /api/room | JWT | List rooms |
| POST | /api/room | Admin | Create room |
| PUT | /api/room/{id} | Admin | Update room |
| DELETE | /api/room/{id} | Admin | Deactivate room |
| POST | /api/payment/initiate | Student | Start Paystack payment |
| GET | /api/payment/mine | Student | My payments |
| GET | /api/payment/reference/{ref} | JWT | Get payment by reference |
| POST | /api/webhook/paystack | None | Paystack webhook |
| GET | /api/allocation/mine | Student | My allocation |
| GET | /api/allocation | Admin | All allocations |
| DELETE | /api/allocation/{id} | Admin | Remove allocation |
| GET | /api/message/conversations | JWT | List conversations |
| GET | /api/message/conversation/{id} | JWT | Get messages with user |
| POST | /api/message | JWT | Send message |

---

## Frontend Pages

| Page | URL | Who |
|------|-----|-----|
| Login | /login.html | All |
| Register | /register.html | New students |
| Student Dashboard | /student-dashboard.html | Student |
| My Hostel Status | /student-status.html | Student |
| Browse Rooms | /rooms.html | Student |
| Hostel Info | /student-info.html | Student |
| Schedule | /student-schedule.html | Student |
| Messages | /student-messages.html | Student |
| Settings | /student-settings.html | Student |
| Payment Callback | /payment.html | Student |
| Admin Dashboard | /admin-dashboard.html | Admin |
| Manage Rooms | /admin-rooms.html | Admin |
| Allocations | /admin-allocations.html | Admin |
| Announcements | /admin-announcements.html | Admin |
| Admin Messages | /admin-messages.html | Admin |
