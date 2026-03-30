using Microsoft.Extensions.Options;
using Axivora.Configuration;
using Axivora.Models;
using Axivora.Repositories.Interfaces;
using Axivora.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Axivora.Services
{
    public class PdfService : IPdfService
    {
        private readonly IConsultationRepository _consultationRepository;
        private readonly ILabTestRepository _labTestRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly HospitalPdfSettings _hospital;

        static PdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public PdfService(
            IConsultationRepository consultationRepository,
            ILabTestRepository labTestRepository,
            IPatientRepository patientRepository,
            IOptions<HospitalPdfSettings> hospitalOptions)
        {
            _consultationRepository = consultationRepository;
            _labTestRepository        = labTestRepository;
            _patientRepository        = patientRepository;
            _hospital                 = hospitalOptions.Value;
        }

        public async Task<byte[]> BuildPrescriptionPdfAsync(int consultationId, int callerUserId, string callerRole)
        {
            var c = await _consultationRepository.GetByIdAsync(consultationId)
                ?? throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            await EnforceConsultationAccessAsync(c, callerUserId, callerRole);

            var appt   = c.Appointment ?? throw new InvalidOperationException("Appointment not loaded.");
            var doctor = appt.Doctor ?? throw new InvalidOperationException("Doctor not loaded.");
            var patient = appt.Patient ?? throw new InvalidOperationException("Patient not loaded.");

            var reportDate = DateOnly.FromDateTime(appt.AppointmentStart.Kind == DateTimeKind.Utc
                ? appt.AppointmentStart.ToLocalTime()
                : appt.AppointmentStart);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    page.Header().Column(col =>
                    {
                        col.Item().Text(_hospital.Name).Bold().FontSize(18).FontColor(Colors.Blue.Darken3);
                        col.Item().Text(_hospital.Address).FontSize(9).FontColor(Colors.Grey.Darken2);
                        col.Item().Text($"Tel: {_hospital.Phone}").FontSize(9);
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(12).Column(main =>
                    {
                        main.Item().Text("PRESCRIPTION").Bold().FontSize(14);

                        main.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(docCol =>
                            {
                                docCol.Item().Text($"Dr. {doctor.FullName}").Bold();
                                docCol.Item().Text($"Qualification: {doctor.Qualification ?? "—"}").FontSize(10);
                                docCol.Item().Text($"Registration: {doctor.LicenseNumber}").FontSize(10);
                            });
                            row.RelativeItem().Column(patCol =>
                            {
                                patCol.Item().Text($"Patient: {patient.FullName}").Bold();
                                patCol.Item().Text(
                                        $"Age / Gender: {AgeYears(patient.DateOfBirth)} yrs / {patient.Gender ?? "—"}")
                                    .FontSize(10);
                                patCol.Item().Text($"Date: {reportDate:yyyy-MM-dd}").FontSize(10);
                            });
                        });

                        main.Item().PaddingTop(12).Text("Clinical").Bold().FontSize(11);
                        main.Item().PaddingTop(4).Text($"Symptoms / chief complaint: {c.ChiefComplaint ?? "—"}")
                            .FontSize(10);
                        main.Item().Text($"Diagnosis: {c.DiagnosisNotes ?? "—"}").FontSize(10);

                        main.Item().PaddingTop(12).Text("Medicines").Bold().FontSize(11);
                        main.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            IContainer CellStyle(IContainer x) =>
                                x.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4);

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Medicine").Bold();
                                header.Cell().Element(CellStyle).Text("Dosage").Bold();
                                header.Cell().Element(CellStyle).Text("Food").Bold();
                                header.Cell().Element(CellStyle).Text("Duration").Bold();
                            });

                            foreach (var p in c.Prescriptions.OrderBy(x => x.PrescriptionId))
                            {
                                var medName = p.Medicine?.MedicineName ?? "—";
                                var dosage    = string.IsNullOrWhiteSpace(p.Dosage) ? p.Frequency ?? "—" : p.Dosage;
                                if (!string.IsNullOrWhiteSpace(p.Frequency) && dosage != p.Frequency)
                                    dosage = $"{dosage} ({p.Frequency})";

                                var food = string.IsNullOrWhiteSpace(p.Route)
                                    ? (p.Instructions?.Contains("AF", StringComparison.OrdinalIgnoreCase) == true
                                        ? "AF"
                                        : p.Instructions?.Contains("BF", StringComparison.OrdinalIgnoreCase) == true
                                            ? "BF"
                                            : "—")
                                    : p.Route;

                                var duration = p.DurationDays.HasValue ? $"{p.DurationDays} day(s)" : "—";

                                table.Cell().Element(CellStyle).Text(medName).FontSize(9);
                                table.Cell().Element(CellStyle).Text(dosage).FontSize(9);
                                table.Cell().Element(CellStyle).Text(food).FontSize(9);
                                table.Cell().Element(CellStyle).Text(duration).FontSize(9);
                            }
                        });

                        main.Item().PaddingTop(12).Text("Recommended tests").Bold().FontSize(11);
                        if (c.OrderedTests.Count == 0)
                            main.Item().Text("—").FontSize(10);
                        else
                            main.Item().Text(string.Join(", ",
                                    c.OrderedTests.Select(o => o.LabTest?.TestName ?? $"Test #{o.LabTestId}")))
                                .FontSize(10);
                    });

                    page.Footer().Column(foot =>
                    {
                        foot.Item().PaddingTop(16).Text("_______________________________").FontSize(10);
                        foot.Item().Text($"Dr. {doctor.FullName}").FontSize(9);
                        foot.Item().PaddingTop(8).AlignCenter().Text("Computer generated prescription")
                            .Italic().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf();

            return pdf;
        }

        public async Task<byte[]> BuildLabReportPdfAsync(int orderedTestId, int callerUserId, string callerRole)
        {
            var ot = await _labTestRepository.GetOrderedTestByIdAsync(orderedTestId)
                ?? throw new KeyNotFoundException($"Lab order with ID {orderedTestId} not found.");

            var consultation = ot.Consultation
                ?? throw new InvalidOperationException("Consultation not loaded.");
            var appt = consultation.Appointment
                ?? throw new InvalidOperationException("Appointment not loaded.");
            var patient = appt.Patient
                ?? throw new InvalidOperationException("Patient not loaded.");

            await EnforceConsultationAccessAsync(consultation, callerUserId, callerRole);

            var doctor = appt.Doctor;
            var lt     = ot.LabTest;

            var resultDate = ot.ResultDate ?? appt.AppointmentStart;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"{_hospital.Name} — Lab Report").Bold().FontSize(16).FontColor(Colors.Blue.Darken3);
                        col.Item().Text(_hospital.Address).FontSize(9);
                        col.Item().Text($"Tel: {_hospital.Phone}").FontSize(9);
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(16).Column(main =>
                    {
                        main.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c1 =>
                            {
                                c1.Item().Text($"Patient: {patient.FullName}").Bold();
                                c1.Item().Text($"Age / Gender: {AgeYears(patient.DateOfBirth)} / {patient.Gender ?? "—"}")
                                    .FontSize(10);
                                c1.Item().Text($"Date: {DateOnly.FromDateTime(resultDate):yyyy-MM-dd}").FontSize(10);
                            });
                            row.RelativeItem().Column(c2 =>
                            {
                                c2.Item().Text(
                                        $"Referring doctor: {doctor?.FullName ?? "—"}")
                                    .FontSize(10);
                            });
                        });

                        main.Item().PaddingTop(16).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn(2);
                            });

                            IContainer Cell(IContainer x) =>
                                x.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4);

                            table.Header(header =>
                            {
                                header.Cell().Element(Cell).Text("Test").Bold();
                                header.Cell().Element(Cell).Text("Result").Bold();
                                header.Cell().Element(Cell).Text("Unit").Bold();
                                header.Cell().Element(Cell).Text("Reference range").Bold();
                            });

                            table.Cell().Element(Cell).Text(lt?.TestName ?? "—").FontSize(9);
                            table.Cell().Element(Cell).Text(string.IsNullOrWhiteSpace(ot.Result) ? "—" : ot.Result).FontSize(9);
                            table.Cell().Element(Cell).Text(lt?.Unit ?? "—").FontSize(9);
                            table.Cell().Element(Cell).Text(lt?.ReferenceRange ?? "—").FontSize(9);
                        });
                    });

                    page.Footer().Column(foot =>
                    {
                        foot.Item().PaddingTop(24).Text("_______________________________").FontSize(10);
                        foot.Item().Text(_hospital.LabTechnicianSignatureLabel).FontSize(9);
                    });
                });
            }).GeneratePdf();

            return pdf;
        }

        private async Task EnforceConsultationAccessAsync(Consultation c, int callerUserId, string callerRole)
        {
            var appt = c.Appointment ?? throw new InvalidOperationException("Appointment not loaded.");

            if (callerRole == "Admin")
                return;

            if (callerRole == "Doctor")
            {
                var doctor = await _consultationRepository.GetDoctorByUserIdAsync(callerUserId);
                if (doctor is null || appt.DoctorId != doctor.DoctorId)
                    throw new UnauthorizedAccessException();
                return;
            }

            if (callerRole == "Patient")
            {
                var patient = await _patientRepository.GetByUserIdAsync(callerUserId);
                if (patient is null || appt.PatientId != patient.PatientId)
                    throw new UnauthorizedAccessException();
                return;
            }

            throw new UnauthorizedAccessException();
        }

        private static int AgeYears(DateOnly dob)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var age   = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;
            return Math.Max(0, age);
        }
    }
}
