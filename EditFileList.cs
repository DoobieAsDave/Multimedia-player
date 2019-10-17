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
    public partial class EditFileList : Form
    {
        public bool EmptyFolder { get; set; } = false;

        private string listPath;
        private string listMode;
        private bool playerMode;        

        public EditFileList(string listPath, bool playerMode)
        {
            try
            {
                InitializeComponent();

                this.listPath = listPath;
                this.playerMode = playerMode;

                listMode = System.IO.Directory.GetParent(System.IO.Directory.GetParent(listPath).FullName).Name;

                if (playerMode)
                {
                    Icon = Properties.Resources.music;                             
                }
                else
                {
                    Icon = Properties.Resources.video;
                }

                Text = "Edit " + listMode.Remove(listMode.Length - 1, 1) + " '" + System.IO.Path.GetFileNameWithoutExtension(listPath) + "'";

                txtListName.Text = System.IO.Path.GetFileNameWithoutExtension(listPath);

                lsbListFiles.DataSource = System.IO.File
                    .ReadAllLines(listPath)
                    .Select(p => TagLib.File.Create(p).Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(p))
                    .ToList();

                lsbListFiles.ClearSelected();
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
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = true;

                if (playerMode)
                {
                    fileDialog.Filter = "Audio Files | *.mp3; *.wav";
                    fileDialog.InitialDirectory = Properties.Settings.Default.MusicPath;
                }
                else
                {
                    fileDialog.Filter = "Video Files | *.mp4; *.mpeg; *.avi";
                    fileDialog.InitialDirectory = Properties.Settings.Default.VideoPath;
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string path in fileDialog.FileNames)
                    {
                        System.IO.File.AppendAllText(listPath, path + Environment.NewLine);
                    }

                    lsbListFiles.DataSource = System.IO.File
                        .ReadAllLines(listPath)
                        .Select(p => TagLib.File.Create(p).Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(p))
                        .ToList();

                    lsbListFiles.ClearSelected();
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

        private void btnDeleteFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsbListFiles.SelectedItems.Count > 0)
                {
                    string[] remainingFileLines = System.IO.File.ReadAllLines(listPath)
                        .Where(l => !lsbListFiles.SelectedItems
                        .Contains(TagLib.File.Create(l).Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(l)))
                        .ToArray();

                    System.IO.File.WriteAllLines(listPath, remainingFileLines);

                    lsbListFiles.DataSource = System.IO.File
                        .ReadAllLines(listPath)
                        .Select(p => TagLib.File.Create(p).Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(p))
                        .ToList();

                    lsbListFiles.ClearSelected();

                    if (remainingFileLines.Length == 0)
                    {
                        PlayerMessageBox message;

                        if (playerMode)
                        {
                            message = new PlayerMessageBox("question", "The " + listMode.Remove(listMode.Length - 1, 1) + " is now empty...\n\nDo you want to delete it?", listMode.Remove(listMode.Length - 1, 1) + " is empty", false, true);
                        }
                        else
                        {
                            message = new PlayerMessageBox("question", "The " + listMode.Remove(listMode.Length - 1, 1) + " is now empty...\n\nDo you want to delete it?", listMode.Remove(listMode.Length - 1, 1) + " is empty", false, false);
                        }

                        message.ShowDialog();

                        if (message.ReturnMode)
                        {
                            System.IO.File.Delete(listPath);

                            if (System.IO.Directory.GetFiles(System.IO.Directory.GetParent(listPath).FullName).Count() == 0)
                            {
                                EmptyFolder = true;
                            }

                            Close();
                        }
                    }
                }
                else
                {
                    PlayerMessageBox message;

                    if (playerMode)
                    {
                        message = new PlayerMessageBox("information", "Please select a File to delete from " + listMode.Remove(listMode.Length - 1, 1) + " '" + System.IO.Path.GetFileNameWithoutExtension(listPath) + "'...", "No File selected", true, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("information", "Please select a File to delete from " + listMode.Remove(listMode.Length - 1, 1) + " '" + System.IO.Path.GetFileNameWithoutExtension(listPath) + "'...", "No File selected", true, false);
                    }

                    message.ShowDialog();
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

        private void btnUpdateName_Click(object sender, EventArgs e)
        {
            try
            {
                string newListPath = System.IO.Directory.GetParent(listPath).FullName + @"\" + txtListName.Text + ".txt";

                System.IO.File.Move(listPath, newListPath);                
                listPath = newListPath;
                Text = "Edit " + listMode.Remove(listMode.Length - 1, 1) + " '" + System.IO.Path.GetFileNameWithoutExtension(listPath) + "'";
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

        private void EditFileList_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (txtListName.Text != System.IO.Path.GetFileNameWithoutExtension(listPath))
                {
                    System.IO.File.Move(listPath, System.IO.Directory.GetParent(listPath).FullName + @"\" + txtListName.Text + ".txt");
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
    }
}
