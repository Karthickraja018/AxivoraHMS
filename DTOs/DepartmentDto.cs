namespace Axivora.DTOs
{
    public class DepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Description { get; set; }
    }

    public class CreateDepartmentDto
    {
        public string DepartmentName { get; set; }
        public string Description { get; set; }
    }
}
