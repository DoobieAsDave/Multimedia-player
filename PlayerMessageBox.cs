using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DoobieAsPlayer
{
    public partial class PlayerMessageBox : Form
    {
        public bool ReturnMode { get; set; }

        private bool playerMode;

        public PlayerMessageBox(string iconMode, string messageText, string messageTitle, bool buttonMode, bool playerMode)
        {
            try
            {
                InitializeComponent();

                this.playerMode = playerMode;

                if (playerMode)
                {
                    Icon = Properties.Resources.music;
                }
                else
                {
                    Icon = Properties.Resources.video;
                }

                switch (iconMode)
                {
                    case "success":
                        pcbMessageIcon.Image = Properties.Resources.success;
                        break;
                    case "error":
                        pcbMessageIcon.Image = Properties.Resources.error;
                        break;
                    case "information":
                        pcbMessageIcon.Image = Properties.Resources.information;
                        break;
                    case "question":
                        pcbMessageIcon.Image = Properties.Resources.question;
                        break;
                    case "dude":
                        pcbMessageIcon.Image = Properties.Resources.dude2;
                        lblSoundcloudLink.Visible = true;
                        break;
                }

                if (buttonMode)
                {
                    btnYes.Visible = false;
                    btnNo.Visible = false;
                    btnOK.Visible = true;
                }
                else
                {
                    btnYes.Visible = true;
                    btnNo.Visible = true;
                    btnOK.Visible = false;
                }

                if (messageText.Split('\n').Length > 1)
                {
                    txtMessageBox.Location = new Point(80, 25);
                }
                else
                {
                    txtMessageBox.Location = new Point(80, 40);
                }

                txtMessageBox.Text = messageText;
                Text = messageTitle;
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (playerMode)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occured", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occured", true, false);
                }

                message.ShowDialog();
            }
        }

        private void btnYes_Click(object sender, EventArgs e)
        {
            ReturnMode = true;

            Close();
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            ReturnMode = false;

            Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            ReturnMode = true;

            Close();
        }

        private void lblSoundcloudLink_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"https://soundcloud.com/doobieasdave");
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (playerMode)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occured", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occured", true, false);
                }

                message.ShowDialog();
            }
        }
    }
}
