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


namespace DrillingDetector
{
    public partial class CUTeffi_DrillingModule : Form
    {
        public SplashScreen FSS = null;
        private int indexSettingPanel = 0;
        public int startstate = 0;
        
        public static AGauge aGauge;
        public static PictureBox pictureBox;
        public static TextBox threshold,RPMmax,CutPerRPM;
        public static Label Feedrate, Spindle_rpm;
        public static CheckBox opt_checkbox;
        Vibration_Monitor Vibration_monitor = new Vibration_Monitor();


        public CUTeffi_DrillingModule()
        {
            InitializeComponent();
            pictureBox = this.pictureBox_state;
            aGauge = this.aGauge1;
            threshold=this.textBox1;
            RPMmax = this.textBox4;
            CutPerRPM = this.textBox6;
            Feedrate = this.label4;
            Spindle_rpm = this.label3;
            opt_checkbox = this.optimization_checkbox;
            this.Size = new Size(816, 539);
            label3.Text = " ";
            label4.Text = " ";
            label9.Text = " ";
            label15.Text = " ";
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
                pictureBox3.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_ButtonTestStop.png");
                optimization_checkbox.Enabled = false;
                button1.Enabled = false;
                startstate = 1;
                if (optimization_checkbox.Checked == true)
                {
                    Refresh();
                    
                }
                else
                {
                    Refresh();
                    Vibration_monitor.StartSampling();
                }
            }
            else if (startstate ==1)
            {
                pictureBox3.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_ButtonTestStart.png");
                optimization_checkbox.Enabled = true;
                button1.Enabled = true;
                startstate = 0;
                if (optimization_checkbox.Checked == true)
                {
                    //CUTeffi_Opt.Stopsampling();
                }
                else
                {
                    Vibration_monitor.Stopsampling();
                }
                aGauge1.Value = 0;
            }
           
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            textBox5.Text = "" + Convert.ToDouble(textBox4.Text) * Convert.ToDouble(textBox6.Text);
            textBox2.Text = "" + Convert.ToDouble(textBox4.Text) * 0.5;
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox3.Text = "" + Convert.ToDouble(textBox2.Text) * Convert.ToDouble(textBox6.Text);
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
