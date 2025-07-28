using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;

namespace CuteKM
{
    public partial class UmaruForm : Form
    {
        private PictureBox spriteBox;
        private System.Windows.Forms.Timer frameTimer;
        private System.Windows.Forms.Timer stateCheckTimer;
        private NotifyIcon trayIcon;

        private IKeyboardMouseEvents? globalHook;

        private List<Image> typingFrames = new();
        private List<Image> watchingFrames = new();
        private List<Image> mouseFrames = new();
        private List<Image> idleFrames = new();

        private int currentFrameIndex = 0;
        private string currentState = "watching";

        private DateTime lastTypingTime = DateTime.MinValue;
        private DateTime lastMouseTime = DateTime.MinValue;
        private DateTime lastInputTime = DateTime.MinValue;

        private readonly Size fixedSize = new(300, 180);
        private Point normalLocation;

        public UmaruForm()
        {
            InitializeComponent();
            this.ShowInTaskbar = false; // Hide from taskbar

            InitializeUI();
            LoadFrames();
            SetupHooks();
            StartStateTimer();
            StartFrameTimer();
            SetupTray();

            this.Location = GetBottomLeftLocation();
            normalLocation = this.Location;
            this.Hide(); // Start hidden, tray-only
        }

        private void InitializeUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            this.Size = fixedSize;

            spriteBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.StretchImage,
            };
            spriteBox.MouseDown += SpriteBox_MouseDown;
            spriteBox.MouseMove += SpriteBox_MouseMove;
            spriteBox.MouseUp += SpriteBox_MouseUp;

            this.Controls.Add(spriteBox);
        }

        private Point GetBottomLeftLocation()
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            return new Point(10, workingArea.Bottom - fixedSize.Height - 50);
        }

        private void SetupTray()
        {
            trayIcon = new NotifyIcon
            {
                Icon = Properties.Resources.Umaru,
                Visible = true,
                Text = "Umaru Assistant"
            };
            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("Show / Hide", null, (s, e) => ShowWindow());
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Application.Exit());
            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) ShowWindow();
            };

            this.FormClosing += (s, e) => trayIcon.Dispose();
        }

        private void ShowWindow()
        {
            if (!this.Visible)
            {
                this.Location = normalLocation;
                this.Show();
                this.Activate();
            }
            else
            {
                this.Hide();
            }
        }

        private Point mouseOffset;
        private bool dragging = false;

        private void SpriteBox_MouseDown(object? sender, MouseEventArgs e)
        {
            dragging = true;
            mouseOffset = new Point(-e.X, -e.Y);
        }

        private void SpriteBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point mousePos = Control.MousePosition;
                mousePos.Offset(mouseOffset.X, mouseOffset.Y);
                Location = mousePos;
                normalLocation = Location;
            }
        }

        private void SpriteBox_MouseUp(object? sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void LoadFrames()
        {
            for (int i = 1; i <= 15; i++)
                typingFrames.Add((Image)Properties.Resources.ResourceManager.GetObject($"umaru_{i}")!);

            for (int i = 1; i <= 5; i++)
                watchingFrames.Add((Image)Properties.Resources.ResourceManager.GetObject($"watching_{i}")!);

            for (int i = 1; i <= 18; i++)
                mouseFrames.Add((Image)Properties.Resources.ResourceManager.GetObject($"mouse_{i}")!);

            for (int i = 1; i <= 12; i++)
                idleFrames.Add((Image)Properties.Resources.ResourceManager.GetObject($"idle_{i}")!);
        }

        private void SetupHooks()
        {
            globalHook = Hook.GlobalEvents();
            globalHook.KeyPress += (s, e) =>
            {
                lastTypingTime = DateTime.Now;
                lastInputTime = lastTypingTime;
                if (currentState != "typing")
                    SwitchState("typing");
            };

            globalHook.MouseMove += (s, e) =>
            {
                lastMouseTime = DateTime.Now;
                lastInputTime = lastMouseTime;

                if (currentState != "mouse" && (DateTime.Now - lastTypingTime).TotalSeconds > 2)
                    SwitchState("mouse");
            };
        }

        private void StartStateTimer()
        {
            stateCheckTimer = new System.Windows.Forms.Timer { Interval = 500 };
            stateCheckTimer.Tick += (s, e) =>
            {
                TimeSpan idleDuration = DateTime.Now - lastInputTime;

                if (idleDuration.TotalSeconds >= 120 && currentState != "idle")
                {
                    SwitchState("idle");
                }
                else if (currentState == "typing" && (DateTime.Now - lastTypingTime).TotalSeconds > 1)
                {
                    SwitchState("watching");
                }
                else if (currentState == "mouse" && (DateTime.Now - lastMouseTime).TotalSeconds > 1 && (DateTime.Now - lastTypingTime).TotalSeconds > 1)
                {
                    SwitchState("watching");
                }
            };
            stateCheckTimer.Start();
        }

        private void StartFrameTimer()
        {
            frameTimer = new System.Windows.Forms.Timer { Interval = 100 };
            frameTimer.Tick += (s, e) => UpdateFrame();
            frameTimer.Start();
        }

        private void UpdateFrame()
        {
            List<Image> currentFrames = currentState switch
            {
                "typing" => typingFrames,
                "watching" => watchingFrames,
                "mouse" => mouseFrames,
                "idle" => idleFrames,
                _ => watchingFrames
            };

            if (currentFrames.Count > 0)
            {
                spriteBox.Image = currentFrames[currentFrameIndex];
                currentFrameIndex = (currentFrameIndex + 1) % currentFrames.Count;
            }
        }

        private void SwitchState(string newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            currentFrameIndex = 0;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            globalHook?.Dispose();
            trayIcon.Dispose();
        }
    }
}
