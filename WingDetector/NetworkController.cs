using System.Diagnostics;

namespace WingDetector;

public class NetworkController
{
    private List<string> _cachedAdapters = new();

    public event Action<string>? OnLog;

    public void DisableAllAdapters()
    {
        _cachedAdapters = GetActiveAdapters();
        OnLog?.Invoke($"[网络] 发现 {_cachedAdapters.Count} 个活动网卡：{string.Join(" | ", _cachedAdapters)}");

        if (_cachedAdapters.Count == 0)
        {
            OnLog?.Invoke("[网络] 警告：未找到活动网卡，无法断网");
            return;
        }

        foreach (var name in _cachedAdapters)
        {
            var (success, output) = RunNetsh($"interface set interface name=\"{name}\" admin=disable");
            if (success)
                OnLog?.Invoke($"[网络] ✓ 已禁用 \"{name}\"");
            else
                OnLog?.Invoke($"[网络] ✗ 禁用 \"{name}\" 失败：{output.Trim()}");
        }
    }

    public void EnableAllAdapters()
    {
        var adapters = _cachedAdapters.Count > 0 ? _cachedAdapters : GetActiveAdapters();

        if (adapters.Count == 0)
        {
            adapters = GetAllPhysicalAdapters();
            OnLog?.Invoke($"[网络] 缓存为空，使用物理网卡列表：{string.Join(" | ", adapters)}");
        }

        if (adapters.Count == 0)
        {
            OnLog?.Invoke("[网络] 未找到可恢复的网卡");
            return;
        }

        foreach (var name in adapters)
        {
            var (success, output) = RunNetsh($"interface set interface name=\"{name}\" admin=enable");
            if (success)
                OnLog?.Invoke($"[网络] ✓ 已启用 \"{name}\"");
            else
                OnLog?.Invoke($"[网络] ✗ 启用 \"{name}\" 失败：{output.Trim()}");
        }
    }

    private List<string> GetActiveAdapters()
    {
        var cmd = "Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' } | Select-Object -ExpandProperty Name";
        return RunPowerShell(cmd);
    }

    private List<string> GetAllPhysicalAdapters()
    {
        var cmd = "Get-NetAdapter -Physical | Select-Object -ExpandProperty Name";
        return RunPowerShell(cmd);
    }

    private List<string> RunPowerShell(string command)
    {
        var psi = new ProcessStartInfo("powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi);
        if (p == null) return new List<string>();

        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stderr))
            OnLog?.Invoke($"[网络] PowerShell 警告：{stderr.Trim()}");

        var adapters = new List<string>();
        foreach (var line in stdout.Split('\n'))
        {
            var name = line.Trim().TrimStart('\ufeff');
            if (!string.IsNullOrEmpty(name) && !adapters.Contains(name))
                adapters.Add(name);
        }
        return adapters;
    }

    private (bool Success, string Output) RunNetsh(string args)
    {
        var psi = new ProcessStartInfo("netsh", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi);
        if (p == null) return (false, "无法启动 netsh");

        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        var msg = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
        return (p.ExitCode == 0, msg);
    }
}