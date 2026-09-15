namespace SchoolAdmission.Models;

/// <summary>
/// Per-print report configuration passed in the PDF generation request.
/// Controls section visibility on the generated single-page PDF report.
/// Defaults to all sections enabled if options are not explicitly specified.
/// </summary>
public class ReportRequestOptions
{
    public bool ShowApplicantInfo { get; set; } = true;
    public bool ShowStudentCode { get; set; } = true;
    public bool ShowFinalScore { get; set; } = true;
    public bool ShowFactorBreakdown { get; set; } = true;
    public bool ShowPreparatoryGrades { get; set; } = true;
    public bool ShowSchoolExamBreakdown { get; set; } = true;
    public bool ShowInterviewBreakdown { get; set; } = true;
    public bool ShowCalculationFormula { get; set; } = true;
}
