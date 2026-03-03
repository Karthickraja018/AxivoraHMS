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
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Patient.FullName))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.Doctor.FullName))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.StatusName));
            CreateMap<CreateAppointmentDto, Appointment>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
            CreateMap<UpdateAppointmentDto, Appointment>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Address, AddressDto>();
            CreateMap<CreateAddressDto, Address>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<Department, DepartmentDto>();
            CreateMap<CreateDepartmentDto, Department>();

            CreateMap<PatientAllergy, PatientAllergyDto>();
            CreateMap<CreatePatientAllergyDto, PatientAllergy>()
                .ForMember(dest => dest.RecordedAt, opt => opt.Ignore());

            CreateMap<Consultation, ConsultationDto>()
                .ForMember(dest => dest.ICDCode, opt => opt.MapFrom(src => src.ICDCode.Code))
                .ForMember(dest => dest.Prescriptions, opt => opt.MapFrom(src => src.Prescriptions))
                .ForMember(dest => dest.OrderedTests, opt => opt.MapFrom(src => src.OrderedTests));
            CreateMap<CreateConsultationDto, Consultation>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<Prescription, PrescriptionDto>()
                .ForMember(dest => dest.MedicineName, opt => opt.MapFrom(src => src.Medicine.MedicineName));
            CreateMap<CreatePrescriptionDto, Prescription>();

            CreateMap<OrderedTest, OrderedTestDto>()
                .ForMember(dest => dest.TestName, opt => opt.MapFrom(src => src.LabTest.TestName));
            CreateMap<CreateOrderedTestDto, OrderedTest>();
        }
    }
}
