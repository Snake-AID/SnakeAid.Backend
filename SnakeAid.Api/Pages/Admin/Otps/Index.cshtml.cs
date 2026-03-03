using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;

namespace SnakeAid.Api.Pages.Admin.Otps
{
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        public IList<Otp> Otps { get; set; } = new List<Otp>();

        public IndexModel(IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task OnGetAsync()
        {
            // Performance Optimization: Limit to top 100 most recent OTPs and use asNoTracking
            // Eager-load User (Account) to get owner details (FullName, Role, IsActive)
            Func<IQueryable<Otp>, IQueryable<Otp>> include = q => q
                .Include(o => o.User);

            var repository = _unitOfWork.GetRepository<Otp>();

            // Note: In a real enterprise scenario with millions of rows, we'd add .Take(100) or pagination.
            // For now, we fetch the most recent ones.
            var list = await repository.GetListAsync(
                include: include,
                orderBy: q => q.OrderByDescending(o => o.ExpirationTime),
                asNoTracking: true
            );

            Otps = list.Take(100).ToList();
        }
    }
}
