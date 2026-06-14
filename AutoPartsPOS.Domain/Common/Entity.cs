namespace AutoPartsPOS.Domain.Common;

public abstract class Entity : IEntity
{
    public long Id { get; protected set; }
}
