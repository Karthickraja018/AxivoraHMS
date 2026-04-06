using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Axivora.Services
{
    public class LabTestService : ILabTestService
    {
        private readonly ILabTestRepository _repository;
        private readonly IEmailService _emailService;
        private readonly ILogger<LabTestService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IPdfService _pdfService;

        public LabTestService(
            ILabTestRepository repository,
            IEmailService emailService,
            ILogger<LabTestService> logger,
            IWebHostEnvironment env,
            IPdfService pdfService)
        {
            _repository = repository;
            _emailService = emailService;
            _logger = logger;
            _env = env;
            _pdfService = pdfService;
        }

        public async Task<LabResultDto> UploadResultAsync(int orderedTestId, LabResultUpdateDto dto)
        {
            var orderedTest = await _repository.GetOrderedTestByIdAsync(orderedTestId);

            if (orderedTest == null)
                throw new KeyNotFoundException($"Ordered test with ID {orderedTestId} not found.");

            orderedTest.Result     = dto.Result;
            orderedTest.Status     = "Completed";
            orderedTest.ResultDate = DateTime.UtcNow;

            await _repository.SaveChangesAsync();

            await TrySendLabResultUploadedEmailAsync(orderedTest);

            return MapToLabResultDto(orderedTest);
        }

        public async Task<IEnumerable<LabResultDto>> GetResultsByPatientAsync(int patientId)
        {
            if (!await _repository.PatientExistsAsync(patientId))
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            var orderedTests = await _repository.GetByPatientIdAsync(patientId);
            return orderedTests.Select(MapToLabResultDto);
        }

        public async Task<IEnumerable<LabResultDto>> GetResultsByConsultationAsync(int consultationId)
        {
            if (!await _repository.ConsultationExistsAsync(consultationId))
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            var orderedTests = await _repository.GetByConsultationIdAsync(consultationId);
            return orderedTests.Select(MapToLabResultDto);
        }

        public async Task<PaginationResponse<LabTestCatalogueDto>> GetCatalogueAsync(string? search, int pageNumber, int pageSize)
        {
            var totalCount = await _repository.CountCatalogueAsync(search);
            var items = await _repository.GetCataloguePagedAsync(search, (pageNumber - 1) * pageSize, pageSize);

            var dtos = items.Select(lt => new LabTestCatalogueDto
            {
                LabTestId      = lt.LabTestId,
                TestName       = lt.TestName,
                Description    = lt.Description,
                TestType       = lt.TestType,
                Unit           = lt.Unit,
                ReferenceRange = lt.ReferenceRange
            }).ToList();

            return new PaginationResponse<LabTestCatalogueDto>(dtos, totalCount, pageNumber, pageSize);
        }

        public async Task<LabTestCatalogueDto?> GetCatalogueItemAsync(int id)
        {
            var labTest = await _repository.GetCatalogueItemAsync(id);

            if (labTest is null)
                return null;

            return new LabTestCatalogueDto
            {
                LabTestId      = labTest.LabTestId,
                TestName       = labTest.TestName,
                Description    = labTest.Description,
                TestType       = labTest.TestType,
                Unit           = labTest.Unit,
                ReferenceRange = labTest.ReferenceRange
            };
        }

        private static LabResultDto MapToLabResultDto(Models.OrderedTest ot) => new()
        {
            OrderedTestId  = ot.OrderedTestId,
            ConsultationId = ot.ConsultationId,
            LabTestId      = ot.LabTestId,
            TestName       = ot.LabTest?.TestName ?? string.Empty,
            Status         = ot.Status,
            Result         = ot.Result,
            ResultDate     = ot.ResultDate,
            OrderedAt      = ot.OrderedAt,
            PatientId      = ot.Consultation?.Appointment?.PatientId ?? 0,
            PatientName    = ot.Consultation?.Appointment?.Patient?.FullName ?? string.Empty,
            TestType       = ot.LabTest?.TestType ?? "Single",
            Unit           = ot.LabTest?.Unit,
            ReferenceRange = ot.LabTest?.ReferenceRange,
            HasReportFile  = !string.IsNullOrWhiteSpace(ot.ReportFilePath),
            ReportFileName = ot.ReportFileName
        };

        public async Task<IEnumerable<PatientLabResultDto>> GetMyLabResultsAsync(int userId)
        {
            var orderedTests = await _repository.GetByUserIdAsync(userId);

            return orderedTests.Select(ot => new PatientLabResultDto
            {
                OrderedTestId = ot.OrderedTestId,
                LabTestName = ot.LabTest?.TestName ?? string.Empty,
                Result      = ot.Result,
                OrderedDate = ot.Consultation?.Appointment?.AppointmentStart ?? DateTime.MinValue,
                ResultDate  = ot.ResultDate,
                DoctorName  = ot.Consultation?.Appointment?.Doctor?.FullName ?? string.Empty,
                HasReportFile = !string.IsNullOrWhiteSpace(ot.ReportFilePath),
                ReportFileName = ot.ReportFileName
            });
        }

        public async Task<IEnumerable<LabResultDto>> GetAllResultsAsync()
        {
            var tests = await _repository.GetAllOrderedTestsAsync();
            return tests.Select(MapToLabResultDto);
        }

        public async Task<LabResultDto> UploadReportFileAsync(
            int orderedTestId,
            IFormFile file,
            string? summary,
            int callerUserId,
            string callerRole,
            CancellationToken ct)
        {
            if (file == null || file.Length <= 0)
                throw new InvalidOperationException("Report file is required.");

            if (file.Length > 10 * 1024 * 1024)
                throw new InvalidOperationException("Report file is too large (max 10MB).");

            var ot = await _repository.GetOrderedTestByIdAsync(orderedTestId)
                ?? throw new KeyNotFoundException($"Ordered test with ID {orderedTestId} not found.");

            EnforceOrderedTestAccess(ot, callerUserId, callerRole);

            // Store under: <contentRoot>/uploads/lab-reports/{orderedTestId}/{guid}_{safeName}
            var baseDir = Path.Combine(_env.ContentRootPath, "uploads", "lab-reports", orderedTestId.ToString());
            Directory.CreateDirectory(baseDir);

            var safeName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "report";

            var fileName = $"{Guid.NewGuid():N}_{safeName}";
            var fullPath = Path.Combine(baseDir, fileName);

            await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fs, ct);
            }

            // If replacing an existing file, try to delete the old one
            if (!string.IsNullOrWhiteSpace(ot.ReportFilePath) && File.Exists(ot.ReportFilePath))
            {
                try { File.Delete(ot.ReportFilePath); } catch { /* best-effort */ }
            }

            ot.ReportFilePath = fullPath;
            ot.ReportFileName = safeName;
            ot.ReportContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
            ot.ReportSizeBytes = file.Length;

            // Mark completed when a report is uploaded
            ot.Status = "Completed";
            ot.ResultDate ??= DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(summary))
                ot.Result = summary.Trim();

            await _repository.SaveChangesAsync();

            await TrySendLabResultUploadedEmailAsync(ot);

            return MapToLabResultDto(ot);
        }

        public async Task<(Stream Stream, string ContentType, string FileName)> DownloadReportFileAsync(
            int orderedTestId,
            int callerUserId,
            string callerRole,
            CancellationToken ct)
        {
            var ot = await _repository.GetOrderedTestByIdAsync(orderedTestId)
                ?? throw new KeyNotFoundException($"Ordered test with ID {orderedTestId} not found.");

            EnforceOrderedTestAccess(ot, callerUserId, callerRole);

            var resolvedPath = ResolveStoredReportPath(ot);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
                throw new KeyNotFoundException("Report file not found.");

            var stream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = string.IsNullOrWhiteSpace(ot.ReportContentType) ? "application/octet-stream" : ot.ReportContentType;
            var fileName = string.IsNullOrWhiteSpace(ot.ReportFileName)
                ? Path.GetFileName(resolvedPath)
                : ot.ReportFileName;
            return (stream, contentType, fileName);
        }

        public async Task<(Stream Stream, string ContentType, string FileName)> DownloadPatientReportAsync(
            int orderedTestId,
            int callerUserId,
            string callerRole,
            CancellationToken ct)
        {
            var ot = await _repository.GetOrderedTestByIdAsync(orderedTestId)
                ?? throw new KeyNotFoundException($"Ordered test with ID {orderedTestId} not found.");

            EnforceOrderedTestAccess(ot, callerUserId, callerRole);

            var testType = (ot.LabTest?.TestType ?? "Single").Trim();
            var isPdfType = testType.Equals("Single", StringComparison.OrdinalIgnoreCase)
                            || testType.Equals("Multi", StringComparison.OrdinalIgnoreCase);

            if (isPdfType)
            {
                var pdf = await _pdfService.BuildLabReportPdfAsync(orderedTestId, callerUserId, callerRole);
                var stream = new MemoryStream(pdf);
                return (stream, "application/pdf", $"lab-report-{orderedTestId}.pdf");
            }

            var resolvedPath = ResolveStoredReportPath(ot);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
                throw new KeyNotFoundException("Report file not found.");

            var fileStream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = string.IsNullOrWhiteSpace(ot.ReportContentType) ? "application/octet-stream" : ot.ReportContentType;
            var fileName = string.IsNullOrWhiteSpace(ot.ReportFileName)
                ? Path.GetFileName(resolvedPath)
                : ot.ReportFileName;
            return (fileStream, contentType, fileName);
        }

        private string? ResolveStoredReportPath(Models.OrderedTest ot)
        {
            var orderedId = ot.OrderedTestId;
            var expectedDir = Path.Combine(_env.ContentRootPath, "uploads", "lab-reports", orderedId.ToString());

            // 1) Prefer DB absolute path when it exists and belongs to this ordered-test directory.
            if (!string.IsNullOrWhiteSpace(ot.ReportFilePath) && File.Exists(ot.ReportFilePath))
            {
                var full = Path.GetFullPath(ot.ReportFilePath);
                var expectedRoot = Path.GetFullPath(expectedDir) + Path.DirectorySeparatorChar;
                if (full.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase))
                    return full;
            }

            // 2) Try resolving by stored original filename inside the ordered-test folder.
            if (Directory.Exists(expectedDir) && !string.IsNullOrWhiteSpace(ot.ReportFileName))
            {
                var exact = Path.Combine(expectedDir, ot.ReportFileName);
                if (File.Exists(exact))
                    return exact;

                var prefixed = Directory.GetFiles(expectedDir, $"*_{ot.ReportFileName}")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(prefixed))
                    return prefixed;
            }

            // 3) Last resort: newest file in the ordered-test folder.
            if (Directory.Exists(expectedDir))
            {
                var newest = Directory.GetFiles(expectedDir)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(newest))
                    return newest;
            }

            return null;
        }

        private static void EnforceOrderedTestAccess(Models.OrderedTest ot, int callerUserId, string callerRole)
        {
            if (callerRole == "Admin")
                return;

            var appt = ot.Consultation?.Appointment
                ?? throw new InvalidOperationException("Appointment not loaded.");

            if (callerRole == "Doctor")
            {
                if (appt.Doctor?.UserId != callerUserId)
                    throw new UnauthorizedAccessException("You do not have permission to access this lab order.");
                return;
            }

            if (callerRole == "Patient")
            {
                if (appt.Patient?.UserId != callerUserId)
                    throw new UnauthorizedAccessException("You do not have permission to access this lab order.");
                return;
            }

            throw new UnauthorizedAccessException("Unsupported role.");
        }

        private async Task TrySendLabResultUploadedEmailAsync(Models.OrderedTest ot)
        {
            try
            {
                var appt = ot.Consultation?.Appointment;
                var patient = appt?.Patient;
                var email = patient?.User?.Email;
                if (string.IsNullOrWhiteSpace(email))
                    return;

                var patientName = patient?.FullName ?? "Patient";
                var doctorName = appt?.Doctor?.FullName ?? "Doctor";
                var testName = ot.LabTest?.TestName ?? $"Test #{ot.LabTestId}";

                await _emailService.SendLabResultUploadedAsync(
                    email, patientName, doctorName, testName, ot.ResultDate ?? DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send lab result uploaded email for OrderedTest {Id}.", ot.OrderedTestId);
            }
        }
    }
}
