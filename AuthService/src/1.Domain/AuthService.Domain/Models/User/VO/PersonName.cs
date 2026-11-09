namespace AuthService.Domain.Models.User.VO;

public readonly record struct PersonName
{
    public string FirstName { get; }
    public string LastName { get; }

    public PersonName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be null or empty.", nameof(firstName));
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be null or empty.", nameof(lastName));
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public static PersonName Of(string firstName, string lastName) => new(firstName, lastName);
    public string FullName() => $"{FirstName} {LastName}";
    public override string ToString() => FullName();
}