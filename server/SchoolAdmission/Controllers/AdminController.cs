using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SchoolAdmission.DTOs;
using SchoolAdmission.Services;
using SchoolAdmission.Repos.IRepos;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SchoolAdmission.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminRepo _adminRepo;
    private readonly IAuthRepo _authRepo;
    private readonly SchoolAdmissionDbContext _db;
    private static readonly List<ExportColumnDefinitionDTO> ExportableColumns = new()
    {
        new ExportColumnDefinitionDTO { Key = "StudentNameEnglish", Label = "Student Name English", Description = "English full name of the applicant" },
        new ExportColumnDefinitionDTO { Key = "StudentNameArabic", Label = "Student Name Arabic", Description = "Arabic full name of the applicant" },
        new ExportColumnDefinitionDTO { Key = "CreatedAt", Label = "Created At", Description = "Date the student record was created" },
        new ExportColumnDefinitionDTO { Key = "SocialID", Label = "National ID", Description = "National identification number" },
        new ExportColumnDefinitionDTO { Key = "City", Label = "City", Description = "Applicant's city" },
        new ExportColumnDefinitionDTO { Key = "StudentPhoneNumber", Label = "Student Phone Number", Description = "Applicant's mobile number" },
        new ExportColumnDefinitionDTO { Key = "ParentPhoneNumber", Label = "Parent Phone Number", Description = "Parent/guardian mobile number" },
        new ExportColumnDefinitionDTO { Key = "Email", Label = "Email", Description = "Applicant email address" },
        new ExportColumnDefinitionDTO { Key = "DateOfBirthYear", Label = "Date of Birth - Year", Description = "Applicant birth year" },
        new ExportColumnDefinitionDTO { Key = "DateOfBirthMonth", Label = "Date of Birth - Month", Description = "Applicant birth month" },
        new ExportColumnDefinitionDTO { Key = "DateOfBirthDay", Label = "Date of Birth - Day", Description = "Applicant birth day" },
        new ExportColumnDefinitionDTO { Key = "AgeYears", Label = "Age - Years", Description = "Applicant age in years as of 1 Oct 2026" },
        new ExportColumnDefinitionDTO { Key = "AgeMonths", Label = "Age - Months", Description = "Applicant age in months remainder as of 1 Oct 2026" },
        new ExportColumnDefinitionDTO { Key = "AgeDays", Label = "Age - Days", Description = "Applicant age in days remainder as of 1 Oct 2026" },
        new ExportColumnDefinitionDTO { Key = "PreviousSchoolType", Label = "Previous School Type", Description = "Previous school type or study track" },
        new ExportColumnDefinitionDTO { Key = "SchoolExamScore", Label = "School Exam Score", Description = "Total score across school exam sections" },
        new ExportColumnDefinitionDTO { Key = "MathPrepScore", Label = "Math Prep Score", Description = "Preparatory Math score" },
        new ExportColumnDefinitionDTO { Key = "EnglishPrepScore", Label = "English Prep Score", Description = "Preparatory English score" },
        new ExportColumnDefinitionDTO { Key = "FinalYearScore", Label = "Final Prep (280)", Description = "Final preparatory-year score out of 280" },
        new ExportColumnDefinitionDTO { Key = "Prep_Final%", Label = "Prep Final %", Description = "Final year percentage" },
        new ExportColumnDefinitionDTO { Key = "MinistryExam%", Label = "Ministry %", Description = "Ministry exam percentage" },
        new ExportColumnDefinitionDTO { Key = "Interviewer1Score", Label = "Interviewer 1 Score", Description = "Score from the first interviewer" },
        new ExportColumnDefinitionDTO { Key = "Interviewer2Score", Label = "Interviewer 2 Score", Description = "Score from the second interviewer" },
        new ExportColumnDefinitionDTO { Key = "Interviewer3Score", Label = "Interviewer 3 Score", Description = "Score from the third interviewer" },
        new ExportColumnDefinitionDTO { Key = "Interviewers_SUM_Scores", Label = "Interviewers Sum", Description = "Sum of interviewer scores" },
        new ExportColumnDefinitionDTO { Key = "Interviewers_Count", Label = "Interviewers Count", Description = "Number of interviewers" },
        new ExportColumnDefinitionDTO { Key = "Interviewers_AVG_Scores%", Label = "Interview Average %", Description = "Average interviewer score (percentage)" },
        new ExportColumnDefinitionDTO { Key = "ExamArabicScore", Label = "Arabic Exam Score", Description = "School exam Arabic score" },
        new ExportColumnDefinitionDTO { Key = "ExamEnglishScore", Label = "English Exam Score", Description = "School exam English score" },
        new ExportColumnDefinitionDTO { Key = "ExamMathScore", Label = "Math Exam Score", Description = "School exam Math score" },
        new ExportColumnDefinitionDTO { Key = "ExamSoftwareScore", Label = "Software Exam Score", Description = "School exam Software score" },
        new ExportColumnDefinitionDTO { Key = "ExamIQScore", Label = "IQ Exam Score", Description = "School exam IQ score; blank for the pre-IQ cohort" },
        new ExportColumnDefinitionDTO { Key = "SchoolExamSection_Count", Label = "Exam Section Count", Description = "Number of school exam sections" },
        new ExportColumnDefinitionDTO { Key = "SchoolExamSection_Scores_AVG%", Label = "Exam Section Avg %", Description = "Average school exam section percentage" },
        new ExportColumnDefinitionDTO { Key = "ResultAdmission1%", Label = "Result Admission 1 %", Description = "Interview 50% + School exam results percentage 50%" },
        new ExportColumnDefinitionDTO { Key = "ResultAdmission2%", Label = "Average %", Description = "Overall admission percentage" },
        new ExportColumnDefinitionDTO { Key = "HasOnlineTrainingCourses", Label = "Courses", Description = "Whether the applicant has online training courses" },
        new ExportColumnDefinitionDTO { Key = "HasICDLLicense", Label = "ICDL", Description = "Whether the applicant has an ICDL license" },
        new ExportColumnDefinitionDTO { Key = "HasLaptop", Label = "Laptop", Description = "Whether the applicant has a laptop" },
        new ExportColumnDefinitionDTO { Key = "Status", Label = "Status", Description = "Current admission status" }
    };

    private static readonly IReadOnlyDictionary<string, string> SpColumnMappings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SocialID"] = "SocialID",
            ["StudentNameEnglish"] = "StudName",
            ["Prep_Final%"] = "[Prep_Final%]",
            ["MinistryExam%"] = "[MinistryExam%]",
            ["Interviewers_SUM_Scores"] = "Interviewers_SUM_Scores",
            ["Interviewers_Count"] = "Interviewers_Count",
            ["Interviewers_AVG_Scores%"] = "[Interviewers_AVG_Scores%]",
            ["SchoolExamSection_Count"] = "SchoolExamSection_Count",
            ["SchoolExamSection_Scores_AVG%"] = "[SchoolExamSection_Scores_AVG%]",
            ["ResultAdmission1%"] = "[ResultAdmission1%]",
            ["ResultAdmission2%"] = "[ResultAdmission2%]",
            ["MathPrepScore"] = "Prep_Scores",
            ["EnglishPrepScore"] = "Prep_Scores",
            ["SchoolExamScore"] = "SchoolExamSection_SUM_Scores"
        };

    public AdminController(IAdminRepo adminRepo, IAuthRepo authRepo, SchoolAdmissionDbContext db)
    {
        _adminRepo = adminRepo;
        _authRepo = authRepo;
        _db = db;
    }

    [HttpGet("students/paginated")]
    public async Task<IActionResult> GetStudentsPaginated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? statusFilter = null,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        try
        {
            var userEmail = await _adminRepo.GetCurrentAdminEmailAsync(User);
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var adminAccount = await _adminRepo.GetAccountByEmailAsync(userEmail);
            if (adminAccount == null)
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var request = new PaginationRequestDTO
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            PaginationResponseDTO<dynamic> result;
            var isInterviewer = await _authRepo.IsInterviewerAsync(userEmail);
            var isBoard = await _authRepo.IsBoardAsync(userEmail);
            var isSuperAdmin = await _authRepo.IsSuperAdminAsync(userEmail);
            
            if (isInterviewer)
            {
                result = await _adminRepo.GetStudentsForAdminPaginatedAsync(userEmail, request);
            }
            else if (isBoard || isSuperAdmin)
            {
                result = await _adminRepo.GetStudentsForSuperAdminPaginatedAsync(userEmail, request);
            }
            else
            {
                return Forbid("You do not have permission to view student information.");
            }

            // Always return the result, even if empty (this is normal for filters)
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while retrieving students", ex.Message));
        }
    }



    [HttpGet("students/filter")]
    public async Task<IActionResult> FilterStudents([FromQuery] string? name, [FromQuery] string? nationalId)
    {
        try
        {
            var userEmail = await _adminRepo.GetCurrentAdminEmailAsync(User);
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var adminAccount = await _adminRepo.GetAccountByEmailAsync(userEmail);
            if (adminAccount == null)
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var isBoard = await _authRepo.IsBoardAsync(userEmail);
            var isSuperAdmin = await _authRepo.IsSuperAdminAsync(userEmail);
            
            if (!isBoard && !isSuperAdmin)
                return Forbid("Only SuperAdmin and Board members can filter students.");

            var result = await _adminRepo.FilterStudentsAsync(name, nationalId);
            if (result.Count == 0)
                return NotFound("No students match the provided filter criteria.");

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while filtering students", ex.Message));
        }
    }



    [HttpPost("student/{studentId}/my-interview-score")]
    public async Task<IActionResult> SetMyInterviewScore(long studentId, [FromBody] double scoreValue)
    {
        try
        {
            var userEmail = await _adminRepo.GetCurrentAdminEmailAsync(User);
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var adminAccount = await _adminRepo.GetAccountByEmailAsync(userEmail);
            if (adminAccount == null)
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var isInterviewer = await _authRepo.IsInterviewerAsync(userEmail);
            if (!isInterviewer)
                return Forbid("Only interviewers can set or edit their own interview score.");

            await _adminRepo.SetInterviewScoreAsync(studentId, adminAccount.Id, scoreValue);

            return Ok(ApiResponse.SuccessResult("Interview score submitted successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while submitting your interview score", ex.Message));
        }
    }


    [HttpPut("student/{studentId}/status")]
    public async Task<IActionResult> UpdateStudentStatus(long studentId, [FromBody] UpdateStudentStatusDTO dto)
    {
        try
        {
            var userEmail = await _adminRepo.GetCurrentAdminEmailAsync(User);
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var adminAccount = await _adminRepo.GetAccountByEmailAsync(userEmail);
            if (adminAccount == null)
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var isSuperAdmin = await _authRepo.IsSuperAdminAsync(userEmail);

            if (!isSuperAdmin)
                return Forbid("Only SuperAdmin can update student status.");

            await _adminRepo.UpdateStudentStatusAsync(studentId, dto.Status);

            return Ok(ApiResponse.SuccessResult($"Student status updated successfully to {dto.Status}."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating student status", ex.Message));
        }
    }

    [HttpPut("students/status/bulk")]
    public async Task<IActionResult> BulkUpdateStudentStatus([FromBody] BulkUpdateStudentStatusDTO dto)
    {
        try
        {
            var userEmail = await _adminRepo.GetCurrentAdminEmailAsync(User);
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var adminAccount = await _adminRepo.GetAccountByEmailAsync(userEmail);
            if (adminAccount == null)
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var isSuperAdmin = await _authRepo.IsSuperAdminAsync(userEmail);

            if (!isSuperAdmin)
                return Forbid("Only SuperAdmin can update student status in bulk.");

            if (dto.StudentIds == null || dto.StudentIds.Count == 0)
                return BadRequest("No students selected for bulk status update.");

            var count = await _adminRepo.BulkUpdateStudentStatusAsync(dto.StudentIds, dto.Status);

            return Ok(ApiResponse.SuccessResult($"Successfully updated status of {count} student(s) to {dto.Status}."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating student statuses in bulk", ex.Message));
        }
    }

    [HttpGet("export-students-columns")]
    public async Task<IActionResult> GetExportStudentsColumns()
    {
        try
        {
            var userEmail = await _adminRepo.GetCurrentAdminEmailAsync(User);
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var adminAccount = await _adminRepo.GetAccountByEmailAsync(userEmail);
            if (adminAccount == null)
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var isBoard = await _authRepo.IsBoardAsync(userEmail);
            var isSuperAdmin = await _authRepo.IsSuperAdminAsync(userEmail);
            
            if (!isBoard && !isSuperAdmin)
                return Forbid("Only SuperAdmin and Board members can configure export data.");

            return Ok(ExportableColumns);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while fetching export columns", ex.Message));
        }
    }

    [HttpPost("export-students-excel")]
    public async Task<IActionResult> ExportStudentsToExcel([FromBody] ExportStudentsRequestDTO? request)
    {
        try
        {
            var userEmail = await _adminRepo.GetCurrentAdminEmailAsync(User);
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var adminAccount = await _adminRepo.GetAccountByEmailAsync(userEmail);
            if (adminAccount == null)
                return Unauthorized("Admin not found or not authorized. Please log in again.");

            var isBoard = await _authRepo.IsBoardAsync(userEmail);
            var isSuperAdmin = await _authRepo.IsSuperAdminAsync(userEmail);
            
            if (!isBoard && !isSuperAdmin)
                return Forbid("Only SuperAdmin and Board members can export students data.");

            if (request?.FromDate.HasValue == true && request.ToDate.HasValue && request.FromDate > request.ToDate)
                return BadRequest("From date cannot be later than To date.");
            if (request?.StudentCount is <= 0)
                return BadRequest("Student count must be greater than zero.");

            var requestedColumns = request?.Columns?
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToList() ?? new List<string>();

            var availableKeys = new HashSet<string>(ExportableColumns.Select(c => c.Key));
            var columnsToExport = (requestedColumns.Count > 0
                ? requestedColumns.Where(key => availableKeys.Contains(key)).Distinct().ToList()
                : ExportableColumns.Select(c => c.Key).ToList());

            if (columnsToExport.Count == 0)
            {
                columnsToExport = ExportableColumns.Select(c => c.Key).ToList();
            }

            var headerLookup = ExportableColumns.ToDictionary(c => c.Key, c => c.Label);

            // The procedure owns all score and percentage calculations.  Keep its dynamic
            // column list restricted to known identifiers; values are passed as parameters.
            var spColumns = columnsToExport
                .Where(SpColumnMappings.ContainsKey)
                .Select(key => SpColumnMappings[key])
                .Append("SocialID") // required to attach non-SP grid fields below
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var activeSettings = await _db.HubSettings
                .AsNoTracking()
                .OrderByDescending(settings => settings.Id)
                .FirstOrDefaultAsync();

            if (activeSettings == null)
                return StatusCode(500, "No admission settings are configured for the export.");

            var whereParts = new List<string>();
            if (request?.FromDate is DateOnly fromDate)
                whereParts.Add($"[Created_AT] >= '{fromDate:yyyy-MM-dd}'");
            if (request?.ToDate is DateOnly toDate)
                whereParts.Add($"[Created_AT] < '{toDate.AddDays(1):yyyy-MM-dd}'");

            var connection = _db.Database.GetDbConnection();
            var openedHere = connection.State != ConnectionState.Open;
            if (openedHere)
                await connection.OpenAsync();

            var rows = new List<Dictionary<string, object?>>();
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SP_AdmissionResult";
                command.CommandType = CommandType.StoredProcedure;
                var databaseName = new SqlConnectionStringBuilder(connection.ConnectionString).InitialCatalog;
                if (string.IsNullOrWhiteSpace(databaseName))
                    throw new InvalidOperationException("The export connection string must specify a database name.");
                AddParameter(command, "@DBname", databaseName);
                AddParameter(command, "@ColumnList", string.Join(", ", spColumns));
                AddParameter(command, "@PrepWeight", ToSpWeight(activeSettings.PreparatoryCertificateWeight));
                AddParameter(command, "@MinistryWeight", ToSpWeight(activeSettings.MinistryExamWeight));
                AddParameter(command, "@InterviewersWeight", ToSpWeight(activeSettings.InterviewWeight));
                AddParameter(command, "@SchoolExamWeight", ToSpWeight(activeSettings.SchoolExamWeight));
                AddParameter(command, "@WhereStr", whereParts.Count == 0 ? "WHERE 1 = 1" : $"WHERE {string.Join(" AND ", whereParts)}");
                AddParameter(command, "@OrderByStr", "ORDER BY [ResultAdmission2%] DESC");

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var resultRow = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (var index = 0; index < reader.FieldCount; index++)
                        resultRow[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);

                    rows.Add(resultRow);
                    if (request?.StudentCount is int studentCount && rows.Count >= studentCount)
                        break;
                }
            }
            finally
            {
                if (openedHere)
                    await connection.CloseAsync();
            }

            if (rows.Count == 0)
                return NotFound("No students found to export.");

            var socialIds = rows
                .Select(row => GetStringValue(row, "SocialID"))
                .Where(socialId => !string.IsNullOrWhiteSpace(socialId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var supplementalBySocialId = await _db.Accounts
                .AsNoTracking()
                .Where(account => account.NationalId != null && socialIds.Contains(account.NationalId))
                .Select(account => new ExportSupplementalData
                {
                    AccountId = account.Id,
                    SocialId = account.NationalId!,
                    StudentNameArabic = account.FullNameAr,
                    StudentNameEnglish = account.FullNameEn,
                    Email = account.Email,
                    StudentPhoneNumber = account.AdmissionProfile != null ? account.AdmissionProfile.PhoneNumber : null,
                    ParentPhoneNumber = account.AdmissionProfile != null ? account.AdmissionProfile.ParentPhoneNumber : null,
                    DateOfBirth = account.AdmissionProfile != null ? account.AdmissionProfile.DateOfBirth : null,
                    Age = account.AdmissionProfile != null ? CalculateAge(account.AdmissionProfile.DateOfBirth) : null,
                    DateOfBirthYear = account.AdmissionProfile != null && account.AdmissionProfile.DateOfBirth.HasValue ? account.AdmissionProfile.DateOfBirth.Value.Year : null,
                    DateOfBirthMonth = account.AdmissionProfile != null && account.AdmissionProfile.DateOfBirth.HasValue ? account.AdmissionProfile.DateOfBirth.Value.Month : null,
                    DateOfBirthDay = account.AdmissionProfile != null && account.AdmissionProfile.DateOfBirth.HasValue ? account.AdmissionProfile.DateOfBirth.Value.Day : null,
                    AgeYears = account.AdmissionProfile != null && account.AdmissionProfile.DateOfBirth.HasValue ? GetAgeParts(account.AdmissionProfile.DateOfBirth).Years : null,
                    AgeMonths = account.AdmissionProfile != null && account.AdmissionProfile.DateOfBirth.HasValue ? GetAgeParts(account.AdmissionProfile.DateOfBirth).Months : null,
                    AgeDays = account.AdmissionProfile != null && account.AdmissionProfile.DateOfBirth.HasValue ? GetAgeParts(account.AdmissionProfile.DateOfBirth).Days : null,
                    PreviousSchoolType = account.AdmissionProfile != null ? account.AdmissionProfile.PreviousSchoolType : null,
                    CreatedAt = account.StudentExamResult != null && account.StudentExamResult.ExamStartedAtUtc.HasValue
                        ? account.StudentExamResult.ExamStartedAtUtc.Value
                        : account.CreatedAt.HasValue
                            ? account.CreatedAt.Value.ToDateTime(TimeOnly.MinValue)
                            : null,
                    City = account.AdmissionProfile != null ? account.AdmissionProfile.City : null,
                    Status = account.AdmissionProfile != null ? account.AdmissionProfile.StatusId : 1,
                    HasOnlineTrainingCourses = account.AdmissionProfile != null && account.AdmissionProfile.HasOnlineTrainingCourses,
                    HasICDLLicense = account.AdmissionProfile != null && account.AdmissionProfile.HasIcdllicense,
                    HasLaptop = account.AdmissionProfile != null && account.AdmissionProfile.HasLaptop,
                    FinalYearScore = account.AdmissionProfile != null ? account.AdmissionProfile.ThirdPrepScore : null,
                    ExamArabicScore = account.StudentExamResult != null ? account.StudentExamResult.ExamArabicScore : null,
                    ExamEnglishScore = account.StudentExamResult != null ? account.StudentExamResult.ExamEnglishScore : null,
                    ExamMathScore = account.StudentExamResult != null ? account.StudentExamResult.ExamMathScore : null,
                    ExamSoftwareScore = account.StudentExamResult != null ? account.StudentExamResult.ExamSoftwareScore : null,
                    ExamIqScore = account.StudentExamResult != null ? account.StudentExamResult.ExamIqScore : null,
                })
                .ToDictionaryAsync(item => item.SocialId, StringComparer.OrdinalIgnoreCase);

            // Match the dashboard's interviewer slots: interviewer 1, 2, and 3 are
            // the first three interviewer account IDs in ascending order.  The stored
            // procedure's aggregate string has no interviewer-ID association, so it
            // cannot reliably populate these individual columns.
            var interviewerIds = (await _authRepo.GetAccountIdsByRoleAsync("Interviewer"))
                .OrderBy(id => id)
                .Take(3)
                .ToList();

            if (interviewerIds.Count > 0 && columnsToExport.Any(key => key is "Interviewer1Score" or "Interviewer2Score" or "Interviewer3Score"))
            {
                var studentIds = supplementalBySocialId.Values.Select(item => item.AccountId).ToList();
                var scores = await _db.InterviewScores
                    .AsNoTracking()
                    .Where(score => studentIds.Contains(score.AccountId) && interviewerIds.Contains(score.InterviewerId))
                    .Select(score => new { score.AccountId, score.InterviewerId, score.Score })
                    .ToListAsync();

                var scoresByStudent = scores
                    .GroupBy(score => score.AccountId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.ToDictionary(score => score.InterviewerId, score => score.Score));

                foreach (var supplemental in supplementalBySocialId.Values)
                {
                    supplemental.InterviewerScoresById = scoresByStudent.TryGetValue(supplemental.AccountId, out var scoresForStudent)
                        ? scoresForStudent
                        : new Dictionary<long, decimal>();
                }
            }

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Students Data");

            for (int i = 0; i < columnsToExport.Count; i++)
            {
                var key = columnsToExport[i];
                worksheet.Cells[1, i + 1].Value = headerLookup.TryGetValue(key, out var label) ? label : key;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

                if (key == "CreatedAt")
                    worksheet.Column(i + 1).Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                else if (key == "DateOfBirth")
                    worksheet.Column(i + 1).Style.Numberformat.Format = "yyyy-mm-dd";
            }

            int row = 2;
            foreach (var spRow in rows)
            {
                supplementalBySocialId.TryGetValue(GetStringValue(spRow, "SocialID") ?? string.Empty, out var supplemental);
                for (int columnIndex = 0; columnIndex < columnsToExport.Count; columnIndex++)
                {
                    var key = columnsToExport[columnIndex];
                    worksheet.Cells[row, columnIndex + 1].Value = GetExportValue(key, spRow, supplemental, interviewerIds);
                }
                row++;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            // Generate file name with timestamp
            var fileName = $"Students_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            // Convert to byte array
            var fileBytes = package.GetAsByteArray();

            // Return file
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while exporting students data", ex.Message));
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    // SP_AdmissionResult composes these values directly into its score expression,
    // so convert our settings percentages (for example, 60) to "0.6*".
    private static string ToSpWeight(decimal percentage)
        => $"{(percentage / 100m).ToString("0.############################", CultureInfo.InvariantCulture)}*";

    private static string? GetStringValue(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) && value != null ? Convert.ToString(value) : null;

    private static object? GetExportValue(
        string key,
        IReadOnlyDictionary<string, object?> row,
        ExportSupplementalData? supplemental,
        IReadOnlyList<long> interviewerIds)
    {
        if (row.TryGetValue(key, out var directValue))
            return directValue;

        return key switch
        {
            // StudName is a legacy stored-procedure field and can contain the Arabic
            // name. Prefer the account's dedicated English-name field for exports.
            "StudentNameEnglish" => supplemental?.StudentNameEnglish ?? GetStringValue(row, "StudName"),
            "StudentNameArabic" => supplemental?.StudentNameArabic,
            "CreatedAt" => supplemental?.CreatedAt,
            "City" => supplemental?.City,
            "StudentPhoneNumber" => supplemental?.StudentPhoneNumber,
            "ParentPhoneNumber" => supplemental?.ParentPhoneNumber,
            "Email" => supplemental?.Email,
            "DateOfBirth" => NormalizeExcelDate(supplemental?.DateOfBirth),
            "DateOfBirthYear" => supplemental?.DateOfBirthYear,
            "DateOfBirthMonth" => supplemental?.DateOfBirthMonth,
            "DateOfBirthDay" => supplemental?.DateOfBirthDay,
            "AgeYears" => supplemental?.AgeYears,
            "AgeMonths" => supplemental?.AgeMonths,
            "AgeDays" => supplemental?.AgeDays,
            "Age" => supplemental?.Age,
            "PreviousSchoolType" => supplemental?.PreviousSchoolType,
            "Status" => GetStatusText(supplemental?.Status),
            "HasOnlineTrainingCourses" => supplemental?.HasOnlineTrainingCourses,
            "HasICDLLicense" => supplemental?.HasICDLLicense,
            "HasLaptop" => supplemental?.HasLaptop,
            "FinalYearScore" => supplemental?.FinalYearScore,
            "MathPrepScore" => GetCompositeValue(row, "Prep_Scores", "MathPrep"),
            "EnglishPrepScore" => GetCompositeValue(row, "Prep_Scores", "EnglishPrep"),
            "Interviewer1Score" => GetInterviewerScore(supplemental, interviewerIds, 0),
            "Interviewer2Score" => GetInterviewerScore(supplemental, interviewerIds, 1),
            "Interviewer3Score" => GetInterviewerScore(supplemental, interviewerIds, 2),
            "ExamArabicScore" => supplemental?.ExamArabicScore,
            "ExamEnglishScore" => supplemental?.ExamEnglishScore,
            "ExamMathScore" => supplemental?.ExamMathScore,
            "ExamSoftwareScore" => supplemental?.ExamSoftwareScore,
            "ExamIQScore" => supplemental?.ExamIqScore,
            "SchoolExamScore" => GetRawValue(row, "SchoolExamSection_SUM_Scores"),
            _ when SpColumnMappings.TryGetValue(key, out var spColumn) => GetRawValue(row, spColumn.Trim('[', ']')),
            _ => null
        };
    }

    private static object? NormalizeExcelDate(DateOnly? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.ToDateTime(TimeOnly.MinValue);
    }

    private static object? GetRawValue(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) ? value : null;

    private static string? GetCompositeValue(IReadOnlyDictionary<string, object?> row, string sourceKey, string itemKey)
    {
        var source = GetStringValue(row, sourceKey);
        if (string.IsNullOrWhiteSpace(source)) return null;

        var prefix = $"{itemKey}=";
        return source.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim().TrimStart('['))
            .FirstOrDefault(item => item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..];
    }

    private static decimal? GetInterviewerScore(ExportSupplementalData? supplemental, IReadOnlyList<long> interviewerIds, int slot)
        => supplemental != null && interviewerIds.Count > slot &&
           supplemental.InterviewerScoresById.TryGetValue(interviewerIds[slot], out var score)
            ? score
            : null;

    private static AgeParts GetAgeParts(DateOnly? dateOfBirth)
    {
        if (!dateOfBirth.HasValue)
            return new AgeParts(0, 0, 0);

        var referenceDate = new DateOnly(2026, 10, 1);

        var years = referenceDate.Year - dateOfBirth.Value.Year;
        var months = referenceDate.Month - dateOfBirth.Value.Month;
        var days = referenceDate.Day - dateOfBirth.Value.Day;

        if (days < 0)
        {
            var previousMonth = new DateOnly(referenceDate.Year, referenceDate.Month, 1).AddDays(-1);
            days += previousMonth.Day;
            months -= 1;
        }

        if (months < 0)
        {
            years -= 1;
            months += 12;
        }

        if (years < 0)
        {
            years = 0;
            months = 0;
            days = 0;
        }

        return new AgeParts(years, months, days);
    }

    private static string? CalculateAge(DateOnly? dateOfBirth)
    {
        var parts = GetAgeParts(dateOfBirth);
        return $"{parts.Days} day{(parts.Days == 1 ? string.Empty : "s")} {parts.Months} month{(parts.Months == 1 ? string.Empty : "s")} {parts.Years} year{(parts.Years == 1 ? string.Empty : "s")}";
    }

    private readonly record struct AgeParts(int Years, int Months, int Days);

    private sealed class ExportSupplementalData
    {
        public long AccountId { get; init; }
        public string SocialId { get; init; } = string.Empty;
        public string? StudentNameArabic { get; init; }
        public string? StudentNameEnglish { get; init; }
        public string? Email { get; init; }
        public string? StudentPhoneNumber { get; init; }
        public string? ParentPhoneNumber { get; init; }
        public DateOnly? DateOfBirth { get; init; }
        public string? Age { get; init; }
        public int? DateOfBirthYear { get; init; }
        public int? DateOfBirthMonth { get; init; }
        public int? DateOfBirthDay { get; init; }
        public int? AgeYears { get; init; }
        public int? AgeMonths { get; init; }
        public int? AgeDays { get; init; }
        public string? PreviousSchoolType { get; init; }
        public DateTime? CreatedAt { get; init; }
        public string? City { get; init; }
        public long Status { get; init; }
        public bool HasOnlineTrainingCourses { get; init; }
        public bool HasICDLLicense { get; init; }
        public bool HasLaptop { get; init; }
        public decimal? FinalYearScore { get; init; }
        public int? ExamArabicScore { get; init; }
        public int? ExamEnglishScore { get; init; }
        public int? ExamMathScore { get; init; }
        public int? ExamSoftwareScore { get; init; }
        public int? ExamIqScore { get; init; }
        public Dictionary<long, decimal> InterviewerScoresById { get; set; } = new();
    }

    private static string GetStatusText(object? status)
    {
        if (status == null) return "Pending";

        // Status IDs match the shared Status table (BusinessEntity = 'AdmissionProfile'):
        //   4 = Pending, 6 = Rejected, 7 = Approved
        string statusStr = status.ToString() ?? "Pending";
        return statusStr switch
        {
            "4" => "Pending",
            "6" => "Rejected",
            "7" => "Approved",
            _ => "Pending"
        };
    }
}
