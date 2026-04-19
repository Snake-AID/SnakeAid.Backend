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
using SnakeAid.Core.Responses.FirstAidGuideline;
using SnakeAid.Core.Responses.SnakeSpecies;
using SnakeAid.Core.Utils;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Extensions;
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
                                    .ThenInclude(vt => vt.FirstAidGuideline)
                    );

                if (snakeSpecies == null)
                {
                    _logger.LogWarning("Snake species with ID {Id} not found", id);
                    throw new NotFoundException($"Snake species with ID {id} not found.");
                }

                _logger.LogInformation("Successfully retrieved snake species with ID: {Id}", id);

                return MapToDetailResponse(snakeSpecies);
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

                var escaped = query
                    .Replace(@"\", @"\\")
                    .Replace("%", @"\%")
                    .Replace("_", @"\_");
                var pattern = $"%{escaped}%";
                const string escapeChar = @"\";
                var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .GetListAsync(
                        predicate: s => s.IsActive &&
                            (EF.Functions.ILike(s.ScientificName, pattern, escapeChar) ||
                             EF.Functions.ILike(s.CommonName, pattern, escapeChar) ||
                             s.AlternativeNames.Any(sn => EF.Functions.ILike(sn.Name, pattern, escapeChar))),
                        include: q => q
                            .Include(s => s.SpeciesVenoms)
                                .ThenInclude(sv => sv.VenomType)
                            .Include(s => s.SpeciesAntivenoms)
                                .ThenInclude(sa => sa.Antivenom)
                            .Include(s => s.LibraryMedias)
                            .Include(s => s.FilterSnakeMappings)
                                .ThenInclude(fm => fm.FilterOption)
                    );

                var result = snakeSpecies.Select(s => new SearchSnakeSpeciesResponse
                {
                    Id = s.Id,
                    ScientificName = s.ScientificName,
                    CommonName = s.CommonName,
                    ImageUrl = s.ImageUrl,
                    GalleryUrls = s.LibraryMedias
                        .Where(m => m.IsActive && m.MediaType == MediaType.Image)
                        .Select(m => m.MediaUrl).ToList(),
                    IsVenomous = s.IsVenomous,
                    PrimaryVenomType = s.PrimaryVenomType,
                    RiskLevel = s.RiskLevel,
                    Identification = s.Identification != null ? new Core.Responses.SnakeSpecies.IdentificationInfo
                    {
                        PhysicalTraits = s.Identification.PhysicalTraits ?? new(),
                        Behaviors = s.Identification.Behaviors ?? new(),
                        Habitat = s.Identification.Habitat
                    } : null,
                    Venoms = s.SpeciesVenoms.Select(sv => new Core.Responses.SnakeSpecies.VenomInfo
                    {
                        Id = sv.VenomTypeId,
                        VenomType = sv.VenomType?.Name ?? "Unknown",
                        Description = sv.VenomType?.Description ?? ""
                    }).ToList(),
                    Antivenoms = s.SpeciesAntivenoms.Select(sa => new Core.Responses.SnakeSpecies.AntivenomInfo
                    {
                        Id = sa.AntivenomId,
                        AntivenomName = sa.Antivenom?.Name ?? "Unknown",
                        Manufacturer = sa.Antivenom?.Manufacturer ?? "",
                        Effectiveness = sa.Antivenom?.Description ?? ""
                    }).ToList(),
                    FirstAid = s.FirstAidGuidelineOverride?.Content != null ? new Core.Responses.SnakeSpecies.FirstAidInfo
                    {
                        Mode = s.FirstAidGuidelineOverride.Mode.ToString(),
                        DoItems = s.FirstAidGuidelineOverride.Content.Dos?.Select(d => d.Text).ToList() ?? new(),
                        DontItems = s.FirstAidGuidelineOverride.Content.Donts?.Select(d => d.Text).ToList() ?? new()
                    } : null,
                    Tags = s.FilterSnakeMappings
                        .Where(fm => fm.IsActive && fm.FilterOption != null)
                        .Select(fm => fm.FilterOption.OptionText).ToList()
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

        public async Task<DetailSnakeSpeciesResponse> CreateSnakeSpeciesAsync(CreateSnakeSpeciesRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            var scientificName = request.ScientificName.Trim();
            var commonName = request.CommonName?.Trim() ?? string.Empty;
            var slug = await GenerateUniqueSnakeSlugAsync(commonName, null, ct);

            await ValidateSnakeSpeciesUniquenessAsync(scientificName, slug, null, ct);

            var libraryMedia = await _unitOfWork.GetRepository<LibraryMedia>()
                .FirstOrDefaultAsync(predicate: x => x.Id == request.MediaId, cancellationToken: ct);

            if (libraryMedia == null)
            {
                throw new NotFoundException($"Library media with ID {request.MediaId} not found.");
            }

            var entity = new SnakeSpecies
            {
                ScientificName = scientificName,
                Slug = slug,
                CommonName = commonName,
                ImageUrl = libraryMedia.MediaUrl,
                Description = request.Description?.Trim() ?? string.Empty,
                IdentificationSummary = request.IdentificationSummary?.Trim() ?? string.Empty,
                PrimaryVenomType = request.PrimaryVenomType,
                Identification = request.Identification,
                SymptomsByTime = request.SymptomsByTime,
                FirstAidGuidelineOverride = request.FirstAidGuidelineOverride,
                RiskLevel = request.RiskLevel,
                IsVenomous = request.IsVenomous,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await EnsureSnakeSpeciesIdSequenceAsync(ct);
            await _unitOfWork.GetRepository<SnakeSpecies>().InsertAsync(entity, ct);
            await _unitOfWork.CommitAsync();

            await LinkLibraryMediaToSpeciesAsync(request.MediaId, entity.Id, ct);
            await _unitOfWork.CommitAsync();

            await SyncRelationsAfterCreateOrUpdateAsync(entity.Id, request.VenomIds, request.AntivenomIds, request.AlternativeNames, ct);

            return await BuildDetailResponseAsync(entity.Id, ct);
        }

        public async Task<DetailSnakeSpeciesResponse> UpdateSnakeSpeciesAsync(int id, UpdateSnakeSpeciesRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            var repository = _unitOfWork.GetRepository<SnakeSpecies>();
            var entity = await repository.FirstOrDefaultAsync(predicate: x => x.Id == id, asNoTracking: false, cancellationToken: ct);

            if (entity == null)
            {
                throw new NotFoundException($"Snake species with ID {id} not found.");
            }

            var updatedScientificName = entity.ScientificName;
            var updatedSlug = entity.Slug;

            if (!string.IsNullOrWhiteSpace(request.ScientificName))
            {
                updatedScientificName = request.ScientificName.Trim();
                entity.ScientificName = updatedScientificName;
            }

            // Regenerate slug if CommonName is being updated
            if (request.CommonName != null)
            {
                var newCommonName = request.CommonName.Trim();
                entity.CommonName = newCommonName;
                updatedSlug = await GenerateUniqueSnakeSlugAsync(newCommonName, id, ct);
                entity.Slug = updatedSlug;
            }

            await ValidateSnakeSpeciesUniquenessAsync(updatedScientificName, updatedSlug, id, ct);

            Guid? mediaIdToLink = null;
            if (request.MediaId.HasValue)
            {
                var libraryMedia = await _unitOfWork.GetRepository<LibraryMedia>()
                    .FirstOrDefaultAsync(predicate: x => x.Id == request.MediaId.Value, cancellationToken: ct);

                if (libraryMedia == null)
                {
                    throw new NotFoundException($"Library media with ID {request.MediaId.Value} not found.");
                }

                entity.ImageUrl = libraryMedia.MediaUrl;
                mediaIdToLink = request.MediaId.Value;
            }

            if (request.Description != null)
            {
                entity.Description = request.Description.Trim();
            }

            if (request.IdentificationSummary != null)
            {
                entity.IdentificationSummary = request.IdentificationSummary.Trim();
            }

            if (request.PrimaryVenomType.HasValue)
            {
                entity.PrimaryVenomType = request.PrimaryVenomType;
            }

            if (request.Identification != null)
            {
                entity.Identification = request.Identification;
            }

            if (request.SymptomsByTime != null)
            {
                entity.SymptomsByTime = request.SymptomsByTime;
            }

            if (request.FirstAidGuidelineOverride != null)
            {
                entity.FirstAidGuidelineOverride = request.FirstAidGuidelineOverride;
            }

            if (request.RiskLevel.HasValue)
            {
                entity.RiskLevel = request.RiskLevel.Value;
            }

            if (request.IsVenomous.HasValue)
            {
                entity.IsVenomous = request.IsVenomous.Value;
            }

            if (request.IsActive.HasValue)
            {
                entity.IsActive = request.IsActive.Value;
            }

            entity.UpdatedAt = DateTime.UtcNow;

            repository.Update(entity);
            await _unitOfWork.CommitAsync();

            if (mediaIdToLink.HasValue)
            {
                await LinkLibraryMediaToSpeciesAsync(mediaIdToLink.Value, id, ct);
                await _unitOfWork.CommitAsync();
            }

            await SyncRelationsAfterCreateOrUpdateAsync(id, request.VenomIds, request.AntivenomIds, request.AlternativeNames, ct);

            return await BuildDetailResponseAsync(id, ct);
        }

        public async Task DeleteSnakeSpeciesAsync(int id, CancellationToken ct = default)
        {
            var repository = _unitOfWork.GetRepository<SnakeSpecies>();
            var entity = await repository.FirstOrDefaultAsync(predicate: x => x.Id == id, asNoTracking: false, cancellationToken: ct);

            if (entity == null)
            {
                throw new NotFoundException($"Snake species with ID {id} not found.");
            }

            repository.Delete(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task<DetailSnakeSpeciesResponse> CreateSnakeSpeciesWithFileAsync(CreateSnakeSpeciesWithFileRequest request, ClaimsPrincipal user, CancellationToken ct = default)
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

            await ValidateSnakeSpeciesUniquenessAsync(parsed.BasicInfo.ScientificName, null, null, ct);

            var snakeSlug = await GenerateUniqueSnakeSlugAsync(
                parsed.BasicInfo.CommonName ?? parsed.BasicInfo.ScientificName, null, ct);

            var createdLibraryMedia = await _libraryMediaService.CreateAsync(new CreateLibraryMediaRequest
            {
                File = request.ImageFile,
                MediaType = MediaType.Image,
                SnakeSpeciesId = null
            }, user, ct);

            var entity = new SnakeSpecies
            {
                ScientificName = parsed.BasicInfo.ScientificName,
                Slug = snakeSlug,
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

            await EnsureSnakeSpeciesIdSequenceAsync(ct);
            await _unitOfWork.GetRepository<SnakeSpecies>().InsertAsync(entity, ct);
            await _unitOfWork.CommitAsync();

            await ApplyPostCreateMappingsAsync(entity.Id, createdLibraryMedia.Id, parsed, ct);

            return await BuildDetailResponseAsync(entity.Id, ct);
        }

        private async Task<DetailSnakeSpeciesResponse> BuildDetailResponseAsync(int id, CancellationToken ct)
        {
            var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                .FirstOrDefaultAsync(
                    predicate: s => s.Id == id,
                    include: query => query
                        .Include(s => s.AlternativeNames)
                        .Include(s => s.SpeciesAntivenoms)
                            .ThenInclude(sa => sa.Antivenom)
                        .Include(s => s.SpeciesVenoms)
                            .ThenInclude(sv => sv.VenomType)
                                .ThenInclude(vt => vt.FirstAidGuideline),
                    cancellationToken: ct);

            if (snakeSpecies == null)
            {
                throw new NotFoundException($"Snake species with ID {id} not found.");
            }

            return MapToDetailResponse(snakeSpecies);
        }

        private static DetailSnakeSpeciesResponse MapToDetailResponse(SnakeSpecies snakeSpecies)
        {
            var response = snakeSpecies.Adapt<DetailSnakeSpeciesResponse>();
            response.AlternativeNames = snakeSpecies.AlternativeNames
                .Select(x => x.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            response.Venoms = snakeSpecies.SpeciesVenoms
                .Select(sv => new VenomInfo
                {
                    Id = sv.VenomTypeId,
                    VenomType = sv.VenomType?.Name ?? "Unknown",
                    Description = sv.VenomType?.Description ?? string.Empty
                })
                .ToList();
            response.Antivenoms = snakeSpecies.SpeciesAntivenoms
                .Select(sa => new AntivenomInfo
                {
                    Id = sa.AntivenomId,
                    AntivenomName = sa.Antivenom?.Name ?? "Unknown",
                    Manufacturer = sa.Antivenom?.Manufacturer ?? string.Empty,
                    Effectiveness = sa.Antivenom?.Description ?? string.Empty
                })
                .ToList();

            response.BaseFirstAidGuideline = GetBaseFirstAidGuidelineResponse(snakeSpecies);
            response.EffectiveFirstAidGuideline = snakeSpecies.GetMergedFirstAidContent();

            return response;
        }

        private static FirstAidGuidelineResponse? GetBaseFirstAidGuidelineResponse(SnakeSpecies snakeSpecies)
        {
            if (snakeSpecies.SpeciesVenoms == null || !snakeSpecies.SpeciesVenoms.Any())
            {
                return null;
            }

            if (snakeSpecies.PrimaryVenomType.HasValue)
            {
                var primaryVenomTypeId = (int)snakeSpecies.PrimaryVenomType.Value + 1;
                var primaryVenom = snakeSpecies.SpeciesVenoms
                    .FirstOrDefault(sv => sv.VenomTypeId == primaryVenomTypeId);

                if (primaryVenom?.VenomType?.FirstAidGuideline != null)
                {
                    return primaryVenom.VenomType.FirstAidGuideline.Adapt<FirstAidGuidelineResponse>();
                }
            }

            var anyVenomWithGuideline = snakeSpecies.SpeciesVenoms
                .FirstOrDefault(sv => sv.VenomType?.FirstAidGuideline != null);

            return anyVenomWithGuideline?.VenomType?.FirstAidGuideline?.Adapt<FirstAidGuidelineResponse>();
        }

        private async Task ValidateSnakeSpeciesUniquenessAsync(string scientificName, string? slug, int? excludeId, CancellationToken ct)
        {
            var scientificNameExists = await _unitOfWork.GetRepository<SnakeSpecies>()
                .ExistsAsync(
                    s => s.ScientificName == scientificName && (!excludeId.HasValue || s.Id != excludeId.Value),
                    ct);

            if (scientificNameExists)
                throw new BadRequestException($"Snake species with scientific name '{scientificName}' already exists.");
        }

        private async Task SyncRelationsAfterCreateOrUpdateAsync(
            int snakeSpeciesId,
            List<int>? venomIds,
            List<int>? antivenomIds,
            List<string>? alternativeNames,
            CancellationToken ct)
        {
            var hasChanges = false;

            if (venomIds != null)
            {
                await SyncVenomMappingsAsync(snakeSpeciesId, venomIds, ct);
                hasChanges = true;
            }

            if (antivenomIds != null)
            {
                await SyncAntivenomMappingsAsync(snakeSpeciesId, antivenomIds, ct);
                hasChanges = true;
            }

            if (alternativeNames != null)
            {
                await SyncAlternativeNamesAsync(snakeSpeciesId, alternativeNames, ct);
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _unitOfWork.CommitAsync();
            }
        }

        private async Task SyncVenomMappingsAsync(int snakeSpeciesId, List<int> venomIds, CancellationToken ct)
        {
            var normalizedIds = venomIds
                .Where(id => id > 0)
                .Distinct()
                .ToHashSet();

            var venomRepository = _unitOfWork.GetRepository<VenomType>();
            var validVenomIds = await venomRepository.GetListAsync(
                predicate: v => normalizedIds.Contains(v.Id),
                cancellationToken: ct);

            if (validVenomIds.Count != normalizedIds.Count)
            {
                throw new NotFoundException("One or more venom IDs were not found.");
            }

            var mappingRepository = _unitOfWork.GetRepository<SpeciesVenom>();
            var existingMappings = await mappingRepository.GetListAsync(
                predicate: x => x.SnakeSpeciesId == snakeSpeciesId,
                cancellationToken: ct);

            foreach (var mapping in existingMappings.Where(x => !normalizedIds.Contains(x.VenomTypeId)))
            {
                mappingRepository.Delete(mapping);
            }

            var existingIds = existingMappings.Select(x => x.VenomTypeId).ToHashSet();
            foreach (var venomId in normalizedIds.Where(id => !existingIds.Contains(id)))
            {
                await mappingRepository.InsertAsync(new SpeciesVenom
                {
                    SnakeSpeciesId = snakeSpeciesId,
                    VenomTypeId = venomId
                }, ct);
            }
        }

        private async Task SyncAntivenomMappingsAsync(int snakeSpeciesId, List<int> antivenomIds, CancellationToken ct)
        {
            var normalizedIds = antivenomIds
                .Where(id => id > 0)
                .Distinct()
                .ToHashSet();

            var antivenomRepository = _unitOfWork.GetRepository<Antivenom>();
            var validAntivenomIds = await antivenomRepository.GetListAsync(
                predicate: a => normalizedIds.Contains(a.Id),
                cancellationToken: ct);

            if (validAntivenomIds.Count != normalizedIds.Count)
            {
                throw new NotFoundException("One or more antivenom IDs were not found.");
            }

            var mappingRepository = _unitOfWork.GetRepository<SpeciesAntivenom>();
            var existingMappings = await mappingRepository.GetListAsync(
                predicate: x => x.SnakeSpeciesId == snakeSpeciesId,
                cancellationToken: ct);

            foreach (var mapping in existingMappings.Where(x => !normalizedIds.Contains(x.AntivenomId)))
            {
                mappingRepository.Delete(mapping);
            }

            var existingIds = existingMappings.Select(x => x.AntivenomId).ToHashSet();
            foreach (var antivenomId in normalizedIds.Where(id => !existingIds.Contains(id)))
            {
                await mappingRepository.InsertAsync(new SpeciesAntivenom
                {
                    SnakeSpeciesId = snakeSpeciesId,
                    AntivenomId = antivenomId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, ct);
            }
        }

        private async Task SyncAlternativeNamesAsync(int snakeSpeciesId, List<string> alternativeNames, CancellationToken ct)
        {
            var normalizedNames = alternativeNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var repository = _unitOfWork.GetRepository<SnakeSpeciesName>();
            var existingNames = await repository.GetListAsync(
                predicate: x => x.SnakeSpeciesId == snakeSpeciesId,
                cancellationToken: ct);

            foreach (var item in existingNames.Where(x => !normalizedNames.Contains(x.Name)))
            {
                repository.Delete(item);
            }

            var existingNameSet = existingNames
                .Where(x => normalizedNames.Contains(x.Name))
                .Select(x => x.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var allExistingSlugs = await repository.GetListAsync(
                predicate: x => true,
                cancellationToken: ct);

            var reservedSlugs = allExistingSlugs
                .Select(x => x.Slug)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var name in normalizedNames.Where(name => !existingNameSet.Contains(name)))
            {
                var slug = GenerateUniqueAltNameSlug(name, reservedSlugs);
                reservedSlugs.Add(slug);

                await repository.InsertAsync(new SnakeSpeciesName
                {
                    Name = name,
                    Slug = slug,
                    SnakeSpeciesId = snakeSpeciesId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, ct);
            }
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

            var basicInfo = new BasicInfoRow
            {
                ScientificName = scientificName,
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

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                result.Add(new AlternativeNameSheetRow { Name = name });
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

        private async Task ApplyPostCreateMappingsAsync(int snakeSpeciesId, Guid libraryMediaId, ParsedSnakeSpeciesExcel parsed, CancellationToken ct)
        {
            await LinkLibraryMediaToSpeciesAsync(libraryMediaId, snakeSpeciesId, ct);
            await AddAntivenomMappingsAsync(snakeSpeciesId, parsed.Antivenoms, ct);
            await AddVenomMappingsAsync(snakeSpeciesId, parsed.Venoms, ct);
            await AddAlternativeNamesAsync(snakeSpeciesId, parsed.AlternativeNames, ct);
            await _unitOfWork.CommitAsync();
        }

        private async Task LinkLibraryMediaToSpeciesAsync(Guid libraryMediaId, int snakeSpeciesId, CancellationToken ct)
        {
            var libraryMediaRepository = _unitOfWork.GetRepository<LibraryMedia>();
            var libraryMedia = await libraryMediaRepository.FirstOrDefaultAsync(
                predicate: x => x.Id == libraryMediaId,
                asNoTracking: false,
                cancellationToken: ct);

            if (libraryMedia == null)
            {
                throw new NotFoundException($"LibraryMedia with ID {libraryMediaId} not found.");
            }

            libraryMedia.SnakeSpeciesId = snakeSpeciesId;
            libraryMedia.UpdatedAt = DateTime.UtcNow;
            libraryMediaRepository.Update(libraryMedia);
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

                var uniqueSlug = GenerateUniqueAltNameSlug(row.Name, reservedSlugs);
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

        private async Task<string> GenerateUniqueSnakeSlugAsync(string name, int? excludeId, CancellationToken ct)
        {
            var baseSlug = SlugGenerator.DefaultSlug(name);
            if (string.IsNullOrWhiteSpace(baseSlug))
                baseSlug = SlugGenerator.DefaultSlug("snake", Guid.NewGuid().ToString("N")[..6]);

            var reservedSlugs = await _unitOfWork.GetRepository<SnakeSpecies>()
                .GetListAsync(
                    predicate: s => !excludeId.HasValue || s.Id != excludeId.Value,
                    cancellationToken: ct);

            var reserved = reservedSlugs.Select(s => s.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!reserved.Contains(baseSlug))
                return baseSlug;

            var counter = 2;
            while (reserved.Contains($"{baseSlug}-{counter}"))
                counter++;

            return $"{baseSlug}-{counter}";
        }

        private static string GenerateUniqueAltNameSlug(string name, HashSet<string> reservedSlugs)
        {
            var baseSlug = SlugGenerator.DefaultSlug(name);
            if (string.IsNullOrWhiteSpace(baseSlug))
                throw new BadRequestException($"Cannot generate slug for alternative name '{name}'.");

            if (!reservedSlugs.Contains(baseSlug))
                return baseSlug;

            var counter = 2;
            while (reservedSlugs.Contains($"{baseSlug}-{counter}"))
                counter++;

            return $"{baseSlug}-{counter}";
        }

        private async Task EnsureSnakeSpeciesIdSequenceAsync(CancellationToken ct)
        {
            const string sql = @"
SELECT setval(
    pg_get_serial_sequence('""SnakeAid"".""SnakeSpecies""', 'Id'),
    GREATEST((SELECT COALESCE(MAX(""Id""), 1) FROM ""SnakeAid"".""SnakeSpecies""), 1),
    true
);";

            await _unitOfWork.Context.Database.ExecuteSqlRawAsync(sql, ct);
        }

        public async Task<List<FilteredSnakeResponse>> FilterSnakesByAnswersAsync(
            List<int> selectedOptionIds,
            CancellationToken ct = default)
        {
            if (selectedOptionIds == null || !selectedOptionIds.Any())
            {
                throw new ArgumentException("Vui lòng chọn ít nhất 1 đáp án", nameof(selectedOptionIds));
            }

            try
            {
                _logger.LogInformation("Filtering snakes with {Count} selected options: {Options}",
                    selectedOptionIds.Count, string.Join(", ", selectedOptionIds));

                // Get all mappings that match selected options
                var matchedMappings = await _unitOfWork.GetRepository<FilterSnakeMapping>()
                    .GetListAsync(
                        predicate: m => m.IsActive && selectedOptionIds.Contains(m.FilterOptionId),
                        include: query => query
                            .Include(m => m.SnakeSpecies)
                            .Include(m => m.FilterOption)
                                .ThenInclude(o => o.Question),
                        asNoTracking: true,
                        cancellationToken: ct
                    );

                // Filter out inactive snakes
                matchedMappings = matchedMappings
                    .Where(m => m.SnakeSpecies.IsActive)
                    .ToList();

                _logger.LogInformation("Found {Count} filter mappings", matchedMappings.Count);

                // Group by snake species and calculate match scores
                var snakeMatchScores = matchedMappings
                    .GroupBy(m => m.SnakeSpeciesId)
                    .Select(g => new
                    {
                        SnakeId = g.Key,
                        MatchCount = g.Count(),
                        MatchedOptions = g.Select(m => m.FilterOption.OptionText).Distinct().ToList(),
                        Snake = g.First().SnakeSpecies
                    })
                    .OrderByDescending(x => x.MatchCount) // Sort by best match first
                    .ThenByDescending(x => x.Snake.RiskLevel) // Then by risk level (venomous first)
                    .ThenBy(x => x.Snake.CommonName)
                    .ToList();

                _logger.LogInformation("Matched {Count} snake species", snakeMatchScores.Count);

                // Build response
                var results = snakeMatchScores.Select(match => new FilteredSnakeResponse
                {
                    Id = match.Snake.Id,
                    ScientificName = match.Snake.ScientificName,
                    CommonName = match.Snake.CommonName,
                    ImageUrl = match.Snake.ImageUrl,
                    IsVenomous = match.Snake.IsVenomous,
                    RiskLevel = match.Snake.RiskLevel,
                    MatchScore = match.MatchCount,
                    TotalAnswered = selectedOptionIds.Count,
                    MatchPercentage = Math.Round((double)match.MatchCount / selectedOptionIds.Count * 100, 1),
                    MatchedFeatures = match.MatchedOptions
                }).ToList();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering snakes by answers: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SnakesByLocationResponse> GetSnakesByLocationAsync(double lat, double lng, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting snakes by location: lat={Lat}, lng={Lng}", lat, lng);

                // Step 1: Find geographic region using PostGIS ST_Contains
                // Note: Cast geography to geometry for spatial operations
                var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                var point = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(lng, lat));

                var region = await _unitOfWork.GetRepository<GeographicRegion>()
                    .CreateBaseQuery(asNoTracking: true)
                    .Where(r => r.IsActive && r.Boundary.Intersects(point))
                    .FirstOrDefaultAsync(ct);

                if (region == null)
                {
                    _logger.LogWarning("No geographic region found for location: lat={Lat}, lng={Lng}", lat, lng);
                    throw new NotFoundException("Không xác định được khu vực. Vui lòng kiểm tra lại vị trí GPS.");
                }

                _logger.LogInformation("Found region: {RegionName} (ID: {RegionId})", region.Name, region.Id);

                // Step 2: Get snakes in this region with metadata
                var snakesInRegion = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .CreateBaseQuery(asNoTracking: true)
                    .Where(s => s.IsActive &&
                                s.RegionSnakeMappings.Any(m =>
                                    m.GeographicRegionId == region.Id &&
                                    m.IsActive))
                    .Select(s => new
                    {
                        Snake = s,
                        Mapping = s.RegionSnakeMappings.First(m =>
                            m.GeographicRegionId == region.Id &&
                            m.IsActive)
                    })
                    .OrderByDescending(x => x.Mapping.Priority)
                    .ThenByDescending(x => x.Mapping.CommonLevel)
                    .ToListAsync(ct);

                _logger.LogInformation("Found {Count} snakes in region {RegionName}", snakesInRegion.Count, region.Name);

                // Step 3: Map to response
                var response = new SnakesByLocationResponse
                {
                    Region = new GeographicRegionDto
                    {
                        Id = region.Id,
                        Name = region.Name,
                        Code = region.Code,
                        Description = region.Description
                    },
                    Snakes = snakesInRegion.Select(x => new SnakeInRegionDto
                    {
                        // Basic snake info
                        Id = x.Snake.Id,
                        ScientificName = x.Snake.ScientificName,
                        CommonName = x.Snake.CommonName ?? string.Empty,
                        Slug = x.Snake.Slug,
                        ImageUrl = x.Snake.ImageUrl,
                        Description = x.Snake.Description,
                        IdentificationSummary = x.Snake.IdentificationSummary,
                        PrimaryVenomType = x.Snake.PrimaryVenomType,
                        RiskLevel = x.Snake.RiskLevel,
                        IsVenomous = x.Snake.IsVenomous,

                        // Region-specific metadata
                        CommonLevel = x.Mapping.CommonLevel.ToString(),
                        Priority = x.Mapping.Priority,
                        DistributionNotes = x.Mapping.DistributionNotes
                    }).ToList()
                };

                return response;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting snakes by location: lat={Lat}, lng={Lng}, Message={Message}", lat, lng, ex.Message);
                throw;
            }
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
        }

        // ── Geographic Region & Distribution ─────────────────────────────────

        public async Task<List<GeographicRegionResponse>> GetAllRegionsAsync(CancellationToken ct = default)
        {
            var regions = await _unitOfWork.GetRepository<GeographicRegion>()
                .GetListAsync(
                    predicate: r => r.IsActive,
                    orderBy: q => q.OrderBy(r => r.DisplayOrder),
                    cancellationToken: ct);

            return regions.Select(MapRegionToResponse).ToList();
        }

        public async Task<List<RegionSnakeMappingResponse>> GetRegionMappingsBySnakeAsync(int snakeSpeciesId, CancellationToken ct = default)
        {
            var snakeExists = await _unitOfWork.GetRepository<SnakeSpecies>()
                .ExistsAsync(s => s.Id == snakeSpeciesId, ct);
            if (!snakeExists)
                throw new NotFoundException($"Snake species with ID {snakeSpeciesId} not found.");

            var mappings = await _unitOfWork.GetRepository<RegionSnakeMapping>()
                .GetListAsync(
                    predicate: m => m.SnakeSpeciesId == snakeSpeciesId,
                    include: q => q.Include(m => m.GeographicRegion),
                    cancellationToken: ct);

            return mappings.Select(MapMappingToResponse).ToList();
        }

        public async Task<List<RegionSnakeMappingResponse>> SyncRegionMappingsAsync(
            int snakeSpeciesId,
            SyncRegionMappingsRequest request,
            CancellationToken ct = default)
        {
            var snakeExists = await _unitOfWork.GetRepository<SnakeSpecies>()
                .ExistsAsync(s => s.Id == snakeSpeciesId, ct);
            if (!snakeExists)
                throw new NotFoundException($"Snake species with ID {snakeSpeciesId} not found.");

            // Validate all region IDs exist
            var regionIds = request.Mappings.Select(m => m.GeographicRegionId).Distinct().ToList();
            var validRegions = await _unitOfWork.GetRepository<GeographicRegion>()
                .GetListAsync(predicate: r => regionIds.Contains(r.Id), cancellationToken: ct);
            if (validRegions.Count != regionIds.Count)
                throw new NotFoundException("One or more geographic region IDs were not found.");

            var repo = _unitOfWork.GetRepository<RegionSnakeMapping>();
            var existing = await repo.GetListAsync(
                predicate: m => m.SnakeSpeciesId == snakeSpeciesId,
                cancellationToken: ct);

            // Delete mappings not in new list
            var incomingRegionIds = request.Mappings.Select(m => m.GeographicRegionId).ToHashSet();
            foreach (var m in existing.Where(m => !incomingRegionIds.Contains(m.GeographicRegionId)))
                repo.Delete(m);

            // Upsert
            var existingByRegion = existing.ToDictionary(m => m.GeographicRegionId);
            foreach (var item in request.Mappings)
            {
                if (existingByRegion.TryGetValue(item.GeographicRegionId, out var existingMapping))
                {
                    existingMapping.CommonLevel = item.CommonLevel;
                    existingMapping.Priority = item.Priority;
                    existingMapping.DistributionNotes = item.DistributionNotes;
                    existingMapping.IsActive = item.IsActive;
                    existingMapping.UpdatedAt = DateTime.UtcNow;
                    repo.Update(existingMapping);
                }
                else
                {
                    await repo.InsertAsync(new RegionSnakeMapping
                    {
                        SnakeSpeciesId = snakeSpeciesId,
                        GeographicRegionId = item.GeographicRegionId,
                        CommonLevel = item.CommonLevel,
                        Priority = item.Priority,
                        DistributionNotes = item.DistributionNotes,
                        IsActive = item.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }, ct);
                }
            }

            await _unitOfWork.CommitAsync();
            return await GetRegionMappingsBySnakeAsync(snakeSpeciesId, ct);
        }

        public async Task<RegionSnakeMappingResponse> AddRegionMappingAsync(
            int snakeSpeciesId,
            AddRegionMappingRequest request,
            CancellationToken ct = default)
        {
            var snakeExists = await _unitOfWork.GetRepository<SnakeSpecies>()
                .ExistsAsync(s => s.Id == snakeSpeciesId, ct);
            if (!snakeExists)
                throw new NotFoundException($"Snake species with ID {snakeSpeciesId} not found.");

            var regionExists = await _unitOfWork.GetRepository<GeographicRegion>()
                .ExistsAsync(r => r.Id == request.GeographicRegionId, ct);
            if (!regionExists)
                throw new NotFoundException($"Geographic region with ID {request.GeographicRegionId} not found.");

            var duplicate = await _unitOfWork.GetRepository<RegionSnakeMapping>()
                .ExistsAsync(m => m.SnakeSpeciesId == snakeSpeciesId && m.GeographicRegionId == request.GeographicRegionId, ct);
            if (duplicate)
                throw new BadRequestException($"Mapping for region {request.GeographicRegionId} already exists. Use PUT to update.");

            var repo = _unitOfWork.GetRepository<RegionSnakeMapping>();
            var entity = new RegionSnakeMapping
            {
                SnakeSpeciesId = snakeSpeciesId,
                GeographicRegionId = request.GeographicRegionId,
                CommonLevel = request.CommonLevel,
                Priority = request.Priority,
                DistributionNotes = request.DistributionNotes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await repo.InsertAsync(entity, ct);
            await _unitOfWork.CommitAsync();

            var created = await _unitOfWork.GetRepository<RegionSnakeMapping>()
                .FirstOrDefaultAsync(
                    predicate: m => m.Id == entity.Id,
                    include: q => q.Include(m => m.GeographicRegion),
                    cancellationToken: ct);

            return MapMappingToResponse(created!);
        }

        public async Task<RegionSnakeMappingResponse> UpdateRegionMappingAsync(
            int snakeSpeciesId,
            int mappingId,
            UpdateRegionMappingRequest request,
            CancellationToken ct = default)
        {
            var repo = _unitOfWork.GetRepository<RegionSnakeMapping>();
            var entity = await repo.FirstOrDefaultAsync(
                predicate: m => m.Id == mappingId && m.SnakeSpeciesId == snakeSpeciesId,
                asNoTracking: false,
                cancellationToken: ct);

            if (entity == null)
                throw new NotFoundException($"Region mapping with ID {mappingId} not found for snake species {snakeSpeciesId}.");

            if (request.CommonLevel.HasValue) entity.CommonLevel = request.CommonLevel.Value;
            if (request.Priority.HasValue) entity.Priority = request.Priority.Value;
            if (request.DistributionNotes != null) entity.DistributionNotes = request.DistributionNotes;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow;

            repo.Update(entity);
            await _unitOfWork.CommitAsync();

            var updated = await _unitOfWork.GetRepository<RegionSnakeMapping>()
                .FirstOrDefaultAsync(
                    predicate: m => m.Id == mappingId,
                    include: q => q.Include(m => m.GeographicRegion),
                    cancellationToken: ct);

            return MapMappingToResponse(updated!);
        }

        public async Task DeleteRegionMappingAsync(int snakeSpeciesId, int mappingId, CancellationToken ct = default)
        {
            var repo = _unitOfWork.GetRepository<RegionSnakeMapping>();
            var entity = await repo.FirstOrDefaultAsync(
                predicate: m => m.Id == mappingId && m.SnakeSpeciesId == snakeSpeciesId,
                asNoTracking: false,
                cancellationToken: ct);

            if (entity == null)
                throw new NotFoundException($"Region mapping with ID {mappingId} not found for snake species {snakeSpeciesId}.");

            repo.Delete(entity);
            await _unitOfWork.CommitAsync();
        }

        private static GeographicRegionResponse MapRegionToResponse(GeographicRegion region)
        {
            var coordinates = new List<double[]>();
            if (region.Boundary?.ExteriorRing != null)
            {
                coordinates = region.Boundary.ExteriorRing.Coordinates
                    .Select(c => new double[] { c.X, c.Y }) // [lng, lat] GeoJSON order
                    .ToList();
            }

            return new GeographicRegionResponse
            {
                Id = region.Id,
                Name = region.Name,
                Code = region.Code,
                Description = region.Description,
                DisplayOrder = region.DisplayOrder,
                IsActive = region.IsActive,
                BoundaryCoordinates = coordinates
            };
        }

        private static RegionSnakeMappingResponse MapMappingToResponse(RegionSnakeMapping m) => new()
        {
            Id = m.Id,
            GeographicRegionId = m.GeographicRegionId,
            RegionName = m.GeographicRegion?.Name ?? string.Empty,
            RegionCode = m.GeographicRegion?.Code ?? string.Empty,
            CommonLevel = m.CommonLevel.ToString(),
            CommonLevelValue = (int)m.CommonLevel,
            Priority = m.Priority,
            DistributionNotes = m.DistributionNotes,
            IsActive = m.IsActive
        };
    }
}
