using Dashboard.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dashboard.Services;

/// <summary>
/// Loads and saves the hosts and their services from the services JSON file.
/// </summary>
/// <param name="configuration">The app configuration, holding the services file path.</param>
/// <param name="environment">The hosting environment, used to resolve a relative file path.</param>
public class HostStore(IConfiguration configuration, IWebHostEnvironment environment)
{
    /// <summary>
    /// Options used to read and write the services file.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    /// <summary>
    /// Full path of the services file, resolved against the content root when relative.
    /// </summary>
    private readonly string FilePath = Path.Combine(environment.ContentRootPath, configuration["Data:ServicesFile"] ?? string.Empty);

    /// <summary>
    /// Guards the cached hosts and the file.
    /// </summary>
    private readonly Lock Gate = new();

    /// <summary>
    /// The cached hosts, loaded from the file on first use.
    /// </summary>
    private List<HostEntry>? Hosts;

    /// <summary>
    /// Returns a copy of every host and its services.
    /// </summary>
    public List<HostEntry> GetAll()
    {
        lock (Gate)
        {
            Hosts ??= File.Exists(FilePath) ? JsonSerializer.Deserialize<List<HostEntry>>(File.ReadAllText(FilePath), JsonOptions) ?? [] : [];
            return Clone(Hosts);
        }
    }

    /// <summary>
    /// Replaces every host and its services, writing them to the file.
    /// </summary>
    /// <param name="hosts">The full list of hosts to save.</param>
    public void Save(List<HostEntry> hosts)
    {
        lock (Gate)
        {
            // Write to a temporary file first so a failed write never leaves a half-written services file.
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            string tempPath = $"{FilePath}.tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(hosts, JsonOptions).ReplaceLineEndings("\r\n") + "\r\n", new UTF8Encoding(true));
            File.Move(tempPath, FilePath, true);
            Hosts = Clone(hosts);
        }
    }

    /// <summary>
    /// Returns a deep copy of the given hosts, so callers can't change the cached list.
    /// </summary>
    /// <param name="hosts">The hosts to copy.</param>
    private static List<HostEntry> Clone(List<HostEntry> hosts)
    {
        return JsonSerializer.Deserialize<List<HostEntry>>(JsonSerializer.Serialize(hosts, JsonOptions), JsonOptions) ?? [];
    }
}