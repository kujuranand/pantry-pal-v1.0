using Microsoft.Extensions.Logging;
using PantryPal.Core.Data;
using PantryPal.Core.Services;
using PantryPal.Core.Services.Abstractions;
using SQLitePCL;
using PantryPal.Mobile.Services;

namespace PantryPal.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Ensure native SQLite is wired
        Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register DB + services
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "pantrypal.db3");
        builder.Services.AddSingleton(new PantryDatabase(dbPath));
        builder.Services.AddSingleton<IUnitsService, UnitsService>();
        builder.Services.AddSingleton<ICategoriesService, CategoriesService>();
        builder.Services.AddSingleton<IListsService, ListsService>();
        builder.Services.AddSingleton<IListItemsService, ListItemsService>();

        var app = builder.Build();

        // Initialize global service accessor HERE (correct place)
        ServiceHelper.Initialize(app.Services);

        // One-time DB init at startup
        var db = app.Services.GetRequiredService<PantryDatabase>();
        Task.Run(async () => await db.InitAsync()).Wait();

        return app;
    }
}
