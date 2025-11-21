using Microsoft.Data.Sqlite;
using System.IO;
using System.Windows; 

namespace WifiManagerWPF
{
    internal class SettingsStorage
    {
        // Double check this path is actually correct on your disk!
        private const string DbPath =
            @"C:\Program Files\QUBBER\ColorSorter\settings.db";

        private const string ConnectionString = $"Data Source={DbPath};Mode=ReadOnly";

        public string? LoadValueFromDb(string key)
        {
            // 1. Check if file exists
            if (!File.Exists(DbPath))
            {
                //MessageBox.Show($"ERROR: Database file not found at:\n{DbPath}", "Debug Check");
                return null;
            }

            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                var cmd = connection.CreateCommand();
                // Using strict parameter name $key
                cmd.CommandText = "SELECT Value FROM AppSettings WHERE key = $key;";
                cmd.Parameters.AddWithValue("$key", key);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string result = reader.GetString(0);
                    //// --- CRITICAL DEBUG MESSAGE ---
                    //MessageBox.Show($"SUCCESS: Database found key '{key}'\nValue returned: '{result}'", "Debug Check");
                    return result;
                }
                else
                {
                    // --- KEY NOT FOUND MESSAGE ---
                    //MessageBox.Show($"FAILURE: Connected to DB, but key '{key}' was not found.", "Debug Check");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show($"CRITICAL ERROR: {ex.Message}", "Debug Check");
                return null;
            }
        }
    }
}