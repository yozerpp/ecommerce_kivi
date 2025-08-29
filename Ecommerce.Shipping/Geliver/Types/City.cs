namespace Ecommerce.Shipping.Geliver.Types;

public class City : IEquatable<City>
{
    public bool Equals(City? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return CityCode == other.CityCode;
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((City)obj);
    }

    public override int GetHashCode() {
        return CityCode.GetHashCode();
    }

    public static bool operator ==(City? left, City? right) {
        return Equals(left, right);
    }

    public static bool operator !=(City? left, City? right) {
        return !Equals(left, right);
    }

    public string Name { get; set; }
    public string CountryCode { get; set; }
    public string CityCode { get; set; }
}

public class District : IEquatable<District>
{
    public bool Equals(District? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return DistrictId == other.DistrictId;
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((District)obj);
    }

    public override int GetHashCode() {
        return DistrictId.GetHashCode();
    }

    public static bool operator ==(District? left, District? right) {
        return Equals(left, right);
    }

    public static bool operator !=(District? left, District? right) {
        return !Equals(left, right);
    }

    public string Name { get; set; }
    public long DistrictId { get; set; }
    public string CityCode { get; set; }
    public string CountryCode { get; set; }
}