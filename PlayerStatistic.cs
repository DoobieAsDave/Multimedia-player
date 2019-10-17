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
    public partial class PlayerStatistic : Form
    {
        public string[] FilePaths { get; set; }

        private List<FileStatistic> fileStatList;
        private bool playerMode;

        public PlayerStatistic(List<FileStatistic> fileStatList, bool statisticMode, bool playerMode)
        {
            try
            {
                InitializeComponent();

                this.fileStatList = fileStatList;
                this.playerMode = playerMode;

                if (playerMode)
                {
                    Icon = Properties.Resources.music;

                    if (statisticMode)
                    {
                        Text = "Music - Most played";
                    }
                    else
                    {
                        Text = "Music - Most hated";
                    }
                }
                else
                {
                    Icon = Properties.Resources.video;

                    if (statisticMode)
                    {
                        Text = "Video - Most played";
                    }
                    else
                    {
                        Text = "Video - Most hated";
                    }
                }

                dgvStatistic.DataSource = fileStatList;

                if (statisticMode)
                {
                    dgvStatistic.Columns[1].Visible = false;
                    dgvStatistic.Columns[2].HeaderText = "Played";
                }
                else
                {
                    dgvStatistic.Columns[1].Visible = false;
                    dgvStatistic.Columns[2].HeaderText = "Deleted";                    
                }

                dgvStatistic.Columns[2].FillWeight = 25;
                dgvStatistic.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
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

        private void btnAddFile_Click(object sender, EventArgs e)
        {
            try
            {
                FilePaths = dgvStatistic.SelectedRows
                    .OfType<DataGridViewRow>()
                    .Select(r => r.Cells[1].Value.ToString())
                    .ToArray();

                Close();
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
    }
}
