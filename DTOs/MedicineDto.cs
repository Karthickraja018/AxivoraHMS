namespace Axivora.DTOs
{
    /// <summary>Read-only medicine catalogue entry.</summary>
    public class MedicineDto
    {
        /// <summary>Unique medicine identifier.</summary>
        public int MedicineId { get; set; }

        /// <summary>Full medicine name including strength (e.g. Paracetamol 500mg).</summary>
        public string MedicineName { get; set; } = null!;
    }
}
