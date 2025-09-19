import { state, clearValidationTimeout, getConstantOverrides, updateSavedFormulaResult } from './state.js';
import {
  updateValidationStatus,
  showErrors,
  updateResult,
  updateBackendIndicator,
  addToHistory,
  renderSavedFormulas
} from './ui.js';

function buildRequestPayload(formulaText) {
  const formulaValue = typeof formulaText === 'string'
    ? formulaText
    : (state.editor ? state.editor.getValue() : '');

  const payload = {
    formula: formulaValue,
    measuredValues: Object.entries(state.measuredValues).map(([id, data]) => ({
      id,
      name: data.name,
      value: data.value,
      unit: data.unit
    }))
  };

  const constantOverrides = getConstantOverrides();
  payload.constants = constantOverrides;

  return payload;
}

function updateSavedFormulaResultByFormula(formula, result) {
  const match = state.savedFormulas.find(entry => entry.formula === formula);
  if (match) {
    updateSavedFormulaResult(match.id, result);
    renderSavedFormulas();
  }
}

function normalizeFormulaSymbols(formula, caretIndex = null) {
  let newCaretIndex = caretIndex ?? null;

  const normalized = formula.replace(/([#$])\1+(?=[a-zA-Z_])/g, (match, symbol, offset) => {
    if (newCaretIndex !== null && newCaretIndex > offset) {
      const reduction = match.length - 1;
      const removalBeforeCaret = Math.min(reduction, newCaretIndex - offset);
      if (removalBeforeCaret > 0) {
        newCaretIndex = Math.max(0, newCaretIndex - removalBeforeCaret);
      }
    }
    return symbol;
  });

  return {
    normalized,
    newCaretIndex: newCaretIndex ?? null
  };
}

export async function performBackendValidation({ allowToast = false, focusError = false } = {}) {
  if (!state.editor) return;
  const originalFormula = state.editor.getValue();

  clearValidationTimeout();

  const cursorPosition = state.editor.getCursorPosition();
  const cursorIndex = state.editor.session.doc.positionToIndex(cursorPosition);
  const { normalized: normalizedFormula, newCaretIndex } = normalizeFormulaSymbols(originalFormula, cursorIndex);

  if (normalizedFormula !== originalFormula) {
    const scrollTop = state.editor.session.getScrollTop();
    const Range = ace.require('ace/range').Range;
    const lastRow = state.editor.session.getLength() - 1;
    const lastCol = lastRow >= 0 ? state.editor.session.getLine(lastRow).length : 0;
    state.editor.session.replace(new Range(0, 0, Math.max(lastRow, 0), lastCol), normalizedFormula);
    if (newCaretIndex !== null) {
      const newCursorPos = state.editor.session.doc.indexToPosition(newCaretIndex);
      state.editor.moveCursorTo(newCursorPos.row, newCursorPos.column);
    }
    state.editor.session.setScrollTop(scrollTop);
  }

  if (!normalizedFormula || normalizedFormula.trim() === '') {
    updateValidationStatus('');
    showErrors([]);
    updateResult(null);
    return;
  }

  updateBackendIndicator(true, 'Validating...');

  const requestData = buildRequestPayload(normalizedFormula);

  try {
    const response = await fetch(window.API_CONFIG.URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        query: `
          mutation ValidateFormula($request: ValidationRequestInput!) {
            validateFormula(request: $request) {
              isValid
              error
              result
              evaluatedFormula
              source
            }
          }
        `,
        variables: {
          request: requestData
        }
      })
    });

    const data = await response.json();

    if (data.errors) {
      console.error('GraphQL errors:', data.errors);
      updateValidationStatus('error');
      showErrors(['Backend validation error']);
      updateResult(null);
      updateBackendIndicator(false, 'Error');
      return;
    }

    const validationResult = data.data.validateFormula;

    if (validationResult.isValid) {
      updateValidationStatus('valid');
      showErrors([]);
      updateResult(validationResult.result);
      addToHistory(normalizedFormula, validationResult.result);
      state.editor.session.clearAnnotations();
      updateSavedFormulaResultByFormula(normalizedFormula, validationResult.result);
    } else {
      updateValidationStatus('error');
      const error = validationResult.error || 'Invalid formula';
      showErrors([error]);
      updateResult(null);

      const locationMatch = error.match(/line\s+(\d+),\s*col(?:umn)?\s+(\d+)/i);
      const row = locationMatch ? Math.max(parseInt(locationMatch[1], 10) - 1, 0) : 0;
      const column = locationMatch ? Math.max(parseInt(locationMatch[2], 10) - 1, 0) : 0;
      state.editor.session.setAnnotations([{
        row,
        column,
        text: error,
        type: 'error'
      }]);

      if (focusError && locationMatch) {
        state.editor.scrollToLine(row, true, true, function () {});
        state.editor.gotoLine(row + 1, column, true);
      }

      if (allowToast) {
        // Optional future toast messaging
      }
    }

    updateBackendIndicator(false, 'Ready');

  } catch (error) {
    console.error('Validation error:', error);
    updateValidationStatus('error');
    showErrors(['Failed to connect to backend']);
    updateResult(null);
    updateBackendIndicator(false, 'Offline');
  }
}
