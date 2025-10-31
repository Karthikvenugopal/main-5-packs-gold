function doPost(e) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sh = ss.getSheetByName("Data") || ss.getActiveSheet();
  let data = {};

  // Ensure summary/chart sheets exist once
  try {
    ensureAllAnalyticsSheets_(ss);
  } catch (err) {
    // non-fatal
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




/** Setup that creates summary sheets and charts. */
function setupVisualization() {
  const ss = SpreadsheetApp.getActive();
  let data = ss.getSheetByName("Data") || ss.getActiveSheet();
  if (data.getName() !== "Data") data.setName("Data");

  ensureAllAnalyticsSheets_(ss, { reset: true });
}

// Ensure all analytics sheets; creates missing ones; if reset:true clears+rebuilds
function ensureAllAnalyticsSheets_(ss, opts) {
  ensureAvgTimeSuccessSheet_(ss, opts);
  ensureAvgTimeFailureSheet_(ss, opts);
  ensureAvgTimeAllSheet_(ss, opts);
  ensureSuccessRateSheet_(ss, opts);
  ensureMedianSuccessSheet_(ss, opts);
}

// Idempotent creator for AvgTime (Success) sheet + chart
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

// Avg time for failures only
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

// Avg time including all attempts
function ensureAvgTimeAllSheet_(ss, opts) {
  let sh = ss.getSheetByName("AvgTime_All");
  if (!sh) sh = ss.insertSheet("AvgTime_All");
  else if (opts && opts.reset) sh.clear();

  const formula = `=QUERY(Data!A2:E, "select C, avg(E) group by C label C 'level_id', avg(E) 'avg_time_all_s'", 0)`;
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

// Attempts and success rate per level
function ensureSuccessRateSheet_(ss, opts) {
  let sh = ss.getSheetByName("Success_Rate");
  if (!sh) sh = ss.insertSheet("Success_Rate");
  else if (opts && opts.reset) sh.clear();

  // avg(D) treats TRUE as 1, FALSE as 0 -> success rate
  const formula = `=QUERY(Data!A2:E, "select C, count(C), avg(D) group by C label C 'level_id', count(C) 'attempts', avg(D) 'success_rate'", 0)`;
  sh.getRange("A1").setValue(formula);

  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
  if (sh.getCharts().length === 0) {
    const chart = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange("A:C"))
      .setPosition(1, 4, 0, 0)
      .setOption("title", "Attempts and Success Rate by Level")
      .setOption("series", {1: {targetAxisIndex: 1}})
      .setOption("vAxes", {0: {title: "Attempts"}, 1: {title: "Success Rate"}})
      .build();
    sh.insertChart(chart);
  }
}

// Median time for successes to reduce outlier sensitivity
function ensureMedianSuccessSheet_(ss, opts) {
  let sh = ss.getSheetByName("Median_Success");
  if (!sh) sh = ss.insertSheet("Median_Success");
  else if (opts && opts.reset) sh.clear();

  // 2 columns: level_id, median_success_time_s
  const formula = "=LET(levels, UNIQUE(FILTER(Data!C2:C, Data!C2:C<>\"\")), HSTACK(levels, MAP(levels, LAMBDA(l, MEDIAN(FILTER(Data!E2:E, Data!C2:C=l, Data!D2:D=TRUE))))))";
  sh.getRange("A1").setValue(formula);

  if (opts && opts.reset) sh.getCharts().forEach(c => sh.removeChart(c));
  if (sh.getCharts().length === 0) {
    const chart = sh.newChart()
      .asColumnChart()
      .addRange(sh.getRange("A:B"))
      .setPosition(1, 3, 0, 0)
      .setOption("title", "Median Time per Level (Success)")
      .setOption("legend", { position: "none" })
      .setOption("hAxis", { title: "Level" })
      .setOption("vAxis", { title: "Median Time (s)" })
      .build();
    sh.insertChart(chart);
  }
}
  let avg = ss.getSheetByName("AvgTime");
  if (avg) return; // already present

  avg = ss.insertSheet("AvgTime");
  const formula = `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE group by C label C 'level_id', avg(E) 'avg_time_s'", 0)`;
  avg.getRange("A1").setValue(formula);

  const chart = avg.newChart()
    .asColumnChart()
    .addRange(avg.getRange("A:B"))
    .setPosition(1, 3, 0, 0)
    .setOption("title", "Average Time per Level (success only)")
    .setOption("legend", { position: "none" })
    .setOption("hAxis", { title: "Level" })
    .setOption("vAxis", { title: "Average Time (s)" })
    .build();
  avg.insertChart(chart);
}


function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu("Chef Analytics")
    .addItem("Rebuild AvgTime Chart", "setupVisualization")
    .addToUi();
} 




function doGet() { return ContentService.createTextOutput("OK"); }
