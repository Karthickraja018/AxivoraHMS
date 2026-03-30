namespace Axivora.Services.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> BuildPrescriptionPdfAsync(int consultationId, int callerUserId, string callerRole);

        Task<byte[]> BuildLabReportPdfAsync(int orderedTestId, int callerUserId, string callerRole);
    }
}
