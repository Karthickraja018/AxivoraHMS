using System.Globalization;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class PatientDashboardService : IPatientDashboardService
    {
        private readonly IPatientService _patientService;
        private readonly IAppointmentReadService _appointmentReadService;
        private readonly IConsultationService _consultationService;
        private readonly IDoctorService _doctorService;

        public PatientDashboardService(
            IPatientService patientService,
            IAppointmentReadService appointmentReadService,
            IConsultationService consultationService,
            IDoctorService doctorService)
        {
            _patientService = patientService;
            _appointmentReadService = appointmentReadService;
            _consultationService = consultationService;
            _doctorService = doctorService;
        }

        public async Task<PatientDashboardDto> GetPatientDashboardAsync(int patientUserId)
        {
            var patient = await _patientService.GetPatientByUserIdAsync(patientUserId);
            var appointments = (await _appointmentReadService.GetAppointmentsByPatientIdAsync(patient.PatientId)).ToList();

            var consultPage = await _consultationService.GetConsultationsByPatientIdAsync(
                patient.PatientId,
                new PaginationParams { PageNumber = 1, PageSize = 50 });

            var consultations = consultPage.Items.ToList();

            var now = DateTime.UtcNow;
            var completedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Completed", "Cancelled", "NoShow"
            };

            var upcoming = appointments
                .Where(a => a.AppointmentStart >= now && !completedStatuses.Contains(a.Status))
                .OrderBy(a => a.AppointmentStart)
                .ToList();

            PatientDashboardNextAppointmentDto? nextDto = null;
            if (upcoming.Count > 0)
            {
                var n = upcoming[0];
                var spec = "General practice";
                try
                {
                    var doc = await _doctorService.GetDoctorByIdAsync(n.DoctorId);
                    if (!string.IsNullOrWhiteSpace(doc.Qualification))
                        spec = doc.Qualification;
                    else if (doc.Departments is { Count: > 0 } deps && !string.IsNullOrWhiteSpace(deps[0].DepartmentName))
                        spec = deps[0].DepartmentName;
                }
                catch
                {
                    /* doctor missing — keep default */
                }

                nextDto = new PatientDashboardNextAppointmentDto
                {
                    AppointmentId = n.AppointmentId,
                    DoctorName = n.DoctorName,
                    Specialization = spec,
                    Date = n.AppointmentStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Time = n.AppointmentStart.ToString("HH:mm", CultureInfo.InvariantCulture),
                    Status = n.Status
                };
            }

            var visitComplete = appointments.Count(a =>
                string.Equals(a.Status, "Completed", StringComparison.OrdinalIgnoreCase));

            var lastVisit = appointments
                .Where(a => string.Equals(a.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.AppointmentStart)
                .FirstOrDefault();

            var pendingTests = consultations
                .SelectMany(c => c.OrderedTests ?? new List<OrderedTestDto>())
                .Count(t => string.Equals(t.Status, "Pending", StringComparison.OrdinalIgnoreCase));

            var activePrescriptionRows = consultations
                .SelectMany(c => (c.Prescriptions ?? new List<PrescriptionDto>()).Select(p =>
                {
                    var startDate = c.AppointmentDate.Date;
                    var durationDays = p.DurationDays.GetValueOrDefault(1);
                    if (durationDays < 1) durationDays = 1;

                    var endDate = startDate.AddDays(durationDays - 1);
                    var isActive = now.Date >= startDate && now.Date <= endDate;
                    var remainingDays = isActive ? (endDate - now.Date).Days + 1 : 0;

                    return new
                    {
                        Consultation = c,
                        Prescription = p,
                        IsActive = isActive,
                        RemainingDays = remainingDays,
                    };
                }))
                .ToList();

            var activeRx = activePrescriptionRows.Count(x => x.IsActive);

            var recentAppts = appointments
                .OrderByDescending(a => a.AppointmentStart)
                .Take(5)
                .Select(a => new PatientDashboardRecentAppointmentDto
                {
                    AppointmentId = a.AppointmentId,
                    DoctorName = a.DoctorName,
                    Date = a.AppointmentStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Time = a.AppointmentStart.ToString("HH:mm", CultureInfo.InvariantCulture),
                    Status = a.Status
                })
                .ToList();

            var recentConsults = consultations
                .Take(3)
                .Select(c => new PatientDashboardRecentConsultationDto
                {
                    ConsultationId = c.ConsultationId,
                    DoctorName = c.DoctorName,
                    Diagnosis = string.IsNullOrWhiteSpace(c.DiagnosisNotes)
                        ? (string.IsNullOrWhiteSpace(c.ICDCode) ? "-" : c.ICDCode)
                        : c.DiagnosisNotes,
                    Date = c.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    HasFeedback = c.SessionFeedback != null
                })
                .ToList();

            var rxPreview = activePrescriptionRows
                .Where(x => x.IsActive)
                .OrderBy(x => x.RemainingDays)
                .ThenByDescending(x => x.Consultation.CreatedAt)
                .Take(6)
                .Select(x => new PatientDashboardPrescriptionDto
                {
                    PrescriptionId = x.Prescription.PrescriptionId,
                    MedicineName = x.Prescription.MedicineName,
                    Dosage = string.IsNullOrWhiteSpace(x.Prescription.Dosage) ? "-" : x.Prescription.Dosage,
                    Frequency = string.IsNullOrWhiteSpace(x.Prescription.Frequency) ? "-" : x.Prescription.Frequency,
                    IsActive = x.IsActive,
                    RemainingDays = x.RemainingDays,
                })
                .ToList();

            var labPreview = consultations
                .OrderByDescending(c => c.CreatedAt)
                .SelectMany(c => (c.OrderedTests ?? new List<OrderedTestDto>()).Select(t => (Consultation: c, Test: t)))
                .Take(8)
                .Select(x => new PatientDashboardLabResultDto
                {
                    OrderedTestId = x.Test.OrderedTestId,
                    TestName = x.Test.TestName,
                    Status = x.Test.Status,
                    TestType = string.IsNullOrWhiteSpace(x.Test.TestType) ? "Single" : x.Test.TestType,
                    HasReportFile = x.Test.HasReportFile,
                    DoctorName = x.Consultation.DoctorName,
                    ReportFileName = x.Test.ReportFileName
                })
                .ToList();

            var activity = BuildActivityFeed(appointments, consultations);

            var ageYears = ComputeAgeYears(patient.DateOfBirth, now);

            var vitalsPreview = new List<PatientDashboardVitalsDto>
            {
                new PatientDashboardVitalsDto { Date = now.AddDays(-6).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Bp = "120/80", HeartRate = 72, Temperature = 98.6m, Weight = 75.0m },
                new PatientDashboardVitalsDto { Date = now.AddDays(-5).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Bp = "122/81", HeartRate = 74, Temperature = 98.7m, Weight = 75.2m },
                new PatientDashboardVitalsDto { Date = now.AddDays(-4).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Bp = "118/79", HeartRate = 70, Temperature = 98.5m, Weight = 74.8m },
                new PatientDashboardVitalsDto { Date = now.AddDays(-3).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Bp = "121/80", HeartRate = 73, Temperature = 98.6m, Weight = 75.1m },
                new PatientDashboardVitalsDto { Date = now.AddDays(-2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Bp = "124/82", HeartRate = 76, Temperature = 98.8m, Weight = 75.3m },
                new PatientDashboardVitalsDto { Date = now.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Bp = "119/78", HeartRate = 71, Temperature = 98.4m, Weight = 74.9m },
                new PatientDashboardVitalsDto { Date = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Bp = "120/80", HeartRate = 72, Temperature = 98.6m, Weight = 75.0m },
            };

            return new PatientDashboardDto
            {
                Profile = new PatientDashboardProfileDto
                {
                    Name = patient.FullName,
                    Age = ageYears.ToString(CultureInfo.InvariantCulture),
                    Gender = patient.Gender ?? "-"
                },
                NextAppointment = nextDto,
                Stats = new PatientDashboardStatsDto
                {
                    TotalVisits = visitComplete,
                    ActivePrescriptions = activeRx,
                    PendingTests = pendingTests,
                    LastVisitDate = lastVisit == null
                        ? null
                        : lastVisit.AppointmentStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                },
                RecentAppointments = recentAppts,
                RecentConsultations = recentConsults,
                Prescriptions = rxPreview,
                LabResults = labPreview,
                RecentActivity = activity,
                VitalsHistory = vitalsPreview
            };
        }

        private static int ComputeAgeYears(DateOnly dateOfBirth, DateTime utcNow)
        {
            var today = DateOnly.FromDateTime(utcNow);
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth > today.AddYears(-age))
                age--;
            return Math.Max(0, age);
        }

        private static List<PatientDashboardActivityDto> BuildActivityFeed(
            List<AppointmentDto> appointments,
            List<ConsultationDto> consultations)
        {
            var rows = new List<PatientDashboardActivityDto>();

            foreach (var a in appointments.OrderByDescending(x => x.AppointmentStart).Take(5))
            {
                rows.Add(new PatientDashboardActivityDto
                {
                    Id = $"appt-{a.AppointmentId}",
                    Kind = "appointment",
                    Title = "Appointment scheduled",
                    Subtitle = $"{a.DoctorName} · {a.Status}",
                    At = a.AppointmentStart
                });
            }

            foreach (var c in consultations.Take(5))
            {
                rows.Add(new PatientDashboardActivityDto
                {
                    Id = $"cons-{c.ConsultationId}",
                    Kind = "consultation",
                    Title = "Consultation recorded",
                    Subtitle = c.DoctorName,
                    At = c.CreatedAt
                });
            }

            foreach (var c in consultations)
            {
                foreach (var t in c.OrderedTests ?? new List<OrderedTestDto>())
                {
                    if (string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        var at = t.ResultDate ?? c.CreatedAt;
                        rows.Add(new PatientDashboardActivityDto
                        {
                            Id = $"lab-{t.OrderedTestId}",
                            Kind = "lab",
                            Title = "Lab result available",
                            Subtitle = t.TestName,
                            At = at
                        });
                    }
                }
            }

            return rows
                .OrderByDescending(r => r.At)
                .Take(12)
                .ToList();
        }
    }
}
