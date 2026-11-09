namespace AuthService.Domain.Models.Permission.VO
{
    public readonly record struct PermissionId
    {
        public string Value { get; }

        public PermissionId(string value)
        {
            if(string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            var trimmedValue = value.Trim();

            if(!Guid.TryParse(trimmedValue, out _))
            {
                throw new ArgumentException("PermissionId must be a valid GUID.", nameof(value));
            }

            Value = trimmedValue;
        }

        public static PermissionId Generate() => new(Guid.NewGuid().ToString());
        public static PermissionId Of(string value) => new(value);
        public override string ToString() => Value;
    }
}