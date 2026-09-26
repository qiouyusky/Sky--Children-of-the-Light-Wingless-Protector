using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WingDetector;

public class NetworkController
{
    private List<string> _cachedAdapters = new();

    public void DisableAllAdapters()
    {
        _cachedAdapters = GetConnectedAdapters();
        foreach (var name in _cachedAdapters)
        {
            RunNetsh($"interface set interface \"{name}\" admin=disabled");
        }
    }

    public void EnableAllAdapters()
    {
        var adapters = _cachedAdapters.Count > 0 ? _cachedAdapters : GetConnectedAdapters();
        foreach (var name in adapters)
        {
            RunNetsh($"interface set interface \"{name}\" admin=enabled");
        }
    }

    private List<string> GetConnectedAdapters()
    {
        var psi = new ProcessStartInfo("netsh", "interface show interface")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.GetEncoding("GBK")
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        var adapters = new List<string>();
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Contains("已连接") || trimmed.Contains("Connected"))
            {
                var parts = Regex.Split(trimmed, @"\s{2,}");
                if (parts.Length >= 4)
                    adapters.Add(parts[^1].Trim());
            }
        }
        return adapters;
    }

    private void RunNetsh(string args)
    {
        var psi = new ProcessStartInfo("netsh", args)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi);
        p?.WaitForExit();
    }
}