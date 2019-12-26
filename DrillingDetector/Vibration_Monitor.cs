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
using CUTeffiDrillingModule;



namespace CUTeffiDrillingModule
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
        //public StreamWriter SW_State;
        //public StreamWriter SW_State2;
        public StreamWriter SW_RawData;
        public double[] vecTime, xdata, ydata, zdata, vecSP;
        public double rms_xdata, rms_ydata, rms_zdata;
        public int startstate = 0, indexP;
        [STAThread]
        public void StartSampling()
        {
            triggerSlope = AnalogEdgeStartTriggerSlope.Rising;
            sensitivityUnits = AIAccelerometerSensitivityUnits.MillivoltsPerG;
            terminalConfiguration = (AITerminalConfiguration)(-1);
            excitationSource = AIExcitationSource.Internal;
            inputCoupling = AICoupling.AC;
            SW_RMSData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\" + DateTime.Now.ToString("yyyy - MM - dd HH - mm - ss") + "_" + "RMSData.txt");
            SW_RawData = new StreamWriter(System.Environment.CurrentDirectory + "\\logData\\" + DateTime.Now.ToString("yyyy - MM - dd HH - mm - ss") + "_" + "RawData.txt");

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
                Monitor();

                analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(1280),
                    analogCallback, myTask, data);


                DrillingDetector.CUTeffi_DrillingModule.aGauge.Value = Convert.ToSingle(rms_zdata);



                //sw.Stop();
                //TimeSpan ts2 = sw.Elapsed;
                //int A = 1;
            }
        }
        public void Stopsampling()
        {
            SW_RMSData.Dispose();
            SW_RawData.Dispose();
            //SW_State.Dispose();
            //SW_State2.Dispose();
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
        private void Monitor()
        {
            
            StartSampling();
            DrillingDetector.CUTeffi_DrillingModule.aGauge.Value = Convert.ToSingle(rms_zdata);
            //及時監控
            if (rms_xdata > 0.2)
            {
                if (rms_zdata > rms_xdata)
                {
                    if (rms_zdata > Convert.ToDouble(DrillingDetector.CUTeffi_DrillingModule.threshold.Text))
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
            for (int i = 0; i < zdata.Length; i++)
            {
                SW_RawData.Write(vecTime[i]);
                //SW_RawData.Write(",");
                // SW_RawData.Write(xpos[i]);
                //SW_RawData.Write(",");
                //SW_RawData.Write(ypos[i]);
                //SW_RawData.Write(",");
                //SW_RawData.Write(SpindleRPM[i]);
                //SW_RawData.Write(",");
                //SW_RawData.Write(Feedrate[i]);
                SW_RawData.Write(",");
                SW_RawData.WriteLine(zdata[i]);
            }

        }
        public void processRed()
        {
            DrillingDetector.CUTeffi_DrillingModule.pictureBox.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_red.png");
            //label4.Text = Convert.ToString(Convert.ToDouble(textBox3.Text) - 20);
            //label3.Text = Convert.ToString(Convert.ToDouble(label4.Text) * 10 - 200);
        }

        public void processGreen()
        {
            DrillingDetector.CUTeffi_DrillingModule.pictureBox.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_green.png");
        }
        private static void Move_Spindle()
        {
            //Z軸Feed = 0
            //Z軸往上拉
            //Spindle Rotation = 0
            //紀錄X、Y座標
            //移動X、Y軸至門口
        }
    }
}
