using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.FilterQuestion;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class FilterQuestionService : IFilterQuestionService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<FilterQuestionService> _logger;

        public FilterQuestionService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<FilterQuestionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<FilterQuestionResponse>> GetAllFilterQuestionsAsync()
        {
            try
            {
                var questions = await _unitOfWork.GetRepository<FilterQuestion>()
                    .GetListAsync(
                        predicate: q => q.IsActive,
                        include: query => query.Include(q => q.FilterOptions.Where(o => o.IsActive)),
                        orderBy: query => query.OrderBy(q => q.Id),
                        asNoTracking: true
                    );

                var response = questions.Select(q => new FilterQuestionResponse
                {
                    Id = q.Id,
                    Question = q.Question,
                    Options = q.FilterOptions
                        .OrderBy(o => o.Id)
                        .Select(o => new FilterOptionResponse
                        {
                            Id = o.Id,
                            OptionText = o.OptionText,
                            OptionImageUrl = o.OptionImageUrl
                        })
                        .ToList()
                }).ToList();

                _logger.LogInformation("Retrieved {Count} filter questions with {TotalOptions} options",
                    response.Count, response.Sum(q => q.Options.Count));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filter questions: {Message}", ex.Message);
                throw;
            }
        }
    }
}
