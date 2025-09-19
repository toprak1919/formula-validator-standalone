import { state, isBackendConstant, getSavedFormulas, removeSavedFormula } from './state.js';
import { escapeHtml } from './utils.js';

let validationCallback = () => {};

let editorExpandedCallback = () => {};

export function setValidationCallback(callback) {
  validationCallback = typeof callback === 'function' ? callback : () => {};
}

export function setEditorExpandedCallback(callback) {
  editorExpandedCallback = typeof callback === 'function' ? callback : () => {};
}

export function showToast(message, type = 'info', duration = 3000) {
  const container = document.getElementById('toastContainer');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  toast.innerHTML = `
    <div class="toast-icon"></div>
    <div class="toast-message">${escapeHtml(message)}</div>
    <div class="toast-close" aria-label="Close"></div>
  `;

  container.appendChild(toast);

  const closeToast = () => {
    toast.style.animation = 'fadeOut 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  };

  toast.querySelector('.toast-close').addEventListener('click', closeToast);
  setTimeout(closeToast, duration);
}

export function updateValidationStatus(status) {
  const statusEl = document.getElementById('validationStatus');
  if (!statusEl) return;

  statusEl.className = `validation-status ${status}`;

  switch (status) {
    case 'valid':
      statusEl.innerHTML = '<span class="status-icon"></span><span>Valid</span>';
      break;
    case 'error':
      statusEl.innerHTML = '<span class="status-icon"></span><span>Error</span>';
      break;
    case 'validating':
      statusEl.innerHTML = '<span class="status-icon"><div class="loading-spinner"></div></span><span>Validating...</span>';
      break;
    default:
      statusEl.innerHTML = '<span class="status-icon"></span><span>Ready</span>';
      break;
  }
}

export function showErrors(errors) {
  const errorsContainer = document.getElementById('errorsContainer');
  const errorsList = document.getElementById('errorsList');
  if (!errorsContainer || !errorsList) return;

  if (errors && errors.length > 0) {
    errorsList.innerHTML = errors.map(error => `
      <div class="error-item">
        <span class="error-icon">!</span>
        <span>${escapeHtml(error)}</span>
      </div>
    `).join('');
    errorsContainer.classList.add('show');
  } else {
    errorsContainer.classList.remove('show');
    errorsList.innerHTML = '';
  }
}

export function updateResult(value) {
  const resultEl = document.getElementById('resultValue');
  if (!resultEl) return;

  if (value !== null && value !== undefined) {
    const formatted = typeof value === 'number' ? value.toFixed(6) : value;
    resultEl.textContent = formatted;
  } else {
    resultEl.textContent = 'Enter a valid formula...';
  }
}

export function updateBackendIndicator(active, message = 'Ready') {
  const indicator = document.getElementById('backendIndicator');
  if (!indicator) return;

  indicator.className = `backend-indicator ${active ? 'active' : ''}`;
  indicator.querySelector('span:first-child').textContent = `Backend: ${message}`;

  if (active) {
    state.requestCount += 1;
    const countEl = document.getElementById('requestCount');
    if (countEl) countEl.textContent = String(state.requestCount);
  }
}

export function addToHistory(formula, result) {
  state.formulaHistory.unshift({ formula, result });
  if (state.formulaHistory.length > 10) {
    state.formulaHistory.pop();
  }
  renderHistory();
}

export function renderHistory() {
  const historyList = document.getElementById('historyList');
  if (!historyList) return;
  historyList.innerHTML = '';

  state.formulaHistory.forEach(item => {
    const historyItem = document.createElement('div');
    historyItem.className = 'history-item';
    historyItem.innerHTML = `
      <span>${escapeHtml(item.formula)}</span>
      <span class="history-result">= ${item.result !== undefined && item.result !== null ? item.result.toFixed(4) : 'Error'}</span>
    `;
    historyItem.addEventListener('click', () => {
      if (!state.editor) return;
      state.editor.setValue(item.formula, 1);
    });
    historyList.appendChild(historyItem);
  });
}

export function renderSavedFormulas() {
  const list = document.getElementById('savedFormulasList');
  if (!list) return;
  list.innerHTML = '';

  const formulas = getSavedFormulas();
  if (!formulas.length) {
    const empty = document.createElement('div');
    empty.className = 'saved-empty';
    empty.textContent = 'No saved formulas yet. Save one to reuse it later.';
    list.appendChild(empty);
    return;
  }

  formulas.forEach(entry => {
    const row = document.createElement('div');
    row.className = 'saved-item';

    const label = document.createElement('div');
    label.className = 'saved-item-label';
    label.innerHTML = `
      <span class="saved-name">${escapeHtml(entry.name)}</span>
      <span class="saved-formula">${escapeHtml(entry.formula)}</span>
      <span class="saved-formula">${entry.result !== null && entry.result !== undefined ? `Result: ${Number(entry.result).toFixed(4)}` : 'Result: —'}</span>
    `;

    const actions = document.createElement('div');
    actions.className = 'saved-item-actions';

    const runBtn = document.createElement('button');
    runBtn.type = 'button';
    runBtn.className = 'icon-btn';
    runBtn.title = 'Run formula';
    runBtn.setAttribute('aria-label', `Run ${entry.name}`);
    runBtn.innerHTML = '<span class="icon icon-chevron-right" aria-hidden="true"></span>';
    runBtn.addEventListener('click', () => {
      if (!state.editor) return;
      editorExpandedCallback();
      state.editor.setValue(entry.formula, 1);
      state.editor.focus();
      validationCallback();
    });

    const deleteBtn = document.createElement('button');
    deleteBtn.type = 'button';
    deleteBtn.className = 'icon-btn';
    deleteBtn.title = 'Delete saved formula';
    deleteBtn.setAttribute('aria-label', `Delete ${entry.name}`);
    deleteBtn.innerHTML = '<span class="icon icon-trash" aria-hidden="true"></span>';
    deleteBtn.addEventListener('click', () => {
      removeSavedFormula(entry.id);
      renderSavedFormulas();
      showToast(`Removed "${entry.name}"`, 'info');
    });

    actions.appendChild(runBtn);
    actions.appendChild(deleteBtn);

    row.appendChild(label);
    row.appendChild(actions);
    list.appendChild(row);
  });
}

function insertIntoEditor(text) {
  if (!state.editor) return;
  state.editor.session.insert(state.editor.getCursorPosition(), text);
  state.editor.focus();
}

export function renderSourceKeyboard() {
  const container = document.getElementById('sourceKeyboard');
  if (!container) return;

  container.innerHTML = '';

  const createGroup = (label, entries) => {
    if (!entries.length) return;

    const group = document.createElement('div');
    group.className = 'keyboard-group';

    const labelEl = document.createElement('span');
    labelEl.className = 'keyboard-group-label';
    labelEl.textContent = label;

    const buttonsWrap = document.createElement('div');
    buttonsWrap.className = 'keyboard-group-buttons';

    entries.forEach(([key, data]) => {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'keyboard-chip';
      btn.textContent = key;
      btn.title = data.name ? `${data.name} (${key})` : key;
      btn.addEventListener('click', () => insertIntoEditor(key));
      buttonsWrap.appendChild(btn);
    });

    group.appendChild(labelEl);
    group.appendChild(buttonsWrap);
    container.appendChild(group);
  };

  const measuredEntries = Object.entries(state.measuredValues);
  const constantEntries = Object.entries(state.constants);

  createGroup('Measured', measuredEntries);
  createGroup('Constants', constantEntries);

  if (!container.childElementCount) {
    const empty = document.createElement('div');
    empty.className = 'keyboard-empty';
    empty.textContent = 'Add variables or constants to insert them quickly.';
    container.appendChild(empty);
  }
}

function createSourceItem(key, data, type) {
  const item = document.createElement('div');
  item.className = 'source-item';
  item.dataset.key = key;
  item.tabIndex = 0;
  item.title = `Insert ${key} into editor`;

  item.innerHTML = `
    <div class="source-header">
      <div class="source-name">${escapeHtml(data.name)}</div>
      <div class="source-actions">
        <button class="icon-btn edit-btn" title="Edit" aria-label="Edit">
          <span class="icon icon-edit" aria-hidden="true"></span>
        </button>
        <button class="icon-btn delete-btn" title="Delete" aria-label="Delete">
          <span class="icon icon-trash" aria-hidden="true"></span>
        </button>
      </div>
    </div>
    <div class="source-code">${escapeHtml(key)}</div>
    <div class="source-value">Value: ${escapeHtml(data.value)}${data.unit ? ' (' + escapeHtml(data.unit) + ')' : ''}</div>
  `;

  const handleInsert = (e) => {
    if (!e.target.closest('.source-actions') && !item.classList.contains('editing')) {
      insertIntoEditor(key);
      item.classList.add('active');
      setTimeout(() => item.classList.remove('active'), 500);
    }
  };

  item.addEventListener('click', handleInsert);
  item.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleInsert(e);
    }
  });

  const editButton = item.querySelector('.edit-btn');
  const deleteButton = item.querySelector('.delete-btn');

  const isCoreConstant = type === 'constant' && isBackendConstant(key);
  if (isCoreConstant && deleteButton) {
    deleteButton.classList.add('hidden');
  }

  editButton.addEventListener('click', (e) => {
    e.stopPropagation();
    showEditForm(item, key, data, type);
  });

  deleteButton.addEventListener('click', (e) => {
    e.stopPropagation();
    if (isCoreConstant) {
      showToast('Built-in constants cannot be deleted.', 'info');
      return;
    }
    if (confirm(`Delete ${data.name}?`)) {
      if (type === 'measured') {
        delete state.measuredValues[key];
        renderMeasuredValues();
      } else {
        delete state.constants[key];
        renderConstants();
      }
      showToast(`${data.name} deleted`, 'info');
      validationCallback();
    }
  });

  return item;
}

function showEditForm(item, key, data, type) {
  item.classList.add('editing');

  const form = document.createElement('div');
  form.className = 'source-edit-form';
  form.innerHTML = `
    <div class="input-group">
      <label for="editName">Name</label>
      <input type="text" value="${escapeHtml(data.name)}" id="editName" placeholder="e.g., Temperature">
      <div class="field-help">Display name shown in lists and suggestions.</div>
    </div>
    <div class="input-group">
      <label for="editValue">Value</label>
      <input type="number" value="${escapeHtml(data.value)}" step="any" id="editValue" placeholder="e.g., 25.5">
      <div class="field-help">Numeric value (decimals supported). Uses dot for decimal separator.</div>
    </div>
    ${type === 'measured' ? `
    <div class="input-group">
      <label for="editUnit">Unit (optional)</label>
      <input type="text" id="editUnit" value="${escapeHtml(data.unit || '')}" placeholder="e.g., C">
      <div class="field-help">Leave empty for unitless values.</div>
    </div>
    ` : ''}
    <div class="edit-actions">
      <button class="btn btn-success" id="saveEdit">Save</button>
      <button class="btn btn-secondary" id="cancelEdit">Cancel</button>
    </div>
  `;

  item.appendChild(form);

  form.querySelector('#saveEdit').addEventListener('click', () => {
    const nameInput = form.querySelector('#editName');
    const valueInput = form.querySelector('#editValue');
    const unitInput = form.querySelector('#editUnit');

    const newName = nameInput.value.trim();
    const newValue = parseFloat(valueInput.value);

    if (!newName || Number.isNaN(newValue)) {
      showToast('Please provide a valid name and numeric value.', 'error');
      return;
    }

    const updated = { name: newName, value: newValue };
    if (type === 'measured') {
      const unitValue = unitInput ? unitInput.value.trim() : '';
      if (unitValue) updated.unit = unitValue;
      state.measuredValues[key] = updated;
      renderMeasuredValues();
    } else {
      state.constants[key] = updated;
      renderConstants();
    }

    showToast(`${newName} updated`, 'success');
    validationCallback();
  });

  form.querySelector('#cancelEdit').addEventListener('click', () => {
    item.classList.remove('editing');
    form.remove();
  });
}

export function renderMeasuredValues(searchTerm = '') {
  const list = document.getElementById('measuredValuesList');
  if (!list) return;
  list.innerHTML = '';

  Object.entries(state.measuredValues)
    .filter(([key, data]) =>
      key.toLowerCase().includes(searchTerm.toLowerCase()) ||
      data.name.toLowerCase().includes(searchTerm.toLowerCase())
    )
    .forEach(([key, data]) => {
      const item = createSourceItem(key, data, 'measured');
      list.appendChild(item);
    });

  renderSourceKeyboard();
}

export function renderConstants(searchTerm = '') {
  const list = document.getElementById('constantsList');
  if (!list) return;
  list.innerHTML = '';

  Object.entries(state.constants)
    .filter(([key, data]) =>
      key.toLowerCase().includes(searchTerm.toLowerCase()) ||
      data.name.toLowerCase().includes(searchTerm.toLowerCase())
    )
    .forEach(([key, data]) => {
      const item = createSourceItem(key, data, 'constant');
      list.appendChild(item);
    });

  renderSourceKeyboard();
}

export function renderFunctions() {
  const renderCategory = (elementId, functions) => {
    const container = document.getElementById(elementId);
    if (!container) return;
    container.innerHTML = '';

    functions.forEach(func => {
      const item = document.createElement('div');
      item.className = 'function-item';
      item.innerHTML = `
        <div class="function-name">${escapeHtml(func.label)}</div>
        <div class="function-desc">${escapeHtml(func.desc)}</div>
      `;
      item.title = `${func.desc} (returns ${func.returns})`;
      item.tabIndex = 0;
      const insert = () => {
        if (!state.editor) return;
        state.editor.focus();
        if (func.snippet) {
          state.editor.insertSnippet(func.snippet);
        } else {
          insertIntoEditor(`${func.id}(`);
        }
      };
      item.addEventListener('click', insert);
      item.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          insert();
        }
      });
      container.appendChild(item);
    });
  };

  renderCategory('basicFunctions', state.functionsDB.basic);
  renderCategory('trigFunctions', state.functionsDB.trig);
  renderCategory('mathFunctions', state.functionsDB.math);
  renderCategory('statFunctions', state.functionsDB.stat);
}

export function setupSourceSearch() {
  const measuredSearch = document.getElementById('measuredSearch');
  if (measuredSearch) {
    measuredSearch.addEventListener('input', (e) => renderMeasuredValues(e.target.value));
  }

  const constantsSearch = document.getElementById('constantsSearch');
  if (constantsSearch) {
    constantsSearch.addEventListener('input', (e) => renderConstants(e.target.value));
  }
}

export function setupAddSourceButtons() {
  const addMeasuredBtn = document.getElementById('addMeasuredBtn');
  if (addMeasuredBtn) {
    addMeasuredBtn.addEventListener('click', () => {
      const key = prompt('Enter variable name (without $):');
      if (!key) return;

      const name = prompt('Enter display name:');
      if (!name) return;

      const valueInput = prompt('Enter value:');
      if (valueInput === null) return;

      const value = parseFloat(valueInput);
      if (Number.isNaN(value)) {
        showToast('Invalid numeric value.', 'error');
        return;
      }

      const unit = prompt('Optional unit (leave blank for unitless):', '');
      const symbol = `$${key}`;

      state.measuredValues[symbol] = {
        name,
        value,
        unit: unit ? unit.trim() : undefined
      };

      renderMeasuredValues();
      showToast(`Added ${name}`, 'success');
      validationCallback();
    });
  }

  const addConstantBtn = document.getElementById('addConstantBtn');
  if (addConstantBtn) {
    addConstantBtn.addEventListener('click', () => {
      const key = prompt('Enter constant name (without #):');
      if (!key) return;

      const name = prompt('Enter display name:');
      if (!name) return;

      const valueInput = prompt('Enter value:');
      if (valueInput === null) return;

      const value = parseFloat(valueInput);
      if (Number.isNaN(value)) {
        showToast('Invalid numeric value.', 'error');
        return;
      }

      const symbol = `#${key}`;
      state.constants[symbol] = { name, value };

      renderConstants();
      showToast(`Added ${name}`, 'success');
      validationCallback();
    });
  }
}

export function setupSourcePanelToggle() {
  const panel = document.getElementById('sourcePanel');
  const toggle = document.getElementById('sourcePanelToggle');
  if (!panel || !toggle) return;

  const icon = toggle.querySelector('.icon');

  const applyState = (collapsed) => {
    panel.classList.toggle('collapsed', collapsed);
    toggle.setAttribute('aria-expanded', String(!collapsed));
    toggle.setAttribute('aria-label', collapsed ? 'Expand sources' : 'Collapse sources');
    toggle.title = collapsed ? 'Expand sources' : 'Collapse sources';
    if (icon) {
      icon.className = `icon ${collapsed ? 'icon-chevron-down' : 'icon-chevron-up'}`;
    }
  };

  applyState(false);

  toggle.addEventListener('click', () => {
    const collapsed = !panel.classList.contains('collapsed');
    applyState(collapsed);
  });
}

export function setupEditorCollapseToggle() {
  const container = document.getElementById('editorContainer');
  const toggle = document.getElementById('editorCollapseToggle');
  if (!container || !toggle) return;

  const icon = toggle.querySelector('.icon');

  const applyState = (collapsed) => {
    container.classList.toggle('collapsed', collapsed);
    toggle.setAttribute('aria-expanded', String(!collapsed));
    toggle.setAttribute('aria-label', collapsed ? 'Expand editor' : 'Collapse editor');
    toggle.title = collapsed ? 'Expand editor' : 'Collapse editor';
    if (icon) {
      icon.className = `icon ${collapsed ? 'icon-chevron-down' : 'icon-chevron-up'}`;
    }
    if (state.editor) {
      setTimeout(() => {
        try {
          state.editor.resize(true);
        } catch (err) {
          /* ignore */
        }
      }, 0);
    }
  };

  applyState(false);

  toggle.addEventListener('click', () => {
    const collapsed = !container.classList.contains('collapsed');
    applyState(collapsed);
  });

  setEditorExpandedCallback((collapsed) => {
    if (container.classList.contains('collapsed')) {
      applyState(false);
    }
  });
}
