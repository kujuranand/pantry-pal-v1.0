using System.Collections.ObjectModel;
using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;
using PantryPal.Mobile.Services;
using System.Globalization;

namespace PantryPal.Mobile.Views;

public partial class ListsPage : ContentPage
{
    private readonly IListsService _lists;
    private readonly IListItemsService _items;

    private readonly ObservableCollection<ListRow> _data = new();

    public ListsPage()
    {
        InitializeComponent();
        _lists = ServiceHelper.Get<IListsService>();
        _items = ServiceHelper.Get<IListItemsService>();
        ListsCollection.ItemsSource = _data;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private static string FormatDate(DateTime utc)
    {
        // Show dd/MMM/yy in device local time
        var local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        return local.ToString("dd/MMM/yy", CultureInfo.InvariantCulture);
    }

    private async Task RefreshAsync()
    {
        _data.Clear();

        var lists = await _lists.GetListsAsync();
        foreach (var l in lists)
        {
            // N+1 on purpose (small data; fine for prototype)
            var items = await _items.GetByListAsync(l.Id);
            var total = items.Sum(i => i.Cost);
            _data.Add(new ListRow
            {
                Id = l.Id,
                Name = l.Name,
                CreatedDisplay = $"Created: {FormatDate(l.CreatedUtc)}",
                TotalDisplay = $"Total: ${total:0.00}"
            });
        }
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

        await RefreshAsync();
        await Shell.Current.GoToAsync($"{nameof(ListDetailPage)}?ListId={id}");
    }

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0) return;
        var row = (ListRow)e.CurrentSelection[0];
        await Shell.Current.GoToAsync($"{nameof(ListDetailPage)}?ListId={row.Id}");
        ((CollectionView)sender).SelectedItem = null;
    }

    private async void OnOpenClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ListRow row)
            await Shell.Current.GoToAsync($"{nameof(ListDetailPage)}?ListId={row.Id}");
    }

    private async void OnRenameSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is ListRow row)
        {
            var newName = await DisplayPromptAsync("Rename List", "New name:", "Save", "Cancel", row.Name);
            if (string.IsNullOrWhiteSpace(newName)) return;
            await _lists.RenameAsync(row.Id, newName.Trim());
            await RefreshAsync();
        }
    }

    private async void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is ListRow row)
        {
            var ok = await DisplayAlert("Delete List", $"Delete '{row.Name}' and its items?", "Delete", "Cancel");
            if (!ok) return;
            await _lists.DeleteAsync(row.Id);
            await RefreshAsync();
        }
    }

    private sealed class ListRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string CreatedDisplay { get; set; } = "";
        public string TotalDisplay { get; set; } = "";
    }
}
