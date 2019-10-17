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
    public partial class FileList : Form
    {
        public string ListName { get; set; }
        public bool Cancelled { get; set; } = true;

        private bool listMode;
        private bool playerMode;

        public FileList(bool listMode, bool playerMode)
        {
            try
            {
                InitializeComponent();

                this.playerMode = playerMode;
                this.listMode = listMode;

                if (listMode)
                {
                    Text = "Create Playlist";
                    lblListName.Text = "Playlistname";
                    btnCreateList.Text = "Create Playlist";
                }
                else
                {
                    Text = "Create Backup";
                    lblListName.Text = "Backupname";
                    btnCreateList.Text = "Create Backup";
                }

                if (playerMode)
                {
                    Icon = Properties.Resources.music;
                }
                else
                {
                    Icon = Properties.Resources.video;
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

        private void btnCreateList_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtListName.Text))
                {
                    ListName = txtListName.Text;
                    Cancelled = false;

                    Close();
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

        private void FileList_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Cancelled)
            {
                PlayerMessageBox message;

                if (listMode)
                {
                    message = new PlayerMessageBox("question", "Dou you really want to cancel creating the Playlist?", "Cancel creating Playlist", false, true);
                }
                else
                {
                    message = new PlayerMessageBox("question", "Dou you really want to cancel creating the Playlist?", "Cancel creating Playlist", false, true);
                }

                message.ShowDialog();

                if (!message.ReturnMode)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
