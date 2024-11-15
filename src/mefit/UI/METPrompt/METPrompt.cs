﻿// Mac EFI Toolkit
// https://github.com/MuertoGB/MacEfiToolkit

// UI Components
// METPrompt.cs
// Released under the GNU GLP v3.0

using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace Mac_EFI_Toolkit.UI
{
    public partial class METPrompt : METForm
    {
        #region Static Members
        static SystemSound MB_SOUND;
        static string MB_MESSAGE;
        static METPromptType MB_TYPE;
        static METPromptButtons MB_BUTTONS;
        static DialogResult MB_RESULT;
        #endregion

        #region Private Members
        private const string INFO =
            "INFORMATION";

        private const string WARN =
            "WARNING";

        private const string ERROR =
            "ERROR";
        #endregion

        #region Constants
        private const int MaxWidth = 350;
        private const int MaxHeight = 800;
        private const int PaddingWidth = 20;
        private const int PaddingHeight = 60;
        #endregion

        #region Constructor
        public METPrompt()
        {
            InitializeComponent();

            // Attach event handlers.
            WireEventHandlers();

            // Enable drag.
            UITools.EnableFormDrag(this, lblTitle);
        }

        private void WireEventHandlers()
        {
            Load += new EventHandler(METMessageBox_Load);
            Shown += new EventHandler(METMessageBox_Shown);
            KeyDown += new KeyEventHandler(METMessageBox_KeyDown);
        }
        #endregion

        #region Window Events
        private void METMessageBox_Load(object sender, EventArgs e)
        {
            // Set title and color based on the message type.
            switch (MB_TYPE)
            {
                case METPromptType.Error:
                    lblTitle.ForeColor = Colours.CLR_ERROR;
                    lblTitle.Text = ERROR;
                    MB_SOUND = System.Media.SystemSounds.Hand;
                    break;
                case METPromptType.Warning:
                    lblTitle.ForeColor = Colours.CLR_WARNING;
                    lblTitle.Text = WARN;
                    MB_SOUND = System.Media.SystemSounds.Exclamation;
                    break;
                case METPromptType.Information:
                    lblTitle.ForeColor = Colours.CLR_INFO;
                    lblTitle.Text = INFO;
                    MB_SOUND = System.Media.SystemSounds.Beep;
                    break;
                case METPromptType.Question:
                    lblTitle.ForeColor = Colours.CLR_INFO;
                    lblTitle.Text = INFO;
                    MB_SOUND = System.Media.SystemSounds.Beep;
                    break;
            }

            lblMessage.Text = MB_MESSAGE;

            if (MB_BUTTONS == METPromptButtons.Okay)
            {
                cmdYes.Hide();
                cmdNo.Text = "OKAY";
            }
            else
            {
                cmdYes.Show();
                cmdNo.Show();
                cmdNo.Text = "NO";
            }

            AdjustFormSize();
        }

        private void METMessageBox_Shown(object sender, EventArgs e)
        {
            if (!Settings.ReadBool(SettingsBoolType.DisableMessageSounds))
                MB_SOUND.Play();

            UITools.FlashForecolor(lblTitle);
        }
        #endregion

        #region Dynamic Resizing
        private void AdjustFormSize()
        {
            lblMessage.MaximumSize = new Size(MaxWidth, MaxHeight);
            lblMessage.AutoSize = true;

            tlpMain.AutoSize = true;
            tlpMain.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Size preferredSize = lblMessage.PreferredSize;
            int idealWidth = Math.Min(preferredSize.Width, MaxWidth);
            int idealHeight = Math.Min(preferredSize.Height, MaxHeight);

            this.ClientSize = new Size(
                idealWidth + PaddingWidth,
                idealHeight + PaddingHeight
            );

            if (this.Owner != null)
            {
                this.Location = new Point(
                    this.Owner.Location.X + (this.Owner.Width - this.Width) / 2,
                    this.Owner.Location.Y + (this.Owner.Height - this.Height) / 2
                );
            }

            this.MaximumSize = this.Size;
        }
        #endregion

        #region KeyDown Events
        private void METMessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                MB_RESULT = DialogResult.Cancel;
                Close();
            }
        }
        #endregion

        #region Overriden Events
        public static DialogResult Show(Form owner, string message, METPromptType type, METPromptButtons buttons = METPromptButtons.Okay)
        {
            SetMessageBoxParameters(message, type, buttons);

            using (METPrompt messageBox = new METPrompt())
            {
                messageBox.StartPosition = owner == null
                    ? FormStartPosition.CenterScreen
                    : FormStartPosition.CenterParent;

                DialogResult result = messageBox.ShowDialog(owner);

                return MB_RESULT;
            }
        }

        private static void SetMessageBoxParameters(string message, METPromptType type, METPromptButtons buttons)
        {
            MB_MESSAGE = message;
            MB_TYPE = type;
            MB_BUTTONS = buttons;
        }
        #endregion

        #region Button Events
        private void cmdClose_Click(object sender, EventArgs e)
        {
            MB_RESULT = DialogResult.Cancel;
            Close();
        }

        private void cmdYes_Click(object sender, EventArgs e)
        {
            MB_RESULT = DialogResult.Yes;
            Close();
        }

        private void cmdNo_Click(object sender, EventArgs e)
        {
            MB_RESULT = MB_BUTTONS == METPromptButtons.Okay ? DialogResult.OK : DialogResult.Cancel;
            Close();
        }
        #endregion
    }
}