using PantryPal.Core.Models;

namespace PantryPal.Core.Services.Abstractions;

public interface IUnitsService
{
    Task<List<Unit>> GetAllAsync();
    Task<int> CreateAsync(Unit unit);
    Task UpdateAsync(Unit unit);
    Task<bool> DeleteAsync(int id); // false => in use (blocked)
}
