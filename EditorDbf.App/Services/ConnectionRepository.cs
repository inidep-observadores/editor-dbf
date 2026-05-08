using System;
using System.Text.Json;
using System.IO;
using EditorDbf.App.Models;

namespace EditorDbf.App.Services;

public sealed class ConnectionRepository
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _stateFilePath;

    public ConnectionRepository()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EditorDbf");

        _stateFilePath = Path.Combine(root, "connections.json");
    }

    public AppState Load()
    {
        if (!File.Exists(_stateFilePath))
        {
            return new AppState();
        }

        var rawJson = File.ReadAllText(_stateFilePath);
        return JsonSerializer.Deserialize<AppState>(rawJson, _jsonOptions) ?? new AppState();
    }

    public void Save(AppState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
        var rawJson = JsonSerializer.Serialize(state, _jsonOptions);
        File.WriteAllText(_stateFilePath, rawJson);
    }
}
