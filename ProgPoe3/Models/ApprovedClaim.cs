public class ApprovedClaim
{
    public int Id { get; set; }
    public string LecturerName { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public string? Notes { get; set; }
    public string? SupportingDocumentName { get; set; }
    public DateTime SubmissionDate { get; set; }
    public DateTime ApprovalDate { get; set; } = DateTime.Now;

}
