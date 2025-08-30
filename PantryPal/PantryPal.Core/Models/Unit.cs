using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantryPal.Core.Models;

/// <summary>
/// user-defined unit of measurement
/// </summary>

public class Unit {

    public int Id { get; set; }                 // PK
    public string Name { get; set; } = "";      // Kilogram
    public string Abbrev { get; set; } = "";    // kg
}
