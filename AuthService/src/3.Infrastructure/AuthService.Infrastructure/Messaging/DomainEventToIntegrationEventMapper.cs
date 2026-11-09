using AuthService.Application.Events;
using AuthService.Domain.Events;
using AuthService.Domain.Models.RefreshToken.Events;
using AuthService.Domain.Models.Role.Events;
using AuthService.Domain.Models.User.Events;

namespace AuthService.Infrastructure.Messaging;

public class DomainEventToIntegrationEventMapper
{
    private const int SchemaV1 = 1;

    public sealed record RoutedEvent(string Topic, string Key, IIntegrationEvent Event);

    public RoutedEvent? Map(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            UserCreatedEvent e => UserLifecycle(e, "auth.user.created.v1", new Dictionary<string, object?>
            {
                ["userId"] = e.UserId,
                ["username"] = e.UserName,
                ["email"] = e.Email
            }),

            UserLoggedInEvent e => Session(e, "auth.session.logged_in.v1", new Dictionary<string, object?>
            {
                ["userId"] = e.UserId,
                ["username"] = e.UserName
            }),

            PasswordChangedEvent e => UserLifecycle(e, "auth.user.password_changed.v1", new Dictionary<string, object?>
            {
                ["userId"] = e.UserId,
                ["username"] = e.UserName
            }),

            EmailChangedEvent e => UserLifecycle(e, "auth.user.email_changed.v1", new Dictionary<string, object?>
            {
                ["userId"] = e.UserId,
                ["oldEmail"] = e.OldEmail,
                ["newEmail"] = e.NewEmail
            }),

            RoleAssignedEvent e => UserLifecycle(e, "auth.user.role_assigned.v1", new Dictionary<string, object?>
            {
                ["userId"] = e.UserId,
                ["roleId"] = e.RoleId
            }),

            RoleRemovedEvent e => UserLifecycle(e, "auth.user.role_removed.v1", new Dictionary<string, object?>
            {
                ["userId"] = e.UserId,
                ["roleId"] = e.RoleId
            }),

            AccountStatusChangedEvent e => UserLifecycle(e, "auth.user.status_changed.v1", new Dictionary<string, object?>
            {
                ["userId"] = e.UserId,
                ["username"] = e.UserName,
                ["transition"] = e.Transition.ToString().ToUpperInvariant()
            }),

            OAuthProviderLinkedEvent e => UserLifecycle(e, "auth.user.oauth_linked.v1", new Dictionary<string, object?>
            {
                ["userId"] = e.UserId,
                ["provider"] = e.Provider,
                ["providerUserId"] = e.ProviderUserId,
                ["newAccount"] = e.NewAccount
            }),

            TokenCreatedEvent e => Session(e, "auth.session.token_created.v1", new Dictionary<string, object?>
            {
                ["tokenId"] = e.TokenId,
                ["userId"] = e.UserId,
                ["family"] = e.Family,
                ["generation"] = e.Generation,
                ["expiresAt"] = e.ExpiresAt
            }),

            TokenRotatedEvent e => Session(e, "auth.session.token_rotated.v1", new Dictionary<string, object?>
            {
                ["userId"] = e.UserId,
                ["family"] = e.Family,
                ["oldTokenId"] = e.OldTokenId,
                ["newTokenId"] = e.NewTokenId,
                ["newGeneration"] = e.NewGeneration
            }),

            TokenRevokedEvent e => Session(e, "auth.session.token_revoked.v1", new Dictionary<string, object?>
            {
                ["tokenId"] = e.TokenId,
                ["userId"] = e.UserId,
                ["family"] = e.Family,
                ["reason"] = e.Reason
            }),

            _ => null
        };
    }

    private static RoutedEvent UserLifecycle(DomainEvent src, string type, Dictionary<string, object?> payload)
    {
        return new RoutedEvent(
            KafkaTopics.AuthUserEvents,
            src.AggregateId,
            new UserLifecycleIntegrationEvent(
                src.EventId,
                type,
                src.OccurredOn,
                src.AggregateId,
                SchemaV1,
                payload
            )
        );
    }

    private static RoutedEvent Session(DomainEvent src, string type, Dictionary<string, object?> payload)
    {
        return new RoutedEvent(
            KafkaTopics.AuthSessionEvents,
            src.AggregateId,
            new SessionIntegrationEvent(
                src.EventId,
                type,
                src.OccurredOn,
                src.AggregateId,
                SchemaV1,
                payload
            )
        );
    }
}
