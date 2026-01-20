using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AKNOVABROW.Services
{
    public class SharedPreferences
    {
        private static SharedPreferences? _instance;
        private readonly string _filePath;
        private Dictionary<string, object> _data;

        private SharedPreferences()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "AKNOVABROW");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "preferences.json");
            _data = new Dictionary<string, object>();
            Load();
        }

        public static async Task<SharedPreferences> GetInstance()
        {
            if (_instance == null)
            {
                _instance = new SharedPreferences();
            }
            return await Task.FromResult(_instance);
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _data = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                            ?? new Dictionary<string, object>();
                }
            }
            catch { }
        }

        private async Task Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch { }
        }

        public double GetDouble(string key, double defaultValue)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is JsonElement element && element.ValueKind == JsonValueKind.Number)
                {
                    return element.GetDouble();
                }
                return Convert.ToDouble(value);
            }
            return defaultValue;
        }

        public bool GetBool(string key, bool defaultValue)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is JsonElement element && element.ValueKind == JsonValueKind.True)
                {
                    return true;
                }
                else if (value is JsonElement element2 && element2.ValueKind == JsonValueKind.False)
                {
                    return false;
                }
                return Convert.ToBoolean(value);
            }
            return defaultValue;
        }

        public async Task SetDouble(string key, double value)
        {
            _data[key] = value;
            await Save();
        }

        public async Task SetBool(string key, bool value)
        {
            _data[key] = value;
            await Save();
        }

        public int GetInt(string key, int defaultValue)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is JsonElement element && element.ValueKind == JsonValueKind.Number)
                {
                    return element.GetInt32();
                }
                return Convert.ToInt32(value);
            }
            return defaultValue;
        }

        public async Task SetInt(string key, int value)
        {
            _data[key] = value;
            await Save();
        }
    }
}