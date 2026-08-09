using Domain.Interfaces;
using System.Linq.Expressions;

namespace Infrastructure.Interfaces
{
    /// <summary>
    /// Generic data-access abstraction introduced for the Loan &amp; Advance
    /// module per its explicit "Repository Pattern" tech-stack requirement.
    /// NOTE: the rest of this codebase's Application-layer services inject
    /// <c>ApplicationDbContext</c> directly (see BiometricDeviceService,
    /// ProbationConfirmationService, etc.) - this repository/UnitOfWork
    /// pair is scoped to Loan &amp; Advance only and does not retrofit that
    /// existing convention elsewhere.
    ///
    /// Lives in Infrastructure (alongside <see cref="ITenantService"/>,
    /// the existing interface-in-Infrastructure precedent) rather than in
    /// Application, because Application already has a ProjectReference on
    /// Infrastructure (see Application/Services/SequenceService.cs
    /// injecting ApplicationDbContext) - putting the interface in
    /// Application and the implementation in Infrastructure would create a
    /// circular project reference.
    ///
    /// <see cref="Query"/> returns a real EF Core <see cref="IQueryable{T}"/>,
    /// so callers still compose <c>.Include()</c>/<c>.Where()</c>/projections
    /// exactly like the rest of the codebase does directly against DbSets -
    /// the abstraction is about WHERE the DbSet is obtained from (behind an
    /// interface, swappable/mockable for unit tests - see Phase 17), not
    /// about hiding LINQ/EF capabilities behind a narrow method surface.
    /// </summary>
    public interface IRepository<T> where T : class, IEntity
    {
        /// <summary>
        /// Composable query root. asNoTracking=true (default) for read
        /// paths; pass false only when the caller intends to mutate and
        /// call <see cref="Update"/> immediately after.
        /// </summary>
        IQueryable<T> Query(bool asNoTracking = true);

        Task<T?> GetByIdAsync(string id);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Assigns a new sequence-prefixed Id (via IDManager.GetNewId,
        /// same convention as every other entity in this codebase) if the
        /// caller hasn't already set one, then stages an Add. Does NOT
        /// call SaveChanges - see IUnitOfWork.SaveChangesAsync.
        /// </summary>
        Task AddAsync(T entity);

        /// <summary>Stages an update (attaches+marks Modified if the entity isn't already tracked).</summary>
        void Update(T entity);

        /// <summary>
        /// Hard removal - only appropriate for entities with no
        /// meaningful history value. Everything deriving BaseEntity
        /// should instead be soft-deleted (IsDeleted=true + Update),
        /// consistent with this codebase's global soft-delete query
        /// filter in ApplicationDbContext.
        /// </summary>
        void Remove(T entity);
    }
}
