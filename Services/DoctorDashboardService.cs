using Axivora.DTOs;
using Axivora.Repositories.Interfaces;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class DoctorDashboardService : IDoctorDashboardService
    {
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentReadService _appointmentReadService;
        private readonly ILabTestRepository _labTestRepository;

        public DoctorDashboardService(
            IDoctorService doctorService,
            IAppointmentReadService appointmentReadService,
            ILabTestRepository labTestRepository)
        {
            _doctorService = doctorService;
            _appointmentReadService = appointmentReadService;
            _labTestRepository = labTestRepository;
        }

        public async Task<DoctorDashboardDto> GetDoctorDashboardAsync(int doctorUserId)
        {
            var doctor = await _doctorService.GetDoctorByUserIdAsync(doctorUserId);
            if (doctor == null)
                throw new KeyNotFoundException("Doctor profile not found for this account.");

            var appointments = (await _appointmentReadService.GetAppointmentsByDoctorIdAsync(doctor.DoctorId)).ToList();

            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);

            var todayAppointments = appointments
                .Where(a => a.AppointmentStart >= todayUtc && a.AppointmentStart < tomorrowUtc)
                .OrderBy(a => a.AppointmentStart)
                .ToList();

            var terminalStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Completed", "Cancelled", "NoShow"
            };

            var nextAppt = todayAppointments
                .Where(a => !terminalStatuses.Contains(a.Status) && a.AppointmentStart >= DateTime.UtcNow.AddMinutes(-10))
                .OrderBy(a => a.AppointmentStart)
                .FirstOrDefault();

            var cancelledTodayCount = todayAppointments.Count(a =>
                string.Equals(a.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));

            var todayPatientsCount = todayAppointments
                .Where(a => !string.Equals(a.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(a.Status, "NoShow", StringComparison.OrdinalIgnoreCase))
                .Select(a => a.PatientId)
                .Distinct()
                .Count();

            // Pending consults: appointments that are not terminal and already started / ready
            var pendingConsultationAppointments = todayAppointments
                .Where(a =>
                    !terminalStatuses.Contains(a.Status) &&
                    (string.Equals(a.Status, "InProgress", StringComparison.OrdinalIgnoreCase) ||
                     (string.Equals(a.Status, "Scheduled", StringComparison.OrdinalIgnoreCase) && a.AppointmentStart <= DateTime.UtcNow)))
                .OrderBy(a => a.AppointmentStart)
                .Take(8)
                .ToList();

            var pendingConsultations = pendingConsultationAppointments
                .Select(a => new DoctorDashboardPendingConsultationDto
                {
                    AppointmentId = a.AppointmentId,
                    PatientName = a.PatientName,
                    Status = a.Status,
                    AppointmentStart = a.AppointmentStart
                })
                .ToList();

            var pendingLab = (await _labTestRepository.GetPendingByDoctorIdAsync(doctor.DoctorId, take: 20))
                .Select(MapToLabResultDto)
                .ToList();

            return new DoctorDashboardDto
            {
                Profile = doctor,
                Stats = new DoctorDashboardStatsDto
                {
                    TodayPatientsCount = todayPatientsCount,
                    NextAppointmentTime = nextAppt?.AppointmentStart,
                    PendingConsultationsCount = pendingConsultations.Count,
                    CancelledTodayCount = cancelledTodayCount
                },
                NextAppointment = nextAppt,
                TodayAppointments = todayAppointments,
                PendingConsultations = pendingConsultations,
                PendingLabResults = pendingLab
            };
        }

        private static LabResultDto MapToLabResultDto(Models.OrderedTest ot) => new()
        {
            OrderedTestId = ot.OrderedTestId,
            ConsultationId = ot.ConsultationId,
            LabTestId = ot.LabTestId,
            TestName = ot.LabTest?.TestName ?? string.Empty,
            Status = ot.Status,
            Result = ot.Result,
            ResultDate = ot.ResultDate,
            OrderedAt = ot.OrderedAt,
            PatientId = ot.Consultation?.Appointment?.PatientId ?? 0,
            PatientName = ot.Consultation?.Appointment?.Patient?.FullName ?? string.Empty,
            TestType = ot.LabTest?.TestType ?? "Single",
            Unit = ot.LabTest?.Unit,
            ReferenceRange = ot.LabTest?.ReferenceRange
        };
    }
}

