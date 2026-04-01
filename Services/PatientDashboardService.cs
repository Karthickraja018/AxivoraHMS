using System.Globalization;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class PatientDashboardService : IPatientDashboardService
    {
        private readonly IPatientService _patientService;
        private readonly IAppointmentService _appointmentService;
        private readonly IConsultationService _consultationService;
        private readonly IDoctorService _doctorService;

        public PatientDashboardService(
            IPatientService patientService,
            IAppointmentService appointmentService,
            IConsultationService consultationService,
            IDoctorService doctorService)
        {
            _patientService = patientService;
            _appointmentService = appointmentService;
            _consultationService = consultationService;
            _doctorService = doctorService;
        }

        public async Task<PatientDashboardDto> GetPatientDashboardAsync(int patientUserId)
        {
            var patient = await _patientService.GetPatientByUserIdAsync(patientUserId);
            var appointments = (await _appointmentService.GetAppointmentsByPatientIdAsync(patient.PatientId)).ToList();

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

            var activeRx = consultations
                .SelectMany(c => c.Prescriptions ?? new List<PrescriptionDto>())
                .Count();

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
                        ? (string.IsNullOrWhiteSpace(c.ICDCode) ? "—" : c.ICDCode)
                        : c.DiagnosisNotes,
                    Date = c.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                })
                .ToList();

            var rxPreview = consultations
                .OrderByDescending(c => c.CreatedAt)
                .SelectMany(c => (c.Prescriptions ?? new List<PrescriptionDto>()).Select(p => (c, p)))
                .Take(6)
                .Select(x => new PatientDashboardPrescriptionDto
                {
                    PrescriptionId = x.p.PrescriptionId,
                    MedicineName = x.p.MedicineName,
                    Dosage = string.IsNullOrWhiteSpace(x.p.Dosage) ? "—" : x.p.Dosage
                })
                .ToList();

            var labPreview = consultations
                .OrderByDescending(c => c.CreatedAt)
                .SelectMany(c => c.OrderedTests ?? new List<OrderedTestDto>())
                .Take(8)
                .Select(t => new PatientDashboardLabResultDto
                {
                    OrderedTestId = t.OrderedTestId,
                    TestName = t.TestName,
                    Status = t.Status
                })
                .ToList();

            var activity = BuildActivityFeed(appointments, consultations);

            var ageYears = ComputeAgeYears(patient.DateOfBirth, now);

            return new PatientDashboardDto
            {
                Profile = new PatientDashboardProfileDto
                {
                    Name = patient.FullName,
                    Age = ageYears.ToString(CultureInfo.InvariantCulture),
                    Gender = patient.Gender ?? "—"
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
                RecentActivity = activity
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
