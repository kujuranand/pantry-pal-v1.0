using PantryPal.Core.Models;

namespace PantryPal.Core.Services.Abstractions;

public interface ICategoriesService
{
    Task<List<Category>> GetAllAsync();
    Task<int> CreateAsync(Category category);
    Task UpdateAsync(Category category);
    Task<bool> DeleteAsync(int id); // false => in use (blocked)
}
