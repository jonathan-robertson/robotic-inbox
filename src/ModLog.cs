using System;

namespace RoboticInbox
{
    internal class ModLog<T>
    {
        private readonly string _className;

        public ModLog()
        {
            _className = typeof(T).FullName;
        }

        public void Trace(string message)
        {
            if (ModApi.DebugMode)
            {
                Log.Out($"[{_className}] TRACE: {message}");
            }
        }

        public void Debug(string message)
        {
            if (ModApi.DebugMode)
            {
                Log.Out($"[{_className}] DEBUG: {message}");
            }
        }

        public void Info(string message)
        {
            Log.Out($"[{_className}] {message}");
        }

        public void Warn(string message, Exception e = null)
        {
            Log.Warning($"[{_className}] {message}");
            if (e != null)
            {
                Log.Warning($"[{_className}] {message}\n{e.Message}\n{e.StackTrace}");
                Log.Exception(e);
            }
        }

        public void Error(string message, Exception e = null)
        {
            if (e == null)
            {
                Log.Error($"[{_className}] {message}");
            }
            else
            {
                Log.Error($"[{_className}] {message}\n{e.Message}\n{e.StackTrace}");
                Log.Exception(e);
            }
        }
    }
}
