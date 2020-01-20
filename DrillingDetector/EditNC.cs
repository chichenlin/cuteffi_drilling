using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DrillingDetector
{
    class EditNC
    {
        List<string> NCi = new List<string>();
        public StreamReader sr;

        public void editNC_opt(double maxSP, double FeedperRotation, double Xpos, double Ypos, double toolnumber,double cutdepth,string s, string saveNC)
        {

            //string fileName = "D4000426_2.cnc";
            
                sr = new StreamReader(s + "\\NC\\O0106(opt).txt");////
            
             
            //StreamReader sr = new StreamReader(s + "\\NC\\O7202.txt");////

            while (!sr.EndOfStream)
            {
                string str = "";

                str = sr.ReadLine();

                str = str.Trim();

                if (str == "%")
                {
                    continue;
                }

                NCi.Add(str);

            }

            EditNc_opt(maxSP, FeedperRotation, Xpos,Ypos,toolnumber,cutdepth);

            writeNC(saveNC);

            ////str.ReadLine();
            ////str.ReadToEnd();
            ////str.Close(); 
        }

        
        private void EditNc_opt(double maxSP, double FeedperRotation, double Xpos, double Ypos,double toolnumber,double cutdepth)
        {
            string[] split;

            string line = "";

            List<string> filter;

            for (int i = 0; i < NCi.Count; i++)
            {
                if (NCi[i].Substring(0, 1) == "#")
                {
                    split = NCi[i].Split(new char[] { '=', '#', '(' });

                    filter = new List<string>();

                    foreach (string s in split)
                        filter.Add(s.Trim());

                    for (int ii = 1; ii < filter.Count; ii++)
                    {
                        if (ii == 1 & filter[ii] == "1")
                        {
                            //filter[ii + 1] = textBox1.Text;

                            line = "#" + filter[ii] + "=" + Convert.ToString(maxSP) + "(" + filter[ii + 2];////

                            NCi[i] = line;
                        }
                        if (ii == 1 & filter[ii] == "2")
                        {

                            line = "#" + filter[ii] + "=" + Convert.ToString(FeedperRotation) + "(" + filter[ii + 2];////

                            NCi[i] = line;
                        }
                        if (ii == 1 & filter[ii] == "3")
                        {

                            line = "#" + filter[ii] + "=" + Convert.ToString(Xpos) + "(" + filter[ii + 2];////

                            NCi[i] = line;
                        }
                        if (ii == 1 & filter[ii] == "4")
                        {

                            line = "#" + filter[ii] + "=" + Convert.ToString(Ypos) + "(" + filter[ii + 2];////

                            NCi[i] = line;
                        }
                        if (ii == 1 & filter[ii] == "6")
                        {

                            line = "#" + filter[ii] + "=" + Convert.ToString(toolnumber) + "(" + filter[ii + 2];////

                            NCi[i] = line;
                        }
                        if (ii == 1 & filter[ii] == "7")
                        {

                            line = "#" + filter[ii] + "=" + Convert.ToString(cutdepth) + "(" + filter[ii + 2];////

                            NCi[i] = line;
                        }

                        line = "";
                    }
                }
            }
        }
        public void editNC_dodrill(double maxSP, double FeedperRotation, double cutdepth, string s, string saveNC)
        {

            //string fileName = "D4000426_2.cnc";

            sr = new StreamReader(s + "\\NC\\O0107(dodrill).txt");////


            //StreamReader sr = new StreamReader(s + "\\NC\\O7202.txt");////

            while (!sr.EndOfStream)
            {
                string str = "";

                str = sr.ReadLine();

                str = str.Trim();

                if (str == "%")
                {
                    continue;
                }

                NCi.Add(str);

            }

            EditNc_dodrill(maxSP, FeedperRotation, cutdepth);

            writeNC(saveNC);

            ////str.ReadLine();
            ////str.ReadToEnd();
            ////str.Close(); 
        }
        private void EditNc_dodrill(double maxSP, double FeedperRotation, double cutdepth)
        {
            string[] split;

            string line = "";

            List<string> filter;

            for (int i = 0; i < NCi.Count; i++)
            {
                if (NCi[i].Substring(0, 1) == "#")
                {
                    split = NCi[i].Split(new char[] { '=', '#', '(' });

                    filter = new List<string>();

                    foreach (string s in split)
                        filter.Add(s.Trim());

                    for (int ii = 1; ii < filter.Count; ii++)
                    {
                        if (ii == 1 & filter[ii] == "1")
                        {
                            //filter[ii + 1] = textBox1.Text;

                            line = "#" + filter[ii] + "=" + Convert.ToString(maxSP) + "(" + filter[ii + 2];////

                            NCi[i] = line;


                        }
                        if (ii == 1 & filter[ii] == "2")
                        {

                            line = "#" + filter[ii] + "=" + Convert.ToString(FeedperRotation) + "(" + filter[ii + 2];////

                            NCi[i] = line;


                        }
                        if (ii == 1 & filter[ii] == "3")
                        {

                            line = "#" + filter[ii] + "=" + Convert.ToString(cutdepth) + "(" + filter[ii + 2];////

                            NCi[i] = line;


                        }
                       

                        line = "";
                    }
                }
            }
        }
        private void writeNC(string saveNC)
        {
            string NewNC = "";

            for (int i = 0; i < NCi.Count; i++)
            {
                NewNC += NCi[i] + "\r\n";

            }

            StreamWriter str = new StreamWriter(saveNC);

            str.WriteLine(NewNC);

            str.Close();

            //string newNC = "";

        }

    }
}
