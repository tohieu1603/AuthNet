using System.Text.RegularExpressions;
using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Mappers;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Models.User;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;

namespace AuthService.Application.Handlers;

public sealed partial class LoginWithGoogleHandler(
    IGoogleIdTokenVerifierPort googleVerifier,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordEncoderPort passwordEncoder,
    ITokenProviderPort tokenProvider,
    TokenDomainService tokenDomainService,
    int refreshExpiryDays = 7
) : ICommandHandler<LoginWithGoogleCommand, AuthResponseDto>
{
    private const string DefaultRole = "ROLE_USER";
    private const int MaxUsernameSuffixTries = 1000;

    [GeneratedRegex(@"[^a-zA-Z0-9_-]", RegexOptions.Compiled)]
    private static partial Regex CleanUsernameRegex();

    public async Task<AuthResponseDto> HandleAsync(LoginWithGoogleCommand command, CancellationToken cancellationToken = default)
    {
        var claims = await googleVerifier.VerifyAsync(command.IdToken, cancellationToken);
        if (!claims.EmailVerified)
        {
            throw new OAuthEmailNotVerifiedException();
        }

        var googleSub = GoogleSub.Of(claims.Sub);
        var user = await userRepository.FindByGoogleSubAsync(googleSub, cancellationToken);

        if (user == null)
        {
            var existingByEmail = await userRepository.FindByEmailAsync(Email.Of(claims.Email), cancellationToken);
            if (existingByEmail != null)
            {
                existingByEmail.LinkGoogleAccount(googleSub);
                user = existingByEmail;
            }
            else
            {
                user = await RegisterNewGoogleUserAsync(claims, cancellationToken);
            }
        }

        user.EnsureAuthenticatable();

        var saved = await userRepository.SaveAsync(user, cancellationToken);
        var userRoles = await roleRepository.FindByIdInAsync(saved.Roles, cancellationToken);
        var roleNames = userRoles.Select(r => r.Name.Value).ToHashSet();

        var issued = tokenProvider.IssueAccessToken(saved, roleNames);
        var refresh = tokenDomainService.IssueForUser(saved, refreshExpiryDays);
        await refreshTokenRepository.SaveAsync(refresh, cancellationToken);

        return AuthResponseDto.Bearer(
            issued.Token,
            refresh.Value.Value,
            issued.ExpiresInSeconds,
            UserDtoMapper.ToDto(saved, userRoles)
        );
    }

    private async Task<User> RegisterNewGoogleUserAsync(
        IGoogleIdTokenVerifierPort.GoogleClaims claims,
        CancellationToken cancellationToken)
    {
        var username = await DeriveUniqueUsernameAsync(claims, cancellationToken);
        var personName = DerivePersonName(claims);
        var email = Email.Of(claims.Email);
        var sub = GoogleSub.Of(claims.Sub);

        var user = User.RegisterFromGoogle(username, email, personName, sub, passwordEncoder);

        var defaultRole = await roleRepository.FindByNameAsync(RoleName.Of(DefaultRole), cancellationToken);
        if (defaultRole != null)
        {
            user.AssignRole(defaultRole.Id);
        }

        return user;
    }

    private async Task<Username> DeriveUniqueUsernameAsync(
        IGoogleIdTokenVerifierPort.GoogleClaims claims,
        CancellationToken cancellationToken)
    {
        string baseName = SanitiseUsername(claims.Email);
        var safeBase = SafeUsername(baseName);
        if (!await userRepository.IsUsernameTakenAsync(safeBase, cancellationToken))
        {
            return safeBase;
        }

        for (int i = 1; i < MaxUsernameSuffixTries; i++)
        {
            string candidate = $"{baseName}{i}";
            var safeCandidate = SafeUsername(candidate);
            if (!await userRepository.IsUsernameTakenAsync(safeCandidate, cancellationToken))
            {
                return safeCandidate;
            }
        }

        return Username.Of($"{baseName}_{Guid.NewGuid().ToString("N")[..8]}");
    }

    private static string SanitiseUsername(string email)
    {
        string local = email.Contains('@') ? email[..email.IndexOf('@')] : email;
        string cleaned = CleanUsernameRegex().Replace(local, "");
        if (cleaned.Length < 3) cleaned += "user";
        if (cleaned.Length > 50) cleaned = cleaned[..50];
        return cleaned;
    }

    private static Username SafeUsername(string candidate)
    {
        try
        {
            return Username.Of(candidate);
        }
        catch (ArgumentException)
        {
            return Username.Of(Guid.NewGuid().ToString("N")[..8] + "x");
        }
    }

    private static PersonName DerivePersonName(IGoogleIdTokenVerifierPort.GoogleClaims claims)
    {
        string given = BlankToDefault(claims.GivenName, "Google");
        string family = BlankToDefault(claims.FamilyName, "User");

        if ((string.IsNullOrWhiteSpace(claims.GivenName) || string.IsNullOrWhiteSpace(claims.FamilyName))
            && !string.IsNullOrWhiteSpace(claims.Name) && claims.Name.Contains(' '))
        {
            var parts = claims.Name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && string.IsNullOrWhiteSpace(claims.GivenName)) given = parts[0];
            if (parts.Length > 1 && string.IsNullOrWhiteSpace(claims.FamilyName)) family = parts[1];
        }

        return PersonName.Of(given, family);
    }

    private static string BlankToDefault(string? s, string fallback) =>
        string.IsNullOrWhiteSpace(s) ? fallback : s.Trim();
}
