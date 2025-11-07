function doPost(e) {
  const data = parseIncoming_(e);
  try {
    const ss = getSpreadsheet_(data);
    return processEvent_(ss, data);
  } catch (err) {
    return ContentService.createTextOutput(JSON.stringify({ ok: false, error: String(err) }))
      .setMimeType(ContentService.MimeType.JSON)
      .setHeader('Access-Control-Allow-Origin', '*')
      .setHeader('Access-Control-Allow-Headers', 'Content-Type')
      .setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  }
}





// Resolve target spreadsheet: prefer active (bound), else 'sheet_id'/'sid' param, else Script Property 'SHEET_ID'
function getSpreadsheet_(data) {
  try {
    const ss = SpreadsheetApp.getActiveSpreadsheet();
    if (ss) return ss;
  } catch (e) {}
  try {
    const sid = (data && (data.sheet_id || data.sid)) || (PropertiesService && PropertiesService.getScriptProperties().getProperty('SHEET_ID'));
    if (sid) return SpreadsheetApp.openById(String(sid));
  } catch (e) {}
  throw new Error('No spreadsheet available. Bind the script to a Sheet or provide sheet_id/sid or Script Property SHEET_ID.');
}
function ensureAllAnalyticsSheets_(ss, opts) {
  ensureAvgTimeSuccessSheet_(ss, opts);
  ensureAvgTimeFailureSheet_(ss, opts);
  // Intentionally omit AvgTime_All and AvgTime
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


// AvgTime_All and AvgTime builders removed (we keep only Success/Failure)

// Heatmap-related helpers removed

function pruneAnalyticsSheets_(ss, opts) {
  const essentials = new Set(["Data"]);
  const withAverages = new Set(["Data", "AvgTime_Success", "AvgTime_Failure", "HeartLoss"]);
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
    .addItem("Rebuild (averages only)", "setupVisualizationWithAverages")
    .addItem("Prune other sheets (keep averages)", "pruneSheetsCommand_")
    .addItem("Prune to essentials", "pruneEssentialsCommand_")
    .addItem("Purge non-whitelisted rows", "purgeNonWhitelistedRows")
    .addSeparator()
    .addItem("Repair Average Formulas", "repairAverageTimeSheets")
     .addItem("Build Heart Loss Chart", "buildHeartLossCharts").addToUi();
}

function pruneSheetsCommand_() {
  const ss = SpreadsheetApp.getActive();
  pruneAnalyticsSheets_(ss, { essentialsOnly: false });
}

function pruneEssentialsCommand_() {
  const ss = SpreadsheetApp.getActive();
  pruneAnalyticsSheets_(ss, { essentialsOnly: true });
}

// pruneExtraHeatmapGrids removed




function doGet(e) {
  // Health check with no params
  if (!e || !e.parameter || Object.keys(e.parameter).length === 0) {
    return ContentService.createTextOutput("OK")
      .setHeader('Access-Control-Allow-Origin', '*')
      .setHeader('Access-Control-Allow-Headers', 'Content-Type')
      .setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  }
  const data = e.parameter || {};
  try {
    const ss = getSpreadsheet_(data);
    return processEvent_(ss, data);
  } catch (err) {
    return ContentService.createTextOutput(JSON.stringify({ ok: false, error: String(err) }))
      .setMimeType(ContentService.MimeType.JSON)
      .setHeader('Access-Control-Allow-Origin', '*')
      .setHeader('Access-Control-Allow-Headers', 'Content-Type')
      .setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  }
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
    // Heatmaps disabled: ignore fail-grid events
    return ContentService.createTextOutput(JSON.stringify({ ok: true, ignored: 'heatmap_disabled' }))
      .setMimeType(ContentService.MimeType.JSON)
      .setHeader('Access-Control-Allow-Origin', '*')
      .setHeader('Access-Control-Allow-Headers', 'Content-Type')
      .setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  }

  if (evt === 'heart_lost') {
    const sh = ensureHeartLossSheet_(ss);
    const session = String(data.session_id || '');
    const player = String(data.player || '');
    const cause = String(data.cause || '');
    const tSince = Number(data.time_since_start_s || data.t_since_start_s || data.time_spent_s || 0) || 0;
    sh.appendRow([new Date(), session, level, player, cause, tSince]);

    return ContentService.createTextOutput(JSON.stringify({ ok: true, type: 'heart_loss' }))
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

// debugInsertSampleHotspot removed

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
  // Omit AvgTime_All and AvgTime per request
}

// ---------- Heart Loss Metrics ----------
function ensureHeartLossSheet_(ss) {
  let sh = ss.getSheetByName('HeartLoss');
  if (!sh) {
    sh = ss.insertSheet('HeartLoss');
    sh.getRange('A1:F1').setValues([[
      'timestamp', 'session_id', 'level_id', 'player', 'cause', 'time_since_start_s'
    ]]);
  }
  return sh;
}

// Build per-attempt heart losses and an averaged view (losses per attempt by cause)
// All heatmap and hotspots-specific functions removed to simplify script

// Build a simple bar chart of heart losses by cause (Level1/Level2 only)
// Level-wise heart loss charts (two separate sheets for Level1 and Level2)
// Place two bar charts (Level 1 and Level 2) directly on the HeartLoss sheet
function ensureHeartLossChartsOnSheet_(ss, opts) {
  const sh = ensureHeartLossSheet_(ss);
  // Place formulas in side columns to avoid the A:F data range
  const l1Where = "(C='Level1Scene' or C='Level1')";
  const l2Where = "(C='Level2Scene' or C='Level2')";
  const f1 = `=QUERY(HeartLoss!A2:F, "select E, count(A) where ${l1Where} group by E label E 'cause', count(A) 'loss_count'", 0)`;
  const f2 = `=QUERY(HeartLoss!A2:F, "select E, count(A) where ${l2Where} group by E label E 'cause', count(A) 'loss_count'", 0)`;
  sh.getRange('H1').setValue(f1); // H:I used for Level 1
  sh.getRange('K1').setValue(f2); // K:L used for Level 2

  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
  if (sh.getCharts().length < 2) {
    // Chart 1: Level 1 causes
    const c1 = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('H:I'))
      .setPosition(1, 8, 0, 0) // Row 1, Col H
      .setOption('title', 'Heart Losses by Cause — Level 1')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Cause' })
      .setOption('vAxis', { title: 'Loss Count' })
      .build();
    sh.insertChart(c1);

    // Chart 2: Level 2 causes
    const c2 = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('K:L'))
      .setPosition(16, 8, 0, 0) // Place lower on the sheet
      .setOption('title', 'Heart Losses by Cause — Level 2')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Cause' })
      .setOption('vAxis', { title: 'Loss Count' })
      .build();
    sh.insertChart(c2);
  }
}

function buildHeartLossCharts() {
  const ss = SpreadsheetApp.getActive();
  ensureHeartLossChartsOnSheet_(ss, { reset: true });
}