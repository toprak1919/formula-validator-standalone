# GraphQL Playground Sample Queries

Copy and paste these queries into the HotChocolate GraphQL Playground at http://localhost:5232/graphql

## 1. Test Hello Query
```graphql
query TestConnection {
  hello
}
```

## 2. Simple Addition Formula
```graphql
mutation ValidateSimpleAddition {
  validateFormula(request: {
    formula: "5 + 3",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

## 3. Formula with Variables
```graphql
mutation ValidateWithVariables {
  validateFormula(request: {
    formula: "$measuredValue_1 + $measuredValue_2",
    measuredValues: [
      { id: "measuredValue_1", name: "$measuredValue_1", value: 10.5 },
      { id: "measuredValue_2", name: "$measuredValue_2", value: 20.3 }
    ],
    constants: []
  }) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

## 4. Formula with Constants
```graphql
mutation ValidateWithConstants {
  validateFormula(request: {
    formula: "#constantsSqrtTwo * 2",
    measuredValues: [],
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

## 5. Complex Formula with Variables and Constants
```graphql
mutation ValidateComplexFormula {
  validateFormula(request: {
    formula: "($measuredValue_1 + $measuredValue_2) * #constantsPi",
    measuredValues: [
      { id: "measuredValue_1", name: "$measuredValue_1", value: 10 },
      { id: "measuredValue_2", name: "$measuredValue_2", value: 20 }
    ],
    constants: [
      { id: "constantsPi", name: "#constantsPi", value: 3.14159 }
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

## 6. Formula with Function (Should Show Error - Functions Not Implemented)
```graphql
mutation ValidateWithFunction {
  validateFormula(request: {
    formula: "sqrt(16)",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

## 7. Invalid Formula - Missing Operator
```graphql
mutation ValidateMissingOperator {
  validateFormula(request: {
    formula: "5 5",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

## 8. Invalid Formula - Trailing Operator
```graphql
mutation ValidateTrailingOperator {
  validateFormula(request: {
    formula: "5 + 3 *",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

## 9. Invalid Formula - Empty Parentheses
```graphql
mutation ValidateEmptyParentheses {
  validateFormula(request: {
    formula: "5 + ()",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

## 10. Invalid Formula - Unbalanced Parentheses
```graphql
mutation ValidateUnbalancedParentheses {
  validateFormula(request: {
    formula: "((5 + 3)",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

## 11. Invalid Formula - Undefined Variable
```graphql
mutation ValidateUndefinedVariable {
  validateFormula(request: {
    formula: "$unknownVariable + 5",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

## 12. Invalid Formula - Double Operators
```graphql
mutation ValidateDoubleOperators {
  validateFormula(request: {
    formula: "5 ++ 3",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
    result
    evaluatedFormula
    source
  }
}
```

## 13. Test All Edge Cases
```graphql
mutation TestMultipleFormulas {
  valid1: validateFormula(request: {
    formula: "1 + 1",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
    evaluatedFormula
  }
  
  valid2: validateFormula(request: {
    formula: "$a + $b",
    measuredValues: [
      { id: "a", name: "$a", value: 5 },
      { id: "b", name: "$b", value: 10 }
    ],
    constants: []
  }) {
    isValid
    error
    evaluatedFormula
  }
  
  invalid1: validateFormula(request: {
    formula: "1 2",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
  }
  
  invalid2: validateFormula(request: {
    formula: "()",
    measuredValues: [],
    constants: []
  }) {
    isValid
    error
  }
}
```

## How to Use in Playground:

1. Open http://localhost:5232/graphql in your browser
2. Copy any of the queries above
3. Paste into the left panel of the playground
4. Click the "Play" button to execute
5. See the results in the right panel

## Variables Tab Usage:

For queries with variables, you can also use the Variables tab. For example:

**Query:**
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
    "formula": "$temp * 1.8 + 32",
    "measuredValues": [
      { "id": "temp", "name": "$temp", value: 25 }
    ],
    "constants": []
  }
}
```