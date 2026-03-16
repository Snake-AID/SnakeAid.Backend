using Mapster;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Lesson;
using SnakeAid.Core.Responses.Lesson;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class LessonService : ILessonService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<LessonService> _logger;

        public LessonService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<LessonService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<LessonResponse> CreateLessonAsync(CreateLessonRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var lesson = new Lesson
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title,
                    Content = request.Content,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<Lesson>().InsertAsync(lesson);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Created lesson with ID: {LessonId}", lesson.Id);

                return lesson.Adapt<LessonResponse>();
            });
        }

        public async Task<LessonResponse> GetLessonByIdAsync(Guid id)
        {
            var lesson = await _unitOfWork.GetRepository<Lesson>().GetByIdAsync(id);

            if (lesson == null)
            {
                throw new NotFoundException($"Lesson with ID {id} not found.");
            }

            return lesson.Adapt<LessonResponse>();
        }

        public async Task<List<LessonResponse>> GetAllLessonsAsync()
        {
            var lessons = await _unitOfWork.GetRepository<Lesson>()
                .GetListAsync(orderBy: query => query.OrderByDescending(l => l.UpdatedAt));

            return lessons.Adapt<List<LessonResponse>>();
        }

        public async Task<LessonResponse> UpdateLessonAsync(Guid id, UpdateLessonRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var lesson = await _unitOfWork.GetRepository<Lesson>().GetByIdAsync(id);

                if (lesson == null)
                {
                    throw new NotFoundException($"Lesson with ID {id} not found.");
                }

                if (!string.IsNullOrWhiteSpace(request.Title))
                {
                    lesson.Title = request.Title;
                }

                if (!string.IsNullOrWhiteSpace(request.Content))
                {
                    lesson.Content = request.Content;
                }

                lesson.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.GetRepository<Lesson>().Update(lesson);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Updated lesson with ID: {LessonId}", id);

                return lesson.Adapt<LessonResponse>();
            });
        }

        public async Task<bool> DeleteLessonAsync(Guid id)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var lesson = await _unitOfWork.GetRepository<Lesson>().GetByIdAsync(id);

                if (lesson == null)
                {
                    throw new NotFoundException($"Lesson with ID {id} not found.");
                }

                _unitOfWork.GetRepository<Lesson>().Delete(lesson);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Deleted lesson with ID: {LessonId}", id);

                return true;
            });
        }
    }
}
