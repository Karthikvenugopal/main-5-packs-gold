function doPost(e) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sh = ss.getSheetByName("Data") || ss.getActiveSheet();
  let data = {};
  try {
    ensureAllAnalyticsSheets_(ss);
  } catch (err) {
    Logger.log("ensureAllAnalyticsSheets_ error: " + err);
  }

  try {
    if (e.postData && e.postData.type === 'application/json') {
      data = JSON.parse(e.postData.contents || '{}');
    } else {
      data = e.parameter || {};
    }
  } catch (err) {
    Logger.log("JSON parse error: " + err);
    return ContentService.createTextOutput(JSON.stringify({ ok: false, error: 'Bad JSON' }))
      .setMimeType(ContentService.MimeType.JSON);
  }

  try {
    const successText = String(data.success || "").toUpperCase();
    const timeSpent = Number(data.time_spent_s) || 0;

    sh.appendRow([
      new Date(),
      data.session_id || "",
      data.level_id || "",
      successText || "FALSE",
      timeSpent
    ]);

    return ContentService.createTextOutput(JSON.stringify({ ok: true }))
      .setMimeType(ContentService.MimeType.JSON)
      .setHeader('Access-Control-Allow-Origin', '*')
      .setHeader('Access-Control-Allow-Headers', 'Content-Type');
  } catch (err) {
    Logger.log("Append error: " + err);
    return ContentService.createTextOutput(JSON.stringify({ ok: false, error: err.message }))
      .setMimeType(ContentService.MimeType.JSON)
      .setHeader('Access-Control-Allow-Origin', '*')
      .setHeader('Access-Control-Allow-Headers', 'Content-Type');
  }
}





function setupVisualization() {
  const ss = SpreadsheetApp.getActive();
  let data = ss.getSheetByName("Data") || ss.getActiveSheet();
  if (data.getName() !== "Data") data.setName("Data");

  ensureAllAnalyticsSheets_(ss, { reset: true, prune: true });
}

function ensureAllAnalyticsSheets_(ss, opts) {
  ensureAvgTimeSuccessSheet_(ss, opts);
  ensureAvgTimeFailureSheet_(ss, opts);
  ensureAvgTimeSheet_(ss, opts);
  if (opts && opts.prune) {
    pruneAnalyticsSheets_(ss);
  }
}

function ensureAvgTimeSuccessSheet_(ss, opts) {
  let sh = ss.getSheetByName("AvgTime_Success");
  if (!sh) sh = ss.insertSheet("AvgTime_Success");
  else if (opts && opts.reset) sh.clear();

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE group by C label C 'level_id', avg(E) 'avg_time_success_s'", 0)`;
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

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = FALSE group by C label C 'level_id', avg(E) 'avg_time_failure_s'", 0)`;
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


function ensureAvgTimeSheet_(ss, opts) {
  let sh = ss.getSheetByName("AvgTime");
  if (!sh) sh = ss.insertSheet("AvgTime");
  else if (opts && opts.reset) sh.clear();

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE group by C label C 'level_id', avg(E) 'avg_time_s'", 0)`;
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

function pruneAnalyticsSheets_(ss) {
  const keep = new Set(["Data", "AvgTime_Success", "AvgTime_Failure", "AvgTime"]);
  ss.getSheets().forEach(sheet => {
    const name = sheet.getName();
    if (!keep.has(name)) {
      if (ss.getSheets().length > 1) {
        ss.deleteSheet(sheet);
      }
    }
  });
}


function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu("Chef Analytics")
    .addItem("Rebuild (keep 4 sheets)", "setupVisualization")
    .addItem("Prune other sheets", "pruneSheetsCommand_")
    .addToUi();
}

function pruneSheetsCommand_() {
  const ss = SpreadsheetApp.getActive();
  pruneAnalyticsSheets_(ss);
}




function doGet() { return ContentService.createTextOutput("OK"); }
