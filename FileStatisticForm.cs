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
    public partial class FileStatisticForm : Form
    {
        private string filePath;

        private bool playerMode;

        private Dictionary<string, int> playlistOccurrences;

        public FileStatisticForm(string filePath, int occurrencesCount, Dictionary<string, int> playlistOccurrences, bool playerMode)
        {
            try
            {
                InitializeComponent();

                this.filePath = filePath;
                this.playerMode = playerMode;
                this.playlistOccurrences = playlistOccurrences;

                if (playerMode)
                {
                    Icon = Properties.Resources.music;
                }
                else
                {
                    Icon = Properties.Resources.video;
                }

                Text = TagLib.File.Create(filePath).Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(filePath) + " Statistic";
                txtXPlayed.Text = occurrencesCount.ToString();

                dgvXInPlaylists.DataSource = playlistOccurrences.ToList();
                dgvXInPlaylists.Columns[0].HeaderText = "Playlist";
                dgvXInPlaylists.Columns[1].HeaderText = "Occurred";

                this.Focus();
            }
            catch (ArgumentNullException nullEx)
            {
                PlayerMessageBox message;

                if (playerMode)
                {
                    message = new PlayerMessageBox("error", nullEx.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", nullEx.Message, "An error occurred", true, false);
                }

                message.ShowDialog();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (playerMode)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }

                message.ShowDialog();
            }
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            try
            {
                PlayerMessageBox alert;

                if (playerMode)
                {
                    alert = new PlayerMessageBox("question", "Do you really want to remove all occurrencies in the Log or Playlists?", "Really want to restore", false, true);
                }
                else
                {
                    alert = new PlayerMessageBox("question", "Do you really want to remove all occurrencies in the Log or Playlists?", "Really want to restore", false, false);
                }

                alert.ShowDialog();

                if (alert.ReturnMode)
                {
                    List<string> filePaths;

                    if (playerMode)
                    {
                        filePaths = System.IO.File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Log\Music\Log.txt").ToList();
                        filePaths.RemoveAll(p => p == filePath);

                        System.IO.File.WriteAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Log\Music\Log.txt", filePaths);

                        foreach(var pair in playlistOccurrences)
                        {
                            var playlistPaths = System.IO.File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Playlists\Music\" + pair.Key + @".txt").ToList();
                            playlistPaths.RemoveAll(p => p == filePath);

                            System.IO.File.WriteAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Playlists\Music\" + pair.Key + @".txt", playlistPaths);
                        }
                    }
                    else
                    {
                        filePaths = System.IO.File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Log\Video\Log.txt").ToList();
                        filePaths.RemoveAll(q => q == filePath);

                        System.IO.File.WriteAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Log\Video\Log.txt", filePaths);

                        foreach (var pair in playlistOccurrences)
                        {
                            var playlistPaths = System.IO.File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Playlists\Video\" + pair.Key + @".txt").ToList();
                            playlistPaths.RemoveAll(p => p == filePath);

                            System.IO.File.WriteAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Playlists\Video\" + pair.Key + @".txt", playlistPaths);
                        }
                    }

                    PlayerMessageBox success;

                    if (playerMode)
                    {
                        success = new PlayerMessageBox("success", "The File has been removed from the Log and all the Playlists", "File removed from Log and Playlists", true, true);
                    }
                    else
                    {
                        success = new PlayerMessageBox("success", "The File has been removed from the Log and all the Playlists", "File removed from Log and Playlists", true, false);
                    }

                    success.ShowDialog();

                    Close();
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (playerMode)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }

                message.ShowDialog();
            }
        }

        private void txtXPlayed_Enter(object sender, EventArgs e)
        {
            lblXPlayed.Focus();
        }
    }
}
