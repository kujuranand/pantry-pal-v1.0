using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace PantryPal.Core.Models;

/// <summary>
/// a grocery list
/// </summary>

[Table("GroceryLists")]
public class GroceryList {

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }                 // PK
    
    [NotNull]
    public string Name { get; set; } = "";      // name of the shopping lists
    
    [NotNull]
    public DateTime CreatedUtc { get; set; }    // UTC for consistency
    
    public string? Notes { get; set; }          // nullable string - optional notes
    
}

