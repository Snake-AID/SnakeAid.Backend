using Mapster;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using VenomTypeRequests = SnakeAid.Core.Requests.VenomType;
using VenomTypeResponses = SnakeAid.Core.Responses.VenomType;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class VenomTypeService : IVenomTypeService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly ILogger<VenomTypeService> _logger;

    public VenomTypeService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        ILogger<VenomTypeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<VenomTypeResponses.VenomTypeResponse> CreateVenomTypeAsync(VenomTypeRequests.CreateVenomTypeRequest request)
    {
        if (request == null)
        {
            throw new BadRequestException("Request data cannot be null.");
        }

        await EnsureNameIsUniqueAsync(request.Name, null);
        await EnsureFirstAidGuidelineExistsAsync(request.FirstAidGuidelineId);

        var entity = new VenomType
        {
            Name = request.Name.Trim(),
            ScientificName = request.ScientificName?.Trim(),
            Description = request.Description.Trim(),
            IsActive = request.IsActive,
            SeverityIndex = request.SeverityIndex,
            FirstAidGuidelineId = request.FirstAidGuidelineId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.GetRepository<VenomType>().InsertAsync(entity);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Created venom type with ID {Id}", entity.Id);
        return entity.Adapt<VenomTypeResponses.VenomTypeResponse>();
    }

    public async Task<VenomTypeResponses.VenomTypeResponse> GetVenomTypeByIdAsync(int id)
    {
        var entity = await _unitOfWork.GetRepository<VenomType>().GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException($"Venom type with ID {id} not found.");
        }

        return entity.Adapt<VenomTypeResponses.VenomTypeResponse>();
    }

    public async Task<List<VenomTypeResponses.VenomTypeResponse>> GetAllVenomTypesAsync()
    {
        var entities = await _unitOfWork.GetRepository<VenomType>()
            .GetListAsync(orderBy: q => q.OrderBy(x => x.Name));

        return entities.Adapt<List<VenomTypeResponses.VenomTypeResponse>>();
    }

    public async Task<VenomTypeResponses.VenomTypeResponse> UpdateVenomTypeAsync(int id, VenomTypeRequests.UpdateVenomTypeRequest request)
    {
        if (request == null)
        {
            throw new BadRequestException("Request data cannot be null.");
        }

        var repository = _unitOfWork.GetRepository<VenomType>();
        var entity = await repository.FirstOrDefaultAsync(predicate: x => x.Id == id, asNoTracking: false);

        if (entity == null)
        {
            throw new NotFoundException($"Venom type with ID {id} not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var normalizedName = request.Name.Trim();
            await EnsureNameIsUniqueAsync(normalizedName, id);
            entity.Name = normalizedName;
        }

        if (request.ScientificName != null)
        {
            entity.ScientificName = request.ScientificName.Trim();
        }

        if (request.Description != null)
        {
            entity.Description = request.Description.Trim();
        }

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        if (request.SeverityIndex.HasValue)
        {
            entity.SeverityIndex = request.SeverityIndex.Value;
        }

        if (request.FirstAidGuidelineId.HasValue)
        {
            await EnsureFirstAidGuidelineExistsAsync(request.FirstAidGuidelineId.Value);
            entity.FirstAidGuidelineId = request.FirstAidGuidelineId.Value;
        }

        entity.UpdatedAt = DateTime.UtcNow;

        repository.Update(entity);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Updated venom type with ID {Id}", id);
        return entity.Adapt<VenomTypeResponses.VenomTypeResponse>();
    }

    public async Task DeleteVenomTypeAsync(int id)
    {
        var repository = _unitOfWork.GetRepository<VenomType>();
        var entity = await repository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException($"Venom type with ID {id} not found.");
        }

        repository.Delete(entity);
        await _unitOfWork.CommitAsync();
        _logger.LogInformation("Deleted venom type with ID {Id}", id);
    }

    private async Task EnsureFirstAidGuidelineExistsAsync(int firstAidGuidelineId)
    {
        var exists = await _unitOfWork.GetRepository<FirstAidGuideline>()
            .ExistsAsync(x => x.Id == firstAidGuidelineId);

        if (!exists)
        {
            throw new NotFoundException($"First aid guideline with ID {firstAidGuidelineId} not found.");
        }
    }

    private async Task EnsureNameIsUniqueAsync(string name, int? excludeId)
    {
        var normalizedName = name.Trim();
        var exists = await _unitOfWork.GetRepository<VenomType>()
            .ExistsAsync(x => x.Name.ToLower() == normalizedName.ToLower() && (!excludeId.HasValue || x.Id != excludeId.Value));

        if (exists)
        {
            throw new BadRequestException($"Venom type with name '{normalizedName}' already exists.");
        }
    }
}
