using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using DoobieAsPlayer.Properties;
using System.IO;
using NAudio.CoreAudioApi;
using System.Security.Principal;

namespace DoobieAsPlayer
{
    public partial class Player : Form
    {
        private string resourcePath = AppDomain.CurrentDomain.BaseDirectory;

        private List<PlayerFile> playerFileList;

        private int currentFileListRow = 0;

        private bool loopActivated, shuffleActivated, playingTrackDeleted, previousCoverView, audioDeviceDetected = false;
        private bool buttonsActivated = true;

        private string shufflePath;
        private List<string> originalPathList;

        private Timer playTimer, secondTimer, timelineTimer;
        private System.Timers.Timer fastBackTimer, fastForwardTimer;

        private double? currentPositionPlaying = null;

        private string currentPathPlaying;

        private int secondsWaited = 0;

        private string autoBackupFileName = null;

        private string currentTime, maxTime;

        private bool enuffPermission = false;

        public Player()
        {
            InitializeComponent();            
        }

        private void Player_Load(object sender, EventArgs e)
        {
            try
            {
                if (!CheckPermissions())
                {
                    PlayerMessageBox message = new PlayerMessageBox("information", "Please run the programm as a Administrator!", "No Administrator Permission", true, true);
                    message.ShowDialog();
                    Close();
                }
                else
                {
                    enuffPermission = true;

                    Icon = Resources.music;
                    Text = "DoobieAsPlayer - Music";

                    wmpPlayer.uiMode = "none";
                    wmpPlayer.settings.volume = tkbVolume.Value;

                    Show();

                    audioDeviceDetected = CheckAudioOutputs();

                    InitializeTimer();

                    if (!CheckSettings())
                    {
                        SetSettings();
                    }

                    CheckPlaylists();
                    CheckBackups(true);
                    CheckLogs();
                    CheckDeleted();

                    if (PreservePlayerlistToolStripMenuItem.Checked)
                    {
                        if (CheckPreserved())
                        {
                            LoadPreserved();
                        }
                    }
                }                
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private bool CheckPermissions()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }

                message.ShowDialog();

                return false;
            }
        }

        private bool CheckAudioOutputs()
        {
            try
            {
                var audioEnumerator = new MMDeviceEnumerator();

                MMDeviceCollection deviceCollection = audioEnumerator.EnumerateAudioEndPoints(
                        DataFlow.Render,
                        DeviceState.Unplugged | DeviceState.Active
                    );

                bool audioDeviceDetected = false;

                foreach(var device in deviceCollection)
                {
                    if(device.State == DeviceState.Active)
                    {
                        audioDeviceDetected = true;
                        break;
                    }

                    audioDeviceDetected = false;
                }

                if (audioDeviceDetected)
                {
                    return true;
                }
                else
                {
                    PlayerMessageBox message;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        message = new PlayerMessageBox("information", "No Output Device is connected!\n\nPlease connect one and click 'OK' to proceed...", "No Output Device found", true, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("information", "No Output Device is connected!\n\nPlease connect one and click 'OK' to proceed...", "No Output Device found", true, false);
                    }

                    message.ShowDialog();

                    while (!CheckAudioOutputs())
                    { 
                    }

                    return true;
                }                
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }

                message.ShowDialog();

                return false;
            }
        }

        private void InitializeTimer()
        {
            try
            {
                playTimer = new Timer();
                playTimer.Interval = 200;
                playTimer.Tick += new EventHandler(playTimer_Tick);

                secondTimer = new Timer();
                secondTimer.Interval = 1000;
                secondTimer.Tick += new EventHandler(secondCounter_Tick);

                timelineTimer = new Timer();
                timelineTimer.Interval = 1000;
                timelineTimer.Tick += new EventHandler(timelineTimer_Tick);

                fastBackTimer = new System.Timers.Timer();
                fastBackTimer.Interval = 500;
                fastBackTimer.Enabled = false;
                fastBackTimer.AutoReset = true;
                fastBackTimer.Elapsed += new System.Timers.ElapsedEventHandler(fastBackTimer_Elapsed);

                fastForwardTimer = new System.Timers.Timer();
                fastForwardTimer.Interval = 500;
                fastForwardTimer.Enabled = false;
                fastForwardTimer.AutoReset = true;
                fastForwardTimer.Elapsed += new System.Timers.ElapsedEventHandler(fastForwardTimer_Elapsed);
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        
        private bool CheckSettings()
        {
            try
            {
                string musicPath = Settings.Default.MusicPath;
                string videoPath = Settings.Default.VideoPath;
                string askForBackup = Settings.Default.AskForBackup;
                string autoBackup = Settings.Default.AutoBackup;
                string minimized = Settings.Default.Minimized;
                string preserveList = Settings.Default.PreserveList;
                string playerMode = Settings.Default.PlayerMode;

                if (musicPath == "" || videoPath == "" || askForBackup == "" || autoBackup == "" || minimized == "" || preserveList == "" || playerMode == "")
                {
                    return false;
                }
                else
                {
                    if (Settings.Default.AskForBackup == "true")
                    {
                        AskToLoadBackupsToolStripMenuItem.Checked = true;
                    }
                    else
                    {
                        AskToLoadBackupsToolStripMenuItem.Checked = false;
                    }

                    if (Settings.Default.AutoBackup == "true")
                    {
                        AutoCreateBackupsToolStripMenuItem.Checked = true;
                    }
                    else
                    {
                        AutoCreateBackupsToolStripMenuItem.Checked = false;
                    }

                    if (Settings.Default.Minimized == "true")
                    {
                        MinimizedToolStripMenuItem.Checked = true;

                        Size = new Size(700, 595);
                    }
                    else
                    {
                        MinimizedToolStripMenuItem.Checked = false;

                        Size = new Size(1250, 669);
                    }

                    if (Settings.Default.PreserveList == "true")
                    {
                        PreservePlayerlistToolStripMenuItem.Checked = true;
                    }
                    else
                    {
                        PreservePlayerlistToolStripMenuItem.Checked = false;
                    }

                    if(Settings.Default.PlayerMode == "music")
                    {
                        MusicToolStripMenuItem_Click(this, EventArgs.Empty);                        
                    }
                    else
                    {
                        VideoToolStripMenuItem_Click(this, EventArgs.Empty);
                        HideTimelineToolStripMenuItem_Click(this, EventArgs.Empty);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }

                message.ShowDialog();

                return false;
            }
        }
        private void SetSettings()
        {
            try
            {
                var browseFolder = new FolderBrowserDialog();
                browseFolder.ShowNewFolderButton = false;
                browseFolder.Description = "Choose your Music Directory";
                browseFolder.RootFolder = Environment.SpecialFolder.Desktop;

                if (browseFolder.ShowDialog() == DialogResult.OK)
                {
                    Settings.Default.MusicPath = browseFolder.SelectedPath;
                }
                else
                {
                    Settings.Default.MusicPath = @"C:\Users\" + Environment.UserName + @"\Music";
                }

                browseFolder.Description = "Choose your Video Directory";

                if (browseFolder.ShowDialog() == DialogResult.OK)
                {
                    Settings.Default.VideoPath = browseFolder.SelectedPath;
                }
                else
                {
                    Settings.Default.VideoPath = @"C:\Users\" + Environment.UserName + @"\Videos";
                }

                PlayerMessageBox messageAsk;

                if (MusicToolStripMenuItem.Checked)
                {
                    messageAsk = new PlayerMessageBox("question", "Do you want to be asked to load Backups?", "Ask to load Backups", false, true);
                }
                else
                {
                    messageAsk = new PlayerMessageBox("question", "Do you want to be asked to load Backups?", "Ask to load Backups", false, false);
                }

                messageAsk.ShowDialog();

                if (messageAsk.ReturnMode)
                {
                    Settings.Default.AskForBackup = "true";
                    AskToLoadBackupsToolStripMenuItem.Checked = true;
                }
                else
                {
                    Settings.Default.AskForBackup = "false";
                    AskToLoadBackupsToolStripMenuItem.Checked = false;
                }

                PlayerMessageBox messageCreate;

                if (MusicToolStripMenuItem.Checked)
                {
                    messageCreate = new PlayerMessageBox("question", "Should the Player create a Backup when you close it?", "Auto create Backup", false, true);
                }
                else
                {
                    messageCreate = new PlayerMessageBox("question", "Should the Player create a Backup when you close it?", "Auto create Backup", false, false);
                }

                messageCreate.ShowDialog();

                if (messageCreate.ReturnMode)
                {
                    Settings.Default.AutoBackup = "true";
                    AutoCreateBackupsToolStripMenuItem.Checked = true;
                }
                else
                {
                    Settings.Default.AutoBackup = "false";
                    AutoCreateBackupsToolStripMenuItem.Checked = false;
                }

                PlayerMessageBox messageMinimized;

                if (MusicToolStripMenuItem.Checked)
                {
                    messageMinimized = new PlayerMessageBox("question", "Should the Player start minimized", "Startup minimized", false, true);
                }
                else
                {
                    messageMinimized = new PlayerMessageBox("question", "Should the Player start minimized", "Startup minimized", false, false);
                }

                messageMinimized.ShowDialog();

                if (messageMinimized.ReturnMode)
                {
                    Settings.Default.Minimized = "true";
                    MinimizedToolStripMenuItem.Checked = true;

                    Size = new Size(700, 595);
                }
                else
                {
                    Settings.Default.Minimized = "false";
                    MinimizedToolStripMenuItem.Checked = false;

                    Size = new Size(1250, 669);
                }

                PlayerMessageBox messagePreserve;

                if (MusicToolStripMenuItem.Checked)
                {
                    messagePreserve = new PlayerMessageBox("question", "Should the Player always load the last opened List?", "Load last opened List", false, true);
                }
                else
                {
                    messagePreserve = new PlayerMessageBox("question", "Should the Player always load the last opened List?", "Load last opened List", false, false);
                }

                messagePreserve.ShowDialog();
                                
                if (messagePreserve.ReturnMode)
                {
                    Settings.Default.PreserveList = "true";
                    PreservePlayerlistToolStripMenuItem.Checked = true;
                }
                else
                {
                    Settings.Default.PreserveList = "false";
                    PreservePlayerlistToolStripMenuItem.Checked = false;
                }

                if (MusicToolStripMenuItem.Checked)
                {
                    Settings.Default.PlayerMode = "music";
                }
                else
                {
                    Settings.Default.PlayerMode = "video";
                }

                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private bool CheckPlaylists()
        {
            try
            {
                if (!Directory.Exists(resourcePath + @"Playlists"))
                {
                    Directory.CreateDirectory(resourcePath + @"Playlists");
                }
                else
                {
                    if (!Directory.Exists(resourcePath + @"Playlists\Music"))
                    {
                        Directory.CreateDirectory(resourcePath + @"Playlists\Music");
                    }

                    if (!Directory.Exists(resourcePath + @"Playlists\Video"))
                    {
                        Directory.CreateDirectory(resourcePath + @"Playlists\Video");
                    }
                }

                int musicPlaylistCount = 0;
                int videoPlaylistCount = 0;

                if (MusicToolStripMenuItem.Checked)
                {
                    musicPlaylistCount = Directory.GetFiles(resourcePath + @"Playlists\Music\", "*.txt", SearchOption.TopDirectoryOnly).Length;
                }
                else
                {
                    videoPlaylistCount = Directory.GetFiles(resourcePath + @"Playlists\Video\", "*.txt", SearchOption.TopDirectoryOnly).Length;
                }

                if (!CheckFileCount())
                {
                    CreatePlaylistToolStripMenuItem.Enabled = false;
                }
                else
                {
                    CreatePlaylistToolStripMenuItem.Enabled = true;
                }

                if (musicPlaylistCount == 0 && videoPlaylistCount == 0)
                {
                    OpenPlaylistToolStripMenuItem.Enabled = false;
                    EditPlaylistToolStripMenuItem.Enabled = false;
                    DeletePlaylistToolStripMenuItem.Enabled = false;

                    return false;
                }
                else
                {
                    if (MusicToolStripMenuItem.Checked)
                    {
                        if (musicPlaylistCount == 0)
                        {
                            OpenPlaylistToolStripMenuItem.Enabled = false;
                            EditPlaylistToolStripMenuItem.Enabled = false;
                            DeletePlaylistToolStripMenuItem.Enabled = false;

                            return false;
                        }
                        else
                        {
                            OpenPlaylistToolStripMenuItem.Enabled = true;
                            EditPlaylistToolStripMenuItem.Enabled = true;
                            DeletePlaylistToolStripMenuItem.Enabled = true;

                            return true;
                        }
                    }
                    else
                    {
                        if (videoPlaylistCount == 0)
                        {
                            OpenPlaylistToolStripMenuItem.Enabled = false;
                            EditPlaylistToolStripMenuItem.Enabled = false;
                            DeletePlaylistToolStripMenuItem.Enabled = false;

                            return false;
                        }
                        else
                        {
                            OpenPlaylistToolStripMenuItem.Enabled = true;
                            EditPlaylistToolStripMenuItem.Enabled = true;
                            DeletePlaylistToolStripMenuItem.Enabled = true;

                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }

                message.ShowDialog();

                return false;
            }
        }

        private bool CheckBackups(bool checking)
        {
            try
            {
                bool returnMode = false;

                if (!Directory.Exists(resourcePath + @"Backups"))
                {
                    Directory.CreateDirectory(resourcePath + @"Backups");
                }
                else
                {
                    if (!Directory.Exists(resourcePath + @"Backups\Music"))
                    {
                        Directory.CreateDirectory(resourcePath + @"Backups\Music");
                    }

                    if (!Directory.Exists(resourcePath + @"Backups\Video"))
                    {
                        Directory.CreateDirectory(resourcePath + @"Backups\Video");
                    }
                }

                int musicBackupCount = 0;
                int videoBackupCount = 0;

                if (MusicToolStripMenuItem.Checked)
                {
                    musicBackupCount = Directory.GetFiles(resourcePath + @"Backups\Music\", "*.txt", SearchOption.TopDirectoryOnly).Length;
                }
                else
                {
                    videoBackupCount = Directory.GetFiles(resourcePath + @"Backups\Video\", "*.txt", SearchOption.TopDirectoryOnly).Length;
                }

                if (!CheckFileCount())
                {
                    CreateBackupToolStripMenuItem.Enabled = false;
                }
                else
                {
                    CreateBackupToolStripMenuItem.Enabled = true;
                }

                if (musicBackupCount == 0 && videoBackupCount == 0)
                {
                    OpenBackupToolStripMenuItem.Enabled = false;
                    EditBackupToolStripMenuItem.Enabled = false;
                    DeleteBackupToolStripMenuItem.Enabled = false;

                    returnMode = false;
                }
                else
                {
                    if (MusicToolStripMenuItem.Checked)
                    {
                        if (musicBackupCount == 0)
                        {
                            OpenBackupToolStripMenuItem.Enabled = false;
                            EditBackupToolStripMenuItem.Enabled = false;
                            DeleteBackupToolStripMenuItem.Enabled = false;

                            returnMode = false;
                        }
                        else
                        {
                            OpenBackupToolStripMenuItem.Enabled = true;
                            EditBackupToolStripMenuItem.Enabled = true;
                            DeleteBackupToolStripMenuItem.Enabled = true;

                            returnMode = true;
                        }
                    }
                    else
                    {
                        if (musicBackupCount == 0)
                        {
                            OpenBackupToolStripMenuItem.Enabled = false;
                            EditBackupToolStripMenuItem.Enabled = false;
                            DeleteBackupToolStripMenuItem.Enabled = false;

                            returnMode = false;
                        }
                        else
                        {
                            OpenBackupToolStripMenuItem.Enabled = true;
                            EditBackupToolStripMenuItem.Enabled = true;
                            DeleteBackupToolStripMenuItem.Enabled = true;

                            returnMode = true;
                        }
                    }
                }

                if (checking)
                {
                    // Check if User wants to be asked
                    if (Settings.Default.AskForBackup == "true")
                    {
                        PlayerMessageBox messageAsk;

                        if (MusicToolStripMenuItem.Checked)
                        {
                            messageAsk = new PlayerMessageBox("question", "You wanted to be asked to load a Backup at the Start of the Player!\n\nReady to choose a Backup?", "Open Backup", false, true);
                        }
                        else
                        {
                            messageAsk = new PlayerMessageBox("question", "You wanted to be asked to load a Backup at the Start of the Player!\n\nReady to choose a Backup?", "Open Backup", false, false);
                        }

                        messageAsk.ShowDialog();

                        if (messageAsk.ReturnMode)
                        {
                            LoadBackup();
                        }
                        else
                        {
                            AskToLoadBackupsToolStripMenuItem.Checked = false;

                            Settings.Default.AskForBackup = "false";
                            Settings.Default.Save();
                        }
                    }
                }

                if (returnMode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }

                message.ShowDialog();

                return false;
            }
        }
        private void LoadBackup()
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = false;
                fileDialog.Filter = "Text Files | *.txt";

                if (MusicToolStripMenuItem.Checked)
                {
                    fileDialog.Title = "Choose a Music Backup";
                    fileDialog.InitialDirectory = resourcePath + @"Backups\Music";
                }
                else
                {
                    fileDialog.Title = "Choose a Video Backup";
                    fileDialog.InitialDirectory = resourcePath + @"Backups\Video";
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClearFiles();

                    LoadProperties(File.ReadAllLines(fileDialog.FileName), true);
                    SetFilesIntoList();

                    playTimer.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void CheckLogs()
        {
            try
            {
                if (!Directory.Exists(resourcePath + @"Log"))
                {
                    Directory.CreateDirectory(resourcePath + @"Log");
                }
                else
                {
                    if (!Directory.Exists(resourcePath + @"Log\Music"))
                    {
                        Directory.CreateDirectory(resourcePath + @"Log\Music");
                    }
                    else
                    {
                        if (!File.Exists(resourcePath + @"Log\Music\Log.txt"))
                        {
                            File.Create(resourcePath + @"Log\Music\Log.txt");

                            PlayerMessageBox message;

                            if (MusicToolStripMenuItem.Checked)
                            {
                                message = new PlayerMessageBox("information", "The 'Log.txt' File could not be found!\n\nNew Log File has been created...", "Log File could not be found", true, true);
                            }
                            else
                            {
                                message = new PlayerMessageBox("information", "The 'Log.txt' File could not be found!\n\nNew Log File has been created...", "Log File could not be found", true, false);
                            }

                            message.ShowDialog();
                        }
                    }

                    if (!Directory.Exists(resourcePath + @"Log\Video"))
                    {
                        Directory.CreateDirectory(resourcePath + @"Log\Video");
                    }
                    else
                    {
                        if (!File.Exists(resourcePath + @"Log\Video\Log.txt"))
                        {
                            File.Create(resourcePath + @"Log\Video\Log.txt");

                            PlayerMessageBox message;

                            if (MusicToolStripMenuItem.Checked)
                            {
                                message = new PlayerMessageBox("information", "The 'Log.txt' File could not be found!\n\nThe Log File has been renewed...", "Log File could not be found", true, true);
                            }
                            else
                            {
                                message = new PlayerMessageBox("information", "The 'Log.txt' File could not be found!\n\nNew Log File has been created...", "Log File could not be found", true, false);
                            }

                            message.ShowDialog();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void CheckDeleted()
        {
            try
            {
                if (!Directory.Exists(resourcePath + @"Deleted"))
                {
                    Directory.CreateDirectory(resourcePath + @"Deleted");
                }
                else
                {
                    if (!Directory.Exists(resourcePath + @"Deleted\Music"))
                    {
                        Directory.CreateDirectory(resourcePath + @"Deleted\Music");
                    }
                    else
                    {
                        if (!File.Exists(resourcePath + @"Deleted\Music\Deleted.txt"))
                        {
                            File.Create(resourcePath + @"Deleted\Music\Deleted.txt");

                            PlayerMessageBox message;

                            if (MusicToolStripMenuItem.Checked)
                            {
                                message = new PlayerMessageBox("information", "The 'Deleted.txt' File could not be found!\n\nThe Deleted File has been renewed...", "Deleted File could not be found", true, true);
                            }
                            else
                            {
                                message = new PlayerMessageBox("information", "The 'Deleted.txt' File could not be found!\n\nThe Deleted File has been renewed...", "Deleted File could not be found", true, false);
                            }

                            message.ShowDialog();
                        }
                    }

                    if (!Directory.Exists(resourcePath + @"Deleted\Video"))
                    {
                        Directory.CreateDirectory(resourcePath + @"Deleted\Video");
                    }
                    else
                    {
                        if (!File.Exists(resourcePath + @"Deleted\Video\Deleted.txt"))
                        {
                            File.Create(resourcePath + @"Deleted\Video\Deleted.txt");

                            PlayerMessageBox message;

                            if (MusicToolStripMenuItem.Checked)
                            {
                                message = new PlayerMessageBox("information", "The 'Deleted.txt' File could not be found!\n\nThe File has been renewed...", "Deleted File could not be found", true, true);
                            }
                            else
                            {
                                message = new PlayerMessageBox("information", "The 'Deleted.txt' File could not be found!\n\nThe File has been renewed...", "Deleted File could not be found", true, false);
                            }

                            message.ShowDialog();
                        }
                    }
                }                
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }
            }
        }

        private bool CheckPreserved()
        {
            try
            {
                if (!Directory.Exists(resourcePath + @"Preserved"))
                {
                    Directory.CreateDirectory(resourcePath + @"Preserved");

                    return false;
                }
                else
                {
                    if (!File.Exists(resourcePath + @"Preserved\Preserved.txt"))
                    {
                        File.Create(resourcePath + @"Preserved\Preserved.txt");

                        PlayerMessageBox message;

                        if (MusicToolStripMenuItem.Checked)
                        {
                            message = new PlayerMessageBox("information", "The 'Preserved.txt' File could not be found!\n\nThe File has been renewed...", "Preserved File could not be found", true, true);
                        }
                        else
                        {
                            message = new PlayerMessageBox("information", "The 'Preserved.txt' File could not be found!\n\nThe File has been renewed...", "Preserved File could not be found", true, false);
                        }

                        message.ShowDialog();

                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }

                message.ShowDialog();

                return false;
            }
        }
        private void LoadPreserved()
        {
            try
            {
                List<string> preservedList = File.ReadAllLines(resourcePath + @"Preserved\Preserved.txt").ToList();

                if(preservedList[preservedList.Count - 1] == "Music")
                {                    
                    MusicToolStripMenuItem_Click(this, EventArgs.Empty);                    
                }
                else
                {                    
                    VideoToolStripMenuItem_Click(this, EventArgs.Empty);
                }

                preservedList.RemoveAt(preservedList.Count - 1);

                LoadProperties(preservedList.ToArray(), true);
                SetFilesIntoList();

                playTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        // ------------------------------------

        private void ClearFiles()
        {
            try
            {
                if (wmpPlayer.playState == WMPLib.WMPPlayState.wmppsPlaying)
                {
                    wmpPlayer.Ctlcontrols.stop();
                }

                timelineTimer.Enabled = false;

                tkbTimeline.Value = 0;

                currentTime = "00:00";
                maxTime = "00:00";
                lblTimeCounter.Text = currentTime + " / " + maxTime;

                if (pcbCover.Visible)
                {
                    pcbCover.Visible = false;
                    ChangeControlLocation();
                }

                wmpPlayer.currentPlaylist.clear();

                playerFileList = null;
                dgvPlayerList.DataSource = null;
                EnDisablePlayControls(false, false, false, false, false, false, false, false, false, false, false, false);
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void LoadProperties(string[] filePaths, bool newList)
        {
            try
            {
                if (playerFileList == null || newList)
                {
                    playerFileList = new List<PlayerFile>();
                }

                foreach (string path in filePaths)
                {
                    var file = new PlayerFile();

                    TagLib.File fileInformation = TagLib.File.Create(path);

                    if (Path.GetExtension(path) != ".wav")
                    {
                        file.Title = fileInformation.Tag.Title ?? Path.GetFileNameWithoutExtension(path);
                    }
                    else
                    {
                        file.Title = Path.GetFileNameWithoutExtension(path) ?? " -                                    ";
                    }

                    file.Album = fileInformation.Tag.Album ?? " -                                    ";
                    file.Artist = fileInformation.Tag.FirstAlbumArtist ?? " -                                    ";
                    file.Seconds = (int)fileInformation.Properties.Duration.TotalSeconds;
                    file.Path = path;

                    var fileSeconds = TimeSpan.FromSeconds(file.Seconds);

                    if (fileSeconds.TotalSeconds >= 3600)
                    {
                        file.Duration = fileSeconds.ToString(@"hh\:mm\:ss");
                    }
                    else
                    {
                        file.Duration = fileSeconds.ToString(@"mm\:ss");
                    }

                    playerFileList.Add(file);
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void SetFilesIntoList()
        {
            try
            {
                dgvPlayerList.DataSource = playerFileList;

                if (dgvPlayerList.Rows.Count > 0)
                {
                    dgvPlayerList.Columns[4].Visible = false;
                    dgvPlayerList.Columns[5].Visible = false;

                    dgvPlayerList.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgvPlayerList.Columns[3].DefaultCellStyle.Font = new Font("Trebuchet MS", 8.25f, FontStyle.Bold);

                    CreatePlaylistToolStripMenuItem.Enabled = true;
                    CreateBackupToolStripMenuItem.Enabled = true;
                    btnDeleteFile.Enabled = true;
                }
                else
                {
                    CreatePlaylistToolStripMenuItem.Enabled = false;
                    CreateBackupToolStripMenuItem.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        // ------------------------------------
        
        private void btnOpenFiles_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = true;

                if (MusicToolStripMenuItem.Checked)
                {
                    fileDialog.Filter = "Audio Files | *.mp3; *.wav";
                    fileDialog.InitialDirectory = Settings.Default.MusicPath;
                }
                else
                {
                    fileDialog.Filter = "Video Files | *.mp4; *.mpeg; *.avi";
                    fileDialog.InitialDirectory = Settings.Default.VideoPath;
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClearFiles();

                    LoadProperties(fileDialog.FileNames, true);
                    SetFilesIntoList();

                    if (CheckFileCount())
                    {
                        currentFileListRow = 0;

                        playTimer.Enabled = true;
                    }
                }
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnAddFiles_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = true;

                if (MusicToolStripMenuItem.Checked)
                {
                    fileDialog.Filter = "Audio Files | *.mp3; *.wav";
                    fileDialog.InitialDirectory = Settings.Default.MusicPath;
                }
                else
                {
                    fileDialog.Filter = "Video Files | *.mp4; *.mpeg; *.avi";
                    fileDialog.InitialDirectory = Settings.Default.VideoPath;
                }

                currentPathPlaying = dgvPlayerList.CurrentRow.Cells[5].Value.ToString();
                currentPositionPlaying = wmpPlayer.Ctlcontrols.currentPosition;

                secondsWaited = 0;

                secondTimer.Start();

                var result = fileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    secondTimer.Stop();

                    currentPositionPlaying += (double)secondsWaited;

                    dgvPlayerList.DataSource = null;

                    LoadProperties(fileDialog.FileNames, false);
                    SetFilesIntoList();

                    currentFileListRow = dgvPlayerList.Rows.OfType<DataGridViewRow>().Where(v => v.Cells[5].Value.Equals(currentPathPlaying)).First().Index;

                    dgvPlayerList.ClearSelection();
                    dgvPlayerList.CurrentCell = dgvPlayerList.Rows[currentFileListRow].Cells[0];
                    dgvPlayerList.Rows[currentFileListRow].Selected = true;

                    playTimer.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void btnFileBack_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentFileListRow - 1 >= 0)
                {
                    currentFileListRow--;
                }
                else
                {
                    currentFileListRow = dgvPlayerList.Rows.Count - 1;
                }

                playTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnFileForward_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentFileListRow + 1 <= dgvPlayerList.Rows.Count - 1)
                {
                    currentFileListRow++;
                }
                else
                {
                    currentFileListRow = 0;
                }

                playTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void btnFastBack_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                btnFastBack.BackColor = Color.PaleTurquoise;

                fastBackTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnFastBack_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                btnFastBack.BackColor = Color.Turquoise;

                fastBackTimer.Enabled = false;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void fastBackTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (wmpPlayer.Ctlcontrols.currentPosition - 5 >= 0)
                {
                    wmpPlayer.Ctlcontrols.currentPosition -= 5;
                    tkbTimeline.Value -= 5;

                    var timeSpan = TimeSpan.FromSeconds(tkbTimeline.Value);

                    if (timeSpan.TotalSeconds > 3600)
                    {
                        currentTime = string.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    }
                    else
                    {
                        currentTime = string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
                    }

                    lblTimeCounter.Text = currentTime + " / " + maxTime;
                }
                else
                {
                    fastBackTimer.Enabled = false;

                    wmpPlayer.Ctlcontrols.currentPosition = 0;
                    tkbTimeline.Value = 0;

                    var timeSpan = TimeSpan.FromSeconds(tkbTimeline.Value);

                    if (timeSpan.TotalSeconds > 3600)
                    {
                        currentTime = string.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    }
                    else
                    {
                        currentTime = string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
                    }

                    lblTimeCounter.Text = currentTime + " / " + maxTime;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnFastForward_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                btnFastForward.BackColor = Color.PaleGreen;

                fastForwardTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnFastForward_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                btnFastForward.BackColor = Color.MediumSpringGreen;

                fastForwardTimer.Enabled = false;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void fastForwardTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (wmpPlayer.Ctlcontrols.currentPosition + 5 <= tkbTimeline.Maximum)
                {
                    wmpPlayer.Ctlcontrols.currentPosition += 5;
                    tkbTimeline.Value += 5;

                    var timeSpan = TimeSpan.FromSeconds(tkbTimeline.Value);

                    if (timeSpan.TotalSeconds > 3600)
                    {
                        currentTime = string.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    }
                    else
                    {
                        currentTime = string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
                    }

                    lblTimeCounter.Text = currentTime + " / " + maxTime;
                }
                else
                {
                    fastForwardTimer.Enabled = true;

                    wmpPlayer.Ctlcontrols.currentPosition = 0;
                    tkbTimeline.Value = 0;

                    var timeSpan = TimeSpan.FromSeconds(tkbTimeline.Value);

                    if (timeSpan.TotalSeconds > 3600)
                    {
                        currentTime = string.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    }
                    else
                    {
                        currentTime = string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
                    }

                    lblTimeCounter.Text = currentTime + " / " + maxTime;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void btnPlayFile_Click(object sender, EventArgs e)
        {
            try
            {
                wmpPlayer.Ctlcontrols.play();
                timelineTimer.Start();

                btnPauseFile.Focus();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnPauseFile_Click(object sender, EventArgs e)
        {
            try
            {
                wmpPlayer.Ctlcontrols.pause();
                timelineTimer.Stop();

                btnPlayFile.Focus();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnStopFile_Click(object sender, EventArgs e)
        {
            try
            {
                wmpPlayer.Ctlcontrols.stop();
                timelineTimer.Enabled = false;
                tkbTimeline.Value = 0;

                var timeSpan = TimeSpan.FromSeconds(tkbTimeline.Value);

                if (tkbTimeline.Maximum >= 3600)
                {
                    currentTime = string.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                }
                else
                {
                    currentTime = string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
                }

                lblTimeCounter.Text = currentTime + " / " + maxTime;

                if (btnAddFiles.Enabled)
                {
                    btnAddFiles.Focus();
                }
                else
                {
                    btnOpenFiles.Focus();
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnShuffleFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (shuffleActivated) // Disable Shuffle
                {
                    shuffleActivated = false;

                    shufflePath = dgvPlayerList.Rows[currentFileListRow].Cells[5].Value.ToString();

                    currentPositionPlaying = wmpPlayer.Ctlcontrols.currentPosition;

                    LoadProperties(originalPathList.ToArray(), true);
                    SetFilesIntoList();

                    playTimer.Enabled = true;

                    currentFileListRow = dgvPlayerList.Rows.OfType<DataGridViewRow>().Where(v => v.Cells[5].Value.Equals(shufflePath)).First().Index;

                    dgvPlayerList.ClearSelection();
                    dgvPlayerList.CurrentCell = dgvPlayerList.Rows[currentFileListRow].Cells[0];
                    dgvPlayerList.Rows[currentFileListRow].Selected = true;

                    foreach (DataGridViewRow row in dgvPlayerList.Rows)
                    {
                        row.DefaultCellStyle.BackColor = SystemColors.ButtonFace;
                    }

                    dgvPlayerList.Rows[currentFileListRow].DefaultCellStyle.BackColor = Color.LightGreen;

                    btnShuffleFile.Text = "Shuffle";
                    btnShuffleFile.BackColor = SystemColors.ButtonFace;

                    ShuffleFilesToolStripMenuItem.Text = "Shuffle";
                }
                else                  // Enable Shuffle
                {
                    shuffleActivated = true;

                    shufflePath = dgvPlayerList.Rows[currentFileListRow].Cells[5].Value.ToString();

                    originalPathList = new List<string>(dgvPlayerList.Rows.OfType<DataGridViewRow>()
                        .Select(c => c.Cells[5].Value
                        .ToString()));

                    Random rdm = new Random();

                    string[] shuffledPathsArray = originalPathList.OrderBy(p => rdm.Next()).ToArray();

                    currentPositionPlaying = wmpPlayer.Ctlcontrols.currentPosition;

                    LoadProperties(shuffledPathsArray, true);
                    SetFilesIntoList();

                    playTimer.Enabled = true;

                    currentFileListRow = dgvPlayerList.Rows.OfType<DataGridViewRow>().Where(v => v.Cells[5].Value.Equals(shufflePath)).First().Index;

                    dgvPlayerList.ClearSelection();
                    dgvPlayerList.CurrentCell = dgvPlayerList.Rows[currentFileListRow].Cells[0];
                    dgvPlayerList.Rows[currentFileListRow].Selected = true;

                    foreach (DataGridViewRow row in dgvPlayerList.Rows)
                    {
                        row.DefaultCellStyle.BackColor = SystemColors.ButtonFace;
                    }

                    dgvPlayerList.Rows[currentFileListRow].DefaultCellStyle.BackColor = Color.LightGreen;

                    btnShuffleFile.Text = "Unshuffle";
                    btnShuffleFile.BackColor = Color.Aquamarine;

                    ShuffleFilesToolStripMenuItem.Text = "Unshuffle";
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnLoopFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (loopActivated) // Disable Loop
                {
                    loopActivated = false;

                    btnLoopFile.Text = "Loop";
                    btnLoopFile.BackColor = SystemColors.ButtonFace;

                    LoopFileToolStripMenuItem.Text = "Loop";
                }
                else               // Enable Loop
                {
                    loopActivated = true;

                    btnLoopFile.Text = "Unloop";
                    btnLoopFile.BackColor = Color.Red;

                    LoopFileToolStripMenuItem.Text = "Unloop";
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnOpenFileFolder_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = true;

                if (MusicToolStripMenuItem.Checked)
                {
                    fileDialog.Filter = "Audio Files | *.mp3; *.wav; *.avi";
                }
                else
                {
                    fileDialog.Filter = "Video Files | *.mp4; *.mpeg";
                }

                if (dgvPlayerList.SelectedRows.Count == 1)
                {
                    fileDialog.InitialDirectory = Path.GetDirectoryName(dgvPlayerList.CurrentRow.Cells[5].Value.ToString());
                    fileDialog.Title = "Click 'Open' to add the selected Files";
                    fileDialog.Multiselect = true;

                    string currentPathPlaying = wmpPlayer.URL;
                    currentPositionPlaying = wmpPlayer.Ctlcontrols.currentPosition;

                    secondsWaited = 0;

                    secondTimer.Start();

                    if (fileDialog.ShowDialog() == DialogResult.OK)
                    {
                        secondTimer.Stop();

                        currentPositionPlaying += (double)secondsWaited;
                        tkbTimeline.Value += secondsWaited;

                        dgvPlayerList.DataSource = null;

                        LoadProperties(fileDialog.FileNames, false);
                        SetFilesIntoList();

                        currentFileListRow = dgvPlayerList.Rows.OfType<DataGridViewRow>().Where(v => v.Cells[5].Value.Equals(currentPathPlaying)).First().Index;

                        dgvPlayerList.ClearSelection();
                        dgvPlayerList.CurrentCell = dgvPlayerList.Rows[currentFileListRow].Cells[0];
                        dgvPlayerList.Rows[currentFileListRow].Selected = true;

                        playTimer.Enabled = true;
                    }
                    else
                    {
                        secondTimer.Stop();
                    }                    
                }
                else
                {
                    PlayerMessageBox message;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        message = new PlayerMessageBox("information", "You have selected multiple Files!\n\nPlease select only one File...", "Multiple Files selected", true, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("information", "You have selected multiple Files!\n\nPlease select only one File...", "Multiple Files selected", true, false);
                    }

                    message.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void btnDeleteFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckFileCount())
                {
                    string[] deletingFileArray = dgvPlayerList
                        .SelectedRows.OfType<DataGridViewRow>()
                        .Select(p => p.Cells[5].Value.ToString())
                        .ToArray();

                    WriteDeletedFiles(deletingFileArray);

                    foreach (PlayerFile file in playerFileList.ToList())
                    {
                        if (deletingFileArray.Contains(file.Path))
                        {
                            playerFileList.Remove(file);

                            if (file.Path == wmpPlayer.currentMedia.sourceURL)
                            {
                                playingTrackDeleted = true;
                                wmpPlayer.Ctlcontrols.stop();
                                
                                if(currentFileListRow >= playerFileList.Count - 1)
                                {
                                    currentFileListRow = 0;
                                }                                
                            }
                            else
                            {
                                playingTrackDeleted = false;
                            }                            
                        }
                    }

                    if (playerFileList.Count > 0)
                    {
                        if (!playingTrackDeleted)
                        {
                            currentPositionPlaying = (double)wmpPlayer.Ctlcontrols.currentPosition;
                        }                        

                        LoadProperties(playerFileList.Select(p => p.Path).ToArray(), true);
                        SetFilesIntoList();

                        if (playingTrackDeleted)
                        {
                            if (dgvPlayerList.Rows.Count != 0)
                            {
                                playTimer.Enabled = true;
                            }
                            else
                            {
                                EnDisablePlayControls(false, false, false, false, false, false, false, false, false, false, false, false);
                            }
                        }
                        else
                        {
                            playTimer.Enabled = true;
                        }
                    }
                    else
                    {
                        ClearFiles();
                    }
                }
                else
                {
                    PlayerMessageBox message;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        message = new PlayerMessageBox("information", "Can not execute command!\n\nThe List is empty...", "Empty List", true, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("information", "Can not execute command!\n\nThe List is empty...", "Empty List", true, false);
                    }

                    message.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void WriteDeletedFiles(string[] deletingFileArray)
        {
            try
            {
                if (MusicToolStripMenuItem.Checked)
                {
                    File.AppendAllLines(resourcePath + @"Deleted\Music\Deleted.txt", deletingFileArray);
                }
                else
                {
                    File.AppendAllLines(resourcePath + @"Deleted\Video\Deleted.txt", deletingFileArray);
                }
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void secondCounter_Tick(object sender, EventArgs e)
        {
            try
            {
                secondsWaited++;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        // ------------------------------------

        private void dgvPlayerList_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                currentFileListRow = dgvPlayerList.CurrentRow.Index;

                tkbTimeline.Value = 0;
                currentPositionPlaying = null;

                playTimer.Enabled = true;
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void wmpPlayer_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            try
            {
                switch (e.newState)
                {
                    case 1: // Stopped
                        EnDisablePlayControls(false, false, true, true, true, true, false, false, true, true, true, true);
                        break;
                    case 2: // Paused
                        EnDisablePlayControls(true, true, true, true, true, true, false, true, true, true, true, true);
                        break;
                    case 3: // Playing
                        EnDisablePlayControls(true, true, true, true, true, false, true, true, true, true, true, true);
                        System.Threading.Thread.Sleep(50);
                        timelineTimer.Enabled = true;
                        break;
                    case 8: // MediaEnded
                        if (!loopActivated)
                        {
                            if (currentFileListRow + 1 != dgvPlayerList.RowCount)
                            {
                                currentFileListRow += 1;
                            }
                            else
                            {
                                currentFileListRow = 0;
                            }

                            playTimer.Enabled = true;
                        }
                        else
                        {
                            playTimer.Enabled = true;
                        }
                        break;
                    case 10: // Ready                       
                        timelineTimer.Enabled = false;

                        tkbTimeline.Value = 0;

                        currentTime = "00:00";
                        maxTime = "00:00";
                        lblTimeCounter.Text = currentTime + " / " + maxTime;

                        EnDisablePlayControls(false, false, false, false, false, false, false, false, false, false, false, false);

                        CheckAudioOutputs();
                        break;
                }
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void playTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                playTimer.Enabled = false;

                PlayNextFile();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void PlayNextFile()
        {
            try
            {
                if (!audioDeviceDetected)
                {
                    PlayerMessageBox message;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        message = new PlayerMessageBox("information", "No Audio Output is connected!\n\nPlease connect a Audio Output Device an press 'OK'", "No Audio Device connected", false, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("information", "No Audio Output is connected!\n\nPlease connect a Audio Output Device an press 'OK'", "No Audio Device connected", false, false);
                    }

                    message.ShowDialog();

                    if (message.ReturnMode)
                    {
                        while (!CheckAudioOutputs())
                        {
                            CheckAudioOutputs();
                        }
                    }
                }

                wmpPlayer.URL = dgvPlayerList.Rows[currentFileListRow].Cells[5].Value.ToString();                

                if (currentPositionPlaying == null)
                {
                    WriteHistoryFiles(wmpPlayer.URL);
                    tkbTimeline.Value = 0;
                }
                else
                {
                    tkbTimeline.Value = (int)currentPositionPlaying;
                }

                GetCoverPicture(wmpPlayer.URL);

                tkbTimeline.Maximum = (int)dgvPlayerList.Rows[currentFileListRow].Cells[4].Value;
                
                var currentTimeSpan = TimeSpan.FromSeconds(tkbTimeline.Value);
                var maxTimeSpan = TimeSpan.FromSeconds(tkbTimeline.Maximum);

                if (currentPositionPlaying != null)
                {
                    wmpPlayer.Ctlcontrols.currentPosition = (double)currentPositionPlaying;
                }

                if (maxTimeSpan.TotalSeconds >= 3600)
                {
                    currentTime = string.Format("{0:00}:{1:00}:{2:00}", currentTimeSpan.Hours, currentTimeSpan.Minutes, currentTimeSpan.Seconds);
                    maxTime = string.Format("{0:00}:{1:00}:{2:00}", maxTimeSpan.Hours, maxTimeSpan.Minutes, maxTimeSpan.Seconds);
                }
                else
                {
                    currentTime = string.Format("{0:00}:{1:00}", currentTimeSpan.Minutes, currentTimeSpan.Seconds);
                    maxTime = string.Format("{0:00}:{1:00}", maxTimeSpan.Minutes, maxTimeSpan.Seconds);
                }

                currentPositionPlaying = null;

                lblTimeCounter.Text = currentTime + " / " + maxTime;

                dgvPlayerList.ClearSelection();
                dgvPlayerList.CurrentCell = dgvPlayerList.Rows[currentFileListRow].Cells[0];
                dgvPlayerList.Rows[currentFileListRow].Selected = true;

                foreach(DataGridViewRow row in dgvPlayerList.Rows)
                {
                    row.DefaultCellStyle.BackColor = SystemColors.ButtonFace;
                }

                dgvPlayerList.CurrentRow.DefaultCellStyle.BackColor = Color.LightGreen;

                wmpPlayer.Ctlcontrols.play();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void WriteHistoryFiles(string path)
        {
            try
            {
                if (MusicToolStripMenuItem.Checked)
                {
                    File.AppendAllText(resourcePath + @"Log\Music\Log.txt", path + Environment.NewLine);
                }
                else
                {
                    File.AppendAllText(resourcePath + @"Log\Video\Log.txt", path + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void GetCoverPicture(string path)
        {
            try
            {
                if (Directory
                    .GetFiles(Path.GetDirectoryName(path))
                    .Select(p => Path.GetFileName(p))
                    .ToArray()
                    .Contains("cover.jpg"))
                {
                    pcbCover.Visible = true;
                    pcbCover.Image = Image.FromFile(Path.GetDirectoryName(path) + @"\cover.jpg");
                }
                else
                {
                    pcbCover.Visible = false;
                    pcbCover.Image = null;
                }

                ChangeControlLocation();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void ChangeControlLocation()
        {
            try
            {
                if (pcbCover.Visible)
                {
                    pnlPlayerList.Location = new Point((pcbCover.Location.X + pcbCover.Size.Width) + 6, pnlPlayerList.Location.Y);

                    if (!previousCoverView)
                    {
                        pnlPlayerList.Size = new Size((pnlPlayerList.Size.Width - pcbCover.Size.Width) - 6, pnlPlayerList.Size.Height);
                    }
                }
                else
                {
                    pnlPlayerList.Location = new Point(13, pnlPlayerList.Location.Y);

                    if (previousCoverView)
                    {
                        pnlPlayerList.Size = new Size((pnlPlayerList.Size.Width + pcbCover.Size.Width) + 6, pnlPlayerList.Size.Height);
                    }
                }

                previousCoverView = pcbCover.Visible;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        // ------------------------------------

        private void tkbVolume_Scroll(object sender, EventArgs e)
        {
            try
            {
                wmpPlayer.settings.volume = tkbVolume.Value;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void tkbTimeline_Scroll(object sender, EventArgs e)
        {
            try
            {
                if (wmpPlayer.playState != WMPLib.WMPPlayState.wmppsUndefined || wmpPlayer.playState != WMPLib.WMPPlayState.wmppsStopped)
                {
                    int percent = (int)Math.Round((double)(100 * tkbTimeline.Value) / tkbTimeline.Maximum);
                    currentPositionPlaying = ((double)tkbTimeline.Maximum / (double)100) * (double)percent;

                    tkbTimeline.Value = (int)currentPositionPlaying;
                    wmpPlayer.Ctlcontrols.currentPosition = (double)currentPositionPlaying;
                }
                else
                {
                    tkbTimeline.Value = 0;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void wmpPlayer_PositionChange(object sender, AxWMPLib._WMPOCXEvents_PositionChangeEvent e)
        {
            try
            {
                tkbTimeline.Value = (int)wmpPlayer.Ctlcontrols.currentPosition;

                var timeSpan = TimeSpan.FromSeconds(tkbTimeline.Value);

                if (timeSpan.TotalSeconds > 3600)
                {
                    currentTime = string.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                }
                else
                {
                    currentTime = string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
                }

                lblTimeCounter.Text = currentTime + " / " + maxTime;
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void timelineTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (tkbTimeline.Value + 1 <= tkbTimeline.Maximum)
                {
                    tkbTimeline.Value++;
                    var timeSpan = TimeSpan.FromSeconds(tkbTimeline.Value);

                    if (timeSpan.TotalSeconds > 3600)
                    {
                        currentTime = string.Format("{0:00}:{1:00}:{2:00}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    }
                    else
                    {
                        currentTime = string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
                    }

                    lblTimeCounter.Text = currentTime + " / " + maxTime;
                }
                else
                {
                    timelineTimer.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        // ------------------------------------

        private bool CheckFileCount()
        {
            try
            {
                if (dgvPlayerList.Rows.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }

                message.ShowDialog();

                return false;
            }
        }
        private void EnDisablePlayControls(bool fastBackMode, bool fastForwardMode, bool backMode, bool forwardMode, bool addMode, bool playMode, bool pauseMode, bool stopMode, bool shuffleMode, bool loopMode, bool locationMode, bool deleteMode)
        {
            try
            {
                if (fastBackMode)
                {
                    btnFastBack.Enabled = true;
                    FastBackToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnFastBack.Enabled = false;
                    FastBackToolStripMenuItem.Enabled = true;
                }

                if (fastForwardMode)
                {
                    btnFastForward.Enabled = true;
                    FastForwardToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnFastForward.Enabled = false;
                    FastForwardToolStripMenuItem.Enabled = false;
                }

                if (backMode)
                {
                    btnFileBack.Enabled = true;
                    btnFileBack.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold);
                    BackToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnFileBack.Enabled = false;
                    btnFileBack.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold | FontStyle.Strikeout);
                    BackToolStripMenuItem.Enabled = false;
                }

                if (forwardMode)
                {
                    btnFileForward.Enabled = true;
                    btnFileForward.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold);
                    ForwardToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnFileForward.Enabled = false;
                    btnFileForward.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold | FontStyle.Strikeout);
                    ForwardToolStripMenuItem.Enabled = false;
                }

                if (addMode)
                {
                    btnAddFiles.Enabled = true;
                    btnAddFiles.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold);
                    AddFilesToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnAddFiles.Enabled = false;
                    btnAddFiles.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold | FontStyle.Strikeout);
                    AddFilesToolStripMenuItem.Enabled = false;
                }

                if (playMode)
                {
                    btnPlayFile.Enabled = true;
                    btnPlayFile.Visible = true;
                    btnPlayFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold);
                    PlayToolStripMenuItem.Enabled = true;                    
                }
                else
                {
                    btnPlayFile.Enabled = false;
                    btnPlayFile.Visible = false;
                    btnPlayFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold | FontStyle.Strikeout);
                    PlayToolStripMenuItem.Enabled = false;
                }

                if (pauseMode)
                {
                    btnPauseFile.Enabled = true;
                    btnPauseFile.Visible = true;
                    btnPauseFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold);
                    PauseToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnPauseFile.Enabled = false;
                    btnPauseFile.Visible = false;
                    btnPauseFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold | FontStyle.Strikeout);
                    PauseToolStripMenuItem.Enabled = false;
                }

                if (stopMode)
                {
                    btnStopFile.Enabled = true;
                    btnStopFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold);
                    StopToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnStopFile.Enabled = false;
                    btnStopFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold | FontStyle.Strikeout);
                    StopToolStripMenuItem.Enabled = false;
                }

                if (shuffleMode)
                {
                    btnShuffleFile.Enabled = true;
                    btnShuffleFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold);
                    ShuffleFilesToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnShuffleFile.Enabled = false;
                    btnShuffleFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold | FontStyle.Strikeout);
                    ShuffleFilesToolStripMenuItem.Enabled = false;
                }

                if (loopMode)
                {
                    btnLoopFile.Enabled = true;
                    btnLoopFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold);
                    LoopFileToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnLoopFile.Enabled = false;
                    btnLoopFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold | FontStyle.Strikeout);
                    LoopFileToolStripMenuItem.Enabled = false;
                }

                if (locationMode)
                {
                    btnOpenFileFolder.Enabled = true;
                    btnOpenFileFolder.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold);
                    OpenFileFolderToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnOpenFileFolder.Enabled = false;
                    btnOpenFileFolder.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold | FontStyle.Strikeout);
                    OpenFileFolderToolStripMenuItem.Enabled = false;
                }

                if (deleteMode)
                {
                    btnDeleteFile.Enabled = true;
                    btnDeleteFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold);
                    DeleteToolStripMenuItem.Enabled = true;
                }
                else
                {
                    btnDeleteFile.Enabled = false;
                    btnDeleteFile.Font = new Font("Bahnschrift", 8.75f, FontStyle.Bold | FontStyle.Strikeout);
                    DeleteToolStripMenuItem.Enabled = false;
                }

                if (!btnPlayFile.Enabled && !btnPauseFile.Enabled)
                {
                    btnPlayFile.Visible = true;
                    btnPauseFile.Visible = false;
                }

                if (btnPlayFile.Enabled && btnPlayFile.Visible)
                {
                    btnPlayFile.Focus();
                }
                else if (btnPauseFile.Enabled && btnPauseFile.Visible)
                {
                    btnPauseFile.Focus();
                }
                else
                {
                    btnOpenFiles.Focus();
                }                
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        // ------------------------------------

        private void MusicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                MusicToolStripMenuItem.Checked = true;
                MusicToolStripMenuItem.Enabled = false;
                VideoToolStripMenuItem.Checked = false;
                VideoToolStripMenuItem.Enabled = true;

                Icon = Resources.music;
                Text = "DoobieAsPlayer - Music";

                HideTimelineToolStripMenuItem_Click(this, EventArgs.Empty);

                ClearFiles();
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void VideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                VideoToolStripMenuItem.Checked = true;
                VideoToolStripMenuItem.Enabled = false;
                MusicToolStripMenuItem.Checked = false;
                MusicToolStripMenuItem.Enabled = true;

                Icon = Resources.video;
                Text = "DoobieAsPlayer - Video";

                HideTimelineToolStripMenuItem_Click(this, EventArgs.Empty);

                ClearFiles();
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void InterfaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var menuItem = (ToolStripMenuItem)sender;

                switch (menuItem.Name)
                {
                    case "FullToolStripMenuItem":
                        FullToolStripMenuItem.Checked = true;
                        MiniToolStripMenuItem.Checked = false;
                        NoneToolStripMenuItem.Checked = false;

                        wmpPlayer.uiMode = "full";
                        break;
                    case "MiniToolStripMenuItem":
                        FullToolStripMenuItem.Checked = false;
                        MiniToolStripMenuItem.Checked = true;
                        NoneToolStripMenuItem.Checked = false;

                        wmpPlayer.uiMode = "mini";
                        break;
                    case "NoneToolStripMenuItem":
                        FullToolStripMenuItem.Checked = false;
                        MiniToolStripMenuItem.Checked = false;
                        NoneToolStripMenuItem.Checked = true;

                        wmpPlayer.uiMode = "none";
                        break;
                }
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlayerMessageBox message;

            if (MusicToolStripMenuItem.Checked)
            {
                message = new PlayerMessageBox("dude", "This application has been programmed and tested by\n\nDavid Griesser aka DoobieAsDave", "About DoobiePlayer", true, true);
            }
            else
            {
                message = new PlayerMessageBox("dude", "This application has been programmed and tested by\n\nDavid Griesser aka DoobieAsDave", "About DoobiePlayer", true, false);
            }

            message.ShowDialog();
        }
        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
                {
                    message = new PlayerMessageBox("dude", "You can mail me your question to:\n\ndavid.griesser(at)hotmail.ch", "DoobieAsPlayer Support", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("dude", "You can mail me your question to:\n\ndavid.griesser(at)hotmail.ch", "DoobieAsPlayer Support", true, false);
                }

                message.ShowDialog();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OpenFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnOpenFiles_Click(this, EventArgs.Empty);
        }
        private void AddFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnAddFiles_Click(this, EventArgs.Empty);
        }                      
        private void BackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnFileBack_Click(this, EventArgs.Empty);
        }
        private void ForwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnFileForward_Click(this, EventArgs.Empty);
        }
        private void PlayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnPlayFile_Click(this, EventArgs.Empty);
        }
        private void PauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnPauseFile_Click(this, EventArgs.Empty);
        }
        private void StopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnStopFile_Click(this, EventArgs.Empty);
        }
        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnDeleteFile_Click(this, EventArgs.Empty);
        }
        private void ShuffleFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnShuffleFile_Click(this, EventArgs.Empty);
        }
        private void LoopFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnLoopFile_Click(this, EventArgs.Empty);
        }
        private void OpenFileFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnOpenFileFolder_Click(this, EventArgs.Empty);
        }
        private void FindFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SearchFile searchForm;

                if (MusicToolStripMenuItem.Checked)
                {
                    searchForm = new SearchFile(true);
                }
                else
                {
                    searchForm = new SearchFile(false);
                }

                searchForm.ShowDialog();

                var searchIndeces = new List<int>();

                foreach (DataGridViewRow row in dgvPlayerList.Rows)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.Value.ToString().ToLower() == searchForm.SearchContent.ToLower() || cell.Value.ToString().ToLower().Contains(searchForm.SearchContent.ToLower()))
                        {
                            searchIndeces.Add(row.Index);
                        }
                    }
                }

                if (searchIndeces.Count > 0)
                {
                    dgvPlayerList.ClearSelection();

                    foreach (int index in searchIndeces)
                    {
                        dgvPlayerList.Rows[index].Selected = true;
                        dgvPlayerList.FirstDisplayedScrollingRowIndex = index;
                    }
                }
                else
                {
                    PlayerMessageBox message;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        message = new PlayerMessageBox("information", "Could not find '" + searchForm.SearchContent + "'", "Search returned", true, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("information", "Could not find '" + searchForm.SearchContent + "'", "Search returned", true, false);
                    }

                    message.ShowDialog();
                }
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void CreatePlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckFileCount())
                {
                    string filePath;
                    FileList listForm;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        filePath = resourcePath + @"Playlists\Music\";
                        listForm = new FileList(true, true);
                    }
                    else
                    {
                        filePath = resourcePath + @"Playlists\Video\";
                        listForm = new FileList(true, false);
                    }                    

                    listForm.ShowDialog();

                    if (!listForm.Cancelled)
                    {
                        if (!File.Exists(filePath + listForm.ListName + ".txt"))
                        {
                            string[] pathArray;

                            if (dgvPlayerList.SelectedRows.Count > 1 && dgvPlayerList.SelectedRows.Count != dgvPlayerList.Rows.Count)
                            {
                                pathArray = dgvPlayerList.SelectedRows.OfType<DataGridViewRow>().Select(p => p.Cells[5].Value.ToString()).ToArray();
                            }
                            else
                            {
                                pathArray = dgvPlayerList.Rows.OfType<DataGridViewRow>().Select(p => p.Cells[5].Value.ToString()).ToArray();
                            }

                            using (var writer = new StreamWriter(filePath + listForm.ListName + ".txt"))
                            {
                                foreach (string path in pathArray)
                                {
                                    writer.WriteLine(path);
                                }

                                writer.Close();
                            }

                            CheckPlaylists();

                            PlayerMessageBox message;

                            if (MusicToolStripMenuItem.Checked)
                            {
                                message = new PlayerMessageBox("success", "Playlist '" + listForm.ListName + "' has been created", "Playlist created", true, true);
                            }
                            else
                            {
                                message = new PlayerMessageBox("success", "Playlist '" + listForm.ListName + "' has been created", "Playlist created", true, false);
                            }

                            message.ShowDialog();
                        }
                    }
                }
                else
                {
                    PlayerMessageBox message;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        message = new PlayerMessageBox("information", "Can not execute command!\n\nThe List is empty...", "Empty List", true, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("information", "Can not execute command!\n\nThe List is empty...", "Empty List", true, false);
                    }

                    message.ShowDialog();
                }
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void OpenPlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = false;
                fileDialog.Filter = "Playlist Files | *.txt";

                if (MusicToolStripMenuItem.Checked)
                {
                    fileDialog.InitialDirectory = resourcePath + @"Playlists\Music\";
                }
                else
                {
                    fileDialog.InitialDirectory = resourcePath + @"Playlists\Video\";
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (CheckFilePaths(fileDialog.FileName))
                    {
                        wmpPlayer.Ctlcontrols.stop();
                        LoadProperties(File.ReadAllLines(fileDialog.FileName), true);
                        SetFilesIntoList();

                        playTimer.Enabled = true;
                    }
                    else
                    {
                        PlayerMessageBox message;

                        if (MusicToolStripMenuItem.Checked)
                        {
                            message = new PlayerMessageBox("information", "Some Files could not been found\n\nThey have been moved to another directory or deleted...", "Some Files could not be found", true, true);
                        }
                        else
                        {
                            message = new PlayerMessageBox("information", "Some Files could not been found\n\nThey have been moved to another directory or deleted...", "Some Files could not be found", true, false);
                        }

                        message.ShowDialog();
                    }
                }
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void EditPlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = false;
                fileDialog.Filter = "Playlist Files | *.txt";

                if (MusicToolStripMenuItem.Checked)
                {
                    fileDialog.InitialDirectory = resourcePath + @"Playlists\Music";
                }
                else
                {
                    fileDialog.InitialDirectory = resourcePath + @"Playlists\Video";
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    var editListForm = new EditFileList(fileDialog.FileName, true);
                    editListForm.ShowDialog();

                    if (editListForm.EmptyFolder)
                    {
                        CheckPlaylists();
                    }
                }
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void DeletePlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = true;
                fileDialog.Filter = "Playlist Files | *.txt";
                fileDialog.Title = "Choose Playlist to delete";

                if (MusicToolStripMenuItem.Checked)
                {
                    fileDialog.InitialDirectory = resourcePath + @"Playlists\Music\";
                }
                else
                {
                    fileDialog.InitialDirectory = resourcePath + @"Playlists\Video\";
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    string message = "Do you really want to delete Playlist(s):\n\n";

                    foreach (string path in fileDialog.FileNames)
                    {
                        message += Path.GetFileNameWithoutExtension(path) + "\n";
                    }

                    PlayerMessageBox messageAsk;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        messageAsk = new PlayerMessageBox("question", message, "Delete Playlist", false, true);
                    }
                    else
                    {
                        messageAsk = new PlayerMessageBox("question", message, "Delete Playlist", false, false);
                    }

                    messageAsk.ShowDialog();

                    if (messageAsk.ReturnMode)
                    {
                        foreach (string path in fileDialog.FileNames)
                        {
                            File.Delete(path);
                        }
                    }
                }

                CheckPlaylists();
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void CreateBackupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckFileCount())
                {
                    string filePath;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        filePath = resourcePath + @"Backups\Music\";
                    }
                    else
                    {
                        filePath = resourcePath + @"Backups\Video\";
                    }

                    if (autoBackupFileName == null)
                    {
                        FileList listForm;

                        if (MusicToolStripMenuItem.Checked)
                        {
                            listForm = new FileList(false, true);
                        }
                        else
                        {
                            listForm = new FileList(false, false);
                        }

                        listForm.ShowDialog();

                        if (!listForm.Cancelled)
                        {
                            if (!File.Exists(filePath + listForm.ListName + ".txt"))
                            {
                                string[] pathArray;

                                if (dgvPlayerList.SelectedRows.Count > 1 && dgvPlayerList.SelectedRows.Count != dgvPlayerList.Rows.Count)
                                {
                                    pathArray = dgvPlayerList.SelectedRows.OfType<DataGridViewRow>().Select(p => p.Cells[5].Value.ToString()).ToArray();
                                }
                                else
                                {
                                    pathArray = dgvPlayerList.Rows.OfType<DataGridViewRow>().Select(p => p.Cells[5].Value.ToString()).ToArray();
                                }

                                using (var writer = new StreamWriter(filePath + listForm.ListName + ".txt"))
                                {
                                    foreach (string path in pathArray)
                                    {
                                        writer.WriteLine(path);
                                    }

                                    writer.Close();
                                }

                                CheckBackups(false);

                                PlayerMessageBox message;

                                if (MusicToolStripMenuItem.Checked)
                                {
                                    message = new PlayerMessageBox("success", "Backup '" + listForm.ListName + "' has been created", "Backup created", true, true);
                                }
                                else
                                {
                                    message = new PlayerMessageBox("success", "Backup '" + listForm.ListName + "' has been created", "Backup created", true, false);
                                }

                                message.ShowDialog();
                            }
                        }
                    }
                    else
                    {
                        if (!File.Exists(filePath + autoBackupFileName + ".txt"))
                        {
                            string[] pathArray;

                            if (dgvPlayerList.SelectedRows.Count > 1 && dgvPlayerList.SelectedRows.Count != dgvPlayerList.Rows.Count)
                            {
                                pathArray = dgvPlayerList.SelectedRows.OfType<DataGridViewRow>().Select(p => p.Cells[5].Value.ToString()).ToArray();
                            }
                            else
                            {
                                pathArray = dgvPlayerList.Rows.OfType<DataGridViewRow>().Select(p => p.Cells[5].Value.ToString()).ToArray();
                            }

                            using (var writer = new StreamWriter(filePath + autoBackupFileName + ".txt"))
                            {
                                foreach (string path in pathArray)
                                {
                                    writer.WriteLine(path);
                                }

                                writer.Close();
                            }

                            PlayerMessageBox message;

                            if (MusicToolStripMenuItem.Checked)
                            {
                                message = new PlayerMessageBox("success", "Created '" + autoBackupFileName + ".txt'", "Auto Backup created", true, true);
                            }
                            else
                            {
                                message = new PlayerMessageBox("success", "Created '" + autoBackupFileName + ".txt'", "Auto Backup created", true, false);
                            }

                            message.ShowDialog();
                        }
                    }
                }
                else
                {
                    PlayerMessageBox message;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        message = new PlayerMessageBox("information", "Can not execute command!\n\nThe List is empty...", "Empty List", true, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("information", "Can not execute command!\n\nThe List is empty...", "Empty List", true, false);
                    }

                    message.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void OpenBackupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = false;
                fileDialog.Filter = "Backup Files | *.txt";

                if (MusicToolStripMenuItem.Checked)
                {
                    fileDialog.InitialDirectory = resourcePath + @"Backups\Music\";
                }
                else
                {
                    fileDialog.InitialDirectory = resourcePath + @"Backups\Video\";
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (CheckFilePaths(fileDialog.FileName))
                    {
                        wmpPlayer.Ctlcontrols.stop();
                        LoadProperties(File.ReadAllLines(fileDialog.FileName), true);
                        SetFilesIntoList();

                        playTimer.Enabled = true;
                    }
                    else
                    {
                        PlayerMessageBox message;

                        if (MusicToolStripMenuItem.Checked)
                        {
                            message = new PlayerMessageBox("information", "Some Files could not been found\n\nThey have been moved to another directory or deleted...", "Some Files could not be found", true, true);
                        }
                        else
                        {
                            message = new PlayerMessageBox("information", "Some Files could not been found\n\nThey have been moved to another directory or deleted...", "Some Files could not be found", true, false);
                        }

                        message.ShowDialog();
                    }                    
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void EditBackupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = true;
                fileDialog.Filter = "Backup Files | *.txt";

                if (MusicToolStripMenuItem.Checked)
                {
                    fileDialog.InitialDirectory = resourcePath + @"Backups\Music";
                }
                else
                {
                    fileDialog.InitialDirectory = resourcePath + @"Backups\Video";
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    var editListForm = new EditFileList(fileDialog.FileName, true);
                    editListForm.ShowDialog();

                    if (editListForm.EmptyFolder)
                    {
                        CheckBackups(false);
                    }
                }
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void DeleteBackupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = true;
                fileDialog.Filter = "Backup Files | *.txt";
                fileDialog.Title = "Choose the Backups to delete";

                if (MusicToolStripMenuItem.Checked)
                {
                    fileDialog.InitialDirectory = resourcePath + @"Backups\Music\";
                }
                else
                {
                    fileDialog.InitialDirectory = resourcePath + @"Backups\Video\";
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    string message = "Do you really want to delete Backup(s):\n\n";

                    foreach (string path in fileDialog.FileNames)
                    {
                        message += Path.GetFileNameWithoutExtension(path) + "\n";
                    }

                    PlayerMessageBox messageAsk;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        messageAsk = new PlayerMessageBox("question", message, "Delete Backup", false, true);
                    }
                    else
                    {
                        messageAsk = new PlayerMessageBox("question", message, "Delete Backup", false, false);
                    }

                    messageAsk.ShowDialog();

                    if (messageAsk.ReturnMode)
                    {
                        foreach (string path in fileDialog.FileNames)
                        {
                            File.Delete(path);
                        }
                    }
                }

                CheckBackups(false);
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void OpenLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckFileCount())
                {
                    currentPathPlaying = dgvPlayerList.CurrentRow.Cells[5].Value.ToString();
                    currentPositionPlaying = wmpPlayer.Ctlcontrols.currentPosition;

                    secondsWaited = 0;

                    secondTimer.Start();

                    PlayerLog logForm;

                    string logPath;
                    if (MusicToolStripMenuItem.Checked)
                    {
                        logForm = new PlayerLog(resourcePath + @"Log\Music\Log.txt", true);
                        logPath = resourcePath + @"Log\Music\Log.txt";
                    }
                    else
                    {
                        logForm = new PlayerLog(resourcePath + @"Log\Video\Log.txt", false);
                        logPath = resourcePath + @"Log\Video\Log.txt";
                    }

                    logForm.ShowDialog();

                    if (logForm.Paths != null)
                    {
                        if (CheckFilePaths(logPath))
                        {
                            secondTimer.Stop();

                            currentPositionPlaying += (double)secondsWaited;

                            dgvPlayerList.DataSource = null;

                            LoadProperties(logForm.Paths, false);
                            SetFilesIntoList();

                            currentFileListRow = dgvPlayerList.Rows.OfType<DataGridViewRow>().Where(v => v.Cells[5].Value.Equals(currentPathPlaying)).First().Index;

                            dgvPlayerList.ClearSelection();
                            dgvPlayerList.CurrentCell = dgvPlayerList.Rows[currentFileListRow].Cells[0];
                            dgvPlayerList.Rows[currentFileListRow].Selected = true;

                            playTimer.Enabled = true;
                        }
                        else
                        {
                            PlayerMessageBox message;

                            if (MusicToolStripMenuItem.Checked)
                            {
                                message = new PlayerMessageBox("information", "Some Files could not been found\n\nThey have been moved to another directory or deleted...", "Some Files could not be found", true, true);
                            }
                            else
                            {
                                message = new PlayerMessageBox("information", "Some Files could not been found\n\nThey have been moved to another directory or deleted...", "Some Files could not be found", true, false);
                            }

                            message.ShowDialog();
                        }                      
                    }
                }
                else
                {
                    PlayerLog logForm;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        logForm = new PlayerLog(resourcePath + @"Log\Music\Log.txt", true);
                    }
                    else
                    {
                        logForm = new PlayerLog(resourcePath + @"Log\Video\Log.txt", false);
                    }

                    logForm.ShowDialog();

                    if (logForm.Paths != null)
                    {
                        dgvPlayerList.DataSource = null;

                        LoadProperties(logForm.Paths, false);
                        SetFilesIntoList();

                        dgvPlayerList.ClearSelection();
                        dgvPlayerList.CurrentCell = dgvPlayerList.Rows[currentFileListRow].Cells[0];
                        dgvPlayerList.Rows[currentFileListRow].Selected = true;

                        playTimer.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void ResetLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (MusicToolStripMenuItem.Checked)
                {
                    PlayerMessageBox message;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        message = new PlayerMessageBox("question", "Do you really want to delete all your Music History?", "Delete Music History", false, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("question", "Do you really want to delete all your Video History?", "Video Music History", false, false);
                    }

                    message.ShowDialog();

                    if (message.ReturnMode)
                    {
                        File.WriteAllText(resourcePath + @"Log\Music\Log.txt", string.Empty);

                        PlayerMessageBox messageSuccess;

                        if (MusicToolStripMenuItem.Checked)
                        {
                            messageSuccess = new PlayerMessageBox("success", "The Music Log File has been reset!", "Music Log reset", true, true);
                        }
                        else
                        {
                            messageSuccess = new PlayerMessageBox("success", "The Music Log File has been reset!", "Music Log reset", true, false);
                        }

                        messageSuccess.ShowDialog();
                    }
                }
                else
                {
                    PlayerMessageBox message;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        message = new PlayerMessageBox("question", "Are you really sure that you want to delete all your Video History?", "Delete VIdeo History", false, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("question", "Are you really sure that you want to delete all your Video History?", "Delete VIdeo History", false, false);
                    }

                    message.ShowDialog();

                    if (message.ReturnMode)
                    {
                        File.WriteAllText(resourcePath + @"Log\Video\Log.txt", string.Empty);

                        PlayerMessageBox messageSuccess;

                        if (MusicToolStripMenuItem.Checked)
                        {
                            messageSuccess = new PlayerMessageBox("success", "The Video Log File has been reset!", "Video Log reset", true, true);
                        }
                        else
                        {
                            messageSuccess = new PlayerMessageBox("success", "The Video Log File has been reset!", "Video Log reset", true, false);
                        }

                        messageSuccess.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private bool CheckFilePaths(string listPath)
        {
            try
            {
                List<string> filePaths = File.ReadAllLines(listPath).ToList();

                int originalSize = filePaths.Count;

                for (int i = 0; i < filePaths.Count; i++)
                {
                    if (!File.Exists(filePaths[i]))
                    {
                        try
                        {
                            filePaths[i] = new DirectoryInfo(Settings.Default.MusicPath)
                                .EnumerateFiles(Path.GetFileName(filePaths[i]), SearchOption.AllDirectories)
                                .Select(d => d.FullName)
                                .ToList()[0];
                        }
                        catch
                        {
                            filePaths[i] = null;
                        }
                    }
                }

                foreach(string path in filePaths.ToList())
                {
                    if(path == null)
                    {
                        filePaths.Remove(path);
                    }
                }

                File.WriteAllLines(listPath, filePaths);

                if(filePaths.Count != originalSize)
                {
                    PlayerMessageBox message;

                    if (MusicToolStripMenuItem.Checked)
                    {
                        message = new PlayerMessageBox("information", "Some file could not been found!\n\nThey have been removed from the List", "Some Files could not be found", true, true);
                    }
                    else
                    {
                        message = new PlayerMessageBox("information", "Some file could not been found!\n\nThey have been removed from the List", "Some Files could not be found", true, false);
                    }

                    message.ShowDialog();
                }

                return true;
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, true);
                }
                else
                {
                    message = new PlayerMessageBox("error", ex.Message, "An error occurred", true, false);
                }

                message.ShowDialog();

                return false;
            }
        }
        
        private void HideButtonsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (buttonsActivated)
                {
                    buttonsActivated = false;

                    pnlControlPanel.Visible = false;

                    tkbVolume.Location = new Point(Size.Width - tkbVolume.Size.Width - 29, tkbVolume.Location.Y);

                    if (pcbCover.Visible)
                    {
                        pnlPlayerList.Size = new Size(((wmpPlayer.Size.Width - tkbVolume.Size.Width) - pcbCover.Size.Width) - 12, pnlPlayerList.Size.Height);
                    }
                    else
                    {
                        pnlPlayerList.Size = new Size((wmpPlayer.Size.Width - tkbVolume.Size.Width) - 10, pnlPlayerList.Size.Height);
                    }

                    HideButtonsToolStripMenuItem.Checked = true;
                }
                else
                {
                    if (Size.Width >= 800)
                    {
                        buttonsActivated = true;

                        pnlControlPanel.Visible = true;

                        tkbVolume.Location = new Point(pnlControlPanel.Location.X - tkbVolume.Size.Width - 4, tkbVolume.Location.Y);

                        if (pcbCover.Visible)
                        {
                            pnlPlayerList.Size = new Size((((wmpPlayer.Size.Width - tkbVolume.Size.Width) - pnlControlPanel.Size.Width) - pcbCover.Size.Width) - 18, pnlPlayerList.Size.Height);
                        }
                        else
                        {
                            pnlPlayerList.Size = new Size(tkbVolume.Location.X - 18, pnlPlayerList.Size.Height);
                        }

                        HideButtonsToolStripMenuItem.Checked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void HideTimelineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (!HideTimelineToolStripMenuItem.Checked)
                {
                    tkbTimeline.Visible = false;
                    pnlTimeCounter.Visible = false;
                    HideTimelineToolStripMenuItem.Checked = true;

                    pnlPlayerPanel.Size = new Size(pnlPlayerPanel.Size.Width, (pnlPlayerList.Location.Y - 6) - pnlPlayerPanel.Location.Y);

                    wmpPlayer.uiMode = "full";
                }
                else
                {
                    tkbTimeline.Visible = true;
                    pnlTimeCounter.Visible = true;
                    HideTimelineToolStripMenuItem.Checked = false;

                    pnlPlayerPanel.Size = new Size(pnlPlayerPanel.Size.Width, (tkbTimeline.Location.Y - 10) - pnlPlayerPanel.Location.Y);

                    wmpPlayer.uiMode = "none";
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void AskToLoadBackupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (AskToLoadBackupsToolStripMenuItem.Checked)
                {
                    AskToLoadBackupsToolStripMenuItem.Checked = false;
                    Settings.Default.AskForBackup = "false";
                }
                else
                {
                    AskToLoadBackupsToolStripMenuItem.Checked = true;
                    Settings.Default.AskForBackup = "true";
                }

                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        private void AutoCreateBackupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (AutoCreateBackupsToolStripMenuItem.Checked)
                {
                    AutoCreateBackupsToolStripMenuItem.Checked = false;
                    Settings.Default.AutoBackup = "false";
                }
                else
                {
                    AutoCreateBackupsToolStripMenuItem.Checked = true;
                    Settings.Default.AutoBackup = "true";
                }

                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void MinimizedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (MinimizedToolStripMenuItem.Checked)
                {
                    MinimizedToolStripMenuItem.Checked = false;
                    Settings.Default.Minimized = "false";
                }
                else
                {
                    MinimizedToolStripMenuItem.Checked = true;
                    Settings.Default.Minimized = "true";
                }

                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void PreservePlayerlistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (PreservePlayerlistToolStripMenuItem.Checked)
                {
                    Settings.Default.PreserveList = "false";
                    PreservePlayerlistToolStripMenuItem.Checked = false;
                }
                else
                {
                    Settings.Default.PreserveList = "true";
                    PreservePlayerlistToolStripMenuItem.Checked = true;
                }

                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void EditSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSettings();
        }

        private void ShowMostPlayedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string[] logPaths = null;

                var fileStatsList = new List<FileStatistic>();
                               
                if (MusicToolStripMenuItem.Checked)
                {
                    CheckFilePaths(resourcePath + @"Log\Music\Log.txt");
                    logPaths = File.ReadAllLines(resourcePath + @"Log\Music\Log.txt").ToArray();
                }
                else
                {
                    CheckFilePaths(resourcePath + @"Log\Music\Log.txt");
                    logPaths = File.ReadAllLines(resourcePath + @"Log\Video\Log.txt").ToArray();
                }                

                foreach(string path in logPaths)
                {
                    var fileStats = new FileStatistic();
                    fileStats.Name = TagLib.File.Create(path).Tag.Title ?? Path.GetFileNameWithoutExtension(path);
                    fileStats.Path = path;

                    if (fileStatsList.All(f => f.Name != fileStats.Name))
                    {
                        fileStatsList.Add(fileStats);
                    }
                    else
                    {
                        fileStatsList[fileStatsList.IndexOf(fileStatsList.Where(f => f.Name == fileStats.Name).FirstOrDefault())].XTouched++;
                    }
                }

                fileStatsList = fileStatsList.OrderByDescending(f => f.XTouched).ToList();

                PlayerStatistic statisticForm;

                if (MusicToolStripMenuItem.Checked)
                {
                    statisticForm = new PlayerStatistic(fileStatsList, true, true);
                }
                else
                {
                    statisticForm = new PlayerStatistic(fileStatsList, true, false);
                }

                statisticForm.ShowDialog();

                if (statisticForm.FilePaths != null)
                {
                    dgvPlayerList.DataSource = null;
                    wmpPlayer.Ctlcontrols.pause();                                       

                    currentPositionPlaying = wmpPlayer.Ctlcontrols.currentPosition;

                    LoadProperties(statisticForm.FilePaths, false);
                    SetFilesIntoList();

                    dgvPlayerList.ClearSelection();
                    dgvPlayerList.CurrentCell = dgvPlayerList.Rows[currentFileListRow].Cells[0];
                    dgvPlayerList.Rows[currentFileListRow].Selected = true;

                    playTimer.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void ShowMostHatedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string[] deletedPaths = null;

                var fileStatsList = new List<FileStatistic>();

                if (MusicToolStripMenuItem.Checked)
                {
                    CheckFilePaths(resourcePath + @"Deleted\Music\Deleted.txt");
                    deletedPaths = File.ReadAllLines(resourcePath + @"Deleted\Music\Deleted.txt");
                }
                else
                {
                    CheckFilePaths(resourcePath + @"Deleted\Video\Deleted.txt");
                    deletedPaths = File.ReadAllLines(resourcePath + @"Deleted\Video\Deleted.txt");
                }

                foreach(string path in deletedPaths)
                {
                    var fileStats = new FileStatistic();
                    fileStats.Name = TagLib.File.Create(path).Tag.Title ?? Path.GetFileNameWithoutExtension(path);
                    fileStats.Path = path;

                    if (fileStatsList.All(f => f.Name != fileStats.Name))
                    {
                        fileStatsList.Add(fileStats);
                    }
                    else
                    {
                        fileStatsList[fileStatsList.IndexOf(fileStatsList.Where(f => f.Name == fileStats.Name).FirstOrDefault())].XTouched++;
                    }                    
                }

                fileStatsList = fileStatsList.OrderByDescending(f => f.XTouched).ToList();

                PlayerStatistic statisticForm;

                if (MusicToolStripMenuItem.Checked)
                {
                    statisticForm = new PlayerStatistic(fileStatsList, false, true);
                }
                else
                {
                    statisticForm = new PlayerStatistic(fileStatsList, false, false);
                }

                statisticForm.ShowDialog();

                if (statisticForm.FilePaths != null)
                {
                    dgvPlayerList.DataSource = null;
                    wmpPlayer.Ctlcontrols.pause();

                    currentPositionPlaying = wmpPlayer.Ctlcontrols.currentPosition;

                    LoadProperties(statisticForm.FilePaths, false);
                    SetFilesIntoList();

                    dgvPlayerList.ClearSelection();
                    dgvPlayerList.CurrentCell = dgvPlayerList.Rows[currentFileListRow].Cells[0];
                    dgvPlayerList.Rows[currentFileListRow].Selected = true;

                    playTimer.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void ShowPlayingFileStatisticToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string currentFilePath = dgvPlayerList.CurrentRow.Cells[5].EditedFormattedValue.ToString();

                int logFileCount;
                string[] playlistsArray;
                Dictionary<string, int> playlistCountDictionary = new Dictionary<string, int>();

                if (MusicToolStripMenuItem.Checked)
                {
                    logFileCount = File.ReadAllLines(resourcePath + @"Log\Music\log.txt")
                        .Where(l => l == currentFilePath)
                        .Count();

                    playlistsArray = Directory.GetFiles(resourcePath + @"Playlists\Music\");

                    foreach (var playlist in playlistsArray)
                    {
                        playlistCountDictionary.Add(Path.GetFileNameWithoutExtension(playlist), File.ReadAllLines(resourcePath + @"Playlists\Music\" + Path.GetFileName(playlist))
                            .Where(l => l == currentFilePath)
                            .Count());
                    }
                }
                else
                {
                    logFileCount = File.ReadAllLines(resourcePath + @"Log\Video\log.txt")
                        .Where(l => l == currentFilePath).Count();

                    playlistsArray = Directory.GetFiles(resourcePath + @"Playlists\Video\");

                    foreach (var playlist in playlistsArray)
                    {
                        playlistCountDictionary.Add(Path.GetFileNameWithoutExtension(playlist), File.ReadAllLines(resourcePath + @"Playlists\Video\" + Path.GetFileName(playlist))
                            .Where(l => l == currentFilePath)
                            .Count());
                    }
                }  
                
                foreach(var pair in playlistCountDictionary.ToList())
                {
                    if(pair.Value == 0)
                    {
                        playlistCountDictionary.Remove(pair.Key);
                    }
                }

                FileStatisticForm fileStatisticForm;

                if (MusicToolStripMenuItem.Checked)
                {
                    fileStatisticForm = new FileStatisticForm(currentFilePath, logFileCount, playlistCountDictionary, true);
                }
                else
                {
                    fileStatisticForm = new FileStatisticForm(currentFilePath, logFileCount, playlistCountDictionary, false);
                }
                
                fileStatisticForm.ShowDialog();
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        // ------------------------------------

        private void Player_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                if (Size.Width <= 800)
                {
                    if (buttonsActivated)
                    {
                        pnlControlPanel.Visible = false;

                        tkbVolume.Location = new Point(Size.Width - tkbVolume.Size.Width - 29, tkbVolume.Location.Y);

                        if (pcbCover.Visible)
                        {
                            pnlPlayerList.Size = new Size(((wmpPlayer.Size.Width - tkbVolume.Size.Width) - pcbCover.Size.Width) - 12, pnlPlayerList.Size.Height);
                        }
                        else
                        {
                            pnlPlayerList.Size = new Size((wmpPlayer.Size.Width - tkbVolume.Size.Width) - 6, pnlPlayerList.Size.Height);
                        }

                        HideButtonsToolStripMenuItem.Checked = true;
                    }
                }
                else
                {
                    if (buttonsActivated)
                    {
                        pnlControlPanel.Visible = true;

                        tkbVolume.Location = new Point(pnlControlPanel.Location.X - tkbVolume.Size.Width - 4, tkbVolume.Location.Y);

                        if (pcbCover.Visible)
                        {
                            pnlPlayerList.Size = new Size(((tkbVolume.Location.X - 18) - pcbCover.Size.Width) - 5, pnlPlayerList.Size.Height);
                        }
                        else
                        {
                            pnlPlayerList.Size = new Size(tkbVolume.Location.X - 18, pnlPlayerList.Size.Height);
                        }

                        HideButtonsToolStripMenuItem.Checked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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

        // ------------------------------------

        private void Player_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (enuffPermission)
                {
                    wmpPlayer.Ctlcontrols.stop();
                    timelineTimer.Enabled = false;

                    if (dgvPlayerList.Rows.Count > 0)
                    {
                        if (AutoCreateBackupsToolStripMenuItem.Checked)
                        {
                            autoBackupFileName = "Auto Backup - " + DateTime.Now.ToShortDateString().Replace('/', '-') + " " + DateTime.Now.ToShortTimeString().Replace(':', '-');
                            CreateBackupToolStripMenuItem_Click(this, EventArgs.Empty);
                            autoBackupFileName = null;
                        }

                        if (PreservePlayerlistToolStripMenuItem.Checked)
                        {
                            WritePreservedFiles();
                        }
                    }

                    pnlTimeCounter.Dispose();
                    pnlControlPanel.Dispose();
                    pcbCover.Dispose();
                    tkbVolume.Dispose();
                    tkbTimeline.Dispose();
                    dgvPlayerList.Dispose();
                    wmpPlayer.Dispose();
                }
            }
            catch (Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
        private void WritePreservedFiles()
        {
            try
            {
                List<string> preserveList = dgvPlayerList.Rows
                                .OfType<DataGridViewRow>()
                                .Select(r => r.Cells[5].Value.ToString())
                                .ToList();

                if (MusicToolStripMenuItem.Checked)
                {
                    preserveList.Add("Music");
                    Settings.Default.PlayerMode = "music";
                }
                else
                {
                    preserveList.Add("Video");
                    Settings.Default.PlayerMode = "video";
                }

                Settings.Default.Save();

                File.WriteAllLines(resourcePath + @"Preserved\Preserved.txt", preserveList);
            }
            catch(Exception ex)
            {
                PlayerMessageBox message;

                if (MusicToolStripMenuItem.Checked)
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
    }

    public class PlayerFile
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Duration { get; set; }
        public int Seconds { get; set; }
        public string Path { get; set; }
    }

    public class FileStatistic
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int XTouched { get; set; } = 1;
    }    
}
