using FODUN.Reservations.Domain.Aggregates.User;
using FODUN.Reservations.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FODUN.Reservations.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ReservationsDbContext _context;

    public UserRepository(ReservationsDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.Users.FirstOrDefaultAsync(
            u => u.Email == email.ToLowerInvariant() && u.IsActive, cancellationToken);

    public async Task<User?> GetByDocumentNumberAsync(string documentNumber, CancellationToken cancellationToken = default) =>
        await _context.Users.FirstOrDefaultAsync(
            u => u.DocumentNumber == documentNumber && u.IsActive, cancellationToken);

    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await _context.Users.FirstOrDefaultAsync(
            u => u.PasswordResetToken == token && u.IsActive, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task<bool> ExistsByDocumentNumberAsync(string documentNumber, CancellationToken cancellationToken = default) =>
        await _context.Users.AnyAsync(u => u.DocumentNumber == documentNumber, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _context.Users.AddAsync(user, cancellationToken);

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }
}
