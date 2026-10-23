using System;
using System.Threading;
using TodoLearn.Views;

namespace TodoLearn.Services
{
    public class InactivityService
    {
        private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(1);
        private readonly object _lock = new object();

        private DateTime _lastActivity;
        private bool _isRunning;
        private Timer? _timer;
        private SynchronizationContext? _syncContext;

        public event Action? OnTimeout;

        public void Start()
        {
            lock (_lock)
            {
                if (_isRunning) return;

                _isRunning = true;
                _lastActivity = DateTime.Now;
                _syncContext = SynchronizationContext.Current;

                _timer = new Timer(_ => CheckTimeout(), null, _checkInterval, _checkInterval);
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

        private void CheckTimeout()
        {
            lock (_lock)
            {
                if (!_isRunning || IsOnLoginPage())
                    return;

                if (DateTime.Now - _lastActivity < _timeout)
                    return;

                _isRunning = false;
                _timer?.Dispose();
                _timer = null;

                if (_syncContext != null)
                    _syncContext.Post(_ => OnTimeout?.Invoke(), null);
                else
                    OnTimeout?.Invoke();
            }
        }

        private bool IsOnLoginPage()
        {
            try
            {
                var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                return currentPage is LoginPage;
            }
            catch
            {
                return false;
            }
        }
    }
}
