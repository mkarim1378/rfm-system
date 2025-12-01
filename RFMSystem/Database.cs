using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RFMSystem
{
    public class Database
    {
        private readonly string _dbPath;

        public Database(string dbPath = "app_history.db")
        {
            _dbPath = dbPath;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }

            using var conn = new SQLiteConnection($"Data Source={_dbPath}");
            conn.Open();

            // Create user_actions table
            var createActionsTable = @"
                CREATE TABLE IF NOT EXISTS user_actions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    action_type TEXT NOT NULL,
                    action_data TEXT,
                    timestamp TEXT NOT NULL,
                    date TEXT NOT NULL,
                    week_number INTEGER,
                    year INTEGER
                )";

            // Create app_settings table
            var createSettingsTable = @"
                CREATE TABLE IF NOT EXISTS app_settings (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                )";

            // Create search_history table
            var createSearchTable = @"
                CREATE TABLE IF NOT EXISTS search_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    search_query TEXT NOT NULL,
                    timestamp TEXT NOT NULL,
                    date TEXT NOT NULL
                )";

            using (var cmd = new SQLiteCommand(createActionsTable, conn))
                cmd.ExecuteNonQuery();
            using (var cmd = new SQLiteCommand(createSettingsTable, conn))
                cmd.ExecuteNonQuery();
            using (var cmd = new SQLiteCommand(createSearchTable, conn))
                cmd.ExecuteNonQuery();
        }

        public void LogAction(string actionType, Dictionary<string, object>? actionData = null)
        {
            using var conn = new SQLiteConnection($"Data Source={_dbPath}");
            conn.Open();

            var now = DateTime.Now;
            var timestamp = now.ToString("yyyy-MM-ddTHH:mm:ss");
            var date = now.ToString("yyyy-MM-dd");
            var weekNumber = GetWeekNumber(now);
            var year = now.Year;
            var actionDataStr = actionData != null ? JsonConvert.SerializeObject(actionData) : null;

            var sql = @"
                INSERT INTO user_actions (action_type, action_data, timestamp, date, week_number, year)
                VALUES (@actionType, @actionData, @timestamp, @date, @weekNumber, @year)";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@actionType", actionType);
            cmd.Parameters.AddWithValue("@actionData", (object?)actionDataStr ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@timestamp", timestamp);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@weekNumber", weekNumber);
            cmd.Parameters.AddWithValue("@year", year);
            cmd.ExecuteNonQuery();
        }

        public List<Dictionary<string, object>> GetRecentActions(int days = 7, int limit = 50)
        {
            using var conn = new SQLiteConnection($"Data Source={_dbPath}");
            conn.Open();

            var sql = @"
                SELECT action_type, action_data, timestamp, date
                FROM user_actions
                WHERE date >= date('now', '-' || @days || ' days')
                ORDER BY timestamp DESC
                LIMIT @limit";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@days", days);
            cmd.Parameters.AddWithValue("@limit", limit);

            var results = new List<Dictionary<string, object>>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var actionData = reader.IsDBNull(1) ? null : JsonConvert.DeserializeObject<Dictionary<string, object>>(reader.GetString(1));
                results.Add(new Dictionary<string, object>
                {
                    ["action_type"] = reader.GetString(0),
                    ["action_data"] = actionData ?? new Dictionary<string, object>(),
                    ["timestamp"] = reader.GetString(2),
                    ["date"] = reader.GetString(3)
                });
            }

            return results;
        }

        public void SaveSearch(string query)
        {
            using var conn = new SQLiteConnection($"Data Source={_dbPath}");
            conn.Open();

            var now = DateTime.Now;
            var timestamp = now.ToString("yyyy-MM-ddTHH:mm:ss");
            var date = now.ToString("yyyy-MM-dd");

            var sql = @"
                INSERT INTO search_history (search_query, timestamp, date)
                VALUES (@query, @timestamp, @date)";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@query", query);
            cmd.Parameters.AddWithValue("@timestamp", timestamp);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.ExecuteNonQuery();
        }

        public string? GetSetting(string key, string? defaultValue = null)
        {
            using var conn = new SQLiteConnection($"Data Source={_dbPath}");
            conn.Open();

            var sql = "SELECT value FROM app_settings WHERE key = @key";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@key", key);

            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? defaultValue;
        }

        public void SetSetting(string key, string value)
        {
            using var conn = new SQLiteConnection($"Data Source={_dbPath}");
            conn.Open();

            var sql = @"
                INSERT OR REPLACE INTO app_settings (key, value)
                VALUES (@key, @value)";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@value", value);
            cmd.ExecuteNonQuery();
        }

        private int GetWeekNumber(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var calendar = culture.Calendar;
            return calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        }
    }
}

