using SnakeAid.Core.Responses.FilterQuestion;

namespace SnakeAid.Service.Interfaces
{
    public interface IFilterQuestionService
    {
        /// <summary>
        /// Get all active filter questions with their options
        /// </summary>
        Task<List<FilterQuestionResponse>> GetAllFilterQuestionsAsync();
    }
}
