namespace SchoolAdmission.Models;

/// <summary>
/// Central location for interview-lab configuration constants.
/// To add a new lab (e.g. Lab 4), change only TotalInterviewLabs here.
/// </summary>
public static class InterviewLabConstants
{
    /// <summary>
    /// The total number of interview labs currently supported (1 … TotalInterviewLabs).
    /// Changing this single constant is all that is needed to add more labs.
    /// </summary>
    public const int TotalInterviewLabs = 3;
}
