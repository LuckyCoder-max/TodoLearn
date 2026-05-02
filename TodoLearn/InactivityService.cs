using System;
using System.Threading;

public class InactivityService
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(1);
    private const int MinPasswordLength = 8;

    private DateTime _lastActivity;
    private bool _isRunning;
    private Timer? _timer;
    private SynchronizationContext? _syncContext;
    private readonly object _lock = new object();

    public event Action? OnTimeout;

    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning) return;

            _isRunning = true;
            _lastActivity = DateTime.Now;
            _syncContext = SynchronizationContext.Current;

            _timer = new Timer(_ =>
            {
                CheckTimeout();
            }, null, _checkInterval, _checkInterval);
        }
    }

    private void CheckTimeout()
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            if (DateTime.Now - _lastActivity >= _timeout)
            {
                _isRunning = false;
                _timer?.Dispose();
                _timer = null;

                if (_syncContext != null)
                    _syncContext.Post(_ => OnTimeout?.Invoke(), null); //
                else
                    OnTimeout?.Invoke(); //
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            if (!_isRunning) return;
            _lastActivity = DateTime.Now;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _isRunning = false;
            _timer?.Dispose();
            _timer = null;
        }
    }

    public (bool IsValid, string? ErrorMessage) ValidatePassword(string? password, bool isPasswordField = true)
    {
        if (!isPasswordField)
            return (true, null);

        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password cannot be empty");

        if (password.Length < MinPasswordLength)
            return (false, $"Password must be at least {MinPasswordLength} characters long");

        return (true, null);
    }
}