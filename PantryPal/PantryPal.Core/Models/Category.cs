using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantryPal.Core.Models;

/// <summary>
/// User-defined grocery item categories
/// </summary>
public class Category { 

    public int Id { get; set; }             // PK
    public string Name { get; set; } = "";  // unique category name like dairy, fruits, etc.

}
