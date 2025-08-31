using SQLite;

namespace PantryPal.Core.Migrations;

/// <summary>Helpful indexes for common queries.</summary>
internal sealed class _0002_AddIndexes : IMigration
{
    public int FromVersion => 1;
    public int ToVersion => 2;

    public async Task UpAsync(SQLiteAsyncConnection conn)
    {
        await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Items_ListId     ON GroceryListItems(ListId);");
        await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Items_CategoryId ON GroceryListItems(CategoryId);");
        await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Items_UnitId     ON GroceryListItems(UnitId);");
        await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Lists_CreatedUtc ON GroceryLists(CreatedUtc);");
    }
}
