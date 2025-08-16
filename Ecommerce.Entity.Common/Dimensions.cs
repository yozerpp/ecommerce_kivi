using static System.Decimal;

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
    public static Dimensions Parse(string str) {
        var parts = str.Split('x');
        var props = typeof(Dimensions).GetProperties();
        int i = 0;
        var ret = new Dimensions();
        foreach (var part in parts){
            TryParse(part, out var dec);
            props[i++].SetValue(ret,dec);
        }
        return ret;
    }
    public string ToString(bool sizeOnly = false) {
        return $"{Width}x{Height}x{Depth}";
    }
    public override string ToString() {
        return $"{Weight}kg, {Width}x{Height}x{Depth}cm^3";
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