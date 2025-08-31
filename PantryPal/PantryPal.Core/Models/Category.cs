using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace PantryPal.Core.Models;

/// <summary>
/// User-defined grocery item categories
/// </summary>

[Table("Categories")]
public class Category {

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }             // PK

    [NotNull]
    public string Name { get; set; } = "";  // unique category name like dairy, fruits, etc.

}
