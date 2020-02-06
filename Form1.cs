using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PauseForms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.TopMost = true;

            LoadTrayIcon();
            LoadSettings();
            StartSchedule();
        }

        protected override bool ShowWithoutActivation => true;

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenu contextMenu1;
        private System.Windows.Forms.MenuItem menuItem1;
        private void LoadTrayIcon()
        {
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();

            // Initialize contextMenu1
            this.contextMenu1.MenuItems.AddRange(
                        new System.Windows.Forms.MenuItem[] { this.menuItem1 });

            // Initialize menuItem1
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "E&xit";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);

            // Create the NotifyIcon.
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);

            // The Icon property sets the icon that will appear
            // in the systray for this application.
            notifyIcon1.Icon = new Icon("Play.ico");

            // The ContextMenu property sets the menu that will
            // appear when the systray icon is right clicked.
            notifyIcon1.ContextMenu = this.contextMenu1;

            // The Text property sets the text that will be displayed,
            // in a tooltip, when the mouse hovers over the systray icon.
            notifyIcon1.Text = "Pause";
            notifyIcon1.Visible = true;
        }

        private void menuItem1_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            this.Close();
            Application.Exit();
        }

        int interval1 = 5;// 20 * 60;
        int display1 = 3;//20;

        private void LoadSettings()
        {
            if (!System.IO.File.Exists("Settings.cfg"))
                return;

            using (var re = new System.IO.StreamReader("Settings.cfg"))
            {
                while (!re.EndOfStream)
                {
                    var line = re.ReadLine().Split(new char[] { '=' });
                    switch (line[0])
                    {
                        case "interval1":
                            int.TryParse(line[1], out interval1);
                            break;

                        case "display1":
                            int.TryParse(line[1], out display1);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void StartSchedule()
        {
            countDownTimer.Interval = 4;
            countDownTimer.Tick += CountDownTimer_Tick;

            scheduleTimer.Tick += ScheduleTimer_Tick;
            scheduleTimer.Interval = interval1 * 1000;
            scheduleTimer.Start();
        }

        int currentTask = -1;
        int totalDisplayTime = 0;
        DateTime displayEndTime;

        private void ScheduleTimer_Tick(object sender, EventArgs e)
        {
            currentTask++;

            // Setup 
            totalDisplayTime = display1;

            // Show window            
            this.Left = Screen.PrimaryScreen.WorkingArea.Width - this.Width;
            this.Top = 30;
            this.Show();

            // Set tray icon
            notifyIcon1.Icon = new Icon("Pause.ico");

            // Count down
            displayEndTime = DateTime.Now.AddSeconds(totalDisplayTime);
            countDownTimer.Start();
        }

        private void CountDownTimer_Tick(object sender, EventArgs e)
        {
            var deltaTime = (displayEndTime - DateTime.Now).TotalSeconds;
            if (deltaTime > 0)
                CountDown = deltaTime;
            else
            {
                CountDown = 0;
                FinishCountDown();
            }
            this.Invalidate();
        }

        private void FinishCountDown()
        {
            countDownTimer.Stop();
            this.Hide();
            // Set tray icon
            notifyIcon1.Icon = new Icon("Play.ico");
        }

        public double CountDown { get; set; }

        Font drawFont = new Font(FontFamily.GenericMonospace.Name, 16);
        SolidBrush drawBrush = new SolidBrush(Color.Black);

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var w = this.Width;
            var h = this.Height;
            e.Graphics.FillRectangle(Brushes.Green, new RectangleF(0, 0, (float)(CountDown / totalDisplayTime * w), h));
            e.Graphics.DrawString(CountDown.ToString("0.0"), drawFont, drawBrush, 2, 2);
        }
    }
}
