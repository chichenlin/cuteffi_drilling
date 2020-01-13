﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NationalInstruments.DAQmx;
using NationalInstruments;
using System.Diagnostics;
using System.IO;

namespace DrillingDetector
{
    public partial class CUTeffi_DrillingModule : Form
    {
        private AnalogMultiChannelReader analogInReader;
        private AIExcitationSource excitationSource;
        private AIAccelerometerSensitivityUnits sensitivityUnits;
        private AITerminalConfiguration terminalConfiguration;
        private AnalogEdgeStartTriggerSlope triggerSlope;
        private AICoupling inputCoupling;

        private NationalInstruments.DAQmx.Task myTask;
        private NationalInstruments.DAQmx.Task runningTask;
        private AsyncCallback analogCallback;
        private AnalogWaveform<double>[] data;

        private int indexSettingPanel = 0;
        public StreamWriter SW_RMSData;
        public StreamWriter SW_State;
        public StreamWriter SW_State2;
        public StreamWriter SW_RawData;
        public double[] vecTime,xdata,ydata,zdata;
        public double rms_xdata, rms_ydata, rms_zdata;
        int startstate = 0;


        public CUTeffi_DrillingModule()
        {
            InitializeComponent();

            this.Size = new Size(816, 539);
            label3.Text = " ";
            label4.Text = " ";
            label9.Text = " ";
            label15.Text = " ";
            //StartSampling();
            pictureBox3.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_ButtonTestStart.png");
            pictureBox5.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_graylight.png");

        }
        private void button1_Click(object sender, EventArgs e)
        {
            switch (indexSettingPanel)
            {
                case 0:
                    for (int i = 0; i <= 300; i = i + 10)
                    {
                        panelSetting.Location = new Point(695 - i, 0);
                        this.Refresh();
                    }
                    indexSettingPanel = 1;
                    break;
                case 1:
                    for (int i = 300; i >= 0; i = i - 10)
                    {
                        panelSetting.Location = new Point(695 - i, 0);
                        this.Refresh();
                    }
                    indexSettingPanel = 0;
                    break;
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            pictureBox_state.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_red.png");
            label3.Text = "1400";
            label4.Text = "170";
            label9.Text = "0";
            label15.Text = "329.952";
            pictureBox5.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_yellowlight.png");
            aGauge1.Value = 8.4F;
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            if (startstate == 0)
            {
                optimization_checkbox.Enabled = false;
                button1.Enabled = false;
                startstate = 1;
                if (optimization_checkbox.Checked == true)
                {
                    CUTeffi_opt();
                }
                else
                {
                    Monitor();
                }
            }
            else if (startstate ==1)
            {
                Stopsampling();
            }
           
            
            
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            textBox5.Text = "" + Convert.ToDouble(textBox4.Text) * Convert.ToDouble(textBox6.Text);
            
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox3.Text = "" + Convert.ToDouble(textBox2.Text) * Convert.ToDouble(textBox6.Text);
        }

        [STAThread]
        private void StartSampling()
        {
            triggerSlope = AnalogEdgeStartTriggerSlope.Rising;
            sensitivityUnits = AIAccelerometerSensitivityUnits.MillivoltsPerG;
            terminalConfiguration = (AITerminalConfiguration)(-1);
            excitationSource = AIExcitationSource.Internal;
            inputCoupling = AICoupling.AC;
            //SW_RMSData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\RMSData.txt");
            //SW_State = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\State.txt");
            //SW_State2 = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\State2.txt");
            //SW_RawData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\RawData.txt");

            myTask = new NationalInstruments.DAQmx.Task();
            AIChannel aiChannel;

            double Vmin = -5;
            double Vmax = 5;
            double sen = 100;
            double EVN = 0.004;
            double[] chan = new double[4] { 1, 1, 1, 0 };

            for (int i = 0; i < chan.Length; i++)
            {
                if (chan[i] == 1)
                {
                    aiChannel = myTask.AIChannels.CreateAccelerometerChannel("cDAQ1Mod1/ai" + Convert.ToString(i), "",
                        terminalConfiguration, Vmin, Vmax, sen, sensitivityUnits, excitationSource,
                        EVN, AIAccelerationUnits.G);
                    aiChannel.Coupling = inputCoupling;
                }

            }

            myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(12800),
                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, Convert.ToInt32(1280));

            myTask.Control(TaskAction.Verify);

            runningTask = myTask;
            analogInReader = new AnalogMultiChannelReader(myTask.Stream);
            analogCallback = new AsyncCallback(AnalogInCallback);

            analogInReader.SynchronizeCallbacks = true;
            analogInReader.BeginReadWaveform(Convert.ToInt32(1280), analogCallback, myTask);

        }

        private void AnalogInCallback(IAsyncResult ar)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            if (runningTask != null && runningTask == ar.AsyncState)
            {
                // Read the available data from the channels
                data = analogInReader.EndReadWaveform(ar);
                int numdata = data.Length;

                //double[] logData = new double[data[0].SampleCount];
                vecTime = new double[data[0].SampleCount];

                xdata = data[0].GetRawData();
                ydata = data[1].GetRawData();
                zdata = data[2].GetRawData();
                //logData1 = data[0].GetRawData();
                PrecisionDateTime[] T = data[0].GetPrecisionTimeStamps();

                for (int i = 0; i < data[0].SampleCount; i++)
                {
                    vecTime[i] = T[i].TimeOfDay.TotalSeconds;
                }
                //double varFs = 1 / ((vecTime[data[0].SampleCount - 1] - vecTime[0]) / (data[0].SampleCount - 1));
                //double varFs2 = 1 / (vecTime[1] - vecTime[0]);
                rms_xdata = rootMeanSquare(xdata);
                rms_ydata = rootMeanSquare(ydata);
                rms_zdata = rootMeanSquare(zdata);

                analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(1280),
                    analogCallback, myTask, data);

                
                //aGauge1.Value = Convert.ToSingle(rms_zdata);

                

                //sw.Stop();
                //TimeSpan ts2 = sw.Elapsed;
                //int A = 1;
            }
        }
        private void Stopsampling()
        {
            SW_RMSData.Dispose();
            SW_State.Dispose();
            SW_State2.Dispose();
            SW_RawData.Dispose();
            runningTask = null;
            myTask.Dispose();
        }

        //--------------------------------------------------------------------------------------------------------------//
        //------------------------------------- Function of root mean square ---------------------------------------//
        //-------------------------------------------------------------------------------------------------------------//
        private static double rootMeanSquare(double[] x)
        {
            double sum = 0;
            for (int i = 0; i < x.Length; i++)
            {
                sum += (x[i] * x[i]);
            }
            return Math.Sqrt(sum / x.Length);
        }

        private void processRed()
        {
            pictureBox_state.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_red.png");
            label4.Text = Convert.ToString(Convert.ToDouble(textBox3.Text) - 20);
            label3.Text = Convert.ToString(Convert.ToDouble(label4.Text) * 10 - 200);
         }

        private void processGreen()
        {
            pictureBox_state.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_green.png");
        }

      

        private void CUTeffi_opt()
        {
            double SPmax = Convert.ToDouble(textBox4.Text);
            textBox2.Text = ""+Convert.ToDouble(textBox4.Text) * 0.5;
            //移動至指定XY座標
            //小助手優化程序
            
            int iii = 0;
            int indexP = 0;
            SW_RMSData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\RMSData.txt");
            SW_State = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\State.txt");
            SW_State2 = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\State2.txt");
            SW_RawData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\RawData.txt");
            StartSampling();
            aGauge1.Value = Convert.ToSingle(rms_zdata);


            double[] vecSP = new double[14];
            double[] vecSP2 = new double[] { 0, 0, 0, 2000, 750, 2500, 250, 1750, 1000, 2750, 1250, 2250, 1500, 500 };
            //for (int i = 0; i < 12; i++)
            for (int i = 0; i < 14; i++)
            {
                vecSP[i] = SPmax - vecSP2[i];//---------------------------------------------------------------------------------------------
                vecSP[0] = 3183;
                vecSP[1] = 3183;
            }

            if (rms_zdata > 0.25 ) // 閥值定義：轉與不轉振動量大小 && entropy[0] < threshold_entropy
            {
                if (iii == 3)//穩定大於閥值0.3秒後，紀錄當下時間
                {
                    SW_State.WriteLine(vecTime[0]);
                    SW_State2.WriteLine("start");
                    Console.WriteLine("start");
                }
                iii++;
            }
            else
            {
                if (iii >= 3)//小於閥值後，紀錄切削結束時間
                {
                    iii = 0;
                    indexP++;
                    SW_State.WriteLine(vecTime[0] - 0.1);
                    SW_State2.WriteLine("stop");
                    Console.WriteLine("Spindle stop now");

                }
                else
                {
                    Console.WriteLine("Spindle will spin up in few seconds ");
                }
            }
            if (indexP == vecSP.Length)//當indexP超過最後一個切削轉速後，停止擷取程式、記錄結束時間、進行最佳化
            {
                SW_State.WriteLine(vecTime[0] - 0.1);
                SW_State2.WriteLine("end");
                Stopsampling();
                Optmization();
                return;
            }
            Console.Write(indexP + "  " + vecSP[indexP] + "  " + vecTime[0] + "  " + rms_zdata + " " + iii + " ");
            //存檔區
            SW_RMSData.Write(vecTime[0]);
            SW_RMSData.Write(",");
            SW_RMSData.Write(vecSP[indexP]);
            SW_RMSData.Write(",");
            SW_RMSData.WriteLine(rms_zdata);
            for (int i = 0; i < xdata.Length; i++)
            {
                SW_RawData.Write(vecTime[i]);
                SW_RawData.Write(",");
                SW_RawData.Write(xdata[i]);
                SW_RawData.Write(",");
                SW_RawData.Write(ydata[i]);
                SW_RawData.Write(",");
                SW_RawData.WriteLine(zdata[i]);
            }
            //存檔區結束
            //輸出轉速、進給
        }

        private void Optmization()
        {
            double SPmax = Convert.ToDouble(textBox4.Text);
            //------------------------------------- Read log_State ---------------------------------------//
            int counter1 = 0;
            string line1;
            StreamReader SR_State = new StreamReader(System.Environment.CurrentDirectory + "\\logdata\\State.txt");
            List<double> DataRead1 = new List<double>();

            while ((line1 = SR_State.ReadLine()) != null)
            {
                DataRead1.Add(Convert.ToDouble(line1));
                //Console.WriteLine(DataRead[counter]);
                counter1++;
            }
            double[] vecState = new double[counter1];
            
            for (int i = 0; i < vecState.Length; i++)//counter1
            {
                vecState[i] = DataRead1[i];
            }
            //------------------------------------- Read log_RMSData ---------------------------------------//
            int counter = 0;
            string line;
            StreamReader SR_RMSData = new StreamReader(System.Environment.CurrentDirectory + "\\logData\\RMSData.txt");
            List<List<double>> DataRead = new List<List<double>>();

            while ((line = SR_RMSData.ReadLine()) != null)
            {
                string[] split;
                split = line.Split(new char[] { ',' });
                DataRead.Add(new List<double>() { Convert.ToDouble(split[0]), Convert.ToDouble(split[1]),
                    Convert.ToDouble(split[2]) });
                //Console.WriteLine(DataRead[counter]);
                counter++;
            }
            double[] vecTime1 = new double[counter];
            double[] SpindleSpeed = new double[counter];
            double[] RMSData = new double[counter];


            for (int i = 0; i < counter; i++)
            {
                vecTime1[i] = DataRead[i][0];
                SpindleSpeed[i] = DataRead[i][1];
                RMSData[i] = DataRead[i][2];
            }
            SR_RMSData.Dispose();
            SR_RMSData.Close();

            SR_State.Close();
            //------------------------------------- Spindle speed optimization ---------------------------------------//
            //int indexMaterial = CUTeffiForm.indexMaterial;
            //double[] vecSP = new double[12];
            double[] vecSP = new double[9];
            //if (indexMaterial == 1)
            //{
            //double[] vecSP2 = new double[] { 0, 2000, 750, 2500, 250, 1750, 1000, 2750, 1250, 2250, 1500, 500 };
            double[] vecSP2 = new double[] { 0, 1600, 200, 800, 400, 1200, 1400, 600, 1000 };
            //for (int i = 0; i < 12; i++)
            for (int i = 0; i < 9; i++)
            {
                vecSP[i] = SPmax - vecSP2[i];//---------------------------------------------------------------------------------------------
            }
            //}
            //else if (indexMaterial == 2)
            //{
            //    //double[] vecSP2 = new double[] { 0, 1600, 600, 2000, 200, 1400, 800, 2200, 1000, 1800, 1200, 400 };
            //    double[] vecSP2 = new double[] { 0, 0, 0, 1600, 600, 2000, 200, 1400, 800, 2200, 1000, 1800, 1200, 400 };
            //    //for (int i = 0; i < 12; i++)
            //    for (int i = 0; i < 13; i++)
            //    {
            //        vecSP[i] = SPmax - vecSP2[i];//---------------------------------------------------------------------------------------------
            //        vecSP[0] = 3000;
            //        vecSP[1] = 3000;
            //    }
            //}
            //double[] vecSP = new double[12];//-------------------------------------------------------------------------------------------------
            int[] vecLoc = new int[vecState.Length];
            //for (int i = 12; i >= 1; i--)
            //{
            //    vecSP[i - 1] = maxSP - 250 * (13 - i - 1);
            //}

            for (int i = 0; i < vecState.Length - 1; i++)
            {
                vecLoc[i] = Array.IndexOf(vecTime1, vecState[i]); 
            }

            double[] Crms = new double[vecSP.Length];
            for (int i = 0; i < vecSP.Length; i++)//vecSP.Length
            {
                int varRange = vecLoc[2 * i + 1] - vecLoc[2 * i]; //stop index - start index
                int varLoc1 = Convert.ToInt32(Math.Floor(varRange * 0.8));
                int varLoc2 = Convert.ToInt32(Math.Floor(varRange * 0.1));
                double[] arrayA = new double[varLoc1 - varLoc2 + 1];
                Array.Copy(RMSData, vecLoc[2 * i] + varLoc2, arrayA, 0, varLoc1 - varLoc2 + 1);
                //Array.Copy(RMSData, vecLoc[i] - varLoc1, arrayA, 0, varLoc1 - varLoc2 + 1);
                double varA = rootMeanSquare(arrayA);
                Crms[i] = varA;
            }

            //double[] OptimizedSP = new double[4];
            double[] OptimizedSP = new double[1];
            double[] arrayB = new double[Crms.Length];
            Array.Copy(Crms, 0, arrayB, 0, Crms.Length);
            //for (int i = 1; i <= 4; i++)
            //{
            int varB = Array.IndexOf(arrayB, arrayB.Min());
            //OptimizedSP[i - 1] = vecSP[varB + 2];
            OptimizedSP[0] = vecSP[varB];
            arrayB[varB] = 1000000000;
            //}
            label3.Text = Convert.ToString(OptimizedSP[0]);
            label4.Text = Convert.ToString(OptimizedSP[0] * Convert.ToInt32(textBox6.Text));
            //return OptimizedSP;
            //double[] A = CUTeffiForm.A;
            //A = OptimizedSP;
            //SplashScreen.cut.panelupdate(A);
        }
       
        
        private void Monitor()
        {
            SW_RMSData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\"+ DateTime.Now.ToString("yyyy - MM - dd HH - mm - ss") + "_" +"RMSData.txt");
            SW_RawData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\" + DateTime.Now.ToString("yyyy - MM - dd HH - mm - ss") + "_" + "RawData.txt");
            StartSampling();
            aGauge1.Value = Convert.ToSingle(rms_zdata);
            //及時監控
            if (rms_xdata > 0.2)
            {
                if (rms_zdata > rms_xdata)
                {
                    if (rms_zdata > Convert.ToDouble(textBox1.Text))
                    {
                        processRed();
                        Stopsampling();
                        Move_Spindle();
                        return;
                    }
                }
                else
                {
                    processGreen();
                }
            }
            SW_RMSData.Write(vecTime[0]);
            SW_RMSData.Write(",");
            SW_RMSData.WriteLine(rms_zdata);
            for (int i = 0; i < xdata.Length; i++)
            {
                SW_RawData.Write(vecTime[i]);
                SW_RawData.Write(",");
                SW_RawData.Write(xdata[i]);
                SW_RawData.Write(",");
                SW_RawData.Write(ydata[i]);
                SW_RawData.Write(",");
                SW_RawData.WriteLine(zdata[i]);
            }
        }
        private static void Move_Spindle()
        {
            //Z軸Feed = 0
            //Z軸往上拉
            //Spindle Rotation = 0
            //紀錄X、Y座標
            //移動X、Y軸至門口
        }
        private static void Avoid_touch()
        {
            //輸入工件幾何
            //讀取NC code(平台移動、主軸下降)
            //辨識路徑是否干涉
            //if干涉 Alarm
        }
    }
}