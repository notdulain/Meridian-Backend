using TrackingService.API.Models;

namespace TrackingService.API.Repositories;

public interface ITrackingRepository
{
    Task<LocationUpdate> LogLocationAsync(LocationUpdate locationUpdate);
    Task<IEnumerable<LocationUpdate>> GetHistoryAsync(int assignmentId);
    Task<LocationUpdate?> GetLastKnownLocationAsync(int driverId);
    Task EnsureDatabaseAsync();
}
