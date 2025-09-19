using UnitsNet;
using UnitsNet.Units;

namespace FormulaValidator.Services
{
    /// <summary>
    /// Unit resolver backed by UnitsNet. Supports the unit sets exposed in the UI so
    /// conversions succeed when users pick those options.
    /// </summary>
    public static class UnitResolver
    {
        private static readonly Dictionary<string, LengthUnit> LengthAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            // meter family
            ["m"] = LengthUnit.Meter,
            ["meter"] = LengthUnit.Meter,
            ["metre"] = LengthUnit.Meter,
            ["meters"] = LengthUnit.Meter,

            // sub-multiples
            ["cm"] = LengthUnit.Centimeter,
            ["centimeter"] = LengthUnit.Centimeter,
            ["centimetre"] = LengthUnit.Centimeter,
            ["mm"] = LengthUnit.Millimeter,
            ["millimeter"] = LengthUnit.Millimeter,
            ["millimetre"] = LengthUnit.Millimeter,

            // larger units
            ["km"] = LengthUnit.Kilometer,
            ["kilometer"] = LengthUnit.Kilometer,
            ["kilometre"] = LengthUnit.Kilometer,
            ["kilometers"] = LengthUnit.Kilometer,
            ["mi"] = LengthUnit.Mile,
            ["mile"] = LengthUnit.Mile,
            ["miles"] = LengthUnit.Mile,
            ["yd"] = LengthUnit.Yard,
            ["yard"] = LengthUnit.Yard,
            ["yards"] = LengthUnit.Yard,
            ["ft"] = LengthUnit.Foot,
            ["foot"] = LengthUnit.Foot,
            ["feet"] = LengthUnit.Foot,
            ["in"] = LengthUnit.Inch,
            ["inch"] = LengthUnit.Inch,
            ["inches"] = LengthUnit.Inch,

            // astronomical unit
            ["au"] = LengthUnit.AstronomicalUnit,
            ["astronomical"] = LengthUnit.AstronomicalUnit,
            ["astronomical_unit"] = LengthUnit.AstronomicalUnit,
            ["astronomicalunit"] = LengthUnit.AstronomicalUnit
        };

        private static readonly Dictionary<string, MassUnit> MassAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["kg"] = MassUnit.Kilogram,
            ["kilogram"] = MassUnit.Kilogram,
            ["g"] = MassUnit.Gram,
            ["gram"] = MassUnit.Gram,
            ["grams"] = MassUnit.Gram
        };

        private static readonly Dictionary<string, DurationUnit> DurationAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["s"] = DurationUnit.Second,
            ["sec"] = DurationUnit.Second,
            ["second"] = DurationUnit.Second,
            ["seconds"] = DurationUnit.Second,
            ["ms"] = DurationUnit.Millisecond,
            ["millisecond"] = DurationUnit.Millisecond,
            ["milliseconds"] = DurationUnit.Millisecond,
            ["min"] = DurationUnit.Minute,
            ["minute"] = DurationUnit.Minute,
            ["minutes"] = DurationUnit.Minute,
            ["h"] = DurationUnit.Hour,
            ["hr"] = DurationUnit.Hour,
            ["hour"] = DurationUnit.Hour,
            ["hours"] = DurationUnit.Hour
        };

        private static readonly Dictionary<string, TemperatureUnit> TemperatureAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["k"] = TemperatureUnit.Kelvin,
            ["kelvin"] = TemperatureUnit.Kelvin,
            ["c"] = TemperatureUnit.DegreeCelsius,
            ["celsius"] = TemperatureUnit.DegreeCelsius,
            ["f"] = TemperatureUnit.DegreeFahrenheit,
            ["fahrenheit"] = TemperatureUnit.DegreeFahrenheit
        };

        private static readonly Dictionary<string, ElectricCurrentUnit> ElectricCurrentAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = ElectricCurrentUnit.Ampere,
            ["ampere"] = ElectricCurrentUnit.Ampere,
            ["amp"] = ElectricCurrentUnit.Ampere
        };

        private static readonly Dictionary<string, ElectricPotentialUnit> ElectricPotentialAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["v"] = ElectricPotentialUnit.Volt,
            ["volt"] = ElectricPotentialUnit.Volt,
            ["volts"] = ElectricPotentialUnit.Volt
        };

        private static readonly Dictionary<string, ElectricResistanceUnit> ElectricResistanceAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ohm"] = ElectricResistanceUnit.Ohm,
            ["Ω"] = ElectricResistanceUnit.Ohm
        };

        private static readonly Dictionary<string, VolumeUnit> VolumeAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["l"] = VolumeUnit.Liter,
            ["liter"] = VolumeUnit.Liter,
            ["litre"] = VolumeUnit.Liter,
            ["ml"] = VolumeUnit.Milliliter,
            ["milliliter"] = VolumeUnit.Milliliter,
            ["millilitre"] = VolumeUnit.Milliliter
        };

        private static readonly Dictionary<string, PressureUnit> PressureAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["pa"] = PressureUnit.Pascal,
            ["pascal"] = PressureUnit.Pascal,
            ["bar"] = PressureUnit.Bar
        };

        private static readonly Dictionary<string, ForceUnit> ForceAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["n"] = ForceUnit.Newton,
            ["newton"] = ForceUnit.Newton,
            ["newtons"] = ForceUnit.Newton
        };

        private static readonly Dictionary<string, EnergyUnit> EnergyAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["j"] = EnergyUnit.Joule,
            ["joule"] = EnergyUnit.Joule,
            ["joules"] = EnergyUnit.Joule
        };

        private static readonly Dictionary<string, PowerUnit> PowerAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["w"] = PowerUnit.Watt,
            ["watt"] = PowerUnit.Watt,
            ["watts"] = PowerUnit.Watt
        };

        public static bool TryConvert(double value, string fromUnit, string toUnit, out double result)
        {
            result = double.NaN;

            if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit))
            {
                return false;
            }

            var from = fromUnit.Trim();
            var to = toUnit.Trim();

            if (TryConvertLength(value, from, to, out result) ||
                TryConvertMass(value, from, to, out result) ||
                TryConvertDuration(value, from, to, out result) ||
                TryConvertTemperature(value, from, to, out result) ||
                TryConvertElectricCurrent(value, from, to, out result) ||
                TryConvertElectricPotential(value, from, to, out result) ||
                TryConvertElectricResistance(value, from, to, out result) ||
                TryConvertVolume(value, from, to, out result) ||
                TryConvertPressure(value, from, to, out result) ||
                TryConvertForce(value, from, to, out result) ||
                TryConvertEnergy(value, from, to, out result) ||
                TryConvertPower(value, from, to, out result))
            {
                return true;
            }

            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            {
                result = value;
                return true;
            }

            return false;
        }

        private static bool TryConvertLength(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!LengthAliases.TryGetValue(from, out var fromUnit) || !LengthAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = Length.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertMass(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!MassAliases.TryGetValue(from, out var fromUnit) || !MassAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = Mass.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertDuration(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!DurationAliases.TryGetValue(from, out var fromUnit) || !DurationAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = Duration.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertTemperature(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!TemperatureAliases.TryGetValue(from, out var fromUnit) || !TemperatureAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = Temperature.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertElectricCurrent(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!ElectricCurrentAliases.TryGetValue(from, out var fromUnit) || !ElectricCurrentAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = ElectricCurrent.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertElectricPotential(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!ElectricPotentialAliases.TryGetValue(from, out var fromUnit) || !ElectricPotentialAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = ElectricPotential.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertElectricResistance(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!ElectricResistanceAliases.TryGetValue(from, out var fromUnit) || !ElectricResistanceAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = ElectricResistance.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertVolume(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!VolumeAliases.TryGetValue(from, out var fromUnit) || !VolumeAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = Volume.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertPressure(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!PressureAliases.TryGetValue(from, out var fromUnit) || !PressureAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = Pressure.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertForce(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!ForceAliases.TryGetValue(from, out var fromUnit) || !ForceAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = Force.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertEnergy(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!EnergyAliases.TryGetValue(from, out var fromUnit) || !EnergyAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = Energy.From(value, fromUnit);
            result = quantity.As(toUnit);
            return true;
        }

        private static bool TryConvertPower(double value, string from, string to, out double result)
        {
            result = double.NaN;
            if (!PowerAliases.TryGetValue(from, out var fromUnit) || !PowerAliases.TryGetValue(to, out var toUnit))
            {
                return false;
            }

            var quantity = Power.From(value, fromUnit);
            result = (double)quantity.As(toUnit);
            return true;
        }
    }
}
