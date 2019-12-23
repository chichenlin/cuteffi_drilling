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
        


        public CUTeffi_DrillingModule()
        {
            InitializeComponent();

            this.Size = new Size(816, 539);
            label3.Text = " ";
            label4.Text = " ";
            label9.Text = " ";
            label15.Text = " ";
            StartSampling();
            pictureBox3.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_ButtonTestStart.png");
            pictureBox5.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_graylight.png");

        }

        [STAThread]
        private void StartSampling()
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
                double[] vecTime = new double[data[0].SampleCount];

                double[] xdata = data[0].GetRawData();
                double[] ydata = data[1].GetRawData();
                double[] zdata = data[2].GetRawData();
                //logData1 = data[0].GetRawData();
                PrecisionDateTime[] T = data[0].GetPrecisionTimeStamps();

                for (int i = 0; i < data[0].SampleCount; i++)
                {
                    vecTime[i] = T[i].TimeOfDay.TotalSeconds;
                }
                double varFs = 1 / ((vecTime[data[0].SampleCount - 1] - vecTime[0]) / (data[0].SampleCount - 1));
                //double varFs2 = 1 / (vecTime[1] - vecTime[0]);

                analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(1280),
                    analogCallback, myTask, data);

                double rms_xdata = rootMeanSquare(xdata);
                double rms_zdata = rootMeanSquare(zdata);
                aGauge1.Value = Convert.ToSingle(rms_zdata);

                if (rms_xdata > 0.2)
                {
                    if (rms_zdata > rms_xdata)
                    {
                        if (rms_zdata > Convert.ToDouble(textBox1.Text))
                        {
                            processRed();
                            //Z軸停止
                            //紀錄XY軸資訊
                            //Z軸上升
                            //Spindle stop
                            //移動X、Y軸至門口
                            Optmization();
                        }
                    }
                    else
                    {
                        processGreen();
                    }
                }

                //sw.Stop();
                //TimeSpan ts2 = sw.Elapsed;
                //int A = 1;
            }
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

        private void button1_Click(object sender, EventArgs e)
        {
            switch (indexSettingPanel)
            {
                case 0:
                    for(int i = 0; i <= 300; i = i + 10)
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
        private static void Optmization()
        {
            //移動至指定XY座標
            //小助手優化程序
            //輸出轉速、進給
        }


    }
}
