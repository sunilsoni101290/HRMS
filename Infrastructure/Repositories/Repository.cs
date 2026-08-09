using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// EF Core-backed generic repository - see Application.Interfaces.Common.IRepository{T}
    /// for the "why a repository at all in a codebase whose other modules
    /// inject ApplicationDbContext directly" rationale. One instance per
    /// entity type per <see cref="UnitOfWork"/>, all sharing the same
    /// ApplicationDbContext/change tracker so a single SaveChangesAsync
    /// commits everything staged across every repository obtained from
    /// that unit of work.
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _set;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _set = context.Set<T>();
        }

        public IQueryable<T> Query(bool asNoTracking = true)
        {
            var query = _set.AsQueryable();
            return asNoTracking ? query.AsNoTracking() : query;
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            return await _set.FirstOrDefaultAsync(BuildIdEquals(id));
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _set.AnyAsync(predicate);
        }

        public async Task AddAsync(T entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
                entity.Id = IDManager.GetNewId(entity);

            await _set.AddAsync(entity);
        }

        public void Update(T entity)
        {
            var entry = _context.Entry(entity);

            if (entry.State == EntityState.Detached)
                _set.Attach(entity);

            entry.State = EntityState.Modified;
        }

        public void Remove(T entity)
        {
            _set.Remove(entity);
        }

        // T.Id is declared on IEntity, not queryable via a compile-time
        // lambda without a generic constraint EF can translate - build the
        // `x => x.Id == id` expression tree manually so Query()/GetByIdAsync
        // still translate to a single indexed SQL lookup rather than
        // pulling rows client-side.
        private static Expression<Func<T, bool>> BuildIdEquals(string id)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, nameof(IEntity.Id));
            var constant = Expression.Constant(id, typeof(string));
            var equals = Expression.Equal(property, constant);
            return Expression.Lambda<Func<T, bool>>(equals, parameter);
        }
    }
}
