using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace kaihatsuProject
{
    interface IMySetter//こいつを持ってるやつをLoadSettingのジェネリック関数で扱える
    {
        void SetMember(List<string> memNames, List<string> memValues);
        void GetMember(List<string> memNames, List<string> memValues);//まだなんも書いてない
    }


    class LoadSettings//本当に暫定版です, あとでいろいろなんとかします
    {
        public static void LoadFromTextTest<T>(string fileName, T obj)//とりあえず本当に暫定
            where T : IMySetter
        {
            StreamReader reader = new StreamReader(fileName);
            string buffer;
            string nameBuf = "";
            string valueBuf = "";
            List<string> memNames = new List<string>();
            List<string> memValues = new List<string>();

            while (false == reader.EndOfStream)
            {
                buffer = reader.ReadLine();
                getNameAndValue(buffer, ref nameBuf, ref valueBuf);

                if ("" != nameBuf && "" != valueBuf)
                {
                    memNames.Add(nameBuf);
                    memValues.Add(valueBuf);
                }
            }

            obj.SetMember(memNames, memValues);


        }

        
        private static bool CharCanBeSkipped(char c)
        {
            if (' ' == c || '\t' == c || '　' == c || '\"' == c || '=' == c)
            {//半角全角スペースとタブと"と=なら読み飛ばす
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void getNameAndValue(string buffer, ref string memName, ref string memValue)
        {
            memName = "";
            memValue = "";

            string str = "";
            int count = 0;
            bool reading = false;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (reading)
                {
                    if (CharCanBeSkipped(buffer[i]))
                    {//ひとつの文字列を読み終えた
                        if (0 == count)
                        {
                            memName = str;
                            str = "";
                            reading = false;
                            count++;
                        }
                        else
                        {
                            memValue = str;
                            return;
                        }
                    }
                    else
                    {//読んでる
                        str = str + buffer[i];
                        if (i == buffer.Length - 1)
                        {//読み終えてしまった
                            if (1 == count)
                            {
                                memValue = str;
                                return;
                            }
                        }
                    }
                }
                else
                {
                    if (!CharCanBeSkipped(buffer[i]))
                    {
                        reading = true;
                        str = "" + buffer[i];
                    }
                }
            }
        }
    }
}






















/*
if (readingCommentout)
                    {
                        if (buffer[i - 1] == '*' && buffer[i] == '/')
                        {
                            readingCommentout = false;// /*で始まったコメントアウトを抜ける
                        }
                    }
                    else
                    {
                        if (i > 0)
                        {
                            if (buffer[i - 1] == '/' && buffer[i] == '/') { break; }//コメントアウト//があったら先は読まない
                            else if(buffer[i - 1] == '/' && buffer[i] == '*')
                            {
                                readingCommentout = true;// /*でコメントアウトに入る
                            }
                        }

                        
                    }


    ////////////////////////////////////////////////////
    ///


    bool outsideBraces = true;
            bool readingString = false;
            bool leftSideOfEqual = true;

            string buffer;
            string classNameBuf = "";
            string memNameBuf;
            string memValueBuf;

            while (false == reader.EndOfStream)
            {
                buffer = reader.ReadLine();
                
                for(int i = 0; i < buffer.Length; i++)
                {
                    if (outsideBraces)//{}の外にいる
                    {
                        if ('{' == buffer[i])
                        {//クラス名が正しければ「読むモード」で{}の内側に入る
                            outsideBraces = false;
                            if (className.ToLower() == classNameBuf.ToLower())
                            {

                            }
                        }
                        else if (readingString)
                        {//クラス名を読んでいる途中
                            if (CharCanBeSkipped(buffer[i]))
                            {
                                readingString = false;//文字列がここで区切れた
                            }
                            else
                            {
                                classNameBuf = classNameBuf + buffer[i];
                            }
                        }
                        else
                        {
                            if (!CharCanBeSkipped(buffer[i]))
                            {//新たなクラス名文字列を発見, 前のは捨てる
                                readingString = true;
                                classNameBuf = "" + buffer[i];

                            }
                        }

                    }
                    else
                    {//{}の中にいる
                        if ('}' == buffer[i])
                        {//{}の外に出る
                            outsideBraces = true;
                        }


                    }
                }

            }
        }




*/
