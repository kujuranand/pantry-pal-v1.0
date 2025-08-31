using SQLite;
using PantryPal.Core.Models;

namespace PantryPal.Core.Migrations;

/// <summary>Initial schema: v1 tables with FK behavior.</summary>
internal sealed class _0001_Initial : IMigration
{
    public int FromVersion => 0;
    public int ToVersion => 1;

    public async Task UpAsync(SQLiteAsyncConnection conn)
    {
        // Create tables with FKs & basics (use IF NOT EXISTS for idempotence)
        await conn.ExecuteAsync(@"
CREATE TABLE IF NOT EXISTS GroceryLists (
  Id          INTEGER PRIMARY KEY AUTOINCREMENT,
  Name        TEXT    NOT NULL,
  CreatedUtc  TEXT    NOT NULL,
  Notes       TEXT
);");

        await conn.ExecuteAsync(@"
CREATE TABLE IF NOT EXISTS Categories (
  Id    INTEGER PRIMARY KEY AUTOINCREMENT,
  Name  TEXT    NOT NULL
);");

        await conn.ExecuteAsync(@"
CREATE TABLE IF NOT EXISTS Units (
  Id      INTEGER PRIMARY KEY AUTOINCREMENT,
  Name    TEXT    NOT NULL,
  Abbrev  TEXT    NOT NULL
);");

        await conn.ExecuteAsync(@"
CREATE TABLE IF NOT EXISTS GroceryListItems (
  Id            INTEGER PRIMARY KEY AUTOINCREMENT,
  ListId        INTEGER NOT NULL,
  Name          TEXT    NOT NULL,
  Brand         TEXT,
  CategoryId    INTEGER,
  Quantity      NUMERIC NOT NULL,
  UnitId        INTEGER NOT NULL,
  Cost          NUMERIC NOT NULL,
  PurchasedDate TEXT,
  ExpiryDate    TEXT,
  Notes         TEXT,
  FOREIGN KEY(ListId)     REFERENCES GroceryLists(Id) ON DELETE CASCADE,
  FOREIGN KEY(CategoryId) REFERENCES Categories(Id)   ON DELETE SET NULL,
  FOREIGN KEY(UnitId)     REFERENCES Units(Id)        ON DELETE RESTRICT
);");

        // Also ensure sqlite-net metadata matches (safe to call)
        await conn.CreateTableAsync<GroceryList>();
        await conn.CreateTableAsync<Category>();
        await conn.CreateTableAsync<Unit>();
        await conn.CreateTableAsync<GroceryListItem>();
    }
}
