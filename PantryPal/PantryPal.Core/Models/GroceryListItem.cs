using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace PantryPal.Core.Models;

/// <summary>
/// a grocery item in a grocery list.
/// </summary>

[Table("GroceryListItems")]
public class GroceryListItem {

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }                     // PK
    
    [Indexed, NotNull]
    public int ListId { get; set; }                 // FK - GroceryLists.Id

    [NotNull]
    public string Name { get; set; } = "";          // item name
    
    public string? Brand { get; set; }              // nullable string - optional

    [Indexed]
    public int? CategoryId { get; set; }            // FK - nullable int - optional - Categories.Id

    [NotNull]
    public decimal Quantity { get; set; }           // > 0

    [Indexed, NotNull]
    public int UnitId { get; set; }                 // FK 

    [NotNull]
    public decimal Cost { get; set; }               // >= 0 - total cost for this row in AUD

    public DateTime? PurchasedDate { get; set; }    // UTC - optional 
    public DateTime? ExpiryDate { get; set; }       // UTC - optional

    public String? Notes { get; set; }              // nullable string - optional notes

}
