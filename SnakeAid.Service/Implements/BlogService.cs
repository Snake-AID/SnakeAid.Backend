using Microsoft.EntityFrameworkCore;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Blog;
using SnakeAid.Core.Responses.Blog;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        public BlogService(IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BlogResponse> CreateBlogAsync(CreateBlogRequest request, Guid authorId)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            if (request.Tags == null || request.Tags.Count == 0)
            {
                throw new BadRequestException("At least one blog tag is required.");
            }

            var account = await _unitOfWork.GetRepository<Account>()
                .FirstOrDefaultAsync(predicate: a => a.Id == authorId);

            if (account == null)
            {
                throw new NotFoundException($"Account with id '{authorId}' not found.");
            }

            var blog = new Blog
            {
                Id = Guid.NewGuid(),
                AuthorId = authorId,
                Title = request.Title.Trim(),
                ThumbnailUrl = request.ThumbnailUrl.Trim(),
                Content = request.Content,
                Category = request.Category,
                Tags = request.Tags.ToList(),
                ReadingTime = request.ReadingTime,
                Status = BlogStatus.Draft,
                RejectionReason = null
            };

            await _unitOfWork.GetRepository<Blog>().InsertAsync(blog);
            await _unitOfWork.CommitAsync();

            blog.Author = account;
            return ToResponse(blog);
        }

        public async Task<BlogResponse> UpdateBlogAsync(Guid id, UpdateBlogRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            if (request.Tags == null || request.Tags.Count == 0)
            {
                throw new BadRequestException("At least one blog tag is required.");
            }

            var blog = await _unitOfWork.GetRepository<Blog>()
                .FirstOrDefaultAsync(
                    predicate: b => b.Id == id,
                    include: q => q.Include(b => b.Author),
                    asNoTracking: false);

            if (blog == null)
            {
                throw new NotFoundException($"Blog with id '{id}' not found.");
            }

            blog.Title = request.Title.Trim();
            blog.ThumbnailUrl = request.ThumbnailUrl.Trim();
            blog.Content = request.Content;
            blog.Category = request.Category;
            blog.Tags = request.Tags.ToList();
            blog.ReadingTime = request.ReadingTime;
            blog.RejectionReason = string.IsNullOrWhiteSpace(request.RejectionReason)
                ? null
                : request.RejectionReason.Trim();

            _unitOfWork.GetRepository<Blog>().Update(blog);
            await _unitOfWork.CommitAsync();

            return ToResponse(blog);
        }

        public async Task<List<BlogResponse>> GetBlogsAsync(GetBlogsRequest request)
        {
            request ??= new GetBlogsRequest();

            var keyword = request.BlogName?.Trim();

            var blogs = await _unitOfWork.GetRepository<Blog>()
                .GetListAsync(
                    predicate: b =>
                        (!request.AccountId.HasValue || b.AuthorId == request.AccountId.Value)
                        && (!request.Status.HasValue || b.Status == request.Status.Value)
                        && (string.IsNullOrWhiteSpace(keyword) || b.Title.ToLower().Contains(keyword.ToLower())),
                    orderBy: q => q.OrderByDescending(b => b.CreatedAt),
                    include: q => q.Include(b => b.Author));

            return blogs.Select(ToResponse).ToList();
        }

        public async Task<BlogResponse> GetBlogByIdAsync(Guid id)
        {
            var blog = await _unitOfWork.GetRepository<Blog>()
                .FirstOrDefaultAsync(
                    predicate: b => b.Id == id,
                    include: q => q.Include(b => b.Author));

            if (blog == null)
            {
                throw new NotFoundException($"Blog with id '{id}' not found.");
            }

            return ToResponse(blog);
        }

        public async Task<BlogResponse> UpdateBlogStatusAsync(Guid id, UpdateBlogStatusRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            var blog = await _unitOfWork.GetRepository<Blog>()
                .FirstOrDefaultAsync(
                    predicate: b => b.Id == id,
                    include: q => q.Include(b => b.Author),
                    asNoTracking: false);

            if (blog == null)
            {
                throw new NotFoundException($"Blog with id '{id}' not found.");
            }

            if (request.Status == BlogStatus.Rejected && string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                throw new BadRequestException("Rejection reason is required when status is Rejected.");
            }

            blog.Status = request.Status;
            blog.RejectionReason = request.Status == BlogStatus.Rejected
                ? request.RejectionReason?.Trim()
                : null;

            _unitOfWork.GetRepository<Blog>().Update(blog);
            await _unitOfWork.CommitAsync();

            return ToResponse(blog);
        }

        public async Task<BlogResponse> IncreaseViewAsync(Guid id)
        {
            var blog = await _unitOfWork.GetRepository<Blog>()
                .FirstOrDefaultAsync(
                    predicate: b => b.Id == id,
                    include: q => q.Include(b => b.Author),
                    asNoTracking: false);

            if (blog == null)
            {
                throw new NotFoundException($"Blog with id '{id}' not found.");
            }

            blog.ViewCount += 1;

            _unitOfWork.GetRepository<Blog>().Update(blog);
            await _unitOfWork.CommitAsync();

            return ToResponse(blog);
        }

        public async Task<BlogResponse> LikeBlogAsync(Guid id, Guid userId)
        {
            var blog = await _unitOfWork.GetRepository<Blog>()
                .FirstOrDefaultAsync(
                    predicate: b => b.Id == id,
                    include: q => q.Include(b => b.Author),
                    asNoTracking: false);

            if (blog == null)
            {
                throw new NotFoundException($"Blog with id '{id}' not found.");
            }

            blog.LikedViewer ??= new List<string>();
            var userIdText = userId.ToString();

            if (!blog.LikedViewer.Contains(userIdText))
            {
                blog.LikedViewer.Add(userIdText);
                blog.LikeCount += 1;
            }

            _unitOfWork.GetRepository<Blog>().Update(blog);
            await _unitOfWork.CommitAsync();

            return ToResponse(blog);
        }

        public async Task<BlogResponse> UnlikeBlogAsync(Guid id, Guid userId)
        {
            var blog = await _unitOfWork.GetRepository<Blog>()
                .FirstOrDefaultAsync(
                    predicate: b => b.Id == id,
                    include: q => q.Include(b => b.Author),
                    asNoTracking: false);

            if (blog == null)
            {
                throw new NotFoundException($"Blog with id '{id}' not found.");
            }

            blog.LikedViewer ??= new List<string>();
            var userIdText = userId.ToString();

            if (blog.LikedViewer.Remove(userIdText) && blog.LikeCount > 0)
            {
                blog.LikeCount -= 1;
            }

            _unitOfWork.GetRepository<Blog>().Update(blog);
            await _unitOfWork.CommitAsync();

            return ToResponse(blog);
        }

        public async Task DeleteBlogAsync(Guid id)
        {
            var blog = await _unitOfWork.GetRepository<Blog>()
                .FirstOrDefaultAsync(
                    predicate: b => b.Id == id,
                    asNoTracking: false);

            if (blog == null)
            {
                throw new NotFoundException($"Blog with id '{id}' not found.");
            }

            _unitOfWork.GetRepository<Blog>().Delete(blog);
            await _unitOfWork.CommitAsync();
        }

        private static BlogResponse ToResponse(Blog blog)
        {
            return new BlogResponse
            {
                Id = blog.Id,
                AuthorId = blog.AuthorId,
                Title = blog.Title,
                ThumbnailUrl = blog.ThumbnailUrl,
                Content = blog.Content,
                Category = blog.Category,
                Tags = blog.Tags.ToList(),
                ViewCount = blog.ViewCount,
                LikeCount = blog.LikeCount,
                ReadingTime = blog.ReadingTime,
                Status = blog.Status,
                RejectionReason = blog.RejectionReason,
                LikedViewer = blog.LikedViewer?.ToList() ?? new List<string>(),
                CreatedAt = blog.CreatedAt,
                UpdatedAt = blog.UpdatedAt,
                Account = blog.Author == null
                    ? null
                    : new BlogAuthorResponse
                    {
                        Id = blog.Author.Id,
                        FullName = blog.Author.FullName,
                        Email = blog.Author.Email,
                        AvatarUrl = blog.Author.AvatarUrl,
                        Role = blog.Author.Role,
                        IsActive = blog.Author.IsActive
                    }
            };
        }
    }
}
