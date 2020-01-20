using System;
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
using DrillingDetector;
using Itri.Vmx.Host;
using Itri.Vmx.Cnc;
using System.ComponentModel.Composition;



namespace DrillingDetector
{
    class Vibration_Monitor
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
        public double[] vecTime, xdata, ydata, zdata, vecSP;
        public double rms_xdata, rms_ydata, rms_zdata;
        public int startstate = 0, indexP;
        public string Zpos;

        [STAThread]
        public void StartSampling()
        {
            triggerSlope = AnalogEdgeStartTriggerSlope.Rising;
            sensitivityUnits = AIAccelerometerSensitivityUnits.MillivoltsPerG;
            terminalConfiguration = (AITerminalConfiguration)(-1);
            excitationSource = AIExcitationSource.Internal;
            inputCoupling = AICoupling.AC;
            


            myTask = new NationalInstruments.DAQmx.Task();
            AIChannel aiChannel;

            double Vmin = -5;
            double Vmax = 5;
            double sen = 100;
            double EVN = 0.004;
            double[] chan = new double[4] { 1, 1, 1, 0 };
            if (CUTeffi_DrillingModule.opt_checkbox.Checked == true)
            {
                SW_RMSData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\RMSData.txt");
                SW_State = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\State.txt");
                SW_State2 = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\State2.txt");
                SW_RawData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\RawData.txt");
            }
            else
            {
                SW_RMSData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\" + DateTime.Now.ToString("yyyy - MM - dd HH - mm - ss") + "_" + "RMSData.txt");
                SW_RawData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\" + DateTime.Now.ToString("yyyy - MM - dd HH - mm - ss") + "_" + "RawData.txt");
            }

            for (int i = 0; i < chan.Length; i++)
            {
                if (chan[i] == 1)
                {
                    aiChannel = myTask.AIChannels.CreateAccelerometerChannel("cDAQ2Mod1/ai" + Convert.ToString(i), "",
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
                //4.Read Value
                CUTeffi_DrillingModule.cnc.ReadDataItem(ref CUTeffi_DrillingModule.mach_pos);
                CUTeffi_DrillingModule.cnc.ReadDataItem(ref CUTeffi_DrillingModule.sCode);
                CUTeffi_DrillingModule.cnc.ReadDataItem(ref CUTeffi_DrillingModule.fCode);
                CUTeffi_DrillingModule.cnc.ReadDataItem(ref CUTeffi_DrillingModule.spindlespeed);
                if (CUTeffi_DrillingModule.opt_checkbox.Checked == true)
                {
                    CUTeffi_OPT();
                }
                else
                {
                    Monitor();
                }


                analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(1280),
                    analogCallback, myTask, data);


                CUTeffi_DrillingModule.aGauge.Value = Convert.ToSingle(rms_zdata);


               

                //sw.Stop();
                //TimeSpan ts2 = sw.Elapsed;
                //int A = 1;
            }
        }
        public void Stopsampling()
        {
            if (CUTeffi_DrillingModule.opt_checkbox.Checked == true)
            {
                SW_RMSData.Dispose();
                SW_RawData.Dispose();
                SW_State.Dispose();
                SW_State2.Dispose();
            }
            else
            {
                SW_RMSData.Dispose();
                SW_RawData.Dispose();
            }
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

        public void Monitor()
        {
            
            CUTeffi_DrillingModule.aGauge.Value = Convert.ToSingle(rms_zdata);
            //5.Show Value
            CUTeffi_DrillingModule.Xpos.Text = (CUTeffi_DrillingModule.mach_pos.Value as Array).GetValue(0).ToString();
            CUTeffi_DrillingModule.Ypos.Text = (CUTeffi_DrillingModule.mach_pos.Value as Array).GetValue(1).ToString();
            Zpos = (CUTeffi_DrillingModule.mach_pos.Value as Array).GetValue(2).ToString();
            CUTeffi_DrillingModule.Spindle_code.Text = CUTeffi_DrillingModule.sCode.Value.ToString();
            CUTeffi_DrillingModule.Feedrate.Text = CUTeffi_DrillingModule.fCode.Value.ToString();
            //及時監控
            if (Convert.ToInt32(CUTeffi_DrillingModule.spindlespeed.Value) >50)
            {
                if (rms_zdata > rms_xdata)
                {
                    if (rms_zdata > Convert.ToDouble(CUTeffi_DrillingModule.threshold.Text))
                    {
                        processRed();
                        Stopsampling();
                        //Move_Spindle();
                        return;
                    }
                }
                else
                {
                    processGreen();
                }
            }
            //存檔區
            if (rms_zdata > 0.25 && Convert.ToDouble(Zpos) < 10) //rms_zdata > 0.25 && Convert.ToDouble(Zpos) <= 10
            {
                SW_RMSData.Write(vecTime[0]);
                SW_RMSData.Write(",");
                SW_RMSData.Write(CUTeffi_DrillingModule.Xpos.Text);
                SW_RMSData.Write(",");
                SW_RMSData.Write(CUTeffi_DrillingModule.Ypos.Text);
                SW_RMSData.Write(",");
                SW_RMSData.Write(CUTeffi_DrillingModule.Spindle_code.Text);
                SW_RMSData.Write(",");
                SW_RMSData.Write(CUTeffi_DrillingModule.Feedrate.Text);
                SW_RMSData.Write(",");
                SW_RMSData.WriteLine(rms_zdata);
                for (int i = 0; i < zdata.Length; i++)
                {
                    SW_RawData.Write(vecTime[i]);
                    SW_RawData.Write(",");
                    SW_RawData.Write(CUTeffi_DrillingModule.Xpos.Text);
                    SW_RawData.Write(",");
                    SW_RawData.Write(CUTeffi_DrillingModule.Ypos.Text);
                    SW_RawData.Write(",");
                    SW_RawData.Write(CUTeffi_DrillingModule.Spindle_code.Text);
                    SW_RawData.Write(",");
                    SW_RawData.Write(CUTeffi_DrillingModule.Feedrate.Text);
                    SW_RawData.Write(",");
                    SW_RawData.WriteLine(zdata[i]);
                }
            }
            
        }
        public void processRed()
        {
            CUTeffi_DrillingModule.pictureBox.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_red.png");
            //label4.Text = Convert.ToString(Convert.ToDouble(textBox3.Text) - 20);
            //label3.Text = Convert.ToString(Convert.ToDouble(label4.Text) * 10 - 200);
        }
        public void processGreen()
        {
            CUTeffi_DrillingModule.pictureBox.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_green.png");
        }

        private static void Move_Spindle()
        {
            //控制PLC寫入
            CUTeffi_DrillingModule.CNCPLC.Parameter[0] = "byte";
            CUTeffi_DrillingModule.CNCPLC.Parameter[1] = "G8.4";
            CUTeffi_DrillingModule.CNCPLC.Value = 90;
            int errorCode = CUTeffi_DrillingModule.cnc.WriteDataItem(CUTeffi_DrillingModule.CNCPLC);
            //
            //Z軸Feed = 0
            //Z軸往上拉
            //Spindle Rotation = 0
            //alarm、緊急停止
            //紀錄X、Y座標
            //移動X、Y軸至門口
        }
        public void CUTeffi_OPT()
        {
            double SPmax = Convert.ToDouble(CUTeffi_DrillingModule.RPMmax.Text);
            //textBox2.Text = "" + Convert.ToDouble(textBox4.Text) * 0.5;
            //移動至指定XY座標
            //小助手優化程序

            int iii = 0;
            int indexP = 0;
            //SW_RMSData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\RMSData.txt");
            //SW_State = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\State.txt");
            //SW_State2 = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\State2.txt");
            //SW_RawData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\RawData.txt");

            double[] vecSP = new double[9];
            double[] vecSP2 = new double[] { 0, 1600, 200, 800, 400, 1200, 1400, 600, 1000 };
            //for (int i = 0; i < 12; i++)
            for (int i = 0; i < 9; i++)
            {
                vecSP[i] = SPmax - vecSP2[i];//---------------------------------------------------------------------------------------------

            }
            //StartSampling();
            CUTeffi_DrillingModule.aGauge.Value = Convert.ToSingle(rms_zdata);
            if (rms_zdata > 0.25 && Convert.ToDouble(Zpos) < 10) // 閥值定義：轉與不轉振動量大小 && Z軸位置
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
            double SPmax = Convert.ToDouble(CUTeffi_DrillingModule.RPMmax.Text);
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
            CUTeffi_DrillingModule.Spindle_code.Text = Convert.ToString(OptimizedSP[0]);
            CUTeffi_DrillingModule.Feedrate.Text = Convert.ToString(OptimizedSP[0] * Convert.ToInt32(CUTeffi_DrillingModule.CutPerRPM.Text));
            //return OptimizedSP;
            //double[] A = CUTeffiForm.A;
            //A = OptimizedSP;
            //SplashScreen.cut.panelupdate(A);
        }
    }

}
