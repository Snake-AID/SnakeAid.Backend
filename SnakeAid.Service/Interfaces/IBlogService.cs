using SnakeAid.Core.Requests.Blog;
using SnakeAid.Core.Responses.Blog;

namespace SnakeAid.Service.Interfaces
{
    public interface IBlogService
    {
        Task<BlogResponse> CreateBlogAsync(CreateBlogRequest request, Guid authorId);

        Task<List<BlogResponse>> GetBlogsAsync(GetBlogsRequest request);

        Task<BlogResponse> GetBlogByIdAsync(Guid id);

        Task<BlogResponse> UpdateBlogAsync(Guid id, UpdateBlogRequest request);

        Task<BlogResponse> UpdateBlogStatusAsync(Guid id, UpdateBlogStatusRequest request);

        Task DeleteBlogAsync(Guid id);
    }
}
