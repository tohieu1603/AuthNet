using AuthService.Domain.Models.User;
using AuthService.Domain.Models.User.VO;

namespace AuthService.Domain.Repositories;

/// <summary>
/// Defines the contracts for User persistence without exposing implementation details.
/// </summary>
public interface IUserRepository
{
    Task<User> SaveAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> FindByIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<User?> FindByUsernameAsync(Username username, CancellationToken cancellationToken = default);
    Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<User?> FindByGoogleSubAsync(GoogleSub googleSub, CancellationToken cancellationToken = default);
    Task<bool> IsUsernameTakenAsync(Username username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(Username username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> FindByIdWithRolesAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<User?> FindByUsernameWithRolesAsync(Username username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> FindAfterCursorAsync(DateTimeOffset? cursorCreatedAt, string? cursorId, int limit, CancellationToken cancellationToken = default);
}
