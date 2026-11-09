using AuthService.Domain.Services;

namespace AuthService.Infrastructure.Security;

public class BCryptPasswordEncoderAdapter : IPasswordEncoderPort
{
    public string Encode(string rawPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(rawPassword);
    }

    public bool Matches(string rawPassword, string encodedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(rawPassword, encodedPassword);
        }
        catch
        {
            return false;
        }
    }
}
