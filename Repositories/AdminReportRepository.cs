using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs.Reports;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class AdminReportRepository : IAdminReportRepository
    {
        private readonly AxivoraDbContext _context;

        public AdminReportRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountAppointmentReportAsync(ReportFilterDto filter)
        {
            var query = BuildAppointmentReportQuery(filter);
            return await query.CountAsync();
        }

        public async Task<IEnumerable<AppointmentReportView>> GetAppointmentReportPagedAsync(
            ReportFilterDto filter, int skip, int take)
        {
            var query = BuildAppointmentReportQuery(filter);
            return await query
                .OrderByDescending(r => r.AppointmentStart)
                .Skip(skip).Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<DoctorWorkloadReportView>> GetDoctorWorkloadReportAsync(
            DateTime? from, DateTime? to)
        {
            IQueryable<DoctorWorkloadReportView> query = _context.DoctorWorkloadReports;

            if (from.HasValue || to.HasValue)
            {
                IQueryable<Appointment> appointmentQuery = _context.Appointments.Where(a => !a.IsDeleted);

                if (from.HasValue)
                    appointmentQuery = appointmentQuery.Where(a => a.AppointmentStart >= from.Value);

                if (to.HasValue)
                    appointmentQuery = appointmentQuery.Where(a => a.AppointmentStart <= to.Value);

                var ids = appointmentQuery.Select(a => a.DoctorId).Distinct();
                query = query.Where(r => ids.Contains(r.DoctorId));
            }

            return await query.OrderBy(r => r.DoctorName).ToListAsync();
        }

        private IQueryable<AppointmentReportView> BuildAppointmentReportQuery(ReportFilterDto filter)
        {
            IQueryable<AppointmentReportView> query = _context.AppointmentReports;

            // Ensure the AppointmentReports view in SQL filters out deleted appointments (a.IsDeleted = 0)

            if (filter.From.HasValue)
                query = query.Where(r => r.AppointmentStart >= filter.From.Value);

            if (filter.To.HasValue)
                query = query.Where(r => r.AppointmentStart <= filter.To.Value);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(r => r.StatusName == filter.Status);

            if (filter.DoctorId.HasValue)
            {
                var doctorId = filter.DoctorId.Value;
                var matchingIds = _context.Appointments
                    .Where(a => a.DoctorId == doctorId && !a.IsDeleted)
                    .Select(a => a.AppointmentId);

                query = query.Where(r => matchingIds.Contains(r.AppointmentId));
            }

            return query;
        }
    }
}
