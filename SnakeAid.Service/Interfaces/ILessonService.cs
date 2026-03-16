using SnakeAid.Core.Requests.Lesson;
using SnakeAid.Core.Responses.Lesson;

namespace SnakeAid.Service.Interfaces
{
    public interface ILessonService
    {
        Task<LessonResponse> CreateLessonAsync(CreateLessonRequest request);
        Task<LessonResponse> GetLessonByIdAsync(Guid id);
        Task<List<LessonResponse>> GetAllLessonsAsync();
        Task<LessonResponse> UpdateLessonAsync(Guid id, UpdateLessonRequest request);
        Task<bool> DeleteLessonAsync(Guid id);
    }
}
