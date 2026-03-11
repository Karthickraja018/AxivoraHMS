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

        public async Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(int doctorId, DateOnly date)
        {
            var slots = await _slotRepository.GetAvailableSlotsByDoctorAndDateAsync(doctorId, date);
            return _mapper.Map<IEnumerable<SlotDto>>(slots);
        }

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

        // ?? Private helpers ??????????????????????????????????????????????????

        /// <summary>
        /// Builds AppointmentSlot objects from a DoctorAvailabilityDay.
        /// Slots are aligned to SlotDurationMinutes boundaries from StartTime to EndTime.
        /// </summary>
        private static List<AppointmentSlot> BuildSlots(DoctorAvailabilityDay day)
        {
            var slots     = new List<AppointmentSlot>();
            var slotSpan  = TimeSpan.FromMinutes(day.SlotDurationMinutes);
            // Combine the calendar date with the time-of-day to produce a full DateTime (UTC)
            var dayBase   = day.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var current   = day.StartTime;

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
