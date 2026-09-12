using System.IO;
using System.Text.Json;

namespace Alchemy.Core.Runs.Saves;

/// <summary>
/// 存档 JSON 服务（System.Text.Json）。文件路径由调用方（Godot 侧 user:// 等）提供。
/// </summary>
public static class SaveService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static string Serialize(RunSaveData data) => JsonSerializer.Serialize(data, Options);

    public static RunSaveData? Deserialize(string json) => JsonSerializer.Deserialize<RunSaveData>(json, Options);

    public static void SaveToFile(RunSaveData data, string path)
    {
        File.WriteAllText(path, Serialize(data));
    }

    public static RunSaveData? LoadFromFile(string path) =>
        File.Exists(path) ? Deserialize(File.ReadAllText(path)) : null;
}
