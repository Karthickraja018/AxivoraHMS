using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Axivora.Configuration;
using Axivora.Data;
using Axivora.Middleware;
using Axivora.Services;
using Axivora.Services.Interfaces;
using Axivora.Repositories;
using Axivora.Repositories.Interfaces;
using Axivora.Mappings;
using Axivora.Security;
using Axivora.Infrastructure.Email;
using Axivora.Models;
using Axivora.Services.BackgroundServices;

namespace Axivora
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            
            // Register DbContext with SQL Server
            builder.Services.AddDbContext<AxivoraDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // Email infrastructure
            // Bind EmailSettings from appsettings.json and make available via IOptions<EmailSettings>
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));

            builder.Services.Configure<HospitalPdfSettings>(
                builder.Configuration.GetSection("HospitalPdf"));

            // Singleton queue: one shared ConcurrentQueue<EmailMessage> across the entire app lifetime
            builder.Services.AddSingleton<IEmailQueue, EmailQueue>();

            // Transient SMTP sender used exclusively by EmailBackgroundService
            builder.Services.AddTransient<SmtpEmailService>();

            // Scoped email service: renders templates and enqueues messages (no SMTP on request thread)
            builder.Services.AddScoped<IEmailService, EmailService>();

            // Hosted services: email delivery worker + appointment reminder job
            builder.Services.AddHostedService<EmailBackgroundService>();
            builder.Services.AddHostedService<AppointmentReminderService>();
            // End email infrastructure

            // Configure JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // FIX 8: Resource-based ownership policy
            // Controllers call: _authorizationService.AuthorizeAsync(User, resource, "ResourceOwner")
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("ResourceOwner", policy =>
                    policy.Requirements.Add(new OwnershipRequirement()));
            });

            // FIX 8: Register the ownership handler that implements the ResourceOwner policy
            builder.Services.AddScoped<IAuthorizationHandler, OwnershipAuthorizationHandler>();

            // Register AutoMapper
            builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);

            // Register Application Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPatientService, PatientService>();
            builder.Services.AddScoped<IDoctorService, DoctorService>();
            builder.Services.AddScoped<IAppointmentService, AppointmentService>();
            builder.Services.AddScoped<IConsultationService, ConsultationService>();
            builder.Services.AddScoped<ILabTestService, LabTestService>();
            builder.Services.AddScoped<IMedicineService, MedicineService>();
            builder.Services.AddScoped<IMedicalHistoryService, MedicalHistoryService>();
            builder.Services.AddScoped<IFeedbackService, FeedbackService>();
            builder.Services.AddScoped<IAdminReportService, AdminReportService>();
            // New feature services
            builder.Services.AddScoped<IDepartmentService, DepartmentService>();
            builder.Services.AddScoped<IICDCodeService, ICDCodeService>();
            builder.Services.AddScoped<IAdminUserService, AdminUserService>();
            builder.Services.AddScoped<IPatientVitalService, PatientVitalService>();
            builder.Services.AddScoped<IDoctorDashboardService, DoctorDashboardService>();
            builder.Services.AddScoped<IPatientDashboardService, PatientDashboardService>();
            builder.Services.AddScoped<IPdfService, PdfService>();
            // Date-based slot scheduling services
            builder.Services.AddScoped<IDoctorAvailabilityTemplateService, DoctorAvailabilityTemplateService>();
            builder.Services.AddScoped<IDoctorAvailabilityService, DoctorAvailabilityService>();
            builder.Services.AddScoped<ISlotService, SlotService>();
            builder.Services.AddHostedService<AvailabilityGenerationBackgroundService>();

            // FIX 11: Idempotency service ù prevents duplicate bookings on network retries
            builder.Services.AddScoped<IdempotencyService>();

            // Register Repositories
            builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
            builder.Services.AddScoped<IPatientRepository, PatientRepository>();
            builder.Services.AddScoped<IConsultationRepository, ConsultationRepository>();
            builder.Services.AddScoped<IMedicalHistoryRepository, MedicalHistoryRepository>();
            builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            builder.Services.AddScoped<ILabTestRepository, LabTestRepository>();
            builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
            builder.Services.AddScoped<IAdminReportRepository, AdminReportRepository>();
            builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            builder.Services.AddScoped<IICDCodeRepository, ICDCodeRepository>();
            builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
            builder.Services.AddScoped<IPatientVitalRepository, PatientVitalRepository>();
            // Date-based slot scheduling repositories
            builder.Services.AddScoped<IAvailabilityTemplateRepository, AvailabilityTemplateRepository>();
            builder.Services.AddScoped<IAvailabilityDayRepository, AvailabilityDayRepository>();
            builder.Services.AddScoped<IAppointmentSlotRepository, AppointmentSlotRepository>();

            // Register token service
            builder.Services.AddScoped<ITokenService, TokenService>();
            // Register password hasher
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

            // Add Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            });

            // Add CORS service
            builder.Services.AddCors();

            var app = builder.Build();

            // Development convenience: auto-apply EF migrations so local DB matches the model.
            // Prevents runtime errors like "Invalid object name ..." when the database is empty/new.
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AxivoraDbContext>();
                db.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            
            // Add Global Exception Handler Middleware
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Add CORS middleware
            app.UseCors(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod());

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
