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

All attempts average (mean):
`=QUERY(Data!A2:E, "select C, avg(E) group by C label C 'level_id', avg(E) 'avg_time_all_s'", 0)`

Robust stats to reduce outlier impact:

- Median (success-only):
`=QUERY(Data!A2:E, "select C, median(E) where D = TRUE group by C label C 'level_id', median(E) 'median_time_success_s'", 0)`

- P90 (success-only):
`=QUERY(Data!A2:E, "select C, percentile(E,0.9) where D = TRUE group by C label C 'level_id', percentile(E,0.9) 'p90_success_s'", 0)`

Context metrics:

- Attempts, successes, success rate per level:
`=QUERY(Data!A2:E, "select C, count(E), sum(D), sum(D)/count(E) where C is not null group by C label C 'level_id', count(E) 'attempts', sum(D) 'successes', sum(D)/count(E) 'success_rate'", 0)`

- Success-time averaged per session first (reduces heavy-user skew), then across sessions:
`=QUERY(QUERY(Data!A2:E, "select C, A, avg(E) where D = TRUE group by C, A label avg(E) ''", 0), "select Col1, avg(Col3) group by Col1 label Col1 'level_id', avg(Col3) 'avg_time_success_per_session_s'", 0)`

Notes

- The game now marks failure automatically when a level is destroyed or the app quits (to capture abandon/quit attempts). This reduces selection bias in averages.
- Times are reported in seconds as integers; when enough attempts accumulate, means/medians are stable despite per-row rounding.

