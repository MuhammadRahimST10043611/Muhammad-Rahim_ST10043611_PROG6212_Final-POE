public class Claim
{
    public int Id { get; set; }
    public string LecturerName { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending";
    public string? SupportingDocumentName { get; set; }
    public DateTime SubmissionDate { get; set; } = DateTime.Now;
    public string? AutomatedChecks { get; set; } // New property

}

