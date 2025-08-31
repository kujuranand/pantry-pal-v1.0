using SQLite;

namespace PantryPal.Core.Migrations;

internal interface IMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    Task UpAsync(SQLiteAsyncConnection conn);
}
