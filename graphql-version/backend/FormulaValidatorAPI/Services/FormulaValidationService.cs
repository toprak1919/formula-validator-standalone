using System.Text.RegularExpressions;
using FormulaValidatorAPI.Models;

namespace FormulaValidatorAPI.Services
{
    public interface IFormulaValidationService
    {
        ValidationResult ValidateFormula(ValidationRequest request);
    }

    public class FormulaValidationService : IFormulaValidationService
    {
        public ValidationResult ValidateFormula(ValidationRequest request)
        {
            var result = new ValidationResult { Source = ValidationSource.Backend };

            try
            {
                // Basic validation checks
                if (string.IsNullOrWhiteSpace(request.Formula))
                {
                    result.Error = "Formula cannot be empty";
                    return result;
                }

                var formula = request.Formula.Trim();

                // Check for missing operators between variables
                if (Regex.IsMatch(formula, @"\$[a-zA-Z_][a-zA-Z0-9_]*\s+\$[a-zA-Z_][a-zA-Z0-9_]*"))
                {
                    result.Error = "Missing operator between variables";
                    return result;
                }

                // Check for missing operators between numbers
                if (Regex.IsMatch(formula, @"\d+\s+\d+"))
                {
                    result.Error = "Missing operator between numbers";
                    return result;
                }

                // Check for double operators
                if (Regex.IsMatch(formula, @"[\+\-\*/]{2,}"))
                {
                    result.Error = "Invalid double operators";
                    return result;
                }

                // Check for leading operators (except minus)
                if (Regex.IsMatch(formula, @"^\s*[\+\*/]"))
                {
                    result.Error = "Formula cannot start with an operator";
                    return result;
                }

                // Check for trailing operators
                if (Regex.IsMatch(formula, @"[\+\-\*/]\s*$"))
                {
                    result.Error = "Incomplete operation - formula ends with an operator";
                    return result;
                }

                // Check for empty parentheses
                if (Regex.IsMatch(formula, @"\(\s*\)"))
                {
                    result.Error = "Empty parentheses are not allowed";
                    return result;
                }

                // Check parentheses balance
                int openCount = formula.Count(c => c == '(');
                int closeCount = formula.Count(c => c == ')');
                if (openCount != closeCount)
                {
                    result.Error = openCount > closeCount 
                        ? "Unmatched opening parenthesis" 
                        : "Unmatched closing parenthesis";
                    return result;
                }

                // Check for invalid hash usage
                if (Regex.IsMatch(formula, @"##|#\d|#\s*$|(?:^|\s)#(?![a-zA-Z_])"))
                {
                    result.Error = "Invalid constant syntax";
                    return result;
                }

                // Check for invalid dollar usage
                if (Regex.IsMatch(formula, @"\$\$|\$\d+$|\$\s*$"))
                {
                    result.Error = "Invalid variable syntax";
                    return result;
                }

                // Replace variables and constants with values
                var evaluatedFormula = formula;
                
                // Replace measured values
                foreach (var measuredValue in request.MeasuredValues)
                {
                    var pattern = $@"\${Regex.Escape(measuredValue.Id)}(?![a-zA-Z0-9_])";
                    evaluatedFormula = Regex.Replace(evaluatedFormula, pattern, measuredValue.Value.ToString());
                }

                // Replace constants
                foreach (var constant in request.Constants)
                {
                    var pattern = $@"#{Regex.Escape(constant.Id)}(?![a-zA-Z0-9_])";
                    evaluatedFormula = Regex.Replace(evaluatedFormula, pattern, constant.Value.ToString());
                }

                // Check for undefined variables
                if (Regex.IsMatch(evaluatedFormula, @"\$[a-zA-Z_][a-zA-Z0-9_]*"))
                {
                    var undefinedVar = Regex.Match(evaluatedFormula, @"\$([a-zA-Z_][a-zA-Z0-9_]*)").Groups[1].Value;
                    result.Error = $"Undefined variable: ${undefinedVar}";
                    return result;
                }

                // Check for undefined constants
                if (Regex.IsMatch(evaluatedFormula, @"#[a-zA-Z_][a-zA-Z0-9_]*"))
                {
                    var undefinedConst = Regex.Match(evaluatedFormula, @"#([a-zA-Z_][a-zA-Z0-9_]*)").Groups[1].Value;
                    result.Error = $"Undefined constant: #{undefinedConst}";
                    return result;
                }

                // Check for undefined functions (basic check)
                if (Regex.IsMatch(evaluatedFormula, @"[a-zA-Z_][a-zA-Z0-9_]*\s*\("))
                {
                    var funcMatch = Regex.Match(evaluatedFormula, @"([a-zA-Z_][a-zA-Z0-9_]*)\s*\(");
                    var funcName = funcMatch.Groups[1].Value;
                    var allowedFunctions = new[] { "sqrt", "sin", "cos", "tan", "log", "exp", "abs", "pow", "min", "max", "round", "floor", "ceil" };
                    if (!allowedFunctions.Contains(funcName.ToLower()))
                    {
                        result.Error = $"Undefined function: {funcName}";
                        return result;
                    }
                }

                // Try to evaluate the formula (simplified evaluation)
                try
                {
                    // For demonstration, we'll just mark it as valid if all checks pass
                    // In a real implementation, you'd use a proper expression evaluator
                    result.IsValid = true;
                    result.EvaluatedFormula = evaluatedFormula;
                    
                    // Simple evaluation for basic operations (very limited)
                    if (Regex.IsMatch(evaluatedFormula, @"^[\d\.\+\-\*/\(\)\s]+$"))
                    {
                        // This is a very basic and unsafe evaluation - in production use a proper expression parser
                        // result.Result = EvaluateSimpleExpression(evaluatedFormula);
                    }
                }
                catch (Exception ex)
                {
                    result.Error = $"Evaluation error: {ex.Message}";
                    result.IsValid = false;
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Validation error: {ex.Message}";
                result.IsValid = false;
            }

            return result;
        }
    }
}