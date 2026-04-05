using System.Runtime.InteropServices;

namespace GoMicFuckYourself.Tray;

internal static class SingleInstanceSignal
{
    private const string ActivationMessageName = "GoMicFuckYourself.Tray.Activate";
    private static readonly uint ActivationMessage = RegisterWindowMessage(ActivationMessageName);

    public static void NotifyRunningInstance()
    {
        if (ActivationMessage == 0) return;

        PostMessage((nint)HwndBroadcast, ActivationMessage, nint.Zero, nint.Zero);
    }

    public static bool IsActivationMessage(Message message)
    {
        return message.Msg == ActivationMessage;
    }

    private const int HwndBroadcast = 0xffff;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);
}
