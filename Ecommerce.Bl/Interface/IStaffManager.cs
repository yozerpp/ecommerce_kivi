using Ecommerce.Entity;
using Ecommerce.Entity.Events;

namespace Ecommerce.Bl.Interface;

public interface IStaffManager
{
    public Staff Create(Staff staff, Staff creator);
    public PermissionRequest RequestPermission(PermissionRequest request);
    public void AnswerPermissionRequest(PermissionRequest request);
}