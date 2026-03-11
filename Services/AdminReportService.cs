using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs.Reports;
using Axivora.Helpers;
using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    /// <inheritdoc />
    public class AdminReportService : IAdminReportService
    {
        private readonly AxivoraDbContext _context;

        public AdminReportService(AxivoraDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<PaginationResponse<AppointmentReportDto>> GetAppointmentReportAsync(
            ReportFilterDto filter)
        {
            IQueryable<AppointmentReportView> query = _context.AppointmentReports;

            // ?? date range ????????????????????????????????????????????????????????
            if (filter.From.HasValue)
                query = query.Where(r => r.AppointmentStart >= filter.From.Value);

            if (filter.To.HasValue)
                query = query.Where(r => r.AppointmentStart <= filter.To.Value);

            // ?? status ????????????????????????????????????????????????????????????
            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(r => r.StatusName == filter.Status);

            // ?? doctor ????????????????????????????????????????????????????????????
            // vw_AppointmentReport doesn't expose DoctorId directly, so we filter
            // via a sub-query join on the Appointments table.
            if (filter.DoctorId.HasValue)
            {
                var doctorId = filter.DoctorId.Value;
                var matchingIds = _context.Appointments
                    .Where(a => a.DoctorId == doctorId && !a.IsDeleted)
                    .Select(a => a.AppointmentId);

                query = query.Where(r => matchingIds.Contains(r.AppointmentId));
            }

            // ?? count before paging ???????????????????????????????????????????????
            var totalCount = await query.CountAsync();

            // ?? sort + page ???????????????????????????????????????????????????????
            var rows = await query
                .OrderByDescending(r => r.AppointmentStart)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dtos = rows.Select(r => new AppointmentReportDto
            {
                AppointmentId   = r.AppointmentId,
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

            return new PaginationResponse<AppointmentReportDto>(
                dtos, totalCount, filter.PageNumber, filter.PageSize);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DoctorWorkloadDto>> GetDoctorWorkloadReportAsync(
            DateTime? from, DateTime? to)
        {
            // The workload view aggregates over all appointments; date filtering is done
            // by restricting which appointment IDs are relevant via the Appointments table,
            // then keeping only doctors who have at least one appointment in that window
            // (or returning all doctors when no date range is specified).
            IQueryable<DoctorWorkloadReportView> query = _context.DoctorWorkloadReports;

            if (from.HasValue || to.HasValue)
            {
                IQueryable<Appointment> appointmentQuery = _context.Appointments
                    .Where(a => !a.IsDeleted);

                if (from.HasValue)
                    appointmentQuery = appointmentQuery.Where(a => a.AppointmentStart >= from.Value);

                if (to.HasValue)
                    appointmentQuery = appointmentQuery.Where(a => a.AppointmentStart <= to.Value);

                var ids = appointmentQuery.Select(a => a.DoctorId).Distinct();
                query = query.Where(r => ids.Contains(r.DoctorId));
            }

            var rows = await query
                .OrderBy(r => r.DoctorName)
                .ToListAsync();

            return rows.Select(r => new DoctorWorkloadDto
            {
                DoctorId             = r.DoctorId,
                DoctorName           = r.DoctorName,
                Qualification        = r.Qualification,
                DepartmentName       = r.DepartmentName,
                TotalAppointments    = r.TotalAppointments,
                CompletedAppointments = r.CompletedAppointments,
                CancelledAppointments = r.CancelledAppointments,
                TotalConsultations   = r.TotalConsultations
            });
        }
    }
}
