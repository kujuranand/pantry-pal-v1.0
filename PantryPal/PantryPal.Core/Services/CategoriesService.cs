using PantryPal.Core.Data;
using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;

namespace PantryPal.Core.Services;

public sealed class CategoriesService : ICategoriesService
{
    private readonly PantryDatabase _db;
    public CategoriesService(PantryDatabase db) => _db = db;

    public async Task<List<Category>> GetAllAsync()
    {
        await _db.InitAsync();
        return await _db.Connection.Table<Category>().OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<int> CreateAsync(Category category)
    {
        await _db.InitAsync();
        if (string.IsNullOrWhiteSpace(category.Name)) throw new ArgumentException("Category.Name required");
        return await _db.Connection.InsertAsync(category);
    }

    public async Task UpdateAsync(Category category)
    {
        await _db.InitAsync();
        await _db.Connection.UpdateAsync(category);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await _db.InitAsync();
        var count = await _db.Connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM GroceryListItems WHERE CategoryId = ?;", id);
        if (count > 0) return false;

        await _db.Connection.ExecuteAsync("DELETE FROM Categories WHERE Id = ?;", id);
        return true;
    }
}
