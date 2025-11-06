function doPost(e) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const data = parseIncoming_(e);
  return processEvent_(ss, data);
}





function setupVisualization() {
  const ss = SpreadsheetApp.getActive();
  ensureDataSheet_(ss);
  // Ensure heatmap grids exist but do not rely on raw rows
  ensureHeatmapGridSheet_(ss, 'Level1Scene');
  ensureHeatmapGridSheet_(ss, 'Level2Scene');
  updateHeatmapDataFromGrid_(ss, 'Level1Scene');
  updateHeatmapDataFromGrid_(ss, 'Level2Scene');
  attachHeatmapChart_(ss, 'Level1Scene');
  attachHeatmapChart_(ss, 'Level2Scene');
  pruneAnalyticsSheets_(ss, { essentialsOnly: true });
}

function ensureAllAnalyticsSheets_(ss, opts) {
  ensureAvgTimeSuccessSheet_(ss, opts);
  ensureAvgTimeFailureSheet_(ss, opts);
  ensureAvgTimeAllSheet_(ss, opts);
  ensureAvgTimeSheet_(ss, opts); 
  // Heatmap grids (no raw rows)
  ensureHeatmapGridSheet_(ss, 'Level1Scene');
  ensureHeatmapGridSheet_(ss, 'Level2Scene');
  if (opts && opts.prune) {
    pruneAnalyticsSheets_(ss);
  }
}

function ensureAvgTimeSuccessSheet_(ss, opts) {
  let sh = ss.getSheetByName("AvgTime_Success");
  if (!sh) sh = ss.insertSheet("AvgTime_Success");
  else if (opts && opts.reset) sh.clear();

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE and (C='Level1Scene' or C='Level1' or C='Level2Scene' or C='Level2') group by C label C 'level_id', avg(E) 'avg_time_success_s'", 0)`;
  sh.getRange("A1").setValue(formula);

  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
  if (sh.getCharts().length === 0) {
    const chart = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange("A:B"))
      .setPosition(1, 3, 0, 0)
      .setOption("title", "Average Time per Level (Success)")
      .setOption("legend", { position: "none" })
      .setOption("hAxis", { title: "Level" })
      .setOption("vAxis", { title: "Average Time (s)" })
      .build();
    sh.insertChart(chart);
  }
}

function ensureAvgTimeFailureSheet_(ss, opts) {
  let sh = ss.getSheetByName("AvgTime_Failure");
  if (!sh) sh = ss.insertSheet("AvgTime_Failure");
  else if (opts && opts.reset) sh.clear();

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = FALSE and (C='Level1Scene' or C='Level1' or C='Level2Scene' or C='Level2') group by C label C 'level_id', avg(E) 'avg_time_failure_s'", 0)`;
  sh.getRange("A1").setValue(formula);

  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
  if (sh.getCharts().length === 0) {
    const chart = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange("A:B"))
      .setPosition(1, 3, 0, 0)
      .setOption("title", "Average Time per Level (Failure)")
      .setOption("legend", { position: "none" })
      .setOption("hAxis", { title: "Level" })
      .setOption("vAxis", { title: "Average Time (s)" })
      .build();
    sh.insertChart(chart);
  }
}


function ensureAvgTimeAllSheet_(ss, opts) {
  let sh = ss.getSheetByName("AvgTime_All");
  if (!sh) sh = ss.insertSheet("AvgTime_All");
  else if (opts && opts.reset) sh.clear();

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where (C='Level1Scene' or C='Level1' or C='Level2Scene' or C='Level2') group by C label C 'level_id', avg(E) 'avg_time_all_s'", 0)`;
  sh.getRange("A1").setValue(formula);

  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
  if (sh.getCharts().length === 0) {
    const chart = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange("A:B"))
      .setPosition(1, 3, 0, 0)
      .setOption("title", "Average Time per Level (All Attempts)")
      .setOption("legend", { position: "none" })
      .setOption("hAxis", { title: "Level" })
      .setOption("vAxis", { title: "Average Time (s)" })
      .build();
    sh.insertChart(chart);
  }
}

function ensureAvgTimeSheet_(ss, opts) {
  let sh = ss.getSheetByName("AvgTime");
  if (!sh) sh = ss.insertSheet("AvgTime");
  else if (opts && opts.reset) sh.clear();

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE and (C='Level1Scene' or C='Level1' or C='Level2Scene' or C='Level2') group by C label C 'level_id', avg(E) 'avg_time_s'", 0)`;
  sh.getRange("A1").setValue(formula);

  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
  if (sh.getCharts().length === 0) {
    const chart = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange("A:B"))
      .setPosition(1, 3, 0, 0)
      .setOption("title", "Average Time per Level (success only)")
      .setOption("legend", { position: "none" })
      .setOption("hAxis", { title: "Level" })
      .setOption("vAxis", { title: "Average Time (s)" })
      .build();
    sh.insertChart(chart);
  }
}

function ensureHeatmapCountsSheet_(ss, opts) {
  let sh = ss.getSheetByName('Heatmap_Counts');
  if (!sh) sh = ss.insertSheet('Heatmap_Counts');
  else if (opts && opts.reset) sh.clear();
  const formula = "=QUERY(Hotspots!A2:I, \"select C, D, E, count(A), avg(F), avg(G), avg(H), avg(I) where (C='Level1Scene' or C='Level1' or C='Level2Scene' or C='Level2') group by C, D, E label C 'level_id', D 'grid_x', E 'grid_y', count(A) 'fail_count', avg(F) 'avg_time_s', avg(G) 'avg_hearts_remaining', avg(H) 'avg_fire_tokens', avg(I) 'avg_water_tokens'\", 0)";
  sh.getRange('A1').setValue(formula);
  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
}

function pruneAnalyticsSheets_(ss, opts) {
  const essentials = new Set(["Data", "Hotspots", "Heatmap_Counts"]);
  const withAverages = new Set(["Data", "Hotspots", "Heatmap_Counts", "AvgTime_Success", "AvgTime_Failure", "AvgTime_All", "AvgTime"]);
  const keep = (opts && opts.essentialsOnly) ? essentials : withAverages;
  ss.getSheets().forEach(sheet => {
    const name = sheet.getName();
    if (!keep.has(name)) {
      if (ss.getSheets().length > 1) {
        ss.deleteSheet(sheet);
      }
    }
  });
  // Remove any legacy sheets the user doesn't want
  ["Success Rate", "Success_Rate", "Median Success", "Median_Success"].forEach(n => {
    const s = ss.getSheetByName(n);
    if (s && ss.getSheets().length > 1) ss.deleteSheet(s);
  });
}


function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu("Chef Analytics")
    .addItem("Rebuild (Heatmap only)", "setupVisualization")
    .addItem("Rebuild (with averages)", "setupVisualizationWithAverages")
    .addItem("Prune other sheets (keep averages)", "pruneSheetsCommand_")
    .addItem("Prune to essentials", "pruneEssentialsCommand_")
    .addItem("Purge non-whitelisted rows", "purgeNonWhitelistedRows")
    .addSeparator()
    .addItem("Repair Average Formulas", "repairAverageTimeSheets")
    .addItem("Build Heatmaps (L1/L2)", "buildAllHeatmaps")
    .addItem("Create Heatmap: Level1Scene", "createHeatmap_Level1")
    .addItem("Create Heatmap: Level2Scene", "createHeatmap_Level2")
    .addToUi();
}

function pruneSheetsCommand_() {
  const ss = SpreadsheetApp.getActive();
  pruneAnalyticsSheets_(ss, { essentialsOnly: false });
}

function pruneEssentialsCommand_() {
  const ss = SpreadsheetApp.getActive();
  pruneAnalyticsSheets_(ss, { essentialsOnly: true });
}




function doGet(e) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  // If called without params, just health-check
  if (!e || !e.parameter || Object.keys(e.parameter).length === 0) {
    return ContentService.createTextOutput("OK")
      .setHeader('Access-Control-Allow-Origin', '*')
      .setHeader('Access-Control-Allow-Headers', 'Content-Type')
      .setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  }
  const data = e.parameter || {};
  return processEvent_(ss, data);
}

// Parse JSON form body or fallback to query params
function parseIncoming_(e) {
  let data = {};
  try {
    if (e && e.postData && e.postData.type === 'application/json') {
      data = JSON.parse(e.postData.contents || '{}');
    } else if (e && e.parameter) {
      data = e.parameter;
    }
  } catch (err) {
    Logger.log('parse error: ' + err);
  }
  return data || {};
}

// Central event handler for GET/POST
function processEvent_(ss, data) {
  const level = String(data.level_id || '').trim();
  const lvlLower = level.toLowerCase();
  const allowed = new Set(['level1', 'level1scene', 'level2', 'level2scene']);
  if (!allowed.has(lvlLower)) {
    return ContentService.createTextOutput(JSON.stringify({ ok: true, ignored: true }))
      .setMimeType(ContentService.MimeType.JSON)
      .setHeader('Access-Control-Allow-Origin', '*')
      .setHeader('Access-Control-Allow-Headers', 'Content-Type')
      .setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  }

  const evt = String(data.event || '').trim().toLowerCase();
  if (evt === 'fail') {
    const gridX = Number(data.grid_x) || 0;
    const gridY = Number(data.grid_y) || 0;
    incrementHeatmapGridCell_(ss, level, gridX, gridY, 1);

    return ContentService.createTextOutput(JSON.stringify({ ok: true, type: 'fail_grid' }))
      .setMimeType(ContentService.MimeType.JSON)
      .setHeader('Access-Control-Allow-Origin', '*')
      .setHeader('Access-Control-Allow-Headers', 'Content-Type')
      .setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  }

  // Default: level result row
  const sh = ensureDataSheet_(ss);
  const successText = String(data.success || '').toUpperCase();
  const timeSpent = Number(data.time_spent_s) || 0;
  sh.appendRow([
    new Date(),
    data.session_id || '',
    level,
    successText || 'FALSE',
    timeSpent
  ]);

  return ContentService.createTextOutput(JSON.stringify({ ok: true, type: 'result' }))
    .setMimeType(ContentService.MimeType.JSON)
    .setHeader('Access-Control-Allow-Origin', '*')
    .setHeader('Access-Control-Allow-Headers', 'Content-Type')
    .setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
}

// Rebuild and keep average time sheets too
function setupVisualizationWithAverages() {
  const ss = SpreadsheetApp.getActive();
  ensureDataSheet_(ss);
  ensureAllAnalyticsSheets_(ss, { reset: true, prune: true });
}

// Ensure the main Data sheet exists with headers
function ensureDataSheet_(ss) {
  let sh = ss.getSheetByName('Data');
  if (!sh) {
    sh = ss.insertSheet('Data');
    sh.getRange('A1:E1').setValues([[
      'timestamp', 'session_id', 'level_id', 'success', 'time_spent_s'
    ]]);
  }
  return sh;
}

// Helper to inject a sample hotspot row (for quick verification)
function debugInsertSampleHotspot() {
  const ss = SpreadsheetApp.getActive();
  const sh = ensureHotspotsSheet_(ss);
  sh.appendRow([new Date(), 'debug_session', 'Level1Scene', 3, 4, 12, 2, 1, 0]);
}

// Remove any Data/Hotspots rows whose level_id is not Level1/Level2
function purgeNonWhitelistedRows() {
  const ss = SpreadsheetApp.getActive();
  const allowed = new Set(['level1', 'level1scene', 'level2', 'level2scene']);

  const purgeSheet = (name, levelCol) => {
    const sh = ss.getSheetByName(name);
    if (!sh) return;
    const last = sh.getLastRow();
    if (last < 2) return;
    const rng = sh.getRange(2, levelCol, last - 1, 1).getValues();
    const toDelete = [];
    for (let i = 0; i < rng.length; i++) {
      const v = String(rng[i][0] || '').toLowerCase();
      if (!allowed.has(v)) toDelete.push(i + 2); // 1-based row index
    }
    // Delete bottom-up
    for (let j = toDelete.length - 1; j >= 0; j--) {
      sh.deleteRow(toDelete[j]);
    }
  };

  purgeSheet('Data', 3);      // C column = level_id
  purgeSheet('Hotspots', 3);  // C column = level_id
}

// Force the AvgTime_* formulas to only include Level1/Level2
function repairAverageTimeSheets() {
  const ss = SpreadsheetApp.getActive();
  const where = "(C='Level1Scene' or C='Level1' or C='Level2Scene' or C='Level2')";

  const setFormula = (name, f) => {
    let sh = ss.getSheetByName(name);
    if (!sh) sh = ss.insertSheet(name);
    sh.clear();
    sh.getRange('A1').setValue(f);
  };

  setFormula('AvgTime_Success', `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE and ${where} group by C label C 'level_id', avg(E) 'avg_time_success_s'", 0)`);
  setFormula('AvgTime_Failure', `=QUERY(Data!A2:E, "select C, avg(E) where D = FALSE and ${where} group by C label C 'level_id', avg(E) 'avg_time_failure_s'", 0)`);
  setFormula('AvgTime_All',     `=QUERY(Data!A2:E, "select C, avg(E) where ${where} group by C label C 'level_id', avg(E) 'avg_time_all_s'", 0)`);
  setFormula('AvgTime',         `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE and ${where} group by C label C 'level_id', avg(E) 'avg_time_s'", 0)`);
}

// ---------- Visual Heatmap Builders ----------
function buildAllHeatmaps() {
  const ss = SpreadsheetApp.getActive();
  ensureHeatmapGridSheet_(ss, 'Level1Scene');
  ensureHeatmapGridSheet_(ss, 'Level2Scene');
  // Reapply formatting in case sizes changed
  createHeatmapGrid_(ss, 'Level1Scene');
  createHeatmapGrid_(ss, 'Level2Scene');
  updateHeatmapDataFromGrid_(ss, 'Level1Scene');
  updateHeatmapDataFromGrid_(ss, 'Level2Scene');
  attachHeatmapChart_(ss, 'Level1Scene');
  attachHeatmapChart_(ss, 'Level2Scene');
}

function createHeatmap_Level1() {
  const ss = SpreadsheetApp.getActive();
  ensureHeatmapGridSheet_(ss, 'Level1Scene');
  createHeatmapGrid_(ss, 'Level1Scene');
  updateHeatmapDataFromGrid_(ss, 'Level1Scene');
  attachHeatmapChart_(ss, 'Level1Scene');
}

function createHeatmap_Level2() {
  const ss = SpreadsheetApp.getActive();
  ensureHeatmapGridSheet_(ss, 'Level2Scene');
  createHeatmapGrid_(ss, 'Level2Scene');
  updateHeatmapDataFromGrid_(ss, 'Level2Scene');
  attachHeatmapChart_(ss, 'Level2Scene');
}

function createHeatmapGrid_(ss, levelId) {
  // Ensure grid sheet exists and keep existing counts; only (re)apply formatting
  const sh = ensureHeatmapGridSheet_(ss, levelId);

  // Freeze headers and apply color-scale formatting to the data region
  const lastRow = Math.max(2, sh.getLastRow());
  const lastCol = Math.max(2, sh.getLastColumn());
  sh.setFrozenRows(1);
  sh.setFrozenColumns(1);

  // Range with current used values (for threshold calculation only)
  const usedRange = sh.getRange(2, 2, lastRow - 1, lastCol - 1);
  // Apply formatting to the full grid so new cells auto-inherit styling
  const fullRange = sh.getRange(2, 2, sh.getMaxRows() - 1, sh.getMaxColumns() - 1);

  // Compute thresholds from current values (simple 3-band scale)
  const values = usedRange.getValues();
  let maxVal = 0;
  for (let r = 0; r < values.length; r++) {
    for (let c = 0; c < values[r].length; c++) {
      const v = Number(values[r][c]) || 0;
      if (v > maxVal) maxVal = v;
    }
  }
  const t1 = Math.max(1, Math.floor(maxVal / 3));
  const t2 = Math.max(2, Math.floor((2 * maxVal) / 3));

  // Replace any prior rules for this range with 3 stepped rules
  const rules = sh.getConditionalFormatRules() || [];
  const otherRules = rules.filter(r => {
    const rngs = r.getRanges();
    return !rngs.some(rg => rg.getA1Notation() === fullRange.getA1Notation());
  });

  const ruleHigh = SpreadsheetApp.newConditionalFormatRule()
    .whenNumberGreaterThanOrEqualTo(t2)
    .setBackground('#d32f2f')
    .setRanges([fullRange])
    .build();
  const ruleMid = SpreadsheetApp.newConditionalFormatRule()
    .whenNumberGreaterThanOrEqualTo(t1)
    .setBackground('#ffb3b3')
    .setRanges([fullRange])
    .build();
  const ruleLow = SpreadsheetApp.newConditionalFormatRule()
    .whenNumberGreaterThan(0)
    .setBackground('#ffecec')
    .setRanges([fullRange])
    .build();
  sh.setConditionalFormatRules(otherRules.concat([ruleLow, ruleMid, ruleHigh]));

  // Optional: make it square-ish and readable
  for (let c = 2; c <= lastCol; c++) sh.setColumnWidth(c, 36);
  for (let r = 2; r <= lastRow; r++) sh.setRowHeight(r, 20);
}

// Ensure per-level grid sheet exists with headers (no formulas)
function ensureHeatmapGridSheet_(ss, levelId) {
  const name = `Heatmap_Grid_${levelId}`;
  let sh = ss.getSheetByName(name);
  if (!sh) {
    sh = ss.insertSheet(name);
    sh.getRange('A1').setValue('grid_y');
  }
  return sh;
}

// Increment a specific (grid_x, grid_y) cell by delta (default +1)
function incrementHeatmapGridCell_(ss, levelId, gridX, gridY, delta) {
  const sh = ensureHeatmapGridSheet_(ss, levelId);

  // Find or create column for gridX (headers in row 1 starting at B1)
  const headers = sh.getRange(1, 2, 1, sh.getLastColumn() - 1).getValues()[0];
  let colIndex = -1;
  for (let i = 0; i < headers.length; i++) {
    if (String(headers[i]) === String(gridX)) { colIndex = i + 2; break; }
  }
  if (colIndex === -1) {
    colIndex = sh.getLastColumn() + 1;
    sh.getRange(1, colIndex).setValue(gridX);
  }

  // Find or create row for gridY (labels in column A starting at A2)
  const yVals = sh.getRange(2, 1, Math.max(0, sh.getLastRow() - 1), 1).getValues().map(r => r[0]);
  let rowIndex = -1;
  for (let i = 0; i < yVals.length; i++) {
    if (String(yVals[i]) === String(gridY)) { rowIndex = i + 2; break; }
  }
  if (rowIndex === -1) {
    rowIndex = sh.getLastRow() + 1;
    sh.getRange(rowIndex, 1).setValue(gridY);
  }

  // Increment the cell
  const cell = sh.getRange(rowIndex, colIndex);
  const current = Number(cell.getValue()) || 0;
  cell.setValue(current + (delta || 1));

  // Keep derived data + chart in sync
  updateHeatmapDataFromGrid_(ss, levelId);
  attachHeatmapChart_(ss, levelId);
}

// Build/refresh hidden data sheet for charts: columns A(grid_x),B(grid_y),C(fail_count)
function updateHeatmapDataFromGrid_(ss, levelId) {
  const grid = ensureHeatmapGridSheet_(ss, levelId);
  const dataName = `Heatmap_Data_${levelId}`;
  let dataSh = ss.getSheetByName(dataName);
  if (!dataSh) dataSh = ss.insertSheet(dataName);

  const lastRow = Math.max(2, grid.getLastRow());
  const lastCol = Math.max(2, grid.getLastColumn());
  const xs = grid.getRange(1, 2, 1, lastCol - 1).getValues()[0];
  const ys = grid.getRange(2, 1, lastRow - 1, 1).getValues().map(r => r[0]);
  const vals = grid.getRange(2, 2, lastRow - 1, lastCol - 1).getValues();

  const out = [['grid_x', 'grid_y', 'fail_count']];
  for (let r = 0; r < ys.length; r++) {
    for (let c = 0; c < xs.length; c++) {
      const n = Number(vals[r][c]) || 0;
      if (n > 0) out.push([xs[c], ys[r], n]);
    }
  }

  dataSh.clear();
  dataSh.getRange(1, 1, out.length, out[0].length).setValues(out);
  // Older runtimes don't support setHidden; use hideSheet()
  try { dataSh.hideSheet(); } catch (e) {}
}

// Attach or ensure a bubble chart on the grid sheet using the hidden data sheet
function attachHeatmapChart_(ss, levelId) {
  const grid = ensureHeatmapGridSheet_(ss, levelId);
  const data = ss.getSheetByName(`Heatmap_Data_${levelId}`);
  if (!data) return;

  const title = `Failure Hotspots (${levelId})`;
  // If there is already a chart with this title, leave it
  const existing = grid.getCharts().find(c => (c.getOptions().get('title') || '') === title);
  if (existing) return;

  const lastCol = Math.max(2, grid.getLastColumn());
  let builder = grid.newChart()
    .addRange(data.getRange('A:C'))
    .setPosition(1, lastCol + 2, 0, 0)
    .setOption('title', title)
    .setOption('legend', { position: 'none' })
    .setOption('hAxis', { title: 'grid_x' })
    .setOption('vAxis', { title: 'grid_y' });

  // Prefer Bubble chart; fall back to Scatter if unsupported
  try {
    builder = builder.setChartType(Charts.ChartType.BUBBLE);
  } catch (e) {
    builder = builder.setChartType(Charts.ChartType.SCATTER);
  }

  const chart = builder.build();
  grid.insertChart(chart);
}

// Ensure the raw Hotspots sheet exists with headers
function ensureHotspotsSheet_(ss) {
  let sh = ss.getSheetByName('Hotspots');
  if (!sh) {
    sh = ss.insertSheet('Hotspots');
    sh.getRange('A1:I1').setValues([[
      'timestamp',
      'session_id',
      'level_id',
      'grid_x',
      'grid_y',
      'time_spent_s',
      'hearts_remaining',
      'fire_tokens',
      'water_tokens'
    ]]);
  }
  return sh;
}
