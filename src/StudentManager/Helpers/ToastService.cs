using System;

namespace StudentManager.Helpers
{
    public static class ToastService
    {
        public static event Action<string, bool>? ToastRequested;

        public static void Show(string message, bool isError = false)
        {
            ToastRequested?.Invoke(message, isError);
        }
    }
}
