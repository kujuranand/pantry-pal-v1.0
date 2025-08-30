using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantryPal.Core.Models;

/// <summary>
/// a grocery list
/// </summary>
public class GroceryList { 
    public int Id { get; set; }                 // PK
    public string Name { get; set; } = "";      // name of the shopping lists
    public DateTime CreatedUtc { get; set; }    // UTC for consistency
    public string? Notes { get; set; }          // nullable string - optional notes
    
}

