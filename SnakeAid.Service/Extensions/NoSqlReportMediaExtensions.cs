using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Interfaces;

namespace SnakeAid.Service.Extensions
{
    public static class NoSqlReportMediaExtensions
    {
        /// <summary>
        /// Explicitly loads and attaches ReportMedia to a collection of parent entities in a single DB query, solving the N+1 problem.
        /// </summary>
        /// <typeparam name="T">Any entity implementing IHasReportMedia</typeparam>
        /// <param name="entities">The collection of parent entities to attach media to</param>
        /// <param name="unitOfWork">The UnitOfWork instance to query the repository</param>
        /// <param name="referenceType">The specific polymorphic type corresponding to these entities</param>
        public static async Task AttachReportMediaAsync<T>(
            this IEnumerable<T> entities,
            IUnitOfWork unitOfWork,
            MediaReferenceType referenceType) where T : IHasReportMedia
        {
            if (entities == null || !entities.Any())
            {
                return;
            }

            var entityIds = entities.Select(e => e.Id).ToList();

            // Single query to fetch all media for the given entities WITH AIRecognitionResults
            var allMedia = await unitOfWork.GetRepository<ReportMedia>()
                .GetListAsync(
                    predicate: m => m.ReferenceId.HasValue && entityIds.Contains(m.ReferenceId.Value) && m.ReferenceType == referenceType,
                    include: q => q
                        .Include(m => m.AIRecognitionResults)
                            .ThenInclude(r => r.AIModel)
                        .Include(m => m.AIRecognitionResults)
                            .ThenInclude(r => r.DetectedSpecies)
                                .ThenInclude(s => s.SpeciesVenoms)
                                    .ThenInclude(sv => sv.VenomType)
                                        .ThenInclude(v => v.FirstAidGuideline));

            // Group media by ReferenceId for O(1) in-memory lookup
            var mediaByReferenceId = allMedia
                .Where(m => m.ReferenceId.HasValue)
                .GroupBy(m => m.ReferenceId!.Value)
                .ToDictionary(g => g.Key, g => (ICollection<ReportMedia>)g.ToList());

            // Attach media to the original entities
            foreach (var entity in entities)
            {
                entity.Media = mediaByReferenceId.GetValueOrDefault(entity.Id) ?? new List<ReportMedia>();
            }
        }

        /// <summary>
        /// Overload for a single entity
        /// </summary>
        public static async Task AttachReportMediaAsync<T>(
            this T entity,
            IUnitOfWork unitOfWork,
            MediaReferenceType referenceType) where T : IHasReportMedia
        {
            if (entity == null) return;

            await new[] { entity }.AttachReportMediaAsync(unitOfWork, referenceType);
        }
    }
}
