namespace AuthService.Domain.Models.RefreshToken.VO;

public readonly record struct GenerationNumber
{
    public int Value { get; }
    public GenerationNumber(int value)
    {
        if(value < 0)
        {
            throw new ArgumentOutOfRangeException("Generation number cannot be negative.");
        }
        Value = value;
    }
    public static GenerationNumber Of(int value) => new GenerationNumber(value);
    public static GenerationNumber Root() => new GenerationNumber(0);



    public GenerationNumber Next() => new GenerationNumber(Value + 1);
    public bool IsAfter(GenerationNumber other) => Value > other.Value;
    public bool IsBefore(GenerationNumber other) => Value < other.Value;

    public bool IsSameAs(GenerationNumber other) => Value == other.Value;
    public int Difference(GenerationNumber other) => Value - other.Value;

    public static bool operator >(GenerationNumber left, GenerationNumber right) => left.Value > right.Value;
    public static bool operator <(GenerationNumber left, GenerationNumber right) => left.Value < right.Value;
    public static bool operator >=(GenerationNumber left, GenerationNumber right) => left.Value >= right.Value;
    public static bool operator <=(GenerationNumber left, GenerationNumber right) => left.Value <= right.Value;
    public override string ToString() => Value.ToString();
}