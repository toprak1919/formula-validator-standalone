import { state, recordCompletionUsage, getUsageScore } from './state.js';
import { escapeHtml } from './utils.js';

export const UNIT_CATALOG = {
  length: {
    label: 'Length',
    units: [
      { value: 'm', aliases: ['meter', 'metre', 'meters'], description: 'Base unit of length (meter)' },
      { value: 'cm', aliases: ['centimeter', 'centimetre'], description: 'Centimeter (1/100 of a meter)' },
      { value: 'mm', aliases: ['millimeter', 'millimetre'], description: 'Millimeter (1/1000 of a meter)' },
      { value: 'km', aliases: ['kilometer', 'kilometre', 'kilometers'], description: 'Kilometer (1000 meters)' },
      { value: 'mi', aliases: ['mile', 'miles'], description: 'Mile' },
      { value: 'yd', aliases: ['yard', 'yards'], description: 'Yard' },
      { value: 'ft', aliases: ['foot', 'feet'], description: 'Foot' },
      { value: 'in', aliases: ['inch', 'inches'], description: 'Inch' },
      { value: 'au', aliases: ['astronomical', 'astronomical_unit', 'astronomicalunit'], description: 'Astronomical Unit (~150M km)' }
    ]
  },
  mass: {
    label: 'Mass',
    units: [
      { value: 'kg', aliases: ['kilogram'], description: 'Kilogram' },
      { value: 'g', aliases: ['gram', 'grams'], description: 'Gram' }
    ]
  },
  duration: {
    label: 'Time',
    units: [
      { value: 's', aliases: ['sec', 'second', 'seconds'], description: 'Second' },
      { value: 'ms', aliases: ['millisecond', 'milliseconds'], description: 'Millisecond' },
      { value: 'min', aliases: ['minute', 'minutes'], description: 'Minute' },
      { value: 'h', aliases: ['hr', 'hour', 'hours'], description: 'Hour' }
    ]
  },
  temperature: {
    label: 'Temperature',
    units: [
      { value: 'K', aliases: ['kelvin'], description: 'Kelvin' },
      { value: 'C', aliases: ['celsius'], description: 'Degrees Celsius' },
      { value: 'F', aliases: ['fahrenheit'], description: 'Degrees Fahrenheit' }
    ]
  },
  electricCurrent: {
    label: 'Electric Current',
    units: [
      { value: 'A', aliases: ['amp', 'ampere'], description: 'Ampere' }
    ]
  },
  electricPotential: {
    label: 'Voltage',
    units: [
      { value: 'V', aliases: ['volt', 'volts'], description: 'Volt' }
    ]
  },
  electricResistance: {
    label: 'Resistance',
    units: [
      { value: 'Ω', aliases: ['ohm'], description: 'Ohm (resistance)' }
    ]
  },
  volume: {
    label: 'Volume',
    units: [
      { value: 'L', aliases: ['liter', 'litre'], description: 'Liter' },
      { value: 'mL', aliases: ['milliliter', 'millilitre'], description: 'Milliliter' }
    ]
  },
  pressure: {
    label: 'Pressure',
    units: [
      { value: 'Pa', aliases: ['pascal'], description: 'Pascal' },
      { value: 'bar', aliases: [], description: 'Bar (pressure unit)' }
    ]
  },
  force: {
    label: 'Force',
    units: [
      { value: 'N', aliases: ['newton', 'newtons'], description: 'Newton' }
    ]
  },
  energy: {
    label: 'Energy',
    units: [
      { value: 'J', aliases: ['joule', 'joules'], description: 'Joule' }
    ]
  },
  power: {
    label: 'Power',
    units: [
      { value: 'W', aliases: ['watt', 'watts'], description: 'Watt' }
    ]
  }
};

export const UNIT_ALIAS_MAP = (() => {
  const map = new Map();
  Object.entries(UNIT_CATALOG).forEach(([category, group]) => {
    group.units.forEach(unit => {
      const aliases = new Set([unit.value, ...unit.aliases]);
      aliases.forEach(alias => {
        map.set(alias.toLowerCase(), {
          alias,
          primary: unit.value,
          description: unit.description,
          category,
          label: group.label
        });
      });
    });
  });
  return map;
})();

const allFunctions = [];
Object.entries(state.functionsDB).forEach(([category, list]) => {
  list.forEach(func => {
    allFunctions.push({ ...func, category });
  });
});

function determineUnitCategory(unit) {
  if (!unit) return null;
  const info = UNIT_ALIAS_MAP.get(unit.toLowerCase());
  return info ? info.category : null;
}

function isVectorValue(entry) {
  return Array.isArray(entry?.values) && entry.values.length > 0;
}

function formatVectorPreview(values, max = 6) {
  if (!Array.isArray(values) || !values.length) return '[]';
  const rounded = values.map((value) => {
    const numeric = typeof value === 'number' ? value : Number.parseFloat(value);
    if (!Number.isFinite(numeric)) return 'NaN';
    return Math.abs(numeric) >= 1e4 || Math.abs(numeric) < 1e-3
      ? numeric.toExponential(2)
      : Number(numeric.toFixed(3)).toString();
  });
  const preview = rounded.slice(0, max).join(', ');
  const suffix = values.length > max ? ', …' : '';
  return `[${preview}${suffix}]`;
}

function renderSymbolDoc(type, id, data) {
  const vector = isVectorValue(data);
  const valueText = vector
    ? formatVectorPreview(data.values)
    : (typeof data.value === 'number' && !Number.isNaN(data.value)
      ? data.value
      : (data.value ?? '—'));
  const rows = [
    { label: 'Name', value: escapeHtml(data.name || id) },
    { label: vector ? 'Values' : 'Value', value: escapeHtml(String(valueText)) },
    { label: 'Unit', value: escapeHtml(data.unit || 'unitless') }
  ];

  if (vector) {
    rows.push({ label: 'Length', value: escapeHtml(String(data.values.length)) });
  }

  return `
    <div class="completion-doc">
      <div class="completion-doc__header">${escapeHtml(type)}</div>
      <div class="completion-doc__title">${escapeHtml(id)}</div>
      <div class="completion-doc__grid">
        ${rows.map(row => `
          <div class="completion-doc__row">
            <span class="completion-doc__label">${row.label}</span>
            <span class="completion-doc__value">${row.value}</span>
          </div>
        `).join('')}
      </div>
      <div class="completion-doc__hint">
        <kbd>Enter</kbd> insert · <kbd>Tab</kbd> keeps typing
      </div>
    </div>
  `;
}

function renderFunctionDoc(func) {
  const rows = [
    { label: 'Description', value: escapeHtml(func.desc) },
    { label: 'Returns', value: escapeHtml(func.returns) },
    { label: 'Category', value: escapeHtml(func.category) }
  ];

  return `
    <div class="completion-doc">
      <div class="completion-doc__header">Function</div>
      <div class="completion-doc__title">${escapeHtml(func.label)}</div>
      <div class="completion-doc__grid">
        ${rows.map(row => `
          <div class="completion-doc__row">
            <span class="completion-doc__label">${row.label}</span>
            <span class="completion-doc__value">${row.value}</span>
          </div>
        `).join('')}
      </div>
      <div class="completion-doc__hint">
        Use <kbd>Tab</kbd> to move between arguments
      </div>
    </div>
  `;
}

function renderUnitDoc(alias, unit, groupLabel, variableUnit) {
  const rows = [
    { label: 'Category', value: escapeHtml(groupLabel) },
    { label: 'Primary', value: escapeHtml(unit.value) },
    { label: 'Alias', value: escapeHtml(alias) }
  ];

  if (variableUnit) {
    rows.push({ label: 'Variable unit', value: escapeHtml(variableUnit) });
  }

  return `
    <div class="completion-doc">
      <div class="completion-doc__header">Unit</div>
      <div class="completion-doc__title">${escapeHtml(alias)}</div>
      <div class="completion-doc__grid">
        ${rows.map(row => `
          <div class="completion-doc__row">
            <span class="completion-doc__label">${row.label}</span>
            <span class="completion-doc__value">${row.value}</span>
          </div>
        `).join('')}
      </div>
      <div class="completion-doc__hint">${escapeHtml(unit.description)}</div>
    </div>
  `;
}

function createCompletionOption({ caption, value, meta, snippet, score, usageId, docHTML, detail, replacementPrefix }) {
  const completion = {
    caption,
    value: value ?? caption,
    meta: detail ? `${meta || ''}${meta ? ' • ' : ''}${detail}` : (meta || ''),
    score: typeof score === 'number' ? score : 1000,
    snippet,
    usageId,
    docHTML,
    replacementPrefix: typeof replacementPrefix === 'string' ? replacementPrefix : ''
  };

  return completion;
}

function buildUnitCompletions(measuredValues, variableKey, typedPrefix) {
  const completions = [];
  const variable = measuredValues[variableKey];
  const currentUnit = (variable && variable.unit) ? variable.unit.toLowerCase() : null;
  const category = determineUnitCategory(variable?.unit);
  const prefixLower = (typedPrefix || '').toLowerCase();
  const groups = category ? [UNIT_CATALOG[category]] : Object.values(UNIT_CATALOG);
  const seen = new Set();

  groups.forEach(group => {
    group.units.forEach(unit => {
      [unit.value, ...unit.aliases].forEach(alias => {
        const aliasLower = alias.toLowerCase();
        if (seen.has(aliasLower)) return;
        if (prefixLower && !aliasLower.startsWith(prefixLower)) return;
        seen.add(aliasLower);

        const usageId = `unit:${aliasLower}`;
        const isCurrent = currentUnit === aliasLower;
        const baseScore = isCurrent ? 1700 : 1400;
        const meta = isCurrent ? `${group.label} • current` : group.label;
        const detail = unit.value !== alias ? `alias of ${unit.value}` : '';

        completions.push(createCompletionOption({
          caption: alias,
          value: alias,
          meta,
          detail,
          score: getUsageScore(baseScore, usageId),
          usageId,
          docHTML: renderUnitDoc(alias, unit, group.label, variable?.unit),
          replacementPrefix: typedPrefix
        }));
      });
    });
  });

  if (!completions.length && typedPrefix) {
    completions.push(createCompletionOption({
      caption: typedPrefix,
      value: typedPrefix,
      meta: 'unit',
      score: 900,
      docHTML: `
        <div class="completion-doc">
          <div class="completion-doc__header">Unit</div>
          <div class="completion-doc__title">${escapeHtml(typedPrefix)}</div>
          <div class="completion-doc__grid">
            <div class="completion-doc__row">
              <span class="completion-doc__label">Status</span>
              <span class="completion-doc__value">Unknown</span>
            </div>
          </div>
          <div class="completion-doc__hint">Press <kbd>Enter</kbd> to insert as-is</div>
        </div>
      `,
      replacementPrefix: typedPrefix
    }));
  }

  return completions;
}

function buildMeasuredValueCompletions(prefix) {
  const completions = [];
  const prefixLower = (prefix || '').toLowerCase();

  Object.entries(state.measuredValues).forEach(([key, data]) => {
    const keyLower = key.toLowerCase();
    if (prefix && !keyLower.includes(prefixLower)) return;

    const usageId = `variable:${keyLower}`;
    const descriptorParts = ['variable'];
    descriptorParts.push(isVectorValue(data) ? 'list' : 'scalar');
    descriptorParts.push(data.unit ? data.unit : 'unitless');
    const unitMeta = descriptorParts.join(' • ');

    completions.push(createCompletionOption({
      caption: key,
      value: key,
      meta: unitMeta,
      score: getUsageScore(1300, usageId),
      usageId,
      docHTML: renderSymbolDoc('Variable', key, data),
      replacementPrefix: prefix || ''
    }));
  });

  return completions;
}

function buildConstantCompletions(prefix) {
  const completions = [];
  const prefixLower = (prefix || '').toLowerCase();

  Object.entries(state.constants).forEach(([key, data]) => {
    const keyLower = key.toLowerCase();
    if (prefix && !keyLower.includes(prefixLower)) return;

    const usageId = `constant:${keyLower}`;

    completions.push(createCompletionOption({
      caption: key,
      value: key,
      meta: 'constant',
      score: getUsageScore(1250, usageId),
      usageId,
      docHTML: renderSymbolDoc('Constant', key, data),
      replacementPrefix: prefix || ''
    }));
  });

  return completions;
}

function buildFunctionCompletions(prefix, context) {
  const completions = [];
  const prefixLower = (prefix || '').toLowerCase();

  allFunctions.forEach(func => {
    const matchable = [func.id, func.label.replace(/\(.*/, '')].map(x => x.toLowerCase());
    if (prefix && !matchable.some(m => m.startsWith(prefixLower))) return;

    let baseScore = 1100;
    if (context === 'boolean' && func.id === 'if') baseScore += 120;
    if (context === 'boolean' && ['mean', 'avg', 'sum', 'prod'].includes(func.id)) baseScore -= 50;

    const usageId = `function:${func.id}`;

    completions.push(createCompletionOption({
      caption: func.label,
      value: `${func.id}(`,
      snippet: func.snippet,
      meta: `function • ${func.returns}`,
      score: getUsageScore(baseScore, usageId),
      usageId,
      docHTML: renderFunctionDoc(func),
      replacementPrefix: prefix
    }));
  });

  return completions;
}

function detectContext(session, pos) {
  const line = session.getLine(pos.row);
  const before = line.substring(0, pos.column);

  if (/if\s*\([^,]*$/i.test(before)) {
    return 'boolean';
  }

  if (/(>=|<=|==|!=|>|<)\s*$/.test(before)) {
    return 'value';
  }

  return 'value';
}

export function createCustomCompleter() {
  return {
    getCompletions(editor, session, pos, prefix, callback) {
      const completions = [];
      const line = session.getLine(pos.row);
      const textBeforeCursor = line.substring(0, pos.column);
      const varDotPattern = /\$([a-zA-Z_][a-zA-Z0-9_]*)\.\s*([a-zA-Z_]*)$/;
      const varDotMatch = textBeforeCursor.match(varDotPattern);

      if (varDotMatch) {
        const varName = '$' + varDotMatch[1];
        const typedUnitPrefix = varDotMatch[2] || '';
        completions.push(...buildUnitCompletions(state.measuredValues, varName, typedUnitPrefix));
      } else {
        const context = detectContext(session, pos);
        completions.push(...buildMeasuredValueCompletions(prefix));
        completions.push(...buildConstantCompletions(prefix));
        completions.push(...buildFunctionCompletions(prefix, context));
      }

      callback(null, completions);
    },
    insertMatch(editor, data) {
      const completion = data && data.completion ? data.completion : data;
      if (!completion) return;

      if (completion.usageId) recordCompletionUsage(completion.usageId);

      const Range = ace.require('ace/range').Range;
      const session = editor.session;
      const cursor = editor.getCursorPosition();
      const line = session.getLine(cursor.row);
      const prefix = completion.replacementPrefix || '';
      let startColumn = Math.max(0, cursor.column - prefix.length);

      if (completion.caption && (completion.caption.startsWith('#') || completion.caption.startsWith('$'))) {
        const symbol = completion.caption[0];
        startColumn = cursor.column;
        while (startColumn > 0 && /[a-zA-Z0-9_]/.test(line[startColumn - 1])) {
          startColumn -= 1;
        }
        while (startColumn > 0 && line[startColumn - 1] === symbol) {
          startColumn -= 1;
        }
      }

      const removalRange = new Range(cursor.row, startColumn, cursor.row, cursor.column);

      const removePrefix = () => {
        session.replace(removalRange, '');
      };

      if (typeof completion.onInsert === 'function') {
        completion.onInsert(editor, completion, removePrefix);
        return true;
      }

      removePrefix();

      const insertPos = editor.getCursorPosition();
      if (completion.snippet) {
        editor.insertSnippet(completion.snippet);
      } else {
        let valueToInsert = completion.value ?? '';
        const symbol = completion.caption && (completion.caption.startsWith('#') || completion.caption.startsWith('$'))
          ? completion.caption[0]
          : null;

        if (symbol) {
          if (!valueToInsert.startsWith(symbol)) {
            valueToInsert = symbol + valueToInsert;
          }

          const lineNow = session.getLine(insertPos.row);
          if (insertPos.column > 0 && lineNow[insertPos.column - 1] === symbol) {
            valueToInsert = valueToInsert.slice(1);
          }
        }

        session.insert(insertPos, valueToInsert);

        if (symbol) {
          const row = insertPos.row;
          let lineText = session.getLine(row);

          let normStart = insertPos.column;
          while (normStart > 0 && lineText[normStart - 1] === symbol) {
            normStart -= 1;
          }

          let normEnd = normStart;
          while (normEnd < lineText.length && /[a-zA-Z0-9_]/.test(lineText[normEnd])) {
            normEnd += 1;
          }

          const token = lineText.slice(normStart, normEnd);
          const extraMatch = token.match(new RegExp(`^\\${symbol}{2,}`));
          if (extraMatch) {
            const rest = token.slice(extraMatch[0].length);
            const normalized = symbol + rest;
            session.replace(new Range(row, normStart, row, normEnd), normalized);
            editor.moveCursorTo(row, normStart + normalized.length);
          }
        }
      }

      editor.focus();
      return true;
    },
    getDocTooltip(item) {
      if (item && item.docHTML) {
        return { title: item.caption, docHTML: item.docHTML };
      }
      return null;
    }
  };
}
