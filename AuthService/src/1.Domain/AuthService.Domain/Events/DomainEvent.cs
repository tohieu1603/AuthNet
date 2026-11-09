using System.Transactions;

namespace AuthService.Domain.Events
{
    public abstract record DomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
        public virtual string EventType => GetType().Name;

        public abstract string AggregateId { get; }

        public override string ToString() => 
            $"{EventType} {{ EventId = {EventId}, AggregateId = {AggregateId}, OccurredOn = {OccurredOn} }}";
    }
}