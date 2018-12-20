using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using AForge.Video;
using AForge.Video.DirectShow;

namespace kaihatsuProject
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    /// 

    public partial class MainWindow : System.Windows.Window//
    {//
        NamedPipeCommunication pipe;
        ExternalAppManager myManager;
        GazedPoint gaze;

        VideoCaptureDevice webcam;
        private FilterInfoCollection videoDevices;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;            

            pipe = new NamedPipeCommunication("mypipe");
            pipe.getMessageEvent += test;
            myCanvas2.DataContext = pipe;//パイプコンストラクト前に置くと機能しないしエラーも出ない
            pipe.WaitConnectionAsync();

            myManager = new ExternalAppManager();
            LoadSettings.LoadFromTextTest<ExternalAppManager>("settings.txt", myManager);

            gaze = new GazedPoint(300, 200);
            Image1.DataContext = gaze;
            myCanvas.DataContext = gaze;
            text1.DataContext = gaze;
            text2.DataContext = gaze;
            text3.DataContext = gaze;
            text4.DataContext = gaze;

            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            comboBox1.Items.Clear();
            if (videoDevices.Count == 0)
                throw new ApplicationException();


            //DeviceExist = true;
            foreach (FilterInfo device in videoDevices)
            {
                comboBox1.Items.Add(device.Name);
            }
            comboBox1.SelectedIndex = 0; //make dafault to first cam            

        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            //Bitmap img = (Bitmap)eventArgs.Frame.Clone();
            //var hbitmap = img.GetHbitmap();
            //videoImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            Dispatcher.Invoke(new Action(() => {
                using (var stream = new MemoryStream())
                using (Bitmap img = (Bitmap)eventArgs.Frame.Clone())
                {
                    img.Save(stream, ImageFormat.Bmp);
                    videoImage.Source = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
            }));            
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            //Point point = e.GetPosition(this);
            //Canvas.SetLeft(Image1, point.X);
            //Canvas.SetTop(Image1, point.Y            
        }

        private void message_Click(object sender, RoutedEventArgs e)
        {//カメラ起動
            //MessageBox.Show("メッセージボックス");
            webcam = new VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);
            webcam.NewFrame += video_NewFrame;
            webcam.Start();            
        }

        private void swichApp_Click(object sender, RoutedEventArgs e)
        {
            if (myManager.IsAlive) { myManager.Close(); }
            else { myManager.Start(); }
        }

        void test(object sender, MyEventArgs e)
        {//角度をパイプから受け取った時に発火する
            //MessageBox.Show(e.message);
            try
            {
                string[] str = e.message.Split(',');
                gaze.ReculculateXY(double.Parse(str[0]), double.Parse(str[1]));
            }
            catch(Exception ex)
            {
                gaze.ReculculateXY(0, 0);
            }            
                       
        }        

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {//終了処理
            //pipe.Close();
            if (myManager.IsAlive)
            {
                myManager.Close();
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //webcam.Stop();
            webcam.NewFrame -= video_NewFrame;
            webcam.SignalToStop();
            webcam = null;
        }        
    }
}

