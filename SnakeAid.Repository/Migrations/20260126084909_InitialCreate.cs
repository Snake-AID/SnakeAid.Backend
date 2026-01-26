using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SnakeAid.Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "AspNetIdentity");

            migrationBuilder.EnsureSchema(
                name: "SnakeAid");

            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "AspNetIdentity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ReputationPoints = table.Column<int>(type: "integer", nullable: false),
                    ReputationStatus = table.Column<int>(type: "integer", nullable: false),
                    SuspendedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspensionReason = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIModels",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EndpointUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ModelParameters = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatchingEnvironments",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatchingEnvironments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpertCertificates",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpertId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    IssuingOrganization = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CertificateUrl = table.Column<string>(type: "text", nullable: false),
                    VerificationStatus = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FilterQuestions",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Question = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FirstAidGuidelines",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstAidGuidelines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationEvents",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionType = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Speed = table.Column<float>(type: "real", nullable: true),
                    Heading = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentCards",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CardNumber = table.Column<string>(type: "text", nullable: false),
                    CardHolderName = table.Column<string>(type: "text", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Cvv = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReputationRules",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SnakeSpecies",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScientificName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CommonName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IdentificationSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PrimaryVenomType = table.Column<int>(type: "integer", nullable: true),
                    Identification = table.Column<string>(type: "jsonb", nullable: false),
                    SymptomsByTime = table.Column<string>(type: "jsonb", nullable: false),
                    FirstAidGuidelineOverride = table.Column<string>(type: "jsonb", nullable: false),
                    RiskLevel = table.Column<float>(type: "real", nullable: false),
                    IsVenomous = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnakeSpecies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specializations",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specializations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                schema: "SnakeAid",
                columns: table => new
                {
                    SettingKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValueType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.SettingKey);
                });

            migrationBuilder.CreateTable(
                name: "TrackingSessions",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionType = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MemberLastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RescuerLastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    EtaMinutes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentFacilities",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContactNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentFacilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppNotifications",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppNotifications_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blogs",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blogs_Accounts_AuthorId",
                        column: x => x.AuthorId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityReports",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdditionalDetails = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityReports_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpertProfiles",
                schema: "SnakeAid",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Biography = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    ConsultationFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false),
                    UnavailableReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertProfiles", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_ExpertProfiles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpertTimeSlots",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpertId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertTimeSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpertTimeSlots_Accounts_ExpertId",
                        column: x => x.ExpertId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberProfiles",
                schema: "SnakeAid",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<float>(type: "real", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false),
                    EmergencyContacts = table.Column<string>(type: "jsonb", nullable: false),
                    HasUnderlyingDisease = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberProfiles", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_MemberProfiles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReputationRawEvents",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceType = table.Column<string>(type: "text", nullable: false),
                    PointsChange = table.Column<int>(type: "integer", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingError = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationRawEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReputationRawEvents_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RescuerProfiles",
                schema: "SnakeAid",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastLocationUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    TotalMissions = table.Column<int>(type: "integer", nullable: false),
                    CompletedMissions = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescuerProfiles", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_RescuerProfiles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalTransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                schema: "AspNetIdentity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFeedbacks",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RaterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFeedbacks_Accounts_RaterId",
                        column: x => x.RaterId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserFeedbacks_Accounts_TargetUserId",
                        column: x => x.TargetUserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "AspNetIdentity",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                schema: "AspNetIdentity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wallets_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FilterOptions",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OptionText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OptionImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilterOptions_FilterQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "SnakeAid",
                        principalTable: "FilterQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenomTypes",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScientificName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SeverityIndex = table.Column<int>(type: "integer", nullable: true),
                    FirstAidGuidelineId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenomTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenomTypes_FirstAidGuidelines_FirstAidGuidelineId",
                        column: x => x.FirstAidGuidelineId,
                        principalSchema: "SnakeAid",
                        principalTable: "FirstAidGuidelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AISnakeClassMappings",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AIModelId = table.Column<int>(type: "integer", nullable: false),
                    SnakeSpeciesId = table.Column<int>(type: "integer", nullable: false),
                    YoloClassName = table.Column<string>(type: "text", nullable: false),
                    YoloClassId = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISnakeClassMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AISnakeClassMappings_AIModels_AIModelId",
                        column: x => x.AIModelId,
                        principalSchema: "SnakeAid",
                        principalTable: "AIModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AISnakeClassMappings_SnakeSpecies_SnakeSpeciesId",
                        column: x => x.SnakeSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibraryMedias",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnakeSpeciesId = table.Column<int>(type: "integer", nullable: false),
                    MediaUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryMedias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryMedias_Accounts_UploadedById",
                        column: x => x.UploadedById,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LibraryMedias_SnakeSpecies_SnakeSpeciesId",
                        column: x => x.SnakeSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SnakeCatchingTariffs",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnakeSpeciesId = table.Column<int>(type: "integer", nullable: false),
                    SizeCategory = table.Column<int>(type: "integer", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnakeCatchingTariffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnakeCatchingTariffs_SnakeSpecies_SnakeSpeciesId",
                        column: x => x.SnakeSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SnakeSpeciesNames",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SnakeSpeciesId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnakeSpeciesNames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnakeSpeciesNames_SnakeSpecies_SnakeSpeciesId",
                        column: x => x.SnakeSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Antivenoms",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TreatmentFacilityId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Antivenoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Antivenoms_TreatmentFacilities_TreatmentFacilityId",
                        column: x => x.TreatmentFacilityId,
                        principalSchema: "SnakeAid",
                        principalTable: "TreatmentFacilities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExpertSpecializations",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpertId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecializationId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertSpecializations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpertSpecializations_ExpertProfiles_ExpertId",
                        column: x => x.ExpertId,
                        principalSchema: "SnakeAid",
                        principalTable: "ExpertProfiles",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpertSpecializations_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalSchema: "SnakeAid",
                        principalTable: "Specializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consultations",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CallerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalleeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ExpertTimeSlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consultations_Accounts_CalleeId",
                        column: x => x.CalleeId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consultations_Accounts_CallerId",
                        column: x => x.CallerId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consultations_ExpertTimeSlots_ExpertTimeSlotId",
                        column: x => x.ExpertTimeSlotId,
                        principalSchema: "SnakeAid",
                        principalTable: "ExpertTimeSlots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReputationTransactions",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReputationRuleId = table.Column<int>(type: "integer", nullable: false),
                    PointsChanged = table.Column<int>(type: "integer", nullable: false),
                    PreviousPoints = table.Column<int>(type: "integer", nullable: false),
                    NewPoints = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReputationTransactions_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReputationTransactions_ReputationRawEvents_RawEventId",
                        column: x => x.RawEventId,
                        principalSchema: "SnakeAid",
                        principalTable: "ReputationRawEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReputationTransactions_ReputationRules_ReputationRuleId",
                        column: x => x.ReputationRuleId,
                        principalSchema: "SnakeAid",
                        principalTable: "ReputationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SnakebiteIncidents",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SymptomsReport = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentSessionNumber = table.Column<int>(type: "integer", nullable: false),
                    CurrentRadiusKm = table.Column<int>(type: "integer", nullable: false),
                    LastSessionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedRescuerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SeverityLevel = table.Column<int>(type: "integer", nullable: true),
                    IncidentOccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnakebiteIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnakebiteIncidents_MemberProfiles_UserId",
                        column: x => x.UserId,
                        principalSchema: "SnakeAid",
                        principalTable: "MemberProfiles",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SnakebiteIncidents_RescuerProfiles_AssignedRescuerId",
                        column: x => x.AssignedRescuerId,
                        principalSchema: "SnakeAid",
                        principalTable: "RescuerProfiles",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SnakeCatchingRequests",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AdditionalDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreferredTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedRescuerId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstimatedPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnakeCatchingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnakeCatchingRequests_MemberProfiles_UserId",
                        column: x => x.UserId,
                        principalSchema: "SnakeAid",
                        principalTable: "MemberProfiles",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SnakeCatchingRequests_RescuerProfiles_AssignedRescuerId",
                        column: x => x.AssignedRescuerId,
                        principalSchema: "SnakeAid",
                        principalTable: "RescuerProfiles",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WalletWithdraws",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BankAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletWithdraws", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletWithdraws_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletWithdraws_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalSchema: "SnakeAid",
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FilterSnakeMappings",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilterOptionId = table.Column<int>(type: "integer", nullable: false),
                    SnakeSpeciesId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterSnakeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilterSnakeMappings_FilterOptions_FilterOptionId",
                        column: x => x.FilterOptionId,
                        principalSchema: "SnakeAid",
                        principalTable: "FilterOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FilterSnakeMappings_SnakeSpecies_SnakeSpeciesId",
                        column: x => x.SnakeSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpeciesVenoms",
                schema: "SnakeAid",
                columns: table => new
                {
                    SnakeSpeciesId = table.Column<int>(type: "integer", nullable: false),
                    VenomTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeciesVenoms", x => new { x.SnakeSpeciesId, x.VenomTypeId });
                    table.ForeignKey(
                        name: "FK_SpeciesVenoms_SnakeSpecies_SnakeSpeciesId",
                        column: x => x.SnakeSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpeciesVenoms_VenomTypes_VenomTypeId",
                        column: x => x.VenomTypeId,
                        principalSchema: "SnakeAid",
                        principalTable: "VenomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SymptomConfigs",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttributeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AttributeLabel = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UIHint = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    TimeScoresJson = table.Column<string>(type: "jsonb", nullable: true),
                    VenomTypeId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymptomConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SymptomConfigs_VenomTypes_VenomTypeId",
                        column: x => x.VenomTypeId,
                        principalSchema: "SnakeAid",
                        principalTable: "VenomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SpeciesAntivenoms",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SnakeSpeciesId = table.Column<int>(type: "integer", nullable: false),
                    AntivenomId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeciesAntivenoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpeciesAntivenoms_Antivenoms_AntivenomId",
                        column: x => x.AntivenomId,
                        principalSchema: "SnakeAid",
                        principalTable: "Antivenoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpeciesAntivenoms_SnakeSpecies_SnakeSpeciesId",
                        column: x => x.SnakeSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalSchema: "SnakeAid",
                        principalTable: "Consultations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationBookings",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpertId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    BookedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: true),
                    TimeSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationBookings_Accounts_ExpertId",
                        column: x => x.ExpertId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultationBookings_Accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultationBookings_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalSchema: "SnakeAid",
                        principalTable: "Consultations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ConsultationBookings_ExpertTimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalSchema: "SnakeAid",
                        principalTable: "ExpertTimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RescueMissions",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RescuerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ActualCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescueMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RescueMissions_RescuerProfiles_RescuerId",
                        column: x => x.RescuerId,
                        principalSchema: "SnakeAid",
                        principalTable: "RescuerProfiles",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RescueMissions_SnakebiteIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakebiteIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RescueRequestSessions",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionNumber = table.Column<int>(type: "integer", nullable: false),
                    RadiusKm = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TriggerType = table.Column<int>(type: "integer", nullable: false),
                    RescuersPinged = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescueRequestSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RescueRequestSessions_SnakebiteIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakebiteIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatchingRequestDetails",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnakeCatchingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnakeSpeciesId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatchingRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatchingRequestDetails_SnakeCatchingRequests_SnakeCatchingR~",
                        column: x => x.SnakeCatchingRequestId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeCatchingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatchingRequestDetails_SnakeSpecies_SnakeSpeciesId",
                        column: x => x.SnakeSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportMedias",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceType = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    MediaUrl = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    UploadBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: true),
                    RequiresAIProcessing = table.Column<bool>(type: "boolean", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SnakeCatchingRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SnakebiteIncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportMedias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportMedias_SnakeCatchingRequests_SnakeCatchingRequestId",
                        column: x => x.SnakeCatchingRequestId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeCatchingRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportMedias_SnakebiteIncidents_SnakebiteIncidentId",
                        column: x => x.SnakebiteIncidentId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakebiteIncidents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SnakeCatchingMissions",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RescuerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnakeCatchingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ActualCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnakeCatchingMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnakeCatchingMissions_RescuerProfiles_RescuerId",
                        column: x => x.RescuerId,
                        principalSchema: "SnakeAid",
                        principalTable: "RescuerProfiles",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SnakeCatchingMissions_SnakeCatchingRequests_SnakeCatchingRe~",
                        column: x => x.SnakeCatchingRequestId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeCatchingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationPingRequests",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RescuerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpertId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RescueMissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationPingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationPingRequests_Accounts_ExpertId",
                        column: x => x.ExpertId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultationPingRequests_Accounts_RescuerId",
                        column: x => x.RescuerId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultationPingRequests_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalSchema: "SnakeAid",
                        principalTable: "Consultations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ConsultationPingRequests_RescueMissions_RescueMissionId",
                        column: x => x.RescueMissionId,
                        principalSchema: "SnakeAid",
                        principalTable: "RescueMissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RescuerRequests",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RescuerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescuerRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RescuerRequests_RescueRequestSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "SnakeAid",
                        principalTable: "RescueRequestSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RescuerRequests_RescuerProfiles_RescuerId",
                        column: x => x.RescuerId,
                        principalSchema: "SnakeAid",
                        principalTable: "RescuerProfiles",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RescuerRequests_SnakebiteIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakebiteIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SnakeAIRecognitionResults",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportMediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AIModelId = table.Column<int>(type: "integer", nullable: false),
                    YoloClassName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    DetectedSpeciesId = table.Column<int>(type: "integer", nullable: true),
                    IsMapped = table.Column<bool>(type: "boolean", nullable: false),
                    AllDetections = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpertId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpertVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpertCorrectedSpeciesId = table.Column<int>(type: "integer", nullable: true),
                    ExpertNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ClassMappingId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnakeAIRecognitionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnakeAIRecognitionResults_AIModels_AIModelId",
                        column: x => x.AIModelId,
                        principalSchema: "SnakeAid",
                        principalTable: "AIModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SnakeAIRecognitionResults_AISnakeClassMappings_ClassMapping~",
                        column: x => x.ClassMappingId,
                        principalSchema: "SnakeAid",
                        principalTable: "AISnakeClassMappings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SnakeAIRecognitionResults_Accounts_ExpertId",
                        column: x => x.ExpertId,
                        principalSchema: "AspNetIdentity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SnakeAIRecognitionResults_ReportMedias_ReportMediaId",
                        column: x => x.ReportMediaId,
                        principalSchema: "SnakeAid",
                        principalTable: "ReportMedias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SnakeAIRecognitionResults_SnakeSpecies_DetectedSpeciesId",
                        column: x => x.DetectedSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SnakeAIRecognitionResults_SnakeSpecies_ExpertCorrectedSpeci~",
                        column: x => x.ExpertCorrectedSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CatchingMissionDetails",
                schema: "SnakeAid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnakeCatchingMissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnakeSpeciesId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatchingMissionDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatchingMissionDetails_SnakeCatchingMissions_SnakeCatchingM~",
                        column: x => x.SnakeCatchingMissionId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeCatchingMissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatchingMissionDetails_SnakeSpecies_SnakeSpeciesId",
                        column: x => x.SnakeSpeciesId,
                        principalSchema: "SnakeAid",
                        principalTable: "SnakeSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "AspNetIdentity",
                table: "Accounts",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_IsActive",
                schema: "AspNetIdentity",
                table: "Accounts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Role",
                schema: "AspNetIdentity",
                table: "Accounts",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "AspNetIdentity",
                table: "Accounts",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_IsActive",
                schema: "SnakeAid",
                table: "AIModels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_IsDefault",
                schema: "SnakeAid",
                table: "AIModels",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_AIModels_Version",
                schema: "SnakeAid",
                table: "AIModels",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AISnakeClassMappings_AIModelId_YoloClassId",
                schema: "SnakeAid",
                table: "AISnakeClassMappings",
                columns: new[] { "AIModelId", "YoloClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AISnakeClassMappings_IsActive",
                schema: "SnakeAid",
                table: "AISnakeClassMappings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AISnakeClassMappings_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "AISnakeClassMappings",
                column: "SnakeSpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_Antivenoms_TreatmentFacilityId",
                schema: "SnakeAid",
                table: "Antivenoms",
                column: "TreatmentFacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_IsRead",
                schema: "SnakeAid",
                table: "AppNotifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_UserId",
                schema: "SnakeAid",
                table: "AppNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_UserId_IsRead",
                schema: "SnakeAid",
                table: "AppNotifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_AuthorId",
                schema: "SnakeAid",
                table: "Blogs",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_Status",
                schema: "SnakeAid",
                table: "Blogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CatchingMissionDetails_SnakeCatchingMissionId",
                schema: "SnakeAid",
                table: "CatchingMissionDetails",
                column: "SnakeCatchingMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchingMissionDetails_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "CatchingMissionDetails",
                column: "SnakeSpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchingRequestDetails_SnakeCatchingRequestId",
                schema: "SnakeAid",
                table: "CatchingRequestDetails",
                column: "SnakeCatchingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchingRequestDetails_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "CatchingRequestDetails",
                column: "SnakeSpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConsultationId",
                schema: "SnakeAid",
                table: "ChatMessages",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                schema: "SnakeAid",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SentAt",
                schema: "SnakeAid",
                table: "ChatMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_UserId",
                schema: "SnakeAid",
                table: "CommunityReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationBookings_ConsultationId",
                schema: "SnakeAid",
                table: "ConsultationBookings",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationBookings_ExpertId",
                schema: "SnakeAid",
                table: "ConsultationBookings",
                column: "ExpertId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationBookings_Status",
                schema: "SnakeAid",
                table: "ConsultationBookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationBookings_TimeSlotId",
                schema: "SnakeAid",
                table: "ConsultationBookings",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationBookings_UserId",
                schema: "SnakeAid",
                table: "ConsultationBookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationPingRequests_ConsultationId",
                schema: "SnakeAid",
                table: "ConsultationPingRequests",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationPingRequests_ExpertId",
                schema: "SnakeAid",
                table: "ConsultationPingRequests",
                column: "ExpertId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationPingRequests_ExpiresAt",
                schema: "SnakeAid",
                table: "ConsultationPingRequests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationPingRequests_RescueMissionId",
                schema: "SnakeAid",
                table: "ConsultationPingRequests",
                column: "RescueMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationPingRequests_RescuerId",
                schema: "SnakeAid",
                table: "ConsultationPingRequests",
                column: "RescuerId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationPingRequests_Status",
                schema: "SnakeAid",
                table: "ConsultationPingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_CalleeId",
                schema: "SnakeAid",
                table: "Consultations",
                column: "CalleeId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_CallerId",
                schema: "SnakeAid",
                table: "Consultations",
                column: "CallerId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_ExpertTimeSlotId",
                schema: "SnakeAid",
                table: "Consultations",
                column: "ExpertTimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_RoomId",
                schema: "SnakeAid",
                table: "Consultations",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_Status",
                schema: "SnakeAid",
                table: "Consultations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertSpecializations_ExpertId_SpecializationId",
                schema: "SnakeAid",
                table: "ExpertSpecializations",
                columns: new[] { "ExpertId", "SpecializationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpertSpecializations_SpecializationId",
                schema: "SnakeAid",
                table: "ExpertSpecializations",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertTimeSlots_ExpertId",
                schema: "SnakeAid",
                table: "ExpertTimeSlots",
                column: "ExpertId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertTimeSlots_ExpertId_StartTime",
                schema: "SnakeAid",
                table: "ExpertTimeSlots",
                columns: new[] { "ExpertId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpertTimeSlots_Status",
                schema: "SnakeAid",
                table: "ExpertTimeSlots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FilterOptions_IsActive",
                schema: "SnakeAid",
                table: "FilterOptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FilterOptions_QuestionId",
                schema: "SnakeAid",
                table: "FilterOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_FilterQuestions_IsActive",
                schema: "SnakeAid",
                table: "FilterQuestions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FilterSnakeMappings_FilterOptionId_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "FilterSnakeMappings",
                columns: new[] { "FilterOptionId", "SnakeSpeciesId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FilterSnakeMappings_IsActive",
                schema: "SnakeAid",
                table: "FilterSnakeMappings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FilterSnakeMappings_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "FilterSnakeMappings",
                column: "SnakeSpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_FirstAidGuidelines_Type",
                schema: "SnakeAid",
                table: "FirstAidGuidelines",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryMedias_IsActive",
                schema: "SnakeAid",
                table: "LibraryMedias",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryMedias_IsPublic",
                schema: "SnakeAid",
                table: "LibraryMedias",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryMedias_MediaType",
                schema: "SnakeAid",
                table: "LibraryMedias",
                column: "MediaType");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryMedias_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "LibraryMedias",
                column: "SnakeSpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryMedias_UploadedById",
                schema: "SnakeAid",
                table: "LibraryMedias",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_LocationEvents_AccountId",
                schema: "SnakeAid",
                table: "LocationEvents",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationEvents_RecordedAt",
                schema: "SnakeAid",
                table: "LocationEvents",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LocationEvents_SessionId",
                schema: "SnakeAid",
                table: "LocationEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationEvents_SessionId_RecordedAt",
                schema: "SnakeAid",
                table: "LocationEvents",
                columns: new[] { "SessionId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportMedias_ReferenceId",
                schema: "SnakeAid",
                table: "ReportMedias",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMedias_ReferenceId_ReferenceType",
                schema: "SnakeAid",
                table: "ReportMedias",
                columns: new[] { "ReferenceId", "ReferenceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportMedias_ReferenceType",
                schema: "SnakeAid",
                table: "ReportMedias",
                column: "ReferenceType");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMedias_RequiresAIProcessing",
                schema: "SnakeAid",
                table: "ReportMedias",
                column: "RequiresAIProcessing");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMedias_SnakebiteIncidentId",
                schema: "SnakeAid",
                table: "ReportMedias",
                column: "SnakebiteIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMedias_SnakeCatchingRequestId",
                schema: "SnakeAid",
                table: "ReportMedias",
                column: "SnakeCatchingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationRawEvents_EventType",
                schema: "SnakeAid",
                table: "ReputationRawEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationRawEvents_IsProcessed",
                schema: "SnakeAid",
                table: "ReputationRawEvents",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationRawEvents_ReferenceId_ReferenceType",
                schema: "SnakeAid",
                table: "ReputationRawEvents",
                columns: new[] { "ReferenceId", "ReferenceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ReputationRawEvents_UserId",
                schema: "SnakeAid",
                table: "ReputationRawEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationRules_Name",
                schema: "SnakeAid",
                table: "ReputationRules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReputationTransactions_RawEventId",
                schema: "SnakeAid",
                table: "ReputationTransactions",
                column: "RawEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationTransactions_ReputationRuleId",
                schema: "SnakeAid",
                table: "ReputationTransactions",
                column: "ReputationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationTransactions_UserId",
                schema: "SnakeAid",
                table: "ReputationTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RescueMissions_IncidentId",
                schema: "SnakeAid",
                table: "RescueMissions",
                column: "IncidentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RescueMissions_RescuerId",
                schema: "SnakeAid",
                table: "RescueMissions",
                column: "RescuerId");

            migrationBuilder.CreateIndex(
                name: "IX_RescueMissions_Status",
                schema: "SnakeAid",
                table: "RescueMissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequestSessions_IncidentId",
                schema: "SnakeAid",
                table: "RescueRequestSessions",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequestSessions_IncidentId_SessionNumber",
                schema: "SnakeAid",
                table: "RescueRequestSessions",
                columns: new[] { "IncidentId", "SessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RescueRequestSessions_Status",
                schema: "SnakeAid",
                table: "RescueRequestSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerProfiles_IsOnline",
                schema: "SnakeAid",
                table: "RescuerProfiles",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerProfiles_Type",
                schema: "SnakeAid",
                table: "RescuerProfiles",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerRequests_ExpiredAt",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "ExpiredAt");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerRequests_IncidentId",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerRequests_RescuerId",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "RescuerId");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerRequests_SessionId",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RescuerRequests_Status",
                schema: "SnakeAid",
                table: "RescuerRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeAIRecognitionResults_AIModelId",
                schema: "SnakeAid",
                table: "SnakeAIRecognitionResults",
                column: "AIModelId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeAIRecognitionResults_ClassMappingId",
                schema: "SnakeAid",
                table: "SnakeAIRecognitionResults",
                column: "ClassMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeAIRecognitionResults_DetectedSpeciesId",
                schema: "SnakeAid",
                table: "SnakeAIRecognitionResults",
                column: "DetectedSpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeAIRecognitionResults_ExpertCorrectedSpeciesId",
                schema: "SnakeAid",
                table: "SnakeAIRecognitionResults",
                column: "ExpertCorrectedSpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeAIRecognitionResults_ExpertId",
                schema: "SnakeAid",
                table: "SnakeAIRecognitionResults",
                column: "ExpertId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeAIRecognitionResults_IsMapped",
                schema: "SnakeAid",
                table: "SnakeAIRecognitionResults",
                column: "IsMapped");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeAIRecognitionResults_ReportMediaId",
                schema: "SnakeAid",
                table: "SnakeAIRecognitionResults",
                column: "ReportMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeAIRecognitionResults_Status",
                schema: "SnakeAid",
                table: "SnakeAIRecognitionResults",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SnakebiteIncidents_AssignedRescuerId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                column: "AssignedRescuerId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakebiteIncidents_Status",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SnakebiteIncidents_UserId",
                schema: "SnakeAid",
                table: "SnakebiteIncidents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingMissions_RequestId",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions",
                column: "SnakeCatchingRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingMissions_RescuerId",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions",
                column: "RescuerId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingMissions_Status",
                schema: "SnakeAid",
                table: "SnakeCatchingMissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingRequests_AssignedRescuerId",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                column: "AssignedRescuerId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingRequests_RequestDate",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                column: "RequestDate");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingRequests_Status",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingRequests_UserId",
                schema: "SnakeAid",
                table: "SnakeCatchingRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingTariffs_IsActive",
                schema: "SnakeAid",
                table: "SnakeCatchingTariffs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeCatchingTariffs_SnakeSpeciesId_SizeCategory",
                schema: "SnakeAid",
                table: "SnakeCatchingTariffs",
                columns: new[] { "SnakeSpeciesId", "SizeCategory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnakeSpecies_IsActive",
                schema: "SnakeAid",
                table: "SnakeSpecies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeSpecies_IsVenomous",
                schema: "SnakeAid",
                table: "SnakeSpecies",
                column: "IsVenomous");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeSpecies_ScientificName",
                schema: "SnakeAid",
                table: "SnakeSpecies",
                column: "ScientificName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnakeSpecies_Slug",
                schema: "SnakeAid",
                table: "SnakeSpecies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnakeSpeciesNames_Name",
                schema: "SnakeAid",
                table: "SnakeSpeciesNames",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SnakeSpeciesNames_Slug",
                schema: "SnakeAid",
                table: "SnakeSpeciesNames",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnakeSpeciesNames_SnakeSpeciesId",
                schema: "SnakeAid",
                table: "SnakeSpeciesNames",
                column: "SnakeSpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_Specializations_Name",
                schema: "SnakeAid",
                table: "Specializations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesAntivenoms_AntivenomId",
                schema: "SnakeAid",
                table: "SpeciesAntivenoms",
                column: "AntivenomId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesAntivenoms_SnakeSpeciesId_AntivenomId",
                schema: "SnakeAid",
                table: "SpeciesAntivenoms",
                columns: new[] { "SnakeSpeciesId", "AntivenomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesVenoms_VenomTypeId",
                schema: "SnakeAid",
                table: "SpeciesVenoms",
                column: "VenomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SymptomConfigs_AttributeKey",
                schema: "SnakeAid",
                table: "SymptomConfigs",
                column: "AttributeKey");

            migrationBuilder.CreateIndex(
                name: "IX_SymptomConfigs_DisplayOrder",
                schema: "SnakeAid",
                table: "SymptomConfigs",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SymptomConfigs_IsActive",
                schema: "SnakeAid",
                table: "SymptomConfigs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SymptomConfigs_VenomTypeId",
                schema: "SnakeAid",
                table: "SymptomConfigs",
                column: "VenomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_IsActive",
                schema: "SnakeAid",
                table: "TrackingSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_SessionId",
                schema: "SnakeAid",
                table: "TrackingSessions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_SessionId_SessionType",
                schema: "SnakeAid",
                table: "TrackingSessions",
                columns: new[] { "SessionId", "SessionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSessions_SessionType",
                schema: "SnakeAid",
                table: "TrackingSessions",
                column: "SessionType");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreatedAt",
                schema: "SnakeAid",
                table: "Transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ReferenceId",
                schema: "SnakeAid",
                table: "Transactions",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionType",
                schema: "SnakeAid",
                table: "Transactions",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                schema: "SnakeAid",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentFacilities_IsActive",
                schema: "SnakeAid",
                table: "TreatmentFacilities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentFacilities_Name",
                schema: "SnakeAid",
                table: "TreatmentFacilities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "AspNetIdentity",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_RaterId",
                schema: "SnakeAid",
                table: "UserFeedbacks",
                column: "RaterId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_ReferenceId_Type",
                schema: "SnakeAid",
                table: "UserFeedbacks",
                columns: new[] { "ReferenceId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_TargetUserId",
                schema: "SnakeAid",
                table: "UserFeedbacks",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_Type",
                schema: "SnakeAid",
                table: "UserFeedbacks",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "AspNetIdentity",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VenomTypes_FirstAidGuidelineId",
                schema: "SnakeAid",
                table: "VenomTypes",
                column: "FirstAidGuidelineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenomTypes_IsActive",
                schema: "SnakeAid",
                table: "VenomTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VenomTypes_Name",
                schema: "SnakeAid",
                table: "VenomTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                schema: "SnakeAid",
                table: "Wallets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdraws_Status",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdraws_UserId",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdraws_WalletId",
                schema: "SnakeAid",
                table: "WalletWithdraws",
                column: "WalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppNotifications",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "Blogs",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "CatchingEnvironments",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "CatchingMissionDetails",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "CatchingRequestDetails",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ChatMessages",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "CommunityReports",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ConsultationBookings",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ConsultationPingRequests",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ExpertCertificates",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ExpertSpecializations",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "FilterSnakeMappings",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "Lessons",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "LibraryMedias",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "LocationEvents",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "PaymentCards",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ReputationTransactions",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "RescuerRequests",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SnakeAIRecognitionResults",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SnakeCatchingTariffs",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SnakeSpeciesNames",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SpeciesAntivenoms",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SpeciesVenoms",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SymptomConfigs",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SystemSettings",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "TrackingSessions",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "AspNetIdentity");

            migrationBuilder.DropTable(
                name: "UserFeedbacks",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "UserLogins",
                schema: "AspNetIdentity");

            migrationBuilder.DropTable(
                name: "UserTokens",
                schema: "AspNetIdentity");

            migrationBuilder.DropTable(
                name: "WalletWithdraws",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SnakeCatchingMissions",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "Consultations",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "RescueMissions",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ExpertProfiles",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "Specializations",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "FilterOptions",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ReputationRawEvents",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ReputationRules",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "RescueRequestSessions",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "AISnakeClassMappings",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ReportMedias",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "Antivenoms",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "VenomTypes",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "Wallets",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "ExpertTimeSlots",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "FilterQuestions",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "AIModels",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SnakeSpecies",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SnakeCatchingRequests",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "SnakebiteIncidents",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "TreatmentFacilities",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "FirstAidGuidelines",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "MemberProfiles",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "RescuerProfiles",
                schema: "SnakeAid");

            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "AspNetIdentity");
        }
    }
}
