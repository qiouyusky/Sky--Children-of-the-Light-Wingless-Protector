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
            var (success, output) = RunPowerShell(
                $"Disable-NetAdapter -Name '{name}' -Confirm:$false");
            if (success)
                OnLog?.Invoke($"[网络] ✓ 已禁用 \"{name}\"");
            else
                OnLog?.Invoke($"[网络] ✗ 禁用 \"{name}\" 失败：{output.Trim()}");
        }
    }

    public void EnableAllAdapters()
    {
        // 恢复时不能只查 Up 的网卡（它们现在是 Down）
        // 优先用缓存，缓存空则列出所有物理网卡
        var adapters = _cachedAdapters.Count > 0 ? _cachedAdapters : GetAllPhysicalAdapters();

        if (adapters.Count == 0)
        {
            OnLog?.Invoke("[网络] 未找到可恢复的网卡");
            return;
        }

        OnLog?.Invoke($"[网络] 尝试恢复：{string.Join(" | ", adapters)}");

        foreach (var name in adapters)
        {
            var (success, output) = RunPowerShell(
                $"Enable-NetAdapter -Name '{name}' -Confirm:$false");
            if (success)
                OnLog?.Invoke($"[网络] ✓ 已启用 \"{name}\"");
            else
                OnLog?.Invoke($"[网络] ✗ 启用 \"{name}\" 失败：{output.Trim()}");
        }

        // 恢复后清空缓存，下次断网重新获取
        _cachedAdapters.Clear();
    }

    private List<string> GetActiveAdapters()
    {
        var cmd = "Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' } | Select-Object -ExpandProperty Name";
        return RunPowerShellLines(cmd);
    }

    private List<string> GetAllPhysicalAdapters()
    {
        var cmd = "Get-NetAdapter -Physical | Select-Object -ExpandProperty Name";
        return RunPowerShellLines(cmd);
    }

    /// <summary>
    /// 执行 PowerShell 命令，返回 (成功, 输出)
    /// </summary>
    private (bool Success, string Output) RunPowerShell(string command)
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
        if (p == null) return (false, "无法启动 powershell.exe");

        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        var msg = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
        return (p.ExitCode == 0, msg);
    }

    /// <summary>
    /// 执行 PowerShell 命令并逐行返回结果（用于获取网卡名列表）
    /// </summary>
    private List<string> RunPowerShellLines(string command)
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

        var names = new List<string>();
        foreach (var line in stdout.Split('\n'))
        {
            var name = line.Trim().TrimStart('\ufeff');
            if (!string.IsNullOrEmpty(name) && !names.Contains(name))
                names.Add(name);
        }
        return names;
    }
}