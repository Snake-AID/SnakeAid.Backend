using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;

namespace SnakeAid.Api.Pages.Admin.SnakebiteIncidents
{
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        public IList<SnakebiteIncident> Incidents { get; set; } = new List<SnakebiteIncident>();

        public IndexModel(IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task OnGetAsync()
        {
            // Eager-load common related entities (user -> account, assigned rescuer, sessions -> requests -> rescuer -> account, media)
            Func<IQueryable<SnakebiteIncident>, IQueryable<SnakebiteIncident>> include = query => query
                .Include(i => i.User).ThenInclude(u => u.Account)
                .Include(i => i.AssignedRescuer).ThenInclude(r => r.Account)
                .Include(i => i.Sessions).ThenInclude(s => s.Requests).ThenInclude(r => r.Rescuer).ThenInclude(rp => rp.Account)
                .Include(i => i.Media);

            Incidents = (await _unitOfWork.GetRepository<SnakebiteIncident>().GetListAsync(include: include, asNoTracking: true)).ToList();
        }
    }
}