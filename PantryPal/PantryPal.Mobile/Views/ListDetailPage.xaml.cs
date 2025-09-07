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

    // Lookups for display + filter source
    private List<Category> _catList = new();
    private Dictionary<int, string> _catNames = new();
    private Dictionary<int, string> _unitAbbrevs = new();

    // Backing data (raw)
    private readonly List<ItemRow> _all = new();

    // View data (filtered)
    private readonly ObservableCollection<ItemRow> _view = new();

    // UI state
    private string _searchText = string.Empty;
    private int? _filterCategoryId = null;

    // Debounce for search
    private CancellationTokenSource? _searchCts;

    public ListDetailPage()
    {
        InitializeComponent();
        _lists = ServiceHelper.Get<IListsService>();
        _items = ServiceHelper.Get<IListItemsService>();
        _cats = ServiceHelper.Get<ICategoriesService>();
        _units = ServiceHelper.Get<IUnitsService>();
        ItemsCollection.ItemsSource = _view;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLookupsAsync();
        await LoadAllItemsAsync();
        await LoadListHeaderAsync();
        RecomputeView();
        PopulateCategoryFilter();
    }

    // --- Loaders ---

    private async Task LoadLookupsAsync()
    {
        _catList = await _cats.GetAllAsync();
        _catNames = _catList.ToDictionary(c => c.Id, c => c.Name);
        _unitAbbrevs = (await _units.GetAllAsync()).ToDictionary(u => u.Id, u => u.Abbrev);
    }

    private async Task LoadListHeaderAsync()
    {
        var list = await _lists.GetAsync(ListId);
        if (list is null)
        {
            await DisplayAlert("Missing", "List not found.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }
        TitleLabel.Text = list.Name;
    }

    private async Task LoadAllItemsAsync()
    {
        _all.Clear();
        var items = await _items.GetByListAsync(ListId);

        foreach (var it in items)
        {
            _all.Add(new ItemRow
            {
                Id = it.Id,
                Name = it.Name,
                Brand = it.Brand ?? "",
                CategoryId = it.CategoryId,
                CategoryName = (it.CategoryId.HasValue && _catNames.TryGetValue(it.CategoryId.Value, out var cn)) ? cn : "-",
                Quantity = it.Quantity,
                UnitAbbrev = _unitAbbrevs.TryGetValue(it.UnitId, out var ua) ? ua : "?",
                Cost = it.Cost,
                PurchasedDate = it.PurchasedDate,
                ExpiryDate = it.ExpiryDate,
                DatesDisplay = (it.PurchasedDate.HasValue || it.ExpiryDate.HasValue)
                               ? $"{D(it.PurchasedDate)} -> {D(it.ExpiryDate)}"
                               : "-"
            });
        }
    }

    private void PopulateCategoryFilter()
    {
        // Build picker items: All + categories
        var items = new List<string> { "All categories" };
        items.AddRange(_catList.Select(c => c.Name));

        CategoryFilterPicker.ItemsSource = items;

        // Keep current selection if possible; otherwise default to "All"
        if (_filterCategoryId is int id)
        {
            var idx = _catList.FindIndex(c => c.Id == id);
            CategoryFilterPicker.SelectedIndex = (idx >= 0) ? idx + 1 : 0;
        }
        else
        {
            CategoryFilterPicker.SelectedIndex = 0;
        }
    }

    // --- Formatting helpers ---

    private static string D(DateTime? utc)
    {
        if (utc is null) return "-";
        var local = DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc).ToLocalTime();
        return local.ToString("dd/MMM/yy", CultureInfo.InvariantCulture);
    }

    private static string Money(decimal v) => $"${v:0.00}";

    // --- View recompute pipeline ---

    private void RecomputeView()
    {
        IEnumerable<ItemRow> q = _all;

        // Text filter: Name + Brand (case-insensitive Contains)
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            q = q.Where(r =>
                r.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                r.Brand.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        // Category filter
        if (_filterCategoryId is int catId)
        {
            q = q.Where(r => r.CategoryId == catId);
        }

        var result = q.ToList();

        // Update list (clear + re-add is fine for prototype)
        _view.Clear();
        foreach (var row in result) _view.Add(row);

        // Totals reflect filtered view
        var total = result.Sum(r => r.Cost);
        TotalLabel.Text = Money(total);
        BottomTotalLabel.Text = Money(total);
    }

    // --- Events ---

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, token);
                if (token.IsCancellationRequested) return;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _searchText = e.NewTextValue?.Trim() ?? string.Empty;
                    RecomputeView();
                });
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    private void OnCategoryFilterChanged(object? sender, EventArgs e)
    {
        if (CategoryFilterPicker.SelectedIndex <= 0)
        {
            _filterCategoryId = null; // "All"
        }
        else
        {
            // subtract 1 because index 0 is "All"
            var idx = CategoryFilterPicker.SelectedIndex - 1;
            if (idx >= 0 && idx < _catList.Count)
                _filterCategoryId = _catList[idx].Id;
        }
        RecomputeView();
    }

    private async void OnRenameClicked(object sender, EventArgs e)
    {
        var current = TitleLabel.Text ?? "";
        var name = await DisplayPromptAsync("Rename List", "New name:", "Save", "Cancel", current);
        if (string.IsNullOrWhiteSpace(name)) return;
        await _lists.RenameAsync(ListId, name.Trim());
        await LoadListHeaderAsync();
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
            await LoadAllItemsAsync();
            RecomputeView();
        }
    }

    // --- ItemRow used for both backing & view (contains fields for filters + display) ---

    private sealed class ItemRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Brand { get; set; } = "";

        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "-";

        public decimal Quantity { get; set; }
        public string UnitAbbrev { get; set; } = "";

        public decimal Cost { get; set; }

        public DateTime? PurchasedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public string DatesDisplay { get; set; } = "";
    }
}
