# Formula Validator – Custom DSL (ANTLR4 + UnitsNet)

A real-time formula validation and evaluation system with a purpose-built DSL, parsed by ANTLR4 and evaluated in C#. Units are handled via UnitsNet. The frontend and GraphQL API remain unchanged.

## Features

- Real grammar (ANTLR4) with clear precedence and associativity
- Friendly syntax errors (token + line/column)
- Variables `$name` and constants `#name`
- Unit suffix on variables: `$name.unit` (e.g., `$d.km`, `$d.au`)
- Comparisons: `>, <, >=, <=, ==, !=` return 1 or 0
- Modulo operator `%` and function `mod(a,b)`
- Standard math functions + `if(cond, a, b)`

## Tech Stack

- Backend: .NET 8, C#, ANTLR4, UnitsNet, HotChocolate (GraphQL)
- Frontend: Vanilla JavaScript, ACE Editor

## DSL Overview

- Literals: numbers with optional decimals and scientific notation.
- Variables: `$foo` or `$foo.unit` (case-insensitive unit alias). Values come from `measuredValues`.
- Constants: `#PI`, `#gravity` from `constants`.
- Functions: `sin, cos, tan, ln, log10, log2, exp, sqrt, pow, floor, ceil, round, abs, sign|sgn, min, max, sum, mean|avg, if, mod, fact, gcd, lcm`.
- Operators (high → low):
  - `^` (power) [left-associative]
  - `* / %`
  - `+ -`
  - Comparisons: `>, <, >=, <=, ==, !=` (yield 1 or 0)

### Units (Length demo)
- Supported target suffixes on variables: `m|meter|metre`, `km|kilometer|kilometre`, `au|astronomical|astronomical_unit`.
- Example: given `{ id: "$d", value: 1000, unit: "meter" }`, `$d.km` evaluates to `1`.
- If a variable is used with a unit suffix but has no unit, validation fails.

### Examples
- `2 + 2 * 3`
- `if($t > #max, 1, 0)`
- `$d.km + 5`
- `mod(10, 3) + 10 % 4`

## How It Works

- Grammar: `backend/FormulaValidator/Parsing/Formula.g4`
- Parser/lexer generation at build (MSBuild Antlr4 item)
- Errors: `CollectingErrorListener` formats the first syntax error
- Evaluation: `EvalVisitor` walks the parse tree and computes the result
- Units: `UnitResolver` uses UnitsNet for conversions (length provided)
- Functions: `FunctionRegistry` provides built-ins; add more easily

See `backend/FormulaValidator/README.md` for a deeper dive and extension tips.

## Prerequisites

- .NET 8 SDK or later
- Modern web browser

## Run

Backend
- cd backend/FormulaValidator
- dotnet restore
- PORT=5001 dotnet run
- GraphQL: http://localhost:5001/graphql

Frontend
- Open `frontend/index.html`

## API

GraphQL Mutation
```
mutation ValidateFormula($request: ValidationRequestInput!) {
  validateFormula(request: $request) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

Variables example
```
{
  "request": {
    "formula": "if($d.km > #limit, 1, 0)",
    "measuredValues": [
      {"id": "$d", "name": "Distance", "value": 149597870700, "unit": "meter"}
    ],
    "constants": [
      {"id": "#limit", "name": "Limit (km)", "value": 100000}
    ]
  }
}
```

## Extending

- Units: add more quantities/aliases in `UnitResolver` (e.g., Mass, Temperature)
- Functions: add entries to `FunctionRegistry.Functions`
- Grammar: edit `Formula.g4` and rebuild; adjust visitors as needed

## License

MIT
