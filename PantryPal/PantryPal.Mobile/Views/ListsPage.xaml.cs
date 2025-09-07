using System.Collections.ObjectModel;
using System.Globalization;
using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;
using PantryPal.Mobile.Services;

namespace PantryPal.Mobile.Views;

public partial class ListsPage : ContentPage
{
    private readonly IListsService _lists;
    private readonly IListItemsService _items;

    // Backing data (all rows with raw values)
    private readonly List<ListRow> _all = new();

    // View data (filtered + sorted)
    private readonly ObservableCollection<ListRowView> _view = new();

    // UI state
    private string _searchText = string.Empty;
    private SortMode _sortMode = SortMode.Newest;

    // Simple debounce token for search
    private CancellationTokenSource? _searchCts;

    public ListsPage()
    {
        InitializeComponent();
        _lists = ServiceHelper.Get<IListsService>();
        _items = ServiceHelper.Get<IListItemsService>();
        ListsCollection.ItemsSource = _view;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAllAsync();
        RecomputeView();
    }

    // --- Events ---

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        // Debounce ~150ms to avoid recomputing on every keystroke
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
            catch (TaskCanceledException) { /* ignored */ }
        }, token);
    }

    private void OnSortCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return; // only act on checked
        _sortMode = (sender == SortNewest) ? SortMode.Newest : SortMode.Oldest;
        RecomputeView();
    }

    private async void OnNewListClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("New List", "List name:", "Create", "Cancel", "Weekly shop");
        if (string.IsNullOrWhiteSpace(name)) return;

        var id = await _lists.CreateAsync(new GroceryList
        {
            Name = name.Trim(),
            CreatedUtc = DateTime.UtcNow
        });

        await LoadAllAsync();
        RecomputeView();
        await Shell.Current.GoToAsync($"{nameof(ListDetailPage)}?ListId={id}");
    }

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0) return;
        var row = (ListRowView)e.CurrentSelection[0];
        await Shell.Current.GoToAsync($"{nameof(ListDetailPage)}?ListId={row.Id}");
        ((CollectionView)sender).SelectedItem = null;
    }

    private async void OnOpenClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ListRowView row)
            await Shell.Current.GoToAsync($"{nameof(ListDetailPage)}?ListId={row.Id}");
    }

    private async void OnRenameSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is ListRowView row)
        {
            var newName = await DisplayPromptAsync("Rename List", "New name:", "Save", "Cancel", row.Name);
            if (string.IsNullOrWhiteSpace(newName)) return;
            await _lists.RenameAsync(row.Id, newName.Trim());
            await LoadAllAsync();
            RecomputeView();
        }
    }

    private async void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is ListRowView row)
        {
            var ok = await DisplayAlert("Delete List", $"Delete '{row.Name}' and its items?", "Delete", "Cancel");
            if (!ok) return;
            await _lists.DeleteAsync(row.Id);
            await LoadAllAsync();
            RecomputeView();
        }
    }

    // --- Data loading & view composition ---

    private async Task LoadAllAsync()
    {
        _all.Clear();

        var lists = await _lists.GetListsAsync();
        foreach (var l in lists)
        {
            // Total cost for each list (small data set; N+1 is fine for prototype)
            var its = await _items.GetByListAsync(l.Id);
            var total = its.Sum(i => i.Cost);

            _all.Add(new ListRow
            {
                Id = l.Id,
                Name = l.Name,
                CreatedUtc = l.CreatedUtc,
                Total = total
            });
        }
    }

    private void RecomputeView()
    {
        IEnumerable<ListRow> q = _all;

        // Text filter (case-insensitive, by Name)
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            q = q.Where(r => r.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        // Sort
        q = _sortMode == SortMode.Newest
            ? q.OrderByDescending(r => r.CreatedUtc)
            : q.OrderBy(r => r.CreatedUtc);

        // Project to view rows (formatted)
        var projected = q.Select(r => new ListRowView
        {
            Id = r.Id,
            Name = r.Name,
            CreatedDisplay = $"Created: {FormatDate(r.CreatedUtc)}",
            TotalDisplay = $"Total: ${r.Total:0.00}"
        }).ToList();

        // Update observable collection
        _view.Clear();
        foreach (var v in projected) _view.Add(v);
    }

    private static string FormatDate(DateTime utc)
    {
        var local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        return local.ToString("dd/MMM/yy", CultureInfo.InvariantCulture);
    }

    // --- DTOs / state ---

    private enum SortMode { Newest, Oldest }

    private sealed class ListRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedUtc { get; set; }
        public decimal Total { get; set; }
    }

    private sealed class ListRowView
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string CreatedDisplay { get; set; } = "";
        public string TotalDisplay { get; set; } = "";
    }
}
