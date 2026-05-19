using FODUN.Reservations.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FODUN.Reservations.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ReservationsDbContext _context;
    private IDbContextTransaction? _transaction;

    // InMemory provider no soporta transacciones reales; se detecta en runtime
    private bool IsInMemory =>
        _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

    public UnitOfWork(ReservationsDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInMemory)
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
