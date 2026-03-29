# HostelMS — VS Code Setup Guide
# No Paystack keys needed. Everything works out of the box.

================================================================
WHAT YOU NEED INSTALLED
================================================================

1. VS Code              → https://code.visualstudio.com
2. .NET 8 SDK           → https://dotnet.microsoft.com/download
3. SQL Server Express   → https://www.microsoft.com/en-us/sql-server/sql-server-downloads
   (scroll down, pick "Express" - it is free)
4. VS Code Extensions:
   - Live Server        (by Ritwick Dey)     ← you already have this
   - C# Dev Kit         (by Microsoft)       ← install this too


================================================================
STEP 1 — OPEN THE PROJECT IN VS CODE
================================================================

1. Unzip the downloaded file somewhere easy, e.g.:
      C:\Projects\HostelMS-final\

2. Open VS Code

3. File → Open Folder → select the HostelMS-final folder
   (the one that contains both Backend/ and frontend/)

You should see this structure in the Explorer panel:
   📁 HostelMS-final
   ├── 📁 Backend
   │   ├── HostelMS.csproj
   │   ├── Program.cs
   │   ├── appsettings.json
   │   └── ...
   └── 📁 frontend
       ├── config.js
       ├── login.html
       └── ...


================================================================
STEP 2 — SET UP THE DATABASE
================================================================

SQL Server Express installs an instance called SQLEXPRESS.
The connection string in appsettings.json already points to it:

   Server=localhost\SQLEXPRESS;Database=HostelManagementDB;...

If your SQL Server instance has a different name, open:
   Backend/appsettings.json
and change "SQLEXPRESS" to your instance name.

You can check your instance name by opening SQL Server
Configuration Manager or SQL Server Management Studio (SSMS).

No need to create the database manually — the app creates it
automatically when it first runs.


================================================================
STEP 3 — RUN THE BACKEND
================================================================

Option A: Use the VS Code Terminal (easiest)

1. In VS Code, open a new terminal:
   Terminal → New Terminal

2. Navigate to the Backend folder:
      cd Backend

3. Restore packages:
      dotnet restore

4. Create the database (run this ONCE on first setup):
      dotnet ef migrations add InitialCreate --output-dir Migrations
      dotnet ef database update

   If you already ran migrations before (upgrading from a previous version):
      dotnet ef migrations add AddAnnouncementsAndEvents --output-dir Migrations
      dotnet ef database update

5. Start the API:
      dotnet run

   You will see output like:
      info: Now listening on: http://localhost:5000

   Leave this terminal open. The API must keep running.

Option B: Open a separate terminal window
   Navigate to the Backend folder and run: dotnet run


================================================================
STEP 4 — RUN THE FRONTEND WITH LIVE SERVER
================================================================

1. In the VS Code Explorer panel, open the frontend/ folder

2. Click on login.html to open it in the editor

3. Right-click anywhere inside the login.html file

4. Select "Open with Live Server"

   Your browser will open at:
      http://127.0.0.1:5500/frontend/login.html

   If it opens at just http://127.0.0.1:5500 (showing the
   folder), manually navigate to /frontend/login.html


================================================================
STEP 5 — LOG IN AND USE THE SYSTEM
================================================================

A default admin account is created automatically:

   Email:    admin@hostel.edu.ng
   Password: Admin@12345

Student accounts: Register using the Register page.


================================================================
FULL STUDENT FLOW (how to test it end to end)
================================================================

1. Register a student account → Register page
2. Log in as student → redirected to Student Dashboard
3. Click "Browse Rooms" in the sidebar
   → Admin must have created at least one room first (see below)
4. Click "Book Now" on a room
5. A booking modal appears — click "Pay with Paystack"
6. You are redirected to the FAKE Paystack page (paystack-mock.html)
   → Enter any card details (they are not validated for real)
      Card number: 4084 0840 8408 4081
      Name:        Any name
      Expiry:      12 / 26
      CVV:         408
7. Click Pay — it processes for 2 seconds then shows success
8. You are redirected back to payment.html
9. Your room is now allocated ✓
10. Go to My Hostel Status to see your allocation details


================================================================
ADMIN FLOW
================================================================

1. Log in as admin@hostel.edu.ng / Admin@12345
2. Go to Manage Rooms → click "+ Add Room" to create rooms
3. Go to Allocations to see all student allocations
4. Go to Messages to reply to student messages
5. Go to Announcements to post notices


================================================================
TROUBLESHOOTING
================================================================

"Unable to connect to SQL Server"
   → Make sure SQL Server Express is running
   → Open Services (Windows) and start "SQL Server (SQLEXPRESS)"
   → Or run: net start MSSQL$SQLEXPRESS  (in cmd as admin)

"dotnet: command not found"
   → Install .NET 8 SDK from https://dotnet.microsoft.com/download
   → Restart VS Code after installing

"CORS error in browser console"
   → The backend is not running
   → Open a terminal, cd to Backend/, run: dotnet run
   → Make sure it says "Now listening on http://localhost:5000"

"404 on API calls"
   → Check the URL in frontend/config.js line 5:
      const API_BASE = "http://localhost:5000/api";
   → This must match where your backend is running

"Live Server opens the wrong page"
   → Navigate manually to: http://127.0.0.1:5500/login.html

"Migrations already exist" error
   → Skip the migrations step, just run: dotnet ef database update

EF Tools not found:
   → Run: dotnet tool install --global dotnet-ef
   → Then: dotnet restore  (again)
   → Then retry the migrations commands


================================================================
FILE REFERENCE
================================================================

config.js         Shared JS — auth, API calls, layout, TTS
login.html        Sign in page
register.html     Student registration
student-dashboard.html    Student home
student-status.html       Allocation + payment history
rooms.html        Browse and book rooms (live updates via SignalR)
paystack-mock.html        Fake Paystack checkout (no real keys)
payment.html      Payment result / callback handler
student-messages.html     Student ↔ Admin chat
student-info.html         Hostel rules and announcements
student-schedule.html     Events calendar
student-settings.html     Profile + TTS toggle
admin-dashboard.html      Admin overview
admin-rooms.html          Create/edit/remove rooms
admin-allocations.html    View and manage all allocations
admin-messages.html       Reply to students
admin-announcements.html  Post hostel notices


================================================================
WANT TO DEPLOY LATER?
================================================================

Backend: publish with  dotnet publish -c Release
         host on Azure App Service, Railway, or Render

Frontend: upload the frontend/ folder to any static host
          (Netlify, Vercel, GitHub Pages)
          Update config.js line 5 to your live API URL
