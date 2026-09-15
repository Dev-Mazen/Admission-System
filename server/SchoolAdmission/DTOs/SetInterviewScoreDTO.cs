namespace SchoolAdmission.DTOs;

/// <summary>
/// Request body for setting an interview score.
/// </summary>
public class SetInterviewScoreDTO
{
    /// <summary>
    /// The interview score value (0–40).
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// The interview lab number the student was interviewed in (1–<see cref="InterviewLabConstants.TotalInterviewLabs"/>).
    /// Defaults to 1 if not supplied.
    /// </summary>
    public int InterviewLab { get; set; } = 1;
}
