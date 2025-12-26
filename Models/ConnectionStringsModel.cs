using System.ComponentModel;

namespace AppSettingsEditor.Models;

public class ConnectionStringsModel
{
    [Description("Connection string to the main database (online mode)")]
    public string? DefaultConnection { get; set; }

    [Description("Connection string to the local/offline database")]
    public string? DefaultOfflineConnection { get; set; }
}

