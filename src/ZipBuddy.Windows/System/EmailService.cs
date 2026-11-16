using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZipBuddy.Windows.System;

public static class EmailService
{
    [DllImport("MAPI32.DLL", CharSet = CharSet.Ansi)]
    private static extern int MAPISendMail(IntPtr sess, IntPtr hwnd, MapiMessage message, int flg, int rsv);

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    private const uint COINIT_APARTMENTTHREADED = 0x2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private sealed class MapiMessage
    {
        public int reserved;
        public string subject = string.Empty;
        public string noteText = string.Empty;
        public string messageType = string.Empty;
        public string dateReceived = string.Empty;
        public string conversationID = string.Empty;
        public int flags;
        public IntPtr originator;
        public int recipCount;
        public IntPtr recips;
        public int fileCount;
        public IntPtr files;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private sealed class MapiFileDesc
    {
        public int reserved;
        public int flags;
        public int position = -1;
        public string path = string.Empty;
        public string name = string.Empty;
        public IntPtr type = IntPtr.Zero;
    }

    private const int MAPI_LOGON_UI = 0x00000001;
    private const int MAPI_DIALOG = 0x00000008;

    /// <summary>
    /// Runs the modal MAPI dialog on a dedicated STA thread so the caller's UI thread stays responsive.
    /// Falls back to a mailto: URI plus Explorer file selection when no MAPI client is available.
    /// </summary>
    public static Task<bool> SendFileByEmailAsync(string filePath, string? subject = null)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            var comInitialized = false;
            try
            {
                var hr = CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);
                comInitialized = hr is 0 or 0x00010001; // S_OK or S_FALSE
                completion.TrySetResult(SendFileByEmail(filePath, subject));
            }
            catch (Exception)
            {
                completion.TrySetResult(false);
            }
            finally
            {
                if (comInitialized)
                    CoUninitialize();
            }
        })
        {
            IsBackground = true,
            Name = "ZipBuddy.MAPI"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    public static bool SendFileByEmail(string filePath, string? subject = null)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return false;

        try
        {
            var fileDesc = new MapiFileDesc
            {
                position = -1,
                path = filePath,
                name = Path.GetFileName(filePath)
            };

            var fileDescSize = Marshal.SizeOf(fileDesc);
            var pFileDesc = Marshal.AllocHGlobal(fileDescSize);
            try
            {
                Marshal.StructureToPtr(fileDesc, pFileDesc, false);

                var message = new MapiMessage
                {
                    subject = subject ?? $"Archive: {Path.GetFileName(filePath)}",
                    fileCount = 1,
                    files = pFileDesc
                };

                int result = MAPISendMail(IntPtr.Zero, IntPtr.Zero, message, MAPI_LOGON_UI | MAPI_DIALOG, 0);
                if (result == 0) // SUCCESS_SUCCESS
                {
                    return true;
                }
            }
            finally
            {
                Marshal.DestroyStructure<MapiFileDesc>(pFileDesc);
                Marshal.FreeHGlobal(pFileDesc);
            }
        }
        catch
        {
            // Ignore MAPI specific load exceptions
        }

        // Fallback: Open mailto URI and select the file in Explorer so user can attach easily
        try
        {
            var fileName = Path.GetFileName(filePath);
            var mailtoUri = $"mailto:?subject={Uri.EscapeDataString($"Archive: {fileName}")}";
            Process.Start(new ProcessStartInfo(mailtoUri) { UseShellExecute = true });
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
