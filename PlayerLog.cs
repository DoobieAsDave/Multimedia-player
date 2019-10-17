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
    public partial class PlayerLog : Form
    {
        public string[] Paths { get; set; }

        private bool playerMode;

        public PlayerLog(string filePath, bool playerMode)
        {
            try
            {
                InitializeComponent();

                this.playerMode = playerMode;

                if (playerMode)
                {
                    Text = "Music Log";
                    Icon = Properties.Resources.music;
                }
                else
                {
                    Text = "Video Log";
                    Icon = Properties.Resources.video;
                }

                if (CheckFilePaths(filePath))
                {
                    List<FileInfo> fileInfoList = new List<FileInfo>();

                    using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
                    {
                        string path;

                        while ((path = reader.ReadLine()) != null)
                        {
                            var file = new FileInfo();

                            file.Title = TagLib.File.Create(path).Tag.Title ??
                                System.IO.Path.GetFileNameWithoutExtension(path);
                            file.Path = path;

                            fileInfoList.Add(file);
                        }
                    }

                    if (fileInfoList.Count > 0)
                    {
                        dgvLogFiles.DataSource = fileInfoList;
                        dgvLogFiles.Columns[1].Visible = false;

                        dgvLogFiles.Rows[dgvLogFiles.Rows.Count - 1].Selected = true;
                        dgvLogFiles.CurrentCell = dgvLogFiles.Rows[dgvLogFiles.Rows.Count - 1].Cells[0];
                    }
                    else
                    {
                        btnLoadLogs.Enabled = false;
                        btnLoadSelected.Enabled = false;
                    }
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

        private bool CheckFilePaths(string filePath)
        {
            try
            {
                List<string> filePathList = System.IO.File.ReadAllLines(filePath).ToList();
                List<string> removeFilePathList = new List<string>();

                foreach(var path in filePathList)
                {
                    if (!System.IO.File.Exists(path))
                    {
                        removeFilePathList.Add(path);
                    }
                }

                if (removeFilePathList.Count > 0)
                {
                    foreach(var path in removeFilePathList)
                    {
                        filePathList.RemoveAll(p => p == path);
                    }

                    System.IO.File.WriteAllLines(filePath, filePathList);
                }

                return true;
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

        private void btnLoadLogs_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvLogFiles.Rows.Count > 0)
                {
                    Paths = dgvLogFiles.Rows
                        .OfType<DataGridViewRow>()
                        .Select(r => r.Cells[1].Value.ToString())
                        .Reverse().ToArray();

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

        private void btnLoadSelected_Click(object sender, EventArgs e)
        {
            try
            {
                if(dgvLogFiles.Rows.Count > 0)
                {
                    Paths = dgvLogFiles.SelectedRows
                        .OfType<DataGridViewRow>()
                        .Select(r => r.Cells[1].Value.ToString())
                        .Reverse().ToArray();

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

        private void dgvLogFiles_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            btnLoadSelected_Click(this, EventArgs.Empty);
        }
    }

    public class FileInfo
    {
        public string Title { get; set; }
        public string Path { get; set; }
    }
}
