using PantryPal.Core.Models;

namespace PantryPal.Core.Services.Abstractions;

public interface IListItemsService
{
    Task<List<GroceryListItem>> GetByListAsync(int listId);
    Task<int> AddOrUpdateAsync(GroceryListItem item); // returns Id
    Task DeleteAsync(int id);
}
