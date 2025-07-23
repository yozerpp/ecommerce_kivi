namespace Ecommerce.Entity;

public class Role
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public List<string> Permissions { get; set; }
}