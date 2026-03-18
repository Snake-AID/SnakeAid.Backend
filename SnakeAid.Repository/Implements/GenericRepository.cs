using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Meta;
using SnakeAid.Repository.Interfaces;
using System.Linq.Expressions;

namespace SnakeAid.Repository.Implements
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly DbContext _dbContext;
        protected readonly DbSet<T> _dbSet;
        protected readonly ILogger<GenericRepository<T>>? _logger;

        public GenericRepository(DbContext context, ILogger<GenericRepository<T>>? logger = null)
        {
            _dbContext = context;
            _dbSet = context.Set<T>();
            _logger = logger;
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }

        #region Get Async

        public virtual async Task<T?> GetByIdAsync<TKey>(TKey id)
        {
            if (id == null) return null;
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                return await _dbSet.AnyAsync(cancellationToken);
            return await _dbSet.AsNoTracking().AnyAsync(predicate, cancellationToken);
        }

        public virtual async Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;

            if (include != null) query = include(query);

            if (predicate != null) query = query.Where(predicate);

            if (orderBy != null) query = orderBy(query);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<TResult?> FirstOrDefaultAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;

            if (include != null) query = include(query);

            if (predicate != null) query = query.Where(predicate);

            if (orderBy != null) query = orderBy(query);

            return await query.Select(selector).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<ICollection<T>> GetListAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int? take = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;

            if (include != null) query = include(query);

            if (predicate != null) query = query.Where(predicate);

            if (orderBy != null) query = orderBy(query);

            if (take.HasValue) query = query.Take(take.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public virtual async Task<ICollection<TResult>> GetListAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int? take = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;

            if (include != null) query = include(query);

            if (predicate != null) query = query.Where(predicate);

            if (orderBy != null) query = orderBy(query);

            var resultQuery = query.Select(selector);

            if (take.HasValue) resultQuery = resultQuery.Take(take.Value);

            return await resultQuery.ToListAsync(cancellationToken);
        }

        public Task<PagedData<T>> GetPagingListAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int page = 1,
            int size = 10,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (orderBy != null) return orderBy(query).ToPaginatedResponse(page, size, 1);
            return query.ToPaginatedResponse(page, size, 1);
        }

        public Task<PagedData<TResult>> GetPagingListAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int page = 1,
            int size = 10,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (orderBy != null) return orderBy(query).Select(selector).ToPaginatedResponse(page, size, 1);
            return query.Select(selector).ToPaginatedResponse(page, size, 1);
        }

        public virtual async Task<int> CountAsync(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            if (predicate != null)
                return await _dbSet.CountAsync(predicate, cancellationToken);

            return await _dbSet.CountAsync(cancellationToken);
        }

        public virtual async Task<HashSet<TProperty>> GetExistingValuesAsync<TProperty>(
            Expression<Func<T, TProperty>> selector,
            List<TProperty> candidates,
            Expression<Func<T, bool>>? additionalFilter = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking();

            if (additionalFilter != null)
                query = query.Where(additionalFilter);

            var values = await query
                .Select(selector)
                .Where(v => candidates.Contains(v))
                .ToListAsync(cancellationToken);

            return values.ToHashSet();
        }

        #endregion

        #region Insert

        public async Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) return null!;
            await _dbSet.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<T>> InsertRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null || !entities.Any()) return Enumerable.Empty<T>();
            await _dbSet.AddRangeAsync(entities, cancellationToken);
            return entities;
        }

        #endregion

        #region Update

        /// Updates an entity in the database.
        /// IMPORTANT: If entity was loaded via Include() navigation property and modified in-place,
        /// consider querying it separately before calling Update() to ensure changes are detected.
        /// nên clear ChangeTracker trước khi gọi Update với các entity phức tạp.
        public virtual bool Update(T entity)
        {
            if (entity == null) return false;

            try
            {
                // Get primary key values
                var keyValues = GetKeyValues(entity);
                if (keyValues == null || keyValues.Length == 0)
                    return false;

                // Check if entity with same key is already tracked
                var tracked = _dbSet.Local.FirstOrDefault(e =>
                    GetKeyValues(e)?.SequenceEqual(keyValues) == true);

                if (tracked != null)
                {
                    // CRITICAL: Check if same reference (entity modified in-place)
                    if (ReferenceEquals(tracked, entity))
                    {
                        // Same instance: entity was loaded and modified in-place
                        // Force Modified state to ensure SaveChanges() detects it
                        var trackedEntry = _dbContext.Entry(tracked);

                        // If state is Unchanged, force it to Modified
                        if (trackedEntry.State == EntityState.Unchanged)
                        {
                            trackedEntry.State = EntityState.Modified;
                        }
                        // If already Modified or other state, leave it as-is

                        return true;
                    }

                    // Different instances: copy values from source to tracked entity
                    // SetValues() will detect which properties changed
                    _dbContext.Entry(tracked).CurrentValues.SetValues(entity);
                    return true;
                }

                // Not tracked - safe to attach and mark as modified
                var entry = _dbContext.Entry(entity);
                if (entry.State == EntityState.Detached)
                    _dbSet.Attach(entity);

                entry.State = EntityState.Modified;
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating entity of type {EntityType}", typeof(T).Name);
                return false;
            }
        }

        private object[]? GetKeyValues(T entity)
        {
            var key = _dbContext.Model.FindEntityType(typeof(T))?.FindPrimaryKey();
            if (key == null) return null;

            return key.Properties
                .Select(p => _dbContext.Entry(entity).Property(p.Name).CurrentValue!)
                .ToArray();
        }

        public virtual bool UpdateProperties(
            T entity,
            params Expression<Func<T, object>>[] propertiesToUpdate)
        {
            if (entity == null) return false;

            try
            {
                var entry = _dbContext.Entry(entity);

                if (entry.State == EntityState.Detached)
                    _dbSet.Attach(entity);

                if (propertiesToUpdate?.Any() != true)
                {
                    entry.State = EntityState.Modified;
                }
                else
                {
                    foreach (var property in propertiesToUpdate)
                        entry.Property(property).IsModified = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating entity of type {EntityType}", typeof(T).Name);
                return false;
            }
        }

        /// Updates multiple entities at once.
        /// WARNING: This may conflict with already-tracked entities.
        /// Consider using individual Update() calls for better tracking control.
        public virtual bool UpdateRange(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                return false;

            try
            {
                var entityList = entities.ToList();

                foreach (var entity in entityList)
                {
                    var keyValues = GetKeyValues(entity);
                    if (keyValues == null || keyValues.Length == 0)
                        continue;

                    // Check if already tracked
                    var tracked = _dbSet.Local.FirstOrDefault(e =>
                        GetKeyValues(e)?.SequenceEqual(keyValues) == true);

                    if (tracked != null)
                    {
                        // Already tracked: update values
                        if (ReferenceEquals(tracked, entity))
                        {
                            // Same reference: force modified state
                            var entry = _dbContext.Entry(tracked);
                            if (entry.State == EntityState.Unchanged)
                            {
                                entry.State = EntityState.Modified;
                            }
                        }
                        else
                        {
                            // Different instance: copy values
                            _dbContext.Entry(tracked).CurrentValues.SetValues(entity);
                        }
                    }
                    else
                    {
                        // Not tracked: attach and mark as modified
                        var entry = _dbContext.Entry(entity);
                        if (entry.State == EntityState.Detached)
                            _dbSet.Attach(entity);
                        entry.State = EntityState.Modified;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating entities of type {EntityType}", typeof(T).Name);
                return false;
            }
        }


        #endregion

        #region Delete

        public virtual bool Delete(T entity)
        {
            if (entity == null) return false;

            try
            {
                _dbSet.Remove(entity);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting entity of type {EntityType}", typeof(T).Name);
                return false;
            }
        }

        public virtual bool DeleteRange(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                return false;

            try
            {
                _dbSet.RemoveRange(entities);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting entities of type {EntityType}", typeof(T).Name);
                return false;
            }
        }

        #endregion

        #region queryable

        /// Tạo query base từ DbSet, cho phép chọn có AsNoTracking hay không.
        /// Caller tự LINQ (Where, Include, OrderBy...) để viết query.
        public virtual IQueryable<T> CreateBaseQuery(bool asNoTracking = true)
        {
            return asNoTracking
                ? _dbSet.AsNoTracking()
                : _dbSet;
        }

        #endregion
    }
}
