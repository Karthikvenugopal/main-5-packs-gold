Google Sheets queries for robust timing metrics

Use these formulas on your Google Sheet where the raw data lives in the `Data` tab with columns:

- A: `session_id`
- C: `level_id`
- D: `success` (TRUE/FALSE)
- E: `time_spent_s` (seconds)

Success-only average (mean):
`=QUERY(Data!A2:E, "select C, avg(E) where D = TRUE group by C label C 'level_id', avg(E) 'avg_time_success_s'", 0)`

Failure-only average (mean):
`=QUERY(Data!A2:E, "select C, avg(E) where D = FALSE group by C label C 'level_id', avg(E) 'avg_time_failure_s'", 0)`

Retry Density (failures/attempts):

- Per-level retry density using existing Data sheet rows:
`=QUERY(Data!A2:E, "select C, count(E)-sum(D), count(E), (count(E)-sum(D))/count(E) group by C label C 'level_id', count(E)-sum(D) 'failures', count(E) 'attempts', (count(E)-sum(D))/count(E) 'retry_density'", 0)`

- If you only want whitelisted levels (Level1/2/3), use:
`=QUERY(Data!A2:E, "select C, count(E)-sum(D), count(E), (count(E)-sum(D))/count(E) where (C='Level1' or C='Level1Scene' or C='Level2' or C='Level2Scene' or C='Level3' or C='Level3Scene' or C='Level4' or C='Level4Scene') group by C label C 'level_id', count(E)-sum(D) 'failures', count(E) 'attempts', (count(E)-sum(D))/count(E) 'retry_density'", 0)`

Notes

- The game now marks failure automatically when a level is destroyed or the app quits (to capture abandon/quit attempts). This reduces selection bias in averages.
- Times are reported in seconds as integers; when enough attempts accumulate, means/medians are stable despite per-row rounding.
