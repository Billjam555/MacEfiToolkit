﻿// Mac EFI Toolkit
// https://github.com/MuertoGB/MacEfiToolkit

// Program.cs
// Released under the GNU GLP v3.0
// MET uses embedded font resource "Segoe MDL2 Assets" which is copyright Microsoft Corp.

using Mac_EFI_Toolkit.UI;
using Mac_EFI_Toolkit.WIN32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Mac_EFI_Toolkit
{
    static class Program
    {
        internal static readonly string appBuild = $"{Application.ProductVersion}-230626.0415";
        internal static readonly string appChannel = "Release";
        internal static string appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static string fsysDirectory = Path.Combine(appDirectory, "fsys_stores");
        internal static string buildsDirectory = Path.Combine(appDirectory, "builds");
        internal static string appName = Assembly.GetExecutingAssembly().Location;
        internal static string draggedFile = string.Empty;
        internal static bool openLastBuild = false;
        internal static string lastBuildPath = string.Empty;
        internal static mainWindow mWindow;
        internal static System.Threading.Timer memoryTimer;

        #region Private Members
        private static NativeMethods.LowLevelKeyboardProc _kbProc = HookCallback;
        private static IntPtr _hookId = IntPtr.Zero;
        private static GCHandle _hookHandle;
        #endregion

        #region Const Members
        internal const int WM_NCLBUTTONDOWN = 0xA1;
        internal const int HT_CAPTION = 0x2;
        internal const int WS_MINIMIZEBOX = 0x20000;
        internal const int CS_DBLCLKS = 0x8;
        internal const int CS_DROP = 0x20000;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_UP = 0x26;
        private const int VK_LWIN = 0x5B;
        private const int KEY_PRESSED = 0x8000;
        #endregion

        #region Internal Fonts
        internal static Font FONT_MDL2_REG_20;
        internal static Font FONT_MDL2_REG_14;
        internal static Font FONT_MDL2_REG_12;
        internal static Font FONT_MDL2_REG_10;
        internal static Font FONT_MDL2_REG_9;
        #endregion

        #region Main Entry Point
        [STAThread]
        static void Main(string[] args)
        {
            // Register exception handler events early
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // Default framework stuff.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Web Security Protocol
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            // Font Data
            byte[] fontData = Properties.Resources.segmdl2;
            FONT_MDL2_REG_9 = new Font(LoadFontFromResource(fontData), 9.0F, FontStyle.Regular);
            FONT_MDL2_REG_10 = new Font(LoadFontFromResource(fontData), 10.0F, FontStyle.Regular);
            FONT_MDL2_REG_12 = new Font(LoadFontFromResource(fontData), 12.0F, FontStyle.Regular);
            FONT_MDL2_REG_14 = new Font(LoadFontFromResource(fontData), 14.0F, FontStyle.Regular);
            FONT_MDL2_REG_20 = new Font(LoadFontFromResource(fontData), 20.0F, FontStyle.Regular);

            // Settings
            if (!File.Exists(Settings.strSettingsFilePath)) Settings.SettingsCreateFile();

            // Register application exit event.
            Application.ApplicationExit += OnExiting;

            // Register low level keyboard hook for preventing WinKey+Up.
            HookKeyboard();

            // Get dragged filepath
            draggedFile = GetDraggedFilePath(args);

            // Create main window instance
            mWindow = new mainWindow();

            // Run mWindow instance.
            Application.Run(mWindow);
        }

        private static string GetDraggedFilePath(string[] args)
        {
            if (args.Length > 0 && File.Exists(args[0]))
            {
                return args[0];
            }

            return string.Empty;
        }

        #endregion

        #region OnExit
        private static void OnExiting(object sender, EventArgs e)
        {
            HandleExitCleanup();
        }

        private static void HandleExitCleanup()
        {
            // Dispose fonts
            FONT_MDL2_REG_9.Dispose();
            FONT_MDL2_REG_10.Dispose();
            FONT_MDL2_REG_12.Dispose();
            FONT_MDL2_REG_14.Dispose();
            FONT_MDL2_REG_20.Dispose();

            // Dispose memory stats timer
            memoryTimer.Dispose();

            // Unhook the low level keyboard hook
            UnhookKeyboard();
        }
        #endregion

        #region Keyboard Hook
        // Register the keyboard hook.
        internal static IntPtr SetHook(NativeMethods.LowLevelKeyboardProc kbProc)
        {
            using (Process process = Process.GetCurrentProcess())
            using (ProcessModule module = process.MainModule)
            {
                return NativeMethods.SetWindowsHookExA(WH_KEYBOARD_LL, kbProc, NativeMethods.GetModuleHandleA(module.ModuleName), 0);
            }
        }

        // Define the keyboard hook callback function
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == VK_UP && (NativeMethods.GetKeyState(VK_LWIN) & KEY_PRESSED) != 0)
                {
                    // Disable the Windows+Up shortcut by not passing it to the operating system
                    return (IntPtr)1;
                }
            }
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static void HookKeyboard()
        {
            _kbProc = HookCallback;
            _hookHandle = GCHandle.Alloc(_kbProc);
            _hookId = SetHook(_kbProc);
        }

        private static void UnhookKeyboard()
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookHandle.Free();
        }
        #endregion

        #region Font Resolver
        private static PrivateFontCollection _privateFontCollection = new PrivateFontCollection();
        internal static FontFamily LoadFontFromResource(byte[] fontData)
        {
            IntPtr pFileView = Marshal.AllocCoTaskMem(fontData.Length);
            Marshal.Copy(fontData, 0, pFileView, fontData.Length);
            try
            {
                uint pNumFonts = 0;
                NativeMethods.AddFontMemResourceEx(pFileView, (uint)fontData.Length, IntPtr.Zero, ref pNumFonts);
                _privateFontCollection.AddMemoryFont(pFileView, fontData.Length);
                return _privateFontCollection.Families.Last();
            }
            finally
            {
                if (pFileView != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pFileView);
                }
            }
        }
        #endregion

        #region Exception Handler
        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            if (e != null) METCatchUnhandledException(e.Exception);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            if (ex != null) METCatchUnhandledException(ex);
        }

        static void METCatchUnhandledException(Exception e)
        {
            Logger.WriteExceptionToAppLog(e);

            DialogResult result = MessageBox.Show($"{e.Message}\r\n\r\n{e}\r\n\r\nWould you like to force quit?", $"{e.GetType()}", System.Windows.Forms.MessageBoxButtons.YesNo, MessageBoxIcon.Error);

            if (result == DialogResult.Yes)
            {
                HandleExitCleanup(); // We need to clean any necessary objects as OnExit will not fire when Environment.Exit is called.
                Environment.Exit(-1);
            }

            // Fix for mainWindow opacity getting stuck at 0.5
            if (mWindow.Opacity != 1.0)
            {
                mWindow.Opacity = 1.0;
            }
        }
        #endregion

        #region Restart MET
        internal static void RestartMet(Form owner)
        {
            if (Settings.SettingsGetBool(SettingsBoolType.DisableConfDiag))
            {
                Application.Restart();
                return;
            }

            DialogResult result = METMessageBox.Show(owner, "Restart application", "Are you sure you want to restart the application?", METMessageType.Question, UI.METMessageButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                Application.Restart();
            }
        }
        #endregion

        #region Exit MET
        internal static void ExitMet(Form owner)
        {
            if (Settings.SettingsGetBool(SettingsBoolType.DisableConfDiag))
            {
                Application.Exit();
                return;
            }

            DialogResult result = METMessageBox.Show(owner, "Exit application", "Are you sure you want to quit the application?", METMessageType.Question, UI.METMessageButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }
        #endregion

    }
}