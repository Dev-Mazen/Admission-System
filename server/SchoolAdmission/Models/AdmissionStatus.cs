namespace SchoolAdmission.Models;

/// <summary>
/// Admission-specific status IDs that correspond to rows in the shared
/// Status table (BusinessEntity = 'AdmissionProfile') in the database.
///   ID 4  → Pending
///   ID 6  → Rejected
///   ID 7  → Approved
/// </summary>
public enum AdmissionStatus
{
    Pending  = 4,
    Rejected = 6,
    Approved = 7
}
