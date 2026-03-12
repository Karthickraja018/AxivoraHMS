using AutoMapper;
using Axivora.Models;
using Axivora.DTOs;

namespace Axivora.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Patient, PatientDto>()
                .ForMember(dest => dest.Allergies, opt => opt.MapFrom(src => src.PatientAllergies));
            CreateMap<CreatePatientDto, Patient>()
                .ForMember(dest => dest.MRN, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
            CreateMap<UpdatePatientDto, Patient>()
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())
                .ForMember(dest => dest.Address, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Doctor, DoctorDto>()
                .ForMember(dest => dest.Departments, opt => opt.MapFrom(src => 
                    src.DoctorDepartments.Select(dd => dd.Department)));
            CreateMap<CreateDoctorDto, Doctor>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorDepartments, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());
            CreateMap<UpdateDoctorDto, Doctor>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Appointment, AppointmentDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Patient!.FullName))
                .ForMember(dest => dest.DoctorName,  opt => opt.MapFrom(src => src.Doctor!.FullName))
                .ForMember(dest => dest.Status,      opt => opt.MapFrom(src => src.Status!.StatusName));
            CreateMap<UpdateAppointmentDto, Appointment>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Address, AddressDto>();
            CreateMap<CreateAddressDto, Address>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
            CreateMap<UpdateAddressDto, Address>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<UpdateAddressDto, CreateAddressDto>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Department, DepartmentDto>();
            CreateMap<CreateDepartmentDto, Department>();

            CreateMap<PatientAllergy, PatientAllergyDto>();
            CreateMap<CreatePatientAllergyDto, PatientAllergy>()
                .ForMember(dest => dest.RecordedAt, opt => opt.Ignore());

            CreateMap<Consultation, ConsultationDto>()
                .ForMember(dest => dest.ICDCode, opt => opt.MapFrom(src => src.ICDCode.Code))
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.Appointment.PatientId))
                .ForMember(dest => dest.AppointmentDate, opt => opt.MapFrom(src => src.Appointment.AppointmentStart))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.Appointment.Doctor.FullName))
                .ForMember(dest => dest.Prescriptions, opt => opt.MapFrom(src => src.Prescriptions))
                .ForMember(dest => dest.OrderedTests, opt => opt.MapFrom(src => src.OrderedTests));
            CreateMap<CreateConsultationDto, Consultation>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<UpdateConsultationDto, Consultation>()
                .ForMember(dest => dest.AppointmentId, opt => opt.Ignore())
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.InternalNotes))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Prescription, PrescriptionDto>()
                .ForMember(dest => dest.MedicineName, opt => opt.MapFrom(src => src.Medicine.MedicineName));
            CreateMap<CreatePrescriptionDto, Prescription>();

            CreateMap<OrderedTest, OrderedTestDto>()
                .ForMember(dest => dest.TestName, opt => opt.MapFrom(src => src.LabTest.TestName))
                .ForMember(dest => dest.Result, opt => opt.MapFrom(src => src.Result));
            CreateMap<CreateOrderedTestDto, OrderedTest>();

            // Session Feedback
            CreateMap<SessionFeedback, SessionFeedbackDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Patient!.FullName))
                .ForMember(dest => dest.DoctorId,    opt => opt.MapFrom(src => src.Consultation!.Appointment!.DoctorId))
                .ForMember(dest => dest.DoctorName,  opt => opt.MapFrom(src => src.Consultation!.Appointment!.Doctor!.FullName))
                .ForMember(dest => dest.RatingLabel, opt => opt.MapFrom(src =>
                    src.Rating == 1 ? "Very Poor" :
                    src.Rating == 2 ? "Poor" :
                    src.Rating == 3 ? "Average" :
                    src.Rating == 4 ? "Good" : "Excellent"));

            // ?? Availability Template ????????????????????????????????????????
            CreateMap<DoctorAvailabilityTemplate, AvailabilityTemplateDto>()
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.Doctor!.FullName))
                .ForMember(dest => dest.DayName,    opt => opt.MapFrom(src => ((DayOfWeek)src.DayOfWeek).ToString()));

            CreateMap<CreateAvailabilityTemplateDto, DoctorAvailabilityTemplate>()
                .ForMember(dest => dest.IsActive,   opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.CreatedAt,  opt => opt.Ignore());

            // ?? Availability Day ?????????????????????????????????????????????
            CreateMap<DoctorAvailabilityDay, AvailabilityDayDto>()
                .ForMember(dest => dest.TotalSlots,     opt => opt.MapFrom(src => src.Slots.Count))
                .ForMember(dest => dest.AvailableSlots, opt => opt.MapFrom(src =>
                    src.Slots.Count(s => s.Status == SlotStatus.Available)));

            // ?? Appointment Slot ?????????????????????????????????????????????
            CreateMap<AppointmentSlot, SlotDto>();
            CreateMap<AppointmentSlot, SlotDetailDto>()
                .ForMember(dest => dest.SlotId, opt => opt.MapFrom(src => src.Id));

            // ?? Departments ????????????????????????????????????????????????
            CreateMap<UpdateDepartmentDto, Department>();

            // ?? ICD Codes ??????????????????????????????????????????????????
            CreateMap<ICDCode, ICDCodeDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ICDId));

            // ?? Patient Vitals ??????????????????????????????????????????????
            CreateMap<PatientVital, PatientVitalDto>();
            CreateMap<CreatePatientVitalDto, PatientVital>()
                .ForMember(dest => dest.RecordedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PatientId,  opt => opt.Ignore());
            CreateMap<UpdatePatientVitalDto, PatientVital>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
