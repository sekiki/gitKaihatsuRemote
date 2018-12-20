using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

namespace kaihatsuProject
{
    class GazedPoint : INotifyPropertyChanged//バインディング用, 視点の座標を求めて格納するとviewに反映する
    {
        private int xValue;
        private int yValue;
        public int X { get { return xValue; } }
        public int Y { get { return yValue; } }

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

        public GazedPoint(int xo, int yo)
        {
            xValue = originXvalue = xo;
            yValue = originYvalue = yo;
            Kx = Ky = 400;
        }

        public void ReculculateXY(double angleX, double angleY)
        {//角度はdegree
            xValue = (int)(originXvalue + Kx * Math.Sin(angleX * Math.PI / 180));
            yValue = (int)(originYvalue + Ky * Math.Sin(angleY * Math.PI / 180));
            RaisePropertyChanged("X");//UIに更新通知
            RaisePropertyChanged("Y");//UIに更新通知
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
