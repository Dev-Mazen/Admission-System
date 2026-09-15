using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.Migrations
{
    /// <inheritdoc />
    public partial class Add_Metadata_To_AdmissionProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountRoles",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleID = table.Column<long>(type: "bigint", nullable: true),
                    AccountID = table.Column<long>(type: "bigint", nullable: true),
                    BusinessEntityName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRoles_Account", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HUB_Settings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    VersionName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Setting_StatusID = table.Column<int>(type: "int", nullable: false),
                    SchoolExamWeight = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    InterviewWeight = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    PreparatoryCertificateWeight = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    MinistryExamWeight = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    ArabicWeight = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    EnglishWeight = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    MathWeight = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    SoftwareWeight = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    IqWeight = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    QuestionsPerSection = table.Column<int>(type: "int", nullable: false),
                    RequireFullQuestionSet = table.Column<int>(type: "int", nullable: false),
                    ExamDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByAccountId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HUB_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: true),
                    BusinessEntity = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Section",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Section", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NationalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    FullNameEN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullNameAR = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Created_at = table.Column<DateOnly>(type: "date", nullable: true, defaultValueSql: "(getdate())"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    StatusId = table.Column<long>(type: "bigint", nullable: false, defaultValue: 4L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Account_Roles",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamQuestion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Choice1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Choice2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Choice3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Choice4 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SectionId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamQuestion_Section_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Section",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionProfile",
                columns: table => new
                {
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoftwareInterviewScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    MathInterviewScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    EnglishInterviewScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ArabicInterviewScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    StudentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MathScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    EnglishScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ThirdPrepScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsAcceptanceLetterReceived = table.Column<bool>(type: "bit", nullable: false),
                    StatusId = table.Column<long>(type: "bigint", nullable: false, defaultValue: 4L),
                    HasOnlineTrainingCourses = table.Column<bool>(type: "bit", nullable: false),
                    HasICDLLicense = table.Column<bool>(type: "bit", nullable: false),
                    HasLaptop = table.Column<bool>(type: "bit", nullable: false),
                    ParentPhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PreviousSchoolType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinistryExamPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ParentOccupation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StreetName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BirthCertificatePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuccessReportPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TuitionFeeReceiptPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferencesSheetPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created_At = table.Column<DateOnly>(type: "date", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionProfile", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_AdmissionProfile_Account",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InterviewScore",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    InterviewerId = table.Column<long>(type: "bigint", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewScore", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewScore_Admin_Account",
                        column: x => x.InterviewerId,
                        principalTable: "Account",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewScore_Student_Account",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Login",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusId = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Login", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Login_Account",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentExamResult",
                columns: table => new
                {
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    ExamArabicScore = table.Column<int>(type: "int", nullable: false),
                    ExamEnglishScore = table.Column<int>(type: "int", nullable: false),
                    ExamMathScore = table.Column<int>(type: "int", nullable: false),
                    ExamSoftwareScore = table.Column<int>(type: "int", nullable: false),
                    ExamIQScore = table.Column<int>(type: "int", nullable: true),
                    ExamStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtensionMinutes = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentExamResult", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_StudentExamResult_Account",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentExamAnswer",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    ExamQuestionId = table.Column<long>(type: "bigint", nullable: false),
                    ChoosedAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentExamAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentExamAnswer_Account",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentExamAnswer_ExamQuestion_ExamQuestionId",
                        column: x => x.ExamQuestionId,
                        principalTable: "ExamQuestion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Account_RoleId",
                table: "Account",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ__Account__A9D10534CCE8DFA0",
                table: "Account",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Account__E9AA32FA70EBBAC3",
                table: "Account",
                column: "NationalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestion_SectionId",
                table: "ExamQuestion",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_HUB_Settings_VersionNumber",
                table: "HUB_Settings",
                column: "VersionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_HUB_Settings_OneActive",
                table: "HUB_Settings",
                column: "Setting_StatusID",
                unique: true,
                filter: "[Setting_StatusID] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewScore_AccountId",
                table: "InterviewScore",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewScore_InterviewerId",
                table: "InterviewScore",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_Login_AccountId",
                table: "Login",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "NonClusteredIndex-20250911-154853",
                table: "Roles",
                column: "RoleName");

            migrationBuilder.CreateIndex(
                name: "UQ_Section_SectionName",
                table: "Section",
                column: "SectionName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAnswer_AccountId",
                table: "StudentExamAnswer",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAnswer_ExamQuestionId",
                table: "StudentExamAnswer",
                column: "ExamQuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountRoles");

            migrationBuilder.DropTable(
                name: "AdmissionProfile");

            migrationBuilder.DropTable(
                name: "HUB_Settings");

            migrationBuilder.DropTable(
                name: "InterviewScore");

            migrationBuilder.DropTable(
                name: "Login");

            migrationBuilder.DropTable(
                name: "StudentExamAnswer");

            migrationBuilder.DropTable(
                name: "StudentExamResult");

            migrationBuilder.DropTable(
                name: "ExamQuestion");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "Section");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
