using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class DoctorScheduleService : IDoctorScheduleService
    {
        private readonly AxivoraDbContext _context;

        public DoctorScheduleService(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<DoctorScheduleDto> CreateScheduleAsync(int doctorId, CreateScheduleDto dto)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.IsDeleted);

            if (doctor == null)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            if (dto.EndTime <= dto.StartTime)
                throw new ArgumentException("EndTime must be after StartTime.");

            var existingActiveSchedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == doctorId && s.IsActive && s.DayOfWeek == dto.DayOfWeek)
                .ToListAsync();

            var hasOverlap = existingActiveSchedules
                .Any(s => s.StartTime < dto.EndTime && s.EndTime > dto.StartTime);

            if (hasOverlap)
                throw new InvalidOperationException(
                    $"A schedule for {(DayOfWeek)dto.DayOfWeek} already overlaps with the requested time range.");

            var schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                SlotDurationMinutes = dto.SlotDurationMinutes,
                IsActive = true
            };

            _context.DoctorSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return MapToDto(schedule, doctor.FullName);
        }

        public async Task<IEnumerable<DoctorScheduleDto>> GetSchedulesByDoctorAsync(int doctorId)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.IsDeleted);

            if (doctor == null)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            var schedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == doctorId)
                .ToListAsync();

            return schedules
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .Select(s => MapToDto(s, doctor.FullName));
        }

        public async Task<DoctorScheduleDto> UpdateScheduleAsync(int scheduleId, UpdateScheduleDto dto)
        {
            var schedule = await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

            if (schedule == null)
                throw new KeyNotFoundException($"Schedule with ID {scheduleId} not found.");

            var newDay = dto.DayOfWeek ?? schedule.DayOfWeek;
            var newStart = dto.StartTime ?? schedule.StartTime;
            var newEnd = dto.EndTime ?? schedule.EndTime;

            if (newEnd <= newStart)
                throw new ArgumentException("EndTime must be after StartTime.");

            var siblingsOnDay = await _context.DoctorSchedules
                .Where(s => s.DoctorId == schedule.DoctorId
                    && s.ScheduleId != scheduleId
                    && s.IsActive
                    && s.DayOfWeek == newDay)
                .ToListAsync();

            var hasOverlap = siblingsOnDay
                .Any(s => s.StartTime < newEnd && s.EndTime > newStart);

            if (hasOverlap)
                throw new InvalidOperationException(
                    $"Updating this schedule would overlap with an existing schedule on {(DayOfWeek)newDay}.");

            if (dto.DayOfWeek.HasValue) schedule.DayOfWeek = dto.DayOfWeek.Value;
            if (dto.StartTime.HasValue) schedule.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) schedule.EndTime = dto.EndTime.Value;
            if (dto.SlotDurationMinutes.HasValue) schedule.SlotDurationMinutes = dto.SlotDurationMinutes.Value;
            if (dto.IsActive.HasValue) schedule.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();

            return MapToDto(schedule, schedule.Doctor!.FullName);
        }

        public async Task DeleteScheduleAsync(int scheduleId)
        {
            var schedule = await _context.DoctorSchedules.FindAsync(scheduleId);

            if (schedule == null)
                throw new KeyNotFoundException($"Schedule with ID {scheduleId} not found.");

            _context.DoctorSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
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
