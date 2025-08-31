using PantryPal.Core.Models;

namespace PantryPal.Core.Services.Abstractions;

public interface IListsService
{
    Task<List<GroceryList>> GetListsAsync();
    Task<GroceryList?> GetAsync(int id);
    Task<int> CreateAsync(GroceryList list);
    Task RenameAsync(int id, string newName);
    Task DeleteAsync(int id);
}
