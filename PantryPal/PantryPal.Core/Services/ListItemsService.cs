using PantryPal.Core.Data;
using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;

namespace PantryPal.Core.Services;

public sealed class ListItemsService : IListItemsService
{
    private readonly PantryDatabase _db;
    public ListItemsService(PantryDatabase db) => _db = db;

    public async Task<List<GroceryListItem>> GetByListAsync(int listId)
    {
        await _db.InitAsync();
        return await _db.Connection.Table<GroceryListItem>()
                                   .Where(i => i.ListId == listId)
                                   .OrderBy(i => i.Id)
                                   .ToListAsync();
    }

    public async Task<int> AddOrUpdateAsync(GroceryListItem item)
    {
        await _db.InitAsync();
        if (string.IsNullOrWhiteSpace(item.Name)) throw new ArgumentException("Item.Name required");
        if (item.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(item.Quantity));
        if (item.Cost < 0) throw new ArgumentOutOfRangeException(nameof(item.Cost));

        if (item.Id == 0)
            return await _db.Connection.InsertAsync(item);
        await _db.Connection.UpdateAsync(item);
        return item.Id;
    }

    public async Task DeleteAsync(int id)
    {
        await _db.InitAsync();
        await _db.Connection.ExecuteAsync("DELETE FROM GroceryListItems WHERE Id = ?;", id);
    }
}
