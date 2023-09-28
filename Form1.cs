using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace magic_win
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);

        private Boolean shortcutkeys = false;
        private Dictionary<string, TextBox> textBoxes;
        private Dictionary<string, List<Keys>> shortcutConfigs;
        private string configFilePath = "按键配置.txt";


        private TextBox textBox1;
        private TextBox textBox2;
        private TextBox textBox3;
        private TextBox textBox4;
        private TextBox textBox5;

        //工具栏
        private ToolStrip toolbar;

        private Bitmap modifiedImage; // 用于存储涂鸦后的图像
        private Point startPoint; // 用于记录鼠标按下的起始点
        private Point endPoint;  // 用于记录鼠标放开的终点
        private Rectangle selectionRect; // 用于记录当前矩形框的位置和大小
        private List<Point> points; // 用于记录涂鸦的点集合
        private int mod = 0;  //工具模式

        //取色器
        private bool isColorPickerActive = false;
        private string colorValue; // 存储颜色值的变量

        // 鼠标钩子回调函数
        private static MouseHookCallback mouseHookCallback;
        private delegate IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam);
        private static IntPtr mouseHookHandle;
        // 鼠标事件类型
        private const int WM_LBUTTONDOWN = 0x0201;



        private Point mouse_Down;
        private Point mouse_Up;
        private Point mouse_Click;

        private int screen_width = Screen.PrimaryScreen.Bounds.Width;
        private int screen_height = Screen.PrimaryScreen.Bounds.Height;

        private int picture_width = 0;
        private int picture_height = 0;

        private bool cropped_or = false;

        private bool cropped_ture = false;

        private bool draw_or = false;

        private bool topping = false;

        private bool usetool = false; // 记录当前是否处于工具栏状态

        private bool isDrawing = false; // 记录当前是否处于涂鸦状态

        private int zoom = 0;

        private bool ck_down_or = false;

        private bool popup_options = true;

        private Bitmap bitmap_new;

        private int save_id = 0;



        public Form1()
        {
            InitializeComponent();
            this.Load += init;
        }

        //初始化
        private void init(object sender, EventArgs e)
        {
            base.TopMost = true;
            base.Width = 60;
            base.Height = 60;
            this.pictureBox1.Width = 60;
            this.pictureBox1.Height = 60;
            base.Left = Screen.PrimaryScreen.Bounds.Width - 220;
            base.Top = 100;
            this.pictureBox1.AllowDrop = true;
            this.pictureBox1.Left = 40;
            this.pictureBox1.Top = -100;
            this.pictureBox1.Width = 10;
            this.pictureBox1.Height = 10;
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
            this.置顶ToolStripMenuItem.Text = "取消置顶";
            this.取色器ToolStripMenuItem.Text = "取色器(关闭)";

            // 初始化文本框字典
            textBoxes = new Dictionary<string, TextBox>
            {
                { "textBox1", textBox1 },
                { "textBox2", textBox2 },
                { "textBox3", textBox3 },
                { "textBox4", textBox4 },
                { "textBox5", textBox5 }
            };

            // 读取快捷键配置#1F1F1F#1F1F1F
            LoadShortcutConfigs();

        }

        private void Mouse_Down()
        {
            this.mouse_Down = Cursor.Position;
        }

        private void Mouse_Up()
        {

            this.mouse_Up = Cursor.Position;
        }

        private void Mouse_Click()
        {

            this.mouse_Click.X = Cursor.Position.X - base.Left;
            this.mouse_Click.Y = Cursor.Position.Y - base.Top;
        }

        private void Mouse_Drag()
        {

            this.Mouse_Down();
            base.Left = this.mouse_Down.X - this.mouse_Click.X;
            base.Top = this.mouse_Down.Y - this.mouse_Click.Y;
        }

        private void Cut_Fun()
        {
            this.cropped_ture = true;
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
            this.pictureBox1.Image = null;
            this.pictureBox2.Left = 10;
            this.Mouse_Up();
            bool flag = this.mouse_Down.X < this.mouse_Up.X && this.mouse_Down.Y < this.mouse_Up.Y;
            if (flag)
            {
                this.picture_width = this.screen_width - this.mouse_Down.X - (this.screen_width - this.mouse_Up.X);
                this.picture_height = this.screen_height - this.mouse_Down.Y - (this.screen_height - this.mouse_Up.Y);
                Bitmap image = new Bitmap(this.picture_width, this.picture_height);
                Graphics graphics = Graphics.FromImage(image);
                graphics.CopyFromScreen(this.mouse_Down.X, this.mouse_Down.Y, 0, 0, new Size(this.picture_width, this.picture_height));
                this.pictureBox1.Image = image;
                this.pictureBox1.Width = this.picture_width;
                this.pictureBox1.Height = this.picture_height;
                base.Width = this.picture_width;
                base.Height = this.picture_height;
                base.Left = this.mouse_Down.X - 10;
                base.Top = this.mouse_Down.Y - 10;
                this.pictureBox2.Left = this.picture_width - 15;
                this.pictureBox2.Top = -100;
            }

            string executablePath = Application.ExecutablePath;
            Process.Start(executablePath);

            Copy_Picture();
            CreateToolbar();
        }


        private void CreateToolbar()
        {

            //太小不显示工具栏
            if (this.Height < 80 || this.Width < 80)
            {
                return;
            }

            // 工具栏
            toolbar = new ToolStrip();
            toolbar.ImageScalingSize = new Size(32, 32);

            ToolStripButton rectangleButton = new ToolStripButton();
            rectangleButton.Image = Properties.Resources.RectangleIcon;
            rectangleButton.ToolTipText = "矩形工具";
            rectangleButton.Click += rectangleButton_Click;

            ToolStripButton arrowButton = new ToolStripButton();
            arrowButton.Image = Properties.Resources.ArrowIcon;
            arrowButton.ToolTipText = "箭头工具";
            arrowButton.Click += arrowButton_Click;

            ToolStripButton brushButton = new ToolStripButton();
            brushButton.Image = Properties.Resources.BrushIcon;
            brushButton.ToolTipText = "画刷工具";
            brushButton.Click += brushButton_Click;


            ToolStripButton mosaicButton = new ToolStripButton();
            mosaicButton.Image = Properties.Resources.MosaicIcon;
            mosaicButton.ToolTipText = "马赛克工具";
            mosaicButton.Click += mosaicButton_Click;


            ToolStripButton finishButton = new ToolStripButton();
            finishButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            finishButton.Text = "完成";
            finishButton.Font = new Font(finishButton.Font.FontFamily, 16);
            finishButton.ToolTipText = "完成";
            finishButton.Click += finishButton_Click;


            toolbar.Items.Add(rectangleButton);
            toolbar.Items.Add(arrowButton);
            toolbar.Items.Add(brushButton);
            toolbar.Items.Add(mosaicButton);
            toolbar.Items.Add(finishButton);

            // 设置工具栏样式和位置
            toolbar.Dock = DockStyle.Bottom;
            toolbar.GripStyle = ToolStripGripStyle.Hidden;
            int toolbarHeight = toolbar.Height + 16;
            this.Height = this.Height + toolbarHeight;


            Controls.Add(toolbar);

            usetool = true;
        }

        private void rectangleButton_Click(object sender, EventArgs e)
        {
            mod = 1;
        }


        private void arrowButton_Click(object sender, EventArgs e)
        {
            mod = 2;
        }

        private void brushButton_Click(object sender, EventArgs e)
        {
            mod = 3;
        }

        private void pickerButton_Click(object sender, EventArgs e)
        {
            mod = 5;
        }

        private void mosaicButton_Click(object sender, EventArgs e)
        {
            mod = 4;
        }

        private void finishButton_Click(object sender, EventArgs e)
        {
            //完成按钮被点击
            usetool = false;
            // 查找并移除所有 ToolStrip 控件
            var toolStrips = Controls.OfType<ToolStrip>().ToList();
            foreach (var toolStrip in toolStrips)
            {
                Controls.Remove(toolStrip);
                toolStrip.Dispose();
            }

            // 更新窗体高度
            int toolbarHeight = toolStrips.Sum(ts => ts.Height);
            this.Height -= toolbarHeight;
            Copy_Picture();
        }


        private void Full_Capture()
        {
            Bitmap bitmap = new Bitmap(this.screen_width, this.screen_height);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, new Size(this.screen_width, this.screen_height));
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "图片文件|*.png";
            saveFileDialog.FileName = string.Concat(new string[]
            {
                "全屏截图_",
                DateTime.Now.Year.ToString(),
                DateTime.Now.Month.ToString(),
                DateTime.Now.DayOfYear.ToString(),
                DateTime.Now.Hour.ToString(),
                DateTime.Now.Minute.ToString(),
                DateTime.Now.Second.ToString()
            });
            bool flag = saveFileDialog.ShowDialog() == DialogResult.OK;
            if (flag)
            {
                bitmap.Save(saveFileDialog.FileName);
            }
        }

        private void Drawing()
        {
            this.mouse_Up.X = Cursor.Position.X;
            this.mouse_Up.Y = Cursor.Position.Y;
            this.picture_width = this.screen_width - this.mouse_Down.X - (this.screen_width - this.mouse_Up.X);
            this.picture_height = this.screen_height - this.mouse_Down.Y - (this.screen_height - this.mouse_Up.Y);
            this.pictureBox1.Invalidate();
        }

        private void Cutt_Full()
        {
            this.cropped_or = true;
            this.zoom = 0;
            this.pictureBox1.Image = null;
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
            Bitmap image = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(image);
            graphics.CopyFromScreen(0, 0, 0, 0, new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
            this.pictureBox1.Image = image;
            this.pictureBox1.Width = Screen.PrimaryScreen.Bounds.Width;
            this.pictureBox1.Height = Screen.PrimaryScreen.Bounds.Height;
            this.pictureBox1.Left = 0;
            this.pictureBox1.Top = 0;
            base.Width = Screen.PrimaryScreen.Bounds.Width;
            base.Height = Screen.PrimaryScreen.Bounds.Height;
            base.Left = 0;
            base.Top = 0;
            this.pictureBox2.Left = this.screen_width - 10;
            this.pictureBox2.Top = -100;
        }


        private void Zoom_Fun()
        {
            bool flag = this.cropped_ture;
            if (flag)
            {
                bool flag2 = this.zoom == 0;
                if (flag2)
                {
                    this.缩小ToolStripMenuItem.Text = "还原";
                    this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                    this.pictureBox2.Left = 38;
                    this.pictureBox2.Top = -100;
                    this.pictureBox1.Width = 60;
                    this.pictureBox1.Height = 60;
                    base.Width = 60;
                    base.Height = 60;
                    this.zoom = 1;
                }
                else
                {
                    this.缩小ToolStripMenuItem.Text = "缩小";
                    this.pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
                    this.pictureBox1.Width = this.picture_width;
                    this.pictureBox1.Height = this.picture_height;
                    base.Width = this.picture_width;
                    base.Height = this.picture_height;
                    this.zoom = 0;
                    this.pictureBox2.Left = this.picture_width - 15;
                    this.pictureBox2.Top = -100;
                }
            }
            else
            {
                MessageBox.Show("请先截图！", "提示！");
            }
        }

        private void Save_Picture()
        {
            bool flag = this.cropped_ture;
            if (flag)
            {
                this.pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "图片文件|*.png";
                this.save_id++;
                saveFileDialog.FileName = string.Concat(new object[]
                {
                    "浮动截图",
                    this.save_id,
                    "_",
                    DateTime.Now.Year.ToString(),
                    DateTime.Now.Month.ToString(),
                    DateTime.Now.DayOfYear.ToString(),
                    DateTime.Now.Hour.ToString(),
                    DateTime.Now.Minute.ToString(),
                    DateTime.Now.Second.ToString()
                });
                bool flag2 = saveFileDialog.ShowDialog() == DialogResult.OK;
                if (flag2)
                {
                    this.pictureBox1.Image.Save(saveFileDialog.FileName);
                }
            }
            else
            {
                MessageBox.Show("请先截图！", "提示！");
            }
        }

        private void Copy_Picture()
        {

            bool flag = this.cropped_ture;
            if (flag)
            {
                this.pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
                Clipboard.SetDataObject(this.pictureBox1.Image);
            }
            else
            {
                MessageBox.Show("请先截图！", "提示！");
            }
        }

        private void Topping()
        {
            bool flag = this.topping;
            if (flag)
            {
                this.置顶ToolStripMenuItem.Text = "取消置顶";
                base.TopMost = true;


                this.topping = false;


            }
            else
            {
                this.置顶ToolStripMenuItem.Text = "置顶";
                base.TopMost = false;
                this.topping = true;
            }
        }

        private void Form1_MouseDown_1(object sender, MouseEventArgs e)
        {

            this.Mouse_Down();
            this.Mouse_Click();
            this.draw_or = true;
            this.ck_down_or = true;
            this.popup_options = true;

            if (usetool)
            {
                if (e.Button == MouseButtons.Left)
                {
                    this.draw_or = false;
                    startPoint = e.Location;
                    isDrawing = true;
                    points = new List<Point>();
                    modifiedImage = new Bitmap(pictureBox1.Image);

                }
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {

            bool flag = this.cropped_or;
            if (flag)
            {
                bool sfch = this.draw_or;

                if (sfch)
                {
                    this.Drawing();
                }
            }
            else
            {
                bool flag2 = this.ck_down_or;
                if (flag2 && !usetool)
                {
                    this.Mouse_Drag();
                }
            }

            if (isDrawing)
            {
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(startPoint.X - e.X);
                int height = Math.Abs(startPoint.Y - e.Y);
                selectionRect = new Rectangle(x, y, width, height);
                endPoint = e.Location;

                points.Add(e.Location);

                // 刷新pictureBox1，触发Paint事件
                pictureBox1.Refresh();

                if (mod == 4)
                {
                    Graphics g = Graphics.FromImage(modifiedImage);
                    g.DrawImage(Properties.Resources.mosaic, e.X, e.Y, 20, 20); // 绘制马赛克方块
                    Graphics.FromImage(pictureBox1.Image).DrawImage(Properties.Resources.mosaic, e.X, e.Y, 20, 20); // 绘制马赛克方块
                }
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            this.draw_or = false;
            this.ck_down_or = false;
            bool flag = this.cropped_or;

            if (flag)
            {
                this.zoom = 0;
                this.cropped_or = false;
                this.Cut_Fun();
            }

            if (isDrawing)
            {
                // 停止涂鸦操作
                isDrawing = false;
                Graphics g = Graphics.FromImage(modifiedImage);

                using (Pen redPen = new Pen(Color.Red, 2))
                {
                    switch (mod)
                    {
                        case 1:

                            g.DrawRectangle(redPen, selectionRect);

                            break;
                        case 2:

                            // 计算箭头长度和角度
                            float arrowLength = 15;
                            float arrowAngle = (float)(Math.PI / 6); // 30度

                            // 计算箭头两侧点的位置
                            float dx = endPoint.X - startPoint.X;
                            float dy = endPoint.Y - startPoint.Y;
                            float angle = (float)Math.Atan2(dy, dx);
                            PointF arrowPoint1 = new PointF(endPoint.X - (float)(arrowLength * Math.Cos(angle - arrowAngle)), endPoint.Y - (float)(arrowLength * Math.Sin(angle - arrowAngle)));
                            PointF arrowPoint2 = new PointF(endPoint.X - (float)(arrowLength * Math.Cos(angle + arrowAngle)), endPoint.Y - (float)(arrowLength * Math.Sin(angle + arrowAngle)));

                            // 绘制箭头
                            g.DrawLine(redPen, startPoint, endPoint);
                            g.DrawLine(redPen, endPoint, arrowPoint1);
                            g.DrawLine(redPen, endPoint, arrowPoint2);

                            break;
                        case 3:
                            g.DrawLines(redPen, points.ToArray());
                            break;
                        default:

                            break;
                    }

                }

                // 清空矩形框和涂鸦内容
                selectionRect = Rectangle.Empty;
                points.Clear();

                // 刷新pictureBox1，清除绘制的矩形框和涂鸦内容
                pictureBox1.Refresh();

                pictureBox1.Image = modifiedImage;

            }

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            bool flag = this.popup_options;
            if (flag)
            {
                this.contextMenuStrip1.Show(this.pictureBox2, 14, -2);
                this.popup_options = false;
            }
            else
            {
                this.popup_options = true;
            }
        }

        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {

            bool sfch = this.draw_or;
            if (sfch)
            {
                using (Pen redPen = new Pen(Color.Red))
                    e.Graphics.DrawRectangle(redPen, this.mouse_Down.X - 2, this.mouse_Down.Y - 2, this.picture_width, this.picture_height);
            }

            if (isDrawing)
            {
                // 绘制截取的图像
                Graphics g = Graphics.FromImage(modifiedImage);
                {

                    using (Pen redPen = new Pen(Color.Red, 2))
                    {
                        switch (mod)
                        {
                            case 1:
                                // 绘制矩形框
                                if (!selectionRect.IsEmpty)
                                {
                                    {
                                        e.Graphics.DrawRectangle(redPen, selectionRect);
                                    }
                                }
                                break;
                            case 2:
                                // 绘制箭头  实时绘制显示
                                using (Pen pen = new Pen(Color.Red, 2))
                                {
                                    e.Graphics.DrawLine(pen, startPoint, endPoint);


                                    // 计算箭头长度和角度
                                    float arrowLength = 15;
                                    float arrowAngle = (float)(Math.PI / 6); // 30度

                                    // 计算箭头两侧点的位置
                                    float dx = endPoint.X - startPoint.X;
                                    float dy = endPoint.Y - startPoint.Y;
                                    float angle = (float)Math.Atan2(dy, dx);
                                    PointF arrowPoint1 = new PointF(endPoint.X - (float)(arrowLength * Math.Cos(angle - arrowAngle)), endPoint.Y - (float)(arrowLength * Math.Sin(angle - arrowAngle)));
                                    PointF arrowPoint2 = new PointF(endPoint.X - (float)(arrowLength * Math.Cos(angle + arrowAngle)), endPoint.Y - (float)(arrowLength * Math.Sin(angle + arrowAngle)));

                                    // 绘制箭头
                                    e.Graphics.DrawLine(pen, endPoint, arrowPoint1);
                                    e.Graphics.DrawLine(pen, endPoint, arrowPoint2);

                                }
                                break;
                            case 3:
                                // 绘制涂鸦内容
                                if (points.Count > 1)
                                {
                                    {
                                        e.Graphics.DrawLines(redPen, points.ToArray());
                                        
                                    }

                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            this.popup_options = true;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int num = this.picture_width / 5;
            int num2 = this.picture_height / 5;
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            bool flag = base.Width >= 50 && base.Height >= 50;
            if (flag)
            {
                bool flag2 = e.Delta > 0;
                if (flag2)
                {
                    this.pictureBox1.Width += num;
                    base.Width += num;
                    this.pictureBox1.Height += num2;
                    base.Height += num2;
                    this.pictureBox2.Left += num;
                }
                else
                {
                    this.pictureBox1.Width -= num;
                    base.Width -= num;
                    this.pictureBox1.Height -= num2;
                    base.Height -= num2;
                    this.pictureBox2.Left -= num;
                }
            }
            else
            {
                base.Width = 50;
                base.Height = 50;
                this.pictureBox2.Left = 39;
            }
        }


        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            base.Width = 50;
            base.Height = 50;
            this.pictureBox1.Width = 50;
            this.pictureBox1.Height = 50;
            this.pictureBox1.Image = null;
            string filename = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            this.bitmap_new = (Bitmap)Image.FromFile(filename);
            this.picture_width = this.bitmap_new.Width;
            this.picture_height = this.bitmap_new.Height;
            this.pictureBox1.Width = this.picture_width;
            this.pictureBox1.Height = this.picture_height;
            base.Width = this.picture_width;
            base.Height = this.picture_height;
            this.pictureBox1.Image = this.bitmap_new;
            this.zoom = 0;
            this.cropped_ture = true;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            bool dataPresent = e.Data.GetDataPresent(DataFormats.FileDrop);
            if (dataPresent)
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }


        private void 截图toolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Cutt_Full();
        }
        private void 全屏截图ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            this.Full_Capture();
        }

        private void 置顶ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Topping();
        }

        private void 保存图片ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Save_Picture();
        }
        private void 取色器ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isColorPickerActive == true)
            {
                isColorPickerActive = false;
                this.取色器ToolStripMenuItem.Text = "取色器(关闭)";
            }
            else
            {
                isColorPickerActive = true;
                this.取色器ToolStripMenuItem.Text = "取色器(开启)";

            }
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("版本0.9.4");
        }


        private void 关闭退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {

        }

        private void 缩小ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Zoom_Fun();

        }

        private void 设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Form settingsForm = new Form();
            // 设置子窗体的位置为父窗体位置下面
            settingsForm.StartPosition = FormStartPosition.Manual;
            settingsForm.Location = new Point(this.Location.X - 200, this.Location.Y + 50 + this.Height);
            // 将子窗体的Owner属性设置为父窗体
            settingsForm.Owner = this;

            settingsForm.Width = 300;
            settingsForm.Height = 240;
            settingsForm.Text = "设置全屏快捷键";

            Label label1 = new Label();
            label1.Text = "截图快捷键：";
            textBox1 = new TextBox();
            textBox1.Width += 30;
            textBox1.KeyDown += TextBox1_KeyDown;
            Label label2 = new Label();
            label2.Text = "全屏截图快捷键：";
            textBox2 = new TextBox();
            textBox2.Width += 30;
            textBox2.KeyDown += TextBox2_KeyDown;
            Label label3 = new Label();
            label3.Text = "置顶快捷键：";
            textBox3 = new TextBox();
            textBox3.Width += 30;
            textBox3.KeyDown += TextBox3_KeyDown;
            Label label4 = new Label();
            label4.Text = "缩小快捷键：";
            textBox4 = new TextBox();
            textBox4.Width += 30;
            textBox4.KeyDown += TextBox4_KeyDown;
            Label label5 = new Label();
            label5.Text = "保存快捷键：";
            textBox5 = new TextBox();
            textBox5.Width += 30;
            textBox5.KeyDown += TextBox5_KeyDown;

            // 初始化文本框字典 没学会传递
            textBoxes = new Dictionary<string, TextBox>
            {
                { "textBox1", textBox1 },
                { "textBox2", textBox2 },
                { "textBox3", textBox3 },
                { "textBox4", textBox4 },
                { "textBox5", textBox5 }
            };

            // 将读取的快捷键配置应用到文本框
            foreach (var entry in shortcutConfigs)
            {
                string textBoxName = entry.Key;
                List<Keys> shortcuts = entry.Value;

                TextBox textBox = textBoxes[textBoxName];
                string shortcutText = string.Join(" + ", shortcuts.Select(key => key.ToString()));
                textBox.Text = shortcutText;
            }

            label1.Location = new Point(10, 10);
            textBox1.Location = new Point(120, 10);
            label2.Location = new Point(10, 40);
            textBox2.Location = new Point(120, 40);
            label3.Location = new Point(10, 70);
            textBox3.Location = new Point(120, 70);
            label4.Location = new Point(10, 100);
            textBox4.Location = new Point(120, 100);
            label5.Location = new Point(10, 130);
            textBox5.Location = new Point(120, 130);

            settingsForm.Controls.Add(label1);
            settingsForm.Controls.Add(textBox1);
            settingsForm.Controls.Add(label2);
            settingsForm.Controls.Add(textBox2);
            settingsForm.Controls.Add(label3);
            settingsForm.Controls.Add(textBox3);
            settingsForm.Controls.Add(label4);
            settingsForm.Controls.Add(textBox4);
            settingsForm.Controls.Add(label5);
            settingsForm.Controls.Add(textBox5);

            Button confirmButton = new Button();
            confirmButton.Text = "确认";
            confirmButton.Location = new Point(60, 160);
            confirmButton.Click += ConfirmButton_Click;

            Button cancelButton = new Button();
            cancelButton.Text = "取消";
            cancelButton.Location = new Point(150, 160);
            cancelButton.Click += CancelButton_Click;

            settingsForm.Controls.Add(confirmButton);
            settingsForm.Controls.Add(cancelButton);

            settingsForm.ShowDialog();

        }


        private void SaveShortcutConfigs()
        {
            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                foreach (var entry in shortcutConfigs)
                {
                    string textBoxName = entry.Key;
                    List<Keys> shortcuts = entry.Value;

                    string shortcutText = string.Join(" + ", shortcuts.Select(key => key.ToString()));
                    writer.WriteLine($"{textBoxName}:{shortcutText}");
                }
            }
        }

        private void LoadShortcutConfigs()
        {
            shortcutConfigs = new Dictionary<string, List<Keys>>();

            // 检查配置文件是否存在
            if (File.Exists(configFilePath))
            {
                shortcutkeys = true;
                using (StreamReader reader = new StreamReader(configFilePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(':');
                        if (parts.Length == 2)
                        {
                            //截取
                            string textBoxName = parts[0].Trim();
                            string[] shortcutKeys = parts[1].Split('+').Select(key => key.Trim()).ToArray();

                            if (textBoxes.ContainsKey(textBoxName))
                            {
                                List<Keys> shortcuts = new List<Keys>();
                                foreach (string key in shortcutKeys)
                                {
                                    if (Enum.TryParse(key, out Keys keyCode))
                                    {
                                        shortcuts.Add(keyCode);
                                    }
                                }

                                shortcutConfigs[textBoxName] = shortcuts;
                            }
                        }
                    }
                }
            }


        }

        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            SaveShortcutConfigs();
            ((Button)sender).FindForm().Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            ((Button)sender).FindForm().Close();
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            bool isCtrlPressed = e.Control;
            bool isShiftPressed = e.Shift;
            bool isAltPressed = e.Alt;

            Keys keyCode = e.KeyCode;


            string shortcut = "";

            if (isCtrlPressed)
                shortcut += "Ctrl + ";
            if (isShiftPressed)
                shortcut += "Shift + ";
            if (isAltPressed)
                shortcut += "Alt + ";

            shortcut += keyCode.ToString();


            textBox1.Text = shortcut;

            shortcutConfigs["textBox1"] = new List<Keys> { Keys.Control, Keys.Shift, keyCode };
        }
        private void TextBox2_KeyDown(object sender, KeyEventArgs e)
        {

            bool isCtrlPressed = e.Control;
            bool isShiftPressed = e.Shift;
            bool isAltPressed = e.Alt;


            Keys keyCode = e.KeyCode;


            string shortcut = "";

            if (isCtrlPressed)
                shortcut += "Ctrl + ";
            if (isShiftPressed)
                shortcut += "Shift + ";
            if (isAltPressed)
                shortcut += "Alt + ";

            shortcut += keyCode.ToString();


            textBox2.Text = shortcut;

            shortcutConfigs["textBox2"] = new List<Keys> { Keys.Control, Keys.Shift, keyCode };


        }
        private void TextBox3_KeyDown(object sender, KeyEventArgs e)
        {
            bool isCtrlPressed = e.Control;
            bool isShiftPressed = e.Shift;
            bool isAltPressed = e.Alt;

            Keys keyCode = e.KeyCode;

            string shortcut = "";

            if (isCtrlPressed)
                shortcut += "Ctrl + ";
            if (isShiftPressed)
                shortcut += "Shift + ";
            if (isAltPressed)
                shortcut += "Alt + ";

            shortcut += keyCode.ToString();

            textBox3.Text = shortcut;
            shortcutConfigs["textBox3"] = new List<Keys> { Keys.Control, Keys.Shift, keyCode };
        }
        private void TextBox4_KeyDown(object sender, KeyEventArgs e)
        {
            bool isCtrlPressed = e.Control;
            bool isShiftPressed = e.Shift;
            bool isAltPressed = e.Alt;

            Keys keyCode = e.KeyCode;

            string shortcut = "";

            if (isCtrlPressed)
                shortcut += "Ctrl + ";
            if (isShiftPressed)
                shortcut += "Shift + ";
            if (isAltPressed)
                shortcut += "Alt + ";

            shortcut += keyCode.ToString();

            textBox4.Text = shortcut;
            shortcutConfigs["textBox4"] = new List<Keys> { Keys.Control, Keys.Shift, keyCode };

        }
        private void TextBox5_KeyDown(object sender, KeyEventArgs e)
        {
            bool isCtrlPressed = e.Control;
            bool isShiftPressed = e.Shift;
            bool isAltPressed = e.Alt;

            Keys keyCode = e.KeyCode;

            string shortcut = "";

            if (isCtrlPressed)
                shortcut += "Ctrl + ";
            if (isShiftPressed)
                shortcut += "Shift + ";
            if (isAltPressed)
                shortcut += "Alt + ";

            shortcut += keyCode.ToString();

            textBox5.Text = shortcut;

            shortcutConfigs["textBox5"] = new List<Keys> { Keys.Control, Keys.Shift, keyCode };
        }




        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (shortcutkeys)
            {

                if (keyData == (shortcutConfigs["textBox1"][0] | shortcutConfigs["textBox1"][1] | shortcutConfigs["textBox1"][2]))
                {

                    this.Cutt_Full();
                    return true;
                }
                if (keyData == (shortcutConfigs["textBox2"][0] | shortcutConfigs["textBox2"][1] | shortcutConfigs["textBox2"][2]))
                {

                    this.Full_Capture();
                    return true;
                }
                if (keyData == (shortcutConfigs["textBox3"][0] | shortcutConfigs["textBox3"][1] | shortcutConfigs["textBox3"][2]))
                {

                    this.Topping();
                    return true;
                }
                if (keyData == (shortcutConfigs["textBox4"][0] | shortcutConfigs["textBox4"][1] | shortcutConfigs["textBox4"][2]))
                {

                    this.Zoom_Fun();
                    return true;
                }
                if (keyData == (shortcutConfigs["textBox5"][0] | shortcutConfigs["textBox5"][1] | shortcutConfigs["textBox5"][2]))
                {

                    this.Save_Picture();
                    return true;
                }
                if (keyData == Keys.Escape)
                {
                    base.Close();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }





        // 导入用户32.dll库，用于设置鼠标钩子和卸载鼠标钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr SetWindowsHookEx(int idHook, MouseHookCallback lpfn, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 安装鼠标钩子
            InstallMouseHook();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // 卸载鼠标钩子
            UninstallMouseHook();
        }

        private void InstallMouseHook()
        {
            // 创建鼠标钩子回调函数
            mouseHookCallback = MouseHookCallbackFunction;

            // 获取当前进程的实例句柄
            IntPtr hInstance = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);

            // 设置鼠标钩子
            mouseHookHandle = SetWindowsHookEx(14, mouseHookCallback, hInstance, 0);
        }

        private void UninstallMouseHook()
        {
            // 卸载鼠标钩子
            if (mouseHookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHookHandle);
            }
        }

        private IntPtr MouseHookCallbackFunction(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                if (isColorPickerActive)
                {
                    // 获取坐标
                    Point clickLocation = Cursor.Position;

                    // 获取像素颜色
                    Color pixelColor = GetPixelColor(clickLocation);

                    // 复制到剪贴板
                    Clipboard.SetText($"#{pixelColor.R:X2}{pixelColor.G:X2}{pixelColor.B:X2}");

                    // 取消取色器激活状态
                    isColorPickerActive = false;
                    this.取色器ToolStripMenuItem.Text = "取色器(关闭)";
                }

            }

            // 继续传递鼠标事件
            return CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
        }

        private Color GetPixelColor(Point location)
        {
            // 获取屏幕上指定位置的像素颜色
            Bitmap screenBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            using (Graphics g = Graphics.FromImage(screenBitmap))
            {
                g.CopyFromScreen(0, 0, 0, 0, screenBitmap.Size);
            }
            return screenBitmap.GetPixel(location.X, location.Y);
        }
    }
}
