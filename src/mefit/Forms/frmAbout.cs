﻿// Mac EFI Toolkit
// https://github.com/MuertoGB/MacEfiToolkit

// Forms
// frmAbout.cs
// Released under the GNU GLP v3.0

using Mac_EFI_Toolkit.UI;
using System;
using System.Windows.Forms;

namespace Mac_EFI_Toolkit.Forms
{
    public partial class frmAbout : METForm
    {
        #region Constructor
        public frmAbout()
        {
            InitializeComponent();

            // Attach event handlers.
            WireEventHandlers();

            // Enable drag.
            UITools.EnableFormDrag(this, lblTitle);

            // Set button properties.
            SetButtonGlyphAndText();
        }

        private void WireEventHandlers()
        {
            Load += frmAbout_Load;
            KeyDown += frmAbout_KeyDown;
        }
        #endregion

        #region Window Events
        private void frmAbout_Load(object sender, EventArgs e)
        {
            lblBuild.Text = $"{Application.ProductVersion}.{METVersion.APP_BUILD} ({METVersion.APP_CHANNEL})";
            lblChannel.Text = $"{APPSTRINGS.LZMA_SDK} {METVersion.LZMA_SDK}";
        }
        #endregion

        #region KeyDown Events
        private void frmAbout_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }
        #endregion

        #region Button Events
        private void cmdClose_Click(object sender, EventArgs e) => Close();
        #endregion

        #region UI Events
        private void SetButtonGlyphAndText()
        {
            cmdClose.Font = Program.FONT_MDL2_REG_10;
            cmdClose.Text = Program.GLYPH_EXIT_CROSS;
        }
        #endregion
    }
}