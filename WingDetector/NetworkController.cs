using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WingDetector;

public class NetworkController
{
    private List<string> _cachedAdapters = new();

    public void DisableAllAdapters()
    {
        // 禁用前先缓存网卡名称，防止禁用后读不到
        _cachedAdapters = GetConnectedAdapters();
        foreach (var name in _cachedAdapters)
        {
            RunNetsh($"interface set interface \"{name}\" admin=disabled");
        }
    }

    public void EnableAllAdapters()
    {
        // 优先使用缓存，如果缓存为空再重新获取
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
            CreateNoWindow = true
            // 注意：这里删除了原来的 StandardOutputEncoding = ... 那一行
            // .NET 10 不再默认支持 GBK 编码，去掉它即可顺利运行
        };
        
        using var p = Process.Start(psi);
        if (p == null) return new List<string>();
        
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        var adapters = new List<string>();
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            
            // 匹配“已连接”或“Connected”作为状态判断（兼容中英文系统）
            if (trimmed.Contains("已连接") || trimmed.Contains("Connected"))
            {
                // 按 2 个以上空格拆分为多列
                var parts = Regex.Split(trimmed, @"\s{2,}");
                
                // netsh 输出中，最后一列是网卡名称
                if (parts.Length >= 4)
                {
                    adapters.Add(parts[^1].Trim());
                }
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