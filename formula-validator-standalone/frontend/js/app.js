import { state, setEditor, setValidationTimeout } from './state.js';
import { createCustomCompleter } from './autocomplete.js';
import {
  renderMeasuredValues,
  renderConstants,
  renderFunctions,
  renderHistory,
  showToast,
  updateValidationStatus,
  setValidationCallback,
  setupSourceSearch,
  setupAddSourceButtons
} from './ui.js';
import { performBackendValidation } from './validation.js';
import { loadTestCases, runTests, exportTestResults } from './tests.js';

function initializeEditor() {
  const editor = ace.edit('editor');
  editor.setTheme('ace/theme/chrome');
  editor.session.setMode('ace/mode/javascript');

  ace.require('ace/ext/language_tools');
  editor.setOptions({
    fontSize: '15px',
    showGutter: true,
    highlightActiveLine: true,
    wrap: true,
    enableBasicAutocompletion: true,
    enableSnippets: true,
    enableLiveAutocompletion: true,
    showPrintMargin: false
  });

  editor.completers = [createCustomCompleter()];

  editor.commands.on('afterExec', function (e) {
    if (!e || e.command.name !== 'insertstring') return;
    if (['.', '$', '#', '('].includes(e.args)) {
      editor.execCommand('startAutocomplete');
    }
  });

  editor.setValue('($temperature * #conversion_factor) + 32', 1);

  editor.session.on('change', () => {
    if (state.validationTimeout) {
      clearTimeout(state.validationTimeout);
    }
    updateValidationStatus('validating');
    const timeout = setTimeout(() => performBackendValidation(), 300);
    setValidationTimeout(timeout);
  });

  setEditor(editor);
}

function simulateDataChange() {
  const keys = Object.keys(state.measuredValues);
  if (!keys.length) return;
  const randomKey = keys[Math.floor(Math.random() * keys.length)];
  const oldValue = state.measuredValues[randomKey].value;
  const newValue = oldValue + (Math.random() - 0.5) * 10;

  state.measuredValues[randomKey].value = parseFloat(newValue.toFixed(2));
  renderMeasuredValues();

  showToast(`${state.measuredValues[randomKey].name} changed from ${oldValue.toFixed(2)} to ${newValue.toFixed(2)}`, 'info');
  performBackendValidation();
}

function setupClipboardButtons() {
  const copyFormulaBtn = document.getElementById('copyFormulaBtn');
  if (copyFormulaBtn) {
    copyFormulaBtn.addEventListener('click', () => {
      const formula = state.editor ? state.editor.getValue() : '';
      navigator.clipboard.writeText(formula);
      showToast('Formula copied to clipboard', 'success');
    });
  }

  const copyResultBtn = document.getElementById('copyResultBtn');
  const resultCopyBtn = document.getElementById('resultCopyBtn');

  const copyResult = () => {
    const result = document.getElementById('resultValue')?.textContent;
    if (result && result !== 'Enter a valid formula...') {
      navigator.clipboard.writeText(result);
      showToast('Result copied', 'success');
    }
  };

  if (copyResultBtn) copyResultBtn.addEventListener('click', copyResult);
  if (resultCopyBtn) resultCopyBtn.addEventListener('click', copyResult);
}

function setupThemeToggle() {
  const themeToggleBtn = document.getElementById('themeToggleBtn');
  if (!themeToggleBtn) return;

  themeToggleBtn.addEventListener('click', () => {
    const current = document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    const next = current === 'dark' ? 'light' : 'dark';
    applyTheme(next);
    localStorage.setItem('theme', next);
  });
}

function applyTheme(theme) {
  document.documentElement.setAttribute('data-theme', theme);
  const iconBtn = document.getElementById('themeToggleBtn');
  if (iconBtn) {
    const iconSpan = iconBtn.querySelector('.icon');
    if (iconSpan) {
      iconSpan.className = `icon ${theme === 'dark' ? 'icon-moon' : 'icon-sun'}`;
    }
  }

  if (state.editor) {
    const aceTheme = theme === 'dark' ? 'ace/theme/tomorrow_night' : 'ace/theme/chrome';
    state.editor.setTheme(aceTheme);
    setTimeout(() => {
      try {
        state.editor.renderer.updateFull(true);
        state.editor.resize(true);
      } catch (err) {
        /* noop */
      }
    }, 0);
  }
}

function setupToolbarButtons() {
  const validateBtn = document.getElementById('validateBtn');
  if (validateBtn) {
    validateBtn.addEventListener('click', () => performBackendValidation({ allowToast: true, focusError: true }));
  }

  const clearBtn = document.getElementById('clearBtn');
  if (clearBtn) {
    clearBtn.addEventListener('click', () => {
      if (!state.editor) return;
      state.editor.setValue('', 1);
      showToast('Editor cleared', 'info');
    });
  }

  const samplesBtn = document.getElementById('samplesBtn');
  if (samplesBtn) {
    samplesBtn.addEventListener('click', () => {
      const formula = state.sampleFormulas[Math.floor(Math.random() * state.sampleFormulas.length)];
      if (state.editor) state.editor.setValue(formula, 1);
      showToast('Sample formula inserted', 'info');
    });
  }

  const simulateBtn = document.getElementById('simulateChangeBtn');
  if (simulateBtn) simulateBtn.addEventListener('click', simulateDataChange);

  const testSuiteBtn = document.getElementById('testSuiteBtn');
  if (testSuiteBtn) {
    testSuiteBtn.addEventListener('click', () => {
      document.getElementById('testModal')?.classList.add('show');
    });
  }

  const closeTestModalBtn = document.getElementById('closeTestModal');
  if (closeTestModalBtn) {
    closeTestModalBtn.addEventListener('click', () => {
      document.getElementById('testModal')?.classList.remove('show');
    });
  }

  const runTestsBtn = document.getElementById('runTestsBtn');
  if (runTestsBtn) runTestsBtn.addEventListener('click', runTests);

  const exportTestsBtn = document.getElementById('exportTestsBtn');
  if (exportTestsBtn) exportTestsBtn.addEventListener('click', exportTestResults);

  const testModal = document.getElementById('testModal');
  if (testModal) {
    testModal.addEventListener('click', (e) => {
      if (e.target.id === 'testModal') {
        testModal.classList.remove('show');
      }
    });
  }
}

function setupResultPanel() {
  const resultValue = document.getElementById('resultValue');
  if (resultValue) resultValue.textContent = 'Enter a formula...';
}

function setupValidationCallback() {
  setValidationCallback(() => performBackendValidation());
}

function setupTabs() {
  document.querySelectorAll('.tab').forEach(tab => {
    tab.addEventListener('click', () => {
      const tabName = tab.dataset.tab;
      const parent = tab.parentElement.parentElement;

      parent.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
      tab.classList.add('active');

      parent.querySelectorAll('.tab-content').forEach(content => {
        content.classList.remove('active');
      });

      const contentId = `${tabName}Tab`;
      const content = document.getElementById(contentId);
      if (content) content.classList.add('active');
    });
  });
}

document.addEventListener('DOMContentLoaded', async () => {
  initializeEditor();
  setupValidationCallback();

  renderMeasuredValues();
  renderConstants();
  renderFunctions();
  renderHistory();
  setupSourceSearch();
  setupAddSourceButtons();
  setupClipboardButtons();
  setupToolbarButtons();
  setupThemeToggle();
  setupTabs();
  setupResultPanel();

  const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
  const savedTheme = localStorage.getItem('theme');
  applyTheme(savedTheme || (prefersDark ? 'dark' : 'light'));

  await loadTestCases();
  renderMeasuredValues();
  renderConstants();

  setTimeout(() => performBackendValidation(), 100);
});
