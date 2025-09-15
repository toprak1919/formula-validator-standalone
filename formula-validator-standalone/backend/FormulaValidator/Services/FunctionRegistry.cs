using System.Globalization;

namespace FormulaValidator.Services
{
    public static class FunctionRegistry
    {
        public delegate double Variadic(params double[] args);

        public static readonly IReadOnlyDictionary<string, Variadic> Functions =
            new Dictionary<string, Variadic>(StringComparer.OrdinalIgnoreCase)
            {
                // Trig (radians)
                ["sin"] = a => Math.Sin(One(a)),
                ["cos"] = a => Math.Cos(One(a)),
                ["tan"] = a => Math.Tan(One(a)),

                // Logs & exp
                ["ln"] = a => Math.Log(One(a)),
                ["log10"] = a => Math.Log10(One(a)),
                ["log2"] = a => Math.Log(One(a), 2.0),
                ["exp"] = a => Math.Exp(One(a)),

                // Roots / power
                ["sqrt"] = a => Math.Sqrt(One(a)),
                ["pow"]  = a => Math.Pow(Arg(a,0), Arg(a,1)),

                // Rounding
                ["floor"] = a => Math.Floor(One(a)),
                ["ceil"]  = a => Math.Ceiling(One(a)),
                ["round"] = a => a.Length == 1
                                ? Math.Round(a[0], 0, MidpointRounding.AwayFromZero)
                                : Math.Round(a[0], (int)a[1], MidpointRounding.AwayFromZero),

                // Abs / sign
                ["abs"]  = a => Math.Abs(One(a)),
                ["sign"] = a => Math.Sign(One(a)),
                ["sgn"]  = a => Math.Sign(One(a)),

                // Aggregates
                ["min"]  = a => a.Min(),
                ["max"]  = a => a.Max(),
                ["sum"]  = a => a.Sum(),
                ["mean"] = a => a.Average(),
                ["avg"]  = a => a.Average(), // alias

                // Conditional
                // if(cond, thenVal, elseVal) -- nonzero cond is true
                ["if"]   = a => ToBool(Arg(a,0)) ? Arg(a,1) : Arg(a,2),

                // Modulo as function too
                ["mod"]  = a => Arg(a,0) % Arg(a,1),

                // Extras shown in your UI (basic)
                ["fact"] = a => Factorial(One(a)),
                ["gcd"]  = a => Gcd((long)Arg(a,0), (long)Arg(a,1)),
                ["lcm"]  = a => Lcm((long)Arg(a,0), (long)Arg(a,1)),
            };

        private static double One(double[] a)
        {
            if (a.Length != 1) throw new ArgumentException("Function expects 1 argument.");
            return a[0];
        }

        private static double Arg(double[] a, int i)
        {
            if (i >= a.Length) throw new ArgumentException($"Function expects at least {i+1} arguments.");
            return a[i];
        }

        private static bool ToBool(double v) => Math.Abs(v) > double.Epsilon;

        private static double Factorial(double x)
        {
            // integer factorial only
            var n = (long)Math.Round(x);
            if (n < 0) throw new ArgumentException("Factorial undefined for negative numbers.");
            long acc = 1;
            for (long i = 2; i <= n; i++) acc *= i;
            return acc;
        }

        private static long Gcd(long a, long b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) { var t = b; b = a % b; a = t; }
            return a;
        }

        private static long Lcm(long a, long b)
        {
            if (a == 0 || b == 0) return 0;
            return Math.Abs(a / Gcd(a, b) * b);
        }
    }
}

