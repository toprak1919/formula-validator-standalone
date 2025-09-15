# Formula Validator with NCalc + UnitsNet

A real-time formula validation and evaluation system using NCalc for math and UnitsNet for units on the backend with instant frontend feedback via GraphQL.

## Features

- ✅ **Real-time formula validation** - Instant feedback as you type
- ✅ **Full math expression evaluation** - Powered by NCalc
- ✅ **Variable support** - Use `$variable` syntax for measured values  
- ✅ **Constant support** - Use `#constant` syntax for constants
- ✅ **Math functions** - sqrt, sin, cos, log, exp, and many more
- ✅ **GraphQL API** - Modern API with playground
- ✅ **Debounced validation** - 300ms delay for optimal performance
- ✅ **Clean UI** - Simple, focused interface

## Tech Stack

- **Backend**: .NET 8, C#, NCalc, UnitsNet, HotChocolate (GraphQL)
- **Frontend**: Vanilla JavaScript, ACE Editor
- **Formula Engine**: NCalc + UnitsNet

## Prerequisites

- .NET 8 SDK or later
- Modern web browser

## Setup & Run

### Backend

1. Navigate to backend folder:
```bash
cd backend/FormulaValidator
```

2. Restore packages:
```bash
dotnet restore
```

3. Run the server:
```bash
dotnet run
```

The backend will start on http://localhost:5001
GraphQL Playground available at http://localhost:5001/graphql

### Frontend

1. Open `frontend/index.html` in your web browser
2. The UI will automatically connect to the backend on port 5001

## Usage

### Formula Examples

**Basic Math:**
- `2 + 2`
- `10 * (5 - 3)`
- `100 / 4`

**With Variables:**
- `$temperature * 1.8 + 32` (Celsius to Fahrenheit)
- `$pressure / 14.7` (PSI conversion)
- `$voltage * $current` (Power calculation)

**With Constants:**
- `#pi * $radius^2` (Circle area)
- `$mass * #gravity` (Weight calculation)

**Math Functions:**
- `sqrt($pressure)`
- `sin(#pi / 2)`
- `log(100) + exp(2)`
- `abs($temperature - #target_temp)`

**Unit-Suffixed Variables (length demo):**
- Provide a unit for a measured value (e.g., `{"id":"$foo","value":149597870700,"unit":"meter"}`)
- Use `$foo.meter`, `$foo.km`, or `$foo.astronomical` in formulas
- Supported aliases: `meter|m`, `kilometer|km`, `astronomical|au`
- Internally, `$var.unit` is translated to `toUnit('var','unit')` and evaluated via UnitsNet
- Example: with `$foo` in meters, `$foo.astronomical` converts to astronomical units (AU)

### Available Variables (in demo)

- `$temperature` - Temperature sensor (25.5)
- `$pressure` - Pressure sensor (101.3)
- `$humidity` - Humidity sensor (65.2)
- `$flow_rate` - Flow rate sensor (12.8)
- `$voltage` - Voltage sensor (220.0)

### Available Constants (in demo)

- `#pi` - Pi (3.14159)
- `#gravity` - Gravity (9.81)
- `#max_temp` - Max Temperature (100.0)
- `#min_temp` - Min Temperature (-10.0)
- `#conversion_factor` - Conversion Factor (1.8)

## API

### GraphQL Endpoint

**Mutation:**
```graphql
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

**Variables:**
```json
{
  "request": {
    "formula": "($temperature * #conversion_factor) + 32",
    "measuredValues": [
      {"id": "$temperature", "name": "Temperature", "value": 25.5},
      {"id": "$foo", "name": "Distance", "value": 149597870700, "unit": "meter"}
    ],
    "constants": [
      {"id": "#conversion_factor", "name": "Conversion Factor", "value": 1.8}
    ]
  }
}
```
Then formulas like `$foo.astronomical` evaluate to AU.

## Deployment

### Backend
1. Set the `PORT` environment variable if needed (default: 5001)
2. Deploy as a standard .NET application

### Frontend
1. Update `PRODUCTION_URL` in `frontend/config.js` with your backend URL
2. Serve the HTML file from any web server

## Functions

NCalc supports common math functions and operators. This project also wires a few helpers:
- `avg(...)` / `mean(...)` – average of arguments
- `mod(a, b)` – modulo operator (also `%` is translated to `mod()`)
- `if(cond, a, b)` – conditional
- `toUnit(name, unit)` – unit conversion using UnitsNet

## License

MIT
