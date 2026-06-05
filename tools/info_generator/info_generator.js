'use strict';
/**
 * gen-data: shared/datas/**\/*.csv → {client,server}/generated/data/**\/*.json
 *
 * CSV structure:
 *   Row 1: field names
 *   Row 2: target scope  — C | S | CS
 *   Row 3: normalized type — int8/16/32/64, uint8/16/32/64, float, double,
 *                            bool, string, string(N), [EnumName]
 *   Row 4: constraints   — PK, FK:[table], NN, UQ, IDX, AUTO (comma-separated)
 *   Row 5+: actual data
 */

const fs     = require('fs');
const path   = require('path');
const crypto = require('crypto');
const cfg    = require('../config-loader');

const VALID_PRIMITIVE_TYPES = new Set([
  'int8','int16','int32','int64',
  'uint8','uint16','uint32','uint64',
  'float','double','bool','string',
]);

const VALID_CONSTRAINTS = new Set(['PK','FK','NN','UQ','IDX','AUTO']);
const VALID_TARGETS     = new Set(['C','S','CS']);

// ── helpers ───────────────────────────────────────────────────────────────────
function ensureDir(dir) {
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
}

function stripBOM(str) {
  return str.charCodeAt(0) === 0xFEFF ? str.slice(1) : str;
}

function parseCSVLine(line) {
  const fields = [];
  let cur = '', inQuote = false;
  for (let i = 0; i < line.length; i++) {
    const c = line[i];
    if (c === '"') {
      if (inQuote && line[i + 1] === '"') { cur += '"'; i++; }
      else inQuote = !inQuote;
    } else if (c === ',' && !inQuote) {
      fields.push(cur.trim()); cur = '';
    } else {
      cur += c;
    }
  }
  fields.push(cur.trim());
  return fields;
}

function isValidType(t) {
  if (VALID_PRIMITIVE_TYPES.has(t)) return true;
  if (/^string\(\d+\)$/.test(t)) return true;   // string(N)
  if (/^[A-Z][A-Za-z0-9_]*$/.test(t)) return true; // EnumName
  return false;
}

function coerceValue(raw, type, fieldName, csvFile, rowIdx, errors) {
  if (raw === '' || raw === null || raw === undefined) return null;
  if (type.startsWith('int') || type.startsWith('uint')) {
    const n = Number(raw);
    if (!Number.isInteger(n)) {
      errors.push({ file: csvFile, row: rowIdx, field: fieldName,
        msg: `value "${raw}" cannot be parsed as ${type}` });
      return null;
    }
    return n;
  }
  if (type === 'float' || type === 'double') {
    const n = Number(raw);
    if (isNaN(n)) {
      errors.push({ file: csvFile, row: rowIdx, field: fieldName,
        msg: `value "${raw}" cannot be parsed as ${type}` });
      return null;
    }
    return n;
  }
  if (type === 'bool') {
    if (raw === 'true') return true;
    if (raw === 'false') return false;
    errors.push({ file: csvFile, row: rowIdx, field: fieldName,
      msg: `value "${raw}" cannot be parsed as bool (expected true/false)` });
    return null;
  }
  return raw; // string, string(N), EnumName → keep as string
}

// ── CSV parser ────────────────────────────────────────────────────────────────
function parseCSV(content, filePath) {
  const relPath = path.relative(cfg.root, filePath);
  const lines   = stripBOM(content).split(/\r?\n/).filter(l => l.trim() !== '');
  const errors  = [];

  if (lines.length < 5) {
    errors.push({ file: relPath, row: 1, field: '-',
      msg: 'CSV must have at least 5 rows (header rows 1-4 + at least 1 data row)' });
    return { errors, schema: null, clientData: null, serverData: null };
  }

  const names       = parseCSVLine(lines[0]);
  const targets     = parseCSVLine(lines[1]);
  const types       = parseCSVLine(lines[2]);
  const constraints = parseCSVLine(lines[3]);
  const colCount    = names.length;

  // ── Validate header rows ──────────────────────────────────────────────────
  for (let c = 0; c < colCount; c++) {
    const name = names[c];
    if (!name) {
      errors.push({ file: relPath, row: 1, field: `col[${c}]`,
        msg: 'Field name cannot be empty' });
    }

    const target = targets[c];
    if (!VALID_TARGETS.has(target)) {
      errors.push({ file: relPath, row: 2, field: name,
        msg: `"${target}" is not a valid target (expected C, S, or CS)` });
    }

    const type = types[c];
    if (!isValidType(type)) {
      errors.push({ file: relPath, row: 3, field: name,
        msg: `"${type}" is not a valid type` });
    }

    const constraintList = constraints[c] ? constraints[c].split(',').map(s => s.trim()) : [];
    for (const con of constraintList) {
      const base = con.split(':')[0];
      if (con && !VALID_CONSTRAINTS.has(base)) {
        errors.push({ file: relPath, row: 4, field: name,
          msg: `"${con}" is not a valid constraint` });
      }
    }
  }

  if (errors.length > 0) return { errors, schema: null, clientData: null, serverData: null };

  // ── Build column metadata ─────────────────────────────────────────────────
  const cols = names.map((name, c) => ({
    name,
    target:      targets[c],
    type:        types[c],
    constraints: constraints[c] ? constraints[c].split(',').map(s => s.trim()).filter(Boolean) : [],
  }));

    const pkCols = cols.filter(col => col.constraints.includes('PK'));
    const pkCol = pkCols[0];

  // ── Parse data rows ───────────────────────────────────────────────────────
  const clientData = [];
  const serverData = [];

  for (let r = 4; r < lines.length; r++) {
    const values  = parseCSVLine(lines[r]);
    const rowNum  = r + 1; // 1-based for user display
    const rowErrors = [];

    // Validate PK not null
    for (const pk of pkCols) {
      const pkIdx = cols.indexOf(pk);
      const pkVal = values[pkIdx];
      if (pkVal === '' || pkVal === undefined) {
        rowErrors.push({ file: relPath, row: rowNum, field: pk.name,
          msg: `NULL value not allowed for primary key (PK)` });
      }
    }

    // Validate NN constraints
    for (let c = 0; c < colCount; c++) {
      if (cols[c].constraints.includes('NN') && (values[c] === '' || values[c] === undefined)) {
        rowErrors.push({ file: relPath, row: rowNum, field: cols[c].name,
          msg: `NULL value not allowed (NN constraint)` });
      }
    }

    errors.push(...rowErrors);
    if (rowErrors.length > 0) continue;

    const clientRow = {};
    const serverRow = {};

    for (let c = 0; c < colCount; c++) {
      const col = cols[c];
      const val = coerceValue(values[c], col.type, col.name, relPath, rowNum, errors);
      if (cfg.dataGen.clientTargets.includes(col.target)) clientRow[col.name] = val;
      if (cfg.dataGen.serverTargets.includes(col.target)) serverRow[col.name] = val;
    }

    clientData.push(clientRow);
    serverData.push(serverRow);
  }

  const schema = {
    columns: cols.map(({ name, target, type, constraints }) =>
      ({ name, target, type, constraints })),
  };

  return { errors, schema, clientData, serverData };
}

// ── C# class generator ───────────────────────────────────────────────────────
function toPascalCase(str) {
  return str.split('_').map(s => s.charAt(0).toUpperCase() + s.slice(1)).join('');
}

function toCSharpType(csvType) {
  const map = cfg.typeMap.csharp;
  if (map[csvType]) return map[csvType];
  if (/^string\(\d+\)$/.test(csvType)) return 'string';
  return csvType; // enum — keep PascalCase type name as-is
}

function generateCSharpClass(className, clientCols, resourcePath, namespace, sourceRel) {
  const primitives = new Set(['sbyte','short','int','long','byte','ushort','uint','ulong','float','double','bool','string']);
  const hasGameTypeEnum = clientCols.some(c => !primitives.has(toCSharpType(c.type)));
  const usings = hasGameTypeEnum
    ? [`using System;`, `using ProjectFlood.Contracts.GameTypes;`]
    : [`using System;`];
  const fields = clientCols.map(c => `        public ${toCSharpType(c.type)} ${c.name};`).join('\n');
  return [
    `// AUTO-GENERATED by gen:data — do not edit`,
    `// Source: shared/datas/${sourceRel.replace(/\\/g, '/')}`,
    ...usings,
    ``,
    `namespace ${namespace}`,
    `{`,
    `    [Serializable]`,
    `    public class ${className}`,
    `    {`,
    `        public const string ResourcePath = "${resourcePath}";`,
    ``,
    fields,
    `    }`,
    `}`,
  ].join('\n');
}

function generateServerCSharpClass(className, serverCols) {
  const lines = [`    public sealed class ${className}`, `    {`];
  for (const c of serverCols) {
    const csType = toCSharpType(c.type);
    const def = csType === 'string' ? ' = "";' : '';
    lines.push(`        public ${csType} ${c.name} { get; set; }${def}`);
  }
  lines.push(`    }`);
  return lines.join('\n');
}

function generateServerCSharpLoader(className, serverCols, pkCols) {
  const lines = [`    public static class ${className}Loader`, `    {`];

  lines.push(`        public static IReadOnlyList<${className}> LoadAll(string csvPath)`);
  lines.push(`        {`);
  lines.push(`            var result = new List<${className}>();`);
  lines.push(`            if (!File.Exists(csvPath)) return result;`);
  lines.push(`            using var reader = new StreamReader(csvPath);`);
  lines.push(`            var headerLine = reader.ReadLine();`);
  lines.push(`            if (headerLine == null) return result;`);
  lines.push(`            var headers = SplitCsvLine(headerLine);`);
  lines.push(`            var idx = new Dictionary<string, int>(StringComparer.Ordinal);`);
  lines.push(`            for (int i = 0; i < headers.Length; i++) idx[headers[i]] = i;`);
  lines.push(`            string? line;`);
  lines.push(`            while ((line = reader.ReadLine()) is not null)`);
  lines.push(`            {`);
  lines.push(`                if (string.IsNullOrWhiteSpace(line)) continue;`);
  lines.push(`                var cols = SplitCsvLine(line);`);
  lines.push(`                result.Add(new ${className}`);
  lines.push(`                {`);

  serverCols.forEach((c, i) => {
    const csType = toCSharpType(c.type);
    const v = `i${i}`;
    let expr;
    if (csType === 'string') {
      expr = `idx.TryGetValue("${c.name}", out var ${v}) && ${v} < cols.Length ? (cols[${v}] ?? "") : ""`;
    } else if (csType === 'bool') {
      expr = `idx.TryGetValue("${c.name}", out var ${v}) && ${v} < cols.Length && cols[${v}] == "true"`;
    } else if (csType === 'float') {
      expr = `idx.TryGetValue("${c.name}", out var ${v}) && ${v} < cols.Length && !string.IsNullOrEmpty(cols[${v}]) ? float.Parse(cols[${v}], System.Globalization.CultureInfo.InvariantCulture) : default`;
    } else if (csType === 'double') {
      expr = `idx.TryGetValue("${c.name}", out var ${v}) && ${v} < cols.Length && !string.IsNullOrEmpty(cols[${v}]) ? double.Parse(cols[${v}], System.Globalization.CultureInfo.InvariantCulture) : default`;
    } else {
      expr = `idx.TryGetValue("${c.name}", out var ${v}) && ${v} < cols.Length && !string.IsNullOrEmpty(cols[${v}]) ? ${csType}.Parse(cols[${v}]) : default`;
    }
    lines.push(`                    ${c.name} = ${expr},`);
  });

  lines.push(`                });`);
  lines.push(`            }`);
  lines.push(`            return result;`);
  lines.push(`        }`);

  if (pkCols.length === 1) {
    const pk = pkCols[0];
    lines.push(``);
    lines.push(`        public static IReadOnlyDictionary<${toCSharpType(pk.type)}, ${className}> LoadAsDict(string csvPath)`);
    lines.push(`            => LoadAll(csvPath).ToDictionary(r => r.${pk.name});`);
  }

  lines.push(``);
  lines.push(`        private static string[] SplitCsvLine(string line)`);
  lines.push(`        {`);
  lines.push(`            var result = new List<string>();`);
  lines.push(`            int i = 0;`);
  lines.push(`            while (true)`);
  lines.push(`            {`);
  lines.push(`                if (i < line.Length && line[i] == '"')`);
  lines.push(`                {`);
  lines.push(`                    i++;`);
  lines.push(`                    var sb = new System.Text.StringBuilder();`);
  lines.push(`                    while (i < line.Length)`);
  lines.push(`                    {`);
  lines.push(`                        if (line[i] == '"')`);
  lines.push(`                        {`);
  lines.push(`                            if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i += 2; }`);
  lines.push(`                            else { i++; break; }`);
  lines.push(`                        }`);
  lines.push(`                        else sb.Append(line[i++]);`);
  lines.push(`                    }`);
  lines.push(`                    result.Add(sb.ToString());`);
  lines.push(`                }`);
  lines.push(`                else`);
  lines.push(`                {`);
  lines.push(`                    int start = i;`);
  lines.push(`                    while (i < line.Length && line[i] != ',') i++;`);
  lines.push(`                    result.Add(line.Substring(start, i - start));`);
  lines.push(`                }`);
  lines.push(`                if (i >= line.Length) break;`);
  lines.push(`                i++;`);
  lines.push(`            }`);
  lines.push(`            return result.ToArray();`);
  lines.push(`        }`);
  lines.push(`    }`);

  return lines.join('\n');
}

function generateServerCSharpFile(className, serverCols, pkCols, namespace, sourceRel) {
  return [
    `// AUTO-GENERATED by gen:data — do not edit`,
    `// Source: shared/datas/${sourceRel.replace(/\\/g, '/')}`,
    `#nullable enable`,
    `using System;`,
    `using System.Collections.Generic;`,
    `using System.IO;`,
    ``,
    `namespace ${namespace}`,
    `{`,
    generateServerCSharpClass(className, serverCols),
    ``,
    generateServerCSharpLoader(className, serverCols, pkCols),
    `}`,
  ].join('\n');
}

// ── Domain POCO / IStaticDataService / StaticDataService codegen ──────────────

const SKIP_DOMAIN_SUBDIRS    = new Set(['ingame', 'string', 'tutorial']);
const SKIP_DOMAIN_POCO_FILES = new Set(['outgame_shop_catalog', 'outgame_stamina_config', 'config_reward_group', 'config_reward_item']);
const SKIP_AUTO_SVC_FILES    = new Set([...SKIP_DOMAIN_POCO_FILES, 'streak_challenge_event']);

function toDomainPocoClassName(csvBasename) {
  return toPascalCase(csvBasename) + 'Data';
}

function generateDomainPocoContent(className, serverCols, sourceRel) {
  const lines = [
    `// AUTO-GENERATED by gen:data — do not edit`,
    `// Source: shared/datas/${sourceRel.replace(/\\/g, '/')}`,
    ``,
    `namespace ${cfg.dataGen.domainStaticDataNamespace};`,
    ``,
    `public class ${className}`,
    `{`,
  ];
  for (const c of serverCols) {
    const csType   = toCSharpType(c.type);
    const propName = toPascalCase(c.name);
    const def      = csType === 'string' ? ' = "";' : '';
    lines.push(`    public ${csType} ${propName} { get; set; }${def}`);
  }
  lines.push(`}`);
  return lines.join('\n');
}

function cleanStaleDomainPocos(dir) {
  if (!fs.existsSync(dir)) return;
  for (const entry of fs.readdirSync(dir)) {
    if (entry.endsWith('.g.cs')) fs.unlinkSync(path.join(dir, entry));
  }
}

function toServiceFieldName(className) {
  const camel = className.charAt(0).toLowerCase() + className.slice(1);
  return `_${camel}s`;
}

function generateInterfaceFile(entries) {
  if (!entries.length) return null;
  const lines = [
    `// AUTO-GENERATED by gen:data — do not edit`,
    `#nullable enable`,
    `using ${cfg.dataGen.domainStaticDataNamespace};`,
    ``,
    `namespace ${cfg.dataGen.domainInterfaceNamespace};`,
    ``,
    `public partial interface IStaticDataService`,
    `{`,
  ];
  for (const e of entries) {
    lines.push(`    ${e.pocoClass}? Get${e.baseClass}(${e.pkCsType} ${e.pkCamelName});`);
    lines.push(`    IReadOnlyList<${e.pocoClass}> GetAll${e.baseClass}s();`);
  }
  lines.push(`}`);
  return lines.join('\n');
}

function generateServiceFile(entries) {
  if (!entries.length) return null;
  const uniqueSubDirs = [...new Set(entries.map(e => e.subDir))];
  const lines = [
    `// AUTO-GENERATED by gen:data — do not edit`,
    `#nullable enable`,
    `using ${cfg.dataGen.domainStaticDataNamespace};`,
    `using ${cfg.dataGen.serverNamespace};`,
    ``,
    `namespace ${cfg.dataGen.infrastructureDataNamespace};`,
    ``,
    `public partial class StaticDataService`,
    `{`,
  ];
  for (const e of entries) {
    lines.push(`    private IReadOnlyDictionary<${e.pkCsType}, ${e.pocoClass}> ${e.fieldName} = new Dictionary<${e.pkCsType}, ${e.pocoClass}>();`);
  }
  lines.push(``);
  lines.push(`    private void InitGeneratedData(string dataRoot)`);
  lines.push(`    {`);
  for (const subDir of uniqueSubDirs) {
    const pascal = toPascalCase(subDir);
    const camel  = pascal.charAt(0).toLowerCase() + pascal.slice(1);
    lines.push(`        var ${camel}Path = System.IO.Path.Combine(dataRoot, "${subDir}");`);
  }
  lines.push(``);
  for (const e of entries) {
    const pascal = toPascalCase(e.subDir);
    const camel  = pascal.charAt(0).toLowerCase() + pascal.slice(1);
    lines.push(`        ${e.fieldName} = ${e.loaderClass}.LoadAll(System.IO.Path.Combine(${camel}Path, "${e.csvFile}"))`);
    lines.push(`            .ToDictionary(r => r.${e.pkCamelName}, r => new ${e.pocoClass}`);
    lines.push(`            {`);
    for (const c of e.serverCols) {
      lines.push(`                ${toPascalCase(c.name)} = r.${c.name},`);
    }
    lines.push(`            });`);
  }
  lines.push(`    }`);
  lines.push(``);
  for (const e of entries) {
    lines.push(`    public ${e.pocoClass}? Get${e.baseClass}(${e.pkCsType} ${e.pkCamelName}) => ${e.fieldName}.GetValueOrDefault(${e.pkCamelName});`);
    lines.push(`    public IReadOnlyList<${e.pocoClass}> GetAll${e.baseClass}s() => ${e.fieldName}.Values.ToList();`);
  }
  lines.push(`}`);
  return lines.join('\n');
}

// ── StringIds.cs generator ────────────────────────────────────────────────────
function keyToConstantName(key) {
  return key.split(/[._-]/).map(p => p.charAt(0).toUpperCase() + p.slice(1)).join('');
}

function generateStringIds(keys, sourceRel) {
  const groups = {};
  for (const key of keys) {
    const prefix = key.split('.')[0];
    if (!groups[prefix]) groups[prefix] = [];
    groups[prefix].push(key);
  }
  const lines = [
    '// AUTO-GENERATED by gen:data — do not edit',
    `// Source: shared/datas/${sourceRel.replace(/\\/g, '/')}`,
    '#if UNITY_EDITOR',
    'namespace Game.Editor',
    '{',
    '    // Keys from client_string.csv — import with: using static Game.Editor.StringIds',
    '    internal static class StringIds',
    '    {',
  ];
  for (const [prefix, prefixKeys] of Object.entries(groups)) {
    lines.push(`        // ${prefix}`);
    for (const key of prefixKeys) {
      lines.push(`        public const string ${keyToConstantName(key)} = "${key}";`);
    }
    lines.push('');
  }
  if (lines[lines.length - 1] === '') lines.pop();
  lines.push('    }', '}', '#endif', '');
  return lines.join('\n');
}

// ── Recursive CSV scan ────────────────────────────────────────────────────────
function cleanStaleServerScripts(dir) {
  if (!fs.existsSync(dir)) return;
  for (const entry of fs.readdirSync(dir)) {
    const full = path.join(dir, entry);
    if (fs.statSync(full).isDirectory()) {
      cleanStaleServerScripts(full);
    } else if (entry.endsWith('.cs') && !entry.endsWith('.g.cs')) {
      fs.unlinkSync(full);
    }
  }
}

function collectCSVFiles(dir, base) {
  const results = [];
  if (!fs.existsSync(dir)) return results;
  for (const entry of fs.readdirSync(dir)) {
    if (entry.startsWith('_')) continue;
    const full = path.join(dir, entry);
    const rel  = base ? path.join(base, entry) : entry;
    if (fs.statSync(full).isDirectory()) {
      results.push(...collectCSVFiles(full, rel));
    } else if (entry.endsWith('.csv')) {
      results.push({ full, rel });
    }
  }
  return results;
}

// ── Hash helpers ──────────────────────────────────────────────────────────────
const escapeCsvValue = (value) => {
  if (value === null) return '';
  const text = String(value);
  return /[",\r\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
};

function buildCsvText(cols, rows) {
  const header = cols.map(c => c.name).join(',');
  const dataLines = rows.map(row => cols.map(c => escapeCsvValue(row[c.name])).join(','));
  return [header, ...dataLines].join('\n');
}

function sha256hex(str) {
  return crypto.createHash('sha256').update(str, 'utf8').digest('hex');
}

// ── Main ──────────────────────────────────────────────────────────────────────
function main() {
  const { datasDir, clientGenerated, clientScriptsGenerated, serverGenerated, serverScriptsGenerated } = cfg.paths;
  cleanStaleServerScripts(serverScriptsGenerated);
  const domainStaticDataDir = cfg.dataGen.domainStaticDataDir;
  const ifaceFilePath       = cfg.dataGen.domainInterfaceFile;
  const serviceFilePath     = cfg.dataGen.infrastructureStaticDataServiceFile;
  ensureDir(domainStaticDataDir);
  ensureDir(path.dirname(ifaceFilePath));
  ensureDir(path.dirname(serviceFilePath));
  cleanStaleDomainPocos(domainStaticDataDir);
  const autoServiceEntries = [];
  const csvFiles = collectCSVFiles(datasDir, '');

  if (csvFiles.length === 0) {
    console.log('[gen-data] No CSV files found in', path.relative(cfg.root, datasDir));
    return;
  }

  let totalErrors = 0;
  const allErrors = [];
  let stringIdsKeys = [];

  // Accumulators for hash computation (CS-scope only, sorted by resourcePath)
  const csSchemaLines  = []; // "{resourcePath}\t{colName}\t{colType}\t{constraints}"
  const csDataLines    = []; // "{resourcePath}\t{rowIdx}\t{colName}\t{value}"
  const clientBundleFiles = {}; // resourcePath → client CSV text (C+CS scope)

  for (const { full, rel } of csvFiles) {
    const content = fs.readFileSync(full, 'utf-8');
    const { errors, schema, clientData, serverData } = parseCSV(content, full);

    if (errors.length > 0) {
      allErrors.push({ rel, errors });
      totalErrors += errors.length;
      continue;
    }

    const baseName   = path.basename(rel, '.csv');
    const subDir     = path.dirname(rel);
    const clientDir  = path.join(clientGenerated, 'data', subDir);
    const serverDir  = path.join(serverGenerated, 'data', subDir);

    ensureDir(clientDir);
    ensureDir(serverDir);

    const clientCols = schema.columns.filter(c => cfg.dataGen.clientTargets.includes(c.target));
    const serverCols = schema.columns.filter(c => cfg.dataGen.serverTargets.includes(c.target));
    const csCols     = schema.columns.filter(c => c.target === 'CS');

    // ── Client CSV ────────────────────────────────────────────────────────────
    const clientCsvText = buildCsvText(clientCols, clientData);
    fs.writeFileSync(path.join(clientDir, `${baseName}.csv`), clientCsvText, 'utf-8');

    // ── Client C# model class ─────────────────────────────────────────────────
    const className    = toPascalCase(baseName);
    const resourcePath = `data/${subDir.replace(/\\/g, '/')}/${baseName}`;
    const csContent    = generateCSharpClass(className, clientCols, resourcePath, cfg.dataGen.clientNamespace, rel);
    const csDir        = path.join(clientScriptsGenerated, subDir);
    ensureDir(csDir);
    fs.writeFileSync(path.join(csDir, `${className}.cs`), csContent, 'utf-8');

    // ── Server CSV ────────────────────────────────────────────────────────────
    const serverCsvText = buildCsvText(serverCols, serverData);
    fs.writeFileSync(path.join(serverDir, `${baseName}.csv`), serverCsvText, 'utf-8');

    const staleServerJson = path.join(serverDir, `${baseName}.json`);
    if (fs.existsSync(staleServerJson)) fs.unlinkSync(staleServerJson);

    // ── Server C# model + loader ──────────────────────────────────────────────
    const serverPkCols = serverCols.filter(c => c.constraints.includes('PK'));
    const serverCsContent = generateServerCSharpFile(className, serverCols, serverPkCols, cfg.dataGen.serverNamespace, rel);
    const serverCsDir = path.join(serverScriptsGenerated, subDir);
    ensureDir(serverCsDir);
    const staleServerCs = path.join(serverCsDir, `${className}.cs`);
    if (fs.existsSync(staleServerCs)) fs.unlinkSync(staleServerCs);
    fs.writeFileSync(path.join(serverCsDir, `${className}.g.cs`), serverCsContent, 'utf-8');

    // ── Collect string keys for StringIds.cs ─────────────────────────────────
    if (subDir === 'string' && baseName === 'client_string') {
      const pkCol = schema.columns.find(c => c.constraints.includes('PK'));
      if (pkCol) {
        stringIdsKeys = clientData.map(row => row[pkCol.name]).filter(Boolean);
      }
    }

    // ── Domain POCO + auto-service ────────────────────────────────────────────
    const csvTopDir   = subDir === '.' ? '' : subDir.split(path.sep)[0];
    const skipDomain  = !csvTopDir || SKIP_DOMAIN_SUBDIRS.has(csvTopDir) || SKIP_DOMAIN_POCO_FILES.has(baseName);
    const skipAutoSvc = skipDomain || SKIP_AUTO_SVC_FILES.has(baseName) || serverPkCols.length !== 1;

    if (!skipDomain) {
      const domainCls = toDomainPocoClassName(baseName);
      fs.writeFileSync(
        path.join(domainStaticDataDir, `${domainCls}.g.cs`),
        generateDomainPocoContent(domainCls, serverCols, rel),
        'utf-8'
      );
      if (!skipAutoSvc) {
        const pk = serverPkCols[0];
        autoServiceEntries.push({
          pocoClass:   domainCls,
          baseClass:   className,
          pkCsType:    toCSharpType(pk.type),
          pkCamelName: pk.name,
          fieldName:   toServiceFieldName(className),
          loaderClass: `${className}Loader`,
          csvFile:     `${baseName}.csv`,
          subDir:      csvTopDir,
          serverCols,
        });
      }
    }

    // ── Collect CS data for hashing ───────────────────────────────────────────
    for (const col of csCols) {
      csSchemaLines.push(`${resourcePath}\t${col.name}\t${col.type}\t${col.constraints.join(',')}`);
    }
    serverData.forEach((row, rowIdx) => {
      for (const col of csCols) {
        const val = row[col.name];
        csDataLines.push(`${resourcePath}\t${rowIdx}\t${col.name}\t${val === null ? '' : String(val)}`);
      }
    });

    // ── Collect client CSV for bundle (C+CS scope) ────────────────────────────
    clientBundleFiles[resourcePath] = clientCsvText;

    console.log(`[gen-data] OK: ${rel}`);
  }

  if (allErrors.length > 0) {
    console.error('');
    for (const { rel, errors } of allErrors) {
      console.error(`[gen-data] ERROR: ${rel}`);
      for (const e of errors) {
        console.error(`  Row ${e.row}, Field "${e.field}": ${e.msg}`);
      }
    }
    console.error(`\n${totalErrors} error(s) found. Aborting.`);
    process.exit(1);
  }

  // ── Generate StringIds.cs (Editor-only, from string/client_string.csv) ─────
  if (stringIdsKeys.length > 0) {
    const editorDir  = path.join(clientScriptsGenerated, '../../Editor');
    const outputPath = path.join(editorDir, 'StringIds.cs');
    ensureDir(editorDir);
    fs.writeFileSync(outputPath, generateStringIds(stringIdsKeys, 'string/client_string.csv'), 'utf-8');
    console.log(`[gen-data] StringIds.cs: ${stringIdsKeys.length} key(s) → ${path.relative(cfg.root, outputPath)}`);
  }

  // ── Write domain interface + service files ────────────────────────────────
  if (autoServiceEntries.length > 0) {
    fs.writeFileSync(ifaceFilePath,   generateInterfaceFile(autoServiceEntries),   'utf-8');
    fs.writeFileSync(serviceFilePath, generateServiceFile(autoServiceEntries), 'utf-8');
    console.log(`[gen-data] Domain: generated ${autoServiceEntries.length} auto-service table(s).`);
  }

  // ── Compute CS hashes ─────────────────────────────────────────────────────
  csSchemaLines.sort();
  csDataLines.sort();
  const dataSchemaVersion = sha256hex(csSchemaLines.join('\n'));
  const metaHashCs        = sha256hex(csDataLines.join('\n'));

  // ── Write hash files to both outputs ─────────────────────────────────────
  const clientDataRoot = path.join(clientGenerated, 'data');
  const serverDataRoot = path.join(serverGenerated, 'data');
  ensureDir(clientDataRoot);
  ensureDir(serverDataRoot);

  fs.writeFileSync(path.join(clientDataRoot, 'data_schema_version.txt'), dataSchemaVersion, 'utf-8');
  fs.writeFileSync(path.join(clientDataRoot, 'meta_hash_cs.txt'),        metaHashCs,        'utf-8');
  fs.writeFileSync(path.join(serverDataRoot, 'data_schema_version.txt'), dataSchemaVersion, 'utf-8');
  fs.writeFileSync(path.join(serverDataRoot, 'meta_hash_cs.txt'),        metaHashCs,        'utf-8');

  // ── Write client bundle JSON to server output ─────────────────────────────
  const bundle = JSON.stringify({ schemaVersion: dataSchemaVersion, metaHash: metaHashCs, files: clientBundleFiles });
  fs.writeFileSync(path.join(serverDataRoot, 'client_bundle.json'), bundle, 'utf-8');

  console.log(`[gen-data] Done: ${csvFiles.length} file(s) processed.`);
  console.log(`[gen-data] dataSchemaVersion: ${dataSchemaVersion}`);
  console.log(`[gen-data] metaHash:          ${metaHashCs}`);
}

try {
  main();
} catch (e) {
  console.error('[gen-data] Unexpected error:', e.message);
  if (e.stack) console.error(e.stack);
  process.exit(1);
}
