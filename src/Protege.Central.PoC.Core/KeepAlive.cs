using System.Runtime.InteropServices;

namespace Protege.Central.PoC.Core;

/// <summary>
/// Mantem o Windows ativo (sem bloqueio de tela / suspensao) movendo o cursor
/// periodicamente e sinalizando ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED ao SO.
/// Motor puro, sem dependencia de UI - a camada de apresentacao decide quando ligar/desligar.
/// </summary>
public sealed class KeepAlive : IDisposable
{
    [Flags]
    private enum ExecutionState : uint
    {
        Continuous = 0x80000000,
        SystemRequired = 0x00000001,
        DisplayRequired = 0x00000002,
    }

    [DllImport("kernel32.dll")]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    private const int StepPixels = 5;
    private System.Threading.Timer? _timer;
    private bool _movingUp = true;

    public bool IsActive { get; private set; }

    public event Action<bool>? ActiveChanged;

    public void Start()
    {
        if (IsActive) return;
        IsActive = true;
        SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.SystemRequired | ExecutionState.DisplayRequired);
        _timer = new System.Threading.Timer(_ => Tick(), null, 2000, 2000);
        ActiveChanged?.Invoke(true);
    }

    public void Stop()
    {
        if (!IsActive) return;
        IsActive = false;
        _timer?.Dispose();
        _timer = null;
        SetThreadExecutionState(ExecutionState.Continuous);
        ActiveChanged?.Invoke(false);
    }

    private void Tick()
    {
        SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.SystemRequired | ExecutionState.DisplayRequired);
        if (!GetCursorPos(out var pos)) return;
        var deltaY = _movingUp ? -StepPixels : StepPixels;
        SetCursorPos(pos.X, pos.Y + deltaY);
        _movingUp = !_movingUp;
    }

    public void Dispose() => Stop();
}
