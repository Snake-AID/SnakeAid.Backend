using SnakeAid.Core.Meta;

namespace SnakeAid.Repository.Interfaces
{
    public interface IGenericRepository<T> : IDisposable where T : class
    {
        #region Get Async

        Task<T?> GetByIdAsync<TKey>(TKey id);
        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<TResult?> FirstOrDefaultAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<ICollection<T>> GetListAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int? take = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<ICollection<TResult>> GetListAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int? take = null,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<PagedData<T>> GetPagingListAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int page = 1,
            int size = 10,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<PagedData<TResult>> GetPagingListAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int page = 1,
            int size = 10,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        Task<HashSet<TProperty>> GetExistingValuesAsync<TProperty>(
            Expression<Func<T, TProperty>> selector,
            List<TProperty> candidates,
            Expression<Func<T, bool>>? additionalFilter = null,
            CancellationToken cancellationToken = default);

        #endregion

        #region Insert

        Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> InsertRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        #endregion

        #region Update

        bool Update(T entity);

        bool UpdateProperties(T entity, params Expression<Func<T, object>>[] propertiesToUpdate);

        bool UpdateRange(IEnumerable<T> entities);

        #endregion

        #region Delete

        bool Delete(T entity);
        bool DeleteRange(IEnumerable<T> entities);

        #endregion

        #region queryable

        IQueryable<T> CreateBaseQuery(bool asNoTracking = true);

        #endregion
    }
}
