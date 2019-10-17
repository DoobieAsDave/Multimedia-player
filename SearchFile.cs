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
    public partial class SearchFile : Form
    {
        public string SearchContent { get; set; }

        private bool playerMode;

        public SearchFile(bool playerMode)
        {
            try
            {
                InitializeComponent();

                this.playerMode = playerMode;

                if (playerMode)
                {
                    Icon = Properties.Resources.music;
                    Text += "Music";
                }
                else
                {
                    Icon = Properties.Resources.video;
                    Text += "Video";
                }
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

        private void btnStartSearch_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckSearchContent())
                {
                    SearchContent = txtSearchContent.Text;

                    Close();
                }
            }
            catch (Exception ex)
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

        private bool CheckSearchContent()
        {
            try
            {
                if (!string.IsNullOrEmpty(txtSearchContent.Text))
                {
                    return true;
                }
                else
                {
                    PlayerMessageBox message;

                    if (playerMode)
                    {
                        message = new PlayerMessageBox("information", "Please insert Text to search...", "Search Input is empty", true, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("information", "Please insert Text to search...", "Search Input is empty", true, false);
                    }

                    message.ShowDialog();

                    return false;
                }
            }
            catch (Exception ex)
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

                return false;
            }
        }
    }
}
