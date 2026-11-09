using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.RefreshToken.Exceptions;

public sealed class TokenReuseDetectedException : DomainException
{
    public string Family { get; }
    public int Generation { get; }

    public TokenReuseDetectedException(string family, int generation)
        : base(ErrorCode.TokenReuseDetected, $"Refresh token reuse detected for family {family} generation {generation}.")
    {
        Family = family;
        Generation = generation;
    }
}