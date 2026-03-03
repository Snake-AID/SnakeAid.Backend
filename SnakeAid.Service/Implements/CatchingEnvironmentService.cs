using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.CatchingEnvironment;
using SnakeAid.Core.Responses.CatchingEnvironment;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class CatchingEnvironmentService : ICatchingEnvironmentService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<CatchingEnvironmentService> _logger;

        public CatchingEnvironmentService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<CatchingEnvironmentService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CatchingEnvironmentResponse> CreateCatchingEnvironmentAsync(CreateCatchingEnvironmentRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var catchingEnvironment = new CatchingEnvironment
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Currency = request.Currency,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<CatchingEnvironment>().InsertAsync(catchingEnvironment);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Created new catching environment with ID: {Id}", catchingEnvironment.Id);

                return catchingEnvironment.Adapt<CatchingEnvironmentResponse>();
            });
        }

        public async Task<CatchingEnvironmentResponse> GetCatchingEnvironmentByIdAsync(int id)
        {
            var catchingEnvironment = await _unitOfWork.GetRepository<CatchingEnvironment>().GetByIdAsync(id);

            if (catchingEnvironment == null)
            {
                throw new NotFoundException($"Catching environment with ID {id} not found.");
            }

            return catchingEnvironment.Adapt<CatchingEnvironmentResponse>();
        }

        public async Task<List<CatchingEnvironmentResponse>> GetAllCatchingEnvironmentsAsync()
        {
            var catchingEnvironments = await _unitOfWork.GetRepository<CatchingEnvironment>()
                .GetListAsync(orderBy: query => query.OrderBy(ce => ce.Name));

            return catchingEnvironments.Adapt<List<CatchingEnvironmentResponse>>();
        }

        public async Task<CatchingEnvironmentResponse> UpdateCatchingEnvironmentAsync(int id, UpdateCatchingEnvironmentRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var catchingEnvironment = await _unitOfWork.GetRepository<CatchingEnvironment>().GetByIdAsync(id);

                if (catchingEnvironment == null)
                {
                    throw new NotFoundException($"Catching environment with ID {id} not found.");
                }

                // Update only if the values are provided
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    catchingEnvironment.Name = request.Name;
                }

                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    catchingEnvironment.Description = request.Description;
                }

                if (request.Price.HasValue)
                {
                    catchingEnvironment.Price = request.Price.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.Currency))
                {
                    catchingEnvironment.Currency = request.Currency;
                }

                catchingEnvironment.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.GetRepository<CatchingEnvironment>().Update(catchingEnvironment);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Updated catching environment with ID: {Id}", id);

                return catchingEnvironment.Adapt<CatchingEnvironmentResponse>();
            });
        }

        public async Task<bool> DeleteCatchingEnvironmentAsync(int id)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var catchingEnvironment = await _unitOfWork.GetRepository<CatchingEnvironment>().GetByIdAsync(id);

                if (catchingEnvironment == null)
                {
                    throw new NotFoundException($"Catching environment with ID {id} not found.");
                }

                // Check if there are any missions using this environment
                var hasMissions = await _unitOfWork.GetRepository<SnakeCatchingMission>()
                    .ExistsAsync(m => m.CatchingEnvironmentId == id);

                if (hasMissions)
                {
                    throw new BadRequestException("Cannot delete catching environment because it is being used by existing missions.");
                }

                _unitOfWork.GetRepository<CatchingEnvironment>().Delete(catchingEnvironment);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Deleted catching environment with ID: {Id}", id);

                return true;
            });
        }
    }
}
