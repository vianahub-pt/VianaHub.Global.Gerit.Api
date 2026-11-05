namespace VianaHub.Global.Gerit.Domain.Base;

public class Entity : IEquatable<Entity>
{
    public int Id { get; protected set; }
    public int CreatedBy { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public int? ModifiedBy { get; protected set; }
    public DateTime? ModifiedAt { get; protected set; }

    protected Entity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    protected Entity(int id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }


    // Equality baseada no Id
    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Entity);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }

    // Método para verificar se a entidade é transiente (ainda não foi persistida)
    public bool IsTransient()
    {
        return Id == 0;
    }
}
