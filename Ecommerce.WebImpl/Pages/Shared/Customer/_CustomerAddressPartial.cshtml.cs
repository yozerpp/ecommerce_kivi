using Ecommerce.Entity.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce.WebImpl.Pages.CustomerPartials;

public class _CustomerAddressPartial 
{
    public IList<Address> Addresses { get; init; }
    public uint CustomerId { get; init; }
    private readonly bool _updatable;
    public required bool Updateable
    {
        get => Editable && _updatable;
        init => Editable = (_updatable = value) ? value : Editable;
    }

    public bool Editable { get; init; }
    
}