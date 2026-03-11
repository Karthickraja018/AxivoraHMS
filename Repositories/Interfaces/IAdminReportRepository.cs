using Axivora.Models;
using Axivora.DTOs.Reports;

namespace Axivora.Repositories.Interfaces
{
    public interface IAdminReportRepository
    {
        Task<int> CountAppointmentReportAsync(ReportFilterDto filter);
        Task<IEnumerable<AppointmentReportView>> GetAppointmentReportPagedAsync(ReportFilterDto filter, int skip, int take);
        Task<IEnumerable<DoctorWorkloadReportView>> GetDoctorWorkloadReportAsync(DateTime? from, DateTime? to);
    }
}
