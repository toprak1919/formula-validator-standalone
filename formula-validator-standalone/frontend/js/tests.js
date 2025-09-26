import { state, resetConstantsToBackend, applyConstantOverrides, getConstantOverrides } from './state.js';
import { showToast } from './ui.js';

function renderTestResult(result) {
  const container = document.getElementById('testResults');
  if (!container) return;
  const item = document.createElement('div');
  item.className = `test-result ${result.passed ? 'pass' : 'fail'}`;
  item.innerHTML = `
    <div class="test-info">
      <div class="test-name">${result.name}</div>
      <div class="test-formula">${result.formula}</div>
    </div>
    <div class="test-status">
      <div class="test-expected">
        Expected: ${result.expected}<br>
        Actual: ${result.actual}
      </div>
      <div class="test-icon ${result.passed ? 'pass' : 'fail'}"></div>
    </div>
  `;
  container.appendChild(item);
}

function updateTestProgress(current, total, results) {
  const progress = (current / total) * 100;
  const progressEl = document.getElementById('testProgress');
  const progressTextEl = document.getElementById('testProgressText');
  const statsEl = document.getElementById('testStats');

  if (progressEl) progressEl.style.width = `${progress}%`;
  if (progressTextEl) progressTextEl.textContent = `${current} / ${total} tests`;

  const passed = results.filter(r => r.passed).length;
  const failed = results.filter(r => !r.passed).length;
  if (statsEl) statsEl.textContent = `Pass: ${passed} | Fail: ${failed}`;
}

async function runSingleTest(testCase) {
  const requestData = {
    formula: testCase.formula,
    measuredValues: Object.entries(state.measuredValues).map(([id, data]) => {
      const entry = {
        id,
        name: data.name
      };

      if (Array.isArray(data.values) && data.values.length > 0) {
        const vectorValues = data.values
          .map((value) => (typeof value === 'number' ? value : Number.parseFloat(value)))
          .filter((value) => !Number.isNaN(value));
        if (vectorValues.length > 0) {
          entry.values = vectorValues;
        }
      } else if (data.value !== undefined && data.value !== null) {
        const numericValue = typeof data.value === 'number'
          ? data.value
          : Number.parseFloat(data.value);
        if (!Number.isNaN(numericValue)) {
          entry.value = numericValue;
        }
      }

      if (data.unit) {
        entry.unit = data.unit;
      }

      return entry;
    }),
    constants: getConstantOverrides()
  };

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
    const validationResult = data.data.validateFormula;

    if (testCase.expectedError) {
      return {
        ...testCase,
        passed: !validationResult.isValid,
        actual: validationResult.error || 'No error',
        expected: testCase.expectedError
      };
    }

    const passed = validationResult.isValid &&
      Math.abs(validationResult.result - testCase.expectedResult) < 0.001;

    return {
      ...testCase,
      passed,
      actual: validationResult.result,
      expected: testCase.expectedResult
    };
  } catch (error) {
    return {
      ...testCase,
      passed: false,
      actual: 'Network error',
      expected: testCase.expectedResult || testCase.expectedError
    };
  }
}

export async function loadTestCases() {
  try {
    const response = await fetch('test-cases.json');
    const data = await response.json();
    state.testCases = data.testCases || [];

    if (data.measuredValues) {
      state.measuredValues = data.measuredValues;
    }
    if (data.constants) {
      resetConstantsToBackend();
      applyConstantOverrides(data.constants);
    } else {
      resetConstantsToBackend();
    }
  } catch (error) {
    console.error('Failed to load test cases:', error);
    state.testCases = [];
  }
}

export async function runTests() {
  if (state.testRunning) return;
  state.testRunning = true;
  state.currentTestIndex = 0;

  const results = [];
  const testSpeedSelect = document.getElementById('testSpeed');
  const testSpeed = testSpeedSelect ? testSpeedSelect.value : 'normal';
  const delay = testSpeed === 'instant' ? 0 : testSpeed === 'fast' ? 100 : 500;

  const resultsContainer = document.getElementById('testResults');
  if (resultsContainer) resultsContainer.innerHTML = '';

  const runBtn = document.getElementById('runTestsBtn');
  if (runBtn) runBtn.disabled = true;

  for (let i = 0; i < state.testCases.length; i++) {
    const testCase = state.testCases[i];
    const result = await runSingleTest(testCase);
    results.push(result);

    updateTestProgress(i + 1, state.testCases.length, results);
    renderTestResult(result);

    if (delay > 0) {
      await new Promise(resolve => setTimeout(resolve, delay));
    }
  }

  state.testRunning = false;
  if (runBtn) runBtn.disabled = false;

  const passed = results.filter(r => r.passed).length;
  const failed = results.filter(r => !r.passed).length;
  showToast(`Test suite completed: ${passed} passed, ${failed} failed`, failed > 0 ? 'error' : 'success');
}

export function exportTestResults() {
  const rows = Array.from(document.querySelectorAll('.test-result')).map(el => {
    const name = el.querySelector('.test-name')?.textContent || '';
    const formula = el.querySelector('.test-formula')?.textContent || '';
    const expected = el.querySelector('.test-expected')?.textContent || '';
    const passed = el.classList.contains('pass');
    return { name, formula, expected, passed };
  });

  const csv = 'Name,Formula,Expected,Passed\n' +
    rows.map(r => `"${r.name}","${r.formula}","${r.expected}",${r.passed}`).join('\n');

  const blob = new Blob([csv], { type: 'text/csv' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'test-results.csv';
  a.click();
  URL.revokeObjectURL(url);

  showToast('Test results exported', 'success');
}
