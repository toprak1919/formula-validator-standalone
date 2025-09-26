using Antlr4.Runtime;
using FormulaValidator.Models;
using FormulaValidator.Parsing;
using FormulaValidator.Services.Visitors;
using System.Globalization;

namespace FormulaValidator.Services
{
    public interface IFormulaValidationService
    {
        ValidationResult ValidateFormula(ValidationRequest request);
    }

    public class FormulaValidationService : IFormulaValidationService
    {
        private readonly IConstantRepository _constantRepository;

        public FormulaValidationService(IConstantRepository constantRepository)
        {
            _constantRepository = constantRepository;
        }

        public ValidationResult ValidateFormula(ValidationRequest request)
        {
            var result = new ValidationResult { Source = "Backend" };

            try
            {
                if (string.IsNullOrWhiteSpace(request.Formula))
                {
                    result.Error = "Formula cannot be empty";
                    return result;
                }

                var input = request.Formula.Trim();

                // --- 1) Parse with ANTLR
                var inputStream = new AntlrInputStream(input);
                var lexer = new FormulaLexer(inputStream);
                var tokens = new CommonTokenStream(lexer);

                var parser = new FormulaParser(tokens)
                {
                    BuildParseTree = true
                };

                var err = new CollectingErrorListener();
                parser.RemoveErrorListeners();
                parser.AddErrorListener(err);

                var tree = parser.formula();

                if (err.HasError)
                {
                    result.Error = err.Error;
                    return result;
                }

                // --- 2) Collect referenced symbols for better errors
                var collector = new SymbolCollector();
                collector.Visit(tree);

                var measuredLookup = BuildMeasuredLookup(request.MeasuredValues, out var measuredError);
                if (measuredError is not null)
                {
                    result.Error = measuredError;
                    return result;
                }

                foreach (var variable in collector.Variables)
                {
                    if (!measuredLookup.TryGetValue(variable, out _))
                    {
                        result.Error = $"Undefined variable: ${variable}";
                        return result;
                    }
                }

                foreach (var variable in collector.VariablesWithIndex)
                {
                    if (!measuredLookup.TryGetValue(variable, out var mv) || mv.Values is null || mv.Values.Count == 0)
                    {
                        result.Error = $"Variable '{variable}' is scalar but is used with an index.";
                        return result;
                    }
                }

                foreach (var variable in collector.VariablesWithIndex)
                {
                    if (collector.VariablesWithoutIndex.Contains(variable))
                    {
                        result.Error = $"Variable '{variable}' is used both with and without an index.";
                        return result;
                    }
                }

                foreach (var variable in collector.VariablesWithoutIndex)
                {
                    if (measuredLookup.TryGetValue(variable, out var mv) && mv.Values is not null && mv.Values.Count > 0)
                    {
                        result.Error = $"Variable '{variable}' is non-scalar. Use an index like '${variable}[i]'.";
                        return result;
                    }
                }

                var constantLookup = MergeConstants(_constantRepository.GetAll(), request.Constants);

                foreach (var c in collector.Constants)
                {
                    if (!constantLookup.ContainsKey(c))
                    {
                        result.Error = $"Undefined constant: #{c}";
                        return result;
                    }
                }

                foreach (var variable in collector.VariablesWithUnit)
                {
                    var mv = measuredLookup[variable];
                    if (string.IsNullOrWhiteSpace(mv.Unit))
                    {
                        result.Error = $"Variable '{variable}' has no unit defined but is used with a unit suffix.";
                        return result;
                    }
                }

                // --- 3) Evaluate
                var evaluator = new EvalVisitor(measuredLookup.Values, constantLookup.Values);
                var value = evaluator.Visit(tree);

                if (double.IsNaN(value))
                {
                    result.Error = "Result is not a real number (NaN)";
                    result.IsValid = false;
                    return result;
                }

                if (double.IsInfinity(value))
                {
                    result.Error = "Result is infinity - division by zero or overflow";
                    result.IsValid = false;
                    return result;
                }

                // Success
                result.IsValid = true;
                result.Result = value;
                result.EvaluatedFormula = input; // Or produce a normalized string if you prefer.
            }
            catch (InvalidOperationException ex)
            {
                // Known validation/evaluation errors (undefined symbol, bad unit, unknown function, etc.)
                result.Error = ex.Message;
                result.IsValid = false;
            }
            catch (Exception ex)
            {
                // Fallback
                result.Error = $"Validation error: {ex.Message}";
                result.IsValid = false;
            }

            return result;
        }

        private static Dictionary<string, MeasuredValue> BuildMeasuredLookup(
            IEnumerable<MeasuredValue>? measuredValues,
            out string? error)
        {
            error = null;
            var lookup = new Dictionary<string, MeasuredValue>(StringComparer.OrdinalIgnoreCase);

            if (measuredValues is null)
            {
                return lookup;
            }

            foreach (var measuredValue in measuredValues)
            {
                var normalizedId = NormalizeVariableId(measuredValue.Id);
                if (string.IsNullOrEmpty(normalizedId))
                {
                    error = "Measured value id cannot be empty.";
                    return lookup;
                }

                if (lookup.ContainsKey(normalizedId))
                {
                    error = $"Duplicate variable: ${normalizedId}";
                    return lookup;
                }

                var hasScalar = measuredValue.Value.HasValue;
                var hasVector = measuredValue.Values is not null && measuredValue.Values.Count > 0;

                if (hasScalar && hasVector)
                {
                    error = $"Variable '${normalizedId}' cannot define both 'value' and 'values'.";
                    return lookup;
                }

                if (!hasScalar && !hasVector)
                {
                    error = $"Variable '${normalizedId}' must define either 'value' or 'values'.";
                    return lookup;
                }

                lookup[normalizedId] = measuredValue;
            }

            return lookup;
        }

        private static string NormalizeVariableId(string id)
        {
            var trimmed = (id ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            return trimmed.StartsWith("$", StringComparison.Ordinal)
                ? trimmed.TrimStart('$').Trim()
                : trimmed;
        }

        private static Dictionary<string, Constant> MergeConstants(
            IEnumerable<Constant> baseConstants,
            IEnumerable<Constant>? overrides)
        {
            var lookup = new Dictionary<string, Constant>(StringComparer.OrdinalIgnoreCase);

            void AddOrReplace(Constant constant)
            {
                var normalizedId = NormalizeConstantId(constant.Id);
                if (normalizedId == "#")
                {
                    return;
                }
                var trimmed = normalizedId.TrimStart('#');
                lookup[trimmed] = new Constant
                {
                    Id = normalizedId,
                    Name = constant.Name,
                    Value = constant.Value
                };
            }

            foreach (var constant in baseConstants)
            {
                AddOrReplace(constant);
            }

            if (overrides != null)
            {
                foreach (var constant in overrides)
                {
                    AddOrReplace(constant);
                }
            }

            return lookup;
        }

        private static string NormalizeConstantId(string id)
        {
            var trimmed = (id ?? string.Empty).Trim();
            if (!trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                trimmed = "#" + trimmed;
            }
            return trimmed;
        }
    }
}
