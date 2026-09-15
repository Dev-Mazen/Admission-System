using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Data;
using SchoolAdmission.Models;
using SchoolAdmission.Repos.IRepos;
using SchoolAdmission.Services;

namespace SchoolAdmission.Controllers;

[ApiController]
[Route("api/superadmin/students")]
[Authorize]
public class SuperAdminReportController : ControllerBase
{
    private readonly AdmissionReportPdfService _reportPdfService;
    private readonly IAuthRepo _authRepo;
    private readonly SchoolAdmissionDbContext _db;

    public SuperAdminReportController(
        AdmissionReportPdfService reportPdfService,
        IAuthRepo authRepo,
        SchoolAdmissionDbContext db)
    {
        _reportPdfService = reportPdfService;
        _authRepo = authRepo;
        _db = db;
    }

    [HttpGet("{studentId}/report")]
    [HttpGet("{studentId}/report/{filename}")]
    public async Task<IActionResult> GetStudentScoreReport(string studentId, [FromQuery] ReportRequestOptions options, string? filename = null)
    {
        var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(email) || !await _authRepo.IsSuperAdminAsync(email))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Only SuperAdmin users can generate student admission score reports."
            });
        }

        // Search student by AccountId or NationalId
        Models.Account? account = null;
        if (long.TryParse(studentId, out var accountId))
        {
            account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId);
        }

        if (account == null)
        {
            account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.NationalId == studentId);
        }

        if (account == null)
        {
            return NotFound(new { message = $"Student with ID '{studentId}' was not found." });
        }

        var reportData = await _reportPdfService.GetReportDataAsync(account.Id, email, options);

        if (!reportData.IsEvaluated)
        {
            return BadRequest(new { message = reportData.ErrorMessage });
        }

        byte[] pdfBytes = _reportPdfService.GeneratePdfReport(reportData);

        // Requirement 4: Filename format: {ApplicationId}_{StudentFullName}_AdmissionReport.pdf
        var sanitizedStudentName = SanitizeFilename(reportData.StudentName);
        var utf8Filename = $"{reportData.AccountId}_{sanitizedStudentName}_AdmissionReport.pdf";
        var asciiFallbackFilename = $"AdmissionReport_{reportData.AccountId}.pdf";

        var encodedUtf8Filename = Uri.EscapeDataString(utf8Filename);

        // Set RFC 5987 / RFC 6266 compliant Content-Disposition header
        Response.Headers.Remove("Content-Disposition");
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{asciiFallbackFilename}\"; filename*=UTF-8''{encodedUtf8Filename}");

        return File(pdfBytes, "application/pdf");
    }

    public static string SanitizeFilename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Student";

        var invalidChars = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' });
        var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        sanitized = Regex.Replace(sanitized, @"\s+", "_").Trim('_');

        return string.IsNullOrWhiteSpace(sanitized) ? "Student" : sanitized;
    }
}
