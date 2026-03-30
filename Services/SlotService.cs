using AutoMapper;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class SlotService : ISlotService
    {
        private readonly IDoctorAvailabilityService _availabilityService;
        private readonly IAppointmentSlotRepository _slotRepository;
        private readonly IAvailabilityDayRepository _dayRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SlotService> _logger;
        private static readonly SemaphoreSlim _generationLock = new SemaphoreSlim(1, 1);

        public SlotService(
            IAppointmentSlotRepository slotRepository,
            IAvailabilityDayRepository dayRepository,
            IDoctorAvailabilityService availabilityService,
            IMapper mapper,
            ILogger<SlotService> logger)
        {
            _slotRepository      = slotRepository;
            _dayRepository       = dayRepository;
            _availabilityService = availabilityService;
            _mapper              = mapper;
            _logger              = logger;
        }

        /// <summary>
        /// Returns all Available slots for a doctor on a given date.
        /// Generates slots on demand if the availability day exists but has no slots yet,
        /// so callers never receive an empty list due to a missed background run.
        /// </summary>
        public async Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(int doctorId, DateOnly date)
        {
            // Trigger on-demand generation before querying â€” idempotent if slots already exist
            await EnsureSlotsGeneratedAsync(doctorId, date);

            var slots = await _slotRepository.GetAvailableSlotsByDoctorAndDateAsync(doctorId, date);
            return _mapper.Map<IEnumerable<SlotDto>>(slots);
        }

        /// <summary>
        /// Ensures slot records exist for the given doctor and date.
        /// If the availability day exists but has no slots yet, generates them now.
        /// Idempotent â€” safe to call repeatedly; no slots are created twice.
        /// </summary>
        public async Task EnsureSlotsGeneratedAsync(int doctorId, DateOnly date)
        {
            // Acquire lock to avoid concurrent generation for the same doctor/date
            await _generationLock.WaitAsync();
            try
            {
                // Always ensure day records exist for this doctor/date before fetching them.
                // This ensures that if a new second shift (e.g. Evening) was added to a day that already has 
                // a first shift (e.g. Morning), both are considered.
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var daysAhead = date > today ? (date.DayNumber - today.DayNumber) : 0;
                daysAhead = Math.Min(daysAhead, 366);
                await _availabilityService.GenerateAvailabilityDaysAsync(doctorId: doctorId, daysAhead: daysAhead);

                var days = (await _dayRepository.GetByDoctorAndDateRangeAsync(doctorId, date, date)).ToList();

                // Still empty? No availability configured for this doctor/date.
                if (!days.Any())
                    return;

                _logger.LogDebug(
                    "Ensuring slots for Doctor {DoctorId} on {Date}. Found {ShiftCount} shifts.",
                    doctorId, date, days.Count);

                foreach (var day in days)
                {
                    // Check if slots are truly empty — this is now protected by the semaphore
                    if (!await _slotRepository.AnyExistForDayAsync(day.Id))
                    {
                        _logger.LogInformation(
                            "Generating slots for Shift {ShiftId} ({Start}-{End}) for Doctor {DoctorId} on {Date}.",
                            day.Id, day.StartTime, day.EndTime, doctorId, date);

                        await GenerateSlotsForDayAsync(day.Id);
                    }
                    else
                    {
                        _logger.LogDebug("Slots already exist for Shift {ShiftId} on {Date}.", day.Id, date);
                    }
                }
            }
            finally
            {
                _generationLock.Release();
            }
        }

        public async Task<SlotDetailDto> GetSlotDetailAsync(int slotId)
        {
            var slot = await _slotRepository.GetByIdAsync(slotId)
                ?? throw new KeyNotFoundException($"Slot with ID {slotId} not found.");

            return new SlotDetailDto
            {
                SlotId        = slot.Id,
                DoctorId      = slot.DoctorId,
                SlotStart     = slot.SlotStart,
                SlotEnd       = slot.SlotEnd,
                Status        = slot.Status,
                AppointmentId = slot.AppointmentId
            };
        }

        public async Task<SlotDetailDto> UpdateSlotStatusAsync(int slotId, UpdateSlotStatusDto dto)
        {
            var slot = await _slotRepository.GetByIdAsync(slotId)
                ?? throw new KeyNotFoundException($"Slot with ID {slotId} not found.");

            slot.Status = dto.Status;
            await _slotRepository.SaveChangesAsync();

            _logger.LogInformation("Admin updated slot {SlotId} status to {Status}.", slotId, dto.Status);

            return new SlotDetailDto
            {
                SlotId        = slot.Id,
                DoctorId      = slot.DoctorId,
                SlotStart     = slot.SlotStart,
                SlotEnd       = slot.SlotEnd,
                Status        = slot.Status,
                AppointmentId = slot.AppointmentId
            };
        }

        public async Task<IEnumerable<DoctorCalendarDayDto>> GetDoctorCalendarAsync(
            int doctorId, DateOnly from, DateOnly to)
        {
            // PROACTIVE: Ensure days exist (from templates) for this range.
            await _availabilityService.GenerateAvailabilityDaysAsync(doctorId: doctorId);

            var slots = await _slotRepository.GetSlotsByDoctorAndDateRangeAsync(doctorId, from, to);
            var days  = await _dayRepository.GetByDoctorAndDateRangeAsync(doctorId, from, to);

            // Group shifts by date to handle multiple templates on the same day (e.g. Morning/Evening)
            var shiftsByDate = days
                .GroupBy(d => d.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var slotsByDate = slots
                .GroupBy(s => DateOnly.FromDateTime(s.SlotStart))
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<DoctorCalendarDayDto>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                shiftsByDate.TryGetValue(date, out var dayShifts);
                slotsByDate.TryGetValue(date, out var daySlots);

                // If multiple shifts exist, pick the "most open" status
                // Logic: if any shift is Open, day is Open. If all Closed, Closed. If any Leave, Leave (takes priority if blocked).
                string status = "NoSchedule";
                if (dayShifts != null && dayShifts.Any())
                {
                    if (dayShifts.Any(s => s.Status == AvailabilityDayStatus.Leave)) status = AvailabilityDayStatus.Leave;
                    else if (dayShifts.Any(s => s.Status == AvailabilityDayStatus.Open)) status = AvailabilityDayStatus.Open;
                    else status = dayShifts.First().Status;
                }

                result.Add(new DoctorCalendarDayDto
                {
                    Date           = date,
                    DayStatus      = status,
                    TotalSlots     = daySlots?.Count ?? 0,
                    AvailableSlots = daySlots?.Count(s => s.Status == SlotStatus.Available) ?? 0,
                    BookedSlots    = daySlots?.Count(s => s.Status == SlotStatus.Booked) ?? 0
                });
            }

            return result;
        }

        public async Task<IEnumerable<PatientAvailabilityPreviewDto>> GetAvailabilityPreviewAsync(
            int doctorId, DateOnly from, DateOnly to)
        {
            // Option A: compute month preview from availability-day rows (template-derived),
            // not from persisted slot rows. This makes new templates show immediately.

            // Ensure availability days exist up to the requested range.
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var daysAhead = to > today ? (to.DayNumber - today.DayNumber) : 0;
            // Guard against accidental huge ranges
            daysAhead = Math.Min(daysAhead, 366);
            await _availabilityService.GenerateAvailabilityDaysAsync(doctorId: doctorId, daysAhead: daysAhead);

            var days = (await _dayRepository.GetByDoctorAndDateRangeNoSlotsAsync(doctorId, from, to))
                .ToList();

            static int ComputeSlots(TimeSpan start, TimeSpan end, int durationMinutes)
            {
                if (durationMinutes <= 0) return 0;
                var minutes = (end - start).TotalMinutes;
                if (minutes <= 0) return 0;
                return (int)Math.Floor(minutes / durationMinutes);
            }

            // Sum counts per date to support multiple shifts in one day.
            var countsByDate = days
                .Where(d => d.Status == AvailabilityDayStatus.Open)
                .GroupBy(d => d.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(d => ComputeSlots(d.StartTime, d.EndTime, d.SlotDurationMinutes))
                );

            var result = new List<PatientAvailabilityPreviewDto>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var count = countsByDate.TryGetValue(date, out var c) ? c : 0;
                result.Add(new PatientAvailabilityPreviewDto
                {
                    Date           = date,
                    AvailableCount = count,
                    AvailableSlots = count,
                });
            }

            return result;
        }

        public async Task ApplyLeaveAsync(int doctorId, DoctorLeaveDto dto)
        {
            var days = await _dayRepository.GetByDoctorAndDateRangeAsync(doctorId, dto.From, dto.To);

            if (!days.Any())
                throw new KeyNotFoundException(
                    $"No availability days found for doctor {doctorId} between {dto.From} and {dto.To}.");

            foreach (var day in days)
            {
                day.Status = AvailabilityDayStatus.Leave;

                foreach (var slot in day.Slots.Where(s => s.Status == SlotStatus.Available))
                    slot.Status = SlotStatus.Blocked;
            }

            await _dayRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Applied leave for doctor {DoctorId} from {From} to {To}. Reason: {Reason}",
                doctorId, dto.From, dto.To, dto.Reason ?? "N/A");
        }

        /// <summary>
        /// Generates and persists AppointmentSlot records for a given availability day.
        /// Idempotent â€” skips generation if slots already exist for the day.
        /// </summary>
        public async Task GenerateSlotsForDayAsync(int availabilityDayId)
        {
            // Idempotent â€” do nothing if slots already exist
            if (await _slotRepository.AnyExistForDayAsync(availabilityDayId))
            {
                _logger.LogDebug(
                    "Slots already exist for availability day {DayId}. Skipping generation.", availabilityDayId);
                return;
            }

            var day = await _dayRepository.GetByIdAsync(availabilityDayId);
            if (day is null)
                throw new KeyNotFoundException(
                    $"Availability day with ID {availabilityDayId} not found.");

            var slots = BuildSlots(day);
            if (!slots.Any())
            {
                _logger.LogWarning(
                    "No slots generated for availability day {DayId} " +
                    "(start={Start}, end={End}, duration={Duration}min).",
                    availabilityDayId, day.StartTime, day.EndTime, day.SlotDurationMinutes);
                return;
            }

            await _slotRepository.AddRangeAsync(slots);
            await _slotRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Generated {Count} slots for availability day {DayId} ({Date}).",
                slots.Count, availabilityDayId, day.Date);
        }

        // Private helpers

        private static List<AppointmentSlot> BuildSlots(DoctorAvailabilityDay day)
        {
            var slots    = new List<AppointmentSlot>();
            var slotSpan = TimeSpan.FromMinutes(day.SlotDurationMinutes);
            var dayBase  = day.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var current  = day.StartTime;

            while (current + slotSpan <= day.EndTime)
            {
                slots.Add(new AppointmentSlot
                {
                    DoctorId          = day.DoctorId,
                    AvailabilityDayId = day.Id,
                    SlotStart         = dayBase + current,
                    SlotEnd           = dayBase + current + slotSpan,
                    Status            = SlotStatus.Available
                });

                current += slotSpan;
            }

            return slots;
        }
    }
}
