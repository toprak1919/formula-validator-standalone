export const COMPLETION_USAGE_KEY = 'formulaCompletionUsage_v2';
const SAVED_FORMULAS_KEY = 'formulaSavedFormulas_v1';

function loadCompletionUsage() {
  try {
    return JSON.parse(localStorage.getItem(COMPLETION_USAGE_KEY) || '{}');
  } catch (err) {
    console.warn('Failed to parse completion usage stats', err);
    return {};
  }
}

function persistCompletionUsage(completionUsage) {
  try {
    localStorage.setItem(COMPLETION_USAGE_KEY, JSON.stringify(completionUsage));
  } catch (err) {
    console.warn('Failed to persist completion usage', err);
  }
}

function loadSavedFormulas() {
  try {
    return JSON.parse(localStorage.getItem(SAVED_FORMULAS_KEY) || '[]');
  } catch (err) {
    console.warn('Failed to load saved formulas', err);
    return [];
  }
}

function persistSavedFormulas(formulas) {
  try {
    localStorage.setItem(SAVED_FORMULAS_KEY, JSON.stringify(formulas));
  } catch (err) {
    console.warn('Failed to persist saved formulas', err);
  }
}

export const state = {
  editor: null,
  validationTimeout: null,
  requestCount: 0,
  formulaHistory: [],
  testCases: [],
  currentTestIndex: 0,
  testRunning: false,
  measuredValues: {
    '$temperature': { name: 'Temperature', value: 25.5, unit: 'C' },
    '$pressure': { name: 'Pressure', value: 101.3 },
    '$humidity': { name: 'Humidity', value: 65.2 },
    '$flow_rate': { name: 'Flow Rate', value: 12.8 },
    '$voltage': { name: 'Voltage', value: 220.0, unit: 'V' }
  },
  constants: {},
  backendConstants: {},
  savedFormulas: loadSavedFormulas(),
  completionUsage: loadCompletionUsage(),
  sampleFormulas: [
    '($temperature * #conversion_factor) + 32',
    'sqrt($pressure * $humidity)',
    'sin(#pi / 2) + cos(0)',
    'max($temperature, $pressure, $humidity)',
    'if($temperature > #max_temp, 1, 0)',
    'log10($voltage) * #gravity',
    '($flow_rate * #gravity) / $pressure',
    'abs($temperature - #max_temp)',
    'round($humidity / 10, 1) * #conversion_factor'
  ],
  functionsDB: {
    basic: [
      { id: 'abs', label: 'abs(x)', desc: 'Absolute value', snippet: 'abs(${1:x})', returns: 'number' },
      { id: 'sign', label: 'sign(x)', desc: 'Sign function', snippet: 'sign(${1:x})', returns: 'number' },
      { id: 'sgn', label: 'sgn(x)', desc: 'Alias of sign', snippet: 'sgn(${1:x})', returns: 'number' },
      { id: 'mod', label: 'mod(x, y)', desc: 'Modulo (x % y)', snippet: 'mod(${1:x}, ${2:y})', returns: 'number' },
      { id: 'fact', label: 'fact(n)', desc: 'Factorial of n', snippet: 'fact(${1:n})', returns: 'number' },
      { id: 'gcd', label: 'gcd(a, b)', desc: 'Greatest common divisor', snippet: 'gcd(${1:a}, ${2:b})', returns: 'number' },
      { id: 'lcm', label: 'lcm(a, b)', desc: 'Least common multiple', snippet: 'lcm(${1:a}, ${2:b})', returns: 'number' },
      { id: 'if', label: 'if(cond, a, b)', desc: 'Conditional expression', snippet: 'if(${1:condition}, ${2:onTrue}, ${3:onFalse})', returns: 'number' }
    ],
    trig: [
      { id: 'sin', label: 'sin(x)', desc: 'Sine (radians)', snippet: 'sin(${1:x})', returns: 'number' },
      { id: 'cos', label: 'cos(x)', desc: 'Cosine (radians)', snippet: 'cos(${1:x})', returns: 'number' },
      { id: 'tan', label: 'tan(x)', desc: 'Tangent (radians)', snippet: 'tan(${1:x})', returns: 'number' },
      { id: 'asin', label: 'asin(x)', desc: 'Arcsine', snippet: 'asin(${1:x})', returns: 'number' },
      { id: 'acos', label: 'acos(x)', desc: 'Arccosine', snippet: 'acos(${1:x})', returns: 'number' },
      { id: 'atan', label: 'atan(x)', desc: 'Arctangent', snippet: 'atan(${1:x})', returns: 'number' },
      { id: 'sinh', label: 'sinh(x)', desc: 'Hyperbolic sine', snippet: 'sinh(${1:x})', returns: 'number' },
      { id: 'cosh', label: 'cosh(x)', desc: 'Hyperbolic cosine', snippet: 'cosh(${1:x})', returns: 'number' },
      { id: 'tanh', label: 'tanh(x)', desc: 'Hyperbolic tangent', snippet: 'tanh(${1:x})', returns: 'number' }
    ],
    math: [
      { id: 'sqrt', label: 'sqrt(x)', desc: 'Square root', snippet: 'sqrt(${1:x})', returns: 'number' },
      { id: 'exp', label: 'exp(x)', desc: 'e^x exponential', snippet: 'exp(${1:x})', returns: 'number' },
      { id: 'ln', label: 'ln(x)', desc: 'Natural logarithm', snippet: 'ln(${1:x})', returns: 'number' },
      { id: 'log10', label: 'log10(x)', desc: 'Base-10 logarithm', snippet: 'log10(${1:x})', returns: 'number' },
      { id: 'log2', label: 'log2(x)', desc: 'Base-2 logarithm', snippet: 'log2(${1:x})', returns: 'number' },
      { id: 'pow', label: 'pow(x, y)', desc: 'x raised to power y', snippet: 'pow(${1:base}, ${2:exp})', returns: 'number' },
      { id: 'floor', label: 'floor(x)', desc: 'Greatest integer ≤ x', snippet: 'floor(${1:x})', returns: 'number' },
      { id: 'ceil', label: 'ceil(x)', desc: 'Smallest integer ≥ x', snippet: 'ceil(${1:x})', returns: 'number' },
      { id: 'round', label: 'round(x, n)', desc: 'Round with optional precision', snippet: 'round(${1:x}, ${2:precision})', returns: 'number' }
    ],
    stat: [
      { id: 'min', label: 'min(a, b, ...)', desc: 'Minimum of arguments', snippet: 'min(${1:a}, ${2:b})', returns: 'number' },
      { id: 'max', label: 'max(a, b, ...)', desc: 'Maximum of arguments', snippet: 'max(${1:a}, ${2:b})', returns: 'number' },
      { id: 'sum', label: 'sum(a, b, ...)', desc: 'Sum of arguments', snippet: 'sum(${1:a}, ${2:b})', returns: 'number' },
      { id: 'prod', label: 'prod(a, b, ...)', desc: 'Product of arguments', snippet: 'prod(${1:a}, ${2:b})', returns: 'number' },
      { id: 'mean', label: 'mean(a, b, ...)', desc: 'Average (mean)', snippet: 'mean(${1:a}, ${2:b})', returns: 'number' },
      { id: 'avg', label: 'avg(a, b, ...)', desc: 'Alias of mean', snippet: 'avg(${1:a}, ${2:b})', returns: 'number' },
      { id: 'var', label: 'var(a, b, ...)', desc: 'Population variance', snippet: 'var(${1:a}, ${2:b})', returns: 'number' },
      { id: 'std', label: 'std(a, b, ...)', desc: 'Population standard deviation', snippet: 'std(${1:a}, ${2:b})', returns: 'number' }
    ]
  }
};

export function recordCompletionUsage(id) {
  if (!id) return;
  state.completionUsage[id] = (state.completionUsage[id] || 0) + 1;
  persistCompletionUsage(state.completionUsage);
}

export function getUsageScore(base, id) {
  const bonus = state.completionUsage[id] ? Math.min(400, state.completionUsage[id] * 25) : 0;
  return base + bonus;
}

export function setEditor(editor) {
  state.editor = editor;
}

export function setValidationTimeout(handle) {
  state.validationTimeout = handle;
}

export function clearValidationTimeout() {
  if (state.validationTimeout) {
    clearTimeout(state.validationTimeout);
    state.validationTimeout = null;
  }
}

function normalizeConstantId(id) {
  const trimmed = (id || '').trim();
  return trimmed.startsWith('#') ? trimmed : `#${trimmed}`;
}

function createId() {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return `formula-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function setBackendConstants(constantsArray) {
  const backend = {};
  if (Array.isArray(constantsArray)) {
    constantsArray.forEach(item => {
      if (!item) return;
      const id = normalizeConstantId(item.id || item.Id || '');
      if (!id || id === '#') return;
      const rawValue = typeof item.value === 'number' ? item.value : Number(item.value);
      if (Number.isNaN(rawValue)) return;

      backend[id] = {
        name: item.name ?? item.Name ?? id,
        value: rawValue
      };
    });
  }

  state.backendConstants = backend;
  state.constants = { ...backend };
}

export function resetConstantsToBackend() {
  state.constants = { ...state.backendConstants };
}

export function applyConstantOverrides(overrides) {
  if (!overrides) return;
  Object.entries(overrides).forEach(([rawId, data]) => {
    const id = normalizeConstantId(rawId);
    if (!data) return;
    const value = typeof data.value === 'number' ? data.value : Number(data.value);
    if (Number.isNaN(value)) return;
    state.constants[id] = {
      name: data.name || id,
      value
    };
  });
}

export function getConstantOverrides() {
  const overrides = [];
  const backend = state.backendConstants;

  Object.entries(state.constants).forEach(([id, data]) => {
    const normalizedId = normalizeConstantId(id);
    const value = typeof data.value === 'number' ? data.value : Number(data.value);
    if (Number.isNaN(value)) return;

    const backendEntry = backend[normalizedId];
    if (!backendEntry || backendEntry.value !== value || (backendEntry.name || normalizedId) !== (data.name || normalizedId)) {
      overrides.push({
        id: normalizedId,
        name: data.name || normalizedId,
        value
      });
    }
  });

  return overrides;
}

export function isBackendConstant(id) {
  const normalizedId = normalizeConstantId(id);
  return Object.prototype.hasOwnProperty.call(state.backendConstants, normalizedId);
}

export function getSavedFormulas() {
  return state.savedFormulas;
}

export function addSavedFormula({ name, formula, result = null }) {
  const trimmedName = (name || '').trim();
  const fallbackName = `Formula ${state.savedFormulas.length + 1}`;
  const entry = {
    id: createId(),
    name: trimmedName || fallbackName,
    formula,
    result
  };
  state.savedFormulas.unshift(entry);
  persistSavedFormulas(state.savedFormulas);
  return entry;
}

export function removeSavedFormula(id) {
  state.savedFormulas = state.savedFormulas.filter(item => item.id !== id);
  persistSavedFormulas(state.savedFormulas);
}

export function updateSavedFormulaResult(id, result) {
  const entry = state.savedFormulas.find(item => item.id === id);
  if (!entry) return;
  entry.result = result;
  persistSavedFormulas(state.savedFormulas);
}
