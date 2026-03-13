using AutoMapper;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class SlotService : ISlotService
    {
        private readonly IAppointmentSlotRepository _slotRepository;
        private readonly IAvailabilityDayRepository _dayRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SlotService> _logger;

        public SlotService(
            IAppointmentSlotRepository slotRepository,
            IAvailabilityDayRepository dayRepository,
            IMapper mapper,
            ILogger<SlotService> logger)
        {
            _slotRepository = slotRepository;
            _dayRepository  = dayRepository;
            _mapper         = mapper;
            _logger         = logger;
        }

        /// <summary>
        /// Returns all Available slots for a doctor on a given date.
        /// Generates slots on demand if the availability day exists but has no slots yet,
        /// so callers never receive an empty list due to a missed background run.
        /// </summary>
        public async Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(int doctorId, DateOnly date)
        {
            // Trigger on-demand generation before querying — idempotent if slots already exist
            await EnsureSlotsGeneratedAsync(doctorId, date);

            var slots = await _slotRepository.GetAvailableSlotsByDoctorAndDateAsync(doctorId, date);
            return _mapper.Map<IEnumerable<SlotDto>>(slots);
        }

        /// <summary>
        /// Ensures slot records exist for the given doctor and date.
        /// If the availability day exists but has no slots yet, generates them now.
        /// Idempotent — safe to call repeatedly; no slots are created twice.
        /// </summary>
        public async Task EnsureSlotsGeneratedAsync(int doctorId, DateOnly date)
        {
            var day = await _dayRepository.GetByDoctorAndDateAsync(doctorId, date);

            // No availability day configured for this doctor/date — nothing to generate
            if (day is null)
                return;

            // Day exists but slots have not been generated yet — generate now
            if (!day.Slots.Any())
            {
                _logger.LogInformation(
                    "On-demand slot generation triggered for doctor {DoctorId} on {Date}.",
                    doctorId, date);

                await GenerateSlotsForDayAsync(day.Id);
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
            var slots = await _slotRepository.GetSlotsByDoctorAndDateRangeAsync(doctorId, from, to);
            var days  = await _dayRepository.GetByDoctorAndDateRangeAsync(doctorId, from, to);

            var dayStatusMap = days.ToDictionary(d => d.Date, d => d.Status);

            var grouped = slots
                .GroupBy(s => DateOnly.FromDateTime(s.SlotStart))
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<DoctorCalendarDayDto>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                grouped.TryGetValue(date, out var daySlots);
                dayStatusMap.TryGetValue(date, out var dayStatus);

                result.Add(new DoctorCalendarDayDto
                {
                    Date           = date,
                    DayStatus      = dayStatus ?? "NoSchedule",
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
            var slots = await _slotRepository.GetAvailableSlotsByDoctorAndDateRangeAsync(doctorId, from, to);

            var grouped = slots
                .GroupBy(s => DateOnly.FromDateTime(s.SlotStart))
                .ToDictionary(g => g.Key, g => g.Count());

            var result = new List<PatientAvailabilityPreviewDto>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                result.Add(new PatientAvailabilityPreviewDto
                {
                    Date           = date,
                    AvailableSlots = grouped.TryGetValue(date, out var count) ? count : 0
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
        /// Idempotent — skips generation if slots already exist for the day.
        /// </summary>
        public async Task GenerateSlotsForDayAsync(int availabilityDayId)
        {
            // Idempotent — do nothing if slots already exist
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
