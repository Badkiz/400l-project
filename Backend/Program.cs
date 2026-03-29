using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using HostelMS.Config;
using HostelMS.Data;
using HostelMS.Hubs;
using HostelMS.Interfaces;
using HostelMS.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────
var jwtSettings      = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
var paystackSettings = builder.Configuration.GetSection("PaystackSettings").Get<PaystackSettings>()!;

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(paystackSettings);

// ── Database ──────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT Authentication ────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidAudience            = jwtSettings.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
        // Allow SignalR to authenticate via query string token
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── CORS — allow all origins (required for Vercel / Live Server) ──
builder.Services.AddCors(o =>
    o.AddPolicy("AllowAll", p =>
        p.SetIsOriginAllowed(_ => true)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

// ── Services ──────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,         AuthService>();
builder.Services.AddScoped<IRoomService,          RoomService>();
builder.Services.AddScoped<IAllocationService,    AllocationService>();
builder.Services.AddScoped<IPaymentService,       PaymentService>();
builder.Services.AddScoped<IMessageService,       MessageService>();
builder.Services.AddScoped<IAnnouncementService,  AnnouncementService>();
builder.Services.AddScoped<IEventService,         EventService>();
builder.Services.AddScoped<IProfileService,       ProfileService>();

// ── SignalR ───────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Controllers + Swagger ─────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hostel MS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header, Name = "Authorization",
        Type = SecuritySchemeType.ApiKey, Scheme = "Bearer",
        Description = "Enter: Bearer {your_token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
        Array.Empty<string>()
    }});
});

var app = builder.Build();

// ── Migrate + seed ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    db.Database.Migrate();

    // ── Admin ─────────────────────────────────────────────
    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        db.Users.Add(new HostelMS.Models.User
        {
            FullName     = "System Administrator",
            Email        = "admin@hostel.edu.ng",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@12345"),
            Role         = "Admin",
            CreatedAt    = DateTime.UtcNow
        });
        db.SaveChanges();
        logger.LogInformation("Admin seeded: admin@hostel.edu.ng / Admin@12345");
    }

    var admin = db.Users.First(u => u.Role == "Admin");
    var now   = DateTime.UtcNow;

    // ── Rooms ─────────────────────────────────────────────
    if (!db.Rooms.Any())
    {
        logger.LogInformation("Seeding rooms...");
        db.Rooms.AddRange(new HostelMS.Models.Room[]
        {
            // Block A – Female
            new(){RoomNumber="A101",HostelBlock="Block A",RoomType="Single",Capacity=1,Price=85000,Description="Ground floor, near common room",IsActive=true,CreatedAt=now},
            new(){RoomNumber="A102",HostelBlock="Block A",RoomType="Single",Capacity=1,Price=85000,Description="Ground floor, quiet end",IsActive=true,CreatedAt=now},
            new(){RoomNumber="A103",HostelBlock="Block A",RoomType="Single",Capacity=1,Price=85000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="A201",HostelBlock="Block A",RoomType="Double",Capacity=2,Price=65000,Description="Second floor, spacious with balcony",IsActive=true,CreatedAt=now},
            new(){RoomNumber="A202",HostelBlock="Block A",RoomType="Double",Capacity=2,Price=65000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="A203",HostelBlock="Block A",RoomType="Double",Capacity=2,Price=65000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="A301",HostelBlock="Block A",RoomType="Quad",Capacity=4,Price=45000,Description="Top floor, shared study space",IsActive=true,CreatedAt=now},
            new(){RoomNumber="A302",HostelBlock="Block A",RoomType="Quad",Capacity=4,Price=45000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="A001",HostelBlock="Block A",RoomType="Single",Capacity=1,Price=85000,Description="Under renovation — unavailable",IsActive=false,CreatedAt=now},
            new(){RoomNumber="A002",HostelBlock="Block A",RoomType="Double",Capacity=2,Price=65000,Description="Structural assessment pending",IsActive=false,CreatedAt=now},
            // Block B – Male
            new(){RoomNumber="B101",HostelBlock="Block B",RoomType="Single",Capacity=1,Price=85000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="B102",HostelBlock="Block B",RoomType="Single",Capacity=1,Price=85000,Description="Near study room and gym",IsActive=true,CreatedAt=now},
            new(){RoomNumber="B103",HostelBlock="Block B",RoomType="Single",Capacity=1,Price=85000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="B201",HostelBlock="Block B",RoomType="Double",Capacity=2,Price=65000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="B202",HostelBlock="Block B",RoomType="Double",Capacity=2,Price=65000,Description="Corner room, excellent ventilation",IsActive=true,CreatedAt=now},
            new(){RoomNumber="B203",HostelBlock="Block B",RoomType="Double",Capacity=2,Price=65000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="B301",HostelBlock="Block B",RoomType="Quad",Capacity=4,Price=45000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="B302",HostelBlock="Block B",RoomType="Quad",Capacity=4,Price=45000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="B001",HostelBlock="Block B",RoomType="Double",Capacity=2,Price=65000,Description="Plumbing works in progress",IsActive=false,CreatedAt=now},
            // Block C – Postgraduate
            new(){RoomNumber="C101",HostelBlock="Block C",RoomType="Single",Capacity=1,Price=110000,Description="PG single — en-suite bathroom",IsActive=true,CreatedAt=now},
            new(){RoomNumber="C102",HostelBlock="Block C",RoomType="Single",Capacity=1,Price=110000,Description="PG single — en-suite bathroom",IsActive=true,CreatedAt=now},
            new(){RoomNumber="C103",HostelBlock="Block C",RoomType="Single",Capacity=1,Price=110000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="C201",HostelBlock="Block C",RoomType="Double",Capacity=2,Price=88000,IsActive=true,CreatedAt=now},
            new(){RoomNumber="C202",HostelBlock="Block C",RoomType="Double",Capacity=2,Price=88000,Description="Corner suite with dedicated study desk",IsActive=true,CreatedAt=now},
            new(){RoomNumber="C001",HostelBlock="Block C",RoomType="Single",Capacity=1,Price=110000,Description="Awaiting electrical certification",IsActive=false,CreatedAt=now},
        });
        db.SaveChanges();
        logger.LogInformation("Rooms seeded.");
    }

    // ── Students ──────────────────────────────────────────
    if (!db.Users.Any(u => u.Role == "Student"))
    {
        logger.LogInformation("Seeding 50 students...");
        var names = new (string full, string matric, string phone)[]
        {
            ("Amaka Okafor",       "CSC/2021/001", "08012345601"),
            ("Chukwuemeka Eze",    "CSC/2021/002", "08012345602"),
            ("Fatima Bello",       "CSC/2021/003", "08012345603"),
            ("Oluwaseun Adeyemi",  "CSC/2021/004", "08012345604"),
            ("Ibrahim Musa",       "CSC/2021/005", "08012345605"),
            ("Ngozi Okeke",        "CSC/2021/006", "08012345606"),
            ("Tunde Fashola",      "CSC/2021/007", "08012345607"),
            ("Aisha Mohammed",     "CSC/2021/008", "08012345608"),
            ("Chidinma Nwachukwu", "CSC/2021/009", "08012345609"),
            ("Emeka Obi",          "CSC/2021/010", "08012345610"),
            ("Yetunde Afolabi",    "CSC/2021/011", "08012345611"),
            ("Suleiman Aliyu",     "CSC/2021/012", "08012345612"),
            ("Blessing Nnaji",     "CSC/2021/013", "08012345613"),
            ("Kemi Adebayo",       "CSC/2021/014", "08012345614"),
            ("Uche Nwosu",         "CSC/2021/015", "08012345615"),
            ("Remi Ogundimu",      "CSC/2021/016", "08012345616"),
            ("Halima Yusuf",       "CSC/2021/017", "08012345617"),
            ("Segun Balogun",      "CSC/2021/018", "08012345618"),
            ("Ifeoma Chukwu",      "CSC/2021/019", "08012345619"),
            ("Biodun Olatunji",    "CSC/2021/020", "08012345620"),
            ("Maryam Garba",       "CSC/2021/021", "08012345621"),
            ("Dele Okonkwo",       "CSC/2021/022", "08012345622"),
            ("Adaeze Ugwu",        "CSC/2021/023", "08012345623"),
            ("Femi Olawale",       "CSC/2021/024", "08012345624"),
            ("Zainab Danladi",     "CSC/2021/025", "08012345625"),
            ("Oluwatobi Akinwale", "CSC/2021/026", "08012345626"),
            ("Patience Nwofor",    "CSC/2021/027", "08012345627"),
            ("Adamu Bello",        "CSC/2021/028", "08012345628"),
            ("Chiamaka Eze",       "CSC/2021/029", "08012345629"),
            ("Rotimi Adeleke",     "CSC/2021/030", "08012345630"),
            ("Nkechi Okonkwo",     "CSC/2021/031", "08012345631"),
            ("Yakubu Hassan",      "CSC/2021/032", "08012345632"),
            ("Tosin Abiodun",      "CSC/2021/033", "08012345633"),
            ("Chinwe Agu",         "CSC/2021/034", "08012345634"),
            ("Bashir Lawal",       "CSC/2021/035", "08012345635"),
            ("Folake Bamisaye",    "CSC/2021/036", "08012345636"),
            ("Obiora Nweke",       "CSC/2021/037", "08012345637"),
            ("Hauwa Jibrin",       "CSC/2021/038", "08012345638"),
            ("Lanre Omotosho",     "CSC/2021/039", "08012345639"),
            ("Uchenna Okafor",     "CSC/2021/040", "08012345640"),
            ("Ngozi Umeh",         "CSC/2021/041", "08012345641"),
            ("Sola Bamidele",      "CSC/2021/042", "08012345642"),
            ("Mariam Salisu",      "CSC/2021/043", "08012345643"),
            ("Chukwudi Okoro",     "CSC/2021/044", "08012345644"),
            ("Olajumoke Aina",     "CSC/2021/045", "08012345645"),
            ("Muhammed Baba",      "CSC/2021/046", "08012345646"),
            ("Adunola Adebiyi",    "CSC/2021/047", "08012345647"),
            ("Kenechukwu Obi",     "CSC/2021/048", "08012345648"),
            ("Fatimah Umar",       "CSC/2021/049", "08012345649"),
            ("Gbemisola Oye",      "CSC/2021/050", "08012345650"),
        };

        var students = names.Select((n, i) => new HostelMS.Models.User
        {
            FullName     = n.full,
            Email        = $"{n.full.ToLower().Replace(" ", ".")}{i + 1}@student.edu.ng",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123"),
            Role         = "Student",
            MatricNumber = n.matric,
            PhoneNumber  = n.phone,
            CreatedAt    = now.AddDays(-(50 - i))
        }).ToList();

        db.Users.AddRange(students);
        db.SaveChanges();

        // Allocate first 30 students
        var allStudents  = db.Users.Where(u => u.Role == "Student").OrderBy(u => u.Id).ToList();
        var activeRooms  = db.Rooms.Where(r => r.IsActive).OrderBy(r => r.Id).ToList();
        int placed = 0;
        int roomIdx = 0;

        foreach (var student in allStudents.Take(30))
        {
            while (roomIdx < activeRooms.Count && activeRooms[roomIdx].OccupiedSlots >= activeRooms[roomIdx].Capacity)
                roomIdx++;
            if (roomIdx >= activeRooms.Count) break;

            var room = activeRooms[roomIdx];
            var pmt = new HostelMS.Models.Payment
            {
                Reference             = $"HMS-SEED-{student.Id}-{room.Id}",
                Status                = "Success",
                Amount                = room.Price,
                UserId                = student.Id,
                RoomId                = room.Id,
                PaystackTransactionId = $"SEED-{Guid.NewGuid():N}",
                VerifiedAt            = now.AddDays(-(30 - placed)),
                CreatedAt             = now.AddDays(-(31 - placed))
            };
            db.Payments.Add(pmt);
            db.SaveChanges();

            db.Allocations.Add(new HostelMS.Models.Allocation
            {
                UserId      = student.Id,
                RoomId      = room.Id,
                PaymentId   = pmt.Id,
                IsActive    = true,
                AllocatedAt = now.AddDays(-(30 - placed))
            });
            room.OccupiedSlots++;
            db.SaveChanges();
            placed++;
        }
        logger.LogInformation("50 students seeded, 30 allocated.");
    }

    // ── Announcements ─────────────────────────────────────
    if (!db.Announcements.Any())
    {
        db.Announcements.AddRange(new HostelMS.Models.Announcement[]
        {
            new(){Title="Room Allocation Open – 2024/2025",Body="Room allocation for the 2024/2025 academic session is now open. All eligible students must complete payment within 14 days to secure accommodation.",Category="info",IsPinned=true,CreatedByUserId=admin.Id,CreatedAt=now.AddDays(-30),IsActive=true},
            new(){Title="Hostel Fees Final Deadline – 31st January",Body="All students with outstanding hostel fees must complete payment by January 31st. Failure to pay will result in forfeiture of room allocation with no refund.",Category="urgent",IsPinned=false,CreatedByUserId=admin.Id,CreatedAt=now.AddDays(-20),IsActive=true},
            new(){Title="Water Supply Interruption – This Wednesday",Body="There will be a scheduled water supply interruption on Wednesday between 9:00 AM and 2:00 PM for pipe maintenance. Please store water in advance.",Category="warning",IsPinned=false,CreatedByUserId=admin.Id,CreatedAt=now.AddDays(-15),IsActive=true},
            new(){Title="Updated Gate Entry Procedures",Body="Effective from 1st February, all students must present valid ID cards at the hostel gate. Visitors must register at the front desk before entry.",Category="info",IsPinned=false,CreatedByUserId=admin.Id,CreatedAt=now.AddDays(-10),IsActive=true},
            new(){Title="Block B Maintenance – Saturday",Body="Scheduled plumbing and electrical maintenance in Block B on Saturday. Students in Block B should plan to be out between 10 AM and 4 PM.",Category="warning",IsPinned=false,CreatedByUserId=admin.Id,CreatedAt=now.AddDays(-5),IsActive=true},
        });
        db.SaveChanges();
        logger.LogInformation("Announcements seeded.");
    }

    // ── Events ────────────────────────────────────────────
    if (!db.Events.Any())
    {
        db.Events.AddRange(new HostelMS.Models.HostelEvent[]
        {
            new(){Title="Monthly Room Inspection",Description="Monthly cleanliness and maintenance inspection by hostel staff. All students must ensure their rooms are tidy.",Category="official",EventDate=now.AddDays(5),EventTime="10:00 AM",CreatedByUserId=admin.Id,CreatedAt=now,IsActive=true},
            new(){Title="Mandatory Fire Safety Drill",Description="All students must evacuate the building within 3 minutes of the alarm sounding. Attendance is compulsory. Non-attendance will be reported.",Category="safety",EventDate=now.AddDays(10),EventTime="11:00 AM",CreatedByUserId=admin.Id,CreatedAt=now,IsActive=true},
            new(){Title="Students Welfare Committee Meeting",Description="Monthly meeting with the hostel welfare committee. Raise concerns, suggest improvements and receive updates from hostel management.",Category="social",EventDate=now.AddDays(14),EventTime="4:00 PM",CreatedByUserId=admin.Id,CreatedAt=now,IsActive=true},
            new(){Title="Final Deadline – Hostel Fees",Description="Absolute final deadline for all outstanding hostel fees. No extensions will be granted. Students with unpaid fees will be delisted.",Category="deadline",EventDate=now.AddDays(21),EventTime="5:00 PM",CreatedByUserId=admin.Id,CreatedAt=now,IsActive=true},
            new(){Title="End of Session Check-Out",Description="All students must vacate rooms and return keys by 5:00 PM. Late check-out attracts a ₦5,000 penalty fee per day.",Category="official",EventDate=now.AddDays(60),EventTime="5:00 PM",CreatedByUserId=admin.Id,CreatedAt=now,IsActive=true},
        });
        db.SaveChanges();
        logger.LogInformation("Events seeded.");
    }

    // ── Messages ──────────────────────────────────────────
    if (!db.Messages.Any())
    {
        var seedStudents = db.Users.Where(u => u.Role == "Student").OrderBy(u => u.Id).Take(8).ToList();
        var convos = new (string s, string a)[]
        {
            ("Good day, I wanted to confirm that my room payment was processed successfully. I can see it on my dashboard but wanted verbal confirmation.", "Hello! Yes, your payment is confirmed and your room allocation is fully active. You can view all details on your dashboard. Welcome to the hostel!"),
            ("I have a maintenance issue in my room — the ceiling fan makes a very loud noise that is affecting my studies. How do I report this?", "Thank you for reaching out. Please visit the hostel office (Block D, Room 1) between 8 AM and 5 PM on weekdays to fill a maintenance request. A technician will attend within 48 hours."),
            ("I am unable to log into the portal — I keep getting an error that my email is not found.", "Please ensure you are using the exact email you registered with, including correct capitalisation. If the issue persists, visit the ICT office with your matric card for a manual password reset."),
            ("I would like to request a room change. My current roommate and I have a personality clash that is affecting my academics.", "Room change requests are reviewed at the end of each session. Please submit a formal written application at the hostel office. We will do our best to accommodate you."),
            ("I noticed my room is listed as Double but I was told it would be Single. Can this be reviewed?", "I can see your allocation details. The system assigned you a Double room based on availability at time of payment. You are on the waiting list for Single rooms and will be notified if one becomes available."),
            ("What is the procedure for renewing my hostel accommodation into the next academic session?", "Renewal applications open 3 weeks before end of session. You will receive an email notification when the portal opens. Existing students get priority allocation before new applicants."),
            ("My room key is not working properly — it takes many attempts before the door opens.", "Please report to the hostel security desk immediately with your student ID. This is a security concern and we will arrange for the lock cylinder to be inspected and replaced within 24 hours."),
            ("Just wanted to say thank you for the quick resolution of the hot water issue in Block C last week. Really appreciated!", "Thank you for the kind feedback! We are committed to a comfortable stay for all students. Please never hesitate to reach out whenever you need anything."),
        };

        for (int i = 0; i < Math.Min(seedStudents.Count, convos.Length); i++)
        {
            var st = seedStudents[i];
            var ts = now.AddDays(-i - 1).AddHours(-2);
            db.Messages.Add(new HostelMS.Models.Message { SenderId = st.Id, ReceiverId = admin.Id, Text = convos[i].s, Timestamp = ts, IsRead = true });
            db.Messages.Add(new HostelMS.Models.Message { SenderId = admin.Id, ReceiverId = st.Id, Text = convos[i].a, Timestamp = ts.AddMinutes(12), IsRead = true });
        }
        db.SaveChanges();
        logger.LogInformation("Messages seeded.");
    }
}

// ── Middleware pipeline ────────────────────────────────────
// ORDER MATTERS: Routing → CORS → Auth → Authorization → Endpoints
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RoomHub>("/hubs/rooms");

app.Run();
