using System;
using System.Drawing;
using System.Windows.Forms;

namespace ITD_Baner_Creator
{
    public partial class BannerEditorForm : Form
    {
        public Bitmap EditedBanner { get; private set; }

        private Bitmap originalImage;
        private Bitmap scaledImage;
        private Rectangle cropRect = new Rectangle(0, 0, 186, 62);

        private bool isDragging = false;
        private Point dragStart;

        public BannerEditorForm()
        {
            InitializeComponent();

            pictureBox1.Paint += PictureBox1_Paint;
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseUp += PictureBox1_MouseUp;

            btnLoad.Click += BtnLoad_Click;
            btnDone.Click += BtnDone_Click;
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            originalImage = new Bitmap(ofd.FileName);

            int newWidth = 186;
            float scale = (float)newWidth / originalImage.Width;
            int newHeight = (int)(originalImage.Height * scale);

            scaledImage = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(scaledImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }

            pictureBox1.Image = scaledImage;
            pictureBox1.Width = newWidth;
            pictureBox1.Height = newHeight;
            cropRect = new Rectangle(0, 0, 186, 62);
            pictureBox1.Invalidate();
        }

        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (scaledImage == null) return;

            using (Pen pen = new Pen(Color.Red, 2))
                e.Graphics.DrawRectangle(pen, cropRect);

            using (Brush b = new SolidBrush(Color.FromArgb(100, Color.Black)))
            {
                e.Graphics.FillRectangle(b, 0, 0, pictureBox1.Width, cropRect.Y);
                e.Graphics.FillRectangle(b, 0, cropRect.Bottom, pictureBox1.Width, pictureBox1.Height - cropRect.Bottom);
            }
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (cropRect.Contains(e.Location))
            {
                isDragging = true;
                dragStart = e.Location;
            }
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || scaledImage == null) return;

            int dy = e.Y - dragStart.Y;
            cropRect.Y += dy;

            if (cropRect.Y < 0) cropRect.Y = 0;
            if (cropRect.Bottom > scaledImage.Height) cropRect.Y = scaledImage.Height - cropRect.Height;

            dragStart = e.Location;
            pictureBox1.Invalidate();
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void BtnDone_Click(object sender, EventArgs e)
        {
            if (scaledImage == null) return;

            EditedBanner?.Dispose();
            EditedBanner = new Bitmap(cropRect.Width, cropRect.Height);
            using (Graphics g = Graphics.FromImage(EditedBanner))
            {
                g.DrawImage(scaledImage, new Rectangle(0, 0, cropRect.Width, cropRect.Height),
                            cropRect, GraphicsUnit.Pixel);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
