namespace Ecommerce.Entity;

public class Permission
{
    public static Permission EditOrder = new(){
        Id = "edit_order",
        Description = "Allows changing status of orders",
    };
    public static Permission EditProduct = new(){
        Id = "edit_product",
        Description = "Allows editing product details"
    };
    public static Permission EditUser = new(){
        Id = "edit_user",
        Description = "Allows editing user information"
    };
    public static Permission DeleteUser = new(){
        Id = "delete_user",
        Description = "Allows deleting user accounts"
    };
    public string Id { get; set; }

    public string Description { get; set; }

    protected bool Equals(Permission other) {
        return Id == other.Id;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) ||Id!=default&& obj is Permission other && Equals(other);
    }

    public override int GetHashCode() {
        return Id == default ? base.GetHashCode() : Id.GetHashCode();
    }
}