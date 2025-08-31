using PantryPal.Core.Data;
using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;

namespace PantryPal.Core.Services;

public sealed class UnitsService : IUnitsService
{
    private readonly PantryDatabase _db;
    public UnitsService(PantryDatabase db) => _db = db;

    public async Task<List<Unit>> GetAllAsync()
    {
        await _db.InitAsync();
        return await _db.Connection.Table<Unit>().OrderBy(u => u.Name).ToListAsync();
    }

    public async Task<int> CreateAsync(Unit unit)
    {
        await _db.InitAsync();
        if (string.IsNullOrWhiteSpace(unit.Name)) throw new ArgumentException("Unit.Name required");
        if (string.IsNullOrWhiteSpace(unit.Abbrev)) throw new ArgumentException("Unit.Abbrev required");
        return await _db.Connection.InsertAsync(unit);
    }

    public async Task UpdateAsync(Unit unit)
    {
        await _db.InitAsync();
        await _db.Connection.UpdateAsync(unit);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await _db.InitAsync();
        // Block delete if in use
        var count = await _db.Connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM GroceryListItems WHERE UnitId = ?;", id);
        if (count > 0) return false;

        await _db.Connection.ExecuteAsync("DELETE FROM Units WHERE Id = ?;", id);
        return true;
    }
}
