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

                // undefined variables
                foreach (var v in collector.Variables)
                {
                    if (!request.MeasuredValues.Any(m => string.Equals(m.Id.TrimStart('$'), v, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Error = $"Undefined variable: ${v}";
                        return result;
                    }
                }

                // undefined constants
                foreach (var c in collector.Constants)
                {
                    if (!request.Constants.Any(k => string.Equals(k.Id.TrimStart('#'), c, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Error = $"Undefined constant: #{c}";
                        return result;
                    }
                }

                // For variables used with .unit ensure the source has units
                foreach (var v in collector.VariablesWithUnit)
                {
                    var mv = request.MeasuredValues.First(m => string.Equals(m.Id.TrimStart('$'), v, StringComparison.OrdinalIgnoreCase));
                    if (string.IsNullOrWhiteSpace(mv.Unit))
                    {
                        result.Error = $"Variable '{v}' has no unit defined but is used with a unit suffix.";
                        return result;
                    }
                }

                // --- 3) Evaluate
                var evaluator = new EvalVisitor(request.MeasuredValues, request.Constants);
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
    }
}
