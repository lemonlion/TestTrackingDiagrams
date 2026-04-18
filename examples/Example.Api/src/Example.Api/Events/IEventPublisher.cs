namespace Example.Api.Events;

public interface IEventPublisher
{
    Task PublishAsync(CakeCreatedEvent @event);
}
