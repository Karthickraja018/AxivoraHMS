using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class DoctorScheduleService : IDoctorScheduleService
    {
        private readonly IDoctorScheduleRepository _repository;

        public DoctorScheduleService(IDoctorScheduleRepository repository)
        {
            _repository = repository;
        }

        public async Task<DoctorScheduleDto> CreateScheduleAsync(int doctorId, CreateScheduleDto dto)
        {
            var doctor = await _repository.GetDoctorByIdAsync(doctorId);

            if (doctor == null)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            if (dto.EndTime <= dto.StartTime)
                throw new ArgumentException("EndTime must be after StartTime.");

            var existingActiveSchedules = await _repository.GetActiveSiblingSchedulesAsync(doctorId, dto.DayOfWeek);

            if (existingActiveSchedules.Any(s => s.StartTime < dto.EndTime && s.EndTime > dto.StartTime))
                throw new InvalidOperationException(
                    $"A schedule for {(System.DayOfWeek)dto.DayOfWeek} already overlaps with the requested time range.");

            var schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                SlotDurationMinutes = dto.SlotDurationMinutes,
                IsActive = true
            };

            await _repository.AddScheduleAsync(schedule);
            await _repository.SaveChangesAsync();

            return MapToDto(schedule, doctor.FullName);
        }

        public async Task<IEnumerable<DoctorScheduleDto>> GetSchedulesByDoctorAsync(int doctorId)
        {
            var doctor = await _repository.GetDoctorByIdAsync(doctorId);

            if (doctor == null)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            var schedules = await _repository.GetByDoctorIdAsync(doctorId);

            return schedules
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .Select(s => MapToDto(s, doctor.FullName));
        }

        public async Task<DoctorScheduleDto> UpdateScheduleAsync(int scheduleId, UpdateScheduleDto dto, int callerUserId, string callerRole)
        {
            var schedule = await _repository.GetByIdWithDoctorAsync(scheduleId);

            if (schedule == null)
                throw new KeyNotFoundException($"Schedule with ID {scheduleId} not found.");

            if (callerRole != "Admin")
            {
                var callerDoctor = await _repository.GetDoctorByUserIdAsync(callerUserId);

                if (callerDoctor == null || schedule.DoctorId != callerDoctor.DoctorId)
                    throw new UnauthorizedAccessException(
                        "You are not authorized to modify another doctor's schedule.");
            }

            var newDay = dto.DayOfWeek ?? schedule.DayOfWeek;
            var newStart = dto.StartTime ?? schedule.StartTime;
            var newEnd = dto.EndTime ?? schedule.EndTime;

            if (newEnd <= newStart)
                throw new ArgumentException("EndTime must be after StartTime.");

            var siblings = await _repository.GetActiveSiblingSchedulesAsync(schedule.DoctorId, newDay, scheduleId);

            if (siblings.Any(s => s.StartTime < newEnd && s.EndTime > newStart))
                throw new InvalidOperationException(
                    $"Updating this schedule would overlap with an existing schedule on {(System.DayOfWeek)newDay}.");

            if (dto.DayOfWeek.HasValue) schedule.DayOfWeek = dto.DayOfWeek.Value;
            if (dto.StartTime.HasValue) schedule.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) schedule.EndTime = dto.EndTime.Value;
            if (dto.SlotDurationMinutes.HasValue) schedule.SlotDurationMinutes = dto.SlotDurationMinutes.Value;
            if (dto.IsActive.HasValue) schedule.IsActive = dto.IsActive.Value;

            await _repository.SaveChangesAsync();

            return MapToDto(schedule, schedule.Doctor!.FullName);
        }

        public async Task DeleteScheduleAsync(int scheduleId, int callerUserId, string callerRole)
        {
            var schedule = await _repository.GetByIdAsync(scheduleId);

            if (schedule == null)
                throw new KeyNotFoundException($"Schedule with ID {scheduleId} not found.");

            if (callerRole != "Admin")
            {
                var callerDoctor = await _repository.GetDoctorByUserIdAsync(callerUserId);

                if (callerDoctor == null || schedule.DoctorId != callerDoctor.DoctorId)
                    throw new UnauthorizedAccessException(
                        "You are not authorized to delete another doctor's schedule.");
            }

            await _repository.RemoveScheduleAsync(schedule);
            await _repository.SaveChangesAsync();
        }

        private static DoctorScheduleDto MapToDto(DoctorSchedule schedule, string doctorName) => new()
        {
            ScheduleId = schedule.ScheduleId,
            DoctorId = schedule.DoctorId,
            DoctorName = doctorName,
            DayOfWeek = schedule.DayOfWeek,
            DayName = ((DayOfWeek)schedule.DayOfWeek).ToString(),
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            SlotDurationMinutes = schedule.SlotDurationMinutes,
            IsActive = schedule.IsActive,
            GeneratedSlots = GenerateSlots(schedule.StartTime, schedule.EndTime, schedule.SlotDurationMinutes)
        };

        private static List<string> GenerateSlots(TimeSpan start, TimeSpan end, int slotDurationMinutes)
        {
            var slots = new List<string>();
            var current = start;

            while (current.Add(TimeSpan.FromMinutes(slotDurationMinutes)) <= end)
            {
                var slotEnd = current.Add(TimeSpan.FromMinutes(slotDurationMinutes));
                slots.Add($"{current:hh\\:mm} - {slotEnd:hh\\:mm}");
                current = slotEnd;
            }

            return slots;
        }
    }
}
