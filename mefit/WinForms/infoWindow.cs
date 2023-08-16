﻿// Mac EFI Toolkit
// https://github.com/MuertoGB/MacEfiToolkit

// WinForms
// infoWindow.cs
// Released under the GNU GLP v3.0

using Mac_EFI_Toolkit.Common;
using Mac_EFI_Toolkit.UI;
using Mac_EFI_Toolkit.Utils;
using Mac_EFI_Toolkit.WIN32;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Mac_EFI_Toolkit.WinForms
{
    public partial class infoWindow : Form
    {

        #region Overriden Properties
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams Params = base.CreateParams;
                Params.ClassStyle = Params.ClassStyle | Program.CS_DBLCLKS | Program.CS_DROP;
                return Params;
            }
        }
        #endregion

        #region Constructor
        public infoWindow()
        {
            InitializeComponent();

            Load += infoWindow_Load;
            KeyDown += infoWindow_KeyDown;
            lblTitle.MouseMove += infoWindow_MouseMove;

            InterfaceUtils.SetTableLayoutPanelHeight(tlpMain);

            cmdClose.Font = Program.FONT_MDL2_REG_12;
            cmdClose.Text = Chars.EXIT_CROSS;
        }
        #endregion

        #region Window Events
        private void infoWindow_Load(object sender, EventArgs e)
        {
            lblBiosId.Text = FWBase.AppleRomInfoSectionData.BiosId
                ?? "N/A";
            lblModel.Text = FWBase.AppleRomInfoSectionData.Model != null
                ? $"{FWBase.AppleRomInfoSectionData.Model} ({MacUtils.ConvertEfiModelCode(FWBase.AppleRomInfoSectionData.Model)})"
                : "N/A";
            lblEfiVersion.Text =
                FWBase.AppleRomInfoSectionData.EfiVersion
                ?? "N/A";
            lblBuiltBy.Text =
                FWBase.AppleRomInfoSectionData.BuiltBy
                ?? "N/A";
            lblDateStamp.Text =
                FWBase.AppleRomInfoSectionData.DateStamp
                ?? "N/A";
            lblRevision.Text =
                FWBase.AppleRomInfoSectionData.Revision
                ?? "N/A";
            lblBootRom.Text =
                FWBase.AppleRomInfoSectionData.RomVersion
                ?? "N/A";
            lblBuildcaveId.Text =
                FWBase.AppleRomInfoSectionData.BuildcaveId
                ?? "N/A";
            lblBuildType.Text =
                FWBase.AppleRomInfoSectionData.BuildType
                ?? "N/A";
            lblCompiler.Text =
                FWBase.AppleRomInfoSectionData.Compiler
                ?? "N/A";
            lblSectionData.Text =
                $"Base: {FWBase.AppleRomInfoSectionData.SectionBase:X2}h, " +
                $"Size: {FWBase.AppleRomInfoSectionData.SectionBytes.Length:X2}h"
                ?? string.Empty;

            foreach (Label label in tlpMain.Controls)
            {
                if (label.Text == "N/A")
                {
                    label.ForeColor = Colours.DISABLED_TEXT;
                }
            }
        }
        #endregion

        #region Mouse Events
        private void infoWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture(new HandleRef(this, Handle));
                NativeMethods.SendMessage(new HandleRef(this, Handle), Program.WM_NCLBUTTONDOWN, (IntPtr)Program.HT_CAPTION, (IntPtr)0);
            }
        }
        #endregion

        #region KeyDown Events
        private void infoWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }
        #endregion

        #region Button Events
        private void cmdClose_Click(object sender, System.EventArgs e)
        {
            Close();
        }
        #endregion

    }
}