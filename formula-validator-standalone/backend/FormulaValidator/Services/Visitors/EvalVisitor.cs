using System.Globalization;
using Antlr4.Runtime.Misc;
using FormulaValidator.Models;
using FormulaValidator.Parsing;

namespace FormulaValidator.Services.Visitors
{
    /// <summary>
    /// Collects referenced symbols ($var and #const) before evaluation
    /// to report undefined ones early with friendly messages.
    /// </summary>
    public sealed class SymbolCollector : FormulaBaseVisitor<object?>
    {
        public readonly HashSet<string> Variables = new(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> VariablesWithUnit = new(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> VariablesWithIndex = new(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> VariablesWithoutIndex = new(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> Constants = new(StringComparer.OrdinalIgnoreCase);

        public override object? VisitVarRef([NotNull] FormulaParser.VarRefContext context)
        {
            var name = context.IDENT().GetText();
            Variables.Add(name);

            bool hasIndex = false;

            foreach (var suffix in context.varRefSuffix())
            {
                if (suffix.DOT() != null)
                {
                    VariablesWithUnit.Add(name);
                }
                else if (suffix.LBRACK() != null)
                {
                    VariablesWithIndex.Add(name);
                    hasIndex = true;
                }
            }

            if (!hasIndex)
            {
                VariablesWithoutIndex.Add(name);
            }

            return null;
        }

        public override object? VisitConstRef([NotNull] FormulaParser.ConstRefContext context)
        {
            var name = context.IDENT().GetText();
            Constants.Add(name);
            return null;
        }
    }

    /// <summary>
    /// Evaluates the expression tree into a double value.
    /// </summary>
    public sealed class EvalVisitor : FormulaBaseVisitor<double>
    {
        private readonly IReadOnlyDictionary<string, MeasuredValue> _measured; // keys: name without '$'
        private readonly IReadOnlyDictionary<string, Constant> _constants;     // keys: name without '#'

        public EvalVisitor(IEnumerable<MeasuredValue> mvs, IEnumerable<Constant> consts)
        {
            _measured = BuildMeasuredMap(mvs);
            _constants = BuildConstantMap(consts);
        }

        private static IReadOnlyDictionary<string, MeasuredValue> BuildMeasuredMap(IEnumerable<MeasuredValue>? values)
        {
            var lookup = new Dictionary<string, MeasuredValue>(StringComparer.OrdinalIgnoreCase);

            if (values is null)
            {
                return lookup;
            }

            foreach (var value in values)
            {
                var key = NormalizeVariableId(value.Id);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                if (lookup.ContainsKey(key))
                {
                    throw new InvalidOperationException($"Duplicate variable: ${key}");
                }

                lookup[key] = value;
            }

            return lookup;
        }

        private static IReadOnlyDictionary<string, Constant> BuildConstantMap(IEnumerable<Constant>? values)
        {
            var lookup = new Dictionary<string, Constant>(StringComparer.OrdinalIgnoreCase);

            if (values is null)
            {
                return lookup;
            }

            foreach (var constant in values)
            {
                var key = NormalizeConstantId(constant.Id);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                if (lookup.ContainsKey(key))
                {
                    throw new InvalidOperationException($"Duplicate constant: #{key}");
                }

                lookup[key] = constant;
            }

            return lookup;
        }

        private static string NormalizeVariableId(string id)
        {
            var trimmed = (id ?? string.Empty).Trim();
            if (trimmed.StartsWith("$", StringComparison.Ordinal))
            {
                trimmed = trimmed.TrimStart('$');
            }

            return trimmed.Trim();
        }

        private static string NormalizeConstantId(string id)
        {
            var trimmed = (id ?? string.Empty).Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                trimmed = trimmed.TrimStart('#');
            }

            return trimmed.Trim();
        }

        private static int NormalizeIndex(double rawIndex, string variableName)
        {
            if (double.IsNaN(rawIndex) || double.IsInfinity(rawIndex))
            {
                throw new InvalidOperationException($"Index for variable '{variableName}' must evaluate to a finite number.");
            }

            var rounded = (int)Math.Round(rawIndex);
            if (Math.Abs(rawIndex - rounded) > 1e-9)
            {
                throw new InvalidOperationException($"Index for variable '{variableName}' must be an integer.");
            }

            if (rounded < 0)
            {
                throw new InvalidOperationException($"Index for variable '{variableName}' must be non-negative.");
            }

            return rounded;
        }

        public override double VisitFormula([NotNull] FormulaParser.FormulaContext context)
            => Visit(context.expr());

        public override double VisitNumberPrimary([NotNull] FormulaParser.NumberPrimaryContext context)
        {
            var txt = context.NUMBER().GetText();
            return double.Parse(txt, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public override double VisitVarPrimary([NotNull] FormulaParser.VarPrimaryContext context)
        {
            var varRef = context.varRef();
            var name = varRef.IDENT().GetText();

            if (!_measured.TryGetValue(name, out var measuredValue))
            {
                throw new InvalidOperationException($"Undefined variable: ${name}");
            }

            int? requestedIndex = null;
            string? requestedUnit = null;

            foreach (var suffix in varRef.varRefSuffix())
            {
                if (suffix.LBRACK() != null)
                {
                    if (requestedIndex.HasValue)
                    {
                        throw new InvalidOperationException($"Variable '{name}' is used with multiple indices; only one dimension is supported.");
                    }

                    var rawIndex = Visit(suffix.expr());
                    requestedIndex = NormalizeIndex(rawIndex, name);
                }
                else if (suffix.DOT() != null)
                {
                    requestedUnit = suffix.IDENT().GetText();
                }
            }

            var hasVector = measuredValue.Values is not null && measuredValue.Values.Count > 0;
            double extractedValue;

            if (hasVector)
            {
                if (!requestedIndex.HasValue)
                {
                    throw new InvalidOperationException($"Variable '{name}' is non-scalar. Use an index like '${name}[i]'.");
                }

                if (requestedIndex.Value >= measuredValue.Values!.Count)
                {
                    throw new InvalidOperationException($"Index {requestedIndex.Value} is out of range for variable '{name}'.");
                }

                extractedValue = measuredValue.Values[requestedIndex.Value];
            }
            else
            {
                if (!measuredValue.Value.HasValue)
                {
                    throw new InvalidOperationException($"Variable '{name}' has no value defined.");
                }

                if (requestedIndex.HasValue)
                {
                    throw new InvalidOperationException($"Variable '{name}' is scalar but used with an index.");
                }

                extractedValue = measuredValue.Value.Value;
            }

            if (requestedUnit is null)
            {
                return extractedValue;
            }

            var fromUnit = (measuredValue.Unit ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fromUnit))
            {
                throw new InvalidOperationException($"Variable '{name}' has no unit defined but is used with '.{requestedUnit}'.");
            }

            if (!UnitResolver.TryConvert(extractedValue, fromUnit, requestedUnit, out var converted))
            {
                throw new InvalidOperationException($"Cannot convert variable '{name}' from '{fromUnit}' to '{requestedUnit}'.");
            }

            return converted;
        }

        public override double VisitConstPrimary([NotNull] FormulaParser.ConstPrimaryContext context)
        {
            var name = context.constRef().IDENT().GetText();
            if (!_constants.TryGetValue(name, out var c))
                throw new InvalidOperationException($"Undefined constant: #{name}");
            return c.Value;
        }

        public override double VisitFuncPrimary([NotNull] FormulaParser.FuncPrimaryContext context)
        {
            var f = context.funcCall();
            var fname = f.IDENT().GetText();

            if (!FunctionRegistry.Functions.TryGetValue(fname, out var impl))
                throw new InvalidOperationException($"Unknown function: {fname}");

            var argExprs = f.expr();
            var args = new double[argExprs.Length];
            for (int i = 0; i < argExprs.Length; i++)
                args[i] = Visit(argExprs[i]);

            return impl(args);
        }

        public override double VisitParenPrimary([NotNull] FormulaParser.ParenPrimaryContext context)
            => Visit(context.expr());

        public override double VisitUnaryPlus([NotNull] FormulaParser.UnaryPlusContext context)
            => +Visit(context.unary());

        public override double VisitUnaryMinus([NotNull] FormulaParser.UnaryMinusContext context)
            => -Visit(context.unary());

        public override double VisitUnaryPrimary([NotNull] FormulaParser.UnaryPrimaryContext context)
            => Visit(context.primary());

        public override double VisitAdd([NotNull] FormulaParser.AddContext context)
        {
            double value = Visit(context.mul(0));
            for (int i = 1; i < context.mul().Length; i++)
            {
                var rhs = Visit(context.mul(i));
                var op = context.GetChild(2 * i - 1).GetText();
                value = op == "+" ? value + rhs : value - rhs;
            }
            return value;
        }

        public override double VisitMul([NotNull] FormulaParser.MulContext context)
        {
            double value = Visit(context.pow(0));
            for (int i = 1; i < context.pow().Length; i++)
            {
                var rhs = Visit(context.pow(i));
                var op = context.GetChild(2 * i - 1).GetText();
                value = op switch
                {
                    "*" => value * rhs,
                    "/" => value / rhs,
                    "%" => value % rhs,          // modulo
                    _ => throw new InvalidOperationException($"Unknown operator: {op}")
                };
            }
            return value;
        }

        public override double VisitPow([NotNull] FormulaParser.PowContext context)
        {
            // Left-associative ^ is fine for typical inputs (2^3). If you want right-associative, evaluate from right.
            double value = Visit(context.unary(0));
            for (int i = 1; i < context.unary().Length; i++)
            {
                var rhs = Visit(context.unary(i));
                value = Math.Pow(value, rhs);
            }
            return value;
        }

        public override double VisitCmp([NotNull] FormulaParser.CmpContext context)
        {
            double value = Visit(context.add(0));

            // If there's no comparator, return arithmetic result.
            if (context.add().Length == 1) return value;

            // Evaluate left-to-right; each comparison yields 1 or 0 (truthy/falsy).
            for (int i = 1; i < context.add().Length; i++)
            {
                var rhs = Visit(context.add(i));
                var op = context.GetChild(2 * i - 1).GetText();

                bool truth = op switch
                {
                    ">"  => value >  rhs,
                    "<"  => value <  rhs,
                    ">=" => value >= rhs,
                    "<=" => value <= rhs,
                    "==" => Math.Abs(value - rhs) < double.Epsilon,
                    "!=" => Math.Abs(value - rhs) >= double.Epsilon,
                    _    => throw new InvalidOperationException($"Unknown comparator: {op}")
                };

                value = truth ? 1.0 : 0.0;
            }

            return value;
        }
    }
}
