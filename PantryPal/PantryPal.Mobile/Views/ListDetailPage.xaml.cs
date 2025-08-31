using System.Collections.ObjectModel;
using System.Globalization;
using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;
using PantryPal.Mobile.Services;

namespace PantryPal.Mobile.Views;

[QueryProperty(nameof(ListId), nameof(ListId))]
public partial class ListDetailPage : ContentPage
{
    public int ListId { get; set; }

    private readonly IListsService _lists;
    private readonly IListItemsService _items;
    private readonly ICategoriesService _cats;
    private readonly IUnitsService _units;

    private readonly ObservableCollection<ItemRow> _data = new();
    private Dictionary<int, string> _catNames = new();
    private Dictionary<int, string> _unitAbbrevs = new();

    public ListDetailPage()
    {
        InitializeComponent();
        _lists = ServiceHelper.Get<IListsService>();
        _items = ServiceHelper.Get<IListItemsService>();
        _cats = ServiceHelper.Get<ICategoriesService>();
        _units = ServiceHelper.Get<IUnitsService>();
        ItemsCollection.ItemsSource = _data;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLookupsAsync();
        await RefreshAsync();
    }

    private async Task LoadLookupsAsync()
    {
        _catNames = (await _cats.GetAllAsync()).ToDictionary(c => c.Id, c => c.Name);
        _unitAbbrevs = (await _units.GetAllAsync()).ToDictionary(u => u.Id, u => u.Abbrev);
    }

    private static string D(DateTime? utc)
    {
        if (utc is null) return "-";
        var local = DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToLocalTime();
        return local.ToString("dd/MMM/yy", CultureInfo.InvariantCulture);
    }

    private async Task RefreshAsync()
    {
        var list = await _lists.GetAsync(ListId);
        if (list is null)
        {
            await DisplayAlert("Missing", "List not found.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        TitleLabel.Text = list.Name;

        _data.Clear();
        var items = await _items.GetByListAsync(ListId);
        foreach (var it in items)
        {
            _data.Add(new ItemRow
            {
                Id = it.Id,
                Name = it.Name,
                Brand = it.Brand ?? "",
                CategoryName = (it.CategoryId.HasValue && _catNames.TryGetValue(it.CategoryId.Value, out var cn)) ? cn : "-",
                Quantity = it.Quantity,
                UnitAbbrev = _unitAbbrevs.TryGetValue(it.UnitId, out var ua) ? ua : "?",
                Cost = it.Cost,
                DatesDisplay = (it.PurchasedDate.HasValue || it.ExpiryDate.HasValue)
                               ? $"{D(it.PurchasedDate)} -> {D(it.ExpiryDate)}"
                               : "-"
            });
        }

        var total = items.Sum(i => i.Cost);
        TotalLabel.Text = $"${total:0.00}";
        BottomTotalLabel.Text = $"${total:0.00}";
    }

    private async void OnRenameClicked(object sender, EventArgs e)
    {
        var current = TitleLabel.Text ?? "";
        var name = await DisplayPromptAsync("Rename List", "New name:", "Save", "Cancel", current);
        if (string.IsNullOrWhiteSpace(name)) return;
        await _lists.RenameAsync(ListId, name.Trim());
        await RefreshAsync();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"{nameof(ItemEditPage)}?ListId={ListId}");
    }

    private async void OnEditSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is ItemRow row)
            await Shell.Current.GoToAsync($"{nameof(ItemEditPage)}?ListId={ListId}&ItemId={row.Id}");
    }

    private async void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is ItemRow row)
        {
            var ok = await DisplayAlert("Delete Item", $"Delete '{row.Name}'?", "Delete", "Cancel");
            if (!ok) return;
            await _items.DeleteAsync(row.Id);
            await RefreshAsync();
        }
    }

    private sealed class ItemRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Brand { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public decimal Quantity { get; set; }
        public string UnitAbbrev { get; set; } = "";
        public decimal Cost { get; set; }
        public string DatesDisplay { get; set; } = "";
    }
}
