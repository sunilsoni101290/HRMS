using Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Interfaces
{
    /// <summary>
    /// Coordinates one or more <see cref="IRepository{T}"/> instances
    /// against a single EF Core change tracker, so a Loan &amp; Advance
    /// service method that touches several tables (e.g. DisburseAsync
    /// writing EmployeeLoan + N LoanEmiSchedule rows) commits them
    /// atomically with one SaveChangesAsync call - or explicitly via
    /// <see cref="BeginTransactionAsync"/> when a step in between needs its
    /// own intermediate SaveChanges.
    /// </summary>
    public interface IUnitOfWork
    {
        IRepository<T> Repository<T>() where T : class, IEntity;

        Task<int> SaveChangesAsync();

        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
