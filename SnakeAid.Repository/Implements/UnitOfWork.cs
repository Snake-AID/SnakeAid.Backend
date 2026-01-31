using SnakeAid.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Repository.Implements
{
    public class UnitOfWork<TContext> : IUnitOfWork<TContext> where TContext : DbContext
    {
        public TContext Context { get; }
        private Dictionary<Type, object> _repositories;

        public UnitOfWork(TContext context)
        {
            Context = context;
        }

        #region Repository Management
        public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            _repositories ??= new Dictionary<Type, object>();
            if (_repositories.TryGetValue(typeof(TEntity), out object repository))
            {
                return (IGenericRepository<TEntity>)repository;
            }

            repository = new GenericRepository<TEntity>(Context);
            _repositories.Add(typeof(TEntity), repository);
            return (IGenericRepository<TEntity>)repository;
        }
        #endregion

        #region Transaction Management
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            var executionStrategy = Context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await Context.Database.BeginTransactionAsync();
                try
                {
                    var result = await operation();
                    await CommitAsync();
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    await RollbackAsync();
                    throw;
                }
            });
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            var executionStrategy = Context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await Context.Database.BeginTransactionAsync();
                try
                {
                    await operation();
                    await CommitAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    await RollbackAsync();
                    throw;
                }
            });
        }

        public int Commit()
        {
            TrackChanges();
            return Context.SaveChanges();
        }

        public async Task<int> CommitAsync()
        {
            TrackChanges();
            return await Context.SaveChangesAsync();
        }

        public Task RollbackAsync()
        {
            Context.ChangeTracker.Clear();
            return Task.CompletedTask;
        }


        private void TrackChanges()
        {
            var validationResults = new List<ValidationResult>();

            var entries = Context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added );

            foreach (var entry in entries)
            {
                var entity = entry.Entity;
                var context = new ValidationContext(entity);

                bool isValid = Validator.TryValidateObject(entity, context, validationResults, true);

                if (!isValid)
                {
                    var exceptionMessage = string.Join(Environment.NewLine,
                        validationResults.Select(error => $"Error: {error.ErrorMessage}"));

                    throw new ValidationException(exceptionMessage);
                }
            }
        }

        #endregion

        #region IDisposable Implementation
        public void Dispose() => GC.SuppressFinalize(this);
        #endregion
    }
}
