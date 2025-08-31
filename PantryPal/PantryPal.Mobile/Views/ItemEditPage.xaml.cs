using System.Globalization;
using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;
using PantryPal.Mobile.Services;

namespace PantryPal.Mobile.Views;

[QueryProperty(nameof(ListId), nameof(ListId))]
[QueryProperty(nameof(ItemId), nameof(ItemId))]
public partial class ItemEditPage : ContentPage
{
    public int ListId { get; set; }
    public int? ItemId { get; set; }

    private readonly IListItemsService _items;
    private readonly ICategoriesService _cats;
    private readonly IUnitsService _units;

    private List<Category> _catList = new();
    private List<Unit> _unitList = new();
    private GroceryListItem? _editing;

    public ItemEditPage()
    {
        InitializeComponent();
        _items = ServiceHelper.Get<IListItemsService>();
        _cats = ServiceHelper.Get<ICategoriesService>();
        _units = ServiceHelper.Get<IUnitsService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _catList = await _cats.GetAllAsync();
        _unitList = await _units.GetAllAsync();

        CategoryPicker.ItemsSource = _catList.Select(c => c.Name).Prepend("Uncategorised").ToList();
        UnitPicker.ItemsSource = _unitList.Select(u => u.Abbrev).ToList();

        // Defaults
        if (UnitPicker.ItemsSource is IList<string> u && u.Count > 0 && UnitPicker.SelectedIndex < 0)
            UnitPicker.SelectedIndex = 0;

        PurchasedPicker.Date = DateTime.Now.Date;
        ExpiryPicker.Date = DateTime.Now.Date.AddDays(7);

        if (ItemId is int id)
        {
            // Editing existing
            var listItems = await _items.GetByListAsync(ListId);
            _editing = listItems.FirstOrDefault(i => i.Id == id);
            if (_editing is null)
            {
                await DisplayAlert("Not found", "Item not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            Title = "Edit Item";
            NameEntry.Text = _editing.Name;
            BrandEntry.Text = _editing.Brand;

            // Category
            var catIndex = 0; // 0 is Uncategorised
            if (_editing.CategoryId.HasValue)
            {
                var idx = _catList.FindIndex(c => c.Id == _editing.CategoryId.Value);
                if (idx >= 0) catIndex = idx + 1; // +1 because of "Uncategorised" at index 0
            }
            CategoryPicker.SelectedIndex = catIndex;

            // Unit
            var unitIndex = _unitList.FindIndex(u => u.Id == _editing.UnitId);
            if (unitIndex >= 0) UnitPicker.SelectedIndex = unitIndex;

            QtyEntry.Text = _editing.Quantity.ToString(CultureInfo.InvariantCulture);
            CostEntry.Text = _editing.Cost.ToString("0.##", CultureInfo.InvariantCulture);

            if (_editing.PurchasedDate.HasValue)
            {
                PurchasedCheck.IsChecked = true;
                PurchasedPicker.Date = DateTime.SpecifyKind(_editing.PurchasedDate.Value, DateTimeKind.Utc).ToLocalTime().Date;
            }
            else
            {
                PurchasedCheck.IsChecked = false;
            }

            if (_editing.ExpiryDate.HasValue)
            {
                ExpiryCheck.IsChecked = true;
                ExpiryPicker.Date = DateTime.SpecifyKind(_editing.ExpiryDate.Value, DateTimeKind.Utc).ToLocalTime().Date;
            }
            else
            {
                ExpiryCheck.IsChecked = false;
            }

            NotesEditor.Text = _editing.Notes;
        }
        else
        {
            Title = "Add Item";
            PurchasedCheck.IsChecked = true;
            ExpiryCheck.IsChecked = false;
        }
    }

    private async void OnCancel(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSave(object sender, EventArgs e)
    {
        // Validate
        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Required", "Name is required.", "OK");
            return;
        }

        if (!decimal.TryParse(QtyEntry.Text?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var qty) || qty <= 0)
        {
            await DisplayAlert("Quantity", "Enter a quantity > 0 (e.g., 1 or 1.5).", "OK");
            return;
        }

        if (!decimal.TryParse(CostEntry.Text?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost < 0)
        {
            await DisplayAlert("Cost", "Enter a cost ≥ 0 (e.g., 3.80).", "OK");
            return;
        }

        if (UnitPicker.SelectedIndex < 0)
        {
            await DisplayAlert("Unit", "Please choose a unit.", "OK");
            return;
        }

        int? categoryId = null;
        if (CategoryPicker.SelectedIndex > 0) // index 0 is "Uncategorised"
            categoryId = _catList[CategoryPicker.SelectedIndex - 1].Id;

        var unitId = _unitList[UnitPicker.SelectedIndex].Id;

        DateTime? purchasedUtc = null;
        DateTime? expiryUtc = null;

        if (PurchasedCheck.IsChecked)
            purchasedUtc = DateTime.SpecifyKind(PurchasedPicker.Date, DateTimeKind.Local).ToUniversalTime();

        if (ExpiryCheck.IsChecked)
            expiryUtc = DateTime.SpecifyKind(ExpiryPicker.Date, DateTimeKind.Local).ToUniversalTime();

        if (purchasedUtc.HasValue && expiryUtc.HasValue && expiryUtc.Value < purchasedUtc.Value)
        {
            await DisplayAlert("Dates", "Expiry must be on or after purchased date.", "OK");
            return;
        }

        var item = _editing ?? new GroceryListItem { ListId = ListId };
        item.Name = name;
        item.Brand = string.IsNullOrWhiteSpace(BrandEntry.Text) ? null : BrandEntry.Text.Trim();
        item.CategoryId = categoryId;
        item.Quantity = qty;
        item.UnitId = unitId;
        item.Cost = cost;
        item.PurchasedDate = purchasedUtc;
        item.ExpiryDate = expiryUtc;
        item.Notes = string.IsNullOrWhiteSpace(NotesEditor.Text) ? null : NotesEditor.Text.Trim();

        await _items.AddOrUpdateAsync(item);
        await Shell.Current.GoToAsync("..");
    }
}
