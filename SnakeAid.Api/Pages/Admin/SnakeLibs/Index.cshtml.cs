using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;

namespace SnakeAid.Api.Pages.Admin.SnakeLibs
{
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        public IList<SnakeSpecies> Species { get; set; } = new List<SnakeSpecies>();

        public IndexModel(IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task OnGetAsync()
        {
            // Eager-load alternative names, media, venoms (+ venom type), and catching tariffs
            Func<IQueryable<SnakeSpecies>, IQueryable<SnakeSpecies>> include = q => q
                .Include(s => s.AlternativeNames)
                .Include(s => s.LibraryMedias)
                .Include(s => s.SpeciesVenoms).ThenInclude(sv => sv.VenomType)
                .Include(s => s.SnakeCatchingTariffs);

            Species = (await _unitOfWork.GetRepository<SnakeSpecies>().GetListAsync(include: include, asNoTracking: true)).ToList();
        }
    }
}