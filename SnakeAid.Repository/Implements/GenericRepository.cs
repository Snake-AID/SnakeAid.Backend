using SnakeAid.Core.Meta;
using SnakeAid.Repository.Interfaces;

namespace SnakeAid.Repository.Implements
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly DbContext _dbContext;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(DbContext context)
        {
            _dbContext = context;
            _dbSet = context.Set<T>();
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

        public virtual bool Update(T entity)
        {
            if (entity == null) return false;

            try
            {
                var entityEntry = _dbContext.Entry(entity);

                // If entity is detached (not being tracked), attach it
                if (entityEntry.State == EntityState.Detached)
                    _dbSet.Attach(entity);

                // Mark entity as modified
                entityEntry.State = EntityState.Modified;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
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
            catch
            {
                return false;
            }
        }

        public virtual bool UpdateRange(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                return false;

            try
            {
                _dbSet.UpdateRange(entities);
                return true;
            }
            catch
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
