using AssignmentService.API.Models;

namespace AssignmentService.API.Repositories;

public interface IAssignmentRepository
{
    Task<Assignment> CreateAsync(Assignment assignment);
    Task<Assignment?> GetByIdAsync(int id);
    Task<(IEnumerable<Assignment> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<Assignment?> GetByDeliveryIdAsync(int deliveryId);
    Task<Assignment?> GetActiveByDriverIdAsync(int driverId);
    Task<bool> UpdateStatusAsync(int id, string status);
}
