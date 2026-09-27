using System.Diagnostics;
using System.Text;

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
            // 用双引号包裹名称，同时对名称内的单引号转义（PowerShell 中用两个单引号转义）
            var safeName = name.Replace("'", "''");
            var cmd = $"Disable-NetAdapter -Name '{safeName}' -Confirm:$false";
            var (success, output) = RunPowerShell(cmd);
            if (success)
                OnLog?.Invoke($"[网络] ✓ 已禁用 \"{name}\"");
            else
                OnLog?.Invoke($"[网络] ✗ 禁用 \"{name}\" 失败：{output.Trim()}");
        }
    }

    public void EnableAllAdapters()
    {
        var adapters = _cachedAdapters.Count > 0 ? _cachedAdapters : GetAllAdaptersIncludingDisabled();

        if (adapters.Count == 0)
        {
            OnLog?.Invoke("[网络] 未找到可恢复的网卡");
            return;
        }

        OnLog?.Invoke($"[网络] 尝试恢复：{string.Join(" | ", adapters)}");

        foreach (var name in adapters)
        {
            var safeName = name.Replace("'", "''");
            var cmd = $"Enable-NetAdapter -Name '{safeName}' -Confirm:$false";
            var (success, output) = RunPowerShell(cmd);
            if (success)
                OnLog?.Invoke($"[网络] ✓ 已启用 \"{name}\"");
            else
                OnLog?.Invoke($"[网络] ✗ 启用 \"{name}\" 失败：{output.Trim()}");
        }

        _cachedAdapters.Clear();
    }

    private List<string> GetActiveAdapters()
    {
        var cmd = "Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' } | Select-Object -ExpandProperty Name";
        return RunPowerShellLines(cmd);
    }

    /// <summary>
    /// 列出所有物理网卡（包括已禁用的），用于恢复网络
    /// </summary>
    private List<string> GetAllAdaptersIncludingDisabled()
    {
        var cmd = "Get-NetAdapter -Physical | Select-Object -ExpandProperty Name";
        return RunPowerShellLines(cmd);
    }

    /// <summary>
    /// 执行 PowerShell 命令，返回 (退出码0为true, 输出文本)
    /// 关键：用 -EncodedCommand 传参，避免中文在命令行解析时乱码
    /// </summary>
    private (bool Success, string Output) RunPowerShell(string command)
    {
        var encoded = EncodeCommand(command);
        var psi = new ProcessStartInfo("powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}")
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
    /// 执行 PowerShell 命令并逐行返回结果
    /// </summary>
    private List<string> RunPowerShellLines(string command)
    {
        var encoded = EncodeCommand(command);
        var psi = new ProcessStartInfo("powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}")
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

    /// <summary>
    /// 把命令编码为 PowerShell 的 -EncodedCommand 格式：UTF-16LE + Base64
    /// </summary>
    private static string EncodeCommand(string command)
    {
        var bytes = Encoding.Unicode.GetBytes(command);
        return Convert.ToBase64String(bytes);
    }
}