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
  if (opts && opts.prune) {
    pruneAnalyticsSheets_(ss);
  }
}

function ensureAvgTimeSuccessSheet_(ss, opts) {
  let sh = ss.getSheetByName('AvgTime_Success');
  if (!sh) sh = ss.insertSheet('AvgTime_Success');
  else if (opts && opts.reset) sh.clear();

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE and (C='Level1Scene' or C='Level1' or C='Level2Scene' or C='Level2' or C='Level3Scene' or C='Level3') group by C label C 'level_id', avg(E) 'avg_time_success_s'", 0)`;
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

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = FALSE and (C='Level1Scene' or C='Level1' or C='Level2Scene' or C='Level2' or C='Level3Scene' or C='Level3') group by C label C 'level_id', avg(E) 'avg_time_failure_s'", 0)`;
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

  const allowed = new Set(['level1','level1scene','level2','level2scene','level3','level3scene']);
  const stats = new Map(); // level -> { total, fail }
  rows.forEach(r => {
    const level = String(r[2] || '').trim();
    const lvlLower = level.toLowerCase();
    if (!allowed.has(lvlLower)) return;
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
  if (sh && opts && opts.reset && opts.fullReset) {
    ss.deleteSheet(sh);
    sh = null;
  }
  if (!sh) {
    sh = ss.insertSheet('TokenCompletion');
  }
  sh.getRange('A1:G1').setValues([[
    'timestamp', 'session_id', 'level_id', 'token_completion_rate', 'tokens_collected', 'tokens_available', 'time_spent_s'
  ]]);
  sh.getRange('G:G').setNumberFormat('0');

  sh.getRange('L:Q').clearContent();
  const scatterHeaders = [[
    'Level 1 Token Completion Rate', 'Level 1 Time Spent (s)',
    'Level 2 Token Completion Rate', 'Level 2 Time Spent (s)',
    'Level 3 Token Completion Rate', 'Level 3 Time Spent (s)'
  ]];
  sh.getRange('L1:Q1').setValues(scatterHeaders);

  const scatterFormula = level => `=IFERROR(FILTER({TokenCompletion!D2:D, TokenCompletion!G2:G}, (TokenCompletion!D2:D<>"")*(TokenCompletion!G2:G<>"")*(REGEXMATCH(TokenCompletion!C2:C,"(?i)^${level}"))), "")`;
  sh.getRange('L2').setFormula(scatterFormula('level1'));
  sh.getRange('N2').setFormula(scatterFormula('level2'));
  sh.getRange('P2').setFormula(scatterFormula('level3'));

  sh.getRange('L:L').setNumberFormat('0%');
  sh.getRange('N:N').setNumberFormat('0%');
  sh.getRange('P:P').setNumberFormat('0%');
  sh.getRange('M:M').setNumberFormat('0');
  sh.getRange('O:O').setNumberFormat('0');
  sh.getRange('Q:Q').setNumberFormat('0');

  const summaryFormula = `=IF(COUNTA(TokenCompletion!D2:D)=0,"",QUERY(TokenCompletion!A2:G,"select C, avg(D), count(D) where D is not null group by C label C 'level_id', avg(D) 'avg_token_completion_rate', count(D) 'attempt_count'",0))`;
  sh.getRange('I1').setValue(summaryFormula);
  sh.getRange('D:D').setNumberFormat('0%');  // raw token completion values
  sh.getRange('J:J').setNumberFormat('0%');  // summary averages

  if (opts && opts.reset) {
    safeGetCharts_(sh).forEach(c => sh.removeChart(c));
  }
  const charts = safeGetCharts_(sh);
  const sheetName = sh.getName();
  const hasSummaryChart = charts.some(chart => {
    if (typeof chart.getChartType !== 'function' || chart.getChartType() !== Charts.ChartType.COLUMN) return false;
    const ranges = chart.getRanges ? chart.getRanges() : [];
    return ranges.some(range => range.getSheet().getName() === sheetName && range.getColumn() === 9);
  });
  if (!hasSummaryChart) {
    const chart = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange('I:J'))
      .setPosition(1, 5, 0, 0)
      .setOption('title', 'Average Token Completion Rate by Level')
      .setOption('legend', { position: 'none' })
      .setOption('vAxis', { title: 'Completion Rate', viewWindow: { min: 0, max: 1 } })
      .setOption('hAxis', { title: 'Level' })
      .setOption('series', { 0: { dataLabel: 'value' } })
      .build();
    sh.insertChart(chart);
  }
  const hasScatterChart = charts.some(chart => {
    if (typeof chart.getChartType !== 'function' || chart.getChartType() !== Charts.ChartType.SCATTER) return false;
    const ranges = chart.getRanges ? chart.getRanges() : [];
    return ranges.some(range => range.getSheet().getName() === sheetName && range.getColumn() === 12);
  });
  if (!hasScatterChart) {
    const scatter = sh.newChart()
      .asScatterChart()
      .addRange(sh.getRange('L:M'))
      .addRange(sh.getRange('N:O'))
      .addRange(sh.getRange('P:Q'))
      .setNumHeaders(1)
      .setPosition(18, 5, 0, 0)
      .setOption('title', 'Token Completion vs Time Bonus')
      .setOption('legend', { position: 'right' })
      .setOption('hAxis', { title: 'Token Completion Rate', viewWindow: { min: -0.05, max: 1.05 }, format: 'percent' })
      .setOption('vAxis', { title: 'Time Spent (s)' })
      .setOption('series', {
        0: { labelInLegend: 'Level 1' },
        1: { labelInLegend: 'Level 2' },
        2: { labelInLegend: 'Level 3' }
      })
      .setOption('pointSize', 5)
      .build();
    sh.insertChart(scatter);
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
  ensureTokenCompletionSheet_(ss, { reset: true });
}

function pruneAnalyticsSheets_(ss, opts) {
  const essentials = new Set(['Data']);
  const withAverages = new Set(['Data', 'AvgTime_Success', 'AvgTime_Failure', 'RetryDensity', 'HeartLoss', 'TokenCompletion']);
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
  const lvlLower = level.toLowerCase();
  const allowed = new Set(['level1', 'level1scene', 'level2', 'level2scene', 'level3', 'level3scene']);
  if (!allowed.has(lvlLower)) {
    return ContentService.createTextOutput(JSON.stringify({ ok: true, ignored: true }))
      .setMimeType(ContentService.MimeType.JSON);
  }

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
    try { ensureRetryDensitySheet_(ss, { reset: false }); } catch (e) { Logger.log(e); }
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
  const allowed = new Set(['level1', 'level1scene', 'level2', 'level2scene', 'level3', 'level3scene']);

  const purgeSheet = (name, levelCol) => {
    const sh = ss.getSheetByName(name);
    if (!sh) return;
    const last = sh.getLastRow();
    if (last < 2) return;
    const rng = sh.getRange(2, levelCol, last - 1, 1).getValues();
    const toDelete = [];
    for (let i = 0; i < rng.length; i++) {
      const v = String(rng[i][0] || '').toLowerCase();
      if (!allowed.has(v)) toDelete.push(i + 2);
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
  const where = "(C='Level1Scene' or C='Level1' or C='Level2Scene' or C='Level2' or C='Level3Scene' or C='Level3')";

  const setFormula = (name, f) => {
    let sh = ss.getSheetByName(name);
    if (!sh) sh = ss.insertSheet(name);
    sh.clear();
    sh.getRange('A1').setValue(f);
  };

  setFormula('AvgTime_Success', `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE and ${where} group by C label C 'level_id', avg(E) 'avg_time_success_s'", 0)`);
  setFormula('AvgTime_Failure', `=QUERY(Data!A2:E, "select C, avg(E) where D = FALSE and ${where} group by C label C 'level_id', avg(E) 'avg_time_failure_s'", 0)`);
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
  const f1 = `=QUERY(HeartLoss!A2:F, "select E, count(A) where ${l1Where} group by E label E 'cause', count(A) 'loss_count'", 0)`;
  const f2 = `=QUERY(HeartLoss!A2:F, "select E, count(A) where ${l2Where} group by E label E 'cause', count(A) 'loss_count'", 0)`;
  const f3 = `=QUERY(HeartLoss!A2:F, "select E, count(A) where ${l3Where} group by E label E 'cause', count(A) 'loss_count'", 0)`;
  sh.getRange('H1').setValue(f1);
  sh.getRange('K1').setValue(f2);
  sh.getRange('N1').setValue(f3);

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
  }
}

function buildHeartLossCharts() {
  const ss = SpreadsheetApp.getActive();
  ensureHeartLossChartsOnSheet_(ss, { reset: true });
}


