using PantryPal.Core.Data;
using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;

namespace PantryPal.Core.Services;

public sealed class ListsService : IListsService
{
    private readonly PantryDatabase _db;
    public ListsService(PantryDatabase db) => _db = db;

    public async Task<List<GroceryList>> GetListsAsync()
    {
        await _db.InitAsync();
        return await _db.Connection.QueryAsync<GroceryList>(
            "SELECT * FROM GroceryLists ORDER BY datetime(CreatedUtc) DESC;");
    }

    public async Task<GroceryList?> GetAsync(int id)
    {
        await _db.InitAsync();
        return await _db.Connection.Table<GroceryList>().Where(l => l.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> CreateAsync(GroceryList list)
    {
        await _db.InitAsync();
        if (string.IsNullOrWhiteSpace(list.Name)) throw new ArgumentException("List.Name required");
        if (list.CreatedUtc == default) list.CreatedUtc = DateTime.UtcNow;
        return await _db.Connection.InsertAsync(list);
    }

    public async Task RenameAsync(int id, string newName)
    {
        await _db.InitAsync();
        await _db.Connection.ExecuteAsync("UPDATE GroceryLists SET Name = ? WHERE Id = ?;", newName, id);
    }

    public async Task DeleteAsync(int id)
    {
        await _db.InitAsync();
        // Items will cascade delete because of FK
        await _db.Connection.ExecuteAsync("DELETE FROM GroceryLists WHERE Id = ?;", id);
    }
}
