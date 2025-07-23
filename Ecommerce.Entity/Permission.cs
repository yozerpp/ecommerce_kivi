namespace Ecommerce.Entity;

public class Permission
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    protected bool Equals(Permission other) {
        return Id == other.Id;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) ||Id!=default&& obj is Permission other && Equals(other);
    }

    public override int GetHashCode() {
        if (Id == default) return base.GetHashCode();
        return (int)Id;
    }
}