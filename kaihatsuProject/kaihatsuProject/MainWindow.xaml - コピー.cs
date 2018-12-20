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

using Microsoft.Expression.Encoder.Devices;//webcamcontrolが要求
using System.Collections.ObjectModel;//webcamcontrolが要求

using System.Windows.Forms.Integration;

namespace kaihatsuProject
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    /// 

    public partial class MainWindow : System.Windows.Window//
    {//
        public Collection<EncoderDevice> VideoDevices { get; set; }
        public Collection<EncoderDevice> AudioDevices { get; set; }

        NamedPipeCommunication pipe;
        ExternalAppManager myManager;
        GazedPoint gaze;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;            

            pipe = new NamedPipeCommunication("mypipe");
            pipe.getMessageEvent += test;
            myCanvas.DataContext = pipe;//パイプコンストラクト前に置くと機能しないしエラーも出ない
            pipe.WaitConnectionAsync();

            myManager = new ExternalAppManager();
            LoadSettings.LoadFromTextTest<ExternalAppManager>("settings.txt", myManager);

            gaze = new GazedPoint(200, 300);

            VideoDevices = EncoderDevices.FindDevices(EncoderDeviceType.Video);
            AudioDevices = EncoderDevices.FindDevices(EncoderDeviceType.Audio);

            
        }




        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            //Point point = e.GetPosition(this);
            //Canvas.SetLeft(Image1, point.X);
            //Canvas.SetTop(Image1, point.Y
            
        }

        private void message_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("メッセージボックス");
            //WebcamViewer
            foreach (var child in LogicalTreeHelper.GetChildren(WebcamViewer))
            {
                if (child is Grid)
                {
                    //(Grid)child;
                    foreach(ComboBox gchild in LogicalTreeHelper.GetChildren((Grid)child))
                    {
                        
                    }
                }
                
            }
        }

        private void swichApp_Click(object sender, RoutedEventArgs e)
        {
            if (myManager.IsAlive)
            {
                myManager.Close();
            }
            else
            {
                myManager.Start();
            }

        }

        void test(object sender, MyEventArgs e)
        {//角度をパイプから受け取った時に発火する

            //MessageBox.Show(e.message);
            
        }

        private void StartCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Display webcam video
                WebcamViewer.StartPreview();
            }
            catch (Microsoft.Expression.Encoder.SystemErrorException ex)
            {
                MessageBox.Show("Device is in use by another application");
            }
        }

        private void StopCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop the display of webcam video.
            WebcamViewer.StopPreview();
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

        }        
    }
}


    
