namespace Example.Api.Events;

public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync(CakeCreatedEvent @event) => Task.CompletedTask;
}
