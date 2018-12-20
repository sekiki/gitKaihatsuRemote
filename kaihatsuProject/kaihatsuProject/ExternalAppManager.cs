using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Windows;//messagebox

namespace kaihatsuProject
{
    class ExternalAppManager : IMySetter//System.Diagnostics.Processのバインディング用クラス(ViewModel的な?)
    {//その割には今バインディング用のメンバがない
        Process myProcess;//ほぼこいつが本体
        bool openingFlag;//IsAlive用

        public bool IsAlive { get { return openingFlag; } }//「自分が」アプリを開いているか

        private void ExitEvent(object sender, System.EventArgs e)
        {//このクラスによって起動されたアプリが終了するとき必ずこのイベントが発生する(終了検知)
            openingFlag = false;
        }


        public ExternalAppManager()
        {
            myProcess = new Process();
            myProcess.StartInfo.FileName = "NotePad";
            myProcess.EnableRaisingEvents = true;
            myProcess.Exited += new EventHandler(ExitEvent);
            openingFlag = false;
        }

        public void Start()
        {
            openingFlag = true;
            try
            {
                myProcess.Start();
            }
            catch(Exception e)
            {//おそらく起動が失敗している
                openingFlag = false;
                myProcess.StartInfo.FileName = "NotePad";
                MessageBox.Show("ファイルが見つからなかったのでメモ帳を設定しました");
            }
        }
        
        public void Close()
        {//起動してるときだけ閉じる処理しないと例外が来る
            if (IsAlive)
            {
                myProcess.CloseMainWindow();
                openingFlag = false;
            }
            
        }

        public void SetMember(List<string> memNames, List<string> memValues)
        {//メモ帳からロードするLoadSettingsの関数にこのクラスをぶち込むために必要なインターフェース(暫定版)
            for (int i = 0; i < memNames.Count; i++)
            {
                if ("path" == memNames[i].ToLower())
                {
                    myProcess.StartInfo.FileName = memValues[i];
                }
                else if ("commandlinearg" == memNames[i].ToLower())
                {
                    myProcess.StartInfo.Arguments = memValues[i];
                }
            }
        }

        public void GetMember(List<string> memNames, List<string> memValues)
        {//未実装

        }
    }
}
