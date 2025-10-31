function doPost(e) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sh = ss.getSheetByName("Data") || ss.getActiveSheet();
  let data = {};

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




/** Setup that creates a summary sheet ("AvgTime") and inserts a live bar graph chart of Average Time per Level.*/
function setupVisualization() {
  const ss = SpreadsheetApp.getActive();
  let data = ss.getSheetByName("Data") || ss.getActiveSheet();
  if (data.getName() !== "Data") data.setName("Data");

  let avg = ss.getSheetByName("AvgTime");
  if (!avg) avg = ss.insertSheet("AvgTime"); else avg.clear();

  // success is TRUE/FALSE;
  const formula =
    `=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE group by C label C 'level_id', avg(E) 'avg_time_s'", 0)`;
  avg.getRange("A1").setValue(formula);

  avg.getCharts().forEach(c => avg.removeChart(c));
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