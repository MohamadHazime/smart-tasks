namespace SmartTasks.Api.Domain;

public abstract class Entity
{
    public int Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
}
