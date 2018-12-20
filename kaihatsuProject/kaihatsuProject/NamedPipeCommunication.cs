using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.ComponentModel;

using System.IO.Pipes;

namespace kaihatsuProject
{
    public class MyEventArgs : EventArgs
    {
        public string message;

        public MyEventArgs() { message = ""; }
    }


    public class NamedPipeCommunication : INotifyPropertyChanged
    {//名前付きパイプによる通信用のクラス, 今とりあえずで一度waitAsyncするとあと勝手に読み待ち入って死んだら復活接続待ちするようになってる
        //あとでそこらへん設定できるようにフラグとか実装すること
        delegate void WaitDelegate();
        delegate void ReadDelegate();
        enum ConnectState//パイプ状態、それぞれ生成前, 別スレッドでクライアント接続待ち中, クライアント接続中, 今後利用不可状態
        {//死んだら生き返らないがRemakeで作り直せる
            notBornYet, born, waitingConnection, availavle, dead
        }
        public delegate void MyEventHandler(object sender, MyEventArgs e);//通信を受け取ったときに発火するイベントの型

        private NamedPipeServerStream pipeS;//名前付きパイプ本体(サーバー側)
        private WaitDelegate waitDelegate;//BeginInvokeを使いたかっただけ, クライアント接続待ち
        private ReadDelegate readDelegate;//同上, 接続後の読み待ち(読むと空になって次が送られてくるか切断するまで待ちになる)

        private string name;//パイプ名を保存しておく
        private string labelStr;//プロパティ用
        private string messageStr;//プロパティ用

        private ConnectState connectState;//enum, 接続状態

        public MyEventHandler getMessageEvent;//パイプから通信でメッセージ受け取ったときに発火するイベント

        public bool IsAvailable
        {//接続中のみtrue
            get
            {
                if (null == pipeS) { return false; }
                else { return pipeS.IsConnected; }                
            }
        }
        

        public string ConnectStateLabel
        {//バインディング用, パイプの接続状況を示す
            get { return labelStr; }
            private set
            {
                labelStr = value;
                RaisePropertyChanged("ConnectStateLabel");//UIに更新通知出すらしい

                if ("接続成功" == value)//このやり方はダメだけどとりあえず暫定
                {
                    ReadAsyncContinuously();
                }
            }
        }

        public string MessageLabel
        {//バインディング用, 読み取ったメッセージを示す
            get { return messageStr; }
            private set
            {
                messageStr = value;
                RaisePropertyChanged("MessageLabel");//UIに更新通知

                if (getMessageEvent != null)
                {//メッセージ取得時設定されたイベントを発火, 引数eのメンバにメッセージ内容
                    MyEventArgs e = new MyEventArgs();
                    e.message = value;
                    getMessageEvent(this, e);
                }

                //ここで切断されてなければ次の読み待ちに入る(ループ)
                if (IsAvailable) { readDelegate.BeginInvoke(null, null); }
                else
                {
                    if (connectState == ConnectState.availavle)
                    {//切断でも読み待ちが即座に解除されるのでここに来る
                        connectState = ConnectState.dead;
                        ConnectStateLabel = "接続切断";
                        //MessageBox.Show("disconnect");
                        Remake();//暫定
                        WaitConnectionAsync();//暫定
                    }
                }
            }
        }



        public NamedPipeCommunication(string str)//コンストラクタ
        {
            name = str;
            waitDelegate = WaitConnectionOnDelegate;
            readDelegate = ReadOnDelegate;
            connectState = ConnectState.notBornYet;

            ConnectStateLabel = "パイプ作成";
            MessageLabel = "";

            try
            {
                connectState = ConnectState.born;
                pipeS = new NamedPipeServerStream(str, PipeDirection.In, 1, PipeTransmissionMode.Byte);                
            }
            catch (Exception e)
            {
                ConnectStateLabel = "パイプ作成に失敗";
                MessageBox.Show(e.ToString());
                connectState = ConnectState.dead;
            }
        }

        public void Close()
        {  
            if (connectState != ConnectState.notBornYet && connectState != ConnectState.dead)
            {//開いてないパイプや閉じた閉じると例外?, 壊れた(切断)パイプ閉じると例外は来ないが謎の待ち時間が発生して怖い
                pipeS.Close();
                connectState = ConnectState.dead;
            }
        }

        public void Remake()
        {//deadなパイプ捨てて作り直す
            //MessageBox.Show("remake");
            pipeS.Close();
            pipeS.Dispose();
            try
            {
                //MessageBox.Show("remake");
                connectState = ConnectState.born;
                ConnectStateLabel = "パイプ作成";
                pipeS = new NamedPipeServerStream(name, PipeDirection.In, 1, PipeTransmissionMode.Byte);
            }
            catch (Exception e)
            {
                connectState = ConnectState.dead;
                ConnectStateLabel = "パイプ作成に失敗";
                MessageBox.Show(e.ToString());
            }
        }

        public void WaitConnectionAsync()
        {
            if (connectState == ConnectState.born)
            {
                connectState = ConnectState.waitingConnection;
                waitDelegate.BeginInvoke(null, null);
            }            
        }

        private void WaitConnectionOnDelegate()
        {//内部用, デリゲートに乗せるための関数
            ConnectStateLabel = "待機中";

            try { pipeS.WaitForConnection(); }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                connectState = ConnectState.dead;
                return;
            }

            connectState = ConnectState.availavle;
            ConnectStateLabel = "接続成功";        
        }

        public void ReadAsyncContinuously()
        {//開始するとデリゲートで非同期読み待ち→読んでmessageLabel更新→また読み待ちが起動
            if (IsAvailable)
            {
                ConnectStateLabel = "読み待ち開始";
                readDelegate.BeginInvoke(null, null);                
            }
        }

        private void ReadOnDelegate()
        {//内部用, デリゲートに乗せるための関数
            int n = 0;
            byte[] buffer = new byte[100];
            try { n = pipeS.Read(buffer, 0, 100); }//ここで待ちが発生(接続時のみ、切断で処理帰る)

            catch(Exception e) { MessageBox.Show(e.ToString()); }

            Array.Resize(ref buffer, n);
            
            MessageLabel = System.Text.Encoding.ASCII.GetString(buffer);                        
            //readDelegate.BeginInvoke(null, null);
            //ここで呼ぶのはうまくいかない, 何故かは分からん  
            //スレッドが違うからではないか?
        }

        


        //以下がviewmodelのクラスには必要らしい
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            var d = PropertyChanged;
            if (d != null)
                d(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

