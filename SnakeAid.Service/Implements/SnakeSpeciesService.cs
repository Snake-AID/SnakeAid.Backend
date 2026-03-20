using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.LibraryMedia;
using SnakeAid.Core.Requests.SnakeSpecies;
using SnakeAid.Core.Responses.SnakeSpecies;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class SnakeSpeciesService : ISnakeSpeciesService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILibraryMediaService _libraryMediaService;
        private readonly ILogger<SnakeSpeciesService> _logger;

        public SnakeSpeciesService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILibraryMediaService libraryMediaService,
            ILogger<SnakeSpeciesService> logger)
        {
            _unitOfWork = unitOfWork;
            _libraryMediaService = libraryMediaService;
            _logger = logger;
        }

        public async Task<List<ListSnakeSpeciesResponse>> GetAllSnakeSpeciesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all snake species");

                var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .GetListAsync(
                        predicate: s => s.IsActive
                    );

                _logger.LogInformation("Retrieved {Count} snake species", snakeSpecies.Count);

                return snakeSpecies.Adapt<List<ListSnakeSpeciesResponse>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all snake species");
                throw;
            }
        }

        public async Task<DetailSnakeSpeciesResponse> GetSnakeSpeciesByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching snake species with ID: {Id}", id);

                var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .FirstOrDefaultAsync(
                        predicate: s => s.Id == id && s.IsActive,
                        include: query => query
                            .Include(s => s.AlternativeNames)
                            .Include(s => s.SpeciesAntivenoms)
                                .ThenInclude(sa => sa.Antivenom)
                            .Include(s => s.SpeciesVenoms)
                                .ThenInclude(sv => sv.VenomType)
                    );

                if (snakeSpecies == null)
                {
                    _logger.LogWarning("Snake species with ID {Id} not found", id);
                    throw new NotFoundException($"Snake species with ID {id} not found.");
                }

                _logger.LogInformation("Successfully retrieved snake species with ID: {Id}", id);

                return snakeSpecies.Adapt<DetailSnakeSpeciesResponse>();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching snake species with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<SearchSnakeSpeciesResponse>> SearchSnakeSpeciesAsync(string query)
        {
            try
            {
                _logger.LogInformation("Searching snake species with query: {Query}", query);

                if (string.IsNullOrWhiteSpace(query))
                {
                    return new List<SearchSnakeSpeciesResponse>();
                }

                var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .GetListAsync(
                        predicate: s => s.IsActive &&
                            (s.ScientificName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                             s.CommonName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                             s.AlternativeNames.Any(sn => sn.Name.Contains(query, StringComparison.OrdinalIgnoreCase))),
                        include: query => query
                            .Include(s => s.SpeciesVenoms)
                                .ThenInclude(sv => sv.VenomType)
                            .Include(s => s.SpeciesAntivenoms)
                                .ThenInclude(sa => sa.Antivenom)
                    );

                var result = snakeSpecies.Select(s => new SearchSnakeSpeciesResponse
                {
                    Id = s.Id,
                    ScientificName = s.ScientificName,
                    CommonName = s.CommonName,
                    ImageUrl = s.ImageUrl,
                    IsVenomous = s.IsVenomous,
                    PrimaryVenomType = s.PrimaryVenomType,
                    Venoms = s.SpeciesVenoms.Select(sv => new Core.Responses.SnakeSpecies.VenomInfo
                    {
                        VenomType = sv.VenomType?.Name ?? "Unknown",
                        Description = sv.VenomType?.Description ?? ""
                    }).ToList(),
                    Antivenoms = s.SpeciesAntivenoms.Select(sa => new Core.Responses.SnakeSpecies.AntivenomInfo
                    {
                        AntivenomName = sa.Antivenom?.Name ?? "Unknown",
                        Manufacturer = sa.Antivenom?.Manufacturer ?? "",
                        Effectiveness = sa.Antivenom?.Description ?? ""
                    }).ToList()
                }).ToList();

                _logger.LogInformation("Found {Count} snake species matching query: {Query}", result.Count, query);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching snake species with query: {Query}", query);
                throw;
            }
        }

        public async Task<DetailSnakeSpeciesResponse> CreateSnakeSpeciesFromExcelAsync(CreateSnakeSpeciesFromExcelRequest request, ClaimsPrincipal user, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            ValidateExcelFile(request.ExcelFile);
            ValidateImageFile(request.ImageFile);

            ParsedSnakeSpeciesExcel parsed;

            try
            {
                using var stream = request.ExcelFile.OpenReadStream();
                using var workbook = new XLWorkbook(stream);

                var basicInfoSheet = GetWorksheet(workbook, 1, "basic info");
                var identificationSheet = GetWorksheet(workbook, 2, "Identification");
                var symptomsSheet = GetWorksheet(workbook, 3, "SymptomsByTime");
                var firstAidSheet = GetWorksheet(workbook, 4, "FirstAidGuidelineOverride");
                var antiVenomSheet = GetWorksheet(workbook, 5, "AntiVenom");
                var venomSheet = GetWorksheet(workbook, 6, "Venom");
                var alternativeNameSheet = GetWorksheet(workbook, 7, "AlternativeName");

                parsed = new ParsedSnakeSpeciesExcel
                {
                    BasicInfo = ParseBasicInfoSheet(basicInfoSheet),
                    Identification = ParseIdentificationSheet(identificationSheet),
                    SymptomsByTime = ParseSymptomsSheet(symptomsSheet),
                    FirstAidGuidelineOverride = ParseFirstAidSheet(firstAidSheet),
                    Antivenoms = ParseAntivenomSheet(antiVenomSheet),
                    Venoms = ParseVenomSheet(venomSheet),
                    AlternativeNames = ParseAlternativeNameSheet(alternativeNameSheet)
                };
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse snake species excel file: {FileName}", request.ExcelFile.FileName);
                throw new BadRequestException("Excel file format is invalid. Please verify all required sheets and columns.");
            }

            var scientificNameExists = await _unitOfWork.GetRepository<SnakeSpecies>()
                .ExistsAsync(s => s.ScientificName == parsed.BasicInfo.ScientificName, ct);

            if (scientificNameExists)
            {
                throw new BadRequestException($"Snake species with scientific name '{parsed.BasicInfo.ScientificName}' already exists.");
            }

            var slugExists = await _unitOfWork.GetRepository<SnakeSpecies>()
                .ExistsAsync(s => s.Slug == parsed.BasicInfo.Slug, ct);

            if (slugExists)
            {
                throw new BadRequestException($"Snake species with slug '{parsed.BasicInfo.Slug}' already exists.");
            }

            var createdLibraryMedia = await _libraryMediaService.CreateAsync(new CreateLibraryMediaRequest
            {
                File = request.ImageFile,
                MediaType = MediaType.Image,
                IsActive = true,
                IsPublic = true,
                SnakeSpeciesId = null
            }, user, ct);

            var entity = new SnakeSpecies
            {
                ScientificName = parsed.BasicInfo.ScientificName,
                Slug = parsed.BasicInfo.Slug,
                CommonName = parsed.BasicInfo.CommonName ?? string.Empty,
                ImageUrl = createdLibraryMedia.MediaUrl,
                Description = parsed.BasicInfo.Description ?? string.Empty,
                IdentificationSummary = parsed.BasicInfo.IdentificationSummary ?? string.Empty,
                PrimaryVenomType = parsed.BasicInfo.PrimaryVenomType,
                Identification = parsed.Identification,
                SymptomsByTime = parsed.SymptomsByTime,
                FirstAidGuidelineOverride = parsed.FirstAidGuidelineOverride,
                RiskLevel = parsed.BasicInfo.RiskLevel,
                IsVenomous = parsed.BasicInfo.IsVenomous,
                IsActive = parsed.BasicInfo.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<SnakeSpecies>().InsertAsync(entity, ct);
            await _unitOfWork.CommitAsync();

            await ApplyPostCreateMappingsAsync(entity.Id, parsed, ct);

            return entity.Adapt<DetailSnakeSpeciesResponse>();
        }

        private static IXLWorksheet GetWorksheet(XLWorkbook workbook, int position, string logicalName)
        {
            if (workbook.Worksheets.Count < position)
            {
                throw new BadRequestException($"Excel file must contain at least 7 sheets. Missing sheet for {logicalName}.");
            }

            return workbook.Worksheet(position);
        }

        private static void ValidateExcelFile(Microsoft.AspNetCore.Http.IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                throw new BadRequestException("ExcelFile is required.");
            }

            var extension = Path.GetExtension(excelFile.FileName)?.ToLowerInvariant();
            if (extension != ".xlsx")
            {
                throw new BadRequestException("ExcelFile must be .xlsx format.");
            }
        }

        private static void ValidateImageFile(Microsoft.AspNetCore.Http.IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                throw new BadRequestException("ImageFile is required.");
            }
        }

        private static BasicInfoRow ParseBasicInfoSheet(IXLWorksheet worksheet)
        {
            var map = BuildHeaderMap(worksheet);
            var dataRow = worksheet.Row(2);

            var scientificName = GetRequiredCell(dataRow, map, "ScientificName");
            var slug = GetRequiredCell(dataRow, map, "Slug");

            var basicInfo = new BasicInfoRow
            {
                ScientificName = scientificName,
                Slug = slug,
                CommonName = GetCell(dataRow, map, "CommonName"),
                Description = GetCell(dataRow, map, "Description"),
                IdentificationSummary = GetCell(dataRow, map, "IdentificationSummary"),
                PrimaryVenomType = ParseNullableEnum<PrimaryVenomType>(GetCell(dataRow, map, "PrimaryVenomType"), "PrimaryVenomType"),
                RiskLevel = ParseFloat(GetRequiredCell(dataRow, map, "RiskLevel"), "RiskLevel"),
                IsVenomous = ParseBoolean(GetRequiredCell(dataRow, map, "IsVenomous"), "IsVenomous"),
                IsActive = ParseBoolean(GetCell(dataRow, map, "IsActive"), "IsActive", true)
            };

            return basicInfo;
        }

        private static IdentificationFeature? ParseIdentificationSheet(IXLWorksheet worksheet)
        {
            var map = BuildHeaderMap(worksheet);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            var physicalTraits = new List<string>();
            var behaviors = new List<string>();
            string? habitat = null;

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                physicalTraits.AddRange(SplitCellValues(GetCell(row, map, "PhysicalTraits")));
                behaviors.AddRange(SplitCellValues(GetCell(row, map, "Behaviors")));

                var rowHabitat = GetCell(row, map, "Habitat");
                if (!string.IsNullOrWhiteSpace(rowHabitat) && string.IsNullOrWhiteSpace(habitat))
                {
                    habitat = rowHabitat;
                }
            }

            if (physicalTraits.Count == 0 && behaviors.Count == 0 && string.IsNullOrWhiteSpace(habitat))
            {
                return null;
            }

            return new IdentificationFeature
            {
                PhysicalTraits = physicalTraits.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Behaviors = behaviors.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Habitat = habitat ?? string.Empty
            };
        }

        private static List<SymptomTimeline>? ParseSymptomsSheet(IXLWorksheet worksheet)
        {
            var map = BuildHeaderMap(worksheet);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            var symptoms = new List<SymptomTimeline>();

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                var timeRange = GetCell(row, map, "TimeRange");
                if (string.IsNullOrWhiteSpace(timeRange))
                {
                    continue;
                }

                var signs = SplitCellValues(GetCell(row, map, "Signs"));
                var isCritical = ParseBoolean(GetCell(row, map, "IsCritical"), "IsCritical", false);

                symptoms.Add(new SymptomTimeline
                {
                    TimeRange = timeRange,
                    Signs = signs,
                    IsCritical = isCritical
                });
            }

            return symptoms.Count == 0 ? null : symptoms;
        }

        private static List<AntivenomSheetRow> ParseAntivenomSheet(IXLWorksheet worksheet)
        {
            var map = BuildHeaderMap(worksheet);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            var result = new List<AntivenomSheetRow>();

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                var antivenomIdRaw = GetCell(row, map, "AntivenomId");
                var antivenomName = GetCell(row, map, "AntivenomName") ?? GetCell(row, map, "Name");
                var isActive = ParseBoolean(GetCell(row, map, "IsActive"), "IsActive", true);

                int? antivenomId = null;
                if (!string.IsNullOrWhiteSpace(antivenomIdRaw))
                {
                    if (!int.TryParse(antivenomIdRaw, out var parsedId))
                    {
                        throw new BadRequestException($"Column 'AntivenomId' has invalid integer value '{antivenomIdRaw}'.");
                    }

                    antivenomId = parsedId;
                }

                if (!antivenomId.HasValue && string.IsNullOrWhiteSpace(antivenomName))
                {
                    continue;
                }

                result.Add(new AntivenomSheetRow
                {
                    AntivenomId = antivenomId,
                    AntivenomName = antivenomName,
                    IsActive = isActive
                });
            }

            return result;
        }

        private static List<VenomSheetRow> ParseVenomSheet(IXLWorksheet worksheet)
        {
            var map = BuildHeaderMap(worksheet);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            var result = new List<VenomSheetRow>();

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                var venomTypeIdRaw = GetCell(row, map, "VenomTypeId");
                var venomName = GetCell(row, map, "VenomName") ?? GetCell(row, map, "Name");

                int? venomTypeId = null;
                if (!string.IsNullOrWhiteSpace(venomTypeIdRaw))
                {
                    if (!int.TryParse(venomTypeIdRaw, out var parsedId))
                    {
                        throw new BadRequestException($"Column 'VenomTypeId' has invalid integer value '{venomTypeIdRaw}'.");
                    }

                    venomTypeId = parsedId;
                }

                if (!venomTypeId.HasValue && string.IsNullOrWhiteSpace(venomName))
                {
                    continue;
                }

                result.Add(new VenomSheetRow
                {
                    VenomTypeId = venomTypeId,
                    VenomName = venomName
                });
            }

            return result;
        }

        private static List<AlternativeNameSheetRow> ParseAlternativeNameSheet(IXLWorksheet worksheet)
        {
            var map = BuildHeaderMap(worksheet);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            var result = new List<AlternativeNameSheetRow>();

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                var name = GetCell(row, map, "Name") ?? GetCell(row, map, "AlternativeName");
                var slug = GetCell(row, map, "Slug");

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                result.Add(new AlternativeNameSheetRow
                {
                    Name = name,
                    Slug = slug
                });
            }

            return result;
        }

        private static FirstAidOverride? ParseFirstAidSheet(IXLWorksheet worksheet)
        {
            var map = BuildHeaderMap(worksheet);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            var firstAid = new FirstAidOverride
            {
                Content = new FirstAidContent()
            };

            var hasData = false;

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                var modeRaw = GetCell(row, map, "Mode");
                if (!string.IsNullOrWhiteSpace(modeRaw))
                {
                    firstAid.Mode = ParseEnum<OverrideMode>(modeRaw, "Mode");
                    hasData = true;
                }

                var section = GetCell(row, map, "Section");
                if (string.IsNullOrWhiteSpace(section))
                {
                    continue;
                }

                var text = GetCell(row, map, "Text");
                var mediaUrl = GetCell(row, map, "MediaUrl");
                var note = GetCell(row, map, "Note");

                if (section.Equals("Notes", StringComparison.OrdinalIgnoreCase))
                {
                    var noteValue = !string.IsNullOrWhiteSpace(note) ? note : text;
                    if (!string.IsNullOrWhiteSpace(noteValue))
                    {
                        firstAid.Content.Notes.Add(noteValue);
                        hasData = true;
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var step = new FirstAidStep
                {
                    Text = text,
                    MediaUrl = mediaUrl ?? string.Empty
                };

                if (section.Equals("Steps", StringComparison.OrdinalIgnoreCase))
                {
                    firstAid.Content.Steps.Add(step);
                    hasData = true;
                }
                else if (section.Equals("Dos", StringComparison.OrdinalIgnoreCase))
                {
                    firstAid.Content.Dos.Add(step);
                    hasData = true;
                }
                else if (section.Equals("Donts", StringComparison.OrdinalIgnoreCase))
                {
                    firstAid.Content.Donts.Add(step);
                    hasData = true;
                }
                else
                {
                    throw new BadRequestException($"Unsupported FirstAid section '{section}'. Allowed values: Steps, Dos, Donts, Notes.");
                }
            }

            return hasData ? firstAid : null;
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet)
        {
            var headerRow = worksheet.Row(1);
            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var column = 1; column <= lastColumn; column++)
            {
                var header = headerRow.Cell(column).GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(header) && !map.ContainsKey(header))
                {
                    map[header] = column;
                }
            }

            return map;
        }

        private static string GetRequiredCell(IXLRow row, Dictionary<string, int> map, string columnName)
        {
            var value = GetCell(row, map, columnName);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BadRequestException($"Column '{columnName}' is required.");
            }

            return value;
        }

        private static string? GetCell(IXLRow row, Dictionary<string, int> map, string columnName)
        {
            if (!map.TryGetValue(columnName, out var columnIndex))
            {
                return null;
            }

            return row.Cell(columnIndex).GetString()?.Trim();
        }

        private static bool ParseBoolean(string? rawValue, string fieldName, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue;
            }

            if (bool.TryParse(rawValue, out var parsed))
            {
                return parsed;
            }

            if (rawValue == "1")
            {
                return true;
            }

            if (rawValue == "0")
            {
                return false;
            }

            throw new BadRequestException($"Column '{fieldName}' has invalid boolean value '{rawValue}'.");
        }

        private static float ParseFloat(string rawValue, string fieldName)
        {
            if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantParsed))
            {
                return invariantParsed;
            }

            if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.CurrentCulture, out var localParsed))
            {
                return localParsed;
            }

            throw new BadRequestException($"Column '{fieldName}' has invalid numeric value '{rawValue}'.");
        }

        private static TEnum ParseEnum<TEnum>(string rawValue, string fieldName) where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(rawValue, true, out var parsed))
            {
                return parsed;
            }

            if (int.TryParse(rawValue, out var numericValue) && Enum.IsDefined(typeof(TEnum), numericValue))
            {
                return (TEnum)Enum.ToObject(typeof(TEnum), numericValue);
            }

            throw new BadRequestException($"Column '{fieldName}' has invalid enum value '{rawValue}'.");
        }

        private static TEnum? ParseNullableEnum<TEnum>(string? rawValue, string fieldName) where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            return ParseEnum<TEnum>(rawValue, fieldName);
        }

        private static List<string> SplitCellValues(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return new List<string>();
            }

            return rawValue
                .Split(new[] { '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private async Task ApplyPostCreateMappingsAsync(int snakeSpeciesId, ParsedSnakeSpeciesExcel parsed, CancellationToken ct)
        {
            await AddAntivenomMappingsAsync(snakeSpeciesId, parsed.Antivenoms, ct);
            await AddVenomMappingsAsync(snakeSpeciesId, parsed.Venoms, ct);
            await AddAlternativeNamesAsync(snakeSpeciesId, parsed.AlternativeNames, ct);
            await _unitOfWork.CommitAsync();
        }

        private async Task AddAntivenomMappingsAsync(int snakeSpeciesId, List<AntivenomSheetRow> rows, CancellationToken ct)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var antivenomRepository = _unitOfWork.GetRepository<Antivenom>();
            var mappingRepository = _unitOfWork.GetRepository<SpeciesAntivenom>();
            var antivenomPairs = new Dictionary<int, bool>();

            foreach (var row in rows)
            {
                var antivenomId = row.AntivenomId ?? await ResolveAntivenomIdByNameAsync(row.AntivenomName, ct);
                if (!antivenomPairs.ContainsKey(antivenomId))
                {
                    antivenomPairs[antivenomId] = row.IsActive;
                }
            }

            foreach (var pair in antivenomPairs)
            {
                var antivenomId = pair.Key;
                var isActive = pair.Value;

                var antivenomExists = await antivenomRepository.ExistsAsync(a => a.Id == antivenomId, ct);
                if (!antivenomExists)
                {
                    throw new NotFoundException($"Antivenom with ID {antivenomId} not found.");
                }

                var mappingExists = await mappingRepository
                    .ExistsAsync(x => x.SnakeSpeciesId == snakeSpeciesId && x.AntivenomId == antivenomId, ct);

                if (mappingExists)
                {
                    continue;
                }

                await mappingRepository.InsertAsync(new SpeciesAntivenom
                {
                    SnakeSpeciesId = snakeSpeciesId,
                    AntivenomId = antivenomId,
                    IsActive = isActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, ct);
            }
        }

        private async Task<int> ResolveAntivenomIdByNameAsync(string? antivenomName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(antivenomName))
            {
                throw new BadRequestException("Each row in AntiVenom sheet must provide AntivenomId or AntivenomName.");
            }

            var antivenom = await _unitOfWork.GetRepository<Antivenom>()
                .FirstOrDefaultAsync(predicate: a => a.Name.ToLower() == antivenomName.ToLower(), cancellationToken: ct);

            if (antivenom == null)
            {
                throw new NotFoundException($"Antivenom with name '{antivenomName}' not found.");
            }

            return antivenom.Id;
        }

        private async Task AddVenomMappingsAsync(int snakeSpeciesId, List<VenomSheetRow> rows, CancellationToken ct)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var venomRepository = _unitOfWork.GetRepository<VenomType>();
            var speciesVenomRepository = _unitOfWork.GetRepository<SpeciesVenom>();
            var venomTypeIds = new HashSet<int>();

            foreach (var row in rows)
            {
                var venomTypeId = row.VenomTypeId ?? await ResolveVenomTypeIdByNameAsync(row.VenomName, ct);
                venomTypeIds.Add(venomTypeId);
            }

            foreach (var venomTypeId in venomTypeIds)
            {
                var venomExists = await venomRepository.ExistsAsync(v => v.Id == venomTypeId, ct);
                if (!venomExists)
                {
                    throw new NotFoundException($"VenomType with ID {venomTypeId} not found.");
                }

                var exists = await speciesVenomRepository
                    .ExistsAsync(x => x.SnakeSpeciesId == snakeSpeciesId && x.VenomTypeId == venomTypeId, ct);

                if (exists)
                {
                    continue;
                }

                await speciesVenomRepository.InsertAsync(new SpeciesVenom
                {
                    SnakeSpeciesId = snakeSpeciesId,
                    VenomTypeId = venomTypeId
                }, ct);
            }
        }

        private async Task<int> ResolveVenomTypeIdByNameAsync(string? venomName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(venomName))
            {
                throw new BadRequestException("Each row in Venom sheet must provide VenomTypeId or VenomName.");
            }

            var venom = await _unitOfWork.GetRepository<VenomType>()
                .FirstOrDefaultAsync(predicate: v => v.Name.ToLower() == venomName.ToLower(), cancellationToken: ct);

            if (venom == null)
            {
                throw new NotFoundException($"VenomType with name '{venomName}' not found.");
            }

            return venom.Id;
        }

        private async Task AddAlternativeNamesAsync(int snakeSpeciesId, List<AlternativeNameSheetRow> rows, CancellationToken ct)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var repository = _unitOfWork.GetRepository<SnakeSpeciesName>();

            var existingNames = await repository.GetListAsync(
                predicate: x => x.SnakeSpeciesId == snakeSpeciesId,
                cancellationToken: ct);

            var existingNameSet = existingNames
                .Select(x => x.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var allExistingSlugs = await repository.GetListAsync(
                predicate: x => true,
                cancellationToken: ct);

            var reservedSlugs = allExistingSlugs
                .Select(x => x.Slug)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (existingNameSet.Contains(row.Name))
                {
                    continue;
                }

                var uniqueSlug = GenerateUniqueSlug(row.Slug, row.Name, reservedSlugs);
                reservedSlugs.Add(uniqueSlug);
                existingNameSet.Add(row.Name);

                await repository.InsertAsync(new SnakeSpeciesName
                {
                    Name = row.Name,
                    Slug = uniqueSlug,
                    SnakeSpeciesId = snakeSpeciesId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, ct);
            }
        }

        private static string GenerateUniqueSlug(string? slug, string name, HashSet<string> reservedSlugs)
        {
            var baseSlug = NormalizeSlug(!string.IsNullOrWhiteSpace(slug) ? slug : name);
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                throw new BadRequestException($"Cannot generate slug for alternative name '{name}'.");
            }

            if (!reservedSlugs.Contains(baseSlug))
            {
                return baseSlug;
            }

            var counter = 2;
            while (reservedSlugs.Contains($"{baseSlug}-{counter}"))
            {
                counter++;
            }

            return $"{baseSlug}-{counter}";
        }

        private static string NormalizeSlug(string input)
        {
            var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var character in normalized)
            {
                if (char.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                sb.Append(character == 'đ' ? 'd' : character);
            }

            var withoutDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);
            var slug = Regex.Replace(withoutDiacritics, "[^a-z0-9]+", "-");
            slug = Regex.Replace(slug, "-+", "-").Trim('-');
            return slug;
        }

        private sealed class ParsedSnakeSpeciesExcel
        {
            public BasicInfoRow BasicInfo { get; set; } = new();
            public IdentificationFeature? Identification { get; set; }
            public List<SymptomTimeline>? SymptomsByTime { get; set; }
            public FirstAidOverride? FirstAidGuidelineOverride { get; set; }
            public List<AntivenomSheetRow> Antivenoms { get; set; } = new();
            public List<VenomSheetRow> Venoms { get; set; } = new();
            public List<AlternativeNameSheetRow> AlternativeNames { get; set; } = new();
        }

        private sealed class BasicInfoRow
        {
            public string ScientificName { get; set; } = string.Empty;
            public string Slug { get; set; } = string.Empty;
            public string? CommonName { get; set; }
            public string? Description { get; set; }
            public string? IdentificationSummary { get; set; }
            public PrimaryVenomType? PrimaryVenomType { get; set; }
            public float RiskLevel { get; set; }
            public bool IsVenomous { get; set; }
            public bool IsActive { get; set; } = true;
        }

        private sealed class AntivenomSheetRow
        {
            public int? AntivenomId { get; set; }
            public string? AntivenomName { get; set; }
            public bool IsActive { get; set; } = true;
        }

        private sealed class VenomSheetRow
        {
            public int? VenomTypeId { get; set; }
            public string? VenomName { get; set; }
        }

        private sealed class AlternativeNameSheetRow
        {
            public string Name { get; set; } = string.Empty;
            public string? Slug { get; set; }
        }
    }
}
