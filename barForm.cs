using CoreAudioApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

namespace Lab2
{
    public partial class barForm : Form
    {
        Settings settings;

        public Color ConfigBackColor;
        public Color ConfigBarColor;

        public bool doUseAcc;

        public bool isLocked = false;

        private DateTime currentTime;
        private DateTime lastTime = new DateTime();
        private TimeSpan delay = new TimeSpan(0, 0, 0, 0, 100);

        private MMDevice device;
        private MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();

        private int speed = 0;
        private Point moveStart;
        private IniFile INI = new IniFile("config.ini");

        private double multiplier;
        private int maxSpeed;
        public double Multiplier
        {
            get => multiplier;
            set
            {
                if (value <= 1)
                    multiplier = 1;
                else if (value >= 100)
                    multiplier = 100;
                else multiplier = value;
            }
        }
        public int MaxSpeed
        {
            get => maxSpeed;
            set
            {
                if (value <= 1)
                    maxSpeed = 1;
                else if (value >= 100)
                    maxSpeed = 100;
                else maxSpeed = value;
            }
        }

        public barForm()
        {
            InitializeComponent();

            
            //
            //КОСТЫЛЬ
            //
            this.volumeBar.progressBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.barForm_MouseDown);
            this.volumeBar.progressBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.barForm_MouseMove);
            //
            //КОСТЫЛЬ 
            //

            

            
        }

        void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            if (volumeBar.InvokeRequired)
            {
                volumeBar.Invoke(new MethodInvoker(delegate
                {
                    volumeBar.Value = (int)(data.MasterVolume * 100);
                }));
            }
            else
            {
                volumeBar.Value = (int)(data.MasterVolume * 100);
            }
        }

        private void barForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                moveStart = new Point(e.X, e.Y);
            }
        }

        private void barForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (((e.Button & MouseButtons.Left) != 0) & !isLocked)
            {
                Point deltaPos = new Point(e.X - moveStart.X, e.Y - moveStart.Y);
                Location = new Point(Location.X + deltaPos.X, Location.Y + deltaPos.Y);
            }
        }

        private void volumeBar_MouseWheel(object sender, MouseEventArgs e)
        {
            if (doUseAcc)
            {
                if (e.Delta > 0)
                {
                    if (currentTime.Subtract(lastTime) > delay || speed <= 0) speed = 1;
                    volumeBar.Value += speed;
                    if (speed < MaxSpeed) speed *= (int)Multiplier;

                }
                else
                {
                    if (currentTime.Subtract(lastTime) > delay || speed >= 0) speed = -1;
                    volumeBar.Value += speed;
                    if (speed > -MaxSpeed) speed *= (int)Multiplier;

                }
                lastTime = currentTime;
                device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)volumeBar.Value / 100;
            }
            else
            if (e.Delta > 0)
            {
                volumeBar.Value += 2;
                device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)volumeBar.Value / 100;
            }
            else
            {
                volumeBar.Value -= 2;
                device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)volumeBar.Value / 100;
            }

        }

        private void load_Config(Settings settings)
        {
            if (INI.KeyExists("Size", "Width"))
                Width = int.Parse(INI.ReadINI("Size", "Width"));
            else
                Width = 200;
            volumeBar.Width = Width;
            volumeBar.progressBox.Width = Width;

            if (INI.KeyExists("Size", "Height"))
                Height = int.Parse(INI.ReadINI("Size", "Height"));
            else
                Height = 27;
            volumeBar.Height = Height;
            volumeBar.progressBox.Height = Height;

            if (INI.KeyExists("Location", "X"))
                Location = new System.Drawing.Point(int.Parse(INI.ReadINI("Location", "X")), Location.Y);
            else
                Location = new System.Drawing.Point(100, Location.Y);

            if (INI.KeyExists("Location", "Y"))
                Location = new System.Drawing.Point( Location.X, int.Parse(INI.ReadINI("Location", "Y")));
            else
                Location = new System.Drawing.Point(Location.X,100);

            if (INI.KeyExists("Colors", "BackColor"))
                volumeBar.BackColor = Color.FromArgb(int.Parse(INI.ReadINI("Colors", "BackColor")));
            else
                volumeBar.BackColor = System.Drawing.SystemColors.Control;
            ConfigBackColor = volumeBar.BackColor;

            if (INI.KeyExists("Colors", "BarColor"))
                volumeBar.progressBox.BackColor = Color.FromArgb(int.Parse(INI.ReadINI("Colors", "BarColor")));
            else
                volumeBar.progressBox.BackColor = System.Drawing.SystemColors.ActiveCaption;
            ConfigBarColor = volumeBar.progressBox.BackColor;

            if (INI.KeyExists("AccSpeed", "UseAcc"))
                doUseAcc = bool.Parse(INI.ReadINI("AccSpeed", "UseAcc"));
            else
                doUseAcc = true;

            if (INI.KeyExists("AccSpeed", "MaxSpeed"))
                MaxSpeed = int.Parse(INI.ReadINI("AccSpeed", "MaxSpeed"));
            else
                maxSpeed = 18;

            if (INI.KeyExists("AccSpeed", "Multiplier"))
                Multiplier = double.Parse(INI.ReadINI("AccSpeed", "Multiplier"));
            else
                multiplier = 2.8;

            settings.load_Config();
        }

        private void save_Config()
        {
            INI.Write("Size", "Width", Width.ToString());
            INI.Write("Size", "Height", Height.ToString());

            INI.Write("Location", "X",Location.X.ToString());
            INI.Write("Location", "Y",Location.Y.ToString());

            INI.Write("Colors", "BackColor",volumeBar.BackColor.ToArgb().ToString());
            INI.Write("Colors", "BarColor",volumeBar.progressBox.BackColor.ToArgb().ToString());

            INI.Write("AccSpeed", "UseAcc",doUseAcc.ToString());
            INI.Write("AccSpeed", "MaxSpeed",MaxSpeed.ToString());
            INI.Write("AccSpeed", "Multiplier", Multiplier.ToString());
        }

        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void barForm_SizeChanged(object sender, EventArgs e)
        {
            volumeBar.Size = ClientSize;

        }

        public void changeVolumeBackColor(Color color)
        {
            volumeBar.BackColor = color;
        }

        public void changeVolumeBarColor(Color color)
        {
            volumeBar.progressBox.BackColor = color;
        }

        private void barForm_Load(object sender, EventArgs e)
        {
            settings = new Settings(this);
            settings.Show();
            settings.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click);
            settings.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            settings.btn_Reset.Click += new System.EventHandler(this.btn_Reset_Click);
            load_Config(settings);

            device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            device.AudioEndpointVolume.OnVolumeNotification += new AudioEndpointVolumeNotificationDelegate(AudioEndpointVolume_OnVolumeNotification);
            volumeBar.Value = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);

            
        }

        private void btn_Reset_Click(object sender, EventArgs e)
        {
            load_Config(settings);
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            isLocked = true;
            save_Config();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.Show();
        }

        private void barForm_LocationChanged(object sender, EventArgs e)
        {

        }
    }
}
