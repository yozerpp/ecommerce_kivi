using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Events;

namespace Ecommerce.Bl.Concrete;

public class StaffManager : IStaffManager
{
    private readonly IRepository<Staff> _staffRepository;
    private readonly IRepository<PermissionRequest> _permissionRequestRepository;
    public StaffManager(IRepository<Staff> staffRepository) {
        _staffRepository = staffRepository;
    }

    public Staff Create(Staff staff, Staff creator) {
        staff.ManagerId = creator.Id;
        staff.Manager = staff.ManagerId != 0 ? null! : creator;
        _staffRepository.Add(staff);
        return staff;
    }

    public PermissionRequest RequestPermission(PermissionRequest request) {
        return _permissionRequestRepository.Add(request);
    }

    public void AnswerPermissionRequest(PermissionRequest request) {
        _permissionRequestRepository.Update(request);
    }
}