namespace Ecommerce.Entity.Common;

public class Dimensions : IEquatable<Dimensions>
{
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal Depth { get; set; }
    public decimal Weight { get; set; }

    public bool Equals(Dimensions? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Width.Equals(other.Width) && Height.Equals(other.Height) && Depth.Equals(other.Depth) && Weight.Equals(other.Weight);
    }

    public override string ToString() {
        return $"Weight: {Weight}kg, Dimensions: {Width}x{Height}x{Depth}cm";
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != typeof(Dimensions)) return false;
        return Equals((Dimensions)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Width, Height, Depth, Weight);
    }

    public static bool operator ==(Dimensions? left, Dimensions? right) {
        return Equals(left, right);
    }

    public static bool operator !=(Dimensions? left, Dimensions? right) {
        return !Equals(left, right);
    }
}