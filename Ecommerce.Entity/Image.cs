using Ecommerce.Entity.Common.Meta;

namespace Ecommerce.Entity;

public class Image
{
    public uint Id { get; set; }
    [Image]
    public string Data { get; set; }
    public bool IsMain { get; set; }
    protected bool Equals(Image other) {
        return Id == other.Id;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || Id!=default&&obj is Image other && Equals(other);
    }

    public override int GetHashCode() {
        if (Id == default) return base.GetHashCode();
        return HashCode.Combine(Id);
    }
}