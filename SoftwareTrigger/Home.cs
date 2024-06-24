using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ThridLibray;
using System.IO;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using S7.Net;

namespace AutomatedVisualInspectionClipFormSilicon
{
    public partial class Home : Form
    {
        bool m_bStartGrab = false;          
        Mutex m_mutex = new Mutex(); 
        Stopwatch m_grabTime = new Stopwatch();    
        private Graphics _g = null; 
        private const string SdkFilePath = @".\ImageConvert.dll";
        IntPtr m_pDstRGB = IntPtr.Zero; 
        List<IFrameRawData> m_frameList = new List<IFrameRawData>(); 
        Thread readerThread = null; 
        bool m_bShowLoop = true;
        Bitmap m_bitmap = null;
        IFrameRawData m_data;
        int m_framewidth;
        int m_frameheight;
        IMGCNV_SOpenParam m_oParam;
        Rectangle m_bitmapRect;
        Rectangle m_pictureBoxRect;
        BitmapData m_bmpData;
        bool isPass;
        private Plc plc;
        string readPLC;       
        string writePLC;
        string resetPlc;
        private IDevice m_dev;
        Bitmap Bit;
        Bitmap BinaryImage;
        Bitmap resizeImg;
        Bitmap resultClosing;
       int x1_clipheadXD083, y1_clipheadXD083, x11_clipheadXD083, y11_clipheadXD083, width1, height1, x2_clipheadXD083, y2_clipheadXD083, x22_clipheadXD083, y22_clipheadXD083, width2, height2;
       int x1_clipedgeXD083, x11_clipedgeXD083, y1_clipedgeXD083, y11_clipedgeXD083, width3, height3;
       int x2_clipedgeXD083, x22_clipedgeXD083, y2_clipedgeXD083, y22_clipedgeXD083, width4, height4;
        int x_form, y_form,x1_form,y1_form, width_form, height_form;
        int x1_whiteClip, y1_whiteClip, x11_whiteClip, y11_whiteClip, width_whiteClip, height_whiteClip;
        int x2_whiteClip, y2_whiteClip, x22_whiteClip, y22_whiteClip, width1_whiteClip, height1_whiteClip;
        int passCount = 0;
        int faileCount = 0;
        int total = 0;
        string folderPath = "";
        string productName = "";
        string productStatus = "";
        string checkDescription = "";
        private byte threshold;
        private byte thresholdXD083;
        int minPixelSilicon;
        int maxPixelSilicon;
        int minPixelSilicon1;
        int maxPixelSilicon1;
        int minPixelClipXD081;    
        int minPixelClip1XD081;
        int minPixelClipXD083;    
        int minPixelClip1XD083;    
        int limitPixelForm;
        int indexCombobox;
        private DateTime timeStart;
        private DateTime timeEnd;
        [DllImport(SdkFilePath, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMGCNV_ConvertToBGR24_Ex(IntPtr pSrcData, ref IMGCNV_SOpenParam pOpenParam, IntPtr pDstData, ref int pDstDataSize, IMGCNV_EBayerDemosaic eBayerDemosaic);

        public enum IMGCNV_EBayerDemosaic
        {
            IMGCNV_DEMOSAIC_NEAREST_NEIGHBOR,        
            IMGCNV_DEMOSAIC_BILINEAR,                
            IMGCNV_DEMOSAIC_EDGE_SENSING,            
            IMGCNV_DEMOSAIC_NOT_SUPPORT = 255,       
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct IMGCNV_SOpenParam
        {
            public int width;                          
            public int height;                         
            public int paddingX;                       
            public int paddingY;                        
            public int dataSize;                        
            public uint pixelForamt;                    
        }
        /// <summary>
        /// </summary>
        /// <param name="pDst"></param>
        /// <param name="pSrc"></param>
        /// <param name="len"></param>
        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory", CharSet = CharSet.Ansi)]
        internal static extern void CopyMemory(IntPtr pDst, IntPtr pSrc, int len);
        public Home()
        {
            InitializeComponent();

         //  this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            InitializeDisplayThread();
        }
        // Khởi tạo luồng hiển thị ảnh
        private void InitializeDisplayThread()
        {
            btnClose.Enabled = false;
            btnSoftwareTrigger.Enabled = false;
            // nếu k có luồng thực  thi thì nó tạo 1 luồng thực thi mới và chạy
            if (null == readerThread)
            {
                readerThread = new Thread(new ThreadStart(ShowThread));
                readerThread.Start();
            }           
            m_oParam = new IMGCNV_SOpenParam();   
            m_bitmapRect = new Rectangle();            
            m_bitmapRect.X = 0;
            m_bitmapRect.Y = 0;            
            m_pictureBoxRect = new Rectangle(0, 0, pbImage.Width, pbImage.Height);
            m_bmpData = new BitmapData();
        }
        // camera open event callback 
        private void OnCameraOpen(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                btnOpen.Enabled = false;
                btnSoftwareTrigger.Enabled = true;
                btnClose.Enabled = true;         
            }));
        }
        // camera close event callback 
        private void OnCameraClose(object sender, EventArgs e)
        {
            if (m_frameList.Count > 0)
            {
                m_mutex.WaitOne();
                m_frameList.Clear();
                m_mutex.ReleaseMutex();
            }
            if (m_bitmap != null)
            {
                m_bitmap.Dispose();
                m_bitmap = null;
            }
            this.Invoke(new Action(() =>
            {
                btnOpen.Enabled = true;
                btnSoftwareTrigger.Enabled = false;
                btnClose.Enabled = false;           
            }));
        }
        // camera disconnect event callback 
        private void OnConnectLoss(object sender, EventArgs e)
        {
            m_bStartGrab = false;
            readerThread.Join(100);     
            if (m_frameList.Count > 0)
            {
                m_mutex.WaitOne();
                m_frameList.Clear();
                m_mutex.ReleaseMutex();
            }
            if (m_pDstRGB != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_pDstRGB);
                m_pDstRGB = IntPtr.Zero;
            }
            if (m_data != null)
            {
                m_data.Dispose(); 
            }
            if (m_bitmap != null)
            {
                m_bitmap.Dispose();
                m_bitmap = null;
            }
            m_framewidth = 0;
            m_frameheight = 0;
            m_dev.ShutdownGrab();
            m_dev.Dispose();
            m_dev = null;
            this.Invoke(new Action(() =>
            {
                btnOpen.Enabled = true;
                btnSoftwareTrigger.Enabled = false;
                btnClose.Enabled = false;           
            }));
        }
        private void btnOpen_Click(object sender, EventArgs e)
        {
            try
            {
                // device search 
                List<IDeviceInfo> li = Enumerator.EnumerateDevices();              
                if (li.Count > 0)
                {
                    // get the first searched device 
                    m_dev = Enumerator.GetDeviceByIndex(0);              
                    // register event callback 
                    m_dev.CameraOpened += OnCameraOpen;
                    m_dev.ConnectionLost += OnConnectLoss;
                    m_dev.CameraClosed += OnCameraClose;              
                    // open device 
                    if (!m_dev.Open())
                    {
                        MessageBox.Show("Open camera failed");
                        return;
                    }
                    // Set Software Trigger 
                    m_dev.TriggerSet.Open(TriggerSourceEnum.Software);
                    // set PixelFormat 
                    using (IEnumParameter p = m_dev.ParameterCollection[ParametrizeNameSet.ImagePixelFormat])
                    {
                        p.SetValue("BayerRG8");                       
                    }
                    // set ExposureTime 
                    using (IFloatParameter p = m_dev.ParameterCollection[ParametrizeNameSet.ExposureTime])
                    {
                        //  p.SetValue(3198);
                        //  p.SetValue(1029);
                        p.SetValue(1185);                    
                    }
                    m_dev.ParameterCollection[ParametrizeNameSet.ExposureAuto].SetValue("Off");
                    int brightness = 52;
                    m_dev.ParameterCollection[ParametrizeNameSet.Brightness].SetValue(brightness);
                    int hue = 50;
                    m_dev.ParameterCollection[ParametrizeNameSet.Hue].SetValue(hue);
                    int saturation = 50;
                    m_dev.ParameterCollection[ParametrizeNameSet.Saturation].SetValue(saturation);
                    m_dev.ParameterCollection[ParametrizeNameSet.BalanceWhiteAuto].SetValue("Off");
                    // set Gain 
                    using (IFloatParameter p = m_dev.ParameterCollection[ParametrizeNameSet.GainRaw])
                    {
                        p.SetValue(1.0);
                    }
                    using (IFloatParameter p = m_dev.ParameterCollection[ParametrizeNameSet.Gamma])
                    {
                        p.SetValue(0.6);
                    }
                    // set buffer count to 8 (default 16) 
                    m_dev.StreamGrabber.SetBufferCount(8);
                    // start grabbing 
                    if (!m_dev.StreamGrabber.Start(GrabStrategyEnum.grabStrartegySequential, GrabLoop.ProvidedByUser))
                    {
                        MessageBox.Show(@"Start grabbing failed");
                        return;
                    }
                    m_bStartGrab = true;
                }
            }
            catch (Exception exception)
            {
                Catcher.Show(exception);
            }
        }
        // stop grabbing 
        private void btnClose_Click(object sender, EventArgs e)
        {
            m_bStartGrab = false;
            if (m_bShowLoop)
            {
                readerThread.Join(100);
            }
            if (m_frameList.Count > 0)
            {
                m_mutex.WaitOne();
                m_frameList.Clear();
                m_mutex.ReleaseMutex();
            }
            if (m_pDstRGB != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_pDstRGB);
                m_pDstRGB = IntPtr.Zero;
            }
            if (m_data != null)
            {
                m_data.Dispose();
            }
            if (m_bitmap != null)
            {
                m_bitmap.Dispose();
                m_bitmap = null;
            }
            m_framewidth = 0;
            m_frameheight = 0;
            try
            {
                if (m_dev == null)
                {
                    throw new InvalidOperationException("Device is invalid");
                }
                m_dev.ShutdownGrab();                                       //stop grabbing 
                m_dev.Close();                                              //close camera 
            }
            catch (Exception exception)
            {
                Catcher.Show(exception);
            }
        }
        // execute software trigger once  
        private void btnSoftwareTrigger_Click(object sender, EventArgs e)
        {
            timeStart = DateTime.Now;
            if (m_dev == null)
            {
                throw new InvalidOperationException("Device is invalid");
            }
            try
            {        
                m_grabTime.Reset();
                m_grabTime.Start();
                m_dev.ExecuteSoftwareTrigger();
                #region 
                // initiative grab 
                if (m_dev != null && m_dev.WaitForFrameTriggerReady(out m_data, 1000))
                {
                    m_grabTime.Stop();
                    long grabTime = m_grabTime.ElapsedMilliseconds;
                    string str = "Camera:" + m_dev.DeviceInfo.Key + " BlockID:" + m_data.BlockID + " soft trigger -> frame:" + grabTime + "ms\r";
                    LogHelper.Instance.Write(str, MessageType.Information);

                    //添加数据
                    m_mutex.WaitOne();
                    try
                    {
                        m_frameList.Add(m_data);
                    }
                    catch (Exception exception)
                    {
                        Catcher.Show(exception);
                    }
                    m_mutex.ReleaseMutex();
                    this.DelayHandle();
                }
                #endregion
            }
            catch (Exception exception)
            {
                Catcher.Show(exception);
            }
        }
        // Window Closed 
        protected override void OnClosed(EventArgs e)
        {
            if (m_bStartGrab)
            {
                btnClose_Click(null, null);
            }
            m_bShowLoop = false;
            readerThread.Join();
            if (m_dev != null)
            {
                m_dev.Dispose();
                m_dev = null;
            }
            if (_g != null)
            {
                _g.Dispose();
                _g = null;
            }
            LogHelper.Instance.Dispose();
            base.OnClosed(e);
        }
        //display thread routine
        // hàm chạy luồng hiển thị ảnh từ camera lên UI user
        private void ShowThread()
        {
            while (m_bShowLoop)
            {
                if (m_frameList.Count == 0)
                {
                    Thread.Sleep(10);
                    continue;
                }

                if (m_frameList.Count > 0)
                {
                    m_mutex.WaitOne();
                    IFrameRawData frame = m_frameList[0];
                    m_frameList.Remove(frame);
                    m_mutex.ReleaseMutex();

                    try
                    {
                        if (CreateOrUpdateMemory(frame) && ImageConvert(frame))
                        {
                            
                            SaveImageToFile();
                        }
                    }
                    catch (Exception exception)
                    {
                        Catcher.Show(exception);
                    }
                    finally
                    {
                       
                        if (frame != null)
                        {
                            frame.Dispose();
                        }
                    }
                }
            }
        }
        // Lưu hình ảnh vào file
        private void SaveImageToFile()
        {
            resizeImg = new Bitmap(m_bitmap, new Size(850, 400));
            string fileName = $"image_{DateTime.Now.ToString("yyyyy_MM_dd_HH_mm_ss")}.jpeg";
            string filePath = Path.Combine(folderPath, fileName);
            resizeImg.Save(filePath, ImageFormat.Jpeg);
            this.Bit = new Bitmap(resizeImg);
        }
        public readonly int DELAY_HANDLE = 160;
        private void DelayHandle() 
        {
            Thread.Sleep(DELAY_HANDLE);
            if(indexCombobox == 0)
            {
                width_whiteClip = x11_whiteClip - x1_whiteClip;
                height_whiteClip = y11_whiteClip - y1_whiteClip;
                width1_whiteClip = x22_whiteClip - x2_whiteClip;
                height1_whiteClip = y22_whiteClip - y2_whiteClip;
                width_form = x1_form - x_form;
                height_form = y1_form - y_form;
                Bitmap originalPicture = DrawRectangleOnClipFormXD083(this.Bit, x1_whiteClip, y1_whiteClip, width_whiteClip, height_whiteClip,
                 x2_whiteClip, y2_whiteClip, width1_whiteClip, height1_whiteClip, x_form, y_form, width_form, height_form);
                picture_original.Image = originalPicture;
                this.HandleProductX_D0287_081(this.Bit);               
            }
            else if(indexCombobox == 1)
            {
                width1 = x11_clipheadXD083 - x1_clipheadXD083;
                height1 = y11_clipheadXD083 - y1_clipheadXD083;
                width2 = x22_clipheadXD083 - x2_clipheadXD083;
                height2 = y22_clipheadXD083 - y2_clipheadXD083;
                width3 = x11_clipedgeXD083 - x1_clipedgeXD083;
                height3 = y11_clipedgeXD083 - y1_clipedgeXD083;
                width4 = x22_clipedgeXD083 - x2_clipedgeXD083;
                height4 = y22_clipedgeXD083 - y2_clipedgeXD083;            
                Bitmap originalPic = DrawRectangleSiliconClipXD081Area(this.Bit, x1_clipheadXD083, y1_clipheadXD083, width1, height1, x2_clipheadXD083, y2_clipheadXD083, width2, height2,
                     x1_clipedgeXD083, y1_clipedgeXD083, width3, height3, x2_clipedgeXD083, y2_clipedgeXD083, width4, height4);
                picture_original.Image = originalPic;      
                this.HandleProductX_D0287_083(this.Bit);
            }
        }
        // Apply for unmanaged memory to save image data
        private bool CreateOrUpdateMemory(IFrameRawData frame)
        {
            if (frame.Width != m_framewidth || frame.Height != m_frameheight)
            {
                if (m_pDstRGB != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(m_pDstRGB);
                    m_pDstRGB = IntPtr.Zero;
                }
                var ImgSize = RGBFactory.EncodeLen(frame.Width, frame.Height, true);
                try
                {
                    m_pDstRGB = Marshal.AllocHGlobal(ImgSize);
                }
                catch
                {
                    frame.Dispose();
                    return false;
                }
                if (m_pDstRGB == IntPtr.Zero)
                {
                    frame.Dispose();
                    return false;
                }
                m_framewidth = frame.Width;
                m_frameheight = frame.Height;
            }
            return true;
        }
        //Transcoding the image
        private bool ImageConvert(IFrameRawData frame)
        {       
            m_oParam.width = frame.Width;
            m_oParam.height = frame.Height;
            m_oParam.paddingX = 0;
            m_oParam.paddingY = 0;
            m_oParam.dataSize = frame.RawSize;
            m_oParam.pixelForamt = (uint)frame.PixelFmt;
            int nDesDataSize = 0;
            int ret = IMGCNV_ConvertToBGR24_Ex(frame.RawData, ref m_oParam, m_pDstRGB, ref nDesDataSize, IMGCNV_EBayerDemosaic.IMGCNV_DEMOSAIC_NEAREST_NEIGHBOR);
            if (ret != 0)
            {
                frame.Dispose(); 
                return false;
            }
            //Bitmap
            CreateOrUpdateBitmap(frame);
            return true;
        }
        private void CreateOrUpdateBitmap(IFrameRawData frame)
        {
            if (m_bitmap == null || m_bitmap.Width != frame.Width || m_bitmap.Height != frame.Height)
            {
                if (m_bitmap != null)
                {
                    m_bitmap.Dispose();
                }
                m_bitmap = new Bitmap(frame.Width, frame.Height, PixelFormat.Format24bppRgb);
            }           
            if (m_bitmapRect.Width != m_bitmap.Width || m_bitmapRect.Height != m_bitmap.Height)
            {
                m_bitmapRect.Height = m_bitmap.Height;
                m_bitmapRect.Width = m_bitmap.Width;
            }
            m_bmpData = m_bitmap.LockBits(m_bitmapRect, ImageLockMode.ReadWrite, m_bitmap.PixelFormat);
            CopyMemory(m_bmpData.Scan0, m_pDstRGB, m_bmpData.Stride * m_bitmap.Height);
            m_bitmap.UnlockBits(m_bmpData);
        }  
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {            
            if (comboBox2.SelectedIndex == 0)
            {
                folderPath = "D:\\doantotnghiep\\Data\\X-D0287-081";
                productName = "X_D0287_081";
                textBox_pathImg.Text = folderPath;
                indexCombobox = 0;
                btnOpen.Enabled = true;
                // hide ngưỡng xd083
                label43.Visible = false;
                trackBar_rangeThreshlodXD083.Visible = false;
                label44.Visible = false;
                label45.Visible = false;

                //show ngưỡng xd081
                label10.Visible = true;
                trackBar_range_threshold.Visible = true;
                label1.Visible = true;
                label_value_threshold.Visible = true;

                // hide xd083
                label16.Visible = false;
                label11.Visible = false;
                label12.Visible = false;
                minPixelSiliconValue.Visible = false;
                label13.Visible = false;
                maxPixelSiliconValue.Visible = false;
                label26.Visible = false;
                label27.Visible = false;
                minpixelclipXD081value.Visible = false;
                
                label19.Visible = false;
                x1SiliconValue.Visible = false;
                y1siliconvalue.Visible = false;
                x11Siliconvalue.Visible = false;
                y11Siliconvalue.Visible = false;
                label21.Visible = false;
                x2Siliconvalue.Visible = false;
                y2siliconvalue.Visible = false;
                x22siliconvalue.Visible = false;
                y22siliconvalue.Visible = false;
                label20.Visible = false;
                x1clipxd081value.Visible = false;
                y1clipxd081value.Visible = false;
                x11clipxd081value.Visible = false;
                y11clipxd081value.Visible = false;
                label22.Visible = false;
                x2clipxd081value.Visible = false;
                y2clipxd081value.Visible = false;
                x22clipxd081value.Visible = false;
                y22clipxd081value.Visible = false;


                // show xd081
                label14.Visible = true;
                label15.Visible = true;
                minPixelClip083Value.Visible = true;
                
             
                label17.Visible = true;
                label18.Visible = true;
                limitPixelFormValue.Visible = true;
                label25.Visible = true;
                xformvalue.Visible = true;
                yformvalue.Visible = true;
                x1formvalue.Visible = true;
                y1formvalue.Visible = true;
                label23.Visible = true;
                x1clipxd083value.Visible = true;
                y1clipxd083value.Visible = true;
                x11clipxd083value.Visible = true;
                y11clipxd083value.Visible = true;
                label24.Visible = true;
                x2clipxd083value.Visible = true;
                y2clipxd083value.Visible = true;
                x22clipxd083value.Visible = true;
                y22clipxd083value.Visible = true;

                label52.Visible = true;
                label53.Visible = true;
                minPixelClip1081Value.Visible = true;
            

                label46.Visible = false;
                label49.Visible = false;
                minPixelSilicon1Value.Visible = false;
            
                maxPixelSilicon1Value.Visible = false;
                label47.Visible = false;
                label50.Visible = false;
                minpixelclip1D083value.Visible = false;
             


                // location
                label14.Location = new System.Drawing.Point(868, 143);
                label15.Location = new System.Drawing.Point(937, 162);
                minPixelClip083Value.Location = new System.Drawing.Point(1024, 162);
              
                label52.Location = new System.Drawing.Point(868, 223);
                label53.Location = new System.Drawing.Point(937, 250);
                minPixelClip1081Value.Location = new System.Drawing.Point(1024, 246);
             

                label17.Location = new System.Drawing.Point(869, 308);
                label18.Location = new System.Drawing.Point(937, 334);
                limitPixelFormValue.Location = new System.Drawing.Point(1024, 330);

                label25.Location = new System.Drawing.Point(1126, 143);
                xformvalue.Location = new System.Drawing.Point(1153, 166);
                yformvalue.Location = new System.Drawing.Point(1153, 195);
                x1formvalue.Location = new System.Drawing.Point(1245, 195);
                y1formvalue.Location = new System.Drawing.Point(1245, 166);

                label23.Location = new System.Drawing.Point(1115, 223);
                x1clipxd083value.Location = new System.Drawing.Point(1153, 246);
                y1clipxd083value.Location = new System.Drawing.Point(1153, 277);
                x11clipxd083value.Location = new System.Drawing.Point(1245, 277);
                y11clipxd083value.Location = new System.Drawing.Point(1245, 246);
                label24.Location = new System.Drawing.Point(1112, 308);
                x2clipxd083value.Location = new System.Drawing.Point(1153, 330);
                y2clipxd083value.Location = new System.Drawing.Point(1153, 357);
                x22clipxd083value.Location = new System.Drawing.Point(1245, 357);
                y22clipxd083value.Location = new System.Drawing.Point(1245, 330); 


            }
            else if (comboBox2.SelectedIndex == 1)
            {
                folderPath = "D:\\doantotnghiep\\Data\\X-D0287-083";
                productName = "X_D0287_083";
                textBox_pathImg.Text = folderPath;
                indexCombobox = 1;
                btnOpen.Enabled = true;
                // Show ngưỡng xd083
                label43.Visible = true;
                trackBar_rangeThreshlodXD083.Visible = true;
                label44.Visible = true;
                label45.Visible = true;

                label43.Location = new System.Drawing.Point(26, 441);
                trackBar_rangeThreshlodXD083.Location = new System.Drawing.Point(29, 474);
                label44.Location = new System.Drawing.Point(27, 522);
                label45.Location = new System.Drawing.Point(91, 522);

                //hide ngưỡng xd081
                label10.Visible = false;
                trackBar_range_threshold.Visible = false;
                label1.Visible =false;
                label_value_threshold.Visible = false;



                // show xd083
                label11.Visible = true;
                label12.Visible = true;
                minPixelSiliconValue.Visible = true;

                label16.Visible = true;

                label13.Visible = true;
                maxPixelSiliconValue.Visible = true;
                label26.Visible = true;
                label27.Visible = true;
                minpixelclipXD081value.Visible = true;


               

                label19.Visible = true;
                x1SiliconValue.Visible = true;
                y1siliconvalue.Visible = true;
                x11Siliconvalue.Visible = true;
                y11Siliconvalue.Visible = true;
                label21.Visible = true;

                x2Siliconvalue.Visible = true;
                y2siliconvalue.Visible = true;
                x22siliconvalue.Visible = true;
                y22siliconvalue.Visible = true;
                label20.Visible = true;
                x1clipxd081value.Visible = true;
                y1clipxd081value.Visible = true;
                x11clipxd081value.Visible = true;
                y11clipxd081value.Visible = true;
                label22.Visible = true;
                x2clipxd081value.Visible = true;
                y2clipxd081value.Visible = true;
                x22clipxd081value.Visible = true;
                y22clipxd081value.Visible = true;

                label46.Visible = true;
                label49.Visible = true;
                minPixelSilicon1Value.Visible = true;
              
                maxPixelSilicon1Value.Visible = true;
                label47.Visible = true;
                label50.Visible = true;
                minpixelclip1D083value.Visible = true;
              

                // hiden xd081
                label14.Visible = false;
                label15.Visible = false;
                minPixelClip083Value.Visible = false;
              
               
                label17.Visible = false;
                label18.Visible = false;
                limitPixelFormValue.Visible = false;
                label25.Visible = false;
                xformvalue.Visible = false;
                yformvalue.Visible = false;
                x1formvalue.Visible = false;
                y1formvalue.Visible = false;
                label23.Visible = false;
                x1clipxd083value.Visible = false;
                y1clipxd083value.Visible = false;
                x11clipxd083value.Visible = false;
                y11clipxd083value.Visible = false;
                label24.Visible = false;
                x2clipxd083value.Visible = false;
                y2clipxd083value.Visible = false;
                x22clipxd083value.Visible = false;
                y22clipxd083value.Visible = false;

                label52.Visible = false;
                label53.Visible = false;
                minPixelClip1081Value.Visible = false;
             

                label16.Location = new System.Drawing.Point(937, 277);

                label46.Location = new System.Drawing.Point(868, 223);
                label49.Location = new System.Drawing.Point(937, 250);
                minPixelSilicon1Value.Location = new System.Drawing.Point(1024, 246);

              
                maxPixelSilicon1Value.Location = new System.Drawing.Point(1024, 277);
                label26.Location = new System.Drawing.Point(869, 308);
                label27.Location = new System.Drawing.Point(937, 334);
                minpixelclipXD081value.Location = new System.Drawing.Point(1024, 330);
               

                label47.Location = new System.Drawing.Point(869, 381);
                label50.Location = new System.Drawing.Point(937, 414);
                minpixelclip1D083value.Location = new System.Drawing.Point(1024, 410);
           


            }
        }
        public Bitmap ApplyMethodClosingProcessXD081(Bitmap img)
        {
            Image<Gray, byte> image = new Image<Gray, byte>(img);
            Rectangle[] rois = new Rectangle[3];
            rois[0] = new Rectangle(x1_whiteClip, y1_whiteClip, x11_whiteClip - x1_whiteClip, y11_whiteClip - y1_whiteClip);
            rois[1] = new Rectangle(x2_whiteClip, y2_whiteClip, x22_whiteClip - x2_whiteClip, y22_whiteClip - y2_whiteClip);
            rois[2] = new Rectangle(x_form, y_form, x1_form - x_form, y1_form - y_form);
            foreach (Rectangle roi in rois)
            {
                var imgRoi = image.GetSubRect(roi);
                // apply closing 
                 Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
                 CvInvoke.MorphologyEx(imgRoi, imgRoi, MorphOp.Close, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0,0,0));
            
                imgRoi.CopyTo(image.GetSubRect(roi));
            }
            return image.ToBitmap();
        }


        // hàm xử lý lỗi xd081
        public void HandleProductX_D0287_081(Bitmap img){
            if (img != null)
            {
                BinaryImage = ConvertColor(img, threshold);
                resultClosing = ApplyMethodClosingProcessXD081(BinaryImage);
                Bitmap resultDetectAreaImg = FormClipWhiteAreaXD083(resultClosing);
                pbImage.Image = resultDetectAreaImg;         
                int formPixel = CountBlackPixels(resultDetectAreaImg, x_form, y_form, width_form, height_form);
                Console.WriteLine(formPixel + " pixel of form ");
                label42.Text = formPixel.ToString();
                int WhiteClipPixel = CountBlackPixels(resultDetectAreaImg, x1_whiteClip, y1_whiteClip, width_whiteClip, height_whiteClip);
                Console.WriteLine(WhiteClipPixel + " pixels of the first white clip range");
                label39.Text = WhiteClipPixel.ToString();
                int WhiteClipPixel1 = CountBlackPixels(resultDetectAreaImg, x2_whiteClip, y2_whiteClip, width1_whiteClip, height1_whiteClip);
                Console.WriteLine(WhiteClipPixel1 + " pixels of the second white clip range");
                label40.Text = WhiteClipPixel1.ToString();       
                bool result = ShowFormAndWhiteClip(formPixel, limitPixelForm, WhiteClipPixel, minPixelClipXD081, WhiteClipPixel1,minPixelClip1XD081);
                productStatus = result ? "Pass" : "Fail";
                checkDescription = result ? "Form and White Clip Ok" : "Form or White Clip NG";
                Product newProduct = new Product(productName, productStatus, checkDescription, DateTime.Now.ToString());
                Product_list.Instance.AddProduct(newProduct);
                CountPassFail();
                ShowPassFailTotal();
                PassFailTotal();
                this.timeEnd = DateTime.Now;
                var timeProcess = this.timeEnd - this.timeStart;
                int minutes = timeProcess.Minutes;
                int seconds = timeProcess.Seconds;
                int miliseconds = timeProcess.Milliseconds;
                string formattedTime = string.Format("{0:D2}:{1:D2}:{2:D3}", minutes, seconds, miliseconds);
                label_value_time.Text = formattedTime;
            }          
        }
        public Bitmap ApplyMethodClosingProcessXD083(Bitmap img)
        {
            Image<Gray, byte> image = new Image<Gray, byte>(img);
            Rectangle[] rois = new Rectangle[4];
            rois[0] = new Rectangle(x1_clipheadXD083, y1_clipheadXD083, x11_clipheadXD083 - x1_clipheadXD083, y11_clipheadXD083 - y1_clipheadXD083);
            rois[1] = new Rectangle(x2_clipheadXD083, y2_clipheadXD083, x22_clipheadXD083 - x2_clipheadXD083, y22_clipheadXD083 - y2_clipheadXD083);
            rois[2] = new Rectangle(x1_clipedgeXD083, y1_clipedgeXD083, x11_clipedgeXD083 - x1_clipedgeXD083, y11_clipedgeXD083 - y1_clipedgeXD083);
            rois[3] = new Rectangle(x2_clipedgeXD083, y2_clipedgeXD083, x22_clipedgeXD083 - x2_clipedgeXD083, y22_clipedgeXD083 - y2_clipedgeXD083);
            foreach (Rectangle roi in rois)
            {
                var imgRoi = image.GetSubRect(roi);
                // apply method closing 
               Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
               CvInvoke.MorphologyEx(imgRoi, imgRoi, MorphOp.Close, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
                imgRoi.CopyTo(image.GetSubRect(roi));
            }
            return image.ToBitmap();

        }
    
        // hàm xử lý lỗi XD083   
        public void HandleProductX_D0287_083(Bitmap img)
        {
           if(img != null)
            {
                BinaryImage = ConvertColor(img, thresholdXD083);          
                resultClosing = ApplyMethodClosingProcessXD083(BinaryImage);
                Bitmap resultDetectAreaImg = DetectClipSiliconAreaXD081(resultClosing);           
                int siliconpixelFirst = CountBlackPixels(resultDetectAreaImg, x1_clipheadXD083, y1_clipheadXD083, width1, height1);
                Console.WriteLine(siliconpixelFirst + " pixels of the first silicon range");
                label30.Text = siliconpixelFirst.ToString();      
                int siliconpixelSecond = CountBlackPixels(resultDetectAreaImg, x2_clipheadXD083, y2_clipheadXD083, width2, height2);
                Console.WriteLine(siliconpixelSecond + " pixels of the second silicon range");
                label32.Text = siliconpixelSecond.ToString();
                int clipFirstPixel = CountBlackPixels(resultDetectAreaImg, x1_clipedgeXD083, y1_clipedgeXD083, width3, height3);
                int clipSecondPixel = CountBlackPixels(resultDetectAreaImg, x2_clipedgeXD083, y2_clipedgeXD083, width4, height4);
                Console.WriteLine(clipFirstPixel + " pixels of the first clip range");
                label34.Text = clipFirstPixel.ToString();
                Console.WriteLine(clipSecondPixel + " pixels of the second clip range");
                label36.Text = clipSecondPixel.ToString();
                bool result = CheckClipAndSiliconXD083(siliconpixelFirst, minPixelSilicon, maxPixelSilicon, siliconpixelSecond, minPixelSilicon1, maxPixelSilicon1,
                    clipFirstPixel,minPixelClipXD083, clipSecondPixel, minPixelClip1XD083);
                productStatus = result ? "Pass" : "Fail";
                checkDescription = result ? "Silicon And Clip OK" : "Silicon Or Clip NG";
                Product newProduct = new Product(productName, productStatus, checkDescription, DateTime.Now.ToString());
                Product_list.Instance.AddProduct(newProduct);
                CountPassFail();
                ShowPassFailTotal();
                PassFailTotal();
                this.timeEnd = DateTime.Now;
                var timeProcess = this.timeEnd - this.timeStart;
                int minutes = timeProcess.Minutes;
                int seconds = timeProcess.Seconds;
                int miliseconds = timeProcess.Milliseconds;
                string formattedTime = string.Format("{0:D2}:{1:D2}:{2:D3}", minutes, seconds, miliseconds);
                label_value_time.Text = formattedTime;
            }
        }
    
        // convert qua binary
        public Bitmap ConvertColor(Bitmap picture, byte threshold)
        {
            Bitmap  binaryImage = new Bitmap(picture.Width, picture.Height);
            for (int i = 0; i < picture.Width; i++)
            {
                for (int j = 0; j < picture.Height; j++)
                {
                    Color pixelColor = picture.GetPixel(i, j);
                    byte R = pixelColor.R;
                    byte G = pixelColor.G;
                    byte B = pixelColor.B;
                    byte grayColor = (byte)(R * 0.299 + G * 0.587 + B * 0.114);
                    if (grayColor < threshold)
                    {
                        grayColor = 0;
                    }
                    else
                    {
                        grayColor = 255;
                    }                       
                    binaryImage.SetPixel(i, j, Color.FromArgb(grayColor, grayColor, grayColor));
                }
            }
            return binaryImage;
        }
        // function count Pixel
        public int CountBlackPixels(Bitmap img, int x, int y, int width, int height)
        {
            // count valiable
            int count = 0;
            int redValue = 0;
            int greenValue = 0;
            int blueValue = 0;
            for (int i = x; i < x + width && i < img.Width; i++)
            {
                for (int j = y; j < y + height && j < img.Height; j++)
                {
                    Color pixel = img.GetPixel(i, j);
                    if (pixel.R == redValue && pixel.G == greenValue && pixel.B == blueValue)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        private void SaveSiliconClipXD081Coordinates()
        {
            string fileName = "siliconclipxd081coordinate.txt";
            try
            {
                using (StreamWriter swrite = new StreamWriter(fileName))
                {
                    swrite.WriteLine(x1_clipheadXD083);
                    swrite.WriteLine(y1_clipheadXD083);
                    swrite.WriteLine(x11_clipheadXD083);
                    swrite.WriteLine(y11_clipheadXD083);
                    swrite.WriteLine(x2_clipheadXD083);
                    swrite.WriteLine(y2_clipheadXD083);
                    swrite.WriteLine(x22_clipheadXD083);
                    swrite.WriteLine(y22_clipheadXD083);
                    swrite.WriteLine(x1_clipedgeXD083);
                    swrite.WriteLine(y1_clipedgeXD083);
                    swrite.WriteLine(x11_clipedgeXD083);
                    swrite.WriteLine(y11_clipedgeXD083);
                    swrite.WriteLine(x2_clipedgeXD083);
                    swrite.WriteLine(y2_clipedgeXD083);
                    swrite.WriteLine(x22_clipedgeXD083);
                    swrite.WriteLine(y22_clipedgeXD083);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }    
        private void LoadSiliconClipXD081Coordinates()
        {
            string fileName = "siliconclipxd081coordinate.txt";
            if (File.Exists(fileName))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(fileName, Encoding.UTF8))
                    {
                        x1_clipheadXD083 = int.Parse(reader.ReadLine());
                        y1_clipheadXD083 = int.Parse(reader.ReadLine());
                        x11_clipheadXD083 = int.Parse(reader.ReadLine());
                        y11_clipheadXD083 = int.Parse(reader.ReadLine());
                        x2_clipheadXD083 = int.Parse(reader.ReadLine());
                        y2_clipheadXD083 = int.Parse(reader.ReadLine());
                        x22_clipheadXD083 = int.Parse(reader.ReadLine());
                        y22_clipheadXD083 = int.Parse(reader.ReadLine());
                        x1_clipedgeXD083 = int.Parse(reader.ReadLine());
                        y1_clipedgeXD083 = int.Parse(reader.ReadLine());
                        x11_clipedgeXD083 = int.Parse(reader.ReadLine());
                        y11_clipedgeXD083 = int.Parse(reader.ReadLine());
                        x2_clipedgeXD083 = int.Parse(reader.ReadLine());
                        y2_clipedgeXD083 = int.Parse(reader.ReadLine());
                        x22_clipedgeXD083 = int.Parse(reader.ReadLine());
                        y22_clipedgeXD083 = int.Parse(reader.ReadLine());
                    }
                    x1SiliconValue.Value = x1_clipheadXD083;
                    y1siliconvalue.Value = y2_clipheadXD083;
                    x11Siliconvalue.Value = x11_clipheadXD083;
                    y11Siliconvalue.Value = y11_clipheadXD083;
                    x2Siliconvalue.Value = x22_clipheadXD083;
                    y2siliconvalue.Value = y22_clipheadXD083;
                    x22siliconvalue.Value = x22_clipheadXD083;
                    y22siliconvalue.Value = y22_clipheadXD083;
                    x1clipxd081value.Value = x1_clipedgeXD083;
                    y1clipxd081value.Value = y1_clipedgeXD083;
                    x11clipxd081value.Value = x11_clipedgeXD083;
                    y11clipxd081value.Value = y11_clipedgeXD083;
                    x2clipxd081value.Value = x2_clipedgeXD083;
                    y2clipxd081value.Value = y2_clipedgeXD083;
                    x22clipxd081value.Value = x22_clipedgeXD083;
                    y22clipxd081value.Value = y22_clipedgeXD083;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
     
        private void x1SiliconValue_ValueChanged(object sender, EventArgs e)
        {
            
            x1_clipheadXD083 = (int)x1SiliconValue.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void y1siliconvalue_ValueChanged(object sender, EventArgs e)
        {
            
            y1_clipheadXD083 = (int)y1siliconvalue.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void x11Siliconvalue_ValueChanged(object sender, EventArgs e)
        {
           
            x11_clipheadXD083 = (int)x11Siliconvalue.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void y11Siliconvalue_ValueChanged(object sender, EventArgs e)
        {
           
            y11_clipheadXD083 = (int)y11Siliconvalue.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void x2Siliconvalue_ValueChanged(object sender, EventArgs e)
        {
          
            x2_clipheadXD083 = (int)x2Siliconvalue.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void y2siliconvalue_ValueChanged(object sender, EventArgs e)
        {
           
            y2_clipheadXD083 = (int)y2siliconvalue.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void x22siliconvalue_ValueChanged(object sender, EventArgs e)
        {
          
            x22_clipheadXD083 = (int)x22siliconvalue.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void y22siliconvalue_ValueChanged(object sender, EventArgs e)
        {
          
            y22_clipheadXD083 = (int)y22siliconvalue.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void x1clipxd081value_ValueChanged(object sender, EventArgs e)
        {
           
            x1_clipedgeXD083 = (int)x1clipxd081value.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void y1clipxd081value_ValueChanged(object sender, EventArgs e)
        {
           
            y1_clipedgeXD083 = (int)y1clipxd081value.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void x11clipxd081value_ValueChanged(object sender, EventArgs e)
        {
         
            x11_clipedgeXD083 = (int)x11clipxd081value.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void y11clipxd081value_ValueChanged(object sender, EventArgs e)
        {
           
            y11_clipedgeXD083 = (int)y11clipxd081value.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void x2clipxd081value_ValueChanged(object sender, EventArgs e)
        {
           
            x2_clipedgeXD083 = (int)x2clipxd081value.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void y2clipxd081value_ValueChanged(object sender, EventArgs e)
        {
           
            y2_clipedgeXD083 = (int)y2clipxd081value.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void x22clipxd081value_ValueChanged(object sender, EventArgs e)
        {
           
            x22_clipedgeXD083 = (int)x22clipxd081value.Value;
            SaveSiliconClipXD081Coordinates();
        }
        private void y22clipxd081value_ValueChanged(object sender, EventArgs e)
        {
           
            y22_clipedgeXD083 = (int)y22clipxd081value.Value;
            SaveSiliconClipXD081Coordinates();
        }
        // handle silicon area         
        public Bitmap DetectClipSiliconAreaXD081(Bitmap img)
        {        
            width1 = x11_clipheadXD083 - x1_clipheadXD083;
            height1 = y11_clipheadXD083 - y1_clipheadXD083;
            width2 = x22_clipheadXD083 - x2_clipheadXD083;
            height2 = y22_clipheadXD083 - y2_clipheadXD083;         
            width3 = x11_clipedgeXD083 - x1_clipedgeXD083;
            height3 = y11_clipedgeXD083 - y1_clipedgeXD083;
            width4 = x22_clipedgeXD083 - x2_clipedgeXD083;
            height4 = y22_clipedgeXD083 - y2_clipedgeXD083;
            Bitmap resultImg = DrawRectangleSiliconClipXD081Area(img, x1_clipheadXD083, y1_clipheadXD083, width1, height1, x2_clipheadXD083, y2_clipheadXD083, width2, height2,
           x1_clipedgeXD083, y1_clipedgeXD083, width3, height3, x2_clipedgeXD083, y2_clipedgeXD083, width4,height4);
            return resultImg;
        }
   
        private Bitmap DrawRectangleSiliconClipXD081Area(Bitmap img, int x, int y, int width, int height, int x1, int y1, int width1, int height1, int x2, int y2, int width2, int height2, int x3, int y3, int width3, int height3)
        {
            Bitmap newPic = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(newPic))
            {
                g.DrawImage(img, 0, 0);
                using (Pen pen = new Pen(Color.Red, 3))
                {
                    if (IsValidROI(img, x, y, width, height))
                        g.DrawRectangle(pen, x, y, width, height);
                    if (IsValidROI(img, x1, y1, width1, height1))
                        g.DrawRectangle(pen, x1, y1, width1, height1);
                    if (IsValidROI(img, x2, y2, width2, height2))
                        g.DrawRectangle(pen, x2, y2, width2, height2);
                    if (IsValidROI(img, x3, y3, width3, height3))
                        g.DrawRectangle(pen, x3, y3, width3, height3);
                }
            }
            return newPic;
        }

        private bool IsValidROI(Bitmap img, int x, int y, int width, int height)
        {
         
            if (width <= 0 || height <= 0)
                return false;

           
            if (x < 0 || y < 0 || x + width > img.Width || y + height > img.Height)
                return false;

            return true;
        }


        private void SaveClipFormXD083Coordinates()
        {
            string fileName = "clipformxd083coordinate.txt";
            try
            {
                using (StreamWriter swrite = new StreamWriter(fileName))
                {
                    swrite.WriteLine(x1_whiteClip);
                    swrite.WriteLine(y1_whiteClip);
                    swrite.WriteLine(x11_whiteClip);
                    swrite.WriteLine(y11_whiteClip);
                    swrite.WriteLine(x2_whiteClip);
                    swrite.WriteLine(y2_whiteClip);
                    swrite.WriteLine(x22_whiteClip);
                    swrite.WriteLine(y22_whiteClip);
                    swrite.WriteLine(x_form);
                    swrite.WriteLine(y_form);
                    swrite.WriteLine(x1_form);
                    swrite.WriteLine(y1_form);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void LoadClipFormXD083Coordinates()
        {
            string fileName = "clipformxd083coordinate.txt";
            if (File.Exists(fileName))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(fileName, Encoding.UTF8))
                    {
                        x1_whiteClip = int.Parse(reader.ReadLine());
                        y1_whiteClip = int.Parse(reader.ReadLine());
                        x11_whiteClip = int.Parse(reader.ReadLine());
                        y11_whiteClip = int.Parse(reader.ReadLine());
                        x2_whiteClip = int.Parse(reader.ReadLine());
                        y2_whiteClip = int.Parse(reader.ReadLine());
                        x22_whiteClip = int.Parse(reader.ReadLine());
                        y22_whiteClip = int.Parse(reader.ReadLine());
                        x_form = int.Parse(reader.ReadLine());
                        y_form = int.Parse(reader.ReadLine());
                        x1_form = int.Parse(reader.ReadLine());
                        y1_form = int.Parse(reader.ReadLine());
                    }
                    x1clipxd083value.Value = x1_whiteClip;
                    y1clipxd083value.Value = y1_whiteClip;
                    x11clipxd083value.Value = x11_whiteClip;
                    y11clipxd083value.Value = y11_whiteClip;
                    x2clipxd083value.Value = x2_whiteClip;
                    y2clipxd083value.Value = y2_whiteClip;
                    x22clipxd083value.Value = x22_whiteClip;
                    y22clipxd083value.Value = y22_whiteClip;
                    xformvalue.Value = x_form;
                    yformvalue.Value = y_form;
                    x1formvalue.Value = x1_form;
                    y1formvalue.Value = y1_form;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void x1clipxd083value_ValueChanged(object sender, EventArgs e)
        {
            x1_whiteClip = (int)x1clipxd083value.Value;
            SaveClipFormXD083Coordinates();
        }
        private void y1clipxd083value_ValueChanged(object sender, EventArgs e)
        {
            y1_whiteClip = (int)y1clipxd083value.Value;
            SaveClipFormXD083Coordinates();
        }
        private void x11clipxd083value_ValueChanged(object sender, EventArgs e)
        {
            x11_whiteClip = (int)x11clipxd083value.Value;
            SaveClipFormXD083Coordinates();
        }
        private void y11clipxd083value_ValueChanged(object sender, EventArgs e)
        {
            y11_whiteClip = (int)y11clipxd083value.Value;
            SaveClipFormXD083Coordinates();
        }
        private void x2clipxd083value_ValueChanged(object sender, EventArgs e)
        {
            x2_whiteClip = (int)x2clipxd083value.Value;
            SaveClipFormXD083Coordinates();
        }
        private void y2clipxd083value_ValueChanged(object sender, EventArgs e)
        {
            y2_whiteClip = (int)y2clipxd083value.Value;
            SaveClipFormXD083Coordinates();
        }
        private void x22clipxd083value_ValueChanged(object sender, EventArgs e)
        {
            x22_whiteClip = (int)x22clipxd083value.Value;
            SaveClipFormXD083Coordinates();
        }
        private void y22clipxd083value_ValueChanged(object sender, EventArgs e)
        {
            y22_whiteClip = (int)y22clipxd083value.Value;
            SaveClipFormXD083Coordinates();
        }
        private void xformvalue_ValueChanged(object sender, EventArgs e)
        {
            x_form = (int)xformvalue.Value;
            SaveClipFormXD083Coordinates();
        }
        private void yformvalue_ValueChanged(object sender, EventArgs e)
        {
            y_form = (int)yformvalue.Value;
            SaveClipFormXD083Coordinates();
        }
        private void x1formvalue_ValueChanged(object sender, EventArgs e)
        {
            x1_form = (int)x1formvalue.Value;
            SaveClipFormXD083Coordinates();
        }
        private void y1formvalue_ValueChanged(object sender, EventArgs e)
        {
            y1_form = (int)y1formvalue.Value;
            SaveClipFormXD083Coordinates();
        }
        public Bitmap FormClipWhiteAreaXD083(Bitmap img)
        {         
            width_whiteClip = x11_whiteClip - x1_whiteClip;
            height_whiteClip = y11_whiteClip - y1_whiteClip;     
            width1_whiteClip = x22_whiteClip - x2_whiteClip;
            height1_whiteClip = y22_whiteClip - y2_whiteClip;     
            width_form = x1_form - x_form;
            height_form = y1_form - y_form;
            Bitmap newImg = DrawRectangleOnClipFormXD083(img, x1_whiteClip, y1_whiteClip, width_whiteClip, height_whiteClip,
                x2_whiteClip, y2_whiteClip, width1_whiteClip, height1_whiteClip, x_form, y_form, width_form, height_form);
            return newImg;
        }
        private Bitmap DrawRectangleOnClipFormXD083(Bitmap pic, int x, int y, int width, int heigh, int x1, int y1, int width1, int height1, int x2, int y2, int width2, int height2)
        {
            Bitmap newBitmap = new Bitmap(pic.Width, pic.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(pic, 0, 0);
                using (Pen pen = new Pen(Color.Red, 3))
                {
                    g.DrawRectangle(pen, x, y, width, heigh);
                    g.DrawRectangle(pen, x1, y1, width1, height1);
                    g.DrawRectangle(pen, x2, y2, width2, height2);
                }
            }
            return newBitmap;
        }    
        // chuyển qua form checkproductlist
        private void checkedProductListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to checked product list ? ", "Notification", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Hide();
                CheckedProductList checkedProductList = new CheckedProductList();
                checkedProductList.ShowDialog();
            }
        }

        void connectPLC()
        {
            plc = new Plc(CpuType.S71500, "192.168.1.10", 0, 0);
            plc.Open();

            if (plc.IsConnected)
            {
                MessageBox.Show("PLC Connected");
            }
            else
            {
                MessageBox.Show("Fail to connect to PLC");
            }
        }
        private void sendResult(bool isPasss)
        {
            if (isPasss)
            {
                plc.Write(writePLC, 1);
                Console.WriteLine("Pass sent");
            }
            else
            {
                plc.Write(writePLC, 0);
                Console.WriteLine("Fail sent");
            }
        }
        private bool readFromPLC(string address)
        {
            object M01 = plc.Read(address);
            bool M01c = Convert.ToBoolean(M01);
            Console.WriteLine(M01c);
            if (M01c)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (plc.IsConnected)
            {
                if (readFromPLC(readPLC))
                {
                    Thread.Sleep(100);
                    btnSoftwareTrigger.PerformClick();
                }
            }
            else
            {
                Console.WriteLine("PLC not connect");
            }
        }

        private void Home_Load(object sender, EventArgs e)
        {
            decentralization();
            ShowAvatar();
            threshold = LoadThresholdValue();
            trackBar_range_threshold.Value = threshold;
            label_value_threshold.Text = trackBar_range_threshold.Value.ToString();
            thresholdXD083 = LoadThresholdValueXD083();
            trackBar_rangeThreshlodXD083.Value = thresholdXD083;
            label45.Text = trackBar_rangeThreshlodXD083.Value.ToString();

            LoadPixelLimitValue();
            LoadSiliconClipXD081Coordinates();
            LoadClipFormXD083Coordinates();

            //  connectPLC();
   
            btnOpen.Enabled = false;
            // hide pixel counting
            label29.Visible = false;
            label30.Visible = false;
            label31.Visible = false;
            label32.Visible = false;
            label37.Visible = false;
            label39.Visible = false;
            label38.Visible = false;
            label40.Visible = false;
            label33.Visible = false;
            label34.Visible = false;
            label35.Visible = false;
            label36.Visible = false;
            label41.Visible = false;
            label42.Visible = false; 

            //hide ngưỡng xd081
            label10.Visible = false;
            trackBar_range_threshold.Visible = false;
            label1.Visible = false;
            label_value_threshold.Visible = false;
            // hide ngưỡng xd083
          label43.Visible = false;
            trackBar_rangeThreshlodXD083.Visible = false;
            label44.Visible = false;
            label45.Visible = false;

            /* Hiden XD081*/
            label11.Visible = false;
            label12.Visible = false;
            minPixelSiliconValue.Visible = false;
            label13.Visible = false;
            maxPixelSiliconValue.Visible = false;
            label26.Visible = false;
            label27.Visible = false;
            minpixelclipXD081value.Visible = false;
          
            label19.Visible = false;
            x1SiliconValue.Visible = false;
            y1siliconvalue.Visible = false;
            x11Siliconvalue.Visible = false;
            y11Siliconvalue.Visible = false;
            label21.Visible = false;
            x2Siliconvalue.Visible = false;
            y2siliconvalue.Visible = false;
            x22siliconvalue.Visible = false;
            y22siliconvalue.Visible = false;
            label20.Visible = false;
            x1clipxd081value.Visible = false;
            y1clipxd081value.Visible = false;
            x11clipxd081value.Visible = false;
            y11clipxd081value.Visible = false;
            label22.Visible = false;
            x2clipxd081value.Visible = false;
            y2clipxd081value.Visible = false;
            x22clipxd081value.Visible = false;
            y22clipxd081value.Visible = false;

            // Hiden XD083
            label14.Visible = false;
            label15.Visible = false;
            minPixelClip083Value.Visible = false;
            

            label17.Visible = false;
            label18.Visible = false;
            limitPixelFormValue.Visible = false;
            label25.Visible = false;
            xformvalue.Visible = false;
            yformvalue.Visible = false;
            x1formvalue.Visible = false;
            y1formvalue.Visible = false;
            label23.Visible = false;
            x1clipxd083value.Visible = false;
            y1clipxd083value.Visible = false;
            x11clipxd083value.Visible = false;
            y11clipxd083value.Visible = false;
            label24.Visible = false;
            x2clipxd083value.Visible = false;
            y2clipxd083value.Visible = false;
            x22clipxd083value.Visible = false;
            y22clipxd083value.Visible = false;

            label46.Visible = false;
            label49.Visible = false;
            minPixelSilicon1Value.Visible = false;
            label16.Visible = false;
            maxPixelSilicon1Value.Visible = false;
            label47.Visible = false;
            label50.Visible = false;
            minpixelclip1D083value.Visible = false;
          
            label52.Visible = false;
            label53.Visible = false;
            minPixelClip1081Value.Visible = false;
      

        }

      

        private void SavePixelLimitValue()
        {
            string fileName = "limitpixelValue.txt";
            try
            {
                using (StreamWriter swrite = new StreamWriter(fileName))
                {                   
                    swrite.WriteLine(minPixelSilicon) ;
                    swrite.WriteLine(maxPixelSilicon);
                    swrite.WriteLine(minPixelSilicon1);
                    swrite.WriteLine(maxPixelSilicon1);
                    swrite.WriteLine(minPixelClipXD083);
                    swrite.WriteLine(minPixelClip1XD083);
                    swrite.WriteLine(minPixelClipXD081);                   
                    swrite.WriteLine(minPixelClip1XD081);             
                    swrite.WriteLine(limitPixelForm);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void LoadPixelLimitValue()
        {
            string fileName = "limitpixelValue.txt";
            if(File.Exists(fileName))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(fileName, Encoding.UTF8))
                    {
                        minPixelSilicon = int.Parse(reader.ReadLine());
                        maxPixelSilicon = int.Parse(reader.ReadLine());
                        minPixelSilicon1 = int.Parse(reader.ReadLine());
                        maxPixelSilicon1 = int.Parse(reader.ReadLine());
                        minPixelClipXD083 = int.Parse(reader.ReadLine());                  
                        minPixelClip1XD083 = int.Parse(reader.ReadLine());
                        minPixelClipXD081 = int.Parse(reader.ReadLine());               
                        minPixelClip1XD081 = int.Parse(reader.ReadLine());                
                        limitPixelForm = int.Parse(reader.ReadLine());
                    }
                    minPixelSiliconValue.Value = minPixelSilicon;
                    maxPixelSiliconValue.Value = maxPixelSilicon;
                    minPixelSilicon1Value.Value = minPixelSilicon1;
                    maxPixelSilicon1Value.Value = maxPixelSilicon1;
                    minpixelclipXD081value.Value = minPixelClipXD083;             
                    minpixelclip1D083value.Value = minPixelClip1XD083;
                    minPixelClip083Value.Value = minPixelClipXD081;             
                    minPixelClip1081Value.Value = minPixelClip1XD081;                  
                    limitPixelFormValue.Value = limitPixelForm;                
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void maxPixelSilicon1Value_ValueChanged_1(object sender, EventArgs e)
        {
            maxPixelSilicon1 = (int)maxPixelSilicon1Value.Value;
            SavePixelLimitValue();
        }
        private void minPixelSiliconValue_ValueChanged(object sender, EventArgs e)
        {           
            minPixelSilicon = (int)minPixelSiliconValue.Value;
            SavePixelLimitValue();
        }
        private void maxPixelSiliconValue_ValueChanged(object sender, EventArgs e)
        {
            maxPixelSilicon = (int)maxPixelSiliconValue.Value;
            SavePixelLimitValue();
        }
        private void minPixelSilicon1Value_ValueChanged(object sender, EventArgs e)
        {
            minPixelSilicon1 = (int)minPixelSilicon1Value.Value;
            SavePixelLimitValue();
        }
        private void minpixelclipXD081value_ValueChanged(object sender, EventArgs e)
        {
            minPixelClipXD083 = (int)minpixelclipXD081value.Value;       
            SavePixelLimitValue();
        }   
        private void minpixelclip1D083value_ValueChanged(object sender, EventArgs e)
        {
            minPixelClip1XD083 = (int)minpixelclip1D083value.Value;
            SavePixelLimitValue();
        }   
        private void minPixelClip083Value_ValueChanged(object sender, EventArgs e)
        {
            minPixelClipXD081 = (int)minPixelClip083Value.Value;      
            SavePixelLimitValue();
        }
        private void minPixelClip1081Value_ValueChanged(object sender, EventArgs e)
        {
            minPixelClip1XD081 = (int)minPixelClip1081Value.Value;
            SavePixelLimitValue();
        }  
        private void limitPixelFormValue_ValueChanged(object sender, EventArgs e)
        {
            limitPixelForm = (int)limitPixelFormValue.Value;
            SavePixelLimitValue();
        }
        private void SaveThresholdValue(byte value)
        {
            string filePath = "threshold.txt";
            File.WriteAllText(filePath, value.ToString());
        }
        private byte LoadThresholdValue()
        {
            string file = "threshold.txt";
            if (File.Exists(file))
            {
                string Threshlodvalue = File.ReadAllText(file);
                if(byte.TryParse(Threshlodvalue, out byte value))
                {
                    return value;
                }
            }
            return 90;
        }   
        private void trackBar_range_threshold_Scroll(object sender, EventArgs e)
        {
            label_value_threshold.Text = trackBar_range_threshold.Value.ToString();
            threshold = (byte)trackBar_range_threshold.Value;
            SaveThresholdValue(threshold);
        }
        private void SaveThresholdValueXD083(byte value)
        {
            string filePath = "thresholdXD083.txt";
            File.WriteAllText(filePath, value.ToString());
        }
        private byte LoadThresholdValueXD083()
        {
            string file = "thresholdXD083.txt";
            if (File.Exists(file))
            {
                string Threshlodvalue = File.ReadAllText(file);
                if (byte.TryParse(Threshlodvalue, out byte value))
                {
                    return value;
                }
            }
            return 90;
        }
        private void trackBar_rangeThreshlodXD083_Scroll(object sender, EventArgs e)
        {
            label45.Text = trackBar_rangeThreshlodXD083.Value.ToString();
            thresholdXD083 = (byte)trackBar_rangeThreshlodXD083.Value;
            SaveThresholdValueXD083(thresholdXD083);
        }
        public bool checkSilicon1(int x,int minPixel, int maxPixel)
        {
            bool check = false;
            if (x >= minPixel && x <= maxPixel)
            {
                check = true;
            }
            else
            {
                check = false;
            }
            return check;
        }
        public bool checkSilicon2(int y, int minPixel, int maxPixel)
        {
            bool check = false;
            if (y >= minPixel && y <= maxPixel)
            {
                check = true;
            }
            else
            {
                check = false;
            }
            return check;
        }
        public bool checkclipxd083(int x, int limitpixel)
        {
            bool check = false;
            if (x >= limitpixel)
            {
                check = true;
            }
            else
            {
                check = false;
            }
            return check;
        }
        public bool checkclip1xd083(int y, int limitpixel)
        {
            bool check = false;
            if (y >= limitpixel)
            {
                check = true;
            }
            else
            {
                check = false;
            }
            return check;
        }
        public bool CheckClipAndSiliconXD083(int a, int b, int c, int d, int e, int f, int m, int n, int x, int y)
        {
            textBox1.Controls.Clear();
            bool silicon1XD083Result = checkSilicon1(a, b, c);
            bool silicon2XD083Result = checkSilicon2(d, e, f);            
            bool clipXD083Result = checkclipxd083(m, n);
            bool clip1XD083Result = checkclip1xd083(x,y);
            bool check = false;
            if (!clipXD083Result || !silicon1XD083Result || !silicon2XD083Result || !clip1XD083Result)
            {
                check = false;
                TextBox failedItem = new TextBox();
                failedItem.Text = "FAIL";
                failedItem.ForeColor = Color.White;
                failedItem.BackColor = Color.Red;
                failedItem.TextAlign = HorizontalAlignment.Center;
                textBox1.Controls.Add(failedItem);
                isPass = false;
            }
            else
            {
                check = true;
                TextBox passItem = new TextBox();
                passItem.Text = "PASS";
                passItem.ForeColor = Color.White;
                passItem.BackColor = Color.Green;
                passItem.TextAlign = HorizontalAlignment.Center;
                textBox1.Controls.Add(passItem);
                isPass = true;
            }
            return check;
        }
        public bool checkForm(int x, int maxpixel)
        {
            bool check = false;
            if (x > maxpixel)
            {
                check = true;
            }
            else
            {
                check = false;
            }
            return check;
        }
        public bool checkWhiteClipXD081(int x,int limitpixel)
        {
            bool check = false;
            if (x >= limitpixel)
            {
                check = true;             
            }
            else
            {
                check = false;      
            }
            return check;
        }
        public bool checkWhiteClip1XD081(int y, int limitpixel)
        {
            bool check = false;
            if ( y >= limitpixel)
            {
                check = true;
            }
            else
            {
                check = false;
            }
            return check;
        }

        // function show pass or failed of form and white clip 
        public bool ShowFormAndWhiteClip(int a, int b, int c, int d, int e, int f)
        {
            textBox1.Controls.Clear();
            bool formResult = checkForm(a,b);
            bool clipXD081result = checkWhiteClipXD081(c, d);
            bool clip1XD081result = checkWhiteClip1XD081(e,f);
            bool check = false;
            if (!formResult || !clipXD081result || !clip1XD081result)
            {
                check = false;
                TextBox failedItem = new TextBox();
                failedItem.Text = "FAIL";
                failedItem.ForeColor = Color.White;
                failedItem.BackColor = Color.Red;
                failedItem.TextAlign = HorizontalAlignment.Center;
                textBox1.Controls.Add(failedItem);
                isPass = false;
            }
            else
            {
                check = true;
                TextBox passItem = new TextBox();
                passItem.Text = "PASS";
                passItem.ForeColor = Color.White;
                passItem.BackColor = Color.Green;
                passItem.TextAlign = HorizontalAlignment.Center;
                textBox1.Controls.Add(passItem);
                isPass = true;
            }
            return check;
        }  
        public void CountPassFail()
        {
            foreach (Control control in textBox1.Controls)
            {
                if (control is TextBox)
                {
                    TextBox textBox = control as TextBox;
                    if (textBox.Text.Equals("PASS", StringComparison.OrdinalIgnoreCase))
                    {
                        passCount++;
                    }
                    else if (textBox.Text.Equals("FAIL", StringComparison.OrdinalIgnoreCase))
                    {
                        faileCount++;
                    }
                }
            }
        }
        public void ShowPassFailTotal()
        {
            textBoxPassed.Text = passCount.ToString();
            textBoxFailed.Text = faileCount.ToString();
        }
        public void PassFailTotal()
        {
            total = passCount + faileCount;
            textBoxTotal.Text = total.ToString();
        }
        // log out
        public void button_logOut_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to log out ? ", "Notification", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Hide();
                Login login = new Login();
                login.ShowDialog();
            }
        }
        public void Menu_accountList_Click(object sender, EventArgs e)
        {
            this.Hide();
            AccountList accountList = new AccountList();
            accountList.ShowDialog();
        }
        public void decentralization()
        {
           switch(Const.Account.AccountType)
            {
                case Account.AccountTypes.worker:
                    label10.Visible = false;
                    label1.Visible = false;
                    label_value_threshold.Visible = false;
                    trackBar_range_threshold.Visible = false;
                    trackBar_range_threshold.Enabled = false;
                    trackBar_rangeThreshlodXD083.Enabled = false;
                    menuStrip1.Visible = false;
                    minPixelSiliconValue.Enabled = false;
                    maxPixelSiliconValue.Enabled = false;
                    minpixelclipXD081value.Enabled = false;
                    minPixelClip083Value.Enabled = false;
                    limitPixelFormValue.Enabled = false;
                    minPixelSilicon1Value.Enabled = false;
                    maxPixelSilicon1Value.Enabled = false;
                    minpixelclip1D083value.Enabled = false;
                    minPixelClip1081Value.Enabled = false;
                    x1SiliconValue.Enabled = false;
                    y11Siliconvalue.Enabled = false;
                    y1siliconvalue.Enabled = false;
                    x11Siliconvalue.Enabled = false;
                    x2Siliconvalue.Enabled = false;
                    y2siliconvalue.Enabled = false;
                    y22siliconvalue.Enabled = false;
                    x22siliconvalue.Enabled = false;
                    x1clipxd081value.Enabled = false;
                    y1clipxd081value.Enabled = false;
                    y11clipxd081value.Enabled = false;
                    x11clipxd081value.Enabled = false;
                    x2clipxd081value.Enabled = false;
                    y22clipxd081value.Enabled = false;
                    y2clipxd081value.Enabled = false;
                    x22clipxd081value.Enabled = false;
                    x1clipxd083value.Enabled = false;
                    y11clipxd083value.Enabled = false;
                    y1clipxd083value.Enabled = false;
                    x11clipxd083value.Enabled = false;
                    x2clipxd083value.Enabled = false;
                    y2clipxd083value.Enabled = false;
                    y22clipxd083value.Enabled = false;
                    x22clipxd083value.Enabled = false;
                    xformvalue.Enabled = false;
                    yformvalue.Enabled = false;
                    x1formvalue.Enabled = false;
                    y1formvalue.Enabled = false;
                    break;
                case Account.AccountTypes.mechanic:
                    button_reset.Enabled = true;                   
                    menuStrip1.Visible = true;
                    break;
            }
            label_levelName.Text = Const.Account.NameShow;
        }
        private void button_reset_Click(object sender, EventArgs e)
        {
            comboBox2.SelectedIndex = -1;
            passCount = 0;
            faileCount = 0;
            total = 0;
            ShowPassFailTotal();
            PassFailTotal();
            pbImage.Image = null;
            picture_original.Image = null;
            textBox1.Controls.Clear();
            textBox_pathImg.Text = "";
            string formattedTime = string.Format("{0:D2}:{1:D2}:{2:D3}", 00, 00, 000);
            label_value_time.Text = formattedTime;
            // save data
            string path = System.Windows.Forms.Application.StartupPath + "\\dataresult.csv";
           Savedata.SaveFile(Product_list.Instance.ProductList, path);   
        }
        // function to show avatar of the user
        public void ShowAvatar()
        {
           switch (Const.Account.AccountType)
            {
                case Account.AccountTypes.mechanic:
                    pictureBox_avt.Image = AutomatedVisualInspectionClipFormSilicon.Properties.Resources.mechanic;
                    break;
                case Account.AccountTypes.engineer:
                    pictureBox_avt.Image = AutomatedVisualInspectionClipFormSilicon.Properties.Resources.engineer;
                    break;
                case Account.AccountTypes.worker:
                    pictureBox_avt.Image = AutomatedVisualInspectionClipFormSilicon.Properties.Resources.factory_worker;
                    break;
            }
        }
    }
}