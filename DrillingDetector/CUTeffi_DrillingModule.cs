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
using Itri.Vmx.Host;
using Itri.Vmx.Cnc;
using Itri.Vmx.Plc;
using System.ComponentModel.Composition;
using System.Threading;

namespace DrillingDetector
{
    [Export(typeof(IVmxApp))]
    public partial class CUTeffi_DrillingModule : Form,Itri.Vmx.Host.IVmxApp
    {
        private int indexSettingPanel = 0;
        public int startstate = 0;
        public static AGauge aGauge;
        public static PictureBox pictureBox;
        public static TextBox threshold,RPMmax,CutPerRPM;
        public static Label Feedrate, Spindle_code,Xpos,Ypos;
        public static CheckBox opt_checkbox;
        Vibration_Monitor Vibration_monitor = new Vibration_Monitor();

        public string AppName => "DrillingDetector";
        public Image Image => Image.FromFile(System.Environment.CurrentDirectory + "\\image\\icon.png");
        //public bool Initialize(IVmxHost host)
        //{
        //    return true;
        //}
        //public static CncAdaptor cnc = null;
        public static Vmx2Fanuc30D cnc;


        public bool Initialize(IVmxHost host)
        {
            cnc = new Vmx2Fanuc30D();
            ConnectingSettingCollection settings = cnc.ConnectingSetting;
            settings["IP"] = "192.168.10.1";
            settings["Port"] = "8193";
            cnc.ConnectingSetting = settings;
            cnc.Connect();
            bool result1 = cnc.IsConnected;
            //if (host.CncAdaptors.Length != 0)
            //{
            //    cnc = host.CncAdaptors[0];
            //}
            return true;
        }
        public static DataItem mach_pos = new DataItem();
        public static DataItem spindlespeed = new DataItem();
        public static DataItem sCode = new DataItem();
        public static DataItem fCode = new DataItem();
        public static DataItem CNCPLC = new DataItem();

        public CUTeffi_DrillingModule()
        {
            InitializeComponent();
            pictureBox = this.pictureBox_state;
            aGauge = this.aGauge1;
            threshold=this.textBox1;
            RPMmax = this.textBox4;
            CutPerRPM = this.textBox6;
            Feedrate = this.label4;
            Spindle_code = this.label3;
            Xpos = this.label9;
            Ypos = this.label15;
            opt_checkbox = this.optimization_checkbox;
            this.Size = new Size(816, 539);
            label3.Text = " ";
            label4.Text = " ";
            label9.Text = " ";
            label15.Text = " ";
            pictureBox3.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_ButtonTestStart.png");
            pictureBox5.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_graylight.png");

            //  3. Set DataItem Path
            mach_pos.Path = "/axes/AbsolutePositions";
            spindlespeed.Path = "/Spindle/ActualSpeed";
            sCode.Path = "/controller/SpindleSpeedCmd";
            fCode.Path = "/controller/feedrateCmd";
            CNCPLC.Path ="/Plc/Fanuc";
         


        }

        private void button_OPTNC_Click(object sender, EventArgs e)
        {
            string s = Application.StartupPath;////

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = "C:\\Users\\User\\Desktop\\";
            sfd.Filter = "txt files (*.txt)|*.txt";
            sfd.ShowDialog();
            string saveNC = sfd.FileName;
            

            if (string.IsNullOrEmpty(saveNC)) { }
            else
            {
                EditNC nc = new EditNC();
                double maxSP = Convert.ToDouble(textBox4.Text);
                double FeedperRotation = Convert.ToDouble(textBox6.Text);
                double Xpos_opt = Convert.ToDouble(label9.Text);
                double Ypos_opt = Convert.ToDouble(label15.Text);
                double cutdepth = Convert.ToDouble(textBox7.Text);
                double toolnumber = Convert.ToDouble(textBox8.Text);
                nc.editNC_opt(maxSP, FeedperRotation, Xpos_opt, Ypos_opt,toolnumber, cutdepth, s, saveNC);  //textbox4: maximum spindle speed, textbox3: cutting depth, textbox5: tool number
                Thread.Sleep(1000);
                string sourcePath = saveNC;
                string destinationPath = "O0106";
                bool result2=cnc.UploadFile(sourcePath, destinationPath);
                //sourcePath：檔案絕對路徑
                //destinationPath：上傳的檔名(Fanuc的命名方式)
            }


        }

        private void button_drillNC_Click(object sender, EventArgs e)
        {
            string s = Application.StartupPath;////

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = "C:\\Users\\User\\Desktop\\";
            sfd.Filter = "txt files (*.txt)|*.txt";
            sfd.ShowDialog();
            string saveNC = sfd.FileName;

            if (string.IsNullOrEmpty(saveNC)) { }
            else
            {
                EditNC nc = new EditNC();
                double optSP = Convert.ToDouble(label3.Text);
                double FeedperRotation = Convert.ToDouble(textBox6.Text);
                double cutdepth = Convert.ToDouble(textBox7.Text);
                nc.editNC_dodrill(optSP, FeedperRotation,cutdepth, s, saveNC);  //textbox4: maximum spindle speed, textbox3: cutting depth, textbox5: tool number
                Thread.Sleep(1000);
                string sourcePath = saveNC;
                string destinationPath = "O0107";
                cnc.UploadFile(sourcePath, destinationPath);
                //cnc.DownloadFile(destinationPath,sourcePath);
                //sourcePath：檔案絕對路徑
                //destinationPath：上傳的檔名(Fanuc的命名方式)
            }

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

                startstate = 1;
                panelSetting.Enabled = false;
                Vibration_monitor.StartSampling();
            }
            else if (startstate ==1)
            {
                pictureBox3.Image = Image.FromFile(System.Environment.CurrentDirectory + "\\image\\image_ButtonTestStart.png");
                optimization_checkbox.Enabled = true;

                startstate = 0;
                panelSetting.Enabled = true;
                Vibration_monitor.Stopsampling();
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


        //private static void Avoid_touch()
        //{
        //    //輸入工件幾何
        //    //讀取NC code(平台移動、主軸下降)
        //    //辨識路徑是否干涉
        //    //if干涉 Alarm
        //}

       
       

    }
}
