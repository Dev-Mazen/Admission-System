using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolAdmission.Data;
using SchoolAdmission.Models;

namespace SchoolAdmission.Services;

public class StudentReportDataModel
{
    public bool IsEvaluated { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public long AccountId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNameAr { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public DateTime GeneratedAtCairo { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public string GeneratedByAdminName { get; set; } = string.Empty;

    // Snapshot Info
    public int VersionNumber { get; set; }
    public string VersionName { get; set; } = string.Empty;
    public DateTime SettingsCreatedAt { get; set; }

    // Final Score
    public double FinalPercentage { get; set; }

    // Main Factors
    public FactorReportItem SchoolExamFactor { get; set; } = new();
    public FactorReportItem InterviewFactor { get; set; } = new();
    public FactorReportItem PrepCertFactor { get; set; } = new();
    public FactorReportItem MinistryFactor { get; set; } = new();

    // School Exam Sub-breakdown
    public List<SubjectReportItem> SchoolExamSubjects { get; set; } = new();
    public double SchoolExamEarnedTotal { get; set; }
    public double SchoolExamMaxTotal { get; set; }
    public double SchoolExamPercentage { get; set; }

    // Interview Sub-breakdown
    public List<InterviewerReportItem> InterviewerDetails { get; set; } = new();
    public double InterviewEarnedTotal { get; set; }
    public double InterviewMaxTotal { get; set; } = 120.0;
    public double InterviewPercentage { get; set; }

    // Student Code & Extensible Metadata
    public string? StudentCode { get; set; }

    // Preparatory Academic Grades (Grade 9 / Third Prep from AdmissionProfile)
    public double? PrepMathScore { get; set; }
    public double? PrepEnglishScore { get; set; }
    public double? PrepTotalScore { get; set; }

    // Per-Print Request Options
    public ReportRequestOptions Options { get; set; } = new();

    // Formula String
    public string FormulaString { get; set; } = string.Empty;
}

public class FactorReportItem
{
    public string SourceName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string RawScoreDisplay { get; set; } = string.Empty;
    public decimal WeightPercentage { get; set; }
    public double ContributionPercentage { get; set; }
}

public class SubjectReportItem
{
    public string SubjectName { get; set; } = string.Empty;
    public int RawScore { get; set; }
    public decimal SubjectWeight { get; set; }
    public double SubtotalContribution { get; set; }
}

public class InterviewerReportItem
{
    public string InterviewerName { get; set; } = string.Empty;
    public double RawScore { get; set; }
    public double MaxScore { get; set; } = 40.0;
    public double Percentage { get; set; }
}

public class AdmissionReportPdfService
{
    private readonly SchoolAdmissionDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly AdmissionSettingsService _settingsService;

    public AdmissionReportPdfService(
        SchoolAdmissionDbContext db,
        IWebHostEnvironment env,
        AdmissionSettingsService settingsService)
    {
        _db = db;
        _env = env;
        _settingsService = settingsService;
    }

    public async Task<StudentReportDataModel> GetReportDataAsync(long accountId, string? generatedByAdminEmail = null, ReportRequestOptions? options = null)
    {
        var opts = options ?? new ReportRequestOptions();
        var account = await _db.Accounts
            .AsNoTracking()
            .Include(a => a.AdmissionProfile)
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null)
        {
            return new StudentReportDataModel
            {
                IsEvaluated = false,
                ErrorMessage = "Student record not found."
            };
        }

        // Fetch Generating Admin Full Name
        string adminName = "SuperAdmin";
        if (!string.IsNullOrWhiteSpace(generatedByAdminEmail))
        {
            var adminAcc = await _db.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Email == generatedByAdminEmail);
            if (adminAcc != null)
            {
                adminName = !string.IsNullOrWhiteSpace(adminAcc.FullNameEn)
                    ? adminAcc.FullNameEn
                    : (!string.IsNullOrWhiteSpace(adminAcc.FullNameAr) ? adminAcc.FullNameAr : generatedByAdminEmail);
            }
        }

        var examResult = await _db.StudentExamResults
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AccountId == accountId);

        var interviewScores = await _db.InterviewScores
            .AsNoTracking()
            .Where(i => i.AccountId == accountId)
            .ToListAsync();

        var profile = account.AdmissionProfile;

        // Check if student has started exam or has any scores recorded
        var hasExam = examResult?.ExamStartedAtUtc != null || (examResult != null && (examResult.ExamArabicScore > 0 || examResult.ExamEnglishScore > 0 || examResult.ExamMathScore > 0 || examResult.ExamSoftwareScore > 0 || (examResult.ExamIqScore ?? 0) > 0));
        var hasInterview = interviewScores.Count > 0;
        var hasPrep = profile != null && (profile.ThirdPrepScore.HasValue && profile.ThirdPrepScore.Value > 0);
        var hasMinistry = profile != null && profile.MinistryExamPercentage > 0;

        if (!hasExam && !hasInterview && !hasPrep && !hasMinistry)
        {
            return new StudentReportDataModel
            {
                IsEvaluated = false,
                ErrorMessage = "Cannot generate score report: Student has not started their exam or completed admission evaluation yet."
            };
        }

        // Snapshot resolution using AdmissionSettingsService
        HubSettings snapshotSettings = await _settingsService.GetForStudentAsync(accountId);

        // Status name lookup (Accepted = 7, Rejected = 6, Pending/Waitlisted = 4)
        var statusId = profile != null && profile.StatusId != 0 ? profile.StatusId : account.StatusId;
        var statusName = "Pending";
        if (statusId == 7) statusName = "Accepted";
        else if (statusId == 6) statusName = "Rejected";
        else if (statusId == 4) statusName = "Pending";

        // School Exam Breakdown
        var arabicScore = examResult?.ExamArabicScore ?? 0;
        var englishScore = examResult?.ExamEnglishScore ?? 0;
        var mathScore = examResult?.ExamMathScore ?? 0;
        var softwareScore = examResult?.ExamSoftwareScore ?? 0;
        var iqScore = examResult?.ExamIqScore ?? 0;

        var examTotalRaw = arabicScore + englishScore + mathScore + softwareScore + iqScore;
        var examMaxRaw = (double)AdmissionSettingsService.GetExamMaximum(snapshotSettings);
        var schoolExamPercentage = AdmissionSettingsService.GetExamPercentage(examResult, snapshotSettings);
        var schoolExamContribution = snapshotSettings.SchoolExamWeight > 0
            ? schoolExamPercentage * (double)snapshotSettings.SchoolExamWeight / 100d
            : 0d;

        var subjects = new List<SubjectReportItem>
        {
            new() { SubjectName = "Mathematics", RawScore = mathScore, SubjectWeight = snapshotSettings.MathWeight, SubtotalContribution = examMaxRaw > 0 ? (mathScore / examMaxRaw) * 100d : 0d },
            new() { SubjectName = "English", RawScore = englishScore, SubjectWeight = snapshotSettings.EnglishWeight, SubtotalContribution = examMaxRaw > 0 ? (englishScore / examMaxRaw) * 100d : 0d },
            new() { SubjectName = "Arabic", RawScore = arabicScore, SubjectWeight = snapshotSettings.ArabicWeight, SubtotalContribution = examMaxRaw > 0 ? (arabicScore / examMaxRaw) * 100d : 0d },
            new() { SubjectName = "Software / Tech", RawScore = softwareScore, SubjectWeight = snapshotSettings.SoftwareWeight, SubtotalContribution = examMaxRaw > 0 ? (softwareScore / examMaxRaw) * 100d : 0d },
            new() { SubjectName = "IQ Assessment", RawScore = iqScore, SubjectWeight = snapshotSettings.IqWeight, SubtotalContribution = examMaxRaw > 0 ? (iqScore / examMaxRaw) * 100d : 0d }
        };

        // Interview Factor & Individual Interviewer Details
        var interviewerAccountIds = interviewScores.Select(i => i.InterviewerId).Distinct().ToList();
        var interviewerAccounts = await _db.Accounts
            .AsNoTracking()
            .Where(a => interviewerAccountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => !string.IsNullOrWhiteSpace(a.FullNameEn) ? a.FullNameEn : (!string.IsNullOrWhiteSpace(a.FullNameAr) ? a.FullNameAr : a.Email));

        var interviewerDetails = interviewScores.Select((i, idx) =>
        {
            var name = interviewerAccounts.TryGetValue(i.InterviewerId, out var n) ? n : $"Interviewer {idx + 1}";
            var raw = (double)i.Score;
            return new InterviewerReportItem
            {
                InterviewerName = name,
                RawScore = raw,
                MaxScore = 40.0,
                Percentage = (raw / 40.0) * 100.0
            };
        }).ToList();

        var interviewSum = (double)interviewScores.Sum(i => i.Score);
        var interviewMax = 40.0 * 3.0; // 120 max
        var interviewPercentage = interviewScores.Count > 0 ? (interviewSum * 100.0 / interviewMax) : 0d;
        var interviewContribution = snapshotSettings.InterviewWeight > 0 && interviewScores.Count > 0
            ? interviewPercentage * (double)snapshotSettings.InterviewWeight / 100d
            : 0d;

        // Prep Cert Factor
        var prepRaw = (double)(profile?.ThirdPrepScore ?? 0m);
        var prepMax = 280.0;
        var prepPercentage = (prepRaw / prepMax) * 100d;
        var prepContribution = snapshotSettings.PreparatoryCertificateWeight > 0
            ? prepPercentage * (double)snapshotSettings.PreparatoryCertificateWeight / 100d
            : 0d;

        // Ministry Factor
        var ministryPercentage = (double)(profile?.MinistryExamPercentage ?? 0m);
        var ministryContribution = snapshotSettings.MinistryExamWeight > 0
            ? ministryPercentage * (double)snapshotSettings.MinistryExamWeight / 100d
            : 0d;

        // Final Total
        var finalScore = AdmissionSettingsService.GetTotalPercentage(
            examResult,
            profile,
            interviewScores.Select(i => (decimal)i.Score),
            snapshotSettings);

        // Build Formula String
        var formulaParts = new List<string>();
        if (snapshotSettings.SchoolExamWeight > 0)
            formulaParts.Add($"SchoolExam ({schoolExamPercentage:F1}% × {snapshotSettings.SchoolExamWeight}%)");
        else
            formulaParts.Add("SchoolExam (Excluded)");

        if (snapshotSettings.InterviewWeight > 0)
        {
            if (interviewScores.Count > 0)
                formulaParts.Add($"Interview ({interviewPercentage:F1}% × {snapshotSettings.InterviewWeight}%)");
            else
                formulaParts.Add($"Interview (Pending × {snapshotSettings.InterviewWeight}%)");
        }
        else
        {
            formulaParts.Add("Interview (Excluded)");
        }

        if (snapshotSettings.PreparatoryCertificateWeight > 0)
            formulaParts.Add($"PrepCert ({prepPercentage:F1}% × {snapshotSettings.PreparatoryCertificateWeight}%)");
        else
            formulaParts.Add("PrepCert (Excluded)");

        if (snapshotSettings.MinistryExamWeight > 0)
            formulaParts.Add($"Ministry ({ministryPercentage:F1}% × {snapshotSettings.MinistryExamWeight}%)");
        else
            formulaParts.Add("Ministry (Excluded)");

        var formulaString = $"Final Score = {string.Join(" + ", formulaParts)} = {finalScore:F2}%";

        var cairoNow = DateTime.UtcNow.AddHours(3);
        var docId = $"DOC-{account.Id}-{cairoNow:yyyyMMddHHmmss}";

        return new StudentReportDataModel
        {
            IsEvaluated = true,
            AccountId = account.Id,
            StudentName = !string.IsNullOrWhiteSpace(account.FullNameAr) ? account.FullNameAr : (!string.IsNullOrWhiteSpace(account.FullNameEn) ? account.FullNameEn : "Applicant"),
            StudentNameAr = account.FullNameAr ?? "",
            NationalId = account.NationalId ?? "N/A",
            City = profile?.City ?? profile?.Location ?? "N/A",
            StatusName = statusName,
            GeneratedAtCairo = cairoNow,
            DocumentId = docId,
            GeneratedByAdminName = adminName,

            VersionNumber = snapshotSettings.VersionNumber,
            VersionName = snapshotSettings.VersionName ?? $"Settings v{snapshotSettings.VersionNumber}",
            SettingsCreatedAt = snapshotSettings.CreatedAtUtc,

            FinalPercentage = finalScore,

            SchoolExamFactor = new FactorReportItem
            {
                SourceName = "School Exam",
                Enabled = snapshotSettings.SchoolExamWeight > 0,
                RawScoreDisplay = $"{examTotalRaw} / {examMaxRaw:F0}",
                WeightPercentage = snapshotSettings.SchoolExamWeight,
                ContributionPercentage = schoolExamContribution
            },
            InterviewFactor = new FactorReportItem
            {
                SourceName = "Interview Evaluation",
                Enabled = snapshotSettings.InterviewWeight > 0,
                RawScoreDisplay = interviewScores.Count > 0 ? $"{interviewSum:F1} / 120 ({interviewScores.Count} Interviewer{(interviewScores.Count > 1 ? "s" : "")})" : "Pending",
                WeightPercentage = snapshotSettings.InterviewWeight,
                ContributionPercentage = interviewContribution
            },
            PrepCertFactor = new FactorReportItem
            {
                SourceName = "Preparatory Certificate",
                Enabled = snapshotSettings.PreparatoryCertificateWeight > 0,
                RawScoreDisplay = $"{prepRaw:F1} / 280",
                WeightPercentage = snapshotSettings.PreparatoryCertificateWeight,
                ContributionPercentage = prepContribution
            },
            MinistryFactor = new FactorReportItem
            {
                SourceName = "Ministry Exam",
                Enabled = snapshotSettings.MinistryExamWeight > 0,
                RawScoreDisplay = $"{ministryPercentage:F1}%",
                WeightPercentage = snapshotSettings.MinistryExamWeight,
                ContributionPercentage = ministryContribution
            },

            SchoolExamSubjects = subjects,
            SchoolExamEarnedTotal = examTotalRaw,
            SchoolExamMaxTotal = examMaxRaw,
            SchoolExamPercentage = schoolExamPercentage,

            InterviewerDetails = interviewerDetails,
            InterviewEarnedTotal = interviewSum,
            InterviewMaxTotal = interviewMax,
            InterviewPercentage = interviewPercentage,

            StudentCode = JsonMetadataHelper.GetProperty(profile?.Metadata, "studentCode"),
            PrepMathScore = profile?.MathScore.HasValue == true ? (double?)profile.MathScore.Value : null,
            PrepEnglishScore = profile?.EnglishScore.HasValue == true ? (double?)profile.EnglishScore.Value : null,
            PrepTotalScore = profile?.ThirdPrepScore.HasValue == true ? (double?)profile.ThirdPrepScore.Value : null,
            Options = opts,

            FormulaString = formulaString
        };
    }

    private string ResolveLogoPath()
    {
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            @"d:\Mazen\MyProjects\Admission-System\public\sewedyLogo.png",
            Path.Combine(_env.WebRootPath ?? "", "sewedyLogo.png"),
            Path.Combine(_env.ContentRootPath ?? "", "public", "sewedyLogo.png"),
            Path.GetFullPath(Path.Combine(cwd, "..", "..", "public", "sewedyLogo.png")),
            Path.GetFullPath(Path.Combine(cwd, "..", "public", "sewedyLogo.png")),
            Path.Combine(cwd, "public", "sewedyLogo.png"),
        };

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .FirstOrDefault() ?? string.Empty;
    }

    public byte[] GeneratePdfReport(StudentReportDataModel model)
    {
        string logoPath = ResolveLogoPath();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15, Unit.Point);
                page.PageColor("#ffffff");
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8.5f).FontColor("#1e293b"));

                page.Header().Element(headerContainer => BuildHeader(headerContainer, model, logoPath));
                page.Content().Element(contentContainer => BuildContent(contentContainer, model));
                page.Footer().Element(footerContainer => BuildFooter(footerContainer, model));
            });
        });

        return document.GeneratePdf();
    }

    private void BuildHeader(IContainer container, StudentReportDataModel model, string logoPath)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(titleCol =>
                {
                    titleCol.Item().Text("EL SEWEDY INTERNATIONAL SCHOOL")
                        .FontSize(13).Bold().FontColor("#ef3131").LetterSpacing(0.04f);
                    titleCol.Item().Text("Admission Evaluation Score Report")
                        .FontSize(15).Bold().FontColor("#0f172a");
                    titleCol.Item().Text($"Audit Snapshot: Settings v{model.VersionNumber} — {model.VersionName} (Effective: {model.SettingsCreatedAt:yyyy-MM-dd})")
                        .FontSize(8).Italic().FontColor("#475569");
                });

                if (File.Exists(logoPath))
                {
                    row.ConstantItem(60).Height(45).Image(logoPath).FitArea();
                }
                else
                {
                    row.ConstantItem(85).Height(40).Background("#ef3131").AlignCenter().AlignMiddle()
                        .Text("EL SEWEDY").FontSize(10).Bold().FontColor("#ffffff");
                }
            });

            col.Item().PaddingTop(5).LineHorizontal(1.5f).LineColor("#ef3131");
        });
    }

    private void BuildContent(IContainer container, StudentReportDataModel model)
    {
        var opts = model.Options ?? new ReportRequestOptions();

        container.PaddingTop(4).Column(col =>
        {
            // 1. Student Info Block & Final Result Card
            if (opts.ShowApplicantInfo || opts.ShowFinalScore)
            {
                col.Item().Row(row =>
                {
                    if (opts.ShowApplicantInfo)
                    {
                        row.RelativeItem(3).Border(1).BorderColor("#cbd5e1").Background("#f8fafc").Padding(6).Column(infoCol =>
                        {
                            infoCol.Item().Text("APPLICANT INFORMATION").FontSize(8f).Bold().FontColor("#ef3131");
                            infoCol.Item().PaddingTop(2).Column(innerCol =>
                            {
                                innerCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(t => { t.Span("Full Name: ").SemiBold(); t.Span(model.StudentName); });
                                    r.RelativeItem().Text(t => { t.Span("National ID: ").SemiBold(); t.Span(model.NationalId); });
                                });
                                innerCol.Item().PaddingTop(2).Row(r =>
                                {
                                    r.RelativeItem().Text(t => { t.Span("Application ID: ").SemiBold(); t.Span($"#{model.AccountId}"); });
                                    r.RelativeItem().Text(t => { t.Span("City/Gov: ").SemiBold(); t.Span(model.City); });
                                });

                                if (opts.ShowStudentCode)
                                {
                                    innerCol.Item().PaddingTop(2).Row(r =>
                                    {
                                        r.RelativeItem().Text(t =>
                                        {
                                            t.Span("Student Code: ").SemiBold();
                                            t.Span(!string.IsNullOrWhiteSpace(model.StudentCode) ? model.StudentCode : "Not Provided")
                                                .FontColor(!string.IsNullOrWhiteSpace(model.StudentCode) ? "#0f172a" : "#64748b");
                                        });
                                    });
                                }
                            });
                        });
                    }

                    if (opts.ShowApplicantInfo && opts.ShowFinalScore)
                    {
                        row.ConstantItem(6);
                    }

                    if (opts.ShowFinalScore)
                    {
                        var statusBg = model.StatusName == "Accepted" ? "#dcfce7" : (model.StatusName == "Rejected" ? "#fee2e2" : "#fef3c7");
                        var statusFg = model.StatusName == "Accepted" ? "#166534" : (model.StatusName == "Rejected" ? "#991b1b" : "#92400e");

                        row.RelativeItem(2).Border(1.5f).BorderColor("#ef3131").Background("#fef2f2").Padding(5).Column(scoreCol =>
                        {
                            scoreCol.Item().AlignCenter().Text("FINAL ADMISSION SCORE").FontSize(7.5f).Bold().FontColor("#b91c1c");
                            scoreCol.Item().AlignCenter().Text($"{model.FinalPercentage:F2}%")
                                .FontSize(20).ExtraBold().FontColor("#ef3131");
                            scoreCol.Item().PaddingTop(1).AlignCenter().Background(statusBg).PaddingVertical(1.5f).PaddingHorizontal(6)
                                .Text($"Status: {model.StatusName}").FontSize(8f).Bold().FontColor(statusFg);
                        });
                    }
                });
            }

            int sectionIdx = 1;

            // 2. Main Breakdown Table
            if (opts.ShowFactorBreakdown)
            {
                col.Item().PaddingTop(5);
                col.Item().Text($"{sectionIdx++}. OVERALL SCORE BREAKDOWN BY EVALUATION FACTOR").FontSize(9f).Bold().FontColor("#ef3131");
                col.Item().PaddingTop(2).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Source Name
                        columns.RelativeColumn(2); // Raw Score
                        columns.RelativeColumn(2); // Snapshot Weight
                        columns.RelativeColumn(2); // Contribution
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#ef3131").Padding(3).Text("Evaluation Factor").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Raw Score").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Snapshot Weight").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Contribution to Final").Bold().FontColor("#ffffff");
                    });

                    RenderFactorRow(table, model.SchoolExamFactor, true);
                    RenderFactorRow(table, model.InterviewFactor, false);
                    RenderFactorRow(table, model.PrepCertFactor, true);
                    RenderFactorRow(table, model.MinistryFactor, false);
                });
            }

            // 3. Preparatory Certificate Grades (Grade 9 Academic Grades) Table
            if (opts.ShowPreparatoryGrades)
            {
                col.Item().PaddingTop(5);
                col.Item().Text($"{sectionIdx++}. PREPARATORY CERTIFICATE GRADES (GRADE 9 ACADEMIC RECORD)").FontSize(9f).Bold().FontColor("#ef3131");
                col.Item().PaddingTop(2).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Subject
                        columns.RelativeColumn(2); // Grade / Score
                        columns.RelativeColumn(2); // Max Score
                        columns.RelativeColumn(2); // Source Notes
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#ef3131").Padding(3).Text("Academic Subject / Certificate").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Student Grade").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Maximum Grade").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Source Record").Bold().FontColor("#ffffff");
                    });

                    // Row 1: Mathematics
                    table.Cell().Background("#ffffff").Padding(3).Text("Mathematics").SemiBold();
                    table.Cell().Background("#ffffff").Padding(3).Text(model.PrepMathScore.HasValue ? $"{model.PrepMathScore.Value:F1}" : "N/A");
                    table.Cell().Background("#ffffff").Padding(3).Text("100.0");
                    table.Cell().Background("#ffffff").Padding(3).Text("Grade 9 Certificate").Italic().FontColor("#64748b");

                    // Row 2: English
                    table.Cell().Background("#f8fafc").Padding(3).Text("English").SemiBold();
                    table.Cell().Background("#f8fafc").Padding(3).Text(model.PrepEnglishScore.HasValue ? $"{model.PrepEnglishScore.Value:F1}" : "N/A");
                    table.Cell().Background("#f8fafc").Padding(3).Text("100.0");
                    table.Cell().Background("#f8fafc").Padding(3).Text("Grade 9 Certificate").Italic().FontColor("#64748b");

                    // Row 3: Total Prep Score
                    table.Cell().Background("#fee2e2").Padding(3).Text("TOTAL PREPARATORY SCORE").Bold().FontColor("#991b1b");
                    table.Cell().Background("#fee2e2").Padding(3).Text(model.PrepTotalScore.HasValue ? $"{model.PrepTotalScore.Value:F1}" : "N/A").Bold().FontColor("#991b1b");
                    table.Cell().Background("#fee2e2").Padding(3).Text("280.0").Bold().FontColor("#991b1b");
                    table.Cell().Background("#fee2e2").Padding(3).Text("3rd Prep Official Total").Bold().FontColor("#991b1b");
                });
            }

            // 4. School Exam Sub-breakdown Table
            if (opts.ShowSchoolExamBreakdown)
            {
                col.Item().PaddingTop(5);
                col.Item().Text($"{sectionIdx++}. SCHOOL ADMISSION EXAM SUBJECT BREAKDOWN").FontSize(9f).Bold().FontColor("#ef3131");
                col.Item().PaddingTop(2).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Subject
                        columns.RelativeColumn(2); // Raw Score
                        columns.RelativeColumn(2); // Subject Weight
                        columns.RelativeColumn(2); // Subtotal Contribution
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#ef3131").Padding(3).Text("Exam Subject").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Raw Mark").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Subject Weight").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Exam Subtotal Share").Bold().FontColor("#ffffff");
                    });

                    bool isAlt = false;
                    foreach (var item in model.SchoolExamSubjects)
                    {
                        var bg = isAlt ? "#f8fafc" : "#ffffff";
                        table.Cell().Background(bg).Padding(3f).Text(item.SubjectName).SemiBold();
                        table.Cell().Background(bg).Padding(3f).Text($"{item.RawScore}");
                        table.Cell().Background(bg).Padding(3f).Text($"{item.SubjectWeight} pts");
                        table.Cell().Background(bg).Padding(3f).Text($"{item.SubtotalContribution:F2}%");
                        isAlt = !isAlt;
                    }

                    // Total Row
                    table.Cell().Background("#fee2e2").Padding(3).Text("TOTAL SCHOOL EXAM").Bold().FontColor("#991b1b");
                    table.Cell().Background("#fee2e2").Padding(3).Text($"{model.SchoolExamEarnedTotal} / {model.SchoolExamMaxTotal:F0}").Bold().FontColor("#991b1b");
                    table.Cell().Background("#fee2e2").Padding(3).Text($"{model.SchoolExamMaxTotal} pts").Bold().FontColor("#991b1b");
                    table.Cell().Background("#fee2e2").Padding(3).Text($"{model.SchoolExamPercentage:F2}%").Bold().FontColor("#991b1b");
                });
            }

            // 5. Interview Evaluation Breakdown Table
            if (opts.ShowInterviewBreakdown)
            {
                col.Item().PaddingTop(5);
                col.Item().Text($"{sectionIdx++}. INTERVIEWER EVALUATION BREAKDOWN").FontSize(9f).Bold().FontColor("#ef3131");
                col.Item().PaddingTop(2).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Interviewer
                        columns.RelativeColumn(2); // Given Mark
                        columns.RelativeColumn(2); // Slot Max
                        columns.RelativeColumn(2); // Score Share %
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#ef3131").Padding(3).Text("Interviewer Name").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Given Mark").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Slot Maximum").Bold().FontColor("#ffffff");
                        header.Cell().Background("#ef3131").Padding(3).Text("Score Percentage").Bold().FontColor("#ffffff");
                    });

                    if (model.InterviewerDetails.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).Background("#ffffff").Padding(3).Text("Interview evaluation pending (No scores recorded)").Italic().FontColor("#64748b");
                    }
                    else
                    {
                        bool isAlt = false;
                        foreach (var item in model.InterviewerDetails)
                        {
                            var bg = isAlt ? "#f8fafc" : "#ffffff";
                            table.Cell().Background(bg).Padding(3f).Text(item.InterviewerName).SemiBold();
                            table.Cell().Background(bg).Padding(3f).Text($"{item.RawScore:F1} / 40");
                            table.Cell().Background(bg).Padding(3f).Text("40.0 pts");
                            table.Cell().Background(bg).Padding(3f).Text($"{item.Percentage:F2}%");
                            isAlt = !isAlt;
                        }

                        // Total Row
                        table.Cell().Background("#fee2e2").Padding(3).Text("TOTAL INTERVIEW SCORE").Bold().FontColor("#991b1b");
                        table.Cell().Background("#fee2e2").Padding(3).Text($"{model.InterviewEarnedTotal:F1} / {model.InterviewMaxTotal:F0}").Bold().FontColor("#991b1b");
                        table.Cell().Background("#fee2e2").Padding(3).Text($"{model.InterviewMaxTotal:F0} pts").Bold().FontColor("#991b1b");
                        table.Cell().Background("#fee2e2").Padding(3).Text($"{model.InterviewPercentage:F2}%").Bold().FontColor("#991b1b");
                    }
                });
            }

            // 6. Transparent Calculation Formula Box
            if (opts.ShowCalculationFormula)
            {
                col.Item().PaddingTop(5);
                col.Item().Border(1).BorderColor("#cbd5e1").Background("#f8fafc").Padding(5).Column(formulaCol =>
                {
                    formulaCol.Item().Text("AUDIT FORMULA & CALCULATION TRANSPARENCY").FontSize(7.5f).Bold().FontColor("#ef3131");
                    formulaCol.Item().PaddingTop(1).Text(model.FormulaString).FontSize(8f).SemiBold().FontColor("#0f172a");
                });
            }
        });
    }

    private void RenderFactorRow(TableDescriptor table, FactorReportItem factor, bool isAlt)
    {
        var bg = isAlt ? "#f8fafc" : "#ffffff";
        var nameColor = factor.Enabled ? "#0f172a" : "#64748b";

        table.Cell().Background(bg).Padding(3.5f).Text(factor.SourceName).SemiBold().FontColor(nameColor);

        if (!factor.Enabled)
        {
            table.Cell().Background(bg).Padding(3.5f).Text("Excluded").Italic().FontColor("#64748b");
            table.Cell().Background(bg).Padding(3.5f).Text("0% (Excluded)").Italic().FontColor("#64748b");
            table.Cell().Background(bg).Padding(3.5f).Text("0.00%").Italic().FontColor("#64748b");
        }
        else
        {
            table.Cell().Background(bg).Padding(3.5f).Text(factor.RawScoreDisplay);
            table.Cell().Background(bg).Padding(3.5f).Text($"{factor.WeightPercentage}%");
            table.Cell().Background(bg).Padding(3.5f).Text($"{factor.ContributionPercentage:F2}%").Bold();
        }
    }

    private void BuildFooter(IContainer container, StudentReportDataModel model)
    {
        container.Column(col =>
        {
            // Authenticity & Audit Section
            col.Item().PaddingBottom(4).Row(row =>
            {
                // Left Column: Audit Data & Metadata
                row.RelativeItem(3).Column(metaCol =>
                {
                    metaCol.Item().Text($"Doc ID: {model.DocumentId}")
                        .FontFamily("Courier").FontSize(7.5f).Bold().FontColor("#334155");
                    metaCol.Item().Text($"Generated By: {model.GeneratedByAdminName} on {model.GeneratedAtCairo:yyyy-MM-dd HH:mm:ss} (Cairo Time)")
                        .FontSize(7.5f).FontColor("#475569");
                    metaCol.Item().Text($"Notice: Refers to an immutable historical snapshot (v{model.VersionNumber}) captured at evaluation.")
                        .FontSize(7.0f).Italic().FontColor("#64748b");
                });

                row.ConstantItem(12);

                // Right Column: Physical Signature / Stamp Box
                row.RelativeItem(2).Border(1).BorderColor("#94a3b8").Background("#ffffff").Height(32).AlignCenter().AlignMiddle()
                    .Column(stampCol =>
                    {
                        stampCol.Item().AlignCenter().Text("Authorized Signature & Stamp")
                            .FontSize(7.5f).Italic().FontColor("#64748b");
                    });
            });

            col.Item().LineHorizontal(1).LineColor("#ef3131");
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text("EL SEWEDY INTERNATIONAL SCHOOL — Official Student Admission Score Report")
                    .FontSize(7.5f).FontColor("#64748b");
                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });
    }
}
