import { state, clearValidationTimeout } from './state.js';
import {
  updateValidationStatus,
  showErrors,
  updateResult,
  updateBackendIndicator,
  addToHistory
} from './ui.js';

function buildRequestPayload() {
  return {
    formula: state.editor ? state.editor.getValue() : '',
    measuredValues: Object.entries(state.measuredValues).map(([id, data]) => ({
      id,
      name: data.name,
      value: data.value,
      unit: data.unit
    })),
    constants: Object.entries(state.constants).map(([id, data]) => ({
      id,
      name: data.name,
      value: data.value
    }))
  };
}

export async function performBackendValidation({ allowToast = false, focusError = false } = {}) {
  if (!state.editor) return;
  const formula = state.editor.getValue();

  clearValidationTimeout();

  if (!formula || formula.trim() === '') {
    updateValidationStatus('');
    showErrors([]);
    updateResult(null);
    return;
  }

  updateBackendIndicator(true, 'Validating...');

  const requestData = buildRequestPayload();

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
      addToHistory(formula, validationResult.result);
      state.editor.session.clearAnnotations();
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
