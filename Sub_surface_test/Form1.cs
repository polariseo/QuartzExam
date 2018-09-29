using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Globalization;
using System.Threading;
using GxIAPINET;
using GxIAPINET.Sample.Common;

namespace Sub_surface_test
{
    //委托
    public delegate void SendEventHandler(Point graphics_point);

    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        int M, N;
        double Xlength, Ylength;
        double Xstep, Ystep;
        double Viewsize;
        bool IfScan = false;
        public Form1(string ylength,string xlength,string ystep,string xstep,string viewsize)
        {
            //双缓冲
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            InitializeComponent();
            logoSet.Image = Image.FromFile(Application.StartupPath + "\\LOGO.png");
            stitching_select.SelectedIndex = 0;
            backgroud_radio1.Checked = true;
            save_radio1.Checked = true;
            SetTimer();
            MT_API.MT_Init();

            if (xlength != null && ylength != null && xstep != null && ystep != null && viewsize != null)
            {
                Xlength = Convert.ToDouble(xlength);
                Ylength = Convert.ToDouble(ylength);
            
                Xstep = Convert.ToDouble(xstep);
                Ystep = Convert.ToDouble(ystep);
                M = (int)Math.Ceiling(Ylength / Ystep);
                N = (int)Math.Ceiling(Xlength / Xstep);
                Viewsize = Convert.ToDouble(viewsize);
                IfScan = true;
            }
        }
        public Form1()
        {
            //双缓冲
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            //SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            //SetStyle(ControlStyles.Opaque, false);
            //this.BackColor = Color.Transparent;

            InitializeComponent();
            logoSet.Image = Image.FromFile(Application.StartupPath + "\\LOGO.png");
            stitching_select.SelectedIndex = 0;
            backgroud_radio1.Checked = true;
            save_radio1.Checked = true;
            SetTimer();
            MT_API.MT_Init();
        }
        /************************************全局变量***************************************/
        /************************CCD Para**************************/
        public IGXStream m_objIGXStream = null;
        public IGXDevice m_objIGXDevice = null;
        public IGXFactory m_objIGXFactory = null;
        public IGXFeatureControl m_objIGXFeatureControl = null;
        GxBitmap m_objGxBitmap = null;
  
        /**********图像处理参数************/

        private int file_rowNum, file_colNum;
        private int mstart, mend;
        private int nstart, nend;
        private int mtotal, ntotal;

        private int correctMax;
        private int correctMin;
        private int overrange;
        private double lineCorrection;
        private int suppressMin;
        private int salRange;
        private double reductionRadio;
        private double viewSize;
        private double moveStep;
        private int m_pixel = 1024; //注意：CCD自身像素参数，为定值
        private double exchange_rate;
        private double overlap_rate;
     
        private bool back_uniform; 
        private int srcImage_width = 512, srcImage_height = 512;
        private int dstImage_width, dstImage_height;

        private List<List<Image<Gray, byte>>> ImgPatchs = new List<List<Image<Gray, byte>>>(); //输入图像总数，二维
        private Image<Gray, byte> dstImage; //输出图像
        private Mat ImgNew = new Mat();
        /*********************************/

        /**********缺陷提取参数************/
        private Mat Img2Detect = new Mat();   //中间变量
        private Image<Bgr, byte> detectImage;  //缺陷提取后的Image图像
        private Mat ImgDraw = new Mat();
        private List<defectStruct> Blocks = new List<defectStruct>();    //块状缺陷
        private List<defectStruct> Lines = new List<defectStruct>();     //线状缺陷（不考虑面积阈值)
        private List<defectStruct> LinesNew = new List<defectStruct>();  //线状缺陷
        private List<defectStruct> LineLinksNew = new List<defectStruct>();   //断线连接
        private List<defectStruct> LineLinks = new List<defectStruct>();      //断线连接（不考虑面积阈值)
        /*********************************/

        /**********图像缩放与显示************/
        private string[] files;           //输入图像地址
        public Bitmap primary_bmp;        //主图图像

        Mat dstImage_small = new Mat();   //主图缩略
        public Bitmap primary_bmp_small;  //主图缩略 将原图缩小为200*200进行观察，不影响缩略图取点
        public Bitmap secondary_bmp;      //缩略图

        private Bitmap rectBmp; //虚拟画布
        RectangleF rectF = new RectangleF();  //缩略图的Zoom大小
        RectangleF rectRed = new RectangleF();  //缩略图红框Zoom大小
        private Graphics graphics_bmp;
        private Pen p;
        private int graphics_width = 64;  //红框宽度
        private int graphics_height = 36; //红框高度
        private int ROI_width, ROI_height;   //ROI参数
        /************************************/

        /**********图像加减************/
        Mat _matFluor = new Mat();
        Mat _matScatter = new Mat();
        int blur_para;
        double liner_corr_power;

        /************************************/


        //public int width;   //图像宽度
        //public int height;  //图像高度

        private float fscale;

        private Point pointTowheel = new Point();  //主视图与缩略图的坐标传递
        private Point wheel_point = new Point(100, 100);   //滚轮坐标传递
        private bool catch_flag = false;
        private bool start_flag = false;

        //word
        Microsoft.Office.Interop.Word.Application wordApp = null;

        Microsoft.Office.Interop.Word.Document wordDoc = null;



        /************************************************************************************/


        /************************************静态DLL库***************************************/
        //[DllImport("ROIselect.dll", CallingConvention = CallingConvention.Cdecl)]
        //public static extern IntPtr ROIselect(byte[] image, int width, int height, int ROI_x, int ROI_y,
        //    int ROI_width, int ROI_height, out int gstep);

        //[DllImport("primaryResize.dll", CallingConvention = CallingConvention.Cdecl)]
        //public static extern IntPtr primaryResize(byte[] image, int width, int height,
        //    int Resize_width, int Resize_height, out int gstep);

        /************************************************************************************/

        //Bitmap=>byte[]
        public static BitmapInfo GetImagePixel(Bitmap Source)
        {
            byte[] result; int step;
            int iWidth = Source.Width; int iHeight = Source.Height;
            Rectangle rect = new Rectangle(0, 0, iWidth, iHeight);
            System.Drawing.Imaging.BitmapData bmpData = Source.LockBits(rect,
                System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            IntPtr iPtr = bmpData.Scan0;
            int iBytes = iWidth * iHeight * 3;  //根据通道数进行设置 
            byte[] PixelValues = new byte[iBytes];
            System.Runtime.InteropServices.Marshal.Copy(iPtr, PixelValues, 0, iBytes);
            Source.UnlockBits(bmpData);
            step = bmpData.Stride;
            result = PixelValues;
            BitmapInfo bi = new BitmapInfo { Result = result, Step = step };
            return bi;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            m_objIGXFactory = IGXFactory.GetInstance();
            m_objIGXFactory.Init();
            //鼠标滚轮事件
            this.MouseWheel += new MouseEventHandler(this.secondaryPic_MouseWheel);

            this.gridView1.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;

            //进度条
            m_processBar.Properties.Step = 1;
            m_processBar.Properties.ProgressViewStyle = DevExpress.XtraEditors.Controls.ProgressViewStyle.Solid;
            m_processBar.Properties.PercentView = true;
            m_processBar.Position = 0;
       
        }

        void secondaryPic_MouseWheel(object sender, MouseEventArgs e)
        {

            if (graphics_width > 30 && graphics_width < 60)
            {
                graphics_width -= (e.Delta / 12);
                graphics_height = (int)(graphics_width * 9.0 / 16.0);
            }
            else if (graphics_width < 30 && e.Delta < 0)
            {
                graphics_width -= (e.Delta / 12);
                graphics_height = (int)(graphics_width * 9.0 / 16.0);
            }
            else if (graphics_width > 60 && e.Delta > 0)
            {
                graphics_width -= (e.Delta / 12);
                graphics_height = (int)(graphics_width * 9.0 / 16.0);
            }

            //矩形框大小缩放(必须)
            ROI_width = (int)(graphics_width * fscale);
            ROI_height = (int)(graphics_height * fscale);
            Point graphics_point = new Point();
            graphics_point.X = wheel_point.X;
            graphics_point.Y = wheel_point.Y;
            primary_rectShow(graphics_point);
        }

        private void secondary_MouseClick(object sender, MouseEventArgs e)
        {

            float secondary_fwidth = secondaryPic.Width;
            float secondary_fheight = secondaryPic.Height;
            float graphics_fwidth = graphics_width;
            float pri_sec_fscale = secondary_fwidth / graphics_fwidth;

            Point graphics_point = new Point();
            graphics_point.X = (int)(e.X / pri_sec_fscale);
            graphics_point.Y = (int)(e.Y / pri_sec_fscale);

            graphics_point.X += pointTowheel.X;
            graphics_point.Y += pointTowheel.Y;

            wheel_point = graphics_point;

            primary_rectShow(graphics_point);
        }

        private void secondary_MouseMove(object sender, MouseEventArgs e)
        {
            //pointTowheel = new Point(e.X, e.Y);

            //计算比例缩放
            //float secondary_width = secondaryPic.Width;
            //float secondary_height = secondaryPic.Height;

        }

        Bitmap ImageZoom(Mat _mat, ref RectangleF rect)//画缩略图
        {
            Mat _matResize = new Mat();
            rect = new RectangleF();

            float scaleX = primaryPic.Width * 1.0F / (float)_mat.Width;
            float scaleY = primaryPic.Height * 1.0F / (float)_mat.Height;
            if (scaleX < scaleY)
            {
                rectF.Width = _mat.Width * scaleX;
                rectF.Height = _mat.Height * scaleX;
            }
            else
            {
                rectF.Width = _mat.Width * scaleY;
                rectF.Height = _mat.Height * scaleY;
            }
            rectF.X = (_mat.Width - rectF.Width) / 2.0F;
            rectF.Y = (_mat.Height - rectF.Height) / 2.0F;

            CvInvoke.Resize(_mat, _matResize, new Size((int)rectF.Width, (int)rectF.Height),
                rectF.X, rectF.Y, Inter.Area);
            return _matResize.ToImage<Bgr, byte>().ToBitmap();
        }

        private void primary_rectShow(Point graphics_point) //事件处理函数
        {
            graphics_bmp.Clear(this.BackColor);
            graphics_bmp.DrawImage(primary_bmp_small, default(Point));

            int graphics_x = graphics_point.X - graphics_width / 2;
            int graphics_y = graphics_point.Y - graphics_height / 2;
            
            //越界处理
            //x
            if (graphics_x < 0)
                graphics_x = 0;
            else if (graphics_x + graphics_width > 199)
                graphics_x = 199 - graphics_width;

            //y
            if (graphics_y < 0)
                graphics_y = 0;
            else if (graphics_y + graphics_height > 199)
                graphics_y = 199 - graphics_height;

            Rectangle rect = new Rectangle(graphics_x, graphics_y, graphics_width, graphics_height);
            graphics_bmp.DrawRectangle(p, rect);
            primaryPic.CreateGraphics().DrawImage(rectBmp, default(Point));
            //p.Dispose();
            //graphics_bmp.Dispose();

            pointTowheel.X = graphics_x;
            pointTowheel.Y = graphics_y;
            graphics_point1 = graphics_point;
            secondaryPic.Refresh();
            /*
            if (this.SendEvent != null)
            {
                SendEvent(graphics_point);
            }
            */
        }

        //画缩略图
        void secondaryPic_show(Point graphics_point)
        {
            /*
            //矩形左上角坐标
            int graphics_x = graphics_point.X - graphics_width / 2;
            int graphics_y = graphics_point.Y - graphics_height / 2;


            //越界处理
            //x
            if (graphics_x < 0)
                graphics_x = 0;
            else if (graphics_x + graphics_width > primaryPic.Width - 1)
                graphics_x = primaryPic.Width - 1 - graphics_width;

            //y
            if (graphics_y < 0)
                graphics_y = 0;
            else if (graphics_y + graphics_height > primaryPic.Height - 1)
                graphics_y = primaryPic.Height - 1 - graphics_height;


            //坐标变换
            rectRed = new RectangleF();  //转换到实际图像上的尺寸
            float scaleX = primary_bmp.Width * 1.0F / primaryPic.Width;
            float scaleY = primary_bmp.Height * 1.0F / primaryPic.Height;
            if (scaleX < scaleY)
            {
                rectRed.X = graphics_x * scaleX;
                rectRed.Y = graphics_y * scaleX;
                rectRed.Width = graphics_width * scaleX;
                rectRed.Height = graphics_height * scaleX;
            }
            else
            {
                rectRed.X = graphics_x * scaleY;
                rectRed.Y = graphics_y * scaleY;
                rectRed.Width = graphics_width * scaleY;
                rectRed.Height = graphics_height * scaleY;
            }
            int idxx, idxy;
            if(rectRed.X< (int)(Imgnew2.Width * 1.0 / 3))
            {
                idxx = 0;
            }
            else
            {
                idxx = 1;
            }
            idxy = (int)Math.Floor((rectRed.Y+1) / (int)(Imgnew2.Height*1.0 / 5));
            if(idxy==4)
            {
                idxy = 3;
            }
            int idx = idxy * 2 + idxx;
            
        /*
            Mat SecondaryImg = new Mat(images[idx], new Rectangle((int)rectRed.X- ((int)(ImgNew.Width * 1.0 / 3))*idxx,
                (int)rectRed.Y- (int)(ImgNew.Height * 1.0 / 5)*idxy, (int)rectRed.Width, (int)rectRed.Height));
          */  
          /*
            Mat SecondaryImg = new Mat(Imgnew2, new Rectangle((int)rectRed.X,
                (int)rectRed.Y, (int)rectRed.Width, (int)rectRed.Height));
            secondary_bmp = SecondaryImg.ToImage<Bgr, byte>().ToBitmap();
            
            secondaryPic.Image = secondary_bmp;
            */
        }

        private void checkButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void settingButton_Click(object sender, EventArgs e)
        {
            Stitching_setup stitching_Setup = new Stitching_setup();
            stitching_Setup.Show();
        }
        int[] Y = new int[4];
        List<Mat> images = new List<Mat>();
        void stitch()
        {
           
            dstImage_height = (int)(mtotal * srcImage_height - (mtotal - 1) * overlap_rate * srcImage_height + overrange * 2);
            dstImage_width = (int)(ntotal * srcImage_width - (ntotal - 1) * overlap_rate * srcImage_width + overrange * 2);
            dstImage = new Image<Gray,byte>(dstImage_width - 1, dstImage_height - 1);

            int start_x = overrange;
            int start_y = overrange;

            List<List<Point>> TopLeftCorner = new List<List<Point>>();
            Point TopLeft = new Point(start_x, start_y);
            Point matchPoints = new Point(0, 0);

            int MatchZone_x = (int)(srcImage_width * overlap_rate);
            int MatchZone_y = (int)(srcImage_height * overlap_rate);
            int MatchStep_x = srcImage_width - MatchZone_x;
            int MatchStep_y = srcImage_height - MatchZone_y;

            Point tmpVal = new Point();
            for (int i = 0; i < mtotal; i++)
            {
                List<Point> tmpList = new List<Point>();
                for (int j = 0; j < ntotal; j++)
                {
                    tmpVal.X = j * MatchStep_x + start_x;
                    tmpVal.Y = i * MatchStep_y + start_y;
                    tmpList.Add(tmpVal);
                }
                TopLeftCorner.Add(tmpList);
            }

            int picnum = 0;
            for (; picnum < mtotal * ntotal;)
            {
                Thread.Sleep(1000);
                string[] names;
                if (IfScan)
                    names = Directory.GetFiles("F:\\stitch", "*tiff", SearchOption.AllDirectories);
                else
                    names = Directory.GetFiles(folderName, "*tiff", SearchOption.AllDirectories);
                List<string> Names = names.OrderBy(name => new FileInfo(name).CreationTime).ToList();
                int tpicnum = Names.Count();
                if (tpicnum > picnum)
                {

                    for (int i = 1; i <= tpicnum - picnum&&i<=mtotal*ntotal; i++)
                    {

                        Image<Gray, byte> tempImage = new Image<Gray, byte>(Names[Names.Count - 1 - tpicnum + picnum + i]);                     
                        int n = (Names.Count - tpicnum + picnum + i) % ntotal - 1;
                        int m;
                        if (n == -1)
                        {
                            m = (int)Math.Floor((Names.Count - tpicnum + picnum + i) * 1.0 / ntotal) - 1;
                            n = ntotal - 1;
                        }
                        else
                        {
                            m = (int)Math.Floor((Names.Count - tpicnum + picnum + i) * 1.0 / ntotal);
                        }


                        if (stitchmethod == 1)
                        {
                            CvInvoke.cvSetImageROI(dstImage, new Rectangle(TopLeftCorner[m][n].X, TopLeftCorner[m][n].Y, srcImage_width, srcImage_height));                          
                            tempImage.CopyTo(dstImage);
                            CvInvoke.cvResetImageROI(dstImage);

                        }
                        if (stitchmethod == 2)
                        {
                            if (m == 0 && n == 0)
                            {
                                TopLeft = TopLeftCorner[m][n];
                            }
                            else if (m == 0)
                            {
                                Point TopLeft_Before = TopLeftCorner[m][n - 1];
                                TopLeft.Y = TopLeft_Before.Y;
                                TopLeft.X = TopLeft_Before.X + MatchStep_x;
                                Rectangle rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, MatchZone_x, srcImage_height);
                                Mat Block_dstImg = new Mat(dstImage.Mat, rect_ROI1);
                                Rectangle rect_ROI2 = new Rectangle(0, 0, MatchZone_x, srcImage_height);
                                Mat Block_Patch = new Mat(tempImage.Mat, rect_ROI2);
                                matchPoints = FindBlock(Block_dstImg, Block_Patch);
                            }
                            else if (n == 0)
                            {
                                Point TopLeft_Before = TopLeftCorner[m - 1][n];
                                TopLeft.Y = TopLeft_Before.Y + MatchStep_y;
                                TopLeft.X = TopLeft_Before.X;
                                Rectangle rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, MatchZone_y);
                                Mat Block_dstImg = new Mat(dstImage.Mat, rect_ROI1);
                                Rectangle rect_ROI2 = new Rectangle(0, 0, srcImage_width, MatchZone_y);
                                Mat Block_Patch = new Mat(tempImage.Mat, rect_ROI2);
                                matchPoints = FindBlock(Block_dstImg, Block_Patch);
                            }
                            else
                            {
                                Point TopLeft_Before = TopLeftCorner[m][n - 1];
                                TopLeft.Y = TopLeft_Before.Y;
                                TopLeft.X = TopLeft_Before.X + MatchStep_x;
                                Rectangle rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, MatchZone_x, srcImage_height);
                                Mat Block_dstImg = new Mat(dstImage.Mat, rect_ROI1);
                                Rectangle rect_ROI2 = new Rectangle(0, 0, MatchZone_x, srcImage_height);
                                Mat Block_Patch = new Mat(tempImage.Mat, rect_ROI2);
                                matchPoints = FindBlock(Block_dstImg, Block_Patch);

                                if (matchPoints.X == 0 && matchPoints.Y == 0)
                                {
                                    TopLeft_Before = TopLeftCorner[m - 1][n];
                                    TopLeft.Y = TopLeft_Before.Y + MatchStep_y;
                                    TopLeft.X = TopLeft_Before.X;
                                    rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, MatchZone_y);
                                    Block_dstImg = new Mat(dstImage.Mat, rect_ROI1);
                                    rect_ROI2 = new Rectangle(0, 0, srcImage_width, MatchZone_y);
                                    Block_Patch = new Mat(tempImage.Mat, rect_ROI2);
                                    matchPoints = FindBlock(Block_dstImg, Block_Patch);
                                }
                            }

                            TopLeft.X -= matchPoints.X;
                            TopLeft.Y -= matchPoints.Y;
                            TopLeft.X = (TopLeft.X < 0 || TopLeft.X > (dstImage_width - srcImage_width)) ? TopLeftCorner[m][n].X : TopLeft.X;
                            TopLeft.Y = (TopLeft.Y < 0 || TopLeft.Y > (dstImage_height - srcImage_height)) ? TopLeftCorner[m][n].Y : TopLeft.Y;
                            Rectangle rect = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, srcImage_height);
                            CvInvoke.cvSetImageROI(dstImage, rect);
                            tempImage.CopyTo(dstImage);
                            CvInvoke.cvResetImageROI(dstImage);

                            TopLeftCorner[m][n] = TopLeft; //更新左上角坐标
                        }

                        tempImage.Dispose();
                    }

                    picnum = tpicnum;

                }

            }
            CvInvoke.Imwrite("F:\\StitchResM2.jpg", dstImage);
            Imgnew2 = CvInvoke.Imread("F:\\StitchResM2.jpg");
            primary_bmp = Imgnew2.ToImage<Bgr, byte>().ToBitmap();
            primary_bmp_small = ImageZoom(Imgnew2, ref rectF);//画缩略图
            rectBmp = new Bitmap(primaryPic.Width, primaryPic.Height);
            graphics_bmp = Graphics.FromImage(rectBmp);
            p = new Pen(Color.Red, 2);
            start_flag = true; //可选打开！                                                                                                                      
            //SendEvent += new SendEventHandler(secondaryPic_show);
            graphics_point1 = new Point(primaryPic.Width / 2, primaryPic.Height / 2);
            primary_rectShow(graphics_point1);
            Img2Detect = Imgnew2.Clone();
        }
        //拼接按钮
        private void stitching_button_Click(object sender, EventArgs e)
        {
            /****************参数归零*****************/
     //       ImgPatchs = new List<List<Image<Gray, byte>>>();
            /****************当前参数*****************/
            //行列数参数
            if (IfScan)
            {
                mtotal = M;
                ntotal = N;
                viewSize = Viewsize;
                moveStep = Xstep;
                srcImage_width = 512;
                srcImage_height = 512;
            }
            else
            {
                mstart = Convert.ToInt32(mstart_Label.Text);
                mend = Convert.ToInt32(mend_Label.Text);
                nstart = Convert.ToInt32(nstart_Label.Text);
                nend = Convert.ToInt32(nend_Label.Text);
                mtotal = mend - mstart + 1;
                ntotal = nend - nstart + 1;
                viewSize = Convert.ToDouble(t_viewSize.Text);
                moveStep = Convert.ToDouble(t_moveStep.Text);
                Mat Img_sample = CvInvoke.Imread(files[0], ImreadModes.AnyDepth);
                srcImage_width = Img_sample.Width;
                srcImage_height = Img_sample.Height;
            }
            //图像处理参数
            Stitching_setup _setup = new Stitching_setup();
            correctMax = Convert.ToInt32(t_correctMax.Text);
            correctMin = Convert.ToInt32(_setup.t_correctMin.Text);
            overrange = Convert.ToInt32(_setup.t_overRange.Text);
            lineCorrection = Convert.ToDouble(_setup.t_lineCorrection.Text);
            suppressMin = Convert.ToInt32(_setup.t_suppressMin.Text);
            salRange = Convert.ToInt32(_setup.t_salRange.Text);
            reductionRadio = Convert.ToDouble(_setup.t_reductRadio.Text);
            back_uniform = backgroud_radio1.Checked;

          

            exchange_rate = viewSize / m_pixel * 1000;
            overlap_rate = (viewSize - moveStep) / viewSize;
            /*****************************************/

            /****************图片载入与图像处理*****************/

            stitch();
      
            /*
            l_processShow.Text = "图像处理中...";
            int bar_count = 0;
            Mat tempMat = new Mat();
            for (int i = mstart - 1; i < mend; i++)
            {
                List<Image<Gray, byte>> tmpList = new List<Image<Gray, byte>>();
                for (int j = nstart - 1; j < nend; j++)
                {
                    tempMat = CvInvoke.Imread(files[i * file_colNum + j], ImreadModes.AnyDepth);
                    CvInvoke.Flip(tempMat, tempMat, FlipType.Horizontal);
                    tmpList.Add(LinerCorrection(tempMat));
                    bar_count = (int)Math.Ceiling(100 * (double)(i * mtotal + j + 1) / (double)(mtotal * ntotal));
                    m_processBar.Position = bar_count;
                    Application.DoEvents();
                }
                ImgPatchs.Add(tmpList);
       
            }
            l_processShow.Text = "图像处理完成！";
            */
       

            /****************图像拼接模块***********************/
            /*
            dstImage_height = (int)(mtotal * srcImage_height - (mend - mstart) * overlap_rate * srcImage_height + overrange * 2);
            dstImage_width = (int)(ntotal * srcImage_width - (nend - nstart) * overlap_rate * srcImage_width + overrange * 2);
            dstImage = new Image<Gray, byte>(dstImage_width - 1, dstImage_height - 1);

            stitchProcess();
            
            for(int i=0;i<ImgPatchs.Count();i++)
            {
                for(int j=0;j<ImgPatchs[0].Count();j++)
                {
                    ImgPatchs[i][j].Dispose();
                }
            }
            

            KeyValuePair<ImwriteFlags, Int32> idx = new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.JpegQuality, 30);
            System.Collections.Generic.KeyValuePair<ImwriteFlags, Int32>[] flags = new KeyValuePair<ImwriteFlags, Int32>[1];
            flags[0] = idx;
            //  Image saveImgnew = ImgNew.ToImage<Gray, byte>();
            CvInvoke.Imwrite("F:\\tempres2.jpg", ImgNew, flags);
            Imgnew2 = CvInvoke.Imread("F:\\tempres2.jpg");
  */
            /****************图像缩放与显示**********************/
            /*
            primary_bmp_small = ImageZoom(Imgnew2, ref rectF);//画缩略图

            rectBmp = new Bitmap(primaryPic.Width, primaryPic.Height);
            graphics_bmp = Graphics.FromImage(rectBmp);
            p = new Pen(Color.Red, 2);

            start_flag = true; //可选打开！                                                                                                                      

            //SendEvent += new SendEventHandler(secondaryPic_show);
            graphics_point1 = new Point(primaryPic.Width / 2, primaryPic.Height / 2);
            primary_rectShow(graphics_point1);
            */
          
        }
        Point graphics_point1;
        //拼接主程序
        void stitchProcess()
        {
            /*--------------------模式选择----------------------*/
            int mode = 1, MatchingMode = 1;
            switch (stitching_select.SelectedIndex)
            {
                case 0:
                    mode = 1;
                    MatchingMode = 1;
                    break;
                case 1:
                    mode = 1;
                    MatchingMode = 2;
                    break;
                case 2:
                    mode = 2;
                    MatchingMode = 1;
                    break;
                case 3:
                    mode = 2;
                    MatchingMode = 2;
                    break;
                case 4:
                    mode = 3;
                    break;
                default:
                    break;
            }
            /*-------------------------------------------------*/

            int start_x = overrange;
            int start_y = overrange;

            List<List<Point>> TopLeftCorner = new List<List<Point>>();
            Point TopLeft = new Point(start_x, start_y);
            Point matchPoints = new Point(0, 0);

            int MatchZone_x = (int)(srcImage_width * overlap_rate);
            int MatchZone_y = (int)(srcImage_height * overlap_rate);
            int MatchStep_x = srcImage_width - MatchZone_x;
            int MatchStep_y = srcImage_height - MatchZone_y;

            //重叠区域Mat及其Flag
            // 0,2为上下,1,3为左右
            int ZONE_FLAG = 2;
            List<Image<Gray, byte>> BlocksFlagSet = new List<Image<Gray, byte>>();
            //List<Image<Gray, byte>> Blocks = new List<Image<Gray, byte>>();
            Mat[] Blocks = new Mat[ZONE_FLAG];
            for (int i = 0; i < ZONE_FLAG; i++)
            {
                Image<Gray, byte> tmpImg = new Image<Gray, byte>(ntotal, mtotal);
                BlocksFlagSet.Add(tmpImg);
            }

            Point tmpVal = new Point();
            for (int i = 0; i < mtotal; i++)
            {
                List<Point> tmpList = new List<Point>();
                for (int j = 0; j < ntotal; j++)
                {
                    tmpVal.X = j * MatchStep_x + start_x;
                    tmpVal.Y = i * MatchStep_y + start_y;
                    tmpList.Add(tmpVal);
                }
                TopLeftCorner.Add(tmpList);
            }

            ImgNew = dstImage.Mat;   //Mat格式
            int bar_count = 0;
            /*--------------------直接拼接----------------------*/
            if (mode == 1)
            {
                l_processShow.Text = (MatchingMode == 1) ? "Z型直接拼接中..." : "蛇型直接拼接中...";
                
                for (int i = 0; i < mtotal; i++)
                {
                    for (int k = 0; k < ntotal; k++)
                    {
                        bar_count = (int)Math.Ceiling(100 * (double)(i * mtotal + k + 1) / (double)(mtotal * ntotal));
                        m_processBar.Position = bar_count;
                        Application.DoEvents();

                   //     int j = (MatchingMode == 1 && i % 2 != 0) ? k : ntotal - 1 - k;
                        int j = k;
                        TopLeft = TopLeftCorner[i][j];
                        Rectangle rect = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, srcImage_height);
                        Mat mat_ROI = new Mat(ImgNew, rect);
                        ImgPatchs[i][j].Mat.CopyTo(mat_ROI);
                        
                    }
                }
                
            }

            /*-------------------------块拼接--------------------------*/
            else if (mode == 2)
            {
                l_processShow.Text = (MatchingMode == 1) ? "Z型块匹配拼接中..." : "蛇型块匹配拼接中...";
                for (int i = 0; i < mtotal; i++)
                {
                    for (int k = 0; k < ntotal; k++)
                    {
                        bar_count = (int)Math.Ceiling(100 * (double)(i * mtotal + k + 1) / (double)(mtotal * ntotal));
                        m_processBar.Position = bar_count;
                        Application.DoEvents();
                        //////////////////////////////////////////////////////////////////////////////////////////////////
                        int j = (MatchingMode == 1 || i % 2 == 0) ? k : ntotal - 1 - k;
                        if (i == 0 && j == 0)
                        {
                            TopLeft = TopLeftCorner[i][j];
                        }
                        else if (i == 0)
                        {
                            Point TopLeft_Before = TopLeftCorner[i][j - 1];
                            TopLeft.Y = TopLeft_Before.Y;
                            TopLeft.X = TopLeft_Before.X + MatchStep_x;
                            Rectangle rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, MatchZone_x, srcImage_height);
                            Mat Block_dstImg = new Mat(ImgNew, rect_ROI1);
                            Rectangle rect_ROI2 = new Rectangle(0, 0, MatchZone_x, srcImage_height);
                            Mat Block_Patch = new Mat(ImgPatchs[i][j].Mat, rect_ROI2);
                            matchPoints = FindBlock(Block_dstImg, Block_Patch);
                        }
                        else if (j == 0)
                        {
                            Point TopLeft_Before = TopLeftCorner[i - 1][j];
                            TopLeft.Y = TopLeft_Before.Y + MatchStep_y;
                            TopLeft.X = TopLeft_Before.X;
                            Rectangle rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, MatchZone_y);
                            Mat Block_dstImg = new Mat(ImgNew, rect_ROI1);
                            Rectangle rect_ROI2 = new Rectangle(0, 0, srcImage_width, MatchZone_y);
                            Mat Block_Patch = new Mat(ImgPatchs[i][j].Mat, rect_ROI2);
                            matchPoints = FindBlock(Block_dstImg, Block_Patch);
                        }
                        else
                        {
                            Point TopLeft_Before = TopLeftCorner[i][j - 1];
                            TopLeft.Y = TopLeft_Before.Y;
                            TopLeft.X = TopLeft_Before.X + MatchStep_x;
                            Rectangle rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, MatchZone_x, srcImage_height);
                            Mat Block_dstImg = new Mat(ImgNew, rect_ROI1);
                            Rectangle rect_ROI2 = new Rectangle(0, 0, MatchZone_x, srcImage_height);
                            Mat Block_Patch = new Mat(ImgPatchs[i][j].Mat, rect_ROI2);
                            matchPoints = FindBlock(Block_dstImg, Block_Patch);

                            if (matchPoints.X == 0 && matchPoints.Y == 0)
                            {
                                TopLeft_Before = TopLeftCorner[i - 1][j];
                                TopLeft.Y = TopLeft_Before.Y + MatchStep_y;
                                TopLeft.X = TopLeft_Before.X;
                                rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, MatchZone_y);
                                Block_dstImg = new Mat(ImgNew, rect_ROI1);
                                rect_ROI2 = new Rectangle(0, 0, srcImage_width, MatchZone_y);
                                Block_Patch = new Mat(ImgPatchs[i][j].Mat, rect_ROI2);
                                matchPoints = FindBlock(Block_dstImg, Block_Patch);
                            }
                        }

                        TopLeft.X -= matchPoints.X;
                        TopLeft.Y -= matchPoints.Y;
                        TopLeft.X = (TopLeft.X < 0 || TopLeft.X > (dstImage_width - srcImage_width)) ? TopLeftCorner[i][j].X : TopLeft.X;
                        TopLeft.Y = (TopLeft.Y < 0 || TopLeft.Y > (dstImage_height - srcImage_height)) ? TopLeftCorner[i][j].Y : TopLeft.Y;
                        Rectangle rect = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, srcImage_height);
                        Mat mat_ROI = new Mat(ImgNew, rect);
                        ImgPatchs[i][j].Mat.CopyTo(mat_ROI);
                        TopLeftCorner[i][j] = TopLeft; //更新左上角坐标
                    }
                }
            }
            /*-------------------------智能拼接--------------------------*/
            else if (mode == 3)
            {
                l_processShow.Text = "智能拼接中...";
                double ThresValue = 30.0;
                int[] BlocksFlag = new int[2] { 0, 0 };
                for (int i = 0; i < mtotal; i++)
                {
                    for (int j = 0; j < ntotal; j++)
                    {
                        Rectangle rect = new Rectangle(0, 0, srcImage_width, MatchZone_y);
                        Blocks[0] = new Mat(ImgPatchs[i][j].Mat, rect);
                        BlocksFlag[0] = IsTexture(Blocks[0], ThresValue);
                        BlocksFlagSet[0].Data[i, j, 0] = (byte)BlocksFlag[0];

                        rect = new Rectangle(0, 0, MatchZone_x, srcImage_height);
                        Blocks[1] = new Mat(ImgPatchs[i][j].Mat, rect);
                        BlocksFlag[1] = IsTexture(Blocks[1], ThresValue);
                        BlocksFlagSet[1].Data[i, j, 0] = (byte)BlocksFlag[1];

                        bar_count = (int)Math.Ceiling(100 * (double)(i * mtotal + j + 1) / (double)(2 * mtotal * ntotal));
                        m_processBar.Position = bar_count;
                        Application.DoEvents();
                    }
                }

                for (int i = 0; i < mtotal; i++)
                {
                    for (int j = 0; j < ntotal; j++)
                    {
                        if (BlocksFlagSet[0].Data[i, j, 0] == 1 || BlocksFlagSet[1].Data[i, j, 0] == 1)
                        {
                            if (i == 0 && j == 0)
                            {
                                TopLeft = TopLeftCorner[i][j];
                            }
                            else if (i == 0)
                            {
                                Point TopLeft_Before = TopLeftCorner[i][j - 1];
                                TopLeft.Y = TopLeft_Before.Y;
                                TopLeft.X = TopLeft_Before.X + MatchStep_x;
                                Rectangle rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, MatchZone_x, srcImage_height);
                                Mat Block_dstImg = new Mat(ImgNew, rect_ROI1);
                                Rectangle rect_ROI2 = new Rectangle(0, 0, MatchZone_x, srcImage_height);
                                Mat Block_Patch = new Mat(ImgPatchs[i][j].Mat, rect_ROI2);
                                matchPoints = FindBlock(Block_dstImg, Block_Patch);
                            }
                            else if (j == 0)
                            {
                                Point TopLeft_Before = TopLeftCorner[i - 1][j];
                                TopLeft.Y = TopLeft_Before.Y + MatchStep_y;
                                TopLeft.X = TopLeft_Before.X;
                                Rectangle rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, MatchZone_y);
                                Mat Block_dstImg = new Mat(ImgNew, rect_ROI1);
                                Rectangle rect_ROI2 = new Rectangle(0, 0, srcImage_width, MatchZone_y);
                                Mat Block_Patch = new Mat(ImgPatchs[i][j].Mat, rect_ROI2);
                                matchPoints = FindBlock(Block_dstImg, Block_Patch);
                            }
                            else
                            {
                                Point TopLeft_Before = TopLeftCorner[i][j - 1];
                                TopLeft.Y = TopLeft_Before.Y;
                                TopLeft.X = TopLeft_Before.X + MatchStep_x;
                                Rectangle rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, MatchZone_x, srcImage_height);
                                Mat Block_dstImg = new Mat(ImgNew, rect_ROI1);
                                Rectangle rect_ROI2 = new Rectangle(0, 0, MatchZone_x, srcImage_height);
                                Mat Block_Patch = new Mat(ImgPatchs[i][j].Mat, rect_ROI2);
                                matchPoints = FindBlock(Block_dstImg, Block_Patch);

                                if (matchPoints.X == 0 && matchPoints.Y == 0)
                                {
                                    TopLeft_Before = TopLeftCorner[i - 1][j];
                                    TopLeft.Y = TopLeft_Before.Y + MatchStep_y;
                                    TopLeft.X = TopLeft_Before.X;
                                    rect_ROI1 = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, MatchZone_y);
                                    Block_dstImg = new Mat(ImgNew, rect_ROI1);
                                    rect_ROI2 = new Rectangle(0, 0, srcImage_width, MatchZone_y);
                                    Block_Patch = new Mat(ImgPatchs[i][j].Mat, rect_ROI2);
                                    matchPoints = FindBlock(Block_dstImg, Block_Patch);
                                }
                            }

                            TopLeft.X -= matchPoints.X;
                            TopLeft.Y -= matchPoints.Y;
                            TopLeft.X = (TopLeft.X < 0 || TopLeft.X > (dstImage_width - srcImage_width)) ? TopLeftCorner[i][j].X : TopLeft.X;
                            TopLeft.Y = (TopLeft.Y < 0 || TopLeft.Y > (dstImage_height - srcImage_height)) ? TopLeftCorner[i][j].Y : TopLeft.Y;
                            Rectangle rect = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, srcImage_height);
                            Mat mat_ROI = new Mat(ImgNew, rect);
                            ImgPatchs[i][j].Mat.CopyTo(mat_ROI);
                            TopLeftCorner[i][j] = TopLeft; //更新左上角坐标
                        }
                        else
                        {
                            TopLeft = TopLeftCorner[i][j];
                            Rectangle rect = new Rectangle(TopLeft.X, TopLeft.Y, srcImage_width, srcImage_height);
                            Mat mat_ROI = new Mat(ImgNew, rect);
                            ImgPatchs[i][j].Mat.CopyTo(mat_ROI);
                        }
                        m_processBar.Position = bar_count + (int)Math.Ceiling(100 * (double)(i * mtotal + j + 1) / (double)(2 * mtotal * ntotal));
                        Application.DoEvents();
                    }
                }
            }

            dstImage = ImgNew.ToImage<Gray, byte>();   //ImgNew是Mat格式, dstImage是Image格式, primary_bmp是Bitmap格式
            primary_bmp = dstImage.ToBitmap();
            Img2Detect = ImgNew.Clone();
            l_processShow.Text = "图像拼接完成!";

            //primaryPic.Image = dstImage.ToBitmap();
            //CvInvoke.Imwrite("E:\\研究生工作\\项目\\20180202亚表面检测\\Sub_surface_test\\test40_40_direct.bmp", dstImage);
        }


        //图像处理主函数:图像校正（显著性算法）
        Image<Gray, byte> LinerCorrection(Mat image)
        {
            //CvInvoke.CvtColor(image, image, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
            //Image<Gray, Single> src = image.Convert<Gray, Single>();
            image.ConvertTo(image, DepthType.Cv32F, 1);
            Image<Gray, Single> src = image.ToImage<Gray, Single>();

            Image<Gray, Single> src_corr_temp1 = new Image<Gray, Single>(srcImage_width, srcImage_height);
            Image<Gray, Single> src_corr_temp2 = new Image<Gray, Single>(srcImage_width, srcImage_height);
            Image<Gray, Single> src_corr_temp3 = new Image<Gray, Single>(srcImage_width, srcImage_height);
            Image<Gray, Single> src_corr_temp4 = new Image<Gray, Single>(srcImage_width, srcImage_height);

            src_corr_temp1 = src / (float)correctMin * 255.0f;
            src_corr_temp2 = src / (((float)correctMax / 100.0f) * 255.0f);
            CvInvoke.GaussianBlur(src, src_corr_temp4, new Size(3, 3), 5);
            CvInvoke.Blur(src, src_corr_temp3, new Size(salRange, salRange), new Point(-1, -1));

            src_corr_temp4 = src_corr_temp4 - src_corr_temp3;
            for (int i = 0; i < src_corr_temp4.Height; i++)
            {
                for (int j = 0; j < src_corr_temp4.Width; j++)
                {
                    if (src_corr_temp4.Data[i, j, 0] < 0)
                        src_corr_temp4.Data[i, j, 0] = 0;
                }
            }
            CvInvoke.Pow(src_corr_temp4, lineCorrection, src_corr_temp4);

            double max = 0.0, min = 0.0;
            Point maxLoc = new Point(0, 0);
            Point minLoc = new Point(0, 0);
            CvInvoke.MinMaxLoc(src_corr_temp4, ref min, ref max, ref minLoc, ref maxLoc);
            int suppress_pow = (int)Math.Pow(suppressMin, lineCorrection);

            if (max < suppress_pow) max = suppress_pow;
            src_corr_temp4 = src_corr_temp4 / max;
            CvInvoke.Multiply(src_corr_temp1, src_corr_temp4, src_corr_temp1);
            CvInvoke.Multiply(src_corr_temp2, (1.0f - src_corr_temp4), src_corr_temp2);

            if (back_uniform)
                src = src_corr_temp2 * reductionRadio + src_corr_temp1;
            else
                src = src_corr_temp2 + src_corr_temp1;

            image = src.Mat;
            image.ConvertTo(image, DepthType.Cv8U, 1);
            Image<Gray, byte> dst = image.ToImage<Gray, byte>();
            return dst;
        }

        //模板匹配算法
        Point FindBlock(Mat Block1, Mat Block2)
        {
            Point matchPoint = new Point(0, 0);

            int Block_height = Block2.Rows;
            int Block_width = Block2.Cols;
            Mat meanImage = new Mat();
            CvInvoke.Blur(Block1, meanImage, new Size(15, 15), new Point(-1, -1));
            Image<Gray, byte> residual = Block1.ToImage<Gray, byte>() - meanImage.ToImage<Gray, byte>();
            Image<Gray, byte> varMask = residual.Mul(residual);
            CvInvoke.Blur(varMask, varMask, new Size(15, 15), new Point(-1, -1));
            Image<Gray, Single> varMask1 = varMask.Convert<Gray, Single>();

            for (int i = 0; i < varMask1.Rows; i++)
            {
                for (int j = 0; j < varMask1.Cols; j++)
                {
                    varMask1.Data[i, j, 0] = (float)Math.Sqrt(varMask1.Data[i, j, 0]);
                }
            }
            double minv = 0.0, maxv = 0.0;
            Point minvLoc = new Point(0, 0);
            Point maxvLoc = new Point(0, 0);
            CvInvoke.MinMaxLoc(varMask1, ref minv, ref maxv, ref minvLoc, ref maxvLoc);

            if (maxv < 6)
                return matchPoint;
            else
            {
                CvInvoke.MinMaxLoc(residual, ref minv, ref maxv, ref minvLoc, ref maxvLoc);
                Image<Gray, byte> test = residual.Convert<byte>(delegate (byte b) { return (byte)((b / maxv > 0.5) ? 255 : 0); });

                Mat element = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(3, 3), new Point(-1, -1));
                CvInvoke.Erode(test, test, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
                CvInvoke.Dilate(test, test, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));

                Emgu.CV.Util.VectorOfVectorOfPoint contourPoints = new Emgu.CV.Util.VectorOfVectorOfPoint();
                Emgu.CV.IOutputArray hierarchy = new Image<Gray, byte>(test.Width, test.Height, new Gray(255));
                CvInvoke.FindContours(test, contourPoints, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxNone);
                CvInvoke.DrawContours(test, contourPoints, -1, new MCvScalar(255));

                int contourNum = contourPoints.Size;
                if (contourNum == 0) return matchPoint;
                int sumX = 0, sumY = 0, cnt = 0;
                List<Point> patchPoints = new List<Point>();
                for (int i = 0; i < contourNum; i++)
                {
                    cnt = contourPoints[i].Size;
                    for (int j = 0; j < cnt; j++)
                    {
                        sumX += contourPoints[i][j].X;
                        sumY += contourPoints[i][j].Y;
                    }
                    Point tmpPoint = new Point(sumX / cnt, sumY / cnt);
                    patchPoints.Add(tmpPoint);
                    sumX = 0; sumY = 0; cnt = 0;
                }

                int patchNum = patchPoints.Count;
                int patchRange = 10;
                int patchCols, patchRows, matchCols, matchRows;
                Point LeftTop = new Point(0, 0);
                Point tmpLoc = new Point(0, 0);
                List<Point> distance = new List<Point>();
                double minVal = 0.0, maxVal = 0.0;
                Point minLoc = new Point(0, 0);
                Point maxLoc = new Point(0, 0);

                Mat patch, matchResult;
                for (int i = 0; i < patchNum; i++)
                {
                    LeftTop.X = (patchPoints[i].X - patchRange < 0) ? 0 : (patchPoints[i].X - patchRange);
                    LeftTop.Y = (patchPoints[i].Y - patchRange < 0) ? 0 : (patchPoints[i].Y - patchRange);
                    patchCols = (patchPoints[i].X + 2 * patchRange > Block1.Cols - 1) ? Block1.Cols - patchPoints[i].X - 1 : 2 * patchRange;
                    patchRows = (patchPoints[i].Y + 2 * patchRange > Block1.Rows - 1) ? Block1.Rows - patchPoints[i].Y - 1 : 2 * patchRange;
                    Rectangle rect = new Rectangle(LeftTop.X, LeftTop.Y, patchCols, patchRows);
                    patch = new Mat(Block1, rect);

                    matchCols = Block2.Cols - patch.Cols + 1;
                    matchRows = Block2.Rows - patch.Rows + 1;
                    matchResult = new Mat(matchRows, matchCols, DepthType.Cv32F, 1);
                    CvInvoke.MatchTemplate(Block2, patch, matchResult, TemplateMatchingType.SqdiffNormed);
                    CvInvoke.Normalize(matchResult, matchResult, 0, 1, NormType.MinMax, DepthType.Default);
                    CvInvoke.MinMaxLoc(matchResult, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                    tmpLoc.X = minLoc.X - LeftTop.X;
                    tmpLoc.Y = minLoc.Y - LeftTop.Y;
                    distance.Add(tmpLoc);
                }

                double length = 0.0, matchRange = 20.0;
                for (int i = 0; i < distance.Count; i++)
                {
                    length = getPointDistance(distance[i]);
                    if (length < matchRange)
                    {
                        matchRange = length;
                        matchPoint = distance[i];
                    }
                }
                return matchPoint;
            }
        }

        double getPointDistance(Point distance)
        {
            double length = 0.0;
            length = distance.X * distance.X + distance.Y * distance.Y;
            length = Math.Sqrt(length);
            return length;
        }

        int IsTexture(Mat Block, double ThresVal)
        {
            int TextureFlag = 0;
            Block.ConvertTo(Block, DepthType.Cv32F, 1);
            Mat sobelx = new Mat();
            Mat sobely = new Mat();
            CvInvoke.Sobel(Block, sobelx, DepthType.Cv32F, 1, 0, 3);
            CvInvoke.Sobel(Block, sobely, DepthType.Cv32F, 0, 1, 3);

            Mat norm = new Mat();
            Mat dir = new Mat();
            CvInvoke.CartToPolar(sobelx, sobely, norm, dir);
            Image<Gray, Single> normImg = norm.ToImage<Gray, Single>();
            Image<Gray, byte> countImg = normImg.Convert<byte>
                (delegate (Single b) { return (byte)(b > ThresVal ? 255 : 0); });

            int nonZeros = CvInvoke.CountNonZero(countImg);
            double ThresNum = 20;
            if (nonZeros > ThresNum)
                TextureFlag = 1;
            else
                TextureFlag = 0;

            return TextureFlag;
        }

        private void primary_MouseClick(object sender, MouseEventArgs e)
        {
            if (start_flag)
            {
                Point graphics_point = new Point(e.X, e.Y);
                wheel_point = graphics_point;
                primary_rectShow(graphics_point);
            }
        }

        private void detect_Button_Click(object sender, EventArgs e)
        {
            l_processShow.Text = "缺陷提取";

            /****************参数归零*****************/
            ImgDraw = new Mat();
            Blocks = new List<defectStruct>();    //块状缺陷
            Lines = new List<defectStruct>();     //线状缺陷（不考虑面积阈值)
            LinesNew = new List<defectStruct>();  //线状缺陷
            LineLinksNew = new List<defectStruct>();   //断线连接
            LineLinks = new List<defectStruct>();      //断线连接（不考虑面积阈值)

            int BWThres = Convert.ToInt32(m_BWThres.Text);
            int SquareThres = Convert.ToInt32(m_squareThres.Text);
            int LengthThres = Convert.ToInt32(m_lengthThres.Text);
            int Length2Square = Convert.ToInt32(m_Length2Square.Text);
            int SquareThresUp = Convert.ToInt32(m_squareThresUp.Text);
            int LengthThresUp = Convert.ToInt32(m_lengthThresUp.Text);

            SquareThresUp = (SquareThresUp == 0) ? 999999 : SquareThresUp;
            LengthThresUp = (LengthThresUp == 0) ? 999999 : LengthThresUp;
            Mat Img2Detect2 = new Mat();
            //  CvInvoke.cvConvertScale(Img2Detect, Img2Detect2, 255, 0);
            CvInvoke.CvtColor(Img2Detect, Img2Detect2, ColorConversion.Bgr2Gray);
            Mat Img = Img2Detect2.Clone();
            CvInvoke.MedianBlur(Img, Img, 5);
            CvInvoke.Threshold(Img, Img, BWThres, 255, ThresholdType.Binary);

            /****************************特征边缘确定********************************/
            Emgu.CV.Util.VectorOfVectorOfPoint Contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            Emgu.CV.IOutputArray hierarchy = new Image<Gray, byte>(Img.Width, Img.Height, new Gray(255));
            CvInvoke.FindContours(Img, Contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxNone);
            Mat ImgBinary = new Mat();
            CvInvoke.DrawContours(ImgBinary, Contours, -1, new MCvScalar(255));

            /****************************缺陷提取********************************/
            CvInvoke.CvtColor(Img, ImgDraw, ColorConversion.Gray2Bgr);

            Emgu.CV.Util.VectorOfPoint curAreas = new Emgu.CV.Util.VectorOfPoint();
            Point TopLeft = new Point();
            Point DownRight = new Point();
            List<RotatedRect> minrectVec = new List<RotatedRect>();
            defectStruct tmpBlock, tmpLine;
            List<double> LineAngle = new List<double>();
            double dLength, dWidth, len2widRadio, defectArea, curAngle;
            int AddRamge = 3, BlockCount = 0, LineCount = 0;

            for (int i = 0; i < Contours.Size; i++)
            {
                curAreas = Contours[Contours.Size - 1 - i];
                TopLeft = curAreas[0];
                DownRight = curAreas[curAreas.Size - 1];
                for (int j = 0; j < curAreas.Size; j++)
                {
                    TopLeft.X = Math.Min(curAreas[j].X, TopLeft.X);
                    TopLeft.Y = Math.Min(curAreas[j].Y, TopLeft.Y);
                    DownRight.X = Math.Max(curAreas[j].X, DownRight.X);
                    DownRight.Y = Math.Max(curAreas[j].Y, DownRight.Y);
                }
                TopLeft.X -= AddRamge;  //拓展3个像素
                TopLeft.Y -= AddRamge;
                DownRight.X += AddRamge;
                DownRight.Y += AddRamge;

                //最小边缘矩形
                RotatedRect rectPoints = CvInvoke.MinAreaRect(curAreas);
                dLength = Math.Max(rectPoints.Size.Width, rectPoints.Size.Height);
                dWidth = Math.Min(rectPoints.Size.Width, rectPoints.Size.Height);
                len2widRadio = (dLength / dWidth);
                len2widRadio = (len2widRadio > 999999) ? 0 : len2widRadio;

                /****************************缺陷分类********************************/
                //块（仅记录，不处理）
                if (len2widRadio < Length2Square)
                {
                    defectArea = CvInvoke.ContourArea(curAreas);
                    if (defectArea >= SquareThres && defectArea <= SquareThresUp)
                    {
                        //CvInvoke.Rectangle(ImgDraw, new Rectangle(TopLeft, DownRight), MCvScalar(0, 0, 255));
                        Rectangle rect = new Rectangle(TopLeft.X, TopLeft.Y, DownRight.X - TopLeft.X, DownRight.Y - TopLeft.Y);
                        CvInvoke.Rectangle(ImgDraw, rect, new MCvScalar(0, 0, 255), 3);
                        string BlockName = "B" + BlockCount.ToString();
                        CvInvoke.PutText(ImgDraw, BlockName, TopLeft, FontFace.HersheySimplex, 2, new MCvScalar(0, 0, 255), 4);
                        BlockCount++;

                        //记录
                        tmpBlock.area = Math.Round(defectArea, 3);
                        tmpBlock.LW_radio = Math.Round(len2widRadio, 3);
                        tmpBlock.minHeight = rectPoints.Size.Height;
                        tmpBlock.minWidth = rectPoints.Size.Width;
                        tmpBlock.TopLeft = TopLeft;
                        tmpBlock.DownRight = DownRight;
                        tmpBlock.defectName = BlockName;
                        Blocks.Add(tmpBlock);
                    }
                }
                //线（先记录，后处理）
                else
                {
                    defectArea = CvInvoke.ContourArea(curAreas);
                    PointF[] fPoints = new PointF[4];
                    fPoints = rectPoints.GetVertices();

                    //Rectangle rect = new Rectangle(TopLeft.X, TopLeft.Y, DownRight.X - TopLeft.X, DownRight.Y - TopLeft.Y);
                    //CvInvoke.Rectangle(ImgDraw, rect, new MCvScalar(0, 255, 0), 2);
                    string LineName = "L" + LineCount.ToString();
                    LineCount++;

                    minrectVec.Add(rectPoints);
                    if (rectPoints.Size.Width < rectPoints.Size.Height)
                        curAngle = Math.Atan((fPoints[1].Y - fPoints[0].Y) / (fPoints[1].X - fPoints[0].X)) * 180 / Math.PI;
                    else
                        curAngle = rectPoints.Angle;
                    LineAngle.Add(curAngle);

                    //缺陷记录
                    tmpLine.area = Math.Round(defectArea, 3);
                    tmpLine.LW_radio = Math.Round(len2widRadio, 3);
                    tmpLine.minHeight = rectPoints.Size.Height;
                    tmpLine.minWidth = rectPoints.Size.Width;
                    tmpLine.TopLeft = TopLeft;
                    tmpLine.DownRight = DownRight;
                    tmpLine.defectName = LineName;
                    Lines.Add(tmpLine);
                }
            }

            /****************************断线连接********************************/
            int comparFlag, count = 0;
            PointF compar1, compar2, compar3, compar4;
            List<int> deleteNum = new List<int>();
            List<defectStruct> tmpDefect = new List<defectStruct>();
            tmpDefect.AddRange(Lines);
            while (LineAngle.Count != 0)
            {
                curAngle = LineAngle[0];
                dWidth = minrectVec[0].Size.Width;
                dLength = minrectVec[0].Size.Height;

                LineLinks.Add(tmpDefect[0]);
                PointF[] tarPoints = new PointF[4];
                tarPoints = minrectVec[0].GetVertices();

                if (dWidth < dLength)
                    comparFlag = 1;
                else
                    comparFlag = 0;

                compar1 = tarPoints[0];
                compar2 = (comparFlag == 1) ? tarPoints[1] : tarPoints[3];

                for (int i = 1; i < LineAngle.Count; i++)
                {
                    if (curAngle <= LineAngle[i] + 5.0 && curAngle >= LineAngle[i] - 5.0)
                    {
                        PointF[] curPoints = new PointF[4];
                        curPoints = minrectVec[i].GetVertices();
                        compar3 = curPoints[0];
                        compar4 = (comparFlag == 1) ? curPoints[1] : curPoints[3];

                        if (compareDistance(compar1, compar4, 20.0) || compareDistance(compar2, compar3, 20.0))
                        {
                            compar1 = compar3;
                            compar2 = compar4;
                            deleteNum.Add(i);
                            LineLinks[count] = AddStruct(LineLinks[count], tmpDefect[i]);
                        }
                    }
                }

                //删除已选出的集合
                for (int k = deleteNum.Count - 1; k >= 0; k--)
                {
                    LineAngle.RemoveAt(deleteNum[k]);
                    minrectVec.RemoveAt(deleteNum[k]);
                    tmpDefect.RemoveAt(deleteNum[k]);
                }
                LineAngle.RemoveAt(0);
                minrectVec.RemoveAt(0);
                tmpDefect.RemoveAt(0);
                deleteNum.Clear();
                count++;
            }

            //对面积区域进行判断
            LineCount = 0;
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].area >= LengthThres && Lines[i].area <= LengthThresUp)
                {
                    tmpLine = Lines[i];
                    tmpLine.defectName = "L" + LineCount.ToString();
                    LinesNew.Add(tmpLine);
                    Rectangle rect = new Rectangle(Lines[i].TopLeft.X, Lines[i].TopLeft.Y,
                        Lines[i].DownRight.X - Lines[i].TopLeft.X, Lines[i].DownRight.Y - Lines[i].TopLeft.Y);
                    CvInvoke.Rectangle(ImgDraw, rect, new MCvScalar(255, 0, 0), 2);
                    LineCount++;
                }
            }

            LineCount = 0;
            for (int i = 0; i < LineLinks.Count; i++)
            {
                if (LineLinks[i].area >= LengthThres && LineLinks[i].area <= LengthThresUp)
                {
                    tmpLine = LineLinks[i];
                    tmpLine.defectName = "L" + LineCount.ToString();
                    LineLinksNew.Add(tmpLine);
                    Rectangle rect = new Rectangle(LineLinks[i].TopLeft.X, LineLinks[i].TopLeft.Y,
                        LineLinks[i].DownRight.X - LineLinks[i].TopLeft.X, LineLinks[i].DownRight.Y - LineLinks[i].TopLeft.Y);
                    CvInvoke.Rectangle(ImgDraw, rect, new MCvScalar(0, 255, 0), 3);
                    CvInvoke.PutText(ImgDraw, tmpLine.defectName, LineLinks[i].TopLeft, FontFace.HersheySimplex, 2, new MCvScalar(0, 255, 0), 4);
                    LineCount++;
                }
            }

            GridDesigner(Blocks, LineLinksNew);   //生成列表

            //detectImage = ImgDraw.ToImage<Bgr, byte>();
            ImgNew = ImgDraw.Clone();
            CvInvoke.Resize(ImgNew, dstImage_small, new Size((int)rectF.Width, (int)rectF.Height), rectF.X, rectF.Y, Inter.Area);
            primary_bmp_small = dstImage_small.ToImage<Bgr, byte>().ToBitmap();

            Point graphics_point = new Point(primaryPic.Width / 2, primaryPic.Height / 2);
            primary_rectShow(graphics_point);

        }

        bool compareDistance(PointF point1, PointF point2, double thresh)
        {
            double distance = (point2.Y - point1.Y) * (point2.Y - point1.Y) +
                (point2.X - point1.X) * (point2.X - point1.X);
            distance = Math.Sqrt(distance);

            if (distance <= thresh)
                return true;
            else
                return false;
        }

        defectStruct AddStruct(defectStruct defect1, defectStruct defect2)
        {
            defectStruct result = new defectStruct();
            result.LW_radio = defect1.LW_radio + defect2.LW_radio;
            result.area = defect1.area + defect2.area;
            result.minHeight = defect1.minHeight + defect2.minHeight;
            result.minWidth = defect1.minWidth + defect2.minWidth;
            result.TopLeft.X = Math.Min(defect1.TopLeft.X, defect2.TopLeft.X);
            result.TopLeft.Y = Math.Min(defect1.TopLeft.Y, defect2.TopLeft.Y);
            result.DownRight.X = Math.Max(defect1.DownRight.X, defect2.DownRight.X);
            result.DownRight.Y = Math.Max(defect1.DownRight.Y, defect2.DownRight.Y);
            return result;
        }

        void GridDesigner(List<defectStruct> Blocks, List<defectStruct> Lines)
        {
            int ItemSum = Blocks.Count + Lines.Count;
            int count = 0;

            //Data Source
            DataTable defect = new DataTable();
            defect.Columns.Add("ItemNum", typeof(string));
            defect.Columns.Add("Len2Square", typeof(double));
            defect.Columns.Add("Coordinate", typeof(Point));
            defect.Columns.Add("DefectArea", typeof(double));

            //Data Save
            for (int i = 0; i < Lines.Count; i++)
            {
                defect.Rows.Add(new object[] { Lines[i].defectName, Lines[i].LW_radio,
                    Lines[i].TopLeft, Lines[i].area });
                count++;
            }
            for (int i = 0; i < Blocks.Count; i++)
            {
                defect.Rows.Add(new object[] { Blocks[i].defectName, Blocks[i].LW_radio,
                    Blocks[i].TopLeft, Blocks[i].area });
                count++;
            }

            g_defectLab.DataSource = defect;
        }

        private void gridView1_RowClick(object sender, DevExpress.XtraGrid.Views.Grid.RowClickEventArgs e)
        {
            //int test = 1;
            int selectedHandle;
            selectedHandle = this.gridView1.GetSelectedRows()[0];
            Point selectedPoint = new Point();
            string selectedMessage = this.gridView1.GetRowCellValue(selectedHandle, "Coordinate").ToString();
            string[] splitMessage = selectedMessage.Split(',');
            splitMessage[1] = splitMessage[1].Substring(0, splitMessage[1].Length - 1);
            selectedPoint = new Point(int.Parse(splitMessage[0].Substring(3)), int.Parse(splitMessage[1].Substring(2)));

            PointF _changePoint = new PointF();
            float scaleX = (float)primaryPic.Width / (float)primary_bmp.Width;
            float scaleY = (float)primaryPic.Height / (float)primary_bmp.Height;
            _changePoint.X = selectedPoint.X * scaleX;
            _changePoint.Y = selectedPoint.Y * scaleY;

            Point graphics_point = new Point();
            graphics_point.X = (int)_changePoint.X;
            graphics_point.Y = (int)_changePoint.Y;
            primary_rectShow(graphics_point);

        }

        private void gridView1_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {

        }

        //读取荧光图像
        private void m_ReadFluore_Click(object sender, EventArgs e)
        {
            ImgNew = new Mat();
            dstImage_small = new Mat();
            openFileDialog1.Filter = "*bmp|*BMP";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog1.FileName;
                _matFluor = CvInvoke.Imread(filePath, ImreadModes.AnyColor);

                secondaryPic.Image = _matFluor.ToImage<Gray, byte>().ToBitmap();

                //ImgNew = _matFluor.Clone();
                //primary_bmp = _matFluor.ToImage<Gray, byte>().ToBitmap();

                //primary_bmp_small = ImageZoom(_matFluor, ref rectF);
                //rectBmp = new Bitmap(primaryPic.Width, primaryPic.Height);
                //graphics_bmp = Graphics.FromImage(rectBmp);
                //p = new Pen(Color.Red, 2);

                //start_flag = true; //可选打开！
                //SendEvent += new SendEventHandler(secondaryPic_show);
                //Point graphics_point = new Point(primaryPic.Width / 2, primaryPic.Height / 2);
                //primary_rectShow(graphics_point);
            }

        }

        //读取散射光图像
        private void m_ReadScatter_Click(object sender, EventArgs e)
        {
            ImgNew = new Mat();
            dstImage_small = new Mat();
            openFileDialog1.Filter = "*bmp|*BMP";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog1.FileName;
                _matScatter = CvInvoke.Imread(filePath, ImreadModes.AnyColor);

                secondaryPic.Image = _matScatter.ToImage<Gray, byte>().ToBitmap();

                //ImgNew = _matScatter.Clone();
                //primary_bmp = _matScatter.ToImage<Gray, byte>().ToBitmap();

                //primary_bmp_small = ImageZoom(_matScatter, ref rectF);
                //rectBmp = new Bitmap(primaryPic.Width, primaryPic.Height);
                //graphics_bmp = Graphics.FromImage(rectBmp);
                //p = new Pen(Color.Red, 2);

                //start_flag = true; //可选打开！
                //SendEvent += new SendEventHandler(secondaryPic_show);
                //Point graphics_point = new Point(primaryPic.Width / 2, primaryPic.Height / 2);
                //primary_rectShow(graphics_point);
            }
        }

        //图像加法
        private void m_addition_Click(object sender, EventArgs e)
        {
            Mat matFluor_Clone = _matFluor.Clone();
            Mat matScatter_Clone = _matScatter.Clone();
            Mat _matAdd = new Mat(matFluor_Clone.Size, DepthType.Cv8U, 3);
            CvInvoke.CvtColor(matFluor_Clone, matFluor_Clone, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(matScatter_Clone, matScatter_Clone, ColorConversion.Bgr2Gray);

            CvInvoke.Threshold(matFluor_Clone, matFluor_Clone, 10, 255, ThresholdType.Binary);
            CvInvoke.Threshold(matScatter_Clone, matScatter_Clone, 10, 255, ThresholdType.Binary);
            _matAdd.SetTo(new MCvScalar(0, 0, 255), matFluor_Clone);
            _matAdd.SetTo(new MCvScalar(255, 0, 0), matScatter_Clone);

            ImgNew = _matAdd.Clone();
            primary_bmp = ImgNew.ToImage<Bgr, byte>().ToBitmap();

            primary_bmp_small = ImageZoom(ImgNew, ref rectF);
            rectBmp = new Bitmap(primaryPic.Width, primaryPic.Height);
            graphics_bmp = Graphics.FromImage(rectBmp);
            p = new Pen(Color.Red, 2);

            start_flag = true; //可选打开！
            //SendEvent += new SendEventHandler(secondaryPic_show);
            Point graphics_point = new Point(primaryPic.Width / 2, primaryPic.Height / 2);
            primary_rectShow(graphics_point);
        }

        //图像减法
        private void m_subtraction_Click(object sender, EventArgs e)
        {
            int thres = Convert.ToInt32(t_dir_range.Text);                  //阈值法阈值
            blur_para = Convert.ToInt32(t_sal_range.Text);                  //判别区域
            liner_corr_power = Convert.ToDouble(t_sal_int.Text);             //分割强度
            int SturctElementSize = Convert.ToInt32(t_dilateExp.Text);      //膨胀范围
            SturctElementSize = (SturctElementSize - 1) / 2;

            Mat matFluor_Clone = _matFluor.Clone();
            Mat matScatter_Clone = _matScatter.Clone();

            Mat element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(SturctElementSize, SturctElementSize), new Point(-1, -1));
            CvInvoke.Dilate(matScatter_Clone, matScatter_Clone, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));

            Mat _matAnd = new Mat(matFluor_Clone.Size, DepthType.Cv8U, 1);
            Mat _matSub = new Mat(matFluor_Clone.Size, DepthType.Cv8U, 1);
            _matSub = matFluor_Clone.Clone();
            if (this.tabPane2.SelectedPage == method_1)
            {
                CvInvoke.CvtColor(matScatter_Clone, matScatter_Clone, ColorConversion.Bgr2Gray);
                CvInvoke.Threshold(matScatter_Clone, matScatter_Clone, thres, 255, ThresholdType.Binary);
            }
            else
            {
                CvInvoke.CvtColor(matScatter_Clone, matScatter_Clone, ColorConversion.Bgr2Gray);
                matScatter_Clone = SignificanceProc(matScatter_Clone);
            }

            CvInvoke.CvtColor(matFluor_Clone, matFluor_Clone, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(matFluor_Clone, matFluor_Clone, 20, 255, ThresholdType.Binary);
            CvInvoke.BitwiseAnd(matFluor_Clone, matScatter_Clone, _matAnd);
            CvInvoke.BitwiseNot(matFluor_Clone, matFluor_Clone);
            matFluor_Clone = RegionGrow(matFluor_Clone, _matAnd);
            _matSub.SetTo(new MCvScalar(0, 0, 0), matFluor_Clone); //图像相减结果

            ImgNew = _matSub.Clone();
            primary_bmp = ImgNew.ToImage<Gray, byte>().ToBitmap();

            primary_bmp_small = ImageZoom(ImgNew, ref rectF);
            rectBmp = new Bitmap(primaryPic.Width, primaryPic.Height);
            graphics_bmp = Graphics.FromImage(rectBmp);
            p = new Pen(Color.Red, 2);

            start_flag = true; //可选打开！
            //SendEvent += new SendEventHandler(secondaryPic_show);
            Point graphics_point = new Point(primaryPic.Width / 2, primaryPic.Height / 2);
            primary_rectShow(graphics_point);
        }

        //显著性算法
        Mat SignificanceProc(Mat srcMat)
        {

            Mat _matGauss = srcMat.Clone();  //tmp1
            Mat _matBlur = srcMat.Clone();   //tmp2
            Mat _matMedian = srcMat.Clone(); //tmp3

            CvInvoke.GaussianBlur(srcMat, _matGauss, new Size(3, 3), 5);
            CvInvoke.Blur(srcMat, _matBlur, new Size(blur_para, blur_para), new Point(-1, -1));
            Image<Gray, byte> _ImgGauss = _matGauss.ToImage<Gray, byte>();
            Image<Gray, byte> _ImgBlur = _matBlur.ToImage<Gray, byte>();
            Image<Gray, byte> _ImgMedian = _matMedian.ToImage<Gray, byte>();

            _ImgMedian = _ImgGauss - _ImgBlur;
            double maxVal = 0.0, minVal = 0.0;
            Point maxLoc = new Point(0, 0); Point minLoc = new Point(0, 0);
            CvInvoke.MinMaxLoc(_ImgMedian, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

            Image<Gray, Single> _ImgPow = _ImgMedian.Convert<Gray, Single>();
            CvInvoke.Pow(_ImgPow, liner_corr_power, _ImgPow);
            double maxPara = Math.Pow(maxVal, liner_corr_power);
            _ImgPow = _ImgPow / maxPara;
            Image<Gray, byte> dstImg = _ImgPow.Convert<byte>(delegate (Single b) { return (byte)(b > 0.7 ? 255 : 0); });

            return dstImg.Mat;
        }

        Mat RegionGrow(Mat src, Mat markers)
        {
            Emgu.CV.Util.VectorOfVectorOfPoint contourPoints = new Emgu.CV.Util.VectorOfVectorOfPoint();
            Emgu.CV.IOutputArray hierarchy = new Image<Gray, byte>(src.Width, src.Height, new Gray(255));
            CvInvoke.FindContours(markers, contourPoints, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxNone);

            const int count = 8;
            int[][] dir = new int[count][] { new int[2]{ -1, -1 }, new int[2] { 0, -1 },
                new int[2]{ 1, -1 }, new int[2]{ 1, 0 }, new int[2]{ 1, 1 },
                new int[2]{ 0, 1 }, new int[2]{ -1, 1 }, new int[2]{ -1, 0 } };

            Queue<Point> seed;
            Image<Gray, byte> _srcImage = src.ToImage<Gray, byte>();
            for (int k = 0; k < contourPoints.Size; k++)
            {
                Point _point = new Point(contourPoints[k][0].X, contourPoints[k][0].Y);
                seed = new Queue<Point>();
                seed.Enqueue(_point);

                while (seed.Count != 0)
                {
                    Point current = seed.Dequeue();
                    for (int i = 0; i < count; i++)
                    {
                        Point pivot = new Point();
                        pivot.X = current.X + dir[i][1];
                        pivot.Y = current.Y + dir[i][0];

                        if (pivot.X < 0 || pivot.X > src.Cols - 1 || pivot.Y < 0 || pivot.Y > src.Rows - 1)
                            continue;

                        if (_srcImage.Data[pivot.Y, pivot.X, 0] == 0)
                        {
                            _srcImage.Data[pivot.Y, pivot.X, 0] = 255;
                            seed.Enqueue(pivot);
                        }
                    }
                }
            }

            Mat res = _srcImage.Mat;
            return res;
        }

        private void simpleButton14_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.FileName = "亚表面缺陷检测报告";
            saveDialog.Filter = "word文件(*.doc)|*.doc";
            saveDialog.ShowDialog();
            reportpath = saveDialog.FileName;


            wordApp = new Microsoft.Office.Interop.Word.ApplicationClass();
            string filename = "F:\\亚表面检测结果输出报告.dot";
            object path = filename;

            object missing = System.Reflection.Missing.Value;

            object readOnly = false;
            //
            wordApp.DisplayAlerts = Microsoft.Office.Interop.Word.WdAlertLevel.wdAlertsNone;

            wordApp.Visible = true;
          
            wordDoc = wordApp.Documents.Open(ref path, ref missing, ref readOnly, 
                ref missing, ref missing, ref missing, ref missing, ref missing,
                ref missing, ref missing, ref missing, ref missing, ref missing, 
                ref missing, ref missing, ref missing);


            // wordDoc = wordApp.Documents.Add(ref missing, ref missing, ref missing, ref missing);
            /************************************************开头***************************************/
            wordApp.Selection.EndKey(Microsoft.Office.Interop.Word.WdUnits.wdStory);// 光标移动至文档的末尾
            wordApp.Selection.TypeParagraph();//另起一段

            Microsoft.Office.Interop.Word.Paragraph wp = wordDoc.Content.Paragraphs.Add(missing);
            DateTime date = DateTime.Now;
            wp.Range.Text = "日期: " + date.ToString("D", System.Globalization.CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat);


            /************************************************插入图片***************************************/
            wordApp.Selection.EndKey(Microsoft.Office.Interop.Word.WdUnits.wdStory);// 光标移动至文档的末尾
            wordApp.Selection.TypeParagraph();//另起一段
            wordApp.Selection.ParagraphFormat.LeftIndent = wordApp.Application.CentimetersToPoints(0);//左缩进
            wordApp.Selection.ParagraphFormat.FirstLineIndent = wordApp.Application.CentimetersToPoints(-1);
            wordApp.Selection.ParagraphFormat.CharacterUnitFirstLineIndent = -1;
            wordApp.Selection.ParagraphFormat.CharacterUnitLeftIndent = 0;
            wordApp.Selection.ParagraphFormat.Alignment =
                Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
            Object linkToFile = false;    //图片是否为外部链接
            Object saveWithDocument = true; //图片是否随文档一起保存
       
            Bitmap wordImage ;
            Mat wordImg = new Mat();
            CvInvoke.Resize(ImgNew, wordImg, new Size(500,500));
            wordImage = wordImg.ToImage<Bgr, byte>().ToBitmap();

            wordImage.Save("F:\\primary_bmp_small.bmp");
            wordDoc.InlineShapes.AddPicture("F:\\primary_bmp_small.bmp", ref linkToFile,
                ref saveWithDocument, ref missing);

            /************************************************插入表格***************************************/
            int rows = Blocks.Count() + LineLinksNew.Count() + 4, cols = 4;
            wordApp.Selection.EndKey(Microsoft.Office.Interop.Word.WdUnits.wdStory);// 光标移动至文档的末尾

            wordApp.Selection.TypeParagraph();//另起一段
            wordApp.Selection.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
            Microsoft.Office.Interop.Word.Table t = wordDoc.Tables.Add(wordApp.Selection.Range, rows, cols, Type.Missing, Type.Missing);
            t.Borders.Enable = 1;
            t.AllowAutoFit = true;//允许自适应大小               
            t.Range.Cells.VerticalAlignment = Microsoft.Office.Interop.Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;//设置单元格竖直方向居中
            
            t.Range.Font.Size = 11;//设置单元格字体大小为默认大小 11

            t.Range.Font.Bold = 0;//设置单元格字体加粗样式为:无，即不加粗

            t.Cell(1, 1).Range.Font.Bold = 1;           
            t.Cell(1, 1).Range.Text = "线信息统计";
            string linesnum = "共计" + LineLinksNew.Count.ToString() + "条线缺陷";
            t.Cell(1, 2).Range.Text = linesnum;
            t.Cell(2, 1).Range.Text = "线号";
            t.Cell(2, 2).Range.Text = "线块比";
            t.Cell(2, 3).Range.Text = "坐标";
            t.Cell(2, 4).Range.Text = "面积";
            for(int i=0;i<LineLinksNew.Count();i++)
            {
                t.Cell(i+3, 1).Range.Text = (i + 1).ToString();
                t.Cell(i+3, 2).Range.Text = LineLinksNew[i].LW_radio.ToString();
                string location = LineLinksNew[i].TopLeft.ToString() + "/" + LineLinksNew[i].DownRight.ToString();
                t.Cell(i+3, 3).Range.Text = location;
                t.Cell(i+3, 4).Range.Text = LineLinksNew[i].area.ToString();
            }
            int bStart = LineLinksNew.Count() + 3;
            t.Cell(bStart, 1).Range.Text = "块信息统计";
            t.Cell(bStart, 1).Range.Font.Bold = 1;
            t.Cell(bStart, 2).Range.Text = "共计" + Blocks.Count().ToString() + "块缺陷";
            t.Cell(bStart+1, 1).Range.Text = "块号";
            t.Cell(bStart+1, 2).Range.Text = "线块比";
            t.Cell(bStart + 1, 3).Range.Text = "坐标";
            t.Cell(bStart + 1, 4).Range.Text = "面积";
            for (int i = 0; i < Blocks.Count(); i++)
            {
                t.Cell(i + bStart + 2, 1).Range.Text = (i + 1).ToString();
                t.Cell(i + bStart + 2, 2).Range.Text = Blocks[i].LW_radio.ToString();
                string location = Blocks[i].TopLeft.ToString() + "/" + Blocks[i].DownRight.ToString();
                t.Cell(i + bStart + 2, 3).Range.Text = location;
                t.Cell(i + bStart + 2, 4).Range.Text = Blocks[i].area.ToString();
            }
            /************************************************保存文件***************************************/
            object fileName = reportpath;
            object format = Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatDocument;//保存格式
            object miss = System.Reflection.Missing.Value;
            wordDoc.SaveAs(ref fileName, ref format, ref miss,
              ref miss, ref miss, ref miss, ref miss,
              ref miss, ref miss, ref miss, ref miss,
              ref miss, ref miss, ref miss, ref miss,
              ref miss);
           
           


        }
        public string reportpath;
        private void simpleButton10_Click(object sender, EventArgs e)
        {
          
        }

        private void simpleButton9_Click(object sender, EventArgs e)
        {
            ImgNew = Imgnew2;
        }

        //private Point Centerpoint = new Point();
        private void primary_MouseDown(object sender, MouseEventArgs e)
        {
            if (start_flag)
            {
                //Centerpoint.X = e.X;
                //Centerpoint.Y = e.Y;
                catch_flag = true;
            }
        }

        private void secondaryPic_Paint(object sender, PaintEventArgs e)
        {
            if(Imgnew2 != null&&catch_flag)
            {
                int graphics_x = graphics_point1.X - graphics_width / 2;
                int graphics_y = graphics_point1.Y - graphics_height / 2;


                //越界处理
                //x
                if (graphics_x < 0)
                    graphics_x = 0;
                else if (graphics_x + graphics_width > primaryPic.Width - 1)
                    graphics_x = primaryPic.Width - 1 - graphics_width;

                //y
                if (graphics_y < 0)
                    graphics_y = 0;
                else if (graphics_y + graphics_height > primaryPic.Height - 1)
                    graphics_y = primaryPic.Height - 1 - graphics_height;


                //坐标变换
                rectRed = new RectangleF();  //转换到实际图像上的尺寸
                float scaleX = primary_bmp.Width * 1.0F / primaryPic.Width;
                float scaleY = primary_bmp.Height * 1.0F / primaryPic.Height;
                if (scaleX < scaleY)
                {
                    rectRed.X = graphics_x * scaleX;
                    rectRed.Y = graphics_y * scaleX;
                    rectRed.Width = graphics_width * scaleX;
                    rectRed.Height = graphics_height * scaleX;
                }
                else
                {
                    rectRed.X = graphics_x * scaleY;
                    rectRed.Y = graphics_y * scaleY;
                    rectRed.Width = graphics_width * scaleY;
                    rectRed.Height = graphics_height * scaleY;
                }
     
                if (secondary_bmp1 != null)
                {
                    secondary_bmp1.Dispose();
                }
                Mat SecondaryImg = new Mat(Imgnew2, new Rectangle((int)rectRed.X,
                    (int)rectRed.Y, (int)rectRed.Width, (int)rectRed.Height));
                Emgu.CV.Image<Bgr, byte> tempImg;
                tempImg = SecondaryImg.ToImage<Bgr, byte>();
                secondary_bmp1 = tempImg.ToBitmap();
                tempImg.Dispose();
                SecondaryImg.Dispose();
                secondaryPic.Image = secondary_bmp1;
                
            }
        }
        Bitmap secondary_bmp1;

        struct ScanPara
        {
            int M;
            int N;
            double length;
            double field;
        }

        private void scan_Click(object sender, EventArgs e)
        {
             /*************************************CCD open*********************************/
    
            /*****************************************************************************/
         /*
            mstart = Convert.ToInt32(mstart_Label.Text);
            mend = Convert.ToInt32(mend_Label.Text);
            nstart = Convert.ToInt32(nstart_Label.Text);
            nend = Convert.ToInt32(nend_Label.Text);
            mtotal = mend - mstart + 1;
            ntotal = nend - nstart + 1;
            string[] names;
            names = Directory.GetFiles(folderName, "*jpg", SearchOption.AllDirectories);
            int picNum;
            picNum = mtotal * ntotal;
           
            picNum = 9;/////////////////////////////////////////////////////
 //           int picStart=mstart*nend
            for (int i = 0; i < picNum; i++)
            {
                Image<Bgr, byte> temp = new Image<Bgr, byte>(names[i]);
                Images.Add(temp);
            }
            */
     //       Thread Tscan = new Thread(new ParameterizedThreadStart(SCAN));
     //       ScanPara para = new ScanPara();
     //       Tscan.Start((object)para);

            /*
            Stitching_setup _setup = new Stitching_setup();
            correctMax = Convert.ToInt32(t_correctMax.Text);
            correctMin = Convert.ToInt32(_setup.t_correctMin.Text);
            overrange = Convert.ToInt32(_setup.t_overRange.Text);
            lineCorrection = Convert.ToDouble(_setup.t_lineCorrection.Text);
            suppressMin = Convert.ToInt32(_setup.t_suppressMin.Text);
            salRange = Convert.ToInt32(_setup.t_salRange.Text);
            reductionRadio = Convert.ToDouble(_setup.t_reductRadio.Text);
            back_uniform = backgroud_radio1.Checked;

            viewSize = Convert.ToDouble(t_viewSize.Text);
            moveStep = Convert.ToDouble(t_moveStep.Text);
            viewSize = Viewsize;
            moveStep = Xstep;
            exchange_rate = viewSize / m_pixel * 1000;
            overlap_rate = (viewSize - moveStep) / viewSize;


            srcImage_height = 1024;
            srcImage_width = 1280;
         
            mtotal = M;
            ntotal = N;
            dstImage_height = (int)(mtotal * srcImage_height - (mtotal-1) * overlap_rate * srcImage_height + overrange * 2);
            dstImage_width = (int)(ntotal * srcImage_width - (ntotal-1) * overlap_rate * srcImage_width + overrange * 2);
            dstImage = new Image<Gray, byte>(dstImage_width - 1, dstImage_height - 1);            
           */
            stitch();
          
             
        }
        private void __CaptureCallbackPro(object objUserParam, IFrameData objIFrameData)
        {
            try
            {
                Form1 objGxSingleCam = objUserParam as Form1;
                objGxSingleCam.ImageShowAndSave(objIFrameData);
            }
            catch (Exception)
            {
            }
        }
        void ImageShowAndSave(IFrameData objIFrameData)
        {
            m_objGxBitmap.Show(objIFrameData);
            DateTime dtNow = System.DateTime.Now;  // 获取系统当前时间
            string strDateTime = dtNow.Year.ToString() + "_"
                               + dtNow.Month.ToString() + "_"
                               + dtNow.Day.ToString() + "_"
                               + dtNow.Hour.ToString() + "_"
                               + dtNow.Minute.ToString() + "_"
                               + dtNow.Second.ToString() + "_"
                               + dtNow.Millisecond.ToString();

            string stfFileName = "F:\\stitch\\" + strDateTime + ".jpg";  // 默认的图像保存名称
            m_objGxBitmap.SaveBmp(objIFrameData, stfFileName);

        }
        List<Image<Bgr, byte>> Images = new List<Image<Bgr, byte>>();
        void SCAN(object param)
        {
            ScanPara para = (ScanPara)param;
            /******************************   导轨移动+CCD    *****************************/
            if(MT_API.MT_Open_USB() !=0)
            {
                MessageBox.Show("USB连接失败");
                return;
            }
            int mall = 3, nall = 3;
            MT_API.MT_Set_Axis_Mode_Position(0);
            MT_API.MT_Set_Axis_Mode_Position(1);
            MT_API.MT_Set_Axis_Mode_Position(2);
            double Xrel = 5;
            double Yrel= 4;
            int PXRel =(int) Xrel * 3200;
            int PYRel = (int)Yrel * 3200;
            m_objIGXFeatureControl.GetCommandFeature("TriggerSoftware").Execute();
            for (int i=0;i<mall;i++)
            {
                if(i%2==0)
                {
                    for (int j = 0; j<nall; j++)
                    {
                        MT_API.MT_Set_Axis_Position_P_Target_Rel(0, PXRel);
                        isover(0);
                        m_objIGXFeatureControl.GetCommandFeature("TriggerSoftware").Execute();
                    }
                }
                else
                {
                    for(int j=nall-1;j>=0;j--)
                    {
                        MT_API.MT_Set_Axis_Position_P_Target_Rel(0, -PXRel);
                        isover(0);
                        m_objIGXFeatureControl.GetCommandFeature("TriggerSoftware").Execute();
                    }
                }
                MT_API.MT_Set_Axis_Position_P_Target_Rel(2, PYRel);
                isover(0);
                m_objIGXFeatureControl.GetCommandFeature("TriggerSoftware").Execute();
            }


        }
        private bool isover(ushort idx)
        {
            byte over = 1;
            byte Dir=1;
            byte Neg=1;
            byte Pos=1;
            byte Zero=1;
            byte Mode=1;
            for(;over!=0;)
            {
                MT_API.MT_Get_Axis_Status(idx, ref over, ref Dir, ref Neg, ref Pos, ref Zero,ref Mode);
            }
            return true;
        }
        private void __SetEnumValue(string strFeatureName, string strValue, IGXFeatureControl objIGXFeatureControl)
        {

            if (null != objIGXFeatureControl)
            {
                //设置当前功能值
                objIGXFeatureControl.GetEnumFeature(strFeatureName).SetValue(strValue);
            }

        }
        int stitchmethod = 1;
     

        private void primary_MouseMove(object sender, MouseEventArgs e)
        {
            if (catch_flag)
            {
                Point graphics_point = new Point(e.X, e.Y);
                wheel_point = graphics_point;
                primary_rectShow(graphics_point);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (null != m_objIGXFeatureControl)
            {
                m_objIGXFeatureControl.GetCommandFeature("AcquisitionStop").Execute();
            }
            if (null != m_objIGXStream)
            {
                m_objIGXStream.StopGrab();
                m_objIGXStream.UnregisterCaptureCallback();
                m_objIGXStream.Close();
                m_objIGXStream = null;
            }
            if (null != m_objIGXDevice)
            {
                m_objIGXDevice.Close();
                m_objIGXDevice = null;
            }
            if (null != m_objIGXFactory)
            {
                m_objIGXFactory.Uninit();
            }
        }

        private void primary_MouseUp(object sender, MouseEventArgs e)
        {
            catch_flag = false;
        }
        string folderName;
        private void openFile_Click(object sender, EventArgs e)
        {

            
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择图片地址";
            //dialog.RootFolder = Environment.SpecialFolder.Personal;
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                folderName = dialog.SelectedPath;
                files = Directory.GetFiles(folderName, "*tiff", SearchOption.AllDirectories);
                int fileSize = files.Length;
                string fileEnd = files[fileSize - 1].Substring(folderName.Length + 1);
                string fileStart = files[0].Substring(folderName.Length + 1);

                mstart = Convert.ToInt32(fileStart.Substring(0, 3));
                nstart = Convert.ToInt32(fileStart.Substring(3, 3));
                mend = Convert.ToInt32(fileEnd.Substring(0, 3));
                nend = Convert.ToInt32(fileEnd.Substring(3, 3));

                file_rowNum = mend;
                file_colNum = nend;

                mstart_Label.Text = mstart.ToString();
                mend_Label.Text = mend.ToString();
                nstart_Label.Text = nstart.ToString();
                nend_Label.Text = nend.ToString();
            }
            


            ////定义打开文件的类型
            //openFileDialog1.Filter = "*tiff|*TIFF";
            ////获取打开文件返回值
            //if (openFileDialog1.ShowDialog() == DialogResult.OK)
            //{
            //    string filePath = openFileDialog1.FileName;//文件路径
            //    FileStream fs = new FileStream(filePath, FileMode.Open); //文件名
            //    byte[] picturebytes;
            //    picturebytes = new byte[fs.Length];

            //    BinaryReader br = new BinaryReader(fs);
            //    picturebytes = br.ReadBytes(Convert.ToInt32(fs.Length));
            //    MemoryStream ms = new MemoryStream(picturebytes); //系统内存的读写操作
            //    primary_bmp = new Bitmap(ms);
            //    //primaryPic.Image = primary_bmp;

            //    width = primary_bmp.Width;    //实图宽度
            //    height = primary_bmp.Height;  //实图高度

            //    BitmapInfo bmpt = GetImagePixel(primary_bmp);
            //    int step = bmpt.Step;
            //    int gstep;
            //    IntPtr primary_pic_small = primaryResize(bmpt.Result, width, height, primaryPic.Width, primaryPic.Height, out gstep);
            //    primary_bmp_small = new Bitmap(primaryPic.Width, primaryPic.Height, gstep,
            //         System.Drawing.Imaging.PixelFormat.Format24bppRgb, primary_pic_small);
            //    primaryPic.Image = primary_bmp_small;

            //    float pic_width = (float)primaryPic.Width; //计算用
            //    float wwidth = (float)width;
            //    fscale = wwidth / pic_width;
            //    ROI_width = (int)(graphics_width * fscale);
            //    ROI_height = (int)(graphics_height * fscale);
            //    //string test_string = primary_bmp.PixelFormat.ToString();
            //    fs.Dispose();
            //}
        }

        //内存清理
        private static void SetTimer()
        {
            System.Timers.Timer aTimer = new System.Timers.Timer(); //初始化定时器
            aTimer.Interval = 60000;//配置时间1分钟
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            aTimer.AutoReset = true;//每到指定时间Elapsed事件是到时间就触发
            aTimer.Enabled = true; //指示 Timer 是否应引发 Elapsed 事件。
        }
        //  Mat Imgnew2 = CvInvoke.Imread("F:\\tempres2.jpg");
        Mat Imgnew2 ;
        private void Save_button_Click(object sender, EventArgs e)
        {
            
                     KeyValuePair<ImwriteFlags, Int32> idx = new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.JpegQuality, 30);           
                     System.Collections.Generic.KeyValuePair<ImwriteFlags, Int32>[] flags = new KeyValuePair<ImwriteFlags, Int32>[1];
                     flags[0] = idx;
                     CvInvoke.Imwrite("F:\\拼接结果.jpg", ImgNew, flags);                    
          
       }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

        }
    }

    public struct defectStruct
    {
        public double LW_radio;   //长宽比
        public double area;       //面积
        public double minWidth;   //最小矩形宽度
        public double minHeight;  //最小矩形高度
        public Point TopLeft;     //最大矩形左上角坐标
        public Point DownRight;   //最大矩形右下角坐标
        public string defectName; //缺陷名
    }

    public class BitmapInfo
    {
        public byte[] Result { get; set; }
        public int Step { get; set; }
    }

    //调色板
    public static class CvToolbox
    {

        // #region Color Pallette  
        /// <summary>  
        /// The ColorPalette of Grayscale for Bitmap Format8bppIndexed  
        /// </summary>  
        public static readonly ColorPalette GrayscalePalette = GenerateGrayscalePalette();

        private static ColorPalette GenerateGrayscalePalette()
        {
            using (Bitmap image = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
            {
                ColorPalette palette = image.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                return palette;
            }
        }
    }

}


