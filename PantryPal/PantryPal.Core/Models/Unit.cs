using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace PantryPal.Core.Models;

/// <summary>
/// user-defined unit of measurement
/// </summary>

[Table("Units")]
public class Unit {

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }                 // PK

    [NotNull]
    public string Name { get; set; } = "";      // Kilogram

    [NotNull]
    public string Abbrev { get; set; } = "";    // kg
}
