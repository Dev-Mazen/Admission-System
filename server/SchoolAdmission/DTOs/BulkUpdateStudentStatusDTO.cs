using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.DTOs;

public class BulkUpdateStudentStatusDTO
{
    [Required]
    public List<long> StudentIds { get; set; } = new();

    [Required]
    public string Status { get; set; } = string.Empty;
}
