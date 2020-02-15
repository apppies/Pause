using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PauseForms
{
    public partial class Form1 : Form
    {
        protected override bool ShowWithoutActivation => true;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.TopMost = true;

            LoadTrayIcon();
            LoadSettings();
            StartSchedule();
        }

        private class Entry
        {
            public int Interval { get; set; } = int.MaxValue;
            public int Display { get; set; } = 0;
        }

        private NotifyIcon notifyIcon;
        private ContextMenu contextMenu;
        private MenuItem menuItem1;
        private Icon pauseIcon;
        private Icon playIcon;

        private void LoadTrayIcon()
        {
            this.contextMenu = new ContextMenu();
            this.menuItem1 = new MenuItem();

            // Initialize contextMenu1
            this.contextMenu.MenuItems.AddRange(
                        new MenuItem[] { this.menuItem1 }
                        );

            // Initialize menuItem1
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "E&xit";
            this.menuItem1.Click += new EventHandler(this.menuItem1_Click);

            // Create the NotifyIcon.
            this.notifyIcon = new NotifyIcon(this.components);

            // The Icon property sets the icon that will appear
            // in the systray for this application.
            playIcon = new Icon("Play.ico");
            pauseIcon = new Icon("Pause.ico");
            notifyIcon.Icon = playIcon;

            // The ContextMenu property sets the menu that will
            // appear when the systray icon is right clicked.
            notifyIcon.ContextMenu = this.contextMenu;

            // The Text property sets the text that will be displayed,
            // in a tooltip, when the mouse hovers over the systray icon.
            notifyIcon.Text = "Pause";
            notifyIcon.Visible = true;
        }

        private void menuItem1_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            this.Close();
            Application.Exit();
        }

        Dictionary<int, Entry> timings = new Dictionary<int, Entry>();

        private void LoadSettings()
        {
            if (!System.IO.File.Exists("Settings.cfg"))
                return;

            using (var re = new System.IO.StreamReader("Settings.cfg"))
            {
                var rx = new Regex(@"^([a-z]+)(\d+)=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

                var matches = rx.Matches(re.ReadToEnd());
                if (matches.Count > 0)
                    timings.Clear();

                foreach (Match match in matches)
                {
                    var group = match.Groups;

                    Debug.WriteLine($"Found group {group[0]}");

                    // Get and validate index
                    var index = -1;
                    if (!int.TryParse(group[2].Value, out index))
                    {
                        continue;
                    }

                    // Add entry if non existant index
                    if (!timings.ContainsKey(index))
                    {
                        timings.Add(index, new Entry());
                    }

                    // Fill entry
                    if (int.TryParse(group[3].Value, out int result))
                    {
                        if (group[1].Value == "interval")
                        {
                            timings[index].Interval = result;
                        }
                        else if (group[1].Value == "display")
                        {
                            timings[index].Display = result;
                        }
                    }
                }
            }
        }

        private void StartSchedule()
        {
            countDownTimer.Interval = 16; // 25 FPS
            countDownTimer.Tick += CountDownTimer_Tick;

            scheduleTimer.Tick += ScheduleTimer_Tick;

            scheduleTimer.Interval = 1000;
            scheduleTimer.Start();
        }

        DateTime displayStartTime;
        DateTime displayEndTime;
        int totalTime = 0;
        readonly int margin = 100;

        private void ScheduleTimer_Tick(object sender, EventArgs e)
        {
            // Update time
            totalTime++;
            totalTime %= (365 * 24 * 60 * 60); // Limit to one year of seconds to prevent overflows

            // Get all entries that want to display
            var toRun = timings.Values.Where(entry => totalTime % entry.Interval == 0);
            if (toRun.Count() == 0)
                return;

            // Select longest running one
            var maxDisplay = toRun.Max(entry => entry.Display);

            // Update display time if needed
            var newEndTime = DateTime.Now.AddSeconds(maxDisplay);
            if (displayEndTime < newEndTime)
                displayEndTime = newEndTime;

            // Set and show window
            this.Left = margin;
            this.Top = 30;
            this.Width = Screen.PrimaryScreen.WorkingArea.Width - margin * 2;
            this.Height = 50;
            this.Show();

            // Set tray icon
            notifyIcon.Icon = pauseIcon;

            // Start count down
            if (!countDownTimer.Enabled)
            {
                displayStartTime = DateTime.Now;
                countDownTimer.Start();
            }
        }

        private void CountDownTimer_Tick(object sender, EventArgs e)
        {
            var remainder = (displayEndTime - DateTime.Now).TotalSeconds;
            if (remainder <= 0)
            {
                FinishCountDown();
            }
            this.Invalidate();
        }

        private void FinishCountDown()
        {
            countDownTimer.Stop();
            this.Hide();

            // Set tray icon
            notifyIcon.Icon = playIcon;
        }

        public double CountDown { get; set; }

        Font drawFont = new Font(FontFamily.GenericMonospace.Name, 24, FontStyle.Bold);
        Brush foregroundBrush = new SolidBrush(Color.FromArgb(40,200,60));
        Brush backgroundBrush = new SolidBrush(Color.FromArgb(40, 43, 42));
        // Draw the progess bar
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var w = this.Width;
            var h = this.Height;

            var remainder = (displayEndTime - DateTime.Now).TotalSeconds;
            var total = (displayEndTime - displayStartTime).TotalSeconds;

            e.Graphics.FillRectangle(backgroundBrush, new RectangleF(0, 0, w, h));
            if (remainder > 0)
            {
                e.Graphics.FillRectangle(foregroundBrush, new RectangleF(1, 1, (float)(remainder / total * w) - 2, h - 2));
                e.Graphics.DrawString(remainder.ToString("0.0"), drawFont, backgroundBrush, 2, 8);
            }
        }
    }
}
