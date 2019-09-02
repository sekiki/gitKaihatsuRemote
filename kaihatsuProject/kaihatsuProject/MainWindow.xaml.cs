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
        public NamedPipeCommunication pipe;
        ExternalAppManager myManager;
        GazedPoint gaze;

        VideoCaptureDevice webcam;
        FilterInfoCollection videoDevices;

        SerialCommunication mySerial;


        public MainWindow()
        {//長い  けどスッキリさせるのが難しい...
            //ログ出力のための設定
            StreamWriter streamWriter = new StreamWriter("debug.log", false);//既にファイルがあっても追加ではなく上書き
            streamWriter.AutoFlush = true;//これやっとかないとバッファに貯めこまれてアプリ終了まで書き込まれなかったりする
            Console.SetOut(streamWriter);
            Console.WriteLine("ウィンドウコンストラクタ");

            InitializeComponent();//これは必須


            pipe = new NamedPipeCommunication("mypipe");
            pipe.MessageReceived += test;//メッセージ受信時の処理
            pipe.WaitConnectionAsync();
            testLabel.DataContext = pipe;//パイプコンストラクト前に置くと機能しないしエラーも出ない
            messageLabel.DataContext = pipe;

            myManager = new ExternalAppManager();
            LoadSettings.LoadFromTextTest<ExternalAppManager>("settings.txt", myManager);//テキストからロード

            gaze = new GazedPoint(300, 200);
            gaze.InterestDetected += OnInterestDetected;
            gazeImage.DataContext = gaze;
            text1.DataContext = gaze;
            text2.DataContext = gaze;
            text3.DataContext = gaze;
            text4.DataContext = gaze;
            gazeStateLabel.DataContext = gaze;
            LoadSettings.LoadFromTextTest<TimeLinePointDatas>("settings.txt", gaze.pointDatas);

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

            mySerial = new SerialCommunication("a", 1);//test
            LoadSettings.LoadFromTextTest<SerialCommunication>("settings.txt", mySerial);
            mySerial.Open();

            Console.WriteLine("ウィンドウコンストラクタ終了");
        }

        //以下イベントハンドラ//////////////////////////////////////////////////////////////////////////////////////////////

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {//webカメラが新しいキャプチャを受け取ったときに発火
            Dispatcher.Invoke(new Action(() => {
                using (var stream = new MemoryStream())
                using (Bitmap img = (Bitmap)eventArgs.Frame.Clone())
                {
                    img.Save(stream, ImageFormat.Bmp);
                    videoImage.Source = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
            }));
        }

        private void message_Click(object sender, RoutedEventArgs e)
        {//カメラ起動
            //MessageBox.Show("メッセージボックス");
            webcam = new VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);
            webcam.NewFrame += video_NewFrame;
            webcam.Start();
        }

        private void swichApp_Click(object sender, RoutedEventArgs e)
        {//連携アプリ起動/終了
            if (myManager.IsAlive) { myManager.Close(); }
            else { myManager.Start(); }
        }

        void test(object sender, MyEventArgs e)
        {//角度をパイプから受け取った時に発火する
            try
            {
                string[] str = e.message.Split(',');

                Dispatcher.Invoke(new Action(() =>
                {
                    gaze.ReculculateXY(double.Parse(str[0]), double.Parse(str[1]));
                }));

            }
            catch (Exception ex)
            {
                gaze.ReculculateXY(0, 0);
            }
            //Console.WriteLine("ヒートマップ開始");

            Dispatcher.Invoke(new Action(() =>
            {
                Bitmap bMapcolor = new Bitmap(600, 350);//bMapをカラーにした後情報を格納するビットマップ
                System.Windows.Media.Imaging.BitmapSource Image;
                IntPtr hbitmap = bMapcolor.GetHbitmap();
                Image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());//新たに追加
                gaze.getHeatMap(ref Image);
                HeatMapa.Source = Image;
                DeleteObject(hbitmap);
            }));

            //Console.WriteLine("ヒートマップ終了");
        }

        void OnInterestDetected(object sender, MyEventArgs e)
        {//興味推定時の処理, シリアル通信で振動デバイスに通知する処理を書く
            //Console.WriteLine("視線検出 テスト");
            mySerial.Write();
        }

        private void aaa_CLick(object sender, RoutedEventArgs e)
        {//カメラ再取得, デモの動画撮るのに必要だっただけで今後消える
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            comboBox1.Items.Clear();
            if (videoDevices.Count == 0)
                throw new ApplicationException();
            foreach (FilterInfo device in videoDevices)
            {
                comboBox1.Items.Add(device.Name);
            }
            comboBox1.SelectedIndex = 0; //make dafault to first cam
        }

        private void WriteToCSV_CLick(object sender, RoutedEventArgs e)
        {//CSV出力モードにする/出力止める
            if (!gaze.IsWriting)
            {
                gaze.IsWriting = true;
                WriteToCSVButton.Content = "csv出力:出力中";
            }
            else
            {
                gaze.IsWriting = false;
                WriteToCSVButton.Content = "csv出力:停止中";
            }
        }

        private void Calibrate_CLick(object sender, RoutedEventArgs e)
        {//

        }

        private void Window_Closing(object sender, EventArgs e)
        {//終了処理
            mySerial.Close();
            if (myManager.IsAlive)
            {
                myManager.Close();
            }
            webcam.NewFrame -= video_NewFrame;
            webcam.SignalToStop();
            webcam = null;
        }
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject); // gdi32.dllのDeleteObjectメソッドの使用を宣言する。
    }
}

