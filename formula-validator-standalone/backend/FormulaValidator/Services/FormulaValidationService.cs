using System.Text.RegularExpressions;
using FormulaValidator.Models;
using org.mariuszgromada.math.mxparser;

namespace FormulaValidator.Services
{
    public interface IFormulaValidationService
    {
        ValidationResult ValidateFormula(ValidationRequest request);
    }

    public class FormulaValidationService : IFormulaValidationService
    {
        public ValidationResult ValidateFormula(ValidationRequest request)
        {
            var result = new ValidationResult { Source = "Backend" };

            try
            {
                // Basic validation checks
                if (string.IsNullOrWhiteSpace(request.Formula))
                {
                    result.Error = "Formula cannot be empty";
                    return result;
                }

                var formula = request.Formula.Trim();

                // Convert our custom syntax to mXparser syntax
                // First handle unit-suffixed variables like $foo.meter / $foo.astronomical
                var unitArgs = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                var processedFormula = ReplaceUnitSuffixedVariables(formula, request, unitArgs, out var unitError);
                if (!string.IsNullOrEmpty(unitError))
                {
                    result.Error = unitError;
                    return result;
                }

                var mxparserFormula = ConvertToMXparserSyntax(processedFormula, request);

                // Create expression with mXparser
                var expression = new Expression(mxparserFormula);

                // Add measured values as arguments (kept as-is)
                foreach (var measuredValue in request.MeasuredValues)
                {
                    var varName = measuredValue.Id.TrimStart('$');
                    expression.defineArgument(varName, measuredValue.Value);
                }

                // Add constants using internal names to avoid collisions with built-ins
                foreach (var constant in request.Constants)
                {
                    var baseName = constant.Id.TrimStart('#');
                    var internalName = GetInternalConstantName(baseName);
                    expression.defineConstant(internalName, constant.Value);
                }

                // Add unit-converted arguments
                foreach (var kv in unitArgs)
                {
                    expression.defineArgument(kv.Key, kv.Value);
                }
                
                // Check syntax
                if (!expression.checkSyntax())
                {
                    var errorMessage = expression.getErrorMessage();
                    
                    // Enhance error messages
                    if (string.IsNullOrEmpty(errorMessage))
                    {
                        errorMessage = "Invalid formula syntax";
                    }
                    else
                    {
                        // Make error messages more user-friendly
                        errorMessage = EnhanceErrorMessage(errorMessage, formula);
                    }
                    
                    result.Error = errorMessage;
                    return result;
                }
                
                // Check for undefined variables (with $ prefix)
                var undefinedVars = Regex.Matches(formula, @"\$([a-zA-Z_][a-zA-Z0-9_]*)");
                foreach (Match match in undefinedVars)
                {
                    var varName = match.Groups[1].Value;
                    var fullVarName = "$" + varName;
                    if (!request.MeasuredValues.Any(mv => mv.Id == fullVarName || mv.Id == varName))
                    {
                        result.Error = $"Undefined variable: ${varName}";
                        return result;
                    }
                }
                
                // Check for undefined constants (with # prefix)
                var undefinedConsts = Regex.Matches(formula, @"#([a-zA-Z_][a-zA-Z0-9_]*)");
                foreach (Match match in undefinedConsts)
                {
                    var constName = match.Groups[1].Value;
                    var fullConstName = "#" + constName;
                    if (!request.Constants.Any(c => c.Id == fullConstName || c.Id == constName))
                    {
                        result.Error = $"Undefined constant: #{constName}";
                        return result;
                    }
                }
                
                // Calculate the result
                var calculationResult = expression.calculate();
                
                // Check for calculation errors
                if (double.IsNaN(calculationResult))
                {
                    result.Error = "Unable to calculate result - check formula syntax";
                    return result;
                }
                
                if (double.IsInfinity(calculationResult))
                {
                    result.Error = "Result is infinity - division by zero or overflow";
                    return result;
                }
                
                // Success
                result.IsValid = true;
                result.Result = calculationResult;
                result.EvaluatedFormula = mxparserFormula;
                
            }
            catch (Exception ex)
            {
                result.Error = $"Validation error: {ex.Message}";
                result.IsValid = false;
            }

            return result;
        }
        
        private string ConvertToMXparserSyntax(string formula, ValidationRequest request)
        {
            var converted = formula;
            
            // Replace $ variables with their names
            foreach (var measuredValue in request.MeasuredValues)
            {
                var id = measuredValue.Id.StartsWith("$") ? measuredValue.Id.Substring(1) : measuredValue.Id;
                // Do not match $id when followed by a dot (handled as unit-suffixed variable earlier)
                var pattern = $@"\${Regex.Escape(id)}(?![a-zA-Z0-9_\.])";
                converted = Regex.Replace(converted, pattern, id);
            }
            
            // Replace # constants with internal names to avoid clashes with built-ins (e.g., pi, e) or functions
            foreach (var constant in request.Constants)
            {
                var id = constant.Id.StartsWith("#") ? constant.Id.Substring(1) : constant.Id;
                var internalName = GetInternalConstantName(id);
                var pattern = $@"#{Regex.Escape(id)}(?![a-zA-Z0-9_])";
                converted = Regex.Replace(converted, pattern, internalName);
            }
            
            // Handle power operator - convert ^ to mXparser's ^ (it's the same)
            // mXparser already uses ^ for power
            
            return converted;
        }

        // Replace tokens like $name.unit with internal argument names and collect their numeric values
        private string ReplaceUnitSuffixedVariables(
            string formula,
            ValidationRequest request,
            Dictionary<string, double> unitArgs,
            out string? error)
        {
            error = null;
            var pattern = new Regex(@"\$(?<name>[a-zA-Z_][a-zA-Z0-9_]*)\.(?<unit>[a-zA-Z_][a-zA-Z0-9_]*)", RegexOptions.Compiled);

            // Use a local variable to capture errors inside the lambda
            string? capturedError = null;

            string evaluator(Match m)
            {
                var name = m.Groups["name"].Value;
                var unit = m.Groups["unit"].Value;

                // Find measured value by id "$name" or "name"
                var mv = request.MeasuredValues.FirstOrDefault(v =>
                    string.Equals(v.Id, "$" + name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(v.Id, name, StringComparison.OrdinalIgnoreCase));

                if (mv == null)
                {
                    capturedError = $"Undefined variable with unit: ${name}.{unit}";
                    return m.Value; // keep original to avoid further issues
                }

                var fromUnit = (mv.Unit ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(fromUnit))
                {
                    capturedError = $"Variable '{name}' has no unit defined but is used with '.{unit}'.";
                    return m.Value;
                }

                if (!UnitConverter.TryConvert(mv.Value, fromUnit, unit, out var converted))
                {
                    capturedError = $"Cannot convert variable '{name}' from '{fromUnit}' to '{unit}'.";
                    return m.Value;
                }

                var internalName = GetInternalUnitArgName(name, unit);
                if (!unitArgs.ContainsKey(internalName))
                    unitArgs[internalName] = converted;

                return internalName;
            }

            var replaced = pattern.Replace(formula, new MatchEvaluator(evaluator));

            // Assign the captured error to the out parameter after the lambda execution
            error = capturedError;

            return replaced;
        }

        private static string GetInternalUnitArgName(string baseName, string unit)
        {
            // Ensure valid identifier: start with letter and use only letters, digits, underscore
            var safeUnit = Regex.Replace(unit, @"[^a-zA-Z0-9_]", "_");
            return $"MV_{baseName}__{safeUnit}";
        }

        private static class UnitConverter
        {
            // Map aliases to canonical units and category
            private static readonly Dictionary<string, (string Canonical, string Category)> Aliases = new(StringComparer.OrdinalIgnoreCase)
            {
                // Length
                {"m", ("m", "length")},
                {"meter", ("m", "length")},
                {"metre", ("m", "length")},
                {"meters", ("m", "length")},
                {"kilometer", ("km", "length")},
                {"kilometre", ("km", "length")},
                {"km", ("km", "length")},
                {"au", ("au", "length")},
                {"astronomical", ("au", "length")},
                {"astronomical_unit", ("au", "length")},
                {"astronomicalunit", ("au", "length")},
            };

            // Factors to meters for length units
            private static readonly Dictionary<string, double> LengthToMeters = new(StringComparer.OrdinalIgnoreCase)
            {
                {"m", 1.0},
                {"km", 1000.0},
                {"au", 149_597_870_700.0}, // IAU 2012 definition
            };

            public static bool TryConvert(double value, string fromUnit, string toUnit, out double result)
            {
                result = double.NaN;
                if (!TryGetCanonical(fromUnit, out var f, out var catF)) return false;
                if (!TryGetCanonical(toUnit, out var t, out var catT)) return false;
                if (!string.Equals(catF, catT, StringComparison.OrdinalIgnoreCase)) return false;

                switch (catF)
                {
                    case "length":
                        if (!LengthToMeters.TryGetValue(f, out var f2m)) return false;
                        if (!LengthToMeters.TryGetValue(t, out var t2m)) return false;
                        var inMeters = value * f2m;
                        result = inMeters / t2m;
                        return true;
                    default:
                        return false;
                }
            }

            private static bool TryGetCanonical(string unit, out string canonical, out string category)
            {
                canonical = string.Empty;
                category = string.Empty;
                if (string.IsNullOrWhiteSpace(unit)) return false;
                var key = unit.Trim();
                if (Aliases.TryGetValue(key, out var val))
                {
                    canonical = val.Canonical;
                    category = val.Category;
                    return true;
                }
                return false;
            }
        }

        private static string GetInternalConstantName(string baseName)
        {
            // Use a prefix starting with a letter to meet identifier rules and avoid collisions
            return $"C_{baseName}";
        }

        private string EnhanceErrorMessage(string mxparserError, string originalFormula)
        {
            // Make mXparser error messages more user-friendly
            if (mxparserError.Contains("Invalid token"))
            {
                // Try to identify what's invalid
                if (Regex.IsMatch(originalFormula, @"[\+\-\*/\^%]{2,}"))
                {
                    return "Invalid double operators in formula";
                }
                if (Regex.IsMatch(originalFormula, @"^\s*[\+\*/\^%]"))
                {
                    return "Formula cannot start with an operator";
                }
                if (Regex.IsMatch(originalFormula, @"[\+\-\*/\^%]\s*$"))
                {
                    return "Incomplete operation - formula ends with an operator";
                }
                return "Invalid syntax in formula";
            }
            
            if (mxparserError.Contains("Duplicated keywords were found", StringComparison.OrdinalIgnoreCase))
            {
                return "Duplicate name conflict with a built-in token (e.g., pi, e). Rename your variable/constant or use the prefixed syntax (#name) as provided.";
            }

            if (mxparserError.Contains("syntax error") || mxparserError.Contains("Syntax error"))
            {
                // Check for common syntax errors
                if (Regex.IsMatch(originalFormula, @"\(\s*\)"))
                {
                    return "Empty parentheses are not allowed";
                }
                if (originalFormula.Count(c => c == '(') != originalFormula.Count(c => c == ')'))
                {
                    var openCount = originalFormula.Count(c => c == '(');
                    var closeCount = originalFormula.Count(c => c == ')');
                    return openCount > closeCount 
                        ? "Unmatched opening parenthesis" 
                        : "Unmatched closing parenthesis";
                }
                if (Regex.IsMatch(originalFormula, @"\d+\s+\d+"))
                {
                    return "Missing operator between numbers";
                }
                if (Regex.IsMatch(originalFormula, @"\$[a-zA-Z_][a-zA-Z0-9_]*\s+\$[a-zA-Z_][a-zA-Z0-9_]*"))
                {
                    return "Missing operator between variables";
                }
                return "Syntax error in formula";
            }
            
            if (mxparserError.Contains("Unknown function"))
            {
                var funcMatch = Regex.Match(mxparserError, @"Unknown function:\s*(\w+)");
                if (funcMatch.Success)
                {
                    return $"Unknown function: {funcMatch.Groups[1].Value}";
                }
                return "Unknown function in formula";
            }
            
            if (mxparserError.Contains("Argument"))
            {
                return "Invalid argument in formula";
            }
            
            // Return the original error if we can't enhance it
            return mxparserError;
        }
    }
}
