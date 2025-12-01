function doPost(e) {
  const data = parseIncoming_(e);
  try {
    const ss = getSpreadsheet_(data);
    return processEvent_(ss, data);
  } catch (err) {
    return ContentService.createTextOutput(JSON.stringify({ ok: false, error: String(err) }))
      .setMimeType(ContentService.MimeType.JSON);
  }
}

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
  ensureRetryDensitySheet_(ss, opts);
  ensureTokenCompletionSheet_(ss, opts);
  ensureCompletionFunnelSheet_(ss, opts);
  ensureFailHotspotHeatmapSheet_(ss, opts);
  if (opts && opts.prune) {
    pruneAnalyticsSheets_(ss);
  }
}

function ensureAvgTimeSuccessSheet_(ss, opts) {
  let sh = ss.getSheetByName('AvgTime_Success');
  if (!sh) sh = ss.insertSheet('AvgTime_Success');
  else if (opts && opts.reset) sh.clear();

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE group by C label C 'level_id', avg(E) 'avg_time_success_s'", 0)`;
  sh.getRange('A1').setValue(formula);

  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
  if (sh.getCharts().length === 0) {
    const chart = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('A:B'))
      .setPosition(1, 3, 0, 0)
      .setOption('title', 'Average Time per Level (Success)')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Level' })
      .setOption('vAxis', { title: 'Average Time (s)' })
      .build();
    sh.insertChart(chart);
  }
}

function ensureAvgTimeFailureSheet_(ss, opts) {
  let sh = ss.getSheetByName('AvgTime_Failure');
  if (!sh) sh = ss.insertSheet('AvgTime_Failure');
  else if (opts && opts.reset) sh.clear();

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = FALSE group by C label C 'level_id', avg(E) 'avg_time_failure_s'", 0)`;
  sh.getRange('A1').setValue(formula);

  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
  if (sh.getCharts().length === 0) {
    const chart = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('A:B'))
      .setPosition(1, 3, 0, 0)
      .setOption('title', 'Average Time per Level (Failure)')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Level' })
      .setOption('vAxis', { title: 'Average Time (s)' })
      .build();
    sh.insertChart(chart);
  }
}

// Retry Density = fail_count / total_count 
function ensureRetryDensitySheet_(ss, opts) {
  let sh = ss.getSheetByName('RetryDensity');
  if (!sh) sh = ss.insertSheet('RetryDensity');
  else if (opts && opts.reset) sh.clear();

  const dataSh = ss.getSheetByName('Data') || ensureDataSheet_(ss);
  const last = dataSh.getLastRow();
  const rows = last > 1 ? dataSh.getRange(2, 1, last - 1, 5).getValues() : [];

  const stats = new Map(); // level -> { total, fail }
  rows.forEach(r => {
    const level = String(r[2] || '').trim();
    if (!level) return;
    let success = r[3];
    if (typeof success !== 'boolean') success = /^(1|true|yes|y)$/i.test(String(success || ''));
    const s = stats.get(level) || { total: 0, fail: 0 };
    s.total += 1;
    if (!success) s.fail += 1;
    stats.set(level, s);
  });

  sh.clear();
  const out = [['level_id','fail_count','total_count','retry_density']];
  Array.from(stats.entries()).forEach(([level, s]) => {
    const density = s.total ? s.fail / s.total : '';
    out.push([level, s.fail, s.total, density]);
  });
  sh.getRange(1, 1, out.length, 4).setValues(out);

  try { sh.setConditionalFormatRules([]); } catch (e) {}

  // Show retry density values as whole-number percentages (e.g. 0.62 -> 62%).
  sh.getRange('D:D').setNumberFormat('0%');

  if (opts && opts.reset) {
    sh.getCharts().forEach(function(c){ sh.removeChart(c); });
  }
  if (sh.getCharts().length === 0) {
    const chart = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('A:A'))
      .addRange(sh.getRange('D:D'))
      .setNumHeaders(1)
      .setPosition(1, 6, 0, 0)
      .setOption('title', 'Retry Density by Level')
      .setOption('legend', { position: 'none' })
      .setOption('vAxis', { title: 'Retry Density', viewWindow: { min: 0, max: 1 }, format: 'percent' })
      .setOption('hAxis', { title: 'Level' })
      .setOption('series', { 0: { dataLabel: 'value' } })
      .build();
    sh.insertChart(chart);
  }
}

function ensureTokenCompletionSheet_(ss, opts) {
  let sh = ss.getSheetByName('TokenCompletion');
  let created = false;
  if (sh && opts && opts.reset && opts.fullReset) {
    ss.deleteSheet(sh);
    sh = null;
  }
  if (!sh) {
    sh = ss.insertSheet('TokenCompletion');
    created = true;
  }
  sh.getRange('A1:G1').setValues([[
    'timestamp', 'session_id', 'level_id', 'token_completion_rate', 'tokens_collected', 'tokens_available', 'time_spent_s'
  ]]);
  sh.getRange('G:G').setNumberFormat('0');
  sh.getRange('D:D').setNumberFormat('0%');

  // Only manage charts when explicitly requested (e.g., from the menu action).
  if (opts && opts.buildCharts) {
    if (opts.reset) {
      safeGetCharts_(sh).forEach(function(chart) {
        sh.removeChart(chart);
      });
    }
    if (safeGetCharts_(sh).length === 0) {
      const chart = sh.newChart()
        .asColumnChart()
        .addRange(sh.getRange('A:B'))
        .setPosition(1, 3, 0, 0)
        .setOption('title', 'Token Completion Rate')
        .setOption('legend', { position: 'none' })
        .setOption('hAxis', { title: 'Level' })
        .setOption('vAxis', { title: 'Rate' })
        .build();
      sh.insertChart(chart);
    }
  }
  return sh;
}

function safeGetCharts_(sh) {
  try {
    return sh.getCharts();
  } catch (err) {
    Logger.log('getCharts failed: ' + err);
    return [];
  }
}


function buildTokenCompletionCharts() {
  const ss = SpreadsheetApp.getActive();
  ensureTokenCompletionSheet_(ss, { reset: true, buildCharts: true });
}

function buildCompletionFunnel() {
  const ss = SpreadsheetApp.getActive();
  ensureCompletionFunnelSheet_(ss, { reset: true });
}

function buildFailHotspotHeatmap() {
  const ss = SpreadsheetApp.getActive();
  ensureFailHotspotHeatmapSheet_(ss, { reset: true });
}

function ensureFailHotspotsSheet_(ss) {
  let sh = ss.getSheetByName('FailHotspots');
  if (!sh) {
    sh = ss.insertSheet('FailHotspots');
    sh.getRange('A1:J1').setValues([[
      'timestamp', 'session_id', 'level_id', 'grid_x', 'grid_y', 'hearts_remaining', 'fire_tokens', 'water_tokens', 'time_spent_s', 'cause'
    ]]);
  }
  return sh;
}

// Aggregated heatmap counts per level / grid cell for failure hotspots.
// Sheet: FailHotspotHeatmap
// Columns: level_id, grid_x, grid_y, fail_count
function ensureFailHotspotHeatmapSheet_(ss, opts) {
  let sh = ss.getSheetByName('FailHotspotHeatmap');
  if (!sh) {
    sh = ss.insertSheet('FailHotspotHeatmap');
  }
  if (opts && opts.reset) {
    sh.clear();
    safeGetCharts_(sh).forEach(function(c){ sh.removeChart(c); });
  }

  const raw = ensureFailHotspotsSheet_(ss);
  const last = raw.getLastRow();
  const rows = last > 1 ? raw.getRange(2, 1, last - 1, 10).getValues() : [];

  const counts = new Map(); // key: level|gx|gy -> count
  rows.forEach(r => {
    const level = String(r[2] || '').trim();
    const gx = Number(r[3]);
    const gy = Number(r[4]);
    if (!level || !isFinite(gx) || !isFinite(gy)) return;
    const key = `${level}|${gx}|${gy}`;
    counts.set(key, (counts.get(key) || 0) + 1);
  });

  const out = [['level_id','grid_x','grid_y','fail_count']];
  Array.from(counts.entries()).sort().forEach(([key, count]) => {
    const parts = key.split('|');
    out.push([parts[0], Number(parts[1]), Number(parts[2]), count]);
  });

  sh.clear();
  sh.getRange(1, 1, out.length, out[0].length).setValues(out);
  sh.getRange('D:D').setNumberFormat('0');

  if (opts && opts.reset) safeGetCharts_(sh).forEach(function(c){ sh.removeChart(c); });
  if (sh.getCharts().length === 0 && out.length > 1) {
    // Scatter chart by grid position. Filter the sheet by level_id to isolate a level; fail_count is visible in column D.
    const chart = sh.newChart()
      .asScatterChart()
      .addRange(sh.getRange(1, 2, out.length, 2)) // grid_x, grid_y
      .setPosition(1, 6, 0, 0)
      .setOption('title', 'Fail Hotspots (filter sheet by level_id to isolate)')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Grid X' })
      .setOption('vAxis', { title: 'Grid Y' })
      .setOption('pointSize', 6)
      .build();
    sh.insertChart(chart);
  }
}

// ---------- Completion Funnel & Quit Rate ----------
// Sheet: CompletionFunnel
// Columns: level_id, starts, completions, avg_attempts_per_completion
function ensureCompletionFunnelSheet_(ss, opts) {
  let sh = ss.getSheetByName('CompletionFunnel');
  if (!sh) {
    sh = ss.insertSheet('CompletionFunnel');
  }
  if (opts && opts.reset) {
    sh.clear();
    safeGetCharts_(sh).forEach(function(c){ sh.removeChart(c); });
  }

  const dataSh = ss.getSheetByName('Data') || ensureDataSheet_(ss);
  const last = dataSh.getLastRow();
  const rows = last > 1 ? dataSh.getRange(2, 1, last - 1, 5).getValues() : [];

  const starts = new Map();     // level -> Set(session)
  const successes = new Map();  // level -> Set(session)
  const attempts = new Map();   // level -> Map(session -> { tries, succeeded })

  const getSet = (map, key) => {
    let s = map.get(key);
    if (!s) { s = new Set(); map.set(key, s); }
    return s;
  };
  const getAttempts = (level, session) => {
    let m = attempts.get(level);
    if (!m) { m = new Map(); attempts.set(level, m); }
    let rec = m.get(session);
    if (!rec) { rec = { tries: 0, succeeded: false }; m.set(session, rec); }
    return rec;
  };

  rows.forEach(r => {
    const session = String(r[1] || '').trim();
    const level = String(r[2] || '').trim();
    if (!level || !session) return;
    const success = (typeof r[3] === 'boolean') ? r[3] : (/^(1|true|yes|y)$/i.test(String(r[3] || '')));

    getSet(starts, level).add(session);
    if (success) getSet(successes, level).add(session);

    const rec = getAttempts(level, session);
    if (!rec.succeeded) {
      rec.tries += 1;
      if (success) rec.succeeded = true;
    }
  });

  const allLevels = new Set([
    ...starts.keys(),
    ...successes.keys(),
    ...attempts.keys()
  ]);

  const out = [['level_id','starts','completions','avg_attempts_per_completion']];
  Array.from(allLevels).sort().forEach(level => {
    const startCount = starts.has(level) ? starts.get(level).size : 0;
    const successCount = successes.has(level) ? successes.get(level).size : 0;

    let avgAttempts = '';
    if (attempts.has(level)) {
      let total = 0;
      let completed = 0;
      attempts.get(level).forEach(rec => {
        if (rec.succeeded) {
          total += rec.tries;
          completed += 1;
        }
      });
      if (completed > 0) {
        avgAttempts = total / completed;
      }
    }

    out.push([level, startCount, successCount, avgAttempts]);
  });

  sh.clear();
  sh.getRange(1, 1, out.length, out[0].length).setValues(out);
  sh.getRange('D:D').setNumberFormat('0.00');

  if (opts && opts.reset) safeGetCharts_(sh).forEach(function(c){ sh.removeChart(c); });
  if (sh.getCharts().length === 0) {
    // Funnel bar chart (starts vs completions)
    const chart1 = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('A:B')) // level, starts
      .addRange(sh.getRange('C:C')) // completions
      .setNumHeaders(1)
      .setPosition(1, 7, 0, 0)
      .setOption('title', 'Level Funnel: Started vs Completed')
      .setOption('legend', { position: 'right' })
      .setOption('hAxis', { title: 'Level' })
      .setOption('vAxis', { title: 'Players' })
      .build();
    sh.insertChart(chart1);

    // Attempts per completion line chart
    const chart2 = sh.newChart()
      .asLineChart()
      .addRange(sh.getRange('A:A')) // level
      .addRange(sh.getRange('D:D')) // avg attempts
      .setNumHeaders(1)
      .setPosition(20, 7, 0, 0)
      .setOption('title', 'Avg Attempts per Completed Level')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Level' })
      .setOption('vAxis', { title: 'Attempts' })
      .build();
    sh.insertChart(chart2);
  }
}

function pruneAnalyticsSheets_(ss, opts) {
  const essentials = new Set(['Data']);
  const withAverages = new Set(['Data', 'AvgTime_Success', 'AvgTime_Failure', 'RetryDensity', 'HeartLoss', 'TokenCompletion', 'CompletionFunnel', 'FailHotspots', 'FailHotspotHeatmap']);
  const keep = (opts && opts.essentialsOnly) ? essentials : withAverages;
  ss.getSheets().forEach(sheet => {
    const name = sheet.getName();
    if (!keep.has(name)) {
      if (ss.getSheets().length > 1) {
        ss.deleteSheet(sheet);
      }
    }
  });
  ['Success Rate', 'Success_Rate', 'Median Success', 'Median_Success'].forEach(n => {
    const s = ss.getSheetByName(n);
    if (s && ss.getSheets().length > 1) ss.deleteSheet(s);
  });
}

function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu('Chef Analytics')
    .addItem('Rebuild (averages only)', 'setupVisualizationWithAverages')
    .addItem('Prune other sheets (keep averages)', 'pruneSheetsCommand_')
    .addItem('Prune to essentials', 'pruneEssentialsCommand_')
    .addItem('Purge non-whitelisted rows', 'purgeNonWhitelistedRows')
    .addSeparator()
    .addItem('Repair Average Formulas', 'repairAverageTimeSheets')
    .addItem('Build Heart Loss Chart', 'buildHeartLossCharts')
    .addItem('Build Token Completion Chart', 'buildTokenCompletionCharts')
    .addItem('Build Completion Funnel', 'buildCompletionFunnel')
    .addItem('Build Fail Hotspot Heatmap', 'buildFailHotspotHeatmap')
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
  if (!e || !e.parameter || Object.keys(e.parameter).length === 0) {
    return ContentService.createTextOutput('OK');
  }
  const data = e.parameter || {};
  try {
    const ss = getSpreadsheet_(data);
    return processEvent_(ss, data);
  } catch (err) {
    return ContentService.createTextOutput(JSON.stringify({ ok: false, error: String(err) }))
      .setMimeType(ContentService.MimeType.JSON);
  }
}

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

function toNumberOrBlank_(value) {
  if (value === undefined || value === null || value === '') return '';
  const num = Number(value);
  return isFinite(num) ? num : '';
}

function extractTokenCompletionStats_(data) {
  const rateRaw = toNumberOrBlank_(data && data.token_completion_rate);
  const tokensCollectedRaw = toNumberOrBlank_(data && data.tokens_collected);
  const tokensAvailableRaw = toNumberOrBlank_(data && data.tokens_available);

  const rate = rateRaw === '' ? '' : Math.max(0, Math.min(1, rateRaw));
  const tokensCollected = tokensCollectedRaw === '' ? '' : Math.max(0, Math.round(tokensCollectedRaw));
  const tokensAvailable = tokensAvailableRaw === '' ? '' : Math.max(0, Math.round(tokensAvailableRaw));

  return [rate, tokensCollected, tokensAvailable];
}


function processEvent_(ss, data) {
  const level = String(data.level_id || '').trim();
  const evt = String(data.event || '').trim().toLowerCase();

  if (evt === 'fail' || /^restart/.test(evt) || evt === 'retry') {
    const shFail = ensureDataSheet_(ss);
    const tSinceFail = Number(data.time_since_start_s || data.t_since_start_s || data.time_spent_s || 0) || 0;
    shFail.appendRow([
      new Date(),
      data.session_id || '',
      level,
      false, 
      tSinceFail
    ]);
    // Capture failure hotspots (grid position) if provided
    if (data.grid_x !== undefined && data.grid_y !== undefined) {
      const gx = Number(data.grid_x);
      const gy = Number(data.grid_y);
      if (isFinite(gx) && isFinite(gy)) {
        const shHotspots = ensureFailHotspotsSheet_(ss);
        const hearts = toNumberOrBlank_(data.hearts_remaining);
        const fireTokens = toNumberOrBlank_(data.fire_tokens);
        const waterTokens = toNumberOrBlank_(data.water_tokens);
        const cause = String(data.cause || '');
        shHotspots.appendRow([
          new Date(),
          data.session_id || '',
          level,
          gx,
          gy,
          hearts,
          fireTokens,
          waterTokens,
          tSinceFail,
          cause
        ]);
        try { ensureFailHotspotHeatmapSheet_(ss, { reset: false }); } catch (e) { Logger.log(e); }
      }
    }
    try { ensureRetryDensitySheet_(ss, { reset: false }); } catch (e) { Logger.log(e); }
    try { ensureCompletionFunnelSheet_(ss, { reset: false }); } catch (e) { Logger.log(e); }
    try { ensureAvgTimeSuccessSheet_(ss, { reset: false }); } catch (e) { Logger.log(e); }
    try { ensureAvgTimeFailureSheet_(ss, { reset: false }); } catch (e) { Logger.log(e); }
    return ContentService.createTextOutput(JSON.stringify({ ok: true, type: 'result', recorded: 'failure', reason: evt }))
      .setMimeType(ContentService.MimeType.JSON);
  }

  if (evt === 'heart_lost') {
    const sh = ensureHeartLossSheet_(ss);
    const session = String(data.session_id || '');
    const player = String(data.player || '');
    const cause = String(data.cause || '');
    const tSince = Number(data.time_since_start_s || data.t_since_start_s || data.time_spent_s || 0) || 0;
    sh.appendRow([new Date(), session, level, player, cause, tSince]);

    return ContentService.createTextOutput(JSON.stringify({ ok: true, type: 'heart_loss' }))
      .setMimeType(ContentService.MimeType.JSON);
  }

  if (evt === 'token_completion') {
    const shTokens = ensureTokenCompletionSheet_(ss);
    const [tokenCompletionRate, tokensCollected, tokensAvailable] = extractTokenCompletionStats_(data);
    const tRaw = (data.time_spent_s !== undefined && data.time_spent_s !== null)
      ? data.time_spent_s
      : (data.time_since_start_s !== undefined && data.time_since_start_s !== null)
        ? data.time_since_start_s
        : data.t_since_start_s;
    const tSpent = toNumberOrBlank_(tRaw);
    shTokens.appendRow([
      new Date(),
      data.session_id || '',
      level,
      tokenCompletionRate,
      tokensCollected,
      tokensAvailable,
      tSpent
    ]);
    return ContentService.createTextOutput(JSON.stringify({ ok: true, type: 'token_completion' }))
      .setMimeType(ContentService.MimeType.JSON);
  }

  const sh = ensureDataSheet_(ss);
  const sRaw = (data && data.success);
  const successBool = (typeof sRaw === 'boolean') ? sRaw : (/^(1|true|yes|y)$/i.test(String(sRaw || '')));
  const timeSpent = Number(data.time_spent_s) || 0;
  sh.appendRow([
    new Date(),
    data.session_id || '',
    level,
    successBool,
    timeSpent
  ]);
  try { ensureRetryDensitySheet_(ss, { reset: false }); } catch (e) { Logger.log(e); }
  try { ensureCompletionFunnelSheet_(ss, { reset: false }); } catch (e) { Logger.log(e); }
  try { ensureAvgTimeSuccessSheet_(ss, { reset: false }); } catch (e) { Logger.log(e); }
  try { ensureAvgTimeFailureSheet_(ss, { reset: false }); } catch (e) { Logger.log(e); }

  return ContentService.createTextOutput(JSON.stringify({ ok: true, type: 'result' }))
    .setMimeType(ContentService.MimeType.JSON);
}

function setupVisualizationWithAverages() {
  const ss = SpreadsheetApp.getActive();
  ensureDataSheet_(ss);
  ensureAllAnalyticsSheets_(ss, { reset: true, prune: true });
}

// Ensure the main Data sheet 
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

function purgeNonWhitelistedRows() {
  const ss = SpreadsheetApp.getActive();
  const allowed = null; // allow all levels; keep rows untouched

  const purgeSheet = (name, levelCol) => {
    const sh = ss.getSheetByName(name);
    if (!sh) return;
    const last = sh.getLastRow();
    if (last < 2) return;
    const rng = sh.getRange(2, levelCol, last - 1, 1).getValues();
    const toDelete = [];
    for (let i = 0; i < rng.length; i++) {
      const v = String(rng[i][0] || '').toLowerCase();
      if (allowed && !allowed.has(v)) toDelete.push(i + 2);
    }
    for (let j = toDelete.length - 1; j >= 0; j--) {
      sh.deleteRow(toDelete[j]);
    }
  };

  purgeSheet('Data', 3);      
  purgeSheet('HeartLoss', 3); 
  purgeSheet('TokenCompletion', 3);
}

// Force the AvgTime_* formulas to only include Level1/Level2
function repairAverageTimeSheets() {
  const ss = SpreadsheetApp.getActive();

  const setFormula = (name, f) => {
    let sh = ss.getSheetByName(name);
    if (!sh) sh = ss.insertSheet(name);
    sh.clear();
    sh.getRange('A1').setValue(f);
  };

  setFormula('AvgTime_Success', `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE group by C label C 'level_id', avg(E) 'avg_time_success_s'", 0)`);
  setFormula('AvgTime_Failure', `=QUERY(Data!A2:E, "select C, avg(E) where D = FALSE group by C label C 'level_id', avg(E) 'avg_time_failure_s'", 0)`);
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

function ensureHeartLossChartsOnSheet_(ss, opts) {
  const sh = ensureHeartLossSheet_(ss);
  const l1Where = "(C='Level1Scene' or C='Level1')";
  const l2Where = "(C='Level2Scene' or C='Level2')";
  const l3Where = "(C='Level3Scene' or C='Level3')";
  const l4Where = "(C='Level4Scene' or C='Level4')";
  const l5Where = "(C='Level5Scene' or C='Level5')";
  const f1 = `=QUERY(HeartLoss!A2:F, "select E, count(A) where ${l1Where} group by E label E 'cause', count(A) 'loss_count'", 0)`;
  const f2 = `=QUERY(HeartLoss!A2:F, "select E, count(A) where ${l2Where} group by E label E 'cause', count(A) 'loss_count'", 0)`;
  const f3 = `=QUERY(HeartLoss!A2:F, "select E, count(A) where ${l3Where} group by E label E 'cause', count(A) 'loss_count'", 0)`;
  const f4 = `=QUERY(HeartLoss!A2:F, "select E, count(A) where ${l4Where} group by E label E 'cause', count(A) 'loss_count'", 0)`;
  const f5 = `=QUERY(HeartLoss!A2:F, "select E, count(A) where ${l5Where} group by E label E 'cause', count(A) 'loss_count'", 0)`;
  sh.getRange('H1').setValue(f1);
  sh.getRange('K1').setValue(f2);
  sh.getRange('N1').setValue(f3);
  sh.getRange('Q1').setValue(f4);
  sh.getRange('T1').setValue(f5);

  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
  if (sh.getCharts().length < 1) {
    const c1 = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('H:I'))
      .setPosition(1, 8, 0, 0)
      .setOption('title', 'Heart Losses by Cause - Level 1')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Cause' })
      .setOption('vAxis', { title: 'Loss Count' })
      .build();
    sh.insertChart(c1);

    const c2 = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('K:L'))
      .setPosition(16, 8, 0, 0)
      .setOption('title', 'Heart Losses by Cause - Level 2')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Cause' })
      .setOption('vAxis', { title: 'Loss Count' })
      .build();
    sh.insertChart(c2);

    const c3 = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('N:O'))
      .setPosition(31, 8, 0, 0)
      .setOption('title', 'Heart Losses by Cause - Level 3')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Cause' })
      .setOption('vAxis', { title: 'Loss Count' })
      .build();
    sh.insertChart(c3);

    const c4 = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('Q:R'))
      .setPosition(46, 8, 0, 0)
      .setOption('title', 'Heart Losses by Cause - Level 4')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Cause' })
      .setOption('vAxis', { title: 'Loss Count' })
      .build();
    sh.insertChart(c4);

    const c5 = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('T:U'))
      .setPosition(61, 8, 0, 0)
      .setOption('title', 'Heart Losses by Cause - Level 5')
      .setOption('legend', { position: 'none' })
      .setOption('hAxis', { title: 'Cause' })
      .setOption('vAxis', { title: 'Loss Count' })
      .build();
    sh.insertChart(c5);
  }
}

function buildHeartLossCharts() {
  const ss = SpreadsheetApp.getActive();
  ensureHeartLossChartsOnSheet_(ss, { reset: true });
}


