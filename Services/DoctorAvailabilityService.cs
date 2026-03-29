using AutoMapper;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class DoctorAvailabilityService : IDoctorAvailabilityService
    {
        private readonly IAvailabilityTemplateRepository _templateRepository;
        private readonly IAvailabilityDayRepository _dayRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DoctorAvailabilityService> _logger;

        public DoctorAvailabilityService(
            IAvailabilityTemplateRepository templateRepository,
            IAvailabilityDayRepository dayRepository,
            IMapper mapper,
            ILogger<DoctorAvailabilityService> logger)
        {
            _templateRepository = templateRepository;
            _dayRepository      = dayRepository;
            _mapper             = mapper;
            _logger             = logger;
        }

        public async Task<IEnumerable<AvailabilityDayDto>> GetAvailabilityDaysAsync(int doctorId)
        {
            var days = await _dayRepository.GetByDoctorIdAsync(doctorId);
            return _mapper.Map<IEnumerable<AvailabilityDayDto>>(days);
        }

        public async Task<AvailabilityDayDto> UpdateDayStatusAsync(
            int dayId, UpdateAvailabilityDayStatusDto dto)
        {
            var day = await _dayRepository.GetByIdWithSlotsAsync(dayId);
            if (day is null)
                throw new KeyNotFoundException($"Availability day with ID {dayId} not found.");

            var previousStatus = day.Status;
            day.Status = dto.Status;

            // Block all slots when the day becomes unavailable
            if (dto.Status is AvailabilityDayStatus.Leave or AvailabilityDayStatus.Holiday
                           or AvailabilityDayStatus.Closed)
            {
                foreach (var slot in day.Slots.Where(s => s.Status == SlotStatus.Available))
                    slot.Status = SlotStatus.Blocked;

                _logger.LogInformation(
                    "Blocked {Count} slots for availability day {DayId} (status changed to {Status}).",
                    day.Slots.Count(s => s.Status == SlotStatus.Blocked), dayId, dto.Status);
            }
            // Restore blocked slots when day is re-opened
            else if (dto.Status == AvailabilityDayStatus.Open
                     && previousStatus != AvailabilityDayStatus.Open)
            {
                foreach (var slot in day.Slots.Where(s => s.Status == SlotStatus.Blocked))
                    slot.Status = SlotStatus.Available;

                _logger.LogInformation(
                    "Restored slots for availability day {DayId} (re-opened).", dayId);
            }

            await _dayRepository.SaveChangesAsync();
            return _mapper.Map<AvailabilityDayDto>(day);
        }

        /// <summary>
        /// Generates <see cref="DoctorAvailabilityDay"/> records for the next
        /// <paramref name="daysAhead"/> days based on all active templates.
        /// If <paramref name="doctorId"/> is provided, only generates for that specific doctor.
        ///
        /// Slot generation is deliberately omitted here Ã¢â‚¬â€ slots are created lazily the first
        /// time a caller requests them via <see cref="ISlotService.EnsureSlotsGeneratedAsync"/>.
        /// This avoids the performance cost of generating slots for every doctor nightly.
        /// </summary>
        public async Task GenerateAvailabilityDaysAsync(int? doctorId = null, int daysAhead = 30)
        {
            var today     = DateOnly.FromDateTime(DateTime.UtcNow);
            var endDate   = today.AddDays(daysAhead);
            var templates = await _templateRepository.GetActiveTemplatesAsync();

            if (doctorId.HasValue)
                templates = templates.Where(t => t.DoctorId == doctorId.Value).ToList();

            // Group templates by doctor to manage existence checks in batches
            var templatesByDoctor = templates.GroupBy(t => t.DoctorId);

            foreach (var doctorGroup in templatesByDoctor)
            {
                var doctorIdNum = doctorGroup.Key;
                // Pre-fetch ONLY existing days for this doctor — avoiding expensive slot includes
                var existingDays = await _dayRepository.GetByDoctorAndDateRangeNoSlotsAsync(doctorIdNum, today, endDate);
                var existingDaysMap = existingDays
                    .GroupBy(d => d.Date)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var template in doctorGroup)
                {
                    for (var date = today; date <= endDate; date = date.AddDays(1))
                    {
                        if ((int)date.DayOfWeek != template.DayOfWeek)
                            continue;

                        if (date < template.EffectiveFromDate)
                            continue;

                        if (template.EffectiveToDate.HasValue && date > template.EffectiveToDate.Value)
                            continue;

                        // Check if a day record with this template ALREADY exists for this date
                        existingDaysMap.TryGetValue(date, out var daysOnDate);
                        var existingDay = daysOnDate?.FirstOrDefault(d => d.SourceTemplateId == template.Id);

                        if (existingDay != null)
                        {
                            // If it exists from this template, we skip to avoid duplicate work.
                            continue;
                        }

                        var day = new DoctorAvailabilityDay
                        {
                            DoctorId            = doctorIdNum,
                            Date                = date,
                            StartTime           = template.StartTime,
                            EndTime             = template.EndTime,
                            SlotDurationMinutes = template.SlotDurationMinutes,
                            Status              = AvailabilityDayStatus.Open,
                            SourceTemplateId    = template.Id,
                            CreatedAt           = DateTime.UtcNow
                        };

                        await _dayRepository.AddAsync(day);
                        
                        _logger.LogDebug(
                            "Queued availability day {Date} for doctor {DoctorIdNum} from template {TemplateId}.",
                            date, doctorIdNum, template.Id);
                    }
                }
            }

            // Sync all changes in a single transaction
            await _dayRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Availability day generation complete for window {Today} Ã¢â‚¬â€œ {End}.", today, endDate);
        }
    }
}

