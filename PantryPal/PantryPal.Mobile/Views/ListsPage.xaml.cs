using PantryPal.Core.Models;
using PantryPal.Core.Services.Abstractions;
using PantryPal.Mobile.Services;

namespace PantryPal.Mobile.Views;

public partial class ListsPage : ContentPage
{
    public ListsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var lists = ServiceHelper.Get<IListsService>();
        var items = ServiceHelper.Get<IListItemsService>();
        var units = ServiceHelper.Get<IUnitsService>();
        var cats = ServiceHelper.Get<ICategoriesService>();

        // seed units/categories if empty
        if (!(await units.GetAllAsync()).Any())
        {
            await units.CreateAsync(new Unit { Name = "Each", Abbrev = "ea" });
            await units.CreateAsync(new Unit { Name = "Kilogram", Abbrev = "kg" });
            await units.CreateAsync(new Unit { Name = "Litre", Abbrev = "L" });
        }
        if (!(await cats.GetAllAsync()).Any())
        {
            await cats.CreateAsync(new Category { Name = "Dairy" });
            await cats.CreateAsync(new Category { Name = "Fruit" });
            await cats.CreateAsync(new Category { Name = "Vegetables" });
        }

        // create a demo list if none
        var all = await lists.GetListsAsync();
        int listId;
        if (all.Count == 0)
        {
            listId = await lists.CreateAsync(new GroceryList
            {
                Name = "Demo List",
                CreatedUtc = DateTime.UtcNow
            });

            var ea = (await units.GetAllAsync()).First(u => u.Abbrev == "ea");
            var dairy = (await cats.GetAllAsync()).First(c => c.Name == "Dairy");

            await items.AddOrUpdateAsync(new GroceryListItem
            {
                ListId = listId,
                Name = "Milk 2L",
                Brand = "DairyBest",
                CategoryId = dairy.Id,
                Quantity = 1,
                UnitId = ea.Id,
                Cost = 3.80m,
                PurchasedDate = DateTime.UtcNow.Date
            });
        }
        else listId = all[0].Id;

        await DisplayAlert("DB OK", $"Seed ready. Lists: {(await lists.GetListsAsync()).Count}\nFirst list id: {listId}", "OK");
    }
}
