# Formula Validator with GraphQL Backend

This is a GraphQL-enabled version of the Formula Editor that performs validation both on the frontend and backend simultaneously.

## Structure

- **backend/** - .NET 8 GraphQL API for formula validation
- **frontend/** - Modified HTML application that sends validation requests to the backend

## Features

- **Dual Validation**: Formulas are validated both on the frontend (JavaScript) and backend (.NET)
- **GraphQL API**: Modern GraphQL endpoint for formula validation
- **Fallback Support**: If the backend is unavailable, the frontend validation continues to work
- **Real-time Feedback**: Shows which validation source (frontend/backend) is being used

## Getting Started

### Backend Setup

1. Install .NET SDK 8.0 or later
2. Navigate to the backend directory:
   ```bash
   cd backend/FormulaValidatorAPI
   ```
3. Run the application:
   ```bash
   dotnet run
   ```
4. The API will be available at `http://localhost:5000`
5. GraphQL endpoint: `http://localhost:5000/graphql`

### Frontend Setup

1. Open `frontend/index.html` in a web browser
2. The application will automatically attempt to connect to the backend at `http://localhost:5000`
3. If the backend is unavailable, it will fall back to frontend-only validation

## How It Works

When you click the "Evaluate" button:

1. The frontend prepares the formula and data sources
2. Sends a GraphQL mutation to the backend for validation
3. The backend performs its own validation logic (.NET)
4. Results are displayed with the source (Backend/Frontend)
5. If the backend is unavailable, frontend validation is used as fallback

## Validation Rules (Backend)

The .NET backend validates:
- Missing operators between variables/numbers
- Double operators (++, --, **, //)
- Leading operators (except minus)
- Trailing operators
- Empty parentheses
- Unbalanced parentheses
- Invalid variable syntax ($variable)
- Invalid constant syntax (#constant)
- Undefined variables and constants
- Undefined functions

## Sample GraphQL Mutation

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

With variables:
```json
{
  "request": {
    "formula": "$measuredValue_1 + #constantsSqrtTwo",
    "measuredValues": [
      { "id": "measuredValue_1", "name": "$measuredValue_1", "value": 10.5 }
    ],
    "constants": [
      { "id": "constantsSqrtTwo", "name": "#constantsSqrtTwo", "value": 1.414 }
    ]
  }
}
```

## Development Notes

- CORS is configured to allow all origins in development
- The backend uses HotChocolate GraphQL library
- Frontend falls back gracefully when backend is unavailable
- Both validation systems can work independently