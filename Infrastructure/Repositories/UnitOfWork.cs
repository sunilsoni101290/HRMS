using Domain.Interfaces;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Concurrent;

namespace Infrastructure.Repositories
{
    /// <summary>EF Core-backed IUnitOfWork - see IUnitOfWork for the rationale/scope (Loan &amp; Advance module only).</summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        // One Repository<T> instance per entity type, reused for the
        // lifetime of this (scoped, i.e. per-HTTP-request) UnitOfWork, so
        // repeated Repository<T>() calls within the same request share the
        // same change tracker state.
        private readonly ConcurrentDictionary<Type, object> _repositories = new();

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<T> Repository<T>() where T : class, IEntity
        {
            return (IRepository<T>)_repositories.GetOrAdd(
                typeof(T),
                _ => new Repository<T>(_context));
        }

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<IDbContextTransaction> BeginTransactionAsync()
            => _context.Database.BeginTransactionAsync();
    }
}
