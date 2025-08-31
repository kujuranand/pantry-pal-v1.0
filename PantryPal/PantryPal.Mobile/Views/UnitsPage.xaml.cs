using System.Collections.ObjectModel;
using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;
using PantryPal.Mobile.Services;

namespace PantryPal.Mobile.Views;

public partial class UnitsPage : ContentPage
{
    private readonly IUnitsService _units;
    private readonly ObservableCollection<Unit> _data = new();

    public UnitsPage()
    {
        InitializeComponent();
        _units = ServiceHelper.Get<IUnitsService>();
        UnitsCollection.ItemsSource = _data;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _data.Clear();
        var rows = await _units.GetAllAsync();
        foreach (var r in rows) _data.Add(r);
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("New Unit", "Name (e.g., Kilogram):", "Save", "Cancel");
        if (string.IsNullOrWhiteSpace(name)) return;

        var abbrev = await DisplayPromptAsync("New Unit", "Abbreviation (e.g., kg):", "Save", "Cancel");
        if (string.IsNullOrWhiteSpace(abbrev)) return;

        try
        {
            await _units.CreateAsync(new Unit { Name = name.Trim(), Abbrev = abbrev.Trim() });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // ---- Edit / Delete from buttons ----
    private async void OnEditButton(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Unit u)
            await EditUnitAsync(u);
    }

    private async void OnDeleteButton(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Unit u)
            await DeleteUnitAsync(u);
    }

    // ---- Edit / Delete from swipe ----
    private async void OnEditSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is Unit u)
            await EditUnitAsync(u);
    }

    private async void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is Unit u)
            await DeleteUnitAsync(u);
    }

    // ---- Helpers ----
    private async Task EditUnitAsync(Unit u)
    {
        var name = await DisplayPromptAsync("Edit Unit", "Name:", "Save", "Cancel", initialValue: u.Name);
        if (string.IsNullOrWhiteSpace(name)) return;

        var abbrev = await DisplayPromptAsync("Edit Unit", "Abbreviation:", "Save", "Cancel", initialValue: u.Abbrev);
        if (string.IsNullOrWhiteSpace(abbrev)) return;

        try
        {
            u.Name = name.Trim();
            u.Abbrev = abbrev.Trim();
            await _units.UpdateAsync(u);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task DeleteUnitAsync(Unit u)
    {
        var confirm = await DisplayAlert("Delete Unit", $"Delete '{u.Name}' ({u.Abbrev})?", "Delete", "Cancel");
        if (!confirm) return;

        var ok = await _units.DeleteAsync(u.Id);
        if (!ok)
        {
            await DisplayAlert("In Use", "This unit is used by one or more items. Remove those items or change their unit before deleting.", "OK");
            return;
        }
        await RefreshAsync();
    }
}
