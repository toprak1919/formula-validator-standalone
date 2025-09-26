Formula Validator DSL – Backend Internals

Overview
- Grammar lives in `Parsing/Formula.g4` (ANTLR4).
- MSBuild generates C# lexer/parser/visitors at build time.
- `Services/Visitors/EvalVisitor.cs` contains:
  - `SymbolCollector` to find `$vars` and `#consts` before evaluation
  - `EvalVisitor` to evaluate expressions (numbers, vars/consts, functions, operators, comparisons)
- `Services/FunctionRegistry.cs` defines available functions.
- `Services/UnitResolver.cs` converts units via UnitsNet (Length demo).
- `Services/FormulaValidationService.cs` wires parse → collect → evaluate and shapes errors.

DSL Reference
- Numbers: `123`, `3.14`, `1e-3`.
- Variables: `$name`, `$name.unit`, `$name[index]`, `$name[index].unit` (unit alias is case-insensitive, index is zero-based).
- Constants: `#name`.
- Functions (case-insensitive): `sin, cos, tan, ln, log10, log2, exp, sqrt, pow, floor, ceil, round, abs, sign|sgn, min, max, sum, mean|avg, if, mod, fact, gcd, lcm`.
- Operators (high → low): `^` | `* / %` | `+ -` | `>, <, >=, <=, ==, !=`.
- Parentheses: `( expr )`.
- Truth: comparisons yield `1` (true) or `0` (false). `if(cond, a, b)` treats nonzero as true.

Units
- Implemented via UnitsNet for Length with common aliases:
  - m, meter, metre
  - km, kilometer, kilometre
  - au, astronomical, astronomical_unit, astronomicalunit
- Usage: if `$d` is provided with a unit, `$d.km` converts to kilometers. If `$d` has no unit but a suffix is used, validation fails.
- Extend by adding more alias maps and quantities (e.g., Mass, Temperature) in `UnitResolver`.

Grammar Notes
- File: `Parsing/Formula.g4`.
- Primary forms: number, variable ref, constant ref, function call, parenthesized expression.
- `varRef`: `$IDENT` optionally followed by `.[unit]` and/or `[expr]` suffixes (indexes are zero-based and must resolve to whole numbers).
- `%` is modulo; comparisons are allowed anywhere an expression appears.
- Current `^` evaluation is left-associative by choice. For right-associative power, evaluate from right to left in `VisitPow`.

Error Handling
- `CollectingErrorListener` captures the first syntax error and formats it as "Syntax error near 'token' at [line x, col y]".
- Semantic errors thrown during evaluation become user-friendly messages (undefined variable/constant, unknown function, bad conversion, missing unit for suffix, division by zero leading to Infinity/NaN, etc.).

Build & Run
- From this folder:
  - `dotnet restore`
  - `PORT=5001 dotnet run`
- GraphQL Playground: `http://localhost:$PORT/graphql`.
- If port is busy, change `PORT` or stop the other process.

ANTLR Tooling
- Codegen: MSBuild `Antlr4` item in the project triggers generation from `Parsing/Formula.g4`.
- Generator/runtime versions are aligned in the project file.
- If you upgrade ANTLR, keep versions compatible (generator jar and runtime should match series).

Extending
- Add functions: update `FunctionRegistry.Functions`.
- Add units: extend `UnitResolver` with new quantity alias maps and conversions via UnitsNet.
- Change syntax: edit `Formula.g4`, then adapt `EvalVisitor`/`SymbolCollector`.
- Non-scalar variables: provide `values` (an array of numbers) instead of `value` in `MeasuredValue`; formulas must access them via `$name[index]` (e.g., `$temps[0]`). Mixing indexed and non-indexed usage of the same variable is rejected.
