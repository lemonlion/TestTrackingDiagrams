namespace Example.Api.Events;

public record CakeCreatedEvent(Guid BatchId, string[] Ingredients);
