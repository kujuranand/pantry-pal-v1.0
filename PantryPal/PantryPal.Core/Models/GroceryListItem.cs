using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantryPal.Core.Models;

/// <summary>
/// a grocery item in a grocery list.
/// </summary>

public class GroceryListItem { 

    public int Id { get; set; }                     // PK
    public int ListId { get; set; }                 // FK

    public string Name { get; set; } = "";          // item name
    public string? Brand { get; set; }              // nullable string - optional
    
    public int? CategoryId { get; set; }            // FK - nullable int - optional
    public decimal Quantity { get; set; }           // > 0
    public int UnitId { get; set; }                 // FK 

    // total cost fo this row in AUD
    public decimal Cost { get; set; }               // >= 0

    public DateTime? PurchasedDate { get; set; }    // UTC - optional 
    public DateTime? ExpiryDate { get; set; }       // UTC - optional

    public String? Notes { get; set; }              // nullable string - optional notes

}
