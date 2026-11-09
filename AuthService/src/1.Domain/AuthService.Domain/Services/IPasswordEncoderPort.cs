namespace AuthService.Domain.Services;

public interface IPasswordEncoderPort
{
    string Encode(string rawPassword);
    bool Matches(string rawPassword, string encodedPassword);
}