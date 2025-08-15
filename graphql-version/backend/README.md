# Formula Validator GraphQL API

## Setup

1. Install .NET SDK 8.0 or later
2. Navigate to the backend directory
3. Run the application:
   ```bash
   cd FormulaValidatorAPI
   dotnet run
   ```

## GraphQL Playground

When running in development mode, access the GraphQL Playground at:
http://localhost:5000/playground

## Sample Queries

### Validate Formula (Mutation)

```graphql
mutation ValidateFormula {
  validateFormula(request: {
    formula: "$measuredValue_1 + #constantsSqrtTwo * 2",
    measuredValues: [
      { id: "measuredValue_1", name: "$measuredValue_1", value: 10.5 }
    ],
    constants: [
      { id: "constantsSqrtTwo", name: "#constantsSqrtTwo", value: 1.414 }
    ]
  }) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

### Hello Query (Test)

```graphql
query {
  hello
}
```

## Validation Rules

The backend validates:
- Missing operators between variables/numbers
- Double operators
- Leading/trailing operators
- Empty parentheses
- Unbalanced parentheses
- Invalid variable/constant syntax
- Undefined variables/constants
- Undefined functions

## CORS

CORS is configured to allow all origins in development mode.