﻿using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace Androids
{
    public partial class Form1 : Form
    {
        private const string CFG_FILE = "config.txt";
        private const string IMG_PATH = "_.png";
        private string adbPath = "adb.exe";
        private Point point = new Point(0, 0);
        private Size mobileSize = new Size(1080, 1920);
        private AdbRunner adbRunner;
        private bool isLoadAdb = false;
        private bool isAdbSet = false;
        private bool isMobileConnect = false;
        private double picRate = 1;
        private readonly System.Timers.Timer timer;
        private int count = 0;
        private DriveDetector detector;
        private bool isRunning = false;
        private Point boxLocaltion = new Point(520, 1064);
        private Point backLocaltion = new Point(520, 1650);

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            timer = new System.Timers.Timer();
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            btnAdb.Visible = false;
            pnlMain.Enabled = false;
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            this.Shown += Form1_Load;
            this.btnAdb.Click += BtnAdb_Click;
            this.btnChoose.Click += BtnChoose_Click;
            this.pb.MouseClick += Pb_Click;

            txtIP.Visible = false;
            btnConnect.Visible = false;
            chkWifi.CheckedChanged += ChkWifi_CheckedChanged;
            btnConnect.Click += BtnConnect_Click;

            this.btnStart.Click += BtnStart_Click;
            this.btnPause.Click += BtnPause_Click;
            this.tbSplit.ValueChanged += TbSplit_ValueChanged;
            this.tbSplit.Value = 6;
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (chkWifi.Checked)
            {
                if (isLoadAdb)
                {
                    var ip = txtIP.Text.Trim();

                    var flag = adbRunner.Connect(ip, 5555);
                    if (flag)
                    {
                        btnConnect.Text = "已连接";
                        GetMobileInfo();
                    }
                    else
                    {
                        MessageBox.Show("连接失败。");
                    }
                }
                else
                {
                    MessageBox.Show("ADB库文件未加载。");
                }
            }
            ChangeState();
        }

        private void ChkWifi_CheckedChanged(object sender, EventArgs e)
        {
            btnConnect.Visible = txtIP.Visible = chkWifi.Checked;
            if (!chkWifi.Checked)
            {
                btnConnect.Text = "连接";
                detector = new DriveDetector();
                detector.UsbChanged += Detector_UsbChanged;
            }
        }

        private void Detector_UsbChanged(object sender, EventArgs e)
        {
            Thread.Sleep(1000);
            GetMobileInfo();
            ChangeState();
        }

        private void TbSplit_ValueChanged(object sender, EventArgs e)
        {
            if (this.tbSplit.Value == 0)
                lblTick.Text = "500豪秒";
            else
                lblTick.Text = this.tbSplit.Value + "秒";
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            count++;
            lblCount.Text = count.ToString();
            this.adbRunner.Tap(this.backLocaltion.X, this.backLocaltion.Y);
            if (!checkBox1.Checked)
            {
                this.adbRunner.Tap(this.boxLocaltion.X, this.boxLocaltion.Y);
            }

            this.adbRunner.Tap(this.point.X, this.point.Y);
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            this.timer.Stop();
            isRunning = false;
            ChangeState();
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            this.adbRunner.Tap(this.point.X, this.point.Y);

            if (this.tbSplit.Value == 0)
                this.timer.Interval = 500;
            else
                this.timer.Interval = this.tbSplit.Value * 1000;
            this.timer.Start();
            isRunning = true;
            ChangeState();
        }

        private void ChangeState()
        {
            this.btnStart.Enabled = !isRunning;
            //this.btnChoose.Enabled = !isRunning;
            this.tbSplit.Enabled = !isRunning;
            this.checkBox1.Enabled = !isRunning;
            this.btnPause.Enabled = isRunning;
            this.pnlMain.Enabled = isMobileConnect;
            if (isRunning)
            {
                if (isMobileConnect)
                {
                    this.timer.Start();
                }
                else
                {
                    this.timer.Stop();
                }
            }
        }

        private void Pb_Click(object sender, MouseEventArgs e)
        {
            if (pb.Image == null)
                return;

            var x = e.X * picRate;
            var y = e.Y * picRate;
            point = new Point((int)x, (int)y);
            this.lblLocation.Text = point.ToString();
        }

        private void BtnChoose_Click(object sender, EventArgs e)
        {
            SetScreen();
        }

        private void SetScreen()
        {
            pb.Image = null;
            var imagePath = adbRunner.GetShotScreent(IMG_PATH);
            pb.Image = FromFile(imagePath);
        }

        private Image FromFile(string file)
        {
            if (File.Exists(file))
            {
                using (var stream = File.Open(file, FileMode.Open))
                {
                    return Image.FromStream(stream);
                }
            }

            return null;
        }

        private void CreateAdbRunner()
        {
            if (File.Exists(adbPath))
            {
                adbRunner = new AdbRunner(adbPath);
                isLoadAdb = true;
            }
            else
            {
                MessageBox.Show("ADB库文件不存在。" + Environment.NewLine + adbPath, "提示");
            }
        }

        private void GetMobileInfo()
        {
            var text = adbRunner.GetMobileInfo();
            if (string.IsNullOrWhiteSpace(text))
            {
                isMobileConnect = false;
                this.Text = "没有连接手机";
            }
            else
            {
                btnAdb.Visible = false;
                isMobileConnect = true;
                this.mobileSize = adbRunner.GetMobileSize();
                this.Text = text + this.mobileSize;

                picRate = this.mobileSize.Height * 1.0 / this.pb.Height;
                var width = this.mobileSize.Width / picRate;
                this.pb.Height = (int)width;

                if (pb.Image == null)
                {
                    SetScreen();
                }
            }
        }

        private void BtnAdb_Click(object sender, EventArgs e)
        {
            if (isLoadAdb)
            {
                GetMobileInfo();
            }
            else
            {
                ChooseAdb();
                LoadAdb();
            }
        }

        private void LoadAdb()
        {
            if (!isAdbSet)
            {
                ChooseAdb();
            }
            if (!isAdbSet)
            {
                btnAdb.Visible = true;
                isLoadAdb = false;
                isMobileConnect = false;
            }
            else
            {
                lblAdb.Text = "就绪。";
                pnlMain.Enabled = true;
                CreateAdbRunner();
                if (isLoadAdb)
                {
                    GetMobileInfo();
                }
            }
            ChangeState();
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            if (File.Exists(CFG_FILE))
            {
                var path = File.ReadAllText(CFG_FILE);
                if (File.Exists(path) && path.ToLower().EndsWith("exe"))
                {
                    adbPath = path;
                    isAdbSet = true;
                }
            }
            else if (File.Exists("adb.exe"))
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb.exe");
                File.WriteAllText(CFG_FILE, "adb.exe");
                adbPath = path;
                isAdbSet = true;
            }

            LoadAdb();
        }

        private void ChooseAdb()
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "adb库文件|*.exe",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            var dlr = dlg.ShowDialog(this);
            if (dlr == DialogResult.OK)
            {
                if (File.Exists(dlg.FileName))
                {
                    var path = dlg.FileName;
                    File.WriteAllText(CFG_FILE, path);
                    adbPath = path;
                    isAdbSet = true;
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (detector != null)
            {
                bool handled = false;
                detector.WndProc(m.HWnd, m.Msg, m.WParam, m.LParam, ref handled);
            }

            base.WndProc(ref m);
        }
    }
}
