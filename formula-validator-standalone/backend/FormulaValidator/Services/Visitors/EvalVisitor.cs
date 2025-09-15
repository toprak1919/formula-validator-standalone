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
        public readonly HashSet<string> Constants = new(StringComparer.OrdinalIgnoreCase);

        public override object? VisitVarRef([NotNull] FormulaParser.VarRefContext context)
        {
            var name = context.IDENT(0).GetText();
            Variables.Add(name);

            // Has ".unit" ?
            if (context.IDENT().Length == 2)
            {
                VariablesWithUnit.Add(name);
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
            _measured = mvs.ToDictionary(m => TrimDollar(m.Id), StringComparer.OrdinalIgnoreCase);
            _constants = consts.ToDictionary(c => TrimHash(c.Id), StringComparer.OrdinalIgnoreCase);
        }

        private static string TrimDollar(string id) => id.StartsWith("$") ? id[1..] : id;
        private static string TrimHash(string id) => id.StartsWith("#") ? id[1..] : id;

        public override double VisitFormula([NotNull] FormulaParser.FormulaContext context)
            => Visit(context.expr());

        public override double VisitNumberPrimary([NotNull] FormulaParser.NumberPrimaryContext context)
        {
            var txt = context.NUMBER().GetText();
            return double.Parse(txt, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public override double VisitVarPrimary([NotNull] FormulaParser.VarPrimaryContext context)
        {
            var v = context.varRef();
            var name = v.IDENT(0).GetText();

            if (!_measured.TryGetValue(name, out var mv))
                throw new InvalidOperationException($"Undefined variable: ${name}");

            // no unit suffix -> plain value
            if (v.IDENT().Length == 1) return mv.Value;

            // has ".unit" -> convert using UnitsNet
            var targetUnit = v.IDENT(1).GetText();
            var fromUnit = (mv.Unit ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(fromUnit))
                throw new InvalidOperationException($"Variable '{name}' has no unit defined but is used with '.{targetUnit}'.");

            if (!UnitResolver.TryConvert(mv.Value, fromUnit, targetUnit, out var converted))
                throw new InvalidOperationException($"Cannot convert variable '{name}' from '{fromUnit}' to '{targetUnit}'.");

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

