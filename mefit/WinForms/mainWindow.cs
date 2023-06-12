﻿// Mac EFI Toolkit
// https://github.com/MuertoGB/MacEfiToolkit

// WinForms
// mainWindow.cs
// Released under the GNU GLP v3.0

using Mac_EFI_Toolkit.Common;
using Mac_EFI_Toolkit.UI;
using Mac_EFI_Toolkit.UI.Renderers;
using Mac_EFI_Toolkit.Utils;
using Mac_EFI_Toolkit.WIN32;
using Mac_EFI_Toolkit.WinForms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Mac_EFI_Toolkit
{
    public partial class mainWindow : Form
    {

        #region Private Members
        private string _strInitialDirectory = Program.appDirectory;
        private static readonly object _lockObject = new object();
        private static System.Threading.Timer _statsTimer;
        private static bool _firmwareLoaded = false;
        #endregion

        #region Overriden Properties
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams Params = base.CreateParams;
                Params.Style |= Program.WS_MINIMIZEBOX;
                Params.ClassStyle = Params.ClassStyle | Program.CS_DBLCLKS | Program.CS_DROP;
                return Params;
            }
        }
        #endregion

        #region Constructor
        public mainWindow()
        {
            InitializeComponent();

            Load += mainWindow_Load;
            FormClosing += mainWindow_FormClosing;
            KeyDown += mainWindow_KeyDown;
            DragEnter += mainWindow_DragEnter;
            DragDrop += mainWindow_DragDrop;

            SetTipHandlers();

            SetMouseMoveEventHandlers();
            SetContextMenuRenderers();
            SetButtonProperties();

            lblVersion.Text = Application.ProductVersion;

            TimerCallback callback = new TimerCallback(UpdateMemoryStats);
            _statsTimer = new System.Threading.Timer(callback, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }
        #endregion

        #region Window Events
        private void mainWindow_Load(object sender, EventArgs e)
        {
            ToggleControlEnable(false);

            lblVersion.Text = Application.ProductVersion;

            SetPrimaryInitialDirectory();

            if (!string.IsNullOrEmpty(Program.draggedFile))
            {
                OpenBinary(Program.draggedFile);
            }

            if (!Settings.SettingsGetBool(SettingsBoolType.DisableVersionCheck) && !IsDebugMode())
            {
                CheckForNewVersion();
            }
        }

        private bool IsDebugMode()
        {
#if DEBUG
            return true;
#else
    return false;
#endif
        }

        private void mainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ModifierKeys == Keys.Alt || ModifierKeys == Keys.F4)
            {
                // We need to cancel the original request to close first, otherwise ExitMet() will close the application regardless of user choice.
                if (!Settings.SettingsGetBool(SettingsBoolType.DisableConfDiag))
                {
                    e.Cancel = true;
                }

                Program.ExitMet(this);
                _statsTimer.Dispose();
            }
        }

        private void mainWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (draggedFiles.Length == 1)
                {
                    // Check if the dragged item is a file and not a folder
                    string draggedFile = draggedFiles[0];
                    FileAttributes attributes = File.GetAttributes(draggedFile);
                    if ((attributes & FileAttributes.Directory) == 0)
                    {
                        e.Effect = DragDropEffects.Copy;
                        return;
                    }
                }
            }

            e.Effect = DragDropEffects.None;
        }

        private void mainWindow_DragDrop(object sender, DragEventArgs e)
        {
            string[] draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            string draggedFilename = draggedFiles[0];
            OpenBinary(draggedFilename);
        }
        #endregion

        #region KeyDown Events
        private void mainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl + Key combinations
            if (e.Modifiers == Keys.Control)
            {
                if (e.KeyCode == Keys.O)
                {
                    cmdOpenBin.PerformClick();
                }
                else if (e.KeyCode == Keys.R)
                {
                    cmdReset.PerformClick();
                }
                else if (e.KeyCode == Keys.E)
                {
                    cmdEditEfirom.PerformClick();
                }
                else if (e.KeyCode == Keys.M)
                {
                    ShowContextMenu(cmdMenu, cmsMainMenu);
                }
                else if (e.KeyCode == Keys.S)
                {
                    using (var frm = new settingsWindow())
                    {
                        frm.ShowDialog();
                    }
                }
                else if (e.KeyCode == Keys.A)
                {
                    using (var frm = new aboutWindow())
                    {
                        frm.ShowDialog();
                    }
                }
            }

            // Alt + Key combinations
            if (e.Modifiers == Keys.Alt)
            {
                if (e.KeyCode == Keys.N)
                {
                    cmdNavigate.PerformClick();
                }
                else if (e.KeyCode == Keys.R)
                {
                    cmdReload.PerformClick();
                }
                else if (e.KeyCode == Keys.C)
                {
                    cmdEveryMacSearch.PerformClick();
                }
                else if (e.KeyCode == Keys.E)
                {
                    cmdExportFsysBlock.PerformClick();
                }
                else if (e.KeyCode == Keys.F)
                {
                    cmdFixFsysCrc.PerformClick();
                }
            }
        }
        #endregion

        #region Mouse Events
        private void mainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture(new HandleRef(this, Handle));
                NativeMethods.SendMessage(new HandleRef(this, Handle), Program.WM_NCLBUTTONDOWN, (IntPtr)Program.HT_CAPTION, (IntPtr)0);
            }
        }

        private void SetMouseMoveEventHandlers()
        {
            Control[] controls = { tlpMain, tlpMainIcon, lblWindowTitle, tlpMenu };
            foreach (Control control in controls)
            {
                control.MouseMove += mainWindow_MouseMove;
            }
        }
        #endregion

        #region Button Events
        private void cmdClose_Click(object sender, EventArgs e)
        {
            Program.ExitMet(this);
        }

        private void cmdMin_Click(object sender, EventArgs e)
        {
            MinimizeWindow();
        }

        private void cmdMenu_Click(object sender, EventArgs e)
        {
            Control control = sender as Control;
            ShowContextMenu(control, cmsMainMenu);
        }

        private void cmdOpenBin_Click(object sender, EventArgs e)
        {

            using (var dialog = new OpenFileDialog
            {
                InitialDirectory = _strInitialDirectory,
                Filter = "Binary Files (*.rom, *.bin)|*.rom;*.bin|All Files (*.*)|*.*"
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    OpenBinary(dialog.FileName);
                }
            }
        }

        private void cmdResetUnload_Click(object sender, EventArgs e)
        {
            if (Settings.SettingsGetBool(SettingsBoolType.DisableConfDiag))
            {
                ResetAllData();
            }
            else
            {
                var result = METMessageBox.Show(this, "Reset", "This will clear all data, and unload the binary.\r\nAre you sure you want to reset?", MsgType.Warning, MsgButton.YesNoCancel);

                switch (result)
                {
                    case DialogResult.Yes:
                        ResetAllData();
                        ToggleControlEnable(false);
                        break;
                    case DialogResult.No:
                        break;
                }
            }
        }

        private void cmdEditEfirom_Click(object sender, EventArgs e)
        {
            bool bOpenEditor = Settings.SettingsGetBool(SettingsBoolType.AcceptedEditingTerms);

            if (!bOpenEditor)
            {
                Opacity = 0.5;
                using (Form frm = new termsWindow())
                {
                    frm.FormClosed += ChildWindowClosed;
                    DialogResult dr = frm.ShowDialog();
                    bOpenEditor = (dr != DialogResult.No);
                }
            }

            if (bOpenEditor)
            {
                Opacity = 0.5;
                using (Form frm = new editorWindow())
                {
                    frm.FormClosed += ChildWindowClosed;
                    frm.ShowDialog();
                }
            }

        }

        private void cmdEveryMacSearch_Click(object sender, EventArgs e)
        {
            Process.Start(string.Concat("https://everymac.com/ultimate-mac-lookup/?search_keywords=", FWBase.FsysSectionData.Serial));
        }

        private void cmdExportFsysBlock_Click(object sender, EventArgs e)
        {
            if (FWBase.FsysSectionData.FsysBytes == null)
            {
                METMessageBox.Show(this, "Error", "Fsys block bytes empty.", MsgType.Critical, MsgButton.Okay);
                return;
            }

            using (var dialog = new SaveFileDialog
            {
                Filter = "Binary Files (*.bin)|*.bin",
                Title = "Export Fsys region data...",
                FileName = string.Concat("FSYS_RGN_", FWBase.FileInfoData.FileNameNoExt, ".bin"),
                OverwritePrompt = true,
                InitialDirectory = _strInitialDirectory
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                File.WriteAllBytes(dialog.FileName, FWBase.FsysSectionData.FsysBytes);
            }
        }

        private void cmdFixFsysCrc_Click(object sender, EventArgs e)
        {
            // Fsys region was not found by the firmware parser
            if (FWBase.FsysSectionData.FsysBytes == null)
            {
                METMessageBox.Show(this, "Error", "Fsys block bytes empty.", MsgType.Critical, MsgButton.Okay);
                return;
            }

            using (var dialog = new SaveFileDialog
            {
                Filter = "Binary Files (*.bin)|*.bin",
                Title = "Save File...",
                FileName = $"FSYS_Fixed_{FWBase.FileInfoData.FileNameNoExt}.bin",
                OverwritePrompt = true,
                InitialDirectory = _strInitialDirectory
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    // Action was cancelled, nothing to do
                    return;
                }

                //// Calculate crc32 of the temporary Fsys bytes
                uint newCrc = EFIUtils.GetUintFsysCrc32(FWBase.FsysSectionData.FsysBytes);
                // Convert new crc32 uint to bytes
                byte[] newCrcBytes = BitConverter.GetBytes(newCrc);
                // Overwrite the loaded Fsys crc32 with the newly calculated crc32
                BinaryUtils.OverwriteBytesAtOffset(FWBase.LoadedBinaryBytes, FWBase.FsysSectionData.FsysOffset + 0x7FC, newCrcBytes);

                // Write the new binary to disk
                File.WriteAllBytes(dialog.FileName, FWBase.LoadedBinaryBytes);

                // Check the new file exists
                if (File.Exists(dialog.FileName))
                {
                    // Ask if user wants to open the repaired file
                    DialogResult result = METMessageBox.Show(this, "File Saved", "New file saved. Would you like to load the new file?", MsgType.Information, MsgButton.YesNoCancel);

                    if (result == DialogResult.Yes)
                    {
                        // Load new file data
                        OpenBinary(dialog.FileName);
                    }
                }
            }
        }

        private void cmdReload_Click(object sender, EventArgs e)
        {
            if (File.Exists(FWBase.LoadedBinaryPath))
            {
                var fileBytes = File.ReadAllBytes(FWBase.LoadedBinaryPath);
                var shaOnDisk = FileUtils.GetSha256Digest(fileBytes);
                var shaInMemory = FileUtils.GetSha256Digest(FWBase.LoadedBinaryBytes);

                if (!string.Equals(shaOnDisk, shaInMemory))
                {
                    OpenBinary(FWBase.LoadedBinaryPath);
                }
                else
                {
                    METMessageBox.Show(this, "MET", "File on disk matches file in memory. Data was not refreshed.", MsgType.Information, MsgButton.Okay);
                }
            }
            else
            {
                METMessageBox.Show(this, "MET", "The file on disk could not be found, it may have been moved or deleted.", MsgType.Critical, MsgButton.Okay);
            }
        }

        private void cmdNavigate_Click(object sender, EventArgs e)
        {
            FileUtils.OpenFileInExplorer(FWBase.LoadedBinaryPath);
        }

        private void SetButtonProperties()
        {
            cmdClose.Font = Program.FONT_MDL2_REG_12;
            cmdClose.Text = Program.closeChar;
            cmdMenu.Font = Program.FONT_MDL2_REG_14;
            cmdMenu.Text = "\xE169";
            cmdNavigate.Font = Program.FONT_MDL2_REG_9;
            cmdNavigate.Text = "\xEC50";
            cmdReload.Font = Program.FONT_MDL2_REG_9;
            cmdReload.Text = "\xE72C";
            cmdExportFsysBlock.Font = Program.FONT_MDL2_REG_9;
            cmdExportFsysBlock.Text = "\xE74E";
            cmdFixFsysCrc.Font = Program.FONT_MDL2_REG_9;
            cmdFixFsysCrc.Text = "\xE90F";
            cmdEveryMacSearch.Font = Program.FONT_MDL2_REG_9;
            cmdEveryMacSearch.Text = "\xF6FA";
        }
        #endregion

        #region Toolstrip Events
        private void viewLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(Logger.strLogFilePath))
            {
                Process.Start(Logger.strLogFilePath);
            }
            else
            {
                METMessageBox.Show(this, "File Information", "The log file was not detected, it has not yet been created.", MsgType.Information, MsgButton.Okay);
            }
        }

        private void restartApplicationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.RestartMet(this);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Opacity = 0.5;
            using (Form formWindow = new settingsWindow())
            {
                formWindow.FormClosed += ChildWindowClosed;
                formWindow.ShowDialog();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Opacity = 0.5;
            using (Form formWindow = new aboutWindow())
            {
                formWindow.FormClosed += ChildWindowClosed;
                formWindow.ShowDialog();
            }

        }

        private void MinimizeWindow()
        {
            WindowState = FormWindowState.Minimized;
        }

        private void resetPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CenterToScreen();
        }

        private void restartApplicationToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Program.RestartMet(this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.ExitMet(this);
        }
        #endregion

        #region Label Events
        private void lblVersion_Click(object sender, EventArgs e)
        {
            Process.Start(METVersion.strLatestUrl);
        }
        #endregion

        #region Picturebox Events
        private void pbxTitleLogo_Click(object sender, EventArgs e)
        {
            Control control = sender as Control;
            ShowContextMenu(control, cmsApplication);
        }

        private void minimizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MinimizeWindow();
        }
        #endregion

        #region Misc Events
        private void ToggleControlEnable(bool enable)
        {
            Button[] buttons = { cmdReset, cmdEditEfirom, cmdExportFsysBlock, cmdEveryMacSearch };
            foreach (Button button in buttons)
            {
                button.Enabled = enable ? true : false;
            }

            // Overrides > For when setting 'Override valid fsys enforement' is enabled, and the Fsys region is not found.
            // I may revisit here in the future.
            if (FWBase.FsysSectionData.FsysBytes != null)
            {
                cmdFixFsysCrc.Enabled = EFIUtils.GetUintFsysCrc32(FWBase.FsysSectionData.FsysBytes).ToString("X8") == FWBase.FsysSectionData.CRC32 ? false : true;
            }
            else
            {
                cmdFixFsysCrc.Enabled = false;
                cmdExportFsysBlock.Enabled = false;
                cmdEditEfirom.Enabled = false;
            }

            cmdEveryMacSearch.Enabled = (FWBase.FsysSectionData.Serial != null);

            tlpMain.Enabled = enable;
        }

        private void SetContextMenuRenderers()
        {
            ContextMenuStrip[] menus = { cmsMainMenu, cmsApplication, cmsFsysMenu };
            foreach (var menu in menus)
            {
                menu.Renderer = new METMenuRenderer();
            }
        }

        private void ChildWindowClosed(object sender, EventArgs e)
        {
            Opacity = 1.0;
        }

        private void ShowContextMenu(Control control, ContextMenuStrip menu)
        {
            Point lowerLeft = new Point(-1, control.Height + 2);
            lowerLeft = control.PointToScreen(lowerLeft);
            menu.Show(lowerLeft);
        }

        internal async void CheckForNewVersion()
        {
            var result = await METVersion.CheckForNewVersion("https://raw.githubusercontent.com/MuertoGB/MacEfiToolkit/main/files/app/version.xml");

            if (result == VersionCheckResult.NewVersionAvailable)
            {
                lblVersion.ForeColor = Color.Tomato;
                lblVersion.Cursor = Cursors.Hand;
                lblVersion.Click += lblVersion_Click;
            }
        }

        private void SetPrimaryInitialDirectory()
        {
            string path = Settings.SettingsGetString(SettingsStringType.InitialDirectory);
            if (!string.IsNullOrEmpty(path))
            {
                if (Directory.Exists(path))
                {
                    _strInitialDirectory = path;
                }
                else
                {
                    _strInitialDirectory = Program.appDirectory;
                }
            }
        }

        private void UpdateMemoryStats(object state)
        {
            lock (_lockObject)
            {
                string privateMemoryString = Helper.FormatSize((ulong)Program.GetPrivateMemorySize());

                lblPrivateMemory.Invoke((Action)(() =>
                {
                    lblPrivateMemory.Text = $"{privateMemoryString}";
                }));
            }
        }

        private void SetTipHandlers()
        {
            cmdNavigate.MouseEnter += HandleMouseEnterTip;
            cmdNavigate.MouseLeave += HandleMouseLeaveTip;
            cmdReload.MouseEnter += HandleMouseEnterTip;
            cmdReload.MouseLeave += HandleMouseLeaveTip;
            cmdExportFsysBlock.MouseEnter += HandleMouseEnterTip;
            cmdExportFsysBlock.MouseLeave += HandleMouseLeaveTip;
            cmdFixFsysCrc.MouseEnter += HandleMouseEnterTip;
            cmdFixFsysCrc.MouseLeave += HandleMouseLeaveTip;
            cmdEveryMacSearch.MouseEnter += HandleMouseEnterTip;
            cmdEveryMacSearch.MouseLeave += HandleMouseLeaveTip;
            cmdOpenBin.MouseEnter += HandleMouseEnterTip;
            cmdOpenBin.MouseLeave += HandleMouseLeaveTip;
            cmdReset.MouseEnter += HandleMouseEnterTip;
            cmdReset.MouseLeave += HandleMouseLeaveTip;
            cmdEditEfirom.MouseEnter += HandleMouseEnterTip;
            cmdEditEfirom.MouseLeave += HandleMouseLeaveTip;
            lblPrivateMemory.MouseEnter += HandleMouseEnterTip;
            lblPrivateMemory.MouseLeave += HandleMouseLeaveTip;
        }

        private void HandleMouseEnterTip(object sender, EventArgs e)
        {
            if (!Settings.SettingsGetBool(SettingsBoolType.DisableTips))
            {
                if (sender == cmdNavigate)
                    lblMessage.Text = "Navigate to file in explorer";
                else if (sender == cmdReload)
                    lblMessage.Text = "Reload current file from disk";
                else if (sender == cmdExportFsysBlock)
                    lblMessage.Text = "Export Fsys Region";
                else if (sender == cmdFixFsysCrc)
                    lblMessage.Text = "Repair Fsys CRC32";
                else if (sender == cmdEveryMacSearch)
                    lblMessage.Text = "View serial number information with EveryMac";
                else if (sender == cmdOpenBin)
                    lblMessage.Text = "Open an EFIROM";
                else if (sender == cmdReset)
                    lblMessage.Text = "Unload EFIROM and clear all data";
                else if (sender == cmdEditEfirom)
                    lblMessage.Text = "Open the firmware editor";
                else if (sender == lblPrivateMemory)
                    lblMessage.Text = "Private memory consumption";
            }
        }

        private void HandleMouseLeaveTip(object sender, EventArgs e)
        {
            lblMessage.Text = string.Empty;
        }

        internal void UpdateControls()
        {
            // File information
            lblFilename.Text = $"FILE: '{FWBase.FileInfoData.FileNameWithExt}'";
            int loadedFileSize = (int)FWBase.FileInfoData.FileLength;
            bool isValidBinSize = FileUtils.GetIsValidBinSize(loadedFileSize);
            lblFileSizeBytes.Text = FileUtils.FormatFileSize(loadedFileSize);
            lblFileSizeBytes.ForeColor = isValidBinSize ? Colours.clrGood : Colours.clrUnknown;
            lblFileSizeBytes.Text += isValidBinSize ? string.Empty : $" ({FileUtils.GetSizeDifference(loadedFileSize)})";
            lblFileCreatedDate.Text = FWBase.FileInfoData.CreationTime;
            lblFileModifiedDate.Text = FWBase.FileInfoData.LastWriteTime;
            lblFileCrc.Text = FWBase.FileInfoData.CRC32;

            // Firmware Data
            lblSerialNumber.Text = FWBase.FsysSectionData.Serial ?? "N/A";
            lblHwc.Text = FWBase.FsysSectionData.HWC ?? "N/A";
            if (FWBase.FsysSectionData.CRC32 != null)
            {
                lblFsysCrc.Text = $"{FWBase.FsysSectionData.CRC32}h";
                lblFsysCrc.ForeColor = FWBase.FsysSectionData.CRC32Calc == FWBase.FsysSectionData.CRC32 ? Colours.clrGood : Colours.clrError;
            }
            else
            {
                lblFsysCrc.Text = "N/A";
                lblFsysCrc.ForeColor = Color.White;
            }
            lblApfsCapable.Text = FWBase.IsApfsCapable;
            lblApfsCapable.ForeColor = FWBase.IsApfsCapable == "Yes" ? Colours.clrGood : Colours.clrUnknown;
            lblEfiVersion.Text = FWBase.ROMInfoData.EfiVersion ?? "N/A";
            lblRomVersion.Text = FWBase.ROMInfoData.RomVersion ?? "N/A";
            lblFitVersion.Text = FWBase.FitVersion;
            lblMeVersion.Text = FWBase.MeVersion;
            lblModel.Text = $"MODEL: {FWBase.EFISectionStore.Model ?? "N/A"}";
            lblBoardId.Text = FWBase.PDRSectionData.MacBoardId ?? "N/A";
            lblOrderNo.Text = FWBase.FsysSectionData.SON ?? "N/A";

            // Load the config code asynchronously
            if (FWBase.FsysSectionData.HWC != null)
            {
                GetConfigCodeAsync(FWBase.FsysSectionData.HWC);
            }

            ToggleControlEnable(true);
        }

        internal async void GetConfigCodeAsync(string strHwc)
        {
            var configCode = await EFIUtils.GetDeviceConfigCodeAsync(strHwc);

            if (configCode != null)
            {
                lblModel.Text += $" '{configCode}'";
            }
        }
        #endregion

        #region Firmware Parsing
        private bool IsValidMinMaxSize()
        {
            var fileInfo = new FileInfo(FWBase.LoadedBinaryPath);

            // The file is too small, ignore it.
            if (fileInfo.Length < FWBase.MIN_IMAGE_SIZE) // 1048576 bytes
            {
                METMessageBox.Show(this, "Warning", "The file is too small and was ignored.", MsgType.Warning, MsgButton.Okay);
                return false;
            }

            // The file is too large, ignore it.
            if (fileInfo.Length > FWBase.MAX_IMAGE_SIZE) // 33554432 bytes
            {
                METMessageBox.Show(this, "Warning", "The file is too large and was ignored.", MsgType.Warning, MsgButton.Okay);
                return false;
            }

            return true;
        }

        private bool IsValidFlashHeader()
        {
            // Invalid flash descriptor signature
            if (!Settings.SettingsGetBool(SettingsBoolType.DisableDescriptorEnforce))
            {
                if (!FWBase.GetIsValidFlashHeader(FWBase.LoadedBinaryBytes))
                {
                    METMessageBox.Show(this, "Warning", "File ignored, the flash descriptor signature was invalid.", MsgType.Warning, MsgButton.Okay);
                    return false;
                }
            }

            return true;
        }

        private void OpenBinary(string filePath)
        {
            ToggleControlEnable(false);

            FWBase.LoadedBinaryPath = filePath;
            FWBase.LoadedBinaryBytes = File.ReadAllBytes(filePath);

            if (IsValidMinMaxSize() && IsValidFlashHeader())
            {
                if (_firmwareLoaded)
                {
                    ResetAllData();
                }

                _strInitialDirectory = Path.GetDirectoryName(filePath);
                FWBase.LoadFirmwareBaseData(FWBase.LoadedBinaryBytes, filePath);

                UpdateControls();
                _firmwareLoaded = true;
            }
            else
            {
                ResetAllData();
                _firmwareLoaded = false;
            }
        }

        private void ResetAllData()
        {
            // Clear labels
            Label[] labels =
            {
                lblFilename, lblFileSizeBytes, lblFileCrc, lblFileCreatedDate, lblFileModifiedDate,
                lblModel, lblSerialNumber, lblHwc, lblFsysCrc, lblApfsCapable, lblEfiVersion, lblRomVersion,
                lblFitVersion, lblMeVersion, lblBoardId, lblOrderNo
            };
            foreach (Label label in labels)
            {
                label.Text = string.Empty;
                label.ForeColor = Color.White;
            }

            // Reset private members
            SetPrimaryInitialDirectory();

            // Garbage collect
            GC.Collect();
            if (GC.WaitForFullGCApproach() == GCNotificationStatus.Succeeded)
            {
                if (GC.WaitForFullGCComplete() == GCNotificationStatus.Succeeded)
                {
                    GC.WaitForPendingFinalizers();
                }
            }

            _firmwareLoaded = false;
        }
        #endregion

        //private void cmdRomInfo_Click(object sender, EventArgs e)
        //{
        //    StringBuilder outputBuilder = new StringBuilder();

        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.SectionOffset:X2}h");
        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.BiosId}");
        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.Model}");
        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.EfiVersion}");
        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.BuiltBy}");
        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.DateStamp}");
        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.Revision}");
        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.RomVersion}");
        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.BuildcaveId}");
        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.BuildType}");
        //    outputBuilder.AppendLine($"{FWBase.ROMInfoData.Compiler}");

        //    string outputText = outputBuilder.ToString();

        //    MessageBox.Show(outputText, "ROM Info Test Box");
        //}
    }
}