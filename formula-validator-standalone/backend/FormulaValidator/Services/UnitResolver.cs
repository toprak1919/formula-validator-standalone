using UnitsNet;
using UnitsNet.Units;

namespace FormulaValidator.Services
{
    /// <summary>
    /// Minimal unit resolver backed by UnitsNet.
    /// Currently implements Length (m, km, au) plus common aliases used in your demo.
    /// Extend this by adding more quantities/aliases as needed.
    /// </summary>
    public static class UnitResolver
    {
        private static readonly Dictionary<string, LengthUnit> LengthAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // meter
                ["m"] = LengthUnit.Meter,
                ["meter"] = LengthUnit.Meter,
                ["metre"] = LengthUnit.Meter,
                ["meters"] = LengthUnit.Meter,

                // kilometer
                ["km"] = LengthUnit.Kilometer,
                ["kilometer"] = LengthUnit.Kilometer,
                ["kilometre"] = LengthUnit.Kilometer,
                ["kilometers"] = LengthUnit.Kilometer,

                // astronomical unit
                ["au"] = LengthUnit.AstronomicalUnit,
                ["astronomical"] = LengthUnit.AstronomicalUnit,
                ["astronomical_unit"] = LengthUnit.AstronomicalUnit,
                ["astronomicalunit"] = LengthUnit.AstronomicalUnit
            };

        public static bool TryConvert(double value, string fromUnit, string toUnit, out double result)
        {
            result = double.NaN;

            if (LengthAliases.TryGetValue(fromUnit.Trim(), out var fromLen) &&
                LengthAliases.TryGetValue(toUnit.Trim(), out var toLen))
            {
                var q = Length.From(value, fromLen);
                result = q.As(toLen);
                return true;
            }

            return false;
        }
    }
}

