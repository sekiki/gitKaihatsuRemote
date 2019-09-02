using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.IO;

namespace kaihatsuProject
{
    class PointData
    {
        static Stopwatch myWatch;

        public double x;
        public double y;
        public long time;
        public PointData(double xv, double yv)
        {
            x = xv; y = yv;
            time = myWatch.ElapsedMilliseconds;
        }

        static PointData()
        {//staticメンバの初期化(C#だとstaticコンストラクタなるものを用いる)
            myWatch = new Stopwatch();
            myWatch.Start();
        }

        //public static long GetCurrentTime() { return myWatch.ElapsedMilliseconds; }
    }

    class TimeLinePointDatas : IMySetter
    {
        private LinkedList<PointData> PointDatas;
        private long timeThres;//時間閾値
        private double distanceThres;//距離閾値
        private long baseTime;//この時間から閾値の時間まで見続けてたら興味あり判定
        public bool StillGazingAtOnePoint { get; private set; }//興味検知の後から他に視線が移動するまでの間true
        public bool InterestDetected { get; private set; }//興味検知がなされた瞬間だけtrue(振動モジュールで通知出すとき使う)


        public TimeLinePointDatas()
        {
            PointDatas = new LinkedList<PointData>();
            timeThres = 100;//ミリ秒
            distanceThres = 30;//ラジアン
            baseTime = 0;
            StillGazingAtOnePoint = false;
            InterestDetected = false;
        }

        public void Add(PointData pd)//listにAddするついでに興味検知してプロパティをアップデートする関数
        {//ここを書き換えることで色々な興味検知の方法を試せるので改変したければここのPointDatas.AddLast(pd);以外を書き換えてください
            int k = PointDatas.Count;
            if (k > 1)
            {
                double d = Math.Pow((PointDatas.Last.Value.x - pd.x), 2) + Math.Pow((PointDatas.Last.Value.y - pd.y), 2);
                d = Math.Sqrt(d);
                if (d < distanceThres)//時間判定へ
                {
                    if (pd.time - baseTime > timeThres)
                    {
                        if (!StillGazingAtOnePoint)
                        {//注視が始まってから最初にここに来た
                            StillGazingAtOnePoint = true;
                            InterestDetected = true;//「今」検知された
                        }
                        else
                        {
                            InterestDetected = false;
                        }
                    }
                }
                else
                {
                    baseTime = pd.time;
                    StillGazingAtOnePoint = false;
                    InterestDetected = false;
                }
            }
            PointDatas.AddLast(pd);
            if (k > 100)
            {//要素数が無限に大きくなってメモリを圧迫しないよう適当なとこで消す
                PointDatas.RemoveFirst();
            }

        }

        public void WriteToCSV(string fileName)//csv出力する
        {
            int k = PointDatas.Count;
            string str = "";
            if (k > 1)
            {
                str = PointDatas.Last.Value.x.ToString() + "," + PointDatas.Last.Value.y.ToString() + "," + PointDatas.Last.Value.time.ToString() + "\n";
            }
            else
            {
                str = "TimeLinePointDatasクラスのPointDatas.countが0です" + "\n";
            }
            File.AppendAllText(fileName, str);
        }

        public LinkedList<PointData> GetPointDatas() { return PointDatas; }

        public void LoadMember(List<string> memNames, List<string> memValues)
        {//メモ帳からロードするLoadSettingsの関数にこのクラスをぶち込むために必要なインターフェース(暫定版)
            for (int i = 0; i < memNames.Count; i++)
            {
                if ("timethres" == memNames[i].ToLower())
                {
                    try { timeThres = long.Parse(memValues[i]); }
                    catch (Exception e) { MessageBox.Show(e.ToString()); }
                    Console.WriteLine("TimeLinePointDatas 時間閾値 " + timeThres.ToString() + " ミリ秒が読み込まれました");
                }
                else if ("distancethres" == memNames[i].ToLower())
                {
                    try { distanceThres = double.Parse(memValues[i]); }
                    catch (Exception e) { MessageBox.Show(e.ToString()); }
                    Console.WriteLine("TimeLinePointDatas 角度閾値 " + distanceThres.ToString() + " 度が読み込まれました");
                }
            }
        }

        public void SaveMember(List<string> memNames, List<string> memValues)
        {//未実装

        }


    }

    /// <summary>
    /// 視点座標を計算・格納し興味検知も行う, バインディング可なプロパティ有
    /// </summary>
    class GazedPoint : INotifyPropertyChanged//バインディング用, 視点の座標を求めて格納するとviewに反映する
    {
        public TimeLinePointDatas pointDatas;//テキストからロードするための暫定処理
        public event MyEventHandler InterestDetected;//興味検知時に発火するイベント, 振動デバイスへの通知が入ればいい
        private string gazeStateStr;
        private int xValue;
        private int yValue;
        public int X { get { return xValue; } }
        public int Y { get { return yValue; } }
        private double SX;
        private double SY;

        private int originXvalue;//原点(0度, 0度を受け取ったときに出てくる点), キャリブレーション用
        private int originYvalue;
        public int OriginX
        {
            get { return originXvalue; }
            set { originXvalue = value; RaisePropertyChanged("OriginX"); }
        }
        public int OriginY
        {
            get { return originYvalue; }
            set { originYvalue = value; RaisePropertyChanged("OriginY"); }
        }

        private int KxValue;
        private int KyValue;
        public int Kx//係数, でかくすると目線が大きく動くように, キャリブレーション用
        {
            get { return KxValue; }
            set { KxValue = value; RaisePropertyChanged("KxValue"); }
        }
        public int Ky
        {
            get { return KyValue; }
            set { KyValue = value; RaisePropertyChanged("KyValue"); }
        }

        //private bool isWritingCsvFlag;
        public bool IsWriting { get; set; }

        public string GazeStateLabel//バインディング用, 興味検知の状態を示す
        {
            get { return gazeStateStr; }
            set
            {
                gazeStateStr = value;
                RaisePropertyChanged("GazeStateLabel");
            }
        }

        //////////////////////////////////////////////////////////////////////////

        public GazedPoint(int xo, int yo)
        {
            xValue = originXvalue = xo;
            yValue = originYvalue = yo;
            Kx = Ky = 400;
            pointDatas = new TimeLinePointDatas();
            IsWriting = false;
            gazeStateStr = "";
            SX = 0;
            SY = 0;
        }

        /// <summary>
        /// 座標の再計算と興味検知の判定を行う
        /// </summary>
        /// <param name="angleX"></param>
        /// <param name="angleY"></param>
        public void ReculculateXY(double angleX, double angleY)
        {//角度はdegree
            xValue = (int)(originXvalue + Kx * Math.Sin(GazeFilterX(angleX) * Math.PI / 180));
            yValue = (int)(originYvalue + Ky * Math.Sin(GazeFilterY(angleY) * Math.PI / 180));
            RaisePropertyChanged("X");//UIに更新通知, なぜここに書く?
            RaisePropertyChanged("Y");//UIに更新通知
            pointDatas.Add(new PointData(angleX, angleY));
            if (pointDatas.InterestDetected)
            {
                //System.Windows.MessageBox.Show("興味検知");
                //ここで振動モジュールに通知するための処理, 書くためにはイベントハンドラ持つ必要あり?
                if (InterestDetected != null)
                {
                    MyEventArgs e = new MyEventArgs();
                    InterestDetected(this, e);
                }
            }

            if (pointDatas.StillGazingAtOnePoint)
            {
                GazeStateLabel = "興味検知中";
            }
            else
            {
                GazeStateLabel = "";
            }

            if (IsWriting)
            {
                pointDatas.WriteToCSV("gazeLog.csv");
            }
        }

        public void getHeatMap(ref System.Windows.Media.Imaging.BitmapSource Image)//HeatMapのbitmapを作成させて吐かせる
        {
            HeatMap.GenerateHeatMap(pointDatas.GetPointDatas(), ref Image);

        }

        //以下がviewmodelのクラスには必要らしい
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            var d = PropertyChanged;
            if (d != null)
                d(this, new PropertyChangedEventArgs(propertyName));
        }

        //指数移動平均で視点座標のばらつきを抑える関数
        public double GazeFilterX(double angle)
        {
            double N = 9;
            double alpha = 2 / (N + 1);

            SX = alpha * angle + (1 - alpha) * SX;
            return SX;
        }

        public double GazeFilterY(double angle)
        {
            double N = 9;
            double alpha = 2 / (N + 1);

            SY = alpha * angle + (1 - alpha) * SY;
            return SY;
        }
    }
}
