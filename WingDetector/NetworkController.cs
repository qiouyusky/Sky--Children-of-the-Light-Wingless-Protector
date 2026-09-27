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
        // 恢复时的网卡来源：优先缓存；缓存为空则查所有物理网卡
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

            // 如果已经启用，跳过
            var statusCmd = $"(Get-NetAdapter -Name '{safeName}').Status";
            var (_, currentStatus) = RunPowerShell(statusCmd);
            if (currentStatus.Trim().Equals("Up", StringComparison.OrdinalIgnoreCase))
            {
                OnLog?.Invoke($"[网络] \"{name}\" 已是启用状态，跳过");
                continue;
            }

            // 启用
            var enableCmd = $"Enable-NetAdapter -Name '{safeName}' -Confirm:$false";
            var (success, output) = RunPowerShell(enableCmd);
            if (!success)
            {
                OnLog?.Invoke($"[网络] ✗ 启用 \"{name}\" 失败：{output.Trim()}");
                continue;
            }

            // 轮询等待状态变成 Up（最多 8 秒）
            bool becameUp = false;
            for (int i = 0; i < 16; i++)
            {
                Thread.Sleep(500);
                var (_, s) = RunPowerShell(statusCmd);
                if (s.Trim().Equals("Up", StringComparison.OrdinalIgnoreCase))
                {
                    becameUp = true;
                    break;
                }
            }

            if (becameUp)
                OnLog?.Invoke($"[网络] ✓ 已启用 \"{name}\"，网卡已就绪");
            else
                OnLog?.Invoke($"[网络] ⚠ \"{name}\" 启用命令已执行，但网卡状态未在 8 秒内变为 Up，请稍等片刻再测试");
        }

        _cachedAdapters.Clear();
    }

    private List<string> GetActiveAdapters()
    {
        var cmd = "Get-NetAdapter -Physical | Where-Object { $_.Status -eq 'Up' } | Select-Object -ExpandProperty Name";
        return RunPowerShellLines(cmd);
    }

    private List<string> GetAllAdaptersIncludingDisabled()
    {
        var cmd = "Get-NetAdapter -Physical | Select-Object -ExpandProperty Name";
        return RunPowerShellLines(cmd);
    }

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

        if (p.ExitCode == 0)
            return (true, stdout);

        var errMsg = CleanError(stderr);
        if (string.IsNullOrWhiteSpace(errMsg)) errMsg = stdout;
        return (false, errMsg);
    }

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

        if (p.ExitCode != 0)
        {
            var errMsg = CleanError(stderr);
            if (!string.IsNullOrWhiteSpace(errMsg))
                OnLog?.Invoke($"[网络] PowerShell 错误：{errMsg}");
        }

        var names = new List<string>();
        foreach (var line in stdout.Split('\n'))
        {
            var name = line.Trim().TrimStart('\ufeff');
            if (!string.IsNullOrEmpty(name) && !names.Contains(name))
                names.Add(name);
        }
        return names;
    }

    private static string CleanError(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr)) return "";

        var text = stderr;
        if (text.StartsWith("#< CLIXML"))
        {
            var start = text.IndexOf('\n');
            if (start >= 0) text = text.Substring(start + 1);
        }

        var sb = new StringBuilder();
        bool insideTag = false;
        foreach (var c in text)
        {
            if (c == '<') { insideTag = true; continue; }
            if (c == '>') { insideTag = false; continue; }
            if (!insideTag) sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    private static string EncodeCommand(string command)
    {
        var bytes = Encoding.Unicode.GetBytes(command);
        return Convert.ToBase64String(bytes);
    }
}