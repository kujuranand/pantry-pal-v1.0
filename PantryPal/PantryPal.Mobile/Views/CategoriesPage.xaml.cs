using System.Collections.ObjectModel;
using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;
using PantryPal.Mobile.Services;

namespace PantryPal.Mobile.Views;

public partial class CategoriesPage : ContentPage
{
    private readonly ICategoriesService _categories;
    private readonly ObservableCollection<Category> _data = new();

    public CategoriesPage()
    {
        InitializeComponent();
        _categories = ServiceHelper.Get<ICategoriesService>();
        CategoriesCollection.ItemsSource = _data;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _data.Clear();
        var rows = await _categories.GetAllAsync();
        foreach (var r in rows) _data.Add(r);
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("New Category", "Name:", "Save", "Cancel");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            await _categories.CreateAsync(new Category { Name = name.Trim() });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // Buttons
    private async void OnEditButton(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Category c)
            await EditCategoryAsync(c);
    }

    private async void OnDeleteButton(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Category c)
            await DeleteCategoryAsync(c);
    }

    // Swipe
    private async void OnEditSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is Category c)
            await EditCategoryAsync(c);
    }

    private async void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is Category c)
            await DeleteCategoryAsync(c);
    }

    // Helpers
    private async Task EditCategoryAsync(Category c)
    {
        var name = await DisplayPromptAsync("Edit Category", "Name:", "Save", "Cancel", initialValue: c.Name);
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            c.Name = name.Trim();
            await _categories.UpdateAsync(c);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task DeleteCategoryAsync(Category c)
    {
        var confirm = await DisplayAlert("Delete Category", $"Delete '{c.Name}'?", "Delete", "Cancel");
        if (!confirm) return;

        var ok = await _categories.DeleteAsync(c.Id);
        if (!ok)
        {
            await DisplayAlert("In Use", "This category is used by one or more items. Reassign or delete those items first.", "OK");
            return;
        }
        await RefreshAsync();
    }
}
