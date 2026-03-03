namespace Axivora.DTOs
{
    public class PatientAllergyDto
    {
        public int AllergyId { get; set; }
        public string AllergenName { get; set; }
        public string Severity { get; set; }
        public string Reaction { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    public class CreatePatientAllergyDto
    {
        public int PatientId { get; set; }
        public string AllergenName { get; set; }
        public string Severity { get; set; }
        public string Reaction { get; set; }
    }
}
