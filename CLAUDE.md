# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a single-file web application - a Formula Editor with GraphQL spellcheck functionality. The entire application is contained within `index.html`, which includes HTML, CSS, and JavaScript.

## Architecture

### File Structure
- `index.html` - Complete application including:
  - HTML structure for the UI
  - Inline CSS with a green theme design
  - JavaScript code for all functionality
  - External dependencies loaded via CDN

### Key Components

1. **Editor**: Uses ACE editor for formula editing with syntax highlighting
2. **Formula Validation**: Custom validation logic with math.js integration
3. **Test Suite**: Built-in test runner for edge case validation
4. **Data Sources**: Support for measured values ($) and constants (#)

### External Dependencies (CDN)
- ACE Editor (v1.32.2) - Code editor
- math.js (v12.2.1) - Formula parsing and evaluation
- expr-eval (v2.0.2) - Expression evaluation
- Google Fonts (Inter, JetBrains Mono)

## Development Notes

### Running the Application
Open `index.html` directly in a web browser. No build process or server required.

### Key Functions
- `validateFormula()` - Main validation logic (index.html:1716)
- `evaluateFormula()` - Formula evaluation handler (index.html:1962)
- `runTestSuite()` - Test suite runner (index.html:2147)
- `prepareFormulaForMathJS()` - Formula preprocessing (index.html:1648)

### Test Suite
The application includes a comprehensive test suite with 60+ edge cases covering:
- Basic operations
- Variable/constant handling
- Error conditions
- Syntax validation

Access via the "Run Test Suite" button in the UI.

### Validation Rules
- Variables must start with `$` followed by valid identifier
- Constants must start with `#` followed by valid identifier
- Operators cannot be standalone or trailing
- Parentheses must be balanced and non-empty
- Functions require parentheses with arguments