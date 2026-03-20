using Mapster;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Antivenom;
using SnakeAid.Core.Responses.Antivenom;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class AntivenomService : IAntivenomService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly ILogger<AntivenomService> _logger;

    public AntivenomService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        ILogger<AntivenomService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AntivenomResponse> CreateAntivenomAsync(CreateAntivenomRequest request)
    {
        if (request == null)
        {
            throw new BadRequestException("Request data cannot be null.");
        }

        var entity = new Antivenom
        {
            Name = request.Name.Trim(),
            Manufacturer = request.Manufacturer.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.GetRepository<Antivenom>().InsertAsync(entity);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Created antivenom with ID {Id}", entity.Id);
        return entity.Adapt<AntivenomResponse>();
    }

    public async Task<AntivenomResponse> GetAntivenomByIdAsync(int id)
    {
        var entity = await _unitOfWork.GetRepository<Antivenom>().GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException($"Antivenom with ID {id} not found.");
        }

        return entity.Adapt<AntivenomResponse>();
    }

    public async Task<List<AntivenomResponse>> GetAllAntivenomsAsync()
    {
        var entities = await _unitOfWork.GetRepository<Antivenom>()
            .GetListAsync(orderBy: q => q.OrderBy(x => x.Name));

        return entities.Adapt<List<AntivenomResponse>>();
    }

    public async Task<AntivenomResponse> UpdateAntivenomAsync(int id, UpdateAntivenomRequest request)
    {
        if (request == null)
        {
            throw new BadRequestException("Request data cannot be null.");
        }

        var repository = _unitOfWork.GetRepository<Antivenom>();
        var entity = await repository.FirstOrDefaultAsync(predicate: x => x.Id == id, asNoTracking: false);

        if (entity == null)
        {
            throw new NotFoundException($"Antivenom with ID {id} not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            entity.Name = request.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Manufacturer))
        {
            entity.Manufacturer = request.Manufacturer.Trim();
        }

        if (request.Description != null)
        {
            entity.Description = request.Description.Trim();
        }

        entity.UpdatedAt = DateTime.UtcNow;

        repository.Update(entity);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Updated antivenom with ID {Id}", id);
        return entity.Adapt<AntivenomResponse>();
    }

    public async Task DeleteAntivenomAsync(int id)
    {
        var repository = _unitOfWork.GetRepository<Antivenom>();
        var entity = await repository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException($"Antivenom with ID {id} not found.");
        }

        repository.Delete(entity);
        await _unitOfWork.CommitAsync();
        _logger.LogInformation("Deleted antivenom with ID {Id}", id);
    }
}