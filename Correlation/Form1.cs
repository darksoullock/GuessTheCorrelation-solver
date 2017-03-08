using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Correlation
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        System.Windows.Forms.Timer timer;

        double sd(IEnumerable<double> someDoubles)
        {
            double average = someDoubles.Average();
            double sumOfSquaresOfDifferences = someDoubles.Select(val => (val - average) * (val - average)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / someDoubles.Count());
        }


        private const int timeBetween = 500;
        private void Form1_Load(object sender, EventArgs e)
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += (se, ev) => DoIt();
        }

        // size of plot in pixels
        static int w = 328;
        static int h = 324;

        // top left corner of the plot in pixels
        int left = 708;
        int top = 200;

        static Bitmap img = new Bitmap(w, h);
        static Bitmap life = new Bitmap(1, 1);
        static Bitmap loaded = new Bitmap(1, 1);

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private void button1_Click(object sender, EventArgs e)
        {
            timer.Enabled = !timer.Enabled;
        }

        void DoIt()
        {
            // browser process name
            var browserName = "Vivaldi";
            var ps = Process.GetProcessesByName(browserName).First(i => i.MainWindowTitle.StartsWith("Guess"));
            var handle = ps.MainWindowHandle;

            if (handle == IntPtr.Zero)
            {
                timer.Stop();
                MessageBox.Show($"{browserName} browser is not running.");
                return;
            }

            SetForegroundWindow(handle);

            Graphics.FromImage(loaded).CopyFromScreen(left, top, 0, 0, new Size(1, 1));
            if (loaded.GetPixel(0, 0).B > 127 && loaded.GetPixel(0, 0).G < 127)
            {
                SendKeys.SendWait("~");
                return;
            }

            var g = Graphics.FromImage(img);

            Graphics.FromImage(img).CopyFromScreen(296, 192, 0, 0, new Size(w, h));

            var points = new List<Point>();
            for (int i = 1; i < w; i += 1)
            {
                for (int j = 1; j < h; j += 1)
                {
                    if (Color.FromArgb(255, img.GetPixel(i, j)).GetBrightness() < 0.6)
                    {
                        points.Add(new Point(i, j));
                        g.DrawRectangle(Pens.White, i - 1, j - 1, 6, 6);
                    }
                }
            }

            int n = points.Count();
            var sum = points.Aggregate((i, j) => new Point(i.X + j.X, i.Y + j.Y));
            var avg = new { X = sum.X / ((double)n), Y = sum.Y / ((double)n) };
            var sx = sd(points.Select(i => (double)i.X));
            var sy = sd(points.Select(i => (double)i.Y));
            var s = sx * sy;
            var inm = points.Sum(i => (i.X - avg.X) * (i.Y - avg.Y));
            var r = Math.Abs(inm / (s * (n - 1)));

            button1.Text = Math.Round(r, 2).ToString();

            string nums = nums = Math.Round(r, 2).ToString();
            if (nums.Length < 2)
                nums = "0";
            else
                nums = nums.Substring(2);

            if (r > 0.98)
                nums = "99";

            foreach (var num in nums)
            {
                SendKeys.SendWait(num.ToString());
            }

            Graphics.FromImage(life).CopyFromScreen(340, 100, 0, 0, new Size(1, 1));
            if (life.GetPixel(0, 0).G < 127)
            {
                Text = nums;
                SendKeys.SendWait("~");
                Thread.Sleep(timeBetween);
                Graphics.FromImage(life).CopyFromScreen(340, 100, 0, 0, new Size(1, 1));
                if (life.GetPixel(0, 0).G < 127)
                    SendKeys.SendWait("~");
            }
            else
            {
                timer.Stop();
            }

            //// show points -- for debug

            //foreach (var i in points)
            //{
            //    g.DrawEllipse(Pens.Red, i.X, i.Y, 3, 3);
            //}

            //Form form = new Form();
            //form.BackgroundImage = img;
            //form.Width = w;
            //form.Height = h;
            //form.Update();
            //form.Show();
        }
    }
}
