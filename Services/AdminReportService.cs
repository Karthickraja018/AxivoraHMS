using Axivora.DTOs.Reports;
using Axivora.Helpers;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    /// <inheritdoc />
    public class AdminReportService : IAdminReportService
    {
        private readonly IAdminReportRepository _repository;

        public AdminReportService(IAdminReportRepository repository)
        {
            _repository = repository;
        }

        /// <inheritdoc />
        public async Task<PaginationResponse<AppointmentReportDto>> GetAppointmentReportAsync(ReportFilterDto filter)
        {
            var totalCount = await _repository.CountAppointmentReportAsync(filter);

            var rows = await _repository.GetAppointmentReportPagedAsync(
                filter,
                (filter.PageNumber - 1) * filter.PageSize,
                filter.PageSize);

            var dtos = rows.Select(r => new AppointmentReportDto
            {
                AppointmentId    = r.AppointmentId,
                AppointmentStart = r.AppointmentStart,
                AppointmentEnd   = r.AppointmentEnd,
                PatientName      = r.PatientName,
                PatientPhone     = r.PatientPhone,
                MRN              = r.MRN,
                DoctorName       = r.DoctorName,
                DepartmentName   = r.DepartmentName,
                StatusName       = r.StatusName,
                Reason           = r.Reason,
                HasConsultation  = r.HasConsultation
            }).ToList();

            return new PaginationResponse<AppointmentReportDto>(dtos, totalCount, filter.PageNumber, filter.PageSize);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DoctorWorkloadDto>> GetDoctorWorkloadReportAsync(DateTime? from, DateTime? to)
        {
            var rows = await _repository.GetDoctorWorkloadReportAsync(from, to);

            return rows.Select(r => new DoctorWorkloadDto
            {
                DoctorId              = r.DoctorId,
                DoctorName            = r.DoctorName,
                Qualification         = r.Qualification,
                DepartmentName        = r.DepartmentName,
                TotalAppointments     = r.TotalAppointments,
                CompletedAppointments = r.CompletedAppointments,
                CancelledAppointments = r.CancelledAppointments,
                TotalConsultations    = r.TotalConsultations
            });
        }
    }
}
