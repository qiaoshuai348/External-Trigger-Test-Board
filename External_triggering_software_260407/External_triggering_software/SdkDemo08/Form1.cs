using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using StructModel;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Threading;
using System.Media;
using System.IO.Ports;

namespace SdkDemo08
{




    public partial class Form1 : Form
    {
        //判断相机是否已经链接,默认为没有连接
        //Whether the camera is connected.The default is no connection
        public int isConnect = 0;
        //handle
        public static IntPtr camhandle;
        //设置一些对象，存放相机的相关参数
        //Set some object, store the related parameters of the camera
        uint x = 0, h = 0, bpp, c, ret;
        double chipw, chiph, pixelw, pixelh;
        byte[] rawArray, rgbArray;
        System.Collections.Queue QHYQueue = new System.Collections.Queue();
        Bitmap bitmap;
        Rectangle rectangle;
        BitmapData bmpData;
        IntPtr ptr;
        int s;
        int index;
        Byte pixData = 0;
        int counter_timer;
        int counter_timer_top = 10;


        Byte trig_mode = 0;


        UInt16 sensorModel = 455;        //global define the sensor model.


        ushort[,] PhaseErrorScan = new ushort[100, 23];
        ushort[,] PhaseEdgeScan = new ushort[100, 23];
        ushort[,] PhaseEdgeScanChanged = new ushort[100, 23];
        ushort[] InputDelayValue = new ushort[100];
        ushort[] BitPosition = new ushort[100];
        byte[] word_position = new byte[100];
        byte[] word_position_before = new byte[100];
        byte[] ErrorScanSum = new byte[100];
        byte[][] id = new byte[10][];


        bool SDKAPI = false;
        bool SDK_LIVESTOP = false;



        private SerialPort ComDevice = new SerialPort();
        private TriggerBoardControl triggerBoardControl;

        public Form1()
        {
            InitializeComponent();
            InitializeTriggerBoardPage();
            this.FormClosing += new FormClosingEventHandler(Form1_TriggerBoardClosing);
        }

        private void InitializeTriggerBoardPage()
        {
            TabPage page = new TabPage("Trig Board");
            page.Name = "tabPageTriggerBoard";
            triggerBoardControl = new TriggerBoardControl();
            page.Controls.Add(triggerBoardControl);

            int insertIndex = tabLVDS.TabPages.Count;
            for (int i = 0; i < tabLVDS.TabPages.Count; i++)
            {
                if (String.Equals(tabLVDS.TabPages[i].Text.Trim(), "QHY461_lite", StringComparison.OrdinalIgnoreCase))
                {
                    insertIndex = i + 1;
                    break;
                }
                if (String.Equals(tabLVDS.TabPages[i].Text.Trim(), "TEST", StringComparison.OrdinalIgnoreCase))
                    insertIndex = i;
            }
            List<TabPage> trailingPages = new List<TabPage>();
            while (tabLVDS.TabPages.Count > insertIndex)
            {
                TabPage trailing = tabLVDS.TabPages[insertIndex];
                tabLVDS.TabPages.Remove(trailing);
                trailingPages.Add(trailing);
            }
            tabLVDS.TabPages.Add(page);
            foreach (TabPage trailing in trailingPages)
                tabLVDS.TabPages.Add(trailing);
        }

        private void Form1_TriggerBoardClosing(object sender, FormClosingEventArgs e)
        {
            if (triggerBoardControl != null)
                triggerBoardControl.Shutdown();
        }
        uint length;


        bool SendData(byte[] data)
        {
            if (ComDevice.IsOpen)
            {
                try
                {
                    //将消息传递给串口
                    ComDevice.Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "发送失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("串口未开启", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        void writeFPGA(ushort index, ushort value)
        {

            if (SDKAPI == false)
            {
                byte[] xdata = new byte[10];
                ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xb9, value, index, 1, xdata);
            }

            else
            {
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDWriteFPGA(camhandle, 0, (byte)index, (byte)value);
            }


        }

        byte readFPGA(ushort index)
        {
            //read one register's value from FPGA 
            byte[] xdata = new byte[10];
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xbc, 0, index, 1, xdata);

            return xdata[0];
        }


        void writeFPGA2(ushort index, ushort value)
        {

            if (SDKAPI == false)
            {

                byte[] xdata = new byte[10];
                ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xbd, value, index, 1, xdata);
            }
            else
            {
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDWriteFPGA(camhandle, 1, (byte)index, (byte)value);
            }


        }






        void LowLevelA0(byte mode, ushort xbin, ushort ybin, byte readmode)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa0;
            xdata[1] = mode;
            xdata[2] = MSB(xbin);
            xdata[3] = LSB(xbin);
            xdata[4] = MSB(ybin);
            xdata[5] = LSB(ybin);
            xdata[6] = readmode;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }

        void LowLevelA1(byte speed)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa1;
            xdata[1] = speed;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }


        void LowLevelA2(byte resmode, ushort roixsize, ushort roixstart, ushort roiysize, ushort roiystart)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa2;
            xdata[1] = resmode;

            xdata[2] = MSB(roixsize);
            xdata[3] = LSB(roixsize);

            xdata[4] = MSB(roixstart);
            xdata[5] = LSB(roixstart);

            xdata[6] = MSB(roiysize);
            xdata[7] = LSB(roiysize);

            xdata[8] = MSB(roiystart);
            xdata[9] = LSB(roiystart);

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }

        void LowLevelA3(uint exptime)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa3;
            xdata[1] = MSB3(exptime);
            xdata[2] = MSB2(exptime);
            xdata[3] = MSB1(exptime);
            xdata[4] = MSB0(exptime);

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }


        void LowLevelA4(ushort againR, ushort dgainR, ushort againG, ushort dgainG, ushort againB, ushort dgainB)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa4;
            xdata[1] = MSB(againR);
            xdata[2] = LSB(againR);
            xdata[3] = MSB(dgainR);
            xdata[4] = LSB(dgainR);
            xdata[5] = MSB(againG);
            xdata[6] = LSB(againG);
            xdata[7] = MSB(dgainG);
            xdata[8] = LSB(dgainG);
            xdata[9] = MSB(againB);
            xdata[10] = LSB(againB);
            xdata[11] = MSB(dgainB);
            xdata[12] = LSB(dgainB);


            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }


        void LowLevelA4_EX(ushort againR, ushort dgainR, ushort againG, ushort dgainG, ushort againB, ushort dgainB, ushort eGain, ushort HGCLGC)
        {
            byte[] xdata = new byte[32];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa4;
            xdata[1] = MSB(againR);
            xdata[2] = LSB(againR);
            xdata[3] = MSB(dgainR);
            xdata[4] = LSB(dgainR);
            xdata[5] = MSB(againG);
            xdata[6] = LSB(againG);
            xdata[7] = MSB(dgainG);
            xdata[8] = LSB(dgainG);
            xdata[9] = MSB(againB);
            xdata[10] = LSB(againB);
            xdata[11] = MSB(dgainB);
            xdata[12] = LSB(dgainB);

            xdata[13] = MSB(eGain);
            xdata[14] = LSB(eGain);
            xdata[15] = MSB(HGCLGC);
            xdata[16] = LSB(HGCLGC);


            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 32, xdata);
        }


        void LowLevelA5(byte usbtraffic)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa5;
            xdata[1] = usbtraffic;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }


        void LowLevelB5(byte watchdogen, byte sdkwatchen, byte feedog)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xB5;
            xdata[1] = watchdogen;
            xdata[2] = sdkwatchen;
            xdata[3] = feedog;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }

        void LowLevelAB(byte rbien)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xab;
            xdata[1] = rbien;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }

        void LowLevelA6(byte command)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa6;
            xdata[1] = command;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }

        void LowLevelA7(byte data)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa7;
            xdata[1] = data;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }

        void LowLevelA8(ushort offset1R, ushort offset1G, ushort offset1B, ushort offset2R, ushort offset2G, ushort offset2B)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa8;
            xdata[1] = MSB(offset1R);
            xdata[2] = LSB(offset1R);
            xdata[3] = MSB(offset1G);
            xdata[4] = LSB(offset1G);
            xdata[5] = MSB(offset1B);
            xdata[6] = LSB(offset1B);
            xdata[7] = MSB(offset2R);
            xdata[8] = LSB(offset2R);
            xdata[9] = MSB(offset2G);
            xdata[10] = LSB(offset2G);
            xdata[11] = MSB(offset2B);
            xdata[12] = LSB(offset2B);

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }

        void LowLevelA9(byte data)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xa9;
            xdata[1] = data;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }

        void LowLevelAC_QJ(byte buf1, byte buf2, byte buf3, byte buf4, byte buf5, byte buf6, byte buf7, byte buf8)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xac;
            xdata[1] = buf1; //trig_en
            xdata[2] = buf2;
            xdata[3] = buf3;
            xdata[4] = buf4;
            xdata[5] = buf5;
            xdata[6] = buf6;
            xdata[7] = buf7;
            xdata[8] = buf8;

            //            xdata[6] = buf6;


            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }


        void LowLevelAC(byte trig_en, byte trig_in_mode, UInt16 fiter_times, byte trig_in_source, byte trig_out_mode)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xac;
            xdata[1] = trig_en; //trig_en
            xdata[2] = trig_in_mode;
            xdata[4] = MSB(fiter_times);
            xdata[5] = LSB(fiter_times);
            xdata[6] = trig_in_source;
            xdata[7] = trig_out_mode;


            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }

        void LowLevelB2(byte bursten, byte burst_start, UInt16 burst_end, UInt16 hsync_stled, UInt16 hsync_edled, byte m6inline_leden, UInt16 m6inline_st, UInt16 m6inline_ed, byte test_mode)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xb2;
            xdata[1] = bursten;
            xdata[2] = burst_start;
            xdata[3] = LSB(burst_end);
            xdata[4] = MSB(burst_end);
            xdata[5] = LSB(hsync_stled);
            xdata[6] = MSB(hsync_stled);
            xdata[7] = LSB(hsync_edled);
            xdata[8] = MSB(hsync_edled);
            xdata[9] = m6inline_leden;
            xdata[10] = LSB(m6inline_st);
            xdata[11] = MSB(m6inline_st);
            xdata[12] = LSB(m6inline_ed);
            xdata[13] = MSB(m6inline_ed);
            xdata[14] = test_mode;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);

        }
        void LowLevelAD(byte bursten, byte burst_start, UInt16 burst_end, byte burstrbien, byte frame_cnt)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xad;
            xdata[1] = bursten;
            xdata[3] = burst_start;
            xdata[4] = MSB(burst_end);
            xdata[5] = LSB(burst_end);
            xdata[6] = burstrbien;
            xdata[7] = frame_cnt;


            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }


        void LowLevelAE(byte command)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xae;
            xdata[1] = command;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }


        void LowLevelAF(byte AutoControl, byte PWM, byte unit, ushort target_temp_c, ushort target_temp_adu)
        {
            byte[] xdata = new byte[16];
            ushort value = 0x00;
            ushort index = 0x00;

            xdata[0] = 0xaf;
            xdata[1] = AutoControl;
            xdata[2] = PWM;
            xdata[3] = unit;
            xdata[4] = MSB(target_temp_c);
            xdata[5] = LSB(target_temp_c);
            xdata[6] = MSB(target_temp_adu);
            xdata[7] = LSB(target_temp_adu);

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 16, xdata);
        }


        void LowLevelAD()
        {

            ushort gain = 8;
            ushort offset = 0X3C99 + 10;

            byte[] xdata = new byte[64];
            ushort value = 0x00;
            ushort index = 0x00;


            byte bits215_208;
            byte bits207_200;
            byte bits167_160;
            byte bits159_152;

            /*gain = 0~7*/



            ushort temp;
            temp = (ushort)(0xc03a | (gain << 10) | (gain << 7));

            bits215_208 = MSB(temp);
            bits207_200 = LSB(temp);


            /*offset = 0~16383*/
            bits167_160 = MSB((ushort)(offset & 0x3fff));
            bits159_152 = LSB((ushort)(offset & 0x3fff));

            xdata[0] = 0xad;
            xdata[32] = 0x00;
            xdata[31] = 0x00;
            xdata[30] = 0x00;
            xdata[29] = 0x00;
            xdata[28] = 0x00;
            xdata[27] = 0x00;
            xdata[26] = 0x00;
            xdata[25] = 0x00;
            xdata[24] = 0x00;
            xdata[23] = 0x00;
            xdata[22] = 0x00;
            xdata[21] = 0x00;
            xdata[20] = 0x00;
            xdata[19] = 0x80;
            xdata[18] = 0x76;
            xdata[17] = 0x75;
            xdata[16] = 0x1c;
            xdata[15] = 0x00;
            xdata[14] = 0x00;
            xdata[13] = bits159_152;
            xdata[12] = bits167_160;
            xdata[11] = 0x38;
            xdata[10] = 0xa6;
            xdata[9] = 0x66;
            xdata[8] = 0x6d;
            xdata[7] = bits207_200;
            xdata[6] = 223;//bits215_208;   
            xdata[5] = 0x24;
            xdata[4] = 0xcb;
            xdata[3] = 0x77;
            xdata[2] = 0x15;
            xdata[1] = 0x05;




            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd1, value, index, 64, xdata);


        }


        void LowLevelD6(ushort command, byte[] xdata)
        {

            ushort value = 0x00;
            ushort index = command;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd6, value, index, 64, xdata);
        }



        void setTrainTo98E0()
        {

            textBoxBitButtonB.Text = "001B";
            textBoxBitButtonA.Text = "001C";

            buttonbit15.Text = "1";
            buttonbit14.Text = "1";
            buttonbit13.Text = "1";
            buttonbit12.Text = "0";
            buttonbit11.Text = "0";
            buttonbit10.Text = "1";
            buttonbit9.Text = "1";
            buttonbit8.Text = "0";

            buttonbit7.Text = "0";
            buttonbit6.Text = "0";
            buttonbit5.Text = "1";
            buttonbit4.Text = "1";
            buttonbit3.Text = "1";
            buttonbit2.Text = "0";
            buttonbit1.Text = "1";
            buttonbit0.Text = "1";







        }

        void writeCMOS(ushort index, ushort value)
        {


            // if( SDKAPI == false )
            //  {

            byte[] xdata = new byte[64];
            xdata[0] = (byte)value;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xb8, 0x00, index, 1, xdata);
            // }

            // else
            // {
            //     ASCOM.QHYCCD.libqhyccd.SetQHYCCDWriteCMOS( camhandle, 0, index, value );
            // }

        }

        byte readCmos(ushort index)
        {
            //read one register's value from FPGA 
            byte[] xdata = new byte[10];
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xb7, 0, index, 1, xdata);

            return xdata[0];
        }




        void writeCMOS_QHY367C(ushort index, ushort value)
        {


            byte[] xdata = new byte[64];
            xdata[0] = (byte)value;


            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xb8, 0x02, index, 1, xdata);

        }


        byte MSB(ushort i)
        {
            return (byte)((i & ~0x00ff) / 256);
        }

        byte LSB(ushort i)
        {
            return (byte)(i & ~0xff00);
        }

        byte MSB3(UInt32 i)
        {
            return (byte)((i & ~0x00ffffff) / 256 / 256 / 256);
        }

        byte MSB2(UInt32 i)
        {
            return (byte)((i & ~0xff00ffff) / 256 / 256);
        }

        byte MSB1(UInt32 i)
        {
            return (byte)((i & ~0xffff00ff) / 256);
        }

        byte MSB0(UInt32 i)
        {
            return (byte)(i & ~0xffffff00);
        }




        void enableDDR(bool i)
        {
            if (i == true)
                writeFPGA(30, 1);
            else
                writeFPGA(30, 0);
        }

        void resetCMOS()
        {


            if (sensorModel == 585 || sensorModel == 2110 || sensorModel == 715 || sensorModel == 678 || sensorModel==132
                ||sensorModel==568)
            {
                writeFPGA(0, 0);
                Thread.Sleep(10);
                writeFPGA(0, 1);
            }
            else if (sensorModel == 6060)
            {
                writeFPGA2(8, 0);
                writeFPGA2(8, 1);

            }
            else
            {
                writeFPGA(0, 1);
                Thread.Sleep(100);
                writeFPGA(0, 0);
            }

            Thread.Sleep(10);

        }

        void setIDLE()
        {
            writeFPGA(35, 0);
        }

        void releaseIDLE()
        {
            writeFPGA(35, 1);
        }
        void rollingsetIDLE(){
             writeFPGA(213,0);
        }

        void rollingreleaseIDLE(){
             writeFPGA(213,1);
        }

        void AMPV_ON()
        {
            writeFPGA(8, 1);
        }

        void AMPV_OFF()
        {
            writeFPGA(8, 0);
        }

        void setAMPVStart(UInt32 value)
        {
            writeFPGA(16, MSB3(value));
            writeFPGA(17, MSB2(value));
            writeFPGA(14, MSB1(value));
            writeFPGA(15, MSB0(value));
        }

        void setAMPVEnd(UInt32 value)
        {
            writeFPGA(12, MSB3(value));
            writeFPGA(13, MSB2(value));
            writeFPGA(9, MSB1(value));
            writeFPGA(10, MSB0(value));
        }

        void BIT8()
        {
            writeFPGA(3, 0);
        }

        void BIT16()
        {
            writeFPGA(3, 1);
        }

        void setHCOUNT1(ushort i)
        {
            writeCMOS(0x3084, LSB(i));
            writeCMOS(0x3085, MSB(i));
        }

        void setHCOUNT2(ushort i)
        {
            writeCMOS(0x3086, LSB(i));
            writeCMOS(0X3087, MSB(i));
        }

        void setVMAX(UInt32 i)
        {
            writeFPGA(22, MSB3(i));
            writeFPGA(23, MSB2(i));
            writeFPGA(24, MSB1(i));
            writeFPGA(25, MSB0(i));
        }
        void SetVmaxMax10(UInt32 i)
        {
            writeFPGA2(14, MSB3(i));
            writeFPGA2(13, MSB2(i));
            writeFPGA2(12, MSB1(i));
            writeFPGA2(11, MSB0(i));

        }


        void setHMAX(ushort i)
        {

            writeFPGA(26, MSB3(i));
            writeFPGA(27, MSB2(i));
            writeFPGA(28, MSB1(i));
            writeFPGA(29, MSB0(i));
        }


        void setSHR(UInt32 i)
        {
           // setIDLE();
            writeFPGA2(15, MSB0(i));
            writeFPGA2(16, MSB1(i));
            writeFPGA2(17, MSB2(i));
            writeFPGA2(18, MSB3(i));
           // releaseIDLE();
        }

        void setSVR(ushort i)
        {
            writeCMOS(0X300F, MSB(i));
            writeCMOS(0X300E, LSB(i));
        }

        //H ROI

        void setCropH_ENABLE(bool i)
        {
            if (i == true)
                writeCMOS(0x3034, 1);
            else
                writeCMOS(0x3034, 0);
        }

        void setHSTART(ushort i)
        {
            writeCMOS(0X3037, MSB(i));
            writeCMOS(0X3036, LSB(i));
        }

        void setHEND(ushort i)
        {
            writeCMOS(0x3039, MSB(i));
            writeCMOS(0x3038, LSB(i));
        }

        //V ROI

        void setWriteVSize(ushort i)
        {
            writeCMOS(0X3131, MSB(i));
            writeCMOS(0X3130, LSB(i));
        }

        void setYoutSize(ushort i)
        {
            writeCMOS(0x3133, MSB(i));
            writeCMOS(0x3132, LSB(i));
        }

        void setOpitcBlackSizeV(ushort i)
        {
            writeCMOS(0X312F, LSB(i));
        }




        void setQHY600CropV_ENABLE(bool i)
        {
            if (i == true)
                writeCMOS(0x0005, 1);
            else
                writeCMOS(0x0005, 0);
        }


        void setQHY600CropV_START(ushort i)
        {



            writeCMOS(0x0007, MSB(i));
            writeCMOS(0x0006, LSB(i));
        }

        void setQHY600CropV_SIZE(ushort i)
        {
            writeCMOS(0x0009, MSB(i));
            writeCMOS(0x0008, LSB(i));
        }


        void setQHY268CropV_ENABLE(bool i)
        {
            if (i == true)
                writeCMOS(0x0007, 1);
            else
                writeCMOS(0x0007, 0);
        }


        void setQHY410CropV_ENABLE(bool i)
        {
            if (i == true)
                writeCMOS(0x0003, 1);
            else
                writeCMOS(0x0003, 0);
        }

        void setQHY268CropV_START(ushort i)
        {
            writeCMOS(0x0009, MSB(i));
            writeCMOS(0x0008, LSB(i));
        }

        void setQHY410CropV_START(ushort i)
        {
            writeCMOS(0x0023, MSB(i));
            writeCMOS(0x0022, LSB(i));
        }
        void setQHY485CropV_START(ushort i)
        {
            writeCMOS(0x3045, MSB(i));
            writeCMOS(0x3044, LSB(i));
        }
        void setQHY492CropV_START(ushort i)
        {
            writeCMOS(0x31E1, MSB(i));
            writeCMOS(0x31E0, LSB(i));
        }
        void setQHY487CropV_START(ushort i)
        {
            writeCMOS(0x0323, MSB(i));
            writeCMOS(0x0322, LSB(i));
        }

        //

        void setQHY268CropV_SIZE(ushort i)
        {
            writeCMOS(0x000B, MSB(i));
            writeCMOS(0x000A, LSB(i));
        }

        void setQHY410CropV_SIZE(ushort i)
        {
            writeCMOS(0x0025, MSB(i));
            writeCMOS(0x0024, LSB(i));
        }
        void setQHY485CropV_SIZE(ushort i)
        {
            writeCMOS(0x3047, MSB(i));
            writeCMOS(0x3046, LSB(i));
        }


        void setQHY492CropV_SIZE(ushort i)
        {
            writeCMOS(0x3133, MSB(i));
            writeCMOS(0x3132, LSB(i));
        }

        void setQHY487CropV_SIZE(ushort i)
        {
            writeCMOS(0x0327, MSB(i));
            writeCMOS(0x0326, LSB(i));
        }
        //image adjustment

        void setAnalogGain(ushort i)
        {
            writeCMOS(0x300B, MSB(i));
            writeCMOS(0x300A, LSB(i));
        }

        void setDigitalGain(ushort i)
        {
            writeCMOS(0x3012, LSB(i));
        }

        void setOffset(ushort i)
        {
            writeCMOS(0x3042, LSB(i));
        }

        void setHGC(bool i)
        {
            if (i == true)
                writeCMOS(0X3092, 1);
            else
                writeCMOS(0x3092, 0);
        }

        void setDelay(byte i)
        {
            writeFPGA(91, i);
            Thread.Sleep(100);
            writeFPGA(92, 0);
            Thread.Sleep(100);
            writeFPGA(92, 1);
            Thread.Sleep(100);
            writeFPGA(92, 0);
        }


        void setDelay(byte channel, byte i)
        {
            writeFPGA(88, channel);
            Thread.Sleep(10);
            writeFPGA(91, i);
            Thread.Sleep(10);
            writeFPGA(92, 0);
            Thread.Sleep(10);
            writeFPGA(92, 1);
            Thread.Sleep(10);
            writeFPGA(92, 0);
        }



        void setOFFSET_TOP(int i)
        {
            //int offset;
            int temp;
            int temp2;



            byte reg25, reg26, reg27;

            //offset is combine with REG03 BIT0   REG04 BIT 7..0   REG05 BIT 7..1   TOTAL 16BIT. 
            //since REG03 BIT 0 is should be always be 1,  reg04 7,6,5 is always be 111   reg05 bit0 is always 1

            //the original input range is 0-511. But after test , the range from 470-511 is good.
            i = i + 470;             //convert . 470-511 ->  0-41   input range 0-41

            if (i > 511) i = 511;   //input range is 0-31




            temp = i & ~0xfe3f;                   // 1111 1110 0011 1111   //get the top 3 bit of last 9bit
            temp = temp >> 6;       //right shift 7bit 
            temp = temp + 0x08;           //0000 1xxx
            reg25 = (byte)temp;


            temp = i & ~0xffc1;            //1111 1111 1100 0001  //get  5 bit of last 9bit
            temp = temp << 2;


            temp2 = i & ~0xfffe;  //1111 1111 1111 1110 //get last bit
            temp = temp + 0x04 + temp2;

            reg26 = (byte)temp;             //xxxx x10x;


            reg27 = 0xf4;       //fixed the reg27 
            writeCMOS(25, reg25);
            writeCMOS(26, reg26);
            writeCMOS(27, reg27);
            //writeCMOS(5, reg05);
        }



        void setPreamp(int i)
        {
            ushort preamp_gain = 0;


            if (sensorModel == 455 || sensorModel == 571 || sensorModel == 411 || sensorModel == 461)
            {
                if (i <= 7)
                {
                    if (i == 0)
                        preamp_gain = 0;
                    else if (i == 1)
                        preamp_gain = 0x0011;
                    else if (i == 2)
                        preamp_gain = 0x0044;
                    else if (i == 3)
                        preamp_gain = 0x0022;
                    else if (i == 4)
                        preamp_gain = 0x0055;
                    else if (i == 5)
                        preamp_gain = 0x0033;
                    else if (i == 6)
                        preamp_gain = 0x0066;
                    else if (i == 7)
                        preamp_gain = 0x0077;

                    //preamp_gain = (ushort)(i *16 + i);
                    writeCMOS(0x067f, preamp_gain);
                }
            }
        }

        void setOFFSET_BUTTOM(int i)
        {

            byte reg29, reg30, reg31;

            //int offset;
            int temp;
            int temp2;
            //reg13 bit4-0  is the highest offset bit 
            //reg14 bit7-0  
            //reg15 bit7-5


            //input range 0-31

            //一共是5位
            //取低四位，放到REG14的高四位，
            //取第五位，放到REG13的最低位。

            i = i + 470;             //convert . 470-511 ->  0-41   input range 0-41

            if (i > 511)
                i = 511;   //input range is 0-31


            temp = i & ~0xfe0f;                   // 1111 1110 0000 1111   //get the top 5 bit of last 9bit
            temp = temp >> 4;
            temp = temp + 0x00;// 000xxxxx;
            reg29 = (byte)temp;


            temp = i & ~0xfff1;                   // 1111 1111 1111  0001   //get the top 5 bit of last 9bit
            temp = temp << 4;

            temp2 = i & ~0xfffe;  //1111 1111 1111 1110 //get last bit
            temp2 = temp2 << 2;
            temp = temp + 0x13 + temp2;          //xxx10x11

            reg30 = (byte)temp;

            reg31 = 0x2f;





            writeCMOS(29, reg29);
            writeCMOS(30, reg30);
            writeCMOS(31, reg31);
            //writeCMOS(5, reg05);
        }



        void setPGAGain_TOP(int i)
        {
            byte reg12, reg13;
            int temp;
            //range 0-63, 6bit   bit 0 is a swith for gain from 0.4-6.6x  / 1.0-16.5x  .After testing the gain 0.4-1.0 will cause it can not get full range of 0-4095. So we will use 
            //so that bit0=0 should be alwasy be  0 

            i = i + 2;  //add 2 for get full range of adc

            if (i > 31)
                i = 31;




            temp = i & ~0xffef;    //b1111 1111 1110 1111                  ///get bit 5
            temp = temp >> 4;   //right shift 4bit
            temp = temp + 0xf2;                   //high 7bit is 1111001x (f2)

            reg12 = (byte)temp;



            temp = i & ~0xfff0;    //b1111 1111 1111 0000                  ///get bit 4
            temp = temp << 4;     //left shift 4bit
            temp = temp + 0x06;

            reg13 = (byte)temp;



            writeCMOS(12, reg12);
            Thread.Sleep(50);
            writeCMOS(13, reg13);
            Thread.Sleep(50);

        }

        void setPGAGain_BUTTOM(int i)
        {
            byte reg37, reg38;

            int temp;

            i = i + 2;

            if (i > 31)
                i = 31;

            temp = i & ~0xffe1;            //b 1111 1111 1110 0001         get bit [4:1] 
            temp = temp >> 1;            //right shift 1 bit
            temp = temp + 0x90;             // 1001 xxxx

            reg37 = (byte)temp;

            temp = i & ~0xfffffe;                     //b 1111 1111 1111 1110   get bit[0]

            temp = temp << 7; //right shift 3bit
            temp = temp + 0x4f;         //x100 1111  0x4f
            reg38 = (byte)temp;


            writeCMOS(37, reg37);
            writeCMOS(38, reg38);


        }

        void setADCGain(int i)
        {
            byte reg05;
            int temp;

            if (i > 63)
                i = 63;

            temp = i & ~0xffc0;          //b 1111 1111 1100 0000
            temp = temp << 1;   //left shift 1 bit
            temp = temp + 0x80;  //1xxxxxx0
            reg05 = (byte)temp;

            writeCMOS(5, reg05);


        }
        string xid;
        //Form1_Load
        private void Connection_Click(object sender, EventArgs e)
        {
            ASCOM.QHYCCD.libqhyccd.CloseQHYCCD(camhandle);
            //释放资源
            //release resource
            ret = ASCOM.QHYCCD.libqhyccd.ReleaseQHYCCDResource();
            //将连接状态的值改为0
            //The connection state to change the value of 0
            isConnect = 0;
            //Connection.Text = "SCAN";

            richTextBox1.Clear();
           

            comboBox4.Items.Clear();
            comboBox4.Text = "请选择设备";
            this.Text = " QHYCCD CAMERA TOOLS  USB  V260407    ";

            //如果目前相机没有连接，执行以下方法连接相机
            //If the camera is not connected, perform the following method to connect camera
            //if (isConnect == 0)
            //{
            //初始化相机资源
            //InitQHYCCDResource
            ASCOM.QHYCCD.libqhyccd.InitQHYCCDResource();
            //获得相机链接数
            //Gain is connected the camera 
            byte totalCamera = 0;
            totalCamera = (byte)(Convert.ToInt32(ASCOM.QHYCCD.libqhyccd.ScanQHYCCD()));
            //给相机设置一个ID
            //set the camera's id
            // StringBuilder id = new StringBuilder( 0 );
            richTextBox1.AppendText("totalCamera ：" + totalCamera.ToString("D") + Environment.NewLine);

            // byte[]id = new byte[100];
            // byte[][] id = new byte[3][];
            string idStr0 = "";
            for (byte i = 0; i < totalCamera; i++)
            {
                id[i] = new byte[1000];
                //ASCOM.QHYCCD.libqhyccd.GetQHYCCDId(i, id[i]);
                if (ASCOM.QHYCCD.libqhyccd.GetQHYCCDId(i, id[i]) == 0)
                {
                    //idStr = id[i].ToString();
                    idStr0 = Encoding.ASCII.GetString(id[i]);
                    comboBox4.Items.Add(idStr0.ToString());

                }
            }

            isConnect = 0;
            Connection.Text = "ReScan Camera";

            richTextBox1.Clear();



            ////如果目前相机没有连接，执行以下方法连接相机
            ////If the camera is not connected, perform the following method to connect camera
            //if (isConnect == 0)
            //{
            //    //初始化相机资源
            //    //InitQHYCCDResource
            //    ASCOM.QHYCCD.libqhyccd.InitQHYCCDResource();
            //    //获得相机链接数
            //    //Gain is connected the camera 
            //    Int32 totalCamera = 0;
            //    totalCamera = Convert.ToInt32(ASCOM.QHYCCD.libqhyccd.ScanQHYCCD());
            //    label60.Text = totalCamera.ToString();

            //    //给相机设置一个ID
            //    //set the camera's id
            //    byte[] id = new byte[100];
            //    ASCOM.QHYCCD.libqhyccd.GetQHYCCDId(Convert.ToInt32(textBox15.Text), id);
            //    //根据ID打开相机
            //    //open the camera depend on ID
            //    camhandle = ASCOM.QHYCCD.libqhyccd.OpenQHYCCD(id);
            //    //根据ID赋给相机一个handle
            //    //According to a handle ID is assigned to the camera
            //    ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(camhandle, 0);
            //    //初始化相机
            //    //Init camera
            //    // ASCOM.QHYCCD.libqhyccd.InitQHYCCD(camhandle);
            //    //button3.Text = ret.ToString();
            //    //获取相机的碎片信息
            //    //Camera fragments of information
            //    //            ASCOM.QHYCCD.libqhyccd.GetQHYCCDChipInfo(camhandle, ref chipw, ref chiph, ref x, ref h, ref pixelw, ref pixelh, ref bpp);
            //    //设置相机的bin
            //    //set bin mode
            //    //            ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(camhandle, 1, 1);
            //    //设置相机分辨率
            //    //set resolution
            //    //            ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(camhandle, 0, 0, x, h);
            //    //获取照片所占用的空间大小
            //    //To get photos occupied space size
            //    //            length = ASCOM.QHYCCD.libqhyccd.GetQHYCCDMemLength(camhandle);
            //    //将照片所占用的空间大小放入byte数组中
            //    //Put pictures occupied space in a byte array
            //    //            rawArray = new byte[length];


            //    xid = Encoding.ASCII.GetString(id);
            //    ss.Text = xid;


            //    //弹出一个提示框，提示连接成功
            //    //Bring up a prompt box, suggesting the connection is successful
            //    // DialogResult dr = MessageBox.Show("connect success");
            //    //将是否连接值改为1，表示已经连接
            //    //Connect whether value is changed to 1, says it has connections
            //    isConnect = 1;
            //}
            //else
            //{
            //    //如果已经连接相机，再次点击，弹出提示框
            //    //If the camera is connected, click again, the pop-up prompts
            //    DialogResult dr = MessageBox.Show("has connected camare");
            //}
        }

        private void DisConnection_Click(object sender, EventArgs e)
        {
            //如果有相机连接，执行以下方法
            //If there is a camera connection, perform the following method
            if (isConnect == 1)
            {
                //关闭相机
                //close camera
                ASCOM.QHYCCD.libqhyccd.CloseQHYCCD(camhandle);
                //释放资源
                //release resource
                ret = ASCOM.QHYCCD.libqhyccd.ReleaseQHYCCDResource();
                //将连接状态的值改为0
                //The connection state to change the value of 0
                isConnect = 0;
                Connection.Text = "Connection";
                if (ret == 0)
                {
                    //关闭成功，弹出提示框
                    //Close the success, pop-up prompts
                    DialogResult dr = MessageBox.Show("success");
                }

            }
            else
            {   //如果目前没有相机连接，弹出提示框
                //If there is no camera connection, pop-up prompts
                DialogResult dr = MessageBox.Show("no camera connection");
            }
        }

        private void setting_Click(object sender, EventArgs e)
        {

        }


        //设置相机参数，这里我只设置了曝光、gain、offset、usbTraffic,你可以根据你的实际情况增删参数
        //set camera param,I just set the exposure, gain, offset, usbTraffic, you can add or delete parameters according to your actual condition
        public static uint setCameraParam(IntPtr handle, double exposure_times, double gain_num, double offSet_num, double usbTraffic_num)
        {
            //设置参数成功的返回值为0
            //Set parameters successful return value is zero
            uint ret = 0;
            if (ret == 0)
            {
                ret = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(camhandle, CONTROL_ID.CONTROL_EXPOSURE, exposure_times); //exposure
            }
            else
            {
                DialogResult dr = MessageBox.Show("failure set exposure");
            }
            if (ret == 0)
            {
                ret = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(camhandle, CONTROL_ID.CONTROL_GAIN, gain_num); //Gain
            }
            else
            {
                DialogResult dr = MessageBox.Show("failure set gain");
            }
            if (ret == 0)
            {
                ret = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(camhandle, CONTROL_ID.CONTROL_OFFSET, offSet_num);//offset
            }
            else
            {
                DialogResult dr = MessageBox.Show("failure set offset");
            }
            if (ret == 0)
            {
                ret = ASCOM.QHYCCD.libqhyccd.SetQHYCCDParam(camhandle, CONTROL_ID.CONTROL_USBTRAFFIC, usbTraffic_num);//usbTraffic

            }
            else
            {
                DialogResult dr = MessageBox.Show("failure set usbTraffic");
            }

            return 0;


        }
        private void single_Click(object sender, EventArgs e)
        {

            //设置相机的图片位数
            //set camare bits mode
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDBitsMode(camhandle, 16);
            uint ret = 1;
            //    ASCOM.QHYCCD.libqhyccd.InitQHYCCD(camhandle);
            //开启曝光
            //exposure
            ASCOM.QHYCCD.libqhyccd.ExpQHYCCDSingleFrame(camhandle);
            //debayer
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(camhandle, true);
            rawArray = new byte[length * 3];
            //获取照片的信息
            //Obtain information on the photos
            while (ret != 0)
            {
                ret = ASCOM.QHYCCD.libqhyccd.C_GetQHYCCDSingleFrame(camhandle, ref x, ref h, ref bpp, ref c, rawArray);
            }
            //显示图片 内存法  
            if (ret == 0)
            {
                bitmap = new Bitmap((int)x, (int)h);
                rectangle = new Rectangle(0, 0, (int)x, (int)h);
                bmpData = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                ptr = bmpData.Scan0;

                s = 0;
                index = 0;
                pixData = 0;
                rgbArray = new Byte[x * h * 4];
                for (int i = 0; i < h; i++)
                {
                    for (int y = 0; y < x; y++)
                    {
                        rgbArray[s] = rawArray[index + 1];
                        rgbArray[s + 1] = rawArray[index + 3];
                        rgbArray[s + 2] = rawArray[index + 5];
                        rgbArray[s + 3] = 255;

                        s += 4;
                        index += 6;
                    }
                }

                Marshal.Copy(rgbArray, 0, ptr, (int)(x * h * 4));

                bitmap.UnlockBits(bmpData);

                //pictureBox1.Image = bitmap;
            }


        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //设置相机的图片位数
            //set camare bits mode
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDBitsMode(camhandle, 8);
            uint ret = 1;
            ASCOM.QHYCCD.libqhyccd.InitQHYCCD(camhandle);
            //开启曝光
            //exposure
            ASCOM.QHYCCD.libqhyccd.ExpQHYCCDSingleFrame(camhandle);

            ASCOM.QHYCCD.libqhyccd.SetQHYCCDDebayerOnOff(camhandle, true);

            rawArray = new byte[length * 3];

            //获取照片的信息
            //Obtain information on the photos
            while (ret != 0)
            {
                ret = ASCOM.QHYCCD.libqhyccd.C_GetQHYCCDSingleFrame(camhandle, ref x, ref h, ref bpp, ref c, rawArray);
            }

            //显示图片 内存法  
            if (ret == 0)
            {
                bitmap = new Bitmap((int)x, (int)h);
                rectangle = new Rectangle(0, 0, (int)x, (int)h);
                bmpData = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                ptr = bmpData.Scan0;

                s = 0;
                index = 0;
                pixData = 0;
                rgbArray = new Byte[x * h * 4];
                for (int i = 0; i < h; i++)
                {
                    for (int y = 0; y < x; y++)
                    {
                        //blue
                        rgbArray[s] = rawArray[index + 1];
                        //Green
                        rgbArray[s + 1] = rawArray[index + 3];
                        //red
                        rgbArray[s + 2] = rawArray[index + 5];
                        rgbArray[s + 3] = 255;

                        s += 4;
                        index += 6;
                    }
                }

                Marshal.Copy(rgbArray, 0, ptr, (int)(x * h * 4));

                bitmap.UnlockBits(bmpData);

                //pictureBox1.Image = bitmap;


            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        void setQHY600OFFSET(ushort value)
        {
            writeCMOS(0x41, MSB(value));
            writeCMOS(0x40, LSB(value));
            writeCMOS(0X43, MSB(value));
            writeCMOS(0x42, LSB(value));

        }

        void setQHY268OFFSET(ushort value)
        {
            writeCMOS(0x43, MSB(value));
            writeCMOS(0x42, LSB(value));
            writeCMOS(0X45, MSB(value));
            writeCMOS(0x44, LSB(value));

        }

        void setQHY410OFFSET(ushort value)
        {
            writeCMOS(0x5D, MSB(value));
            writeCMOS(0x5C, LSB(value));

        }

        void setQHY485OFFSET(ushort value)
        {
            writeCMOS(0x30DD, MSB(value));
            writeCMOS(0x30DC, LSB(value));

        }

        void setQHY492OFFSET(ushort value)
        {
            writeCMOS(0x3043, MSB(value));
            writeCMOS(0x3042, LSB(value));

        }

        void setSC2210OFFSET(ushort value)
        {
            writeCMOS(0x3907, MSB(value));
            writeCMOS(0x3908, LSB(value));
        }
        void setQHY487OFFSET(ushort value)
        {
            if (value > 4095)
                value = 4095;

            writeCMOS(0x07b5, MSB(value));
            writeCMOS(0x07b4, LSB(value));
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            label5.Text = "OFFSET:" + hScrollBar1.Value.ToString();
            if (sensorModel == 455)
                setQHY600OFFSET((ushort)hScrollBar1.Value);
            if (sensorModel == 571)
                setQHY268OFFSET((ushort)hScrollBar1.Value);
            if (sensorModel == 410)
                setQHY410OFFSET((ushort)hScrollBar1.Value);
            if (sensorModel == 485)
                setQHY485OFFSET((ushort)hScrollBar1.Value);
            if (sensorModel == 492)
                setQHY492OFFSET((ushort)hScrollBar1.Value);
            if (sensorModel == 2110)//sc2210
                setSC2210OFFSET((ushort)hScrollBar1.Value);
            if (sensorModel == 487)
                setQHY487OFFSET((ushort)hScrollBar1.Value);
        }


        void setQHY600AnalogGain(ushort value)
        {
            writeCMOS(0X2F, MSB(value));
            writeCMOS(0x2E, LSB(value));
            writeCMOS(0x31, MSB(value));
            writeCMOS(0x30, LSB(value));
        }

        void setQHY268AnalogGain(ushort value)
        {
            writeCMOS(0X31, MSB(value));
            writeCMOS(0x30, LSB(value));
            writeCMOS(0x33, MSB(value));
            writeCMOS(0x32, LSB(value));
        }

        void setQHY410AnalogGain(ushort value)
        {
            writeCMOS(0X3E, MSB(value));
            writeCMOS(0x3D, LSB(value));
            writeCMOS(0x40, MSB(value));
            writeCMOS(0x3F, LSB(value));
        }
        void setQHY485AnalogGain(ushort value)
        {

            if (value > 2047)
                value = 2047;

            writeCMOS(0X3085, MSB(value));
            writeCMOS(0x3084, LSB(value));

        }
        void setQHY492AnalogGain(ushort value)
        {

            if (value > 2047)
                value = 2047;

            writeCMOS(0X300B, MSB(value));
            writeCMOS(0x300A, LSB(value));

        }
        void setQHY487AnalogGain(ushort value)
        {

            if (value > 480)
                value = 480;

            writeCMOS(0X0715, MSB(value));
            writeCMOS(0x0714, LSB(value));

        }





        private void hScrollBar2_Scroll(object sender, ScrollEventArgs e)
        {
            label6.Text = hScrollBar2.Value.ToString();

            if (sensorModel == 455)
                setQHY600AnalogGain((ushort)hScrollBar2.Value);
            if (sensorModel == 571)
                setQHY268AnalogGain((ushort)hScrollBar2.Value);
            if (sensorModel == 410)
                setQHY410AnalogGain((ushort)hScrollBar2.Value);
            if (sensorModel == 485)
                setQHY485AnalogGain((ushort)hScrollBar2.Value);
            if (sensorModel == 492)
                setQHY492AnalogGain((ushort)hScrollBar2.Value);
            if (sensorModel == 487)
                setQHY487AnalogGain((ushort)hScrollBar2.Value);
            else
            {
                LowLevelA4_EX((ushort)hScrollBar2.Value, 8, (ushort)hScrollBar2.Value, 8, (ushort)hScrollBar2.Value, 8, 0, 0);
            }
        }






        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // label7.Text = hScrollBar3.Value.ToString();


            //  byte[] xdata = new byte[10];
            // byte addr;
            // addr = (byte)(textBox1.Value & ~0XFF00); 
            //  xdata[0] = (byte)(hScrollBar3.Value);

            //  label7.Text = label7.Text + " " + xdata[0].ToString();


            //  ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, addr, 0x0000, 0x3012, 1, xdata);
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }


        private void hScrollBar4_Scroll(object sender, ScrollEventArgs e)
        {
            label8.Text = hScrollBar4.Value.ToString();

            ushort index, value;
            if (radioButtonHEX.Checked == true)
                index = Convert.ToUInt16(textBox1.Text, 16);
            else
                index = Convert.ToUInt16(textBox1.Text, 10);

            value = (ushort)hScrollBar4.Value;

            writeCMOS(index, value);

            //textBox1
            //  byte addr;
            // addr = Convert.ToInt32("textBox1", 16);
            // byte[] xdata = new byte[10];
            // xdata[0] = (byte)(hScrollBar4.Value & ~0XFF00);

            // addr = (byte )(textBox1);

            //   label8.Text = label8.Text + " " + xdata[0].ToString();


            //   ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, addr, 0x0000, 0x302c, 1, xdata);

        }



        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void hScrollBar8_Scroll(object sender, ScrollEventArgs e)
        {


            ushort hmax;
            hmax = (ushort)hScrollBar8.Value;





            //for master mode of 492
            if (sensorModel == 492)
            {
                writeCMOS(0x30ac, LSB(hmax));
                writeCMOS(0x30ad, MSB(hmax));
            }

            else if (sensorModel == 485)
            {
                writeCMOS(0x3028, LSB(hmax));
                writeCMOS(0x3029, MSB(hmax));

            }

            else
            {
                setIDLE();
                setHMAX(hmax);
                releaseIDLE();
            }




            label9.Text = "HMAX:" + hScrollBar8.Value.ToString();




        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void hScrollBar5_Scroll(object sender, ScrollEventArgs e)
        {
            label16.Text = "VMAX:" + hScrollBar5.Value.ToString();
            UInt32 vmax;
            vmax = (UInt32)hScrollBar5.Value;







            //for master mode of 492
            if (sensorModel == 492)
            {
                writeCMOS(0x30ab, MSB2(vmax));
                writeCMOS(0x30aa, MSB1(vmax));
                writeCMOS(0x30a9, MSB0(vmax));
            }

            else if (sensorModel == 485)
            {
                writeCMOS(0x3026, MSB2(vmax));
                writeCMOS(0x3025, MSB1(vmax));
                writeCMOS(0x3024, MSB0(vmax));

                Thread.Sleep(50);
            }

            else
            {
                //slave mode cameras
                setIDLE();
                setVMAX(vmax);
                releaseIDLE();
            }

        }

        private void hScrollBar9_Scroll(object sender, ScrollEventArgs e)
        {


        }

        private void hScrollBar10_Scroll(object sender, ScrollEventArgs e)
        {

        }

        void setQHY600SHR(ushort value)
        {
            writeCMOS(0X17, MSB(value));
            writeCMOS(0X16, LSB(value));
        }

        void setQHY268SHR(ushort value)
        {
            writeCMOS(0X19, MSB(value));
            writeCMOS(0X18, LSB(value));
        }


        void setQHY410SHR(ushort value)
        {
            writeCMOS(0X06, MSB(value));
            writeCMOS(0X05, LSB(value));
        }

        void setQHY485SHR(ushort value)
        {
            writeCMOS(0X3052, MSB2(value));
            writeCMOS(0X3051, MSB1(value));
            writeCMOS(0X3050, MSB0(value));
        }

        void setQHY492SHR(ushort value)
        {
            writeCMOS(0X302D, MSB(value));
            writeCMOS(0X302C, LSB(value));

        }
        void setQHY5III568SHR(ushort value)
        {
            writeCMOS(0X3242, 0x00);
            writeCMOS(0X3241, MSB(value));
            writeCMOS(0X3240, LSB(value));
        }
        void setQHY487SHR(ushort value)
        {
            writeCMOS(0X0442, MSB2(value));
            writeCMOS(0X0441, MSB1(value));
            writeCMOS(0X0440, MSB0(value));
        }

        private void hScrollBar11_Scroll(object sender, ScrollEventArgs e)
        {
            label18.Text = "SHR=" + hScrollBar11.Value.ToString();
            if (sensorModel == 455)
                setQHY600SHR((ushort)hScrollBar11.Value);
            if (sensorModel == 571)
                setQHY268SHR((ushort)hScrollBar11.Value);
            if (sensorModel == 410)
                setQHY410SHR((ushort)hScrollBar11.Value);
            if (sensorModel == 485)
                setQHY485SHR((ushort)hScrollBar11.Value);
            if (sensorModel == 492)
                setQHY492SHR((ushort)hScrollBar11.Value);
            if (sensorModel == 568)
                setQHY5III568SHR((ushort)hScrollBar11.Value);
            if(sensorModel == 487)
                setQHY487SHR((ushort)hScrollBar11.Value);

        }

        private void hScrollBar12_Scroll(object sender, ScrollEventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }


        void writeCMOS_bitbuttonA()
        {
            ushort index;
            int value;

            if (radioButtonHEX.Checked == true)
                index = Convert.ToUInt16(textBoxBitButtonA.Text, 16);           //输入框为16进制
            else
                index = Convert.ToUInt16(textBoxBitButtonA.Text, 10);           //输入框为16进制


            value = Convert.ToUInt16(buttonbit0.Text, 16) +
                         Convert.ToUInt16(buttonbit1.Text, 16) * 2 +
                        Convert.ToUInt16(buttonbit2.Text, 16) * 4 +
                        Convert.ToUInt16(buttonbit3.Text, 16) * 8 +
                        Convert.ToUInt16(buttonbit4.Text, 16) * 16 +
                        Convert.ToUInt16(buttonbit5.Text, 16) * 32 +
                        Convert.ToUInt16(buttonbit6.Text, 16) * 64 +
                        Convert.ToUInt16(buttonbit7.Text, 16) * 128;

            writeCMOS(index, (ushort)value);

            labelValueA.Text = value.ToString();
            //checkBoxTrain.Checked = false;
            // checkBoxTrain.Checked = true;
        }


        void writeCMOS_bitbuttonB()
        {
            ushort index;
            int value;

            if (radioButtonHEX.Checked == true)
                index = Convert.ToUInt16(textBoxBitButtonB.Text, 16);                 //输入框为16进制
            else
                index = Convert.ToUInt16(textBoxBitButtonB.Text, 10);                 //输入框为16进制

            value = Convert.ToUInt16(buttonbit8.Text, 16) +
                         Convert.ToUInt16(buttonbit9.Text, 16) * 2 +
                        Convert.ToUInt16(buttonbit10.Text, 16) * 4 +
                        Convert.ToUInt16(buttonbit11.Text, 16) * 8 +
                        Convert.ToUInt16(buttonbit12.Text, 16) * 16 +
                        Convert.ToUInt16(buttonbit13.Text, 16) * 32 +
                        Convert.ToUInt16(buttonbit14.Text, 16) * 64 +
                        Convert.ToUInt16(buttonbit15.Text, 16) * 128;

            writeCMOS(index, (ushort)value);
            labelValueB.Text = value.ToString();

            //  checkBoxTrain.Checked = false;
            //  checkBoxTrain.Checked = true;
        }


        private void button3_Click(object sender, EventArgs e)
        {

            ushort index;
            ushort value;
            if (radioButtonHEX.Checked == true)
                index = Convert.ToUInt16(textBoxIndex.Text, 16);
            else
                index = Convert.ToUInt16(textBoxIndex.Text, 10);




            value = Convert.ToUInt16(textBoxValue.Text, 16);


            labelIndex.Text = index.ToString();
            labelValue.Text = value.ToString();

            writeCMOS(index, value);
        }

        private void button11_Click(object sender, EventArgs e)
        {

        }

        private void buttonbit0_Click(object sender, EventArgs e)
        {
            if (buttonbit0.Text == "0")
                buttonbit0.Text = "1";
            else
                buttonbit0.Text = "0";
            writeCMOS_bitbuttonA();
        }

        private void buttonbit1_Click(object sender, EventArgs e)
        {
            if (buttonbit1.Text == "0")
                buttonbit1.Text = "1";
            else
                buttonbit1.Text = "0";
            writeCMOS_bitbuttonA();
        }

        private void buttonbit2_Click(object sender, EventArgs e)
        {
            if (buttonbit2.Text == "0")
                buttonbit2.Text = "1";
            else
                buttonbit2.Text = "0";
            writeCMOS_bitbuttonA();
        }

        private void buttonbit3_Click(object sender, EventArgs e)
        {
            if (buttonbit3.Text == "0")
                buttonbit3.Text = "1";
            else
                buttonbit3.Text = "0";
            writeCMOS_bitbuttonA();
        }

        private void buttonbit4_Click(object sender, EventArgs e)
        {
            if (buttonbit4.Text == "0")
                buttonbit4.Text = "1";
            else
                buttonbit4.Text = "0";
            writeCMOS_bitbuttonA();

        }

        private void buttonbit5_Click(object sender, EventArgs e)
        {
            if (buttonbit5.Text == "0")
                buttonbit5.Text = "1";
            else
                buttonbit5.Text = "0";
            writeCMOS_bitbuttonA();
        }

        private void buttonbit6_Click(object sender, EventArgs e)
        {
            if (buttonbit6.Text == "0")
                buttonbit6.Text = "1";
            else
                buttonbit6.Text = "0";
            writeCMOS_bitbuttonA();

        }

        private void buttonbit7_Click(object sender, EventArgs e)
        {
            if (buttonbit7.Text == "0")
                buttonbit7.Text = "1";
            else
                buttonbit7.Text = "0";
            writeCMOS_bitbuttonA();


        }

        private void buttonbit8_Click(object sender, EventArgs e)
        {
            if (buttonbit8.Text == "0")
                buttonbit8.Text = "1";
            else
                buttonbit8.Text = "0";


            writeCMOS_bitbuttonB();


        }


        private void buttonbit9_Click(object sender, EventArgs e)
        {
            if (buttonbit9.Text == "0")
                buttonbit9.Text = "1";
            else
                buttonbit9.Text = "0";

            writeCMOS_bitbuttonB();
        }



        private void buttonbit10_Click(object sender, EventArgs e)
        {
            if (buttonbit10.Text == "0")
                buttonbit10.Text = "1";
            else
                buttonbit10.Text = "0";

            writeCMOS_bitbuttonB();
        }

        private void buttonbit11_Click(object sender, EventArgs e)
        {
            if (buttonbit11.Text == "0")
                buttonbit11.Text = "1";
            else
                buttonbit11.Text = "0";
            writeCMOS_bitbuttonB();
        }

        private void buttonbit12_Click(object sender, EventArgs e)
        {
            if (buttonbit12.Text == "0")
                buttonbit12.Text = "1";
            else
                buttonbit12.Text = "0";

            writeCMOS_bitbuttonB();

        }

        private void buttonbit13_Click(object sender, EventArgs e)
        {
            if (buttonbit13.Text == "0")
                buttonbit13.Text = "1";
            else
                buttonbit13.Text = "0";

            writeCMOS_bitbuttonB();
        }

        private void buttonbit14_Click(object sender, EventArgs e)
        {
            if (buttonbit14.Text == "0")
                buttonbit14.Text = "1";
            else
                buttonbit14.Text = "0";

            writeCMOS_bitbuttonB();

        }

        private void buttonbit15_Click(object sender, EventArgs e)
        {
            if (buttonbit15.Text == "0")
                buttonbit15.Text = "1";
            else
                buttonbit15.Text = "0";

            writeCMOS_bitbuttonB();
        }

        private void buttonWriteFPGA_Click(object sender, EventArgs e)
        {
            ushort index;
            ushort value;

            index = Convert.ToUInt16(textBoxFpgaIndex.Text, 10);
            value = Convert.ToUInt16(textBoxFpgaValue.Text, 16);


            writeFPGA(index, value);

        }

        private void hScrollBar13_Scroll(object sender, ScrollEventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void hScrollBar16_Scroll(object sender, ScrollEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)//PFY
        {
           
                  tabPage1.Parent = null;
                  tabPage2.Parent = null;
                  tabPage3.Parent = null;
                  tabPage4.Parent = null;
                  tabPage5.Parent = null;
                  tabPage6.Parent = null;
                  tabPage7.Parent = null;
                  tabPage14.Parent = null;
             


        }

        private void hScrollBar17_Scroll(object sender, ScrollEventArgs e)
        {

        }

        private void hScrollBar14_Scroll(object sender, ScrollEventArgs e)
        {
            label19.Text = "AMPVSTART:" + hScrollBar14.Value.ToString();
            setAMPVStart((UInt32)hScrollBar14.Value);
        }

        private void hScrollBar15_Scroll(object sender, ScrollEventArgs e)
        {
            label52.Text = "AMPVEND:" + hScrollBar15.Value.ToString();
            setAMPVEnd((UInt32)hScrollBar15.Value);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            setHEND(4280);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //setHCOUNT1((ushort)hScrollBar5.Value);
            //setHCOUNT2((ushort)hScrollBar5.Value);
            //button5.Text = hScrollBar5.Value.ToString();

        }

        private void button6_Click(object sender, EventArgs e)
        {
            //曝光时间为 （VMAX-SHR）行，因此当设置为 VMAX-(VMAX-10)=10行时，时间为10行曝光，比较短，有利于在白天进行调试而不饱和。


        }

        private void textBoxFpgaIndex_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxFpgaValue_TextChanged(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            PLL_RECONFIG(1);

        }

        private void label18_Click(object sender, EventArgs e)
        {

        }

        private void label19_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void textBoxValue_TextChanged(object sender, EventArgs e)
        {

        }

        void correctFirstPixelPosition()
        {
            richTextBox1.AppendText("FirstPixel :" + Environment.NewLine);

            writeCMOS(15, 0x46);       //enable pattern
            writeCMOS(13, 0x00);       //set gain minumum                           

            Thread.Sleep(100);
            byte[] PIXEL = new byte[6];

            for (int i = 0; i < 6; i++)
            {
                PIXEL[i] = readFPGA((ushort)(33 + i));
            }


            //labelWordPosition.Text = "";

            for (int i = 0; i < 6; i++)
            {
                richTextBox1.AppendText("  " + PIXEL[i].ToString("D2") + "  ");
                //labelWordPosition.Text = labelWordPosition.Text + " " + PIXEL[i].ToString();
            }


            int FirstPixel = 0;
            int InitPixel = PIXEL[0];

            for (int i = 0; i < 6; i++)
            {
                if (PIXEL[i] != InitPixel)
                {
                    FirstPixel = i;
                    i = 7;

                }
            }


        

            writeFPGA(97, (ushort)FirstPixel);

            writeCMOS(15, 0xC0);       //disable pattern
            writeCMOS(13, 0xf6);       //set gain back to default          

        }



        private void button9_Click(object sender, EventArgs e)
        {

            correctFirstPixelPosition();

        }


        private void button10_Click(object sender, EventArgs e)
        {
            PLL_RECONFIG(2);
        }

        private void button11_Click_1(object sender, EventArgs e)
        {
            setIDLE();
            Thread.Sleep(100);
            releaseIDLE();
        }

        private void button12_Click(object sender, EventArgs e)
        {


            bool success = true;
            byte max_value = 0;
            int delta = 0;

            ushort pos;

            richTextBox1.Clear();

            // writeCMOS(6,0) ;       //to avoid the random 98e0 (light responsed ) been detected , first disable the analog channel to let it output fixed vlaue 
            //writeCMOS( 13, 0 );

            writeCMOS(21, 0xdf);//for Gsense4040, must enable the bit of train in spi register then the 0x98e0 will appear in the data when the SYNC is high

            Thread.Sleep(100);

            for (ushort i = 0; i < 8; i++)
            {
                writeFPGA(88, i);  //select lane
                word_position[i] = readFPGA(32);
                richTextBox1.AppendText("lane" + i.ToString() + ":" + (word_position[i]).ToString() + "\r\n");
            }

            for (ushort i = 0; i < 8; i++)
            {
                if (word_position[i] < 6)        //normally the position shold be small. filter off the un-normal value
                {
                    if (max_value < word_position[i])
                        max_value = word_position[i];
                }

                else
                {
                    success = false;
                    richTextBox1.AppendText("position is not normal , failur\r\n");

                }
            }


            richTextBox1.AppendText("max position : " + max_value.ToString() + "\r\n");


            //compare with the max and do adjustment (-12) to the shiftResgiter position
            for (ushort i = 0; i < 8; i++)
            {
                if (word_position[i] < max_value)
                {
                    delta = (int)max_value - (int)word_position[i];

                    richTextBox1.AppendText("correct lane# : " + i.ToString() + "  -" + delta.ToString() + "\r\n");

                    pos = (ushort)(BitPosition[i] - delta * 12);
                    writeFPGA(88, i);
                    writeFPGA(93, pos);
                    writeFPGA(94, 1);
                    writeFPGA(94, 0);


                }

            }

            // writeCMOS( 6, 0x80 );       //enable the analog of cmos
            // writeCMOS( 13, 0x9e );


            writeCMOS(21, 0xde);//for Gsense4040, must enable the bit of train in spi register then the 0x98e0 will appear in the data when the SYNC is high

            ushort WordPosition = 0;
            WordPosition = (ushort)(word_position[0] + 1);
            richTextBox1.AppendText("WordPosition Write to FPGA:" + WordPosition.ToString());
            writeFPGA(97, WordPosition);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            resetCMOS();
        }

        private void label20_Click(object sender, EventArgs e)
        {

        }

        private void button14_Click(object sender, EventArgs e)
        {
            writeFPGA(0x38, 0x01);
            writeFPGA(0x38, 0x01);
            writeFPGA(0x38, 0x01);
            writeFPGA(0x38, 0x01);

            writeFPGA(0x39, 0x00);

            writeFPGA(0x38, 0x00);
        }


        void PLL_RECONFIG(ushort i)
        {
            byte[] REG = new byte[18];

            writeFPGA(0, 0);//CMOS RST_N       to avoid the PLL reset cause the image is wrong or dead, need to make cmos to the reset statu








            if (i == 0)
            {
                //C0=600M
                //C1=600/12=50M     FPS=43
                //C3=300M
                //C4=300/12=25M     FPS=21.5

                REG[0] = 0x30;
                REG[1] = 0x18;
                REG[2] = 0x20;
                REG[3] = 0x10;
                REG[4] = 0x0f;
                REG[5] = 0x0f;
                REG[6] = 0x85;
                REG[7] = 0x80;
                REG[8] = 0xc0;
                REG[9] = 0x00;
                REG[10] = 0x00;
                REG[11] = 0x4c;
                REG[12] = 0X06;
                REG[13] = 0x00;
                REG[14] = 0x04;
                REG[15] = 0x06;
                REG[16] = 0x00;
                REG[17] = 0x30;
            }

            else if (i == 1)
            {
                //C0=450M
                //C1=450/12=37.5M      FPS=32.5
                //C3=300M
                //C4=300/12=25M         FPS=21.5

                REG[0] = 0x48;
                REG[1] = 0x24;
                REG[2] = 0x20;
                REG[3] = 0x28;
                REG[4] = 0x0F;
                REG[5] = 0x0F;
                REG[6] = 0x84;
                REG[7] = 0xC0;
                REG[8] = 0x60;
                REG[9] = 0x80;
                REG[10] = 0x40;
                REG[11] = 0x12;
                REG[12] = 0x09;
                REG[13] = 0x00;
                REG[14] = 0x04;
                REG[15] = 0x06;
                REG[16] = 0x02;
                REG[17] = 0x30;
            }

            else if (i == 2)
            {
                //C0=525M
                //C1=525/12=43.75M                  25FPS
                //C2=350M
                //C3=350/12=29.166667M          38fps

                REG[0] = 0x48;
                REG[1] = 0x24;
                REG[2] = 0x20;
                REG[3] = 0x28;
                REG[4] = 0x0F;
                REG[5] = 0x0F;
                REG[6] = 0x84;
                REG[7] = 0xC0;
                REG[8] = 0x60;
                REG[9] = 0x80;
                REG[10] = 0x40;
                REG[11] = 0x2A;
                REG[12] = 0X15;
                REG[13] = 0x00;
                REG[14] = 0x04;
                REG[15] = 0x06;
                REG[16] = 0x02;
                REG[17] = 0x30;
            }


            else if (i == 3)
            {
                //16FPS
                //C0  400M
                //C1  400/12M

                //8FPS
                //C2  200M
                //C3  200/12M

                REG[0] = 0x18;
                REG[1] = 0x0c;
                REG[2] = 0x10;
                REG[3] = 0x08;
                REG[4] = 0x0F;
                REG[5] = 0x0F;
                REG[6] = 0x84;
                REG[7] = 0xC0;
                REG[8] = 0x60;
                REG[9] = 0x80;
                REG[10] = 0x40;
                REG[11] = 0x02;
                REG[12] = 0X01;
                REG[13] = 0x00;
                REG[14] = 0x04;
                REG[15] = 0x06;
                REG[16] = 0x03;
                REG[17] = 0xb0;
            }

            //for testing/debug data pattern
            else if (i == 255)
            {
                //16FPS
                //C0  400M
                //C1  400/12M

                //8FPS
                //C2  200M
                //C3  200/12M

                REG[0] = 0x80;
                REG[1] = 0x00;
                REG[2] = 0x00;
                REG[3] = 0x00;
                REG[4] = 0x00;
                REG[5] = 0x00;
                REG[6] = 0x00;
                REG[7] = 0x00;
                REG[8] = 0x00;
                REG[9] = 0x00;
                REG[10] = 0x00;
                REG[11] = 0x00;
                REG[12] = 0X00;
                REG[13] = 0x00;
                REG[14] = 0x00;
                REG[15] = 0x00;
                REG[16] = 0x00;
                REG[17] = 0x01;
            }



            writeFPGA(102, 0);  //make sure the WR low.
            writeFPGA(103, 0);//make sure the UPDATE low

            for (ushort address = 0; address < 18; address++)
            {
                writeFPGA(100, REG[address]);  //REG100 Data
                writeFPGA(101, address); //REG101 ADDRESS
                writeFPGA(102, 1);// WR  rising edge , write the data into the address position
                writeFPGA(102, 0); //WR return 0.
            }



            writeFPGA(103, 1); //UPDATE PLL
            writeFPGA(103, 0); //UPDATE return 0

            //after update , the PLL need some time to finish this progress and the updateDone will return low . Then can reset
            Thread.Sleep(10);

            writeFPGA(83, 0);   //PLL rst return idle
            writeFPGA(83, 1);   //PLL rst start
            writeFPGA(83, 0); //PLL RST end

            writeFPGA(0, 1);  //CMOS RST end;        

            //PLL1_RESET();
            //writeFPGA( 83, 1 ); //PLL1 RESET
            //writeFPGA( 83, 0 ); //PLL1 RESET return 0.

        }

        private void button15_Click(object sender, EventArgs e)
        {
            PLL_RECONFIG(0);
        }

        private void button15_Click_1(object sender, EventArgs e)
        {
            setSVR(0x00);
            writeCMOS(0x3002, 0x01);
        }



        private void textBoxIndex_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void button18_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void button20_Click(object sender, EventArgs e)
        {

        }

        private void button21_Click(object sender, EventArgs e)
        {

        }

        private void button22_Click(object sender, EventArgs e)
        {
            counter_timer = 0;
            timer1.Enabled = true;
            //ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode( camhandle, 1 );




            // writeCMOS( 0X3033, 0X10 );

        }

        private void button23_Click(object sender, EventArgs e)
        {
            byte mode;
            ushort xbin;
            ushort ybin;
            byte readmode;

            mode = Convert.ToByte(textBoxLLA0_MODE.Text);
            xbin = Convert.ToUInt16(textBoxLLA0_XBIN.Text);
            ybin = Convert.ToUInt16(textBoxLLA0_YBIN.Text);
            readmode = Convert.ToByte(textBoxLLA0_READMODE.Text);

            LowLevelA0(mode, xbin, ybin, readmode);

        }

        private void button24_Click(object sender, EventArgs e)
        {
            byte speed;
            speed = Convert.ToByte(textBoxLLA1_SPEED.Text);
            LowLevelA1(speed);
        }

        private void button25_Click(object sender, EventArgs e)
        {
            byte resmode;
            ushort xsize;
            ushort xstart;
            ushort ysize;
            ushort ystart;

            resmode = Convert.ToByte(textBoxLLA2_MODE.Text);
            xsize = Convert.ToUInt16(textBoxLLA2_XSIZE.Text);
            xstart = Convert.ToUInt16(textBoxLLA2_XSTART.Text);
            ysize = Convert.ToUInt16(textBoxLLA2_YSIZE.Text);
            ystart = Convert.ToUInt16(textBoxLLA2_YSTART.Text);


            LowLevelA2(resmode, xsize, xstart, ysize, ystart);
        }

        private void button26_Click(object sender, EventArgs e)
        {
            uint expTime;

            expTime = Convert.ToUInt32(textBoxLLA3_US.Text) + 1000 * Convert.ToUInt32(textBoxLLA3_MS.Text) + 1000 * 1000 * Convert.ToUInt32(textBoxLLA3_SEC.Text);
            LowLevelA3(expTime);
        }

        private void button27_Click(object sender, EventArgs e)
        {
            ushort againR, againG, againB;
            ushort dgainR, dgainG, dgainB;
            ushort eGain;
            ushort HGCLGC;

            againR = Convert.ToUInt16(textBoxLLA4_AGAINR.Text);
            againG = Convert.ToUInt16(textBoxLLA4_AGAING.Text);
            againB = Convert.ToUInt16(textBoxLLA4_AGAINB.Text);

            dgainR = Convert.ToUInt16(textBoxLLA4_DGAINR.Text);
            dgainG = Convert.ToUInt16(textBoxLLA4_DGAING.Text);
            dgainB = Convert.ToUInt16(textBoxLLA4_DGAINB.Text);

            eGain = Convert.ToUInt16(textBoxLLA4_EGAIN.Text);
            HGCLGC = Convert.ToUInt16(textBoxLLA4_HGCLCG.Text);

            LowLevelA4_EX(againR, dgainR, againG, dgainG, againB, dgainB, eGain, HGCLGC);
        }

        private void button28_Click(object sender, EventArgs e)
        {
            byte usbtraffic;
            usbtraffic = Convert.ToByte(textBoxLLA5_USBTRAFFIC.Text);
            LowLevelA5(usbtraffic);

        }

        private void button29_Click(object sender, EventArgs e)
        {
            byte command;
            command = Convert.ToByte(textBoxLLA6_COMMAND.Text);
            LowLevelA6(command);
        }

        private void button30_Click(object sender, EventArgs e)
        {
            LowLevelA6(0x00);
        }

        private void button31_Click(object sender, EventArgs e)
        {
            LowLevelA6(0x11);
        }

        private void button32_Click(object sender, EventArgs e)
        {
            LowLevelA6(0xff);
        }

        private void button33_Click(object sender, EventArgs e)
        {
            if (radioButton8bit.Checked == true)
                LowLevelA7(0x00);
            else
                LowLevelA7(0x01);
        }

        private void button34_Click(object sender, EventArgs e)
        {
            ushort offset1R, offset1G, offset1B;
            ushort offset2R, offset2G, offset2B;
            offset1R = Convert.ToUInt16(textBoxLLA8_OFFSET1R.Text);
            offset1G = Convert.ToUInt16(textBoxLLA8_OFFSET1G.Text);
            offset1B = Convert.ToUInt16(textBoxLLA8_OFFSET1B.Text);
            offset2R = Convert.ToUInt16(textBoxLLA8_OFFSET2R.Text);
            offset2G = Convert.ToUInt16(textBoxLLA8_OFFSET2G.Text);
            offset2B = Convert.ToUInt16(textBoxLLA8_OFFSET2B.Text);

            LowLevelA8(offset1R, offset1G, offset1B, offset2R, offset2G, offset2B);

        }

        private void button35_Click(object sender, EventArgs e)
        {
            if (checkBoxEnableDDR.Checked == true)
                LowLevelA9(0xFF);
            else
                LowLevelA9(0X00);




        }

        private void button36_Click(object sender, EventArgs e)
        {
            LowLevelAD();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked == true)
                writeFPGA(8, 1);
            else
                writeFPGA(8, 0);
        }

        private void label44_Click(object sender, EventArgs e)
        {

        }

        private void button37_Click(object sender, EventArgs e)
        {
            //ushort lane;
            ushort bit_delay;
            int deserdes_monitor;

            bit_delay = Convert.ToUInt16(textBoxBitDelay.Text);

            writeFPGA(93, bit_delay);
            writeFPGA(94, 1);                //wr raise edge
            writeFPGA(94, 0);

            deserdes_monitor = 256 * readFPGA(28) + readFPGA(29);
            label46.Text = deserdes_monitor.ToString("X");


        }

        ushort train_pos_lut(ushort i)
        {
            ushort pos = 0;

            switch (i)
            {
                case 0x98e0:
                    pos = 36;
                    break;
                case 0x31D0:
                    pos = 25;
                    break;
                case 0X63A0:
                    pos = 26;
                    break;
                case 0XC740:
                    pos = 27;
                    break;
                case 0X8E90:
                    pos = 28;
                    break;
                case 0X1D30:
                    pos = 29;
                    break;
                case 0X3A60:
                    pos = 30;
                    break;
                case 0X74C0:
                    pos = 31;
                    break;
                case 0XE980:
                    pos = 32;
                    break;
                case 0XD310:
                    pos = 33;
                    break;
                case 0XA630:
                    pos = 34;
                    break;
                case 0X4C70:
                    pos = 35;
                    break;

                default:
                    break;
            }

            return pos;
        }

        private void button38_Click(object sender, EventArgs e)
        {
            ushort lane;
            int deserdes_monitor;
            ushort pos;



            richTextBox1.Clear();

            //writeFPGA( 8, 1 );  //enable the train for gsense400         
            writeCMOS(21, 0xdf); //enable train register of gense2020              
            writeFPGA2(2, 1);  //pull the sync high to enable train for gense2020
            //writeCMOS( 0x1c,0x12 );              //set register 0x1c  (tranning_en=1) to enable the tranning for gsense2020 

            for (lane = 0; lane < 8; lane++)
            {
                writeFPGA(88, lane);
                writeFPGA(93, 36);
                writeFPGA(94, 1);                //wr raise edge
                writeFPGA(94, 0);
                deserdes_monitor = 256 * readFPGA(28) + readFPGA(29);  //read monitor
                richTextBox1.AppendText("read lane #" + lane.ToString() + " [11:0]=" + deserdes_monitor.ToString("X") + Environment.NewLine);
                pos = train_pos_lut((ushort)deserdes_monitor);
                BitPosition[lane] = pos;
                richTextBox1.AppendText("pos=" + pos.ToString() + Environment.NewLine);
                writeFPGA(88, lane);
                writeFPGA(93, pos);
                writeFPGA(94, 1);
                writeFPGA(94, 0);
                deserdes_monitor = 256 * readFPGA(28) + readFPGA(29);  //read monitor
                richTextBox1.AppendText("results:" + deserdes_monitor.ToString("X") + Environment.NewLine);
                if (deserdes_monitor == 0x98E0)
                    richTextBox1.AppendText("-------------success------------------" + Environment.NewLine);
                else
                    richTextBox1.AppendText("-------------failur------------------" + Environment.NewLine);




            }

            richTextBox1.AppendText("----------------BitPosition-------" + Environment.NewLine);
            for (lane = 0; lane < 8; lane++)
            {
                richTextBox1.AppendText(BitPosition[lane] + " ");
            }


            // writeFPGA( 8, 0 );  //pull down the train , end of trainning  for QHY42

            // writeCMOS( 0x1c,0x12 );     //set the register to disable trainning for gense2020
            writeFPGA2(2, 0);  //pull the sync low to disable train for gense2020
            writeCMOS(21, 0xde);
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked == true)
                enableDDR(true);
            else
                enableDDR(false);

        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked == true) setIDLE();
            else releaseIDLE();

        }

     

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox6.Checked == true)
                AMPV_ON();
            else
                AMPV_OFF();
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox7.Checked == true)
                BIT16();
            else
            {
                BIT8();
            }
        }

        private void button40_Click(object sender, EventArgs e)
        {


            writeCMOS(0, 0X00);
            writeCMOS(1, 0X00);
            writeCMOS(2, 0X00);
            writeCMOS(3, 0X00);
            writeCMOS(4, 0X3B);

            writeCMOS(5, 0XC0);
            writeCMOS(6, 0XEA);
            writeCMOS(7, 0X00);
            writeCMOS(8, 0X06);
            writeCMOS(9, 0X59);

            writeCMOS(10, 0X01);
            writeCMOS(11, 0X7A);
            writeCMOS(12, 0XF3);
            writeCMOS(13, 0XF6);
            writeCMOS(14, 0X14);

            writeCMOS(15, 0XC0);
            writeCMOS(16, 0X01);
            writeCMOS(17, 0XF9);
            writeCMOS(18, 0X00);
            writeCMOS(19, 0X22);

            writeCMOS(20, 0X05);
            writeCMOS(21, 0XDE);
            writeCMOS(22, 0X08);
            writeCMOS(23, 0X00);
            writeCMOS(24, 0XF0);

            writeCMOS(25, 0X0F);
            writeCMOS(26, 0X9C);
            writeCMOS(27, 0XF4);
            writeCMOS(28, 0XC7);
            writeCMOS(29, 0X1E);

            writeCMOS(30, 0X73);
            writeCMOS(31, 0X2F);
            writeCMOS(32, 0XA8);
            writeCMOS(33, 0X88);
            writeCMOS(34, 0X0A);

            writeCMOS(35, 0X03);
            writeCMOS(36, 0XF8);
            writeCMOS(37, 0X96);
            writeCMOS(38, 0X4F);
            writeCMOS(39, 0XBE);

            writeCMOS(40, 0X00);
            writeCMOS(41, 0X00);
            writeCMOS(42, 0X00);
            writeCMOS(43, 0X00);
            writeCMOS(44, 0X00);

            writeCMOS(45, 0X00);
            writeCMOS(46, 0X00);
            writeCMOS(47, 0X00);




        }

        void setCMOSClock(ushort i)
        {
            //0=300Mhz    1=600Mhz
            if (i == 0)
                writeFPGA(11, 0);
            else
                writeFPGA(11, 1);
        }
      

     

        private void button41_Click(object sender, EventArgs e)
        {
            writeCMOS(0, 0X6E);
            writeCMOS(1, 0x02);
            writeCMOS(2, 0xB7);
            writeCMOS(3, 0x0F);
            writeCMOS(4, 0xE4);
            writeCMOS(5, 0x7F);
            writeCMOS(6, 0x80);
            writeCMOS(7, 0X17);
            writeCMOS(8, 0xB1);
            writeCMOS(9, 0x37);

            writeCMOS(10, 0x00);
            writeCMOS(11, 0xB4);
            writeCMOS(12, 0xC1);
            writeCMOS(13, 0x9E);
            writeCMOS(14, 0x47);
            writeCMOS(15, 0xE5);
            writeCMOS(16, 0x89);
            writeCMOS(17, 0xF1);
            writeCMOS(18, 0x62);
            writeCMOS(19, 0x15);
            writeCMOS(20, 0x83);

            writeCMOS(21, 0xC0);
            writeCMOS(22, 0x00);
            writeCMOS(23, 0x71);
            writeCMOS(24, 0x77);
            writeCMOS(25, 0xDA);
            writeCMOS(26, 0X89);//2CMS
            writeCMOS(27, 0xE6);
            writeCMOS(28, 0x3B);
            writeCMOS(29, 0x12);
            writeCMOS(30, 0x75);
            writeCMOS(31, 0x03);

        }

        private void button42_Click(object sender, EventArgs e)
        {



            setDelay(Convert.ToByte(textBoxPhaseDelay.Text));

        }

        private void label53_Click(object sender, EventArgs e)
        {

        }

        private void radioButton16bit_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button44_Click(object sender, EventArgs e)
        {

        }

        private void button45_Click(object sender, EventArgs e)
        {

        }

        private void button45_Click_1(object sender, EventArgs e)
        {

        }


        void PLL1_RESET()
        {

            // writeFPGA( 105, 1 );   //ATLCLKCTL clock enable = false
            //Thread.Sleep( 100 );
            //writeFPGA2( 8, 0 );  //MAX10 driver idle
            //  writeFPGA( 0, 0 );//CMOS RST_N       to avoid the PLL reset cause the image is wrong or dead, need to make cmos to the reset statu

            //  Thread.Sleep( 100 );

            writeFPGA(83, 0);
            writeFPGA(83, 1);
            writeFPGA(83, 0);


            //writeFPGA( 0, 1 );  //CMOS RST end;        

            //writeFPGA2( 8, 1 );       //MAX10 driver release  
            //Thread.Sleep( 100 );

            // writeFPGA( 105, 0 );    //ALTCLCTL CLOCK ENABLE=true

        }

        void PLL2_RESET()
        {
            writeFPGA(87, 0);
            writeFPGA(87, 1);
            writeFPGA(87, 0);
        }


        void PLL1_DPA(ushort channel, ushort direction)
        {
            writeFPGA(80, channel);  //Convert.ToByte( textBox4.Text ) ); //设置C0-C4通道
            writeFPGA(81, direction);          //direction    up down      方向

            writeFPGA(82, 1);           //generate the rising edge  产生一个上升沿
            writeFPGA(82, 0);
        }


        void PLL2_DPA(ushort channel, ushort direction)
        {
            writeFPGA(84, channel);  //Convert.ToByte( textBox4.Text ) ); //设置C0-C4通道
            writeFPGA(85, direction);          //direction    up down      方向

            writeFPGA(86, 1);           //generate the rising edge  产生一个上升沿
            writeFPGA(86, 0);
        }


        private void button45_Click_2(object sender, EventArgs e)
        {
            PLL1_RESET();
        }

        private void button46_Click(object sender, EventArgs e)
        {
            PLL2_RESET();
        }

        private void button47_Click(object sender, EventArgs e)
        {
            writeFPGA(72, 1);
            writeFPGA(72, 0);
            writeFPGA(72, 1);
        }

        private void button48_Click(object sender, EventArgs e)
        {
            ushort index;
            ushort value;

            index = Convert.ToUInt16(textBoxFpgaIndex.Text, 10);
            value = Convert.ToUInt16(textBoxFpgaValue.Text, 16);


            writeFPGA2(index, value);
        }

        private void button49_Click(object sender, EventArgs e)
        {
            ushort lane;
            lane = Convert.ToUInt16(textBoxLane.Text, 10);
            writeFPGA(88, lane);
        }


        void EnableAllTrain(bool value)
        {
            if (value == true)
            {
                writeCMOS(21, 0xdf);
                writeFPGA2(2, 1);
            }
            else
            {
                writeFPGA2(2, 0);
                writeCMOS(21, 0xde);
            }
        }


        void EnablePartTrain(bool value)
        {
            if (value == true)
            {
                writeCMOS(21, 0xdf);
            }
            else
            {
                writeCMOS(21, 0xde);
            }
        }


     



        void SetInputDelay(ushort lane, ushort delay)
        {
            writeFPGA(88, lane);//select lane
            writeFPGA(91, delay);  //write the delay value 
            writeFPGA(92, 0); //execute
            writeFPGA(92, 1);
            writeFPGA(92, 0);
        }

        void EnableTrain(bool i)
        {
            if (i == true)
            {
                writeFPGA2(2, 1);
                Thread.Sleep(50);
                writeFPGA(54, 1);
            }
            else
            {
                writeFPGA(54, 0);
                writeFPGA2(2, 0);

            }
        }


        void scanAllPhase()
        {

            //this API will scan all channel's phaseError, by using the different phase delay value . The scan results will be store
            //in a 2-dimention array. PhaseErrorScan[i,j]   the i is channel number  the j is delay value.


            ushort s = 0;
            ushort i = 0;
            ushort j = 0;

            //SetInputDelay(99, 0);
            richTextBox1.Clear();

            for ( i = 0; i < 14; i++)
            {
                InputDelayValue[i] = 0;
                SetInputDelay(i, 0);
            }

            //reset PLL to start point
           // PLL1_RESET();
            //PLL1_DPA(5, 1);     //let phase to go -1 to allow check phase ahead

            //enable train    set to 98e
            EnableTrain(true);
        

            for ( j = 0; j < 23; j++)
            {

                // richTextBox1.AppendText( "Test Phase Position " + j.ToString() +"\r\n" );

                for ( i = 0; i < 14; i++)
                {
                    writeFPGA(88, i);//select lane
                    writeFPGA(91, j);  //write the delay value 
                    writeFPGA(92, 0); //execute
                    writeFPGA(92, 1);
                    writeFPGA(92, 0);
                }



                writeFPGA(95, 0);                  //clear all counter in detector
                writeFPGA(95, 1);                  //execute   
                 Thread.Sleep( 50 );



                //select each channel and readout the phaseError data
                for ( i = 0; i < 14; i++)
                {
                    writeFPGA(88, i);                                                                        //select channel 
                    PhaseErrorScan[i, j] = (ushort)(readFPGA(30) * 256 + readFPGA(31));        //readout data
                    PhaseEdgeScan[i, j] = (ushort)(readFPGA(28) * 256 + readFPGA(29));
                    // richTextBox1.AppendText( "Lane " + i.ToString() + " :" + ( PhaseErrorScan[ i,j ] ).ToString() + "\r\n" );
                }

            }

            richTextBox1.AppendText("--------------RESULT-----------------\r\n");
            for ( i = 0; i < 14; i++)
            {
                richTextBox1.AppendText("Lane  PhaseErrorScan" + " "+i.ToString() + ":");
                for ( j = 0; j < 23; j++)
                {
                    richTextBox1.AppendText((PhaseErrorScan[i, j]).ToString() + " ");
                }
                richTextBox1.AppendText("\r\n");

                    //richTextBox1.AppendText("Lane  PhaseEdgeScan" +" " + i.ToString() + ":");
                    //for ( j = 0; j < 23; j++)
                    //{
                    //    richTextBox1.AppendText((PhaseEdgeScan[i, j]).ToString() + " ");
                    //}
                //richTextBox1.AppendText("\r\n");

                //ushort k;
                //k = PhaseEdgeScan[i, 0];
                for ( j = 0; j < 23; j++)
                {

                    if (PhaseEdgeScan[i, j] != 0X2CE3)
                        PhaseEdgeScanChanged[i, j] = 1;
                    else
                        PhaseEdgeScanChanged[i, j] = 0;
                  //  k = PhaseEdgeScan[i, j];



                    //use the phaseErrorScan data to correct the PhaseEdgeScan data to avoid the unstable position been regarded as good position
                    if (PhaseErrorScan[i, j] != 0)
                    {
                        PhaseEdgeScanChanged[i, j] = 2;
                    }

                }




                richTextBox1.AppendText("PhaseEdgeScanChanged :" );
                for ( j = 0; j < 23; j++)
                {
                    richTextBox1.AppendText((PhaseEdgeScanChanged[i, j]).ToString() + " ");
                }
                richTextBox1.AppendText("\r\n");




                s = 0;
                //richTextBox1.AppendText("Select intermediate phase:"+"\r\n");
                richTextBox1.AppendText("\r\n");
               


                //secondary scan for better position , to seek the five 0 range

                while (s < 23)
                {
                    if (PhaseEdgeScanChanged[i, s] == 0 )
                    {
                        SetInputDelay(i, (ushort)(s));      //set to the middle. Since we use DPA PLL1 to reverse a step. So it is middle - 1  (may not accurate since one PLL step is not one InputDelay step
                        InputDelayValue[i] = (ushort)(s);
                        s = 23;

                    }
                    else
                        s++;
                }


                s = 0;

                while (s < 21)
                {
                    if (PhaseEdgeScanChanged[i,s] == 0 && PhaseEdgeScanChanged[i,s + 1] == 0 && PhaseEdgeScanChanged[i,s + 2] == 0)
                    {
                        SetInputDelay(i, (ushort)(s + 1));      //set to the middle. Since we use DPA PLL1 to reverse a step. So it is middle - 1  (may not accurate since one PLL step is not one InputDelay step
                        InputDelayValue[i] = (ushort)(s + 1);
                        s = 21;

                    }
                    else
                        s++;
                }


                s = 0;

                while (s < 19)
                {
                    if (PhaseEdgeScanChanged[i,s] == 0 && PhaseEdgeScanChanged[i,s + 1] == 0 && PhaseEdgeScanChanged[i,s + 2] == 0 && PhaseEdgeScanChanged[i,s + 3] == 0 && PhaseEdgeScanChanged[i,s + 4] == 0)
                    {
                        SetInputDelay(i, (ushort)(s + 2));      //set to the middle. Since we use DPA PLL1 to reverse a step. So it is middle - 1  (may not accurate since one PLL step is not one InputDelay step
                        InputDelayValue[i] = (ushort)(s + 2);
                        s = 19;

                    }
                    else
                        s++;
                }

                s = 0;
                while (s < 17)
                {
                    if (PhaseEdgeScanChanged[i,s] == 0 && PhaseEdgeScanChanged[i,s + 1] == 0 && PhaseEdgeScanChanged[i,s + 2] == 0 && PhaseEdgeScanChanged[i,s + 3] == 0 && PhaseEdgeScanChanged[i,s + 4] == 0 && PhaseEdgeScanChanged[i,s + 5] == 0 && PhaseEdgeScanChanged[i,s + 6] == 0)
                    {
                        SetInputDelay(i, (ushort)(s + 3)); //set to the middle. Since we use DPA PLL1 to reverse a step. So it is middle - 1  (may not accurate since one PLL step is not one InputDelay step
                        InputDelayValue[i] = (ushort)(s + 3);
                        s = 17;
                    }
                    else
                        s++;
                }

            }

            richTextBox1.AppendText("\r\n");

            richTextBox1.AppendText("Select InputDelayValue :" + "\r\n");
            for ( i = 0; i < 14; i++)
            {
                richTextBox1.AppendText("Input delay Lane number :" + i.ToString("D") + "Value: " + InputDelayValue[i].ToString() + " " + "\r\n");
            }


            //disable train 
            Thread.Sleep(50);
            EnableTrain(false);

                   

        }

        private void button50_Click(object sender, EventArgs e)
        {

            ushort[] PhaseError = new ushort[8];
            ushort[] PhaseDelay = new ushort[9];


            for (ushort i = 0; i < 8; i++)
            {
                PhaseDelay[i] = 0;
                writeFPGA(88, i);//select lane
                writeFPGA(91, PhaseDelay[i]);  //write the inital zero value 
                writeFPGA(92, 0); //execute
                writeFPGA(92, 1);
                writeFPGA(92, 0);
            }


            //enable train 
            writeCMOS(21, 0xdf);
            writeFPGA2(2, 1);



            //InputPhaseDetectorEXE
            writeFPGA(95, 0);                  //clear all counter in detector
            writeFPGA(95, 1);                  //execute   
            Thread.Sleep(50);

            richTextBox1.Clear();

            //select each channel and readout the phaseError data
            for (ushort i = 0; i < 8; i++)
            {
                writeFPGA(88, i);                                                                        //select channel 
                PhaseError[i] = (ushort)(readFPGA(30) * 256 + readFPGA(31));        //readout data
                richTextBox1.AppendText("Lane " + i.ToString() + " :" + (PhaseError[i]).ToString() + "\r\n");
            }


            for (ushort i = 0; i < 8; i++)
            {
                if (PhaseError[i] != 0)
                {

                    richTextBox1.AppendText("Detected Lane" + i.ToString() + "Phase Not Good\r\n");
                    writeFPGA(88, i);  //set to this lane
                    PhaseDelay[i] += 2;      //adjust the phase
                    richTextBox1.AppendText("Adjust Phase to" + (PhaseDelay[i]).ToString() + "\r\n");
                    writeFPGA(91, PhaseDelay[i]);
                    writeFPGA(92, 0); //execute
                    writeFPGA(92, 1);
                    writeFPGA(92, 0);
                }

            }




            //disable train 
            writeFPGA2(2, 0);
            writeCMOS(21, 0xde);

            // writeFPGA( 95, 0 );

        }




        private void labelWordPosition_Click(object sender, EventArgs e)
        {

        }

        private void button9_Click_1(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            scanAllPhase();
        }

        private void button12_Click_1(object sender, EventArgs e)
        {

        }

        private void button51_Click(object sender, EventArgs e)
        {

        }


        void setHDRMode(ushort i)
        {
            //i=0  HDR 4096*2048   i=1  only output left 2048*2048 (single channle low gain )  i=2 only output right 2048*2048(single channel high gain)
            writeFPGA(96, i);
        }

        private void hScrollBarHDR_Scroll(object sender, ScrollEventArgs e)
        {
            setHDRMode((ushort)hScrollBarHDR.Value);
            labelHDRMODE.Text = "HDRMODE:" + hScrollBarHDR.Value.ToString();

        }

        

        private void button53_Click(object sender, EventArgs e)
        {
            setIDLE();
            setVMAX(4128);
            setSHR(10);
            releaseIDLE();
        }

        private void button54_Click(object sender, EventArgs e)
        {
            writeFPGA(97, Convert.ToUInt16(textBoxWordDelay.Text));

        }

        private void button55_Click(object sender, EventArgs e)
        {
            PLL_RECONFIG(3);
        }


        void SetBurstMode(ushort startFrame, ushort EndFrame, bool enable)
        {
                writeFPGA(50, startFrame);
                writeFPGA(51, MSB(EndFrame));
                writeFPGA(52, LSB(EndFrame));

                if (enable == true)
                {
                    writeFPGA(57, 1);
                    setPatchNumber(32001);
                }
                else
                {
                    writeFPGA(57, 0);
                    setPatchNumber(0);
                }
        }

        void LowLevelReadF2()
        {
            //ReadTemperature
            byte[] xdata = new byte[64];
            ushort value = 0;
            ushort index = 0;
            double temper;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xF2, value, index, 64, xdata);
            richTextBox1.Clear();

            richTextBox1.AppendText("BUFF 0:" + xdata[0].ToString("x2") + " buff1: " + xdata[1].ToString("x2") + "  buff2: " + xdata[2].ToString("x2") + " buff3:  " + xdata[3].ToString("x2") + Environment.NewLine);

            if (xdata[0] == 1)
                temper = (double)((xdata[1] * 256 + xdata[2]) / -10.00);
            else
                temper = (double)((xdata[1] * 256 + xdata[2]) / 10.00);



            temper = (double)(xdata[1] * 256 + xdata[2]);

            richTextBox1.AppendText("ReadTemperature :" + temper.ToString()+ Environment.NewLine);
        }

        void LowLevelReadD2()
        {
            byte[] xdata = new byte[64];
            ushort value = 0;
            ushort index = 0;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd2, value, index, 64, xdata);

            richTextBox1.Clear();
            richTextBox1.AppendText("freq:" + xdata[0].ToString() + Environment.NewLine);

            int timeToEnd;
            timeToEnd = xdata[1] * 256 * 256 * 256 + xdata[2] * 256 * 256 + xdata[3] * 256 + xdata[4];
            richTextBox1.AppendText("time to end" + timeToEnd.ToString() + Environment.NewLine);

            int expTime;
            expTime = xdata[5] * 256 * 256 * 256 + xdata[6] * 256 * 256 + xdata[7] * 256 + xdata[8];
            richTextBox1.AppendText("exposure time:" + expTime.ToString() + Environment.NewLine);



            richTextBox1.AppendText("firmware version year:" + xdata[9].ToString() + Environment.NewLine);
            richTextBox1.AppendText("firmware version month:" + xdata[10].ToString() + Environment.NewLine);
            richTextBox1.AppendText("firmware version day:" + xdata[11].ToString() + Environment.NewLine);

            richTextBox1.AppendText("buff 12 :" + xdata[12].ToString() + Environment.NewLine);
            int currentTemp_a = ((ushort)xdata[13] << 8) + (ushort)xdata[14];
            int targetTemp_a = ((ushort)xdata[15] << 8) + (ushort)xdata[16];

            Int16 currentTemp = (Int16)((UInt16)currentTemp_a);
            Int16 targetTemp = (Int16)((UInt16)targetTemp_a);

            double currentTempF = (double)currentTemp / 10;
            double targetTempF = (double)targetTemp / 10;




            richTextBox1.AppendText("current temperature(C):" + currentTempF.ToString() + Environment.NewLine);
            richTextBox1.AppendText("target temperature(C):" + targetTempF.ToString() + Environment.NewLine);


            richTextBox1.AppendText("current pwm value:" + xdata[17].ToString() + Environment.NewLine);
            richTextBox1.AppendText("temperature control mode:" + xdata[18].ToString() + "  (1=auto  0=manual)" + Environment.NewLine);



            int ddrNumber = xdata[19] * 256 * 256 + xdata[20] * 256 + xdata[21];
            richTextBox1.AppendText("data In DDR:" + ddrNumber.ToString() + Environment.NewLine);

            int currentTempADU = xdata[22] * 256 + xdata[23];
            int targetTempADU = xdata[24] * 256 + xdata[25];

            richTextBox1.AppendText("current temperature(ADU):" + currentTempADU.ToString() + Environment.NewLine);
            richTextBox1.AppendText("target temperature(ADU):" + targetTempADU.ToString() + Environment.NewLine);


            int imageX = xdata[28] * 256 + xdata[29];
            int imageY = xdata[30] * 256 + xdata[31];

            richTextBox1.AppendText("imageX:" + imageX.ToString() + Environment.NewLine);
            richTextBox1.AppendText("imageY:" + imageY.ToString() + Environment.NewLine);

            richTextBox1.AppendText("image bit depth:" + xdata[32].ToString() + "    (0=8bit 1=16bit)" + Environment.NewLine);
            richTextBox1.AppendText("usb speed:" + xdata[33].ToString() + Environment.NewLine);

            richTextBox1.AppendText("--------CFW buffer---------" + Environment.NewLine);
            for (int i = 0; i < 8; i++)
            {
                richTextBox1.AppendText(xdata[38 + i].ToString("X") + " ");
            }

            richTextBox1.AppendText(Environment.NewLine);

            richTextBox1.AppendText("camera sub model:" + xdata[46].ToString() + Environment.NewLine);
            richTextBox1.AppendText("color/mono info:" + xdata[47].ToString() + Environment.NewLine);

            richTextBox1.AppendText("---------camera series number-------" + Environment.NewLine);

            for (int i = 0; i < 16; i++)
            {
                richTextBox1.AppendText(xdata[48 + i].ToString("X") + " ");
            }

        }


        private void button1_Click(object sender, EventArgs e)
        {
            LowLevelReadD2();
        }


        void LowLevelReadD3()
        {

            byte[] xdata = new byte[64];
            ushort value = 0;
            ushort index = 0;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd3, value, index, 64, xdata);

            richTextBox1.Clear();

            int shr;
            shr = xdata[2] * 256 + xdata[3];

            int hmax;
            hmax = xdata[4] * 256 + xdata[5];

            int vmax;
            vmax = xdata[41] * 256 * 256 * 256 + xdata[42] * 256 * 256 + xdata[43] * 256 + xdata[44];

            ushort hmax_ref, vmax_ref, vmax_ref_roi;
            hmax_ref = (ushort)(xdata[47] * 256 + xdata[48]);
            vmax_ref = (ushort)(xdata[49] * 256 + xdata[50]);
            vmax_ref_roi = (ushort)(xdata[61] * 256 + xdata[62]);

            int usb_traffic;
            usb_traffic = xdata[8] * 256 + xdata[9];

            int linePeriod;
            linePeriod = xdata[10] * 256 + xdata[11];

            int roi_start, roi_height;
            roi_start = xdata[30] * 256 + xdata[31];
            roi_height = xdata[32] * 256 + xdata[33];

            int expTime;
            expTime = xdata[14] * 256 * 256 * 256 + xdata[15] * 256 * 256 + xdata[16] * 256 + xdata[17];

            int actual_expTime;
            actual_expTime = xdata[18] * 256 * 256 * 256 + xdata[19] * 256 * 256 + xdata[20] * 256 + xdata[21];

            int framePeriod;
            framePeriod = xdata[22] * 256 * 256 * 256 + xdata[23] * 256 * 256 + xdata[24] * 256 + xdata[25];

            richTextBox1.AppendText("shr:" + shr.ToString() + Environment.NewLine);
            richTextBox1.AppendText("hmax:" + hmax.ToString() + Environment.NewLine);
            richTextBox1.AppendText("vmax:" + vmax.ToString() + Environment.NewLine);
            richTextBox1.AppendText("hmax_ref:" + hmax_ref.ToString() + Environment.NewLine);
            richTextBox1.AppendText("vmax_ref:" + vmax_ref.ToString() + Environment.NewLine);
            richTextBox1.AppendText("vmax_ref_roi:" + vmax_ref_roi.ToString() + Environment.NewLine);
            richTextBox1.AppendText("usb_traffic:" + usb_traffic.ToString() + Environment.NewLine);
            richTextBox1.AppendText("linePeriod:" + linePeriod.ToString() + Environment.NewLine);
            richTextBox1.AppendText("buff12 ***********:" + xdata[12].ToString() + Environment.NewLine);
            richTextBox1.AppendText("isLoneExposureMode:" + xdata[13].ToString() + Environment.NewLine);
            richTextBox1.AppendText("roi_start:" + roi_start.ToString() + Environment.NewLine);
            richTextBox1.AppendText("roi_height:" + roi_height.ToString() + Environment.NewLine);
            richTextBox1.AppendText("expTime:" + expTime.ToString() + Environment.NewLine);
            richTextBox1.AppendText("actual_expTime:" + actual_expTime.ToString() + Environment.NewLine);
            richTextBox1.AppendText("framePeriod:" + framePeriod.ToString() + Environment.NewLine);

            int is16bit;
            is16bit = xdata[38];

            int isSingleFrameMode;
            isSingleFrameMode = xdata[39];

            int enable_ddr;
            enable_ddr = xdata[40];

            richTextBox1.AppendText("is16bit:" + is16bit.ToString() + Environment.NewLine);          
            richTextBox1.AppendText("isSingleFrameMode:" + isSingleFrameMode.ToString() + Environment.NewLine);           
            richTextBox1.AppendText("enable_ddr:" + enable_ddr.ToString() + Environment.NewLine);

            ushort tsx;
            tsx = (ushort)(xdata[45] * 256 + xdata[46]);
            richTextBox1.AppendText("tsx:" + tsx.ToString() + Environment.NewLine);

            byte fx3rstn_num;
            fx3rstn_num = 0;
            fx3rstn_num = xdata[6];
            richTextBox1.AppendText("fx3 rstn num:" + fx3rstn_num.ToString() + Environment.NewLine);
            richTextBox1.AppendText(Environment.NewLine);

            richTextBox1.AppendText(xdata[51].ToString("x"));

            int HumValue = xdata[57] * 256 + xdata[58];
            int T = xdata[59] * 256 + xdata[60];

            richTextBox1.AppendText(Environment.NewLine);
            richTextBox1.AppendText(HumValue.ToString());
            richTextBox1.AppendText(Environment.NewLine);
            richTextBox1.AppendText(T.ToString());



        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            LowLevelReadD3();
        }


        void LowLevelReadD4()
        {

            //low level D4 is used to readout the input delay data ,bit alignment data and word alignment data
            byte[] xdata = new byte[64];
            ushort value = 0;
            ushort index = 0;
            int total_channles = 16;

            for (int i = 0; i < total_channles; i++)
            {
                index = (ushort)i;
                ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd4, value, index, 64, xdata);
                for (int j = 0; j < 23; j++)
                    PhaseEdgeScanChanged[i, j] = xdata[j];
            }

            index = 0x100;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd4, value, index, 64, xdata);
            for (int i = 0; i < total_channles; i++)
                InputDelayValue[i] = xdata[i];


            index = 0x101;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd4, value, index, 64, xdata);
            for (int i = 0; i < total_channles; i++)
                BitPosition[i] = (ushort)(xdata[i]);


            index = 0x102;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd4, value, index, 64, xdata);
            for (int i = 0; i < total_channles; i++)
                word_position[i] = xdata[i];

            index = 0x103;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd4, value, index, 64, xdata);
            for (int i = 0; i < total_channles; i++)
                ErrorScanSum[i] = xdata[i];

            index = 0x104;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd4, value, index, 64, xdata);
            for (int i = 0; i < total_channles; i++)
                word_position_before[i] = xdata[i];

            richTextBox1.Clear();
            richTextBox1.AppendText("LOW LEVEL READ 0XD4 Train Information" + Environment.NewLine);


            richTextBox1.AppendText("Input Delay" + Environment.NewLine);
            for (int i = 0; i < total_channles; i++)
            {
                richTextBox1.AppendText(InputDelayValue[i].ToString() + " ");
            }
            richTextBox1.AppendText(Environment.NewLine);

            richTextBox1.AppendText("Bit Position" + Environment.NewLine);
            for (int i = 0; i < total_channles; i++)
            {
                richTextBox1.AppendText(BitPosition[i].ToString() + " ");
            }
            richTextBox1.AppendText(Environment.NewLine);

            richTextBox1.AppendText("Word Position" + Environment.NewLine);
            for (int i = 0; i < total_channles; i++)
            {
                richTextBox1.AppendText(word_position[i].ToString() + " ");
            }
            richTextBox1.AppendText(Environment.NewLine);

            richTextBox1.AppendText("Word Position before" + Environment.NewLine);
            for (int i = 0; i < total_channles; i++)
            {
                richTextBox1.AppendText(word_position_before[i].ToString() + " ");
            }
            richTextBox1.AppendText(Environment.NewLine);


            richTextBox1.AppendText("Error Scan Sum" + Environment.NewLine);
            for (int i = 0; i < total_channles; i++)
            {
                richTextBox1.AppendText(ErrorScanSum[i].ToString() + " ");
            }
            richTextBox1.AppendText(Environment.NewLine);


            for (int i = 0; i < total_channles; i++)
            {
                for (int j = 0; j < 23; j++)
                    richTextBox1.AppendText(PhaseEdgeScanChanged[i, j].ToString() + " ");

                richTextBox1.AppendText(Environment.NewLine);
            }





        }


        /*
              void LowLevelReadD5(ushort index)
              {

                  byte[] xdata = new byte[ 64 ];
                  UInt16[] C = new UInt16[ 8 ];
                  UInt16[] E = new UInt16[ 8 ];


                  ushort value = 0;

                  ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead( camhandle, 0xd5, value, index, 64, xdata );

                  richTextBox1.Clear();

                  int s = 0;
                  for( int i = 0; i < 7; i++ )
                  {
                      C[ i ] = ( UInt16 ) ( xdata[ s ] * 256 + xdata[ s + 1 ] );
                      richTextBox1.AppendText( C[ i ].ToString() + " " );

                      s = s + 2;
                  }

                  richTextBox1.AppendText( Environment.NewLine );


                  for( int i = 0; i < 7; i++ )
                  {
                      E[ i ] = ( UInt16 ) ( xdata[ s ] * 256 + xdata[ s + 1 ] );
                      richTextBox1.AppendText(E[i].ToString() + " " );

                      s = s + 2;
                  }

                  richTextBox1.AppendText( Environment.NewLine );




                  for( int i = 0; i < 7; i++ )
                  {
                      richTextBox1.AppendText( ( xdata[ s ] * 256 + xdata[ s + 1 ] ).ToString() + " " );
                      s = s + 2;
                  }
                  richTextBox1.AppendText( Environment.NewLine );


                 richTextBox1.AppendText( Environment.NewLine );
                 richTextBox1.AppendText( xdata[ 58].ToString("x") );
                 richTextBox1.AppendText( Environment.NewLine );
                 richTextBox1.AppendText( xdata[ 59].ToString("x") );


                 richTextBox1.AppendText( Environment.NewLine );
                 richTextBox1.AppendText( xdata[ 60 ].ToString("x") );
                 richTextBox1.AppendText( Environment.NewLine );
                 richTextBox1.AppendText( xdata[ 61 ].ToString("x") );

                 richTextBox1.AppendText( Environment.NewLine );
                 Int32 T,P,H;
                 T =(xdata[ 32 ] * 256*256 + xdata[ 33 ]*256+xdata[34]);
                 P=  ( xdata[ 35 ] * 256 * 256 + xdata[ 36 ] * 256 + xdata[ 37 ] );
                 H = ( xdata[ 38 ] * 256 * 256 + xdata[ 39 ] * 256 + xdata[ 40 ] );


                 richTextBox1.AppendText("T="+ T.ToString() );
                 richTextBox1.AppendText( Environment.NewLine );
                 richTextBox1.AppendText( "P=" + P.ToString() );
                 richTextBox1.AppendText( Environment.NewLine );
                 richTextBox1.AppendText( "H=" + H.ToString() );
                 richTextBox1.AppendText( Environment.NewLine );


                 UInt32 pressure;
                 UInt16 temperature;

                 pressure = (UInt32)(xdata[ 41 ] * 256 * 256 * 256 + xdata[ 42 ] * 256 * 256 + xdata[ 43 ] * 256 + xdata[ 44 ]);

                 temperature = (UInt16)(xdata[ 45 ] * 256 + xdata[ 46 ]);

                 double temperature_c = ((double)temperature-27315) / 100;

                 richTextBox1.AppendText( "pressure=" + pressure.ToString() );
                 richTextBox1.AppendText( Environment.NewLine );
                 richTextBox1.AppendText( "temperature=" + temperature.ToString() + " centigrae:"+temperature_c.ToString("f") );

                 richTextBox1.AppendText( Environment.NewLine );
                 richTextBox1.AppendText( xdata[ 62 ].ToString("x") );

              }

      */

        void LowLevelReadD5(ushort index, byte[] xdata)
        {

            //byte[] xdata = new byte[ 64 ];

            ushort value = 0;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd5, value, index, 64, xdata);


        }


        private void button4_Click_1(object sender, EventArgs e)
        {
            LowLevelReadD4();
        }


        void clearDDR()
        {
            writeFPGA(63, 0);   //reset ddr
            writeFPGA(63, 1);
            writeFPGA(63, 0);
        }

        void resetDDR()
        {
            writeFPGA(1, 1);   //reset ddr
            writeFPGA(1, 0);
            writeFPGA(1, 1);   //reset ddr
        }

        void resetDDR_skipCheck()
        {
            writeFPGA(1, 1);   //reset ddr
            writeFPGA(1, 2);
            writeFPGA(1, 1);   //reset ddr
        }


        byte crc4_PT(UInt16[] n_prom) // n_prom defined as 8x unsigned int (n_prom[8])
        {
            int cnt; // simple counter
            UInt32 n_rem = 0; // crc remainder
            byte n_bit;
            n_prom[0] = (UInt16)(((n_prom[0]) & 0x0FFF)); // CRC byte is replaced by 0 
            n_prom[7] = 0; // Subsidiary value, set to 0
            for (cnt = 0; cnt < 16; cnt++) // operation is performed on bytes
            { // choose LSB or MSB
                if (cnt % 2 == 1) n_rem ^= (UInt16)((n_prom[cnt >> 1]) & 0x00FF);

                else n_rem ^= (UInt16)(n_prom[cnt >> 1] >> 8);
                for (n_bit = 8; n_bit > 0; n_bit--)
                {
                    if ((n_rem & (0x8000)) == 0x0001) n_rem = (n_rem << 1) ^ 0x3000;
                    else n_rem = (n_rem << 1);
                }
            }
            n_rem = ((n_rem >> 12) & 0x000F); // final 4-bit remainder is CRC code
            return (byte)(n_rem);
        }


        private void button6_Click_1(object sender, EventArgs e)
        {
            clearDDR();
        }


        void resetFrameCounter()
        {


            writeFPGA(82, 1);
            writeFPGA(82, 0);

        }



        void BurstModeRun()
        {


                    setIDLE();
                    //rollingsetIDLE();
                    Thread.Sleep(200);
                    releaseIDLE();
                    //rollingreleaseIDLE();
        }


        private void buttonSingleCapture_Click(object sender, EventArgs e)
        {
            BurstModeRun();
        }



        void setPatchNumber(UInt32 value)
        {       
                writeFPGA(41, MSB3(value));
                writeFPGA(42, MSB2(value));
                writeFPGA(43, MSB1(value));
                writeFPGA(44, MSB0(value));
           
        }

        private void buttonClearFrameCounter_Click(object sender, EventArgs e)
        {
                writeFPGA(1, 1);
                writeFPGA(1, 0);
                writeFPGA(1, 1);
                

        }

        void enableStampFrameCounter(bool i)
        {
                if (i == true)
                    writeFPGA(56, 1);
                else
                    writeFPGA(56, 0);

        }

        private void button11_Click_2(object sender, EventArgs e)
        {
            writeCMOS(21, 0xdf);
            byte[] WORDPOS = new byte[8];

            Thread.Sleep(100);


            ushort i;
            for (i = 0; i < 8; i++)
            {
                writeFPGA(88, i);
                WORDPOS[i] = readFPGA(32);

            }


            richTextBox1.Clear();

            for (i = 0; i < 8; i++)
            {
                richTextBox1.AppendText(WORDPOS[i].ToString() + " ");
            }

            writeCMOS(21, 0Xde);
        }





        private void button14_Click_1(object sender, EventArgs e)
        {


            ushort laneSyncNumber = 99;

            ushort[] SyncEdgeScan = new ushort[23];
            EnablePartTrain(true);

            richTextBox1.Clear();
            richTextBox1.AppendText("Search 098E posiiton in part train mode" + Environment.NewLine);
            richTextBox1.AppendText("Let assume the SYNC is in safe position (setInputDelay=0). So we can find 098E" + Environment.NewLine);
            SetInputDelay(laneSyncNumber, 0);
            Thread.Sleep(100);

            byte[] PIX = new byte[6];
            bool success = false;
            byte TrainCodePos = 0;

            for (byte i = 0; i < 6; i++)
            {
                PIX[i] = readFPGA((ushort)(33 + i));
                richTextBox1.AppendText(PIX[i].ToString("x") + " ");
            }

            richTextBox1.AppendText(Environment.NewLine);


            for (byte i = 0; i < 6; i++)
            {
                if (PIX[i] == 0X98)
                {
                    richTextBox1.AppendText("Find Train Position:" + i.ToString() + " ");
                    TrainCodePos = i;
                    success = true;
                }
            }

            richTextBox1.AppendText(Environment.NewLine);
            richTextBox1.AppendText(success.ToString());
            richTextBox1.AppendText(Environment.NewLine);

            Application.DoEvents();

            if (success == false)
            {
                richTextBox1.AppendText("Since we can not find the 98E. we assume the position is not good. Now change a position(pos=3) to try again" + Environment.NewLine);

                SetInputDelay(laneSyncNumber, 3);
                Thread.Sleep(100);


                for (int i = 0; i < 6; i++)
                {
                    PIX[i] = readFPGA((ushort)(33 + i));
                    richTextBox1.AppendText(PIX[i].ToString("x") + " ");
                }

                richTextBox1.AppendText(Environment.NewLine);


                for (byte i = 0; i < 6; i++)
                {
                    if (PIX[i] == 0X98)
                    {
                        richTextBox1.AppendText("Find Train Position:" + i.ToString() + " ");
                        TrainCodePos = i;
                        success = true;
                    }
                }

                richTextBox1.AppendText(Environment.NewLine);
                richTextBox1.AppendText(success.ToString());
                richTextBox1.AppendText(Environment.NewLine);
                Application.DoEvents();


            }





            if (success == true)
            {

                richTextBox1.AppendText("now begin to scan sync phase based on the train code in PIX:" + TrainCodePos.ToString());
                richTextBox1.AppendText(Environment.NewLine);

                Thread.Sleep(100);

                ushort j;
                for (j = 0; j < 23; j++)
                {

                    SetInputDelay(laneSyncNumber, j);

                    // writeFPGA( 88, 1 );                       //select channel
                    Thread.Sleep(50);
                    //PhaseErrorScan[i][j] =  readFPGA( 30 ) * 256 + readFPGA( 31 ) ;        //readout data
                    SyncEdgeScan[j] = (ushort)(readFPGA((ushort)(33 + TrainCodePos)));
                }




                for (j = 0; j < 23; j++)
                {
                    richTextBox1.AppendText(SyncEdgeScan[j].ToString("x") + " ");
                }


                richTextBox1.AppendText(Environment.NewLine);
                for (j = 0; j < 21; j++)
                {
                    if (SyncEdgeScan[j] == 0x98 && SyncEdgeScan[j + 1] == 0x98 && SyncEdgeScan[j + 2] == 0x98)
                    {
                        SetInputDelay(laneSyncNumber, (ushort)(j + 1));
                        richTextBox1.AppendText("SET SYNC INPUT DELAY:" + (j + 1).ToString());
                        j = 22;
                    }
                }


            }

            else
            {
                SetInputDelay(laneSyncNumber, 0);
                richTextBox1.AppendText("sync phase correction not success:");
            }

            EnablePartTrain(false);
        }


        private void button13_Click_1(object sender, EventArgs e)
        {
            resetCMOS();
        }


        void initCMOS_IMX455(UInt32 mode)
        {

            resetCMOS();

            writeFPGA(0x0, 0x0);             //reg0置1， CMOS复位信号XCLR拉高
            writeFPGA(0x0, 0x1);             //reg0置1， CMOS复位信号XCLR拉高
            Thread.Sleep(100);              //Accroding to Power-ON sequence Figure on page 254 in the Datasheet of IMX411ALR, Register setting should be delayed at least 10us.
            Thread.Sleep(100);


            //writeCMOS(0x06, 0x53);        //测试writeCMOS函数，测试寄存器读写
            writeCMOS(0x019E, 0x01);        //PWR_AUTO = 1h
            writeCMOS(0x0000, 0x04);        //WAKEUP = 1h

            //All-pixel readout mode 16bit ADC

            if (mode == 0x0a)
            {

                writeCMOS(0x0000, 0x04);
                writeCMOS(0x0001, 0x00);
                writeCMOS(0x0002, 0x10);
                writeCMOS(0x0006, 0x1D);
                writeCMOS(0x0016, 0x02);
                writeCMOS(0x0025, 0x0A);
                writeCMOS(0x0028, 0x0A);
                writeCMOS(0x0046, 0x03);
                writeCMOS(0x004F, 0x08);
                writeCMOS(0x0067, 0x30);
                writeCMOS(0x00A9, 0x00);
                writeCMOS(0x00C6, 0x08);
                writeCMOS(0x00CC, 0x8A);
                writeCMOS(0x00CE, 0x8A);
                writeCMOS(0x00D1, 0x92);
                writeCMOS(0x00D3, 0x92);
                writeCMOS(0x00DA, 0x31);
                writeCMOS(0x0112, 0x04);
                writeCMOS(0x0113, 0x00);        // Without CRC and ECC insertion
                writeCMOS(0x019E, 0x01);
                writeCMOS(0x01A0, 0x06);
                writeCMOS(0x03A0, 0x0F);
                writeCMOS(0x03A2, 0x07);
                writeCMOS(0x03A3, 0x11);
                writeCMOS(0x03A4, 0x11);
                writeCMOS(0x03A5, 0x11);
                writeCMOS(0x03A6, 0x11);
                writeCMOS(0x048F, 0xCD);
                writeCMOS(0x0498, 0xCD);
                writeCMOS(0x04CB, 0x02);
                writeCMOS(0x050F, 0x6E);
                writeCMOS(0x0517, 0x6E);
                writeCMOS(0x051F, 0x7D);
                writeCMOS(0x0553, 0xCC);
                writeCMOS(0x0573, 0x00);
                writeCMOS(0x0574, 0x02);
                writeCMOS(0x0575, 0x02);
                writeCMOS(0x0576, 0x02);
                writeCMOS(0x0577, 0x02);
                writeCMOS(0x0581, 0x00);
                writeCMOS(0x0582, 0x1C);
                writeCMOS(0x0583, 0x1C);
                writeCMOS(0x0584, 0x1C);
                writeCMOS(0x0585, 0x1C);
                writeCMOS(0x0586, 0x10);
                writeCMOS(0x0587, 0x10);
                writeCMOS(0x0588, 0x10);
                writeCMOS(0x0589, 0x10);
                writeCMOS(0x059A, 0x00);
                writeCMOS(0x05A1, 0x6E);
                writeCMOS(0x05A8, 0x6E);
                writeCMOS(0x05AF, 0x7D);
                writeCMOS(0x0603, 0x6E);
                writeCMOS(0x0605, 0x6E);
                writeCMOS(0x062A, 0xE0);
                writeCMOS(0x0630, 0xDE);
                writeCMOS(0x0646, 0xD1);
                writeCMOS(0x064A, 0xD1);
                writeCMOS(0x066D, 0x33);
                writeCMOS(0x066E, 0x11);
                writeCMOS(0x0670, 0x33);
                writeCMOS(0x0671, 0x11);
                writeCMOS(0x0674, 0x11);
                writeCMOS(0x0677, 0x11);
                writeCMOS(0x067E, 0x06);
                writeCMOS(0x07D0, 0x06);
                writeCMOS(0x07D1, 0x0B);
                writeCMOS(0x07D3, 0x06);
                writeCMOS(0x07D4, 0x0B);
                writeCMOS(0x07D6, 0x06);
                writeCMOS(0x0A80, 0x82);
                writeCMOS(0x0A81, 0x02);
                writeCMOS(0x0A82, 0x8E);
                writeCMOS(0x0A83, 0x07);
                writeCMOS(0x0A84, 0xD2);
                writeCMOS(0x0A85, 0x09);
                writeCMOS(0x0A86, 0x60);
                writeCMOS(0x0A87, 0x11);
                writeCMOS(0x0A88, 0x03);
                writeCMOS(0x0A96, 0x01);

                //writeCMOS(0x00C4, 0x03);//TEST mode  shading mode 

            }

            else if (mode == 0x0b)
            {

                writeCMOS(0x0000, 0x04);
                writeCMOS(0x0002, 0x10);
                writeCMOS(0x0006, 0x1D);
                writeCMOS(0x0016, 0x02);
                writeCMOS(0x0025, 0x0A);
                writeCMOS(0x0028, 0x0A);
                writeCMOS(0x0046, 0x03);
                writeCMOS(0x004F, 0x08);
                writeCMOS(0x00C6, 0x08);
                writeCMOS(0x00DA, 0x31);
                writeCMOS(0x019E, 0x01);
                writeCMOS(0x01A0, 0x06);
                writeCMOS(0x0113, 0x00);        // Without CRC and ECC insertion
                writeCMOS(0x03A0, 0x0F);
                writeCMOS(0x03A2, 0x07);
                writeCMOS(0x03A3, 0x11);
                writeCMOS(0x03A4, 0x11);
                writeCMOS(0x03A5, 0x11);
                writeCMOS(0x03A6, 0x11);
                writeCMOS(0x048F, 0xC7);
                writeCMOS(0x0498, 0xC7);
                writeCMOS(0x04CB, 0x02);
                writeCMOS(0x0573, 0x00);
                writeCMOS(0x0574, 0x02);
                writeCMOS(0x0575, 0x02);
                writeCMOS(0x0576, 0x02);
                writeCMOS(0x0577, 0x02);
                writeCMOS(0x0582, 0x18);
                writeCMOS(0x0583, 0x18);
                writeCMOS(0x0584, 0x18);
                writeCMOS(0x0585, 0x18);
                writeCMOS(0x0586, 0x10);
                writeCMOS(0x0587, 0x10);
                writeCMOS(0x0588, 0x10);
                writeCMOS(0x0589, 0x10);
                writeCMOS(0x059A, 0x00);
                writeCMOS(0x0673, 0x77);
                writeCMOS(0x0674, 0x77);
                writeCMOS(0x0676, 0x77);
                writeCMOS(0x0677, 0x77);
                writeCMOS(0x067E, 0x06);
                writeCMOS(0x07D0, 0x06);
                writeCMOS(0x07D1, 0x0B);
                writeCMOS(0x07D3, 0x06);
                writeCMOS(0x07D4, 0x0B);
                writeCMOS(0x07D6, 0x06);
                writeCMOS(0x0A80, 0x72);
                writeCMOS(0x0A81, 0x02);
                writeCMOS(0x0A82, 0x0E);
                writeCMOS(0x0A83, 0x04);
                writeCMOS(0x0A84, 0x52);
                writeCMOS(0x0A85, 0x06);
                writeCMOS(0x0A86, 0xB8);
                writeCMOS(0x0A87, 0x08);
                writeCMOS(0x0A88, 0x03);
                writeCMOS(0x0A96, 0x01);
            }


            else if (mode == 0x0c)
            {

                writeCMOS(0x0000, 0x04);
                writeCMOS(0x0001, 0x80);//////
                writeCMOS(0x0002, 0x10);
                writeCMOS(0x0006, 0x1D);
                writeCMOS(0x0016, 0x02);
                writeCMOS(0x0025, 0x0A);
                writeCMOS(0x0028, 0x0A);
                writeCMOS(0x0046, 0x03);
                writeCMOS(0x004F, 0x08);
                writeCMOS(0x00A9, 0x02);//////
                writeCMOS(0x00C6, 0x08);
                writeCMOS(0x00CC, 0x7E);//////
                writeCMOS(0x00CE, 0x7E);//////
                writeCMOS(0x00D1, 0x86);/////
                writeCMOS(0x00D3, 0x86);
                writeCMOS(0x00D4, 0x00);
                writeCMOS(0x00D5, 0x00);
                writeCMOS(0x00D7, 0x88);
                writeCMOS(0x00DA, 0x31);
                writeCMOS(0x0112, 0x02);
                writeCMOS(0x0113, 0x00);        // Without CRC and ECC insertion
                writeCMOS(0x019E, 0x01);
                writeCMOS(0x01A0, 0x06);
                writeCMOS(0x03A0, 0x0F);
                writeCMOS(0x03A2, 0x07);
                writeCMOS(0x03A3, 0x11);
                writeCMOS(0x03A4, 0x11);
                writeCMOS(0x03A5, 0x11);
                writeCMOS(0x03A6, 0x11);
                writeCMOS(0x048F, 0xC1);
                writeCMOS(0x0498, 0xC1);
                writeCMOS(0x04CB, 0x02);
                writeCMOS(0x0509, 0x9B);
                writeCMOS(0x050F, 0x62);
                writeCMOS(0x0510, 0x1B);
                writeCMOS(0x0512, 0xD2);
                writeCMOS(0x0513, 0xFF);
                writeCMOS(0x0514, 0xFF);
                writeCMOS(0x0515, 0x00);
                writeCMOS(0x0517, 0x62);
                writeCMOS(0x0518, 0x1B);
                writeCMOS(0x051A, 0xD2);
                writeCMOS(0x051B, 0xFF);
                writeCMOS(0x051C, 0xFF);
                writeCMOS(0x051F, 0x71);
                writeCMOS(0x0553, 0xC0);
                writeCMOS(0x0573, 0x00);
                writeCMOS(0x0574, 0x0D);
                writeCMOS(0x0575, 0x0D);
                writeCMOS(0x0576, 0x0D);
                writeCMOS(0x0577, 0x0D);
                writeCMOS(0x0581, 0x04);
                writeCMOS(0x0582, 0x1E);
                writeCMOS(0x0583, 0x1E);
                writeCMOS(0x0584, 0x1E);
                writeCMOS(0x0585, 0x1E);
                writeCMOS(0x0586, 0x10);
                writeCMOS(0x0587, 0x10);
                writeCMOS(0x0588, 0x10);
                writeCMOS(0x0589, 0x10);
                writeCMOS(0x05A1, 0x62);
                writeCMOS(0x05A2, 0x1B);
                writeCMOS(0x05A4, 0xD2);
                writeCMOS(0x05A5, 0xFF);
                writeCMOS(0x05A6, 0xFF);
                writeCMOS(0x05A8, 0x62);
                writeCMOS(0x05A9, 0x1B);
                writeCMOS(0x05AB, 0xD2);
                writeCMOS(0x05AC, 0xFF);
                writeCMOS(0x05AD, 0xFF);
                writeCMOS(0x05AF, 0x71);
                writeCMOS(0x0603, 0x62);
                writeCMOS(0x0605, 0x62);
                writeCMOS(0x062A, 0xD4);
                writeCMOS(0x0630, 0xD2);
                writeCMOS(0x0646, 0xC5);
                writeCMOS(0x064A, 0xC5);
                writeCMOS(0x066D, 0x00);
                writeCMOS(0x066E, 0x00);
                writeCMOS(0x0670, 0x00);
                writeCMOS(0x0671, 0x00);
                writeCMOS(0x0673, 0x00);
                writeCMOS(0x0674, 0x00);
                writeCMOS(0x0676, 0x00);
                writeCMOS(0x0677, 0x00);
                writeCMOS(0x0679, 0x07);
                writeCMOS(0x067E, 0x06);
                writeCMOS(0x068A, 0x88);
                writeCMOS(0x07D0, 0x06);
                writeCMOS(0x07D1, 0x0B);
                writeCMOS(0x07D3, 0x06);
                writeCMOS(0x07D4, 0x0B);
                writeCMOS(0x07D6, 0x06);
                writeCMOS(0x0A80, 0x62);
                writeCMOS(0x0A81, 0x02);
                writeCMOS(0x0A82, 0xE6);
                writeCMOS(0x0A83, 0x02);
                writeCMOS(0x0A84, 0x78);
                writeCMOS(0x0A85, 0x04);
                writeCMOS(0x0A86, 0x3A);
                writeCMOS(0x0A87, 0x05);
                writeCMOS(0x0A88, 0x03);
                writeCMOS(0x0A96, 0x01);



            }






            writeCMOS(0x4F, 0x08);          //SDO_ACT = 1;SDO输出使能


            setIDLE();
            setHMAX(5000);
            setVMAX(7000);

            releaseIDLE();

        }


        void initCMOS_IMX571(byte mode)
        {
            //writeFPGA( 57, 0 );               //sf_flag = 0;

            resetCMOS();

            writeFPGA(0x0, 0x0);
            writeFPGA(0x0, 0x1);             //reg0置1， CMOS复位信号XCLR拉高
            Thread.Sleep(200);              //Accroding to Power-ON sequence Figure on page 254 in the Datasheet of IMX411ALR, Register setting should be delayed at least 10us.
            // Thread.Sleep( 100 );


            //writeCMOS(0x06, 0x53);        //测试writeCMOS函数，测试寄存器读写
            writeCMOS(0x019E, 0x01);        //PWR_AUTO = 1h
            writeCMOS(0x0000, 0x04);        //WAKEUP = 1h

            if (mode == 0x0a)
            {
                //All-pixel readout mode 16bit ADC
                writeCMOS(0x0000, 0x04);
                writeCMOS(0x0002, 0x80);
                writeCMOS(0x0003, 0x10);
                writeCMOS(0x0018, 0x01);
                writeCMOS(0x0027, 0x06);
                writeCMOS(0x002A, 0x0A);
                writeCMOS(0x0051, 0x08);
                writeCMOS(0x0069, 0x30);
                writeCMOS(0x006C, 0xE6);
                writeCMOS(0x006D, 0x00);
                writeCMOS(0x00D3, 0x08);
                writeCMOS(0x01EE, 0x01);
                writeCMOS(0x0400, 0x0E);
                writeCMOS(0x0454, 0x22);
                writeCMOS(0x0456, 0x22);
                writeCMOS(0x0559, 0x19);
                writeCMOS(0x055A, 0x17);
                writeCMOS(0x055C, 0x19);
                writeCMOS(0x055D, 0x17);
                writeCMOS(0x055F, 0x20);
                writeCMOS(0x0560, 0x1E);
                writeCMOS(0x0562, 0x20);
                writeCMOS(0x0563, 0x1E);
                writeCMOS(0x056B, 0x27);
                writeCMOS(0x056C, 0x25);
                writeCMOS(0x056E, 0x20);
                writeCMOS(0x056F, 0x1E);
                writeCMOS(0x0573, 0x00);
                writeCMOS(0x0574, 0x02);
                writeCMOS(0x0575, 0x02);
                writeCMOS(0x0576, 0x02);
                writeCMOS(0x0577, 0x02);
                writeCMOS(0x0581, 0x00);
                writeCMOS(0x0582, 0x16);
                writeCMOS(0x0583, 0x16);
                writeCMOS(0x0584, 0x16);
                writeCMOS(0x0585, 0x16);
                writeCMOS(0x0590, 0x01);
                writeCMOS(0x0596, 0x19);
                writeCMOS(0x0597, 0x14);
                writeCMOS(0x0598, 0x20);
                writeCMOS(0x0599, 0x1B);
                writeCMOS(0x059A, 0x00);
                writeCMOS(0x0600, 0x1C);
                writeCMOS(0x062A, 0x8B);
                writeCMOS(0x0630, 0x89);
                writeCMOS(0x0635, 0x19);
                writeCMOS(0x0636, 0x15);
                writeCMOS(0x0637, 0x20);
                writeCMOS(0x0638, 0x15);
                writeCMOS(0x063A, 0x19);
                writeCMOS(0x063B, 0x15);
                writeCMOS(0x063C, 0x20);
                writeCMOS(0x063D, 0x15);
                writeCMOS(0x063F, 0x19);
                writeCMOS(0x0640, 0x15);
                writeCMOS(0x0641, 0x20);
                writeCMOS(0x0642, 0x15);
                writeCMOS(0x066D, 0x11);
                writeCMOS(0x066E, 0x11);
                writeCMOS(0x0670, 0x11);
                writeCMOS(0x0671, 0x11);
                writeCMOS(0x0673, 0x11);
                writeCMOS(0x0674, 0x11);
                writeCMOS(0x0676, 0x11);
                writeCMOS(0x0677, 0x11);
                writeCMOS(0x07CC, 0x0A);
                writeCMOS(0x0A2F, 0x8F);
                writeCMOS(0x0A30, 0x01);
                writeCMOS(0x0A31, 0x8F);
                writeCMOS(0x0A32, 0x01);
                writeCMOS(0x0A36, 0x8F);
                writeCMOS(0x0A37, 0x01);
            }
            else if (mode == 0x0b)//14bit
            {
                writeCMOS(0x0000, 0x04);
                writeCMOS(0x0003, 0x10);
                writeCMOS(0x0018, 0x01);
                writeCMOS(0x0027, 0x06);
                writeCMOS(0x002A, 0x0A);
                writeCMOS(0x0051, 0x08);
                writeCMOS(0x00D3, 0x08);
                writeCMOS(0x00DF, 0x00);
                writeCMOS(0x01EE, 0x01);
                writeCMOS(0x0400, 0x0E);
                writeCMOS(0x0454, 0x22);
                writeCMOS(0x0456, 0x22);
                writeCMOS(0x0512, 0x80);
                writeCMOS(0x0513, 0x80);
                writeCMOS(0x0514, 0x80);
                writeCMOS(0x0573, 0x00);
                writeCMOS(0x0582, 0x21);
                writeCMOS(0x0583, 0x21);
                writeCMOS(0x0584, 0x21);
                writeCMOS(0x0585, 0x21);
                writeCMOS(0x0590, 0x01);
                writeCMOS(0x059A, 0x00);
                writeCMOS(0x0600, 0x1C);
                writeCMOS(0x0673, 0x77);
                writeCMOS(0x0674, 0x77);
                writeCMOS(0x0676, 0x77);
                writeCMOS(0x0677, 0x77);
                writeCMOS(0x07CC, 0x0A);
            }
            else if (mode == 0x0c)//12bit
            {

                writeCMOS(0x0000, 0x04);
                writeCMOS(0x0002, 0x54);
                writeCMOS(0x0003, 0x10);
                writeCMOS(0x0018, 0x01);
                writeCMOS(0x0027, 0x06);
                writeCMOS(0x002A, 0x0A);
                writeCMOS(0x0051, 0x08);
                writeCMOS(0x00D3, 0x08);
                writeCMOS(0x00D6, 0x53);
                writeCMOS(0x00D8, 0x53);
                writeCMOS(0x00DB, 0x5A);
                writeCMOS(0x00DD, 0x5A);
                writeCMOS(0x00DF, 0x00);
                writeCMOS(0x00E2, 0x88);
                writeCMOS(0x01EE, 0x01);
                writeCMOS(0x0400, 0x0E);
                writeCMOS(0x0454, 0x22);
                writeCMOS(0x0456, 0x22);
                writeCMOS(0x050F, 0x59);
                writeCMOS(0x0512, 0xBF);
                writeCMOS(0x0513, 0xBF);
                writeCMOS(0x0514, 0xBF);
                writeCMOS(0x0515, 0x00);
                writeCMOS(0x0517, 0x50);
                writeCMOS(0x051F, 0x5F);
                writeCMOS(0x0553, 0x7B);
                writeCMOS(0x0559, 0x19);
                writeCMOS(0x055A, 0x17);
                writeCMOS(0x055C, 0x19);
                writeCMOS(0x055D, 0x17);
                writeCMOS(0x055F, 0x20);
                writeCMOS(0x0560, 0x1E);
                writeCMOS(0x0562, 0x20);
                writeCMOS(0x0563, 0x1E);
                writeCMOS(0x056B, 0x27);
                writeCMOS(0x056C, 0x25);
                writeCMOS(0x056E, 0x20);
                writeCMOS(0x056F, 0x1E);
                writeCMOS(0x0573, 0x00);
                writeCMOS(0x0574, 0x0F);
                writeCMOS(0x0575, 0x0F);
                writeCMOS(0x0576, 0x0F);
                writeCMOS(0x0577, 0x0F);
                writeCMOS(0x0581, 0x04);
                writeCMOS(0x0582, 0x24);
                writeCMOS(0x0583, 0x24);
                writeCMOS(0x0584, 0x24);
                writeCMOS(0x0585, 0x24);
                writeCMOS(0x0590, 0x01);
                writeCMOS(0x0596, 0x19);
                writeCMOS(0x0597, 0x14);
                writeCMOS(0x0598, 0x20);
                writeCMOS(0x0599, 0x1B);
                writeCMOS(0x0600, 0x1C);
                writeCMOS(0x0603, 0x4B);
                writeCMOS(0x0605, 0x4B);
                writeCMOS(0x062A, 0x81);
                writeCMOS(0x062C, 0x52);
                writeCMOS(0x0630, 0x7F);
                writeCMOS(0x0632, 0x52);
                writeCMOS(0x0635, 0x19);
                writeCMOS(0x0636, 0x15);
                writeCMOS(0x0637, 0x20);
                writeCMOS(0x0638, 0x15);
                writeCMOS(0x063A, 0x19);
                writeCMOS(0x063B, 0x15);
                writeCMOS(0x063C, 0x20);
                writeCMOS(0x063D, 0x15);
                writeCMOS(0x063F, 0x19);
                writeCMOS(0x0640, 0x15);
                writeCMOS(0x0641, 0x20);
                writeCMOS(0x0642, 0x15);
                writeCMOS(0x0646, 0x7B);
                writeCMOS(0x064A, 0x7B);
                writeCMOS(0x066D, 0x00);
                writeCMOS(0x066E, 0x11);
                writeCMOS(0x0670, 0x00);
                writeCMOS(0x0671, 0x11);
                writeCMOS(0x0673, 0x00);
                writeCMOS(0x0674, 0x11);
                writeCMOS(0x0676, 0x00);
                writeCMOS(0x0677, 0x11);
                writeCMOS(0x067E, 0x04);
                writeCMOS(0x068A, 0x88);
                writeCMOS(0x07CC, 0x0A);
            }

            writeCMOS(0X0113, 0X00); //disable ECC
            writeCMOS(0x51, 0x08);          //SDO_ACT = 1;SDO输出使能



            setIDLE();
            setHMAX(7000);
            setVMAX(7000);

            releaseIDLE();

        }


        void initCMOS_IMX410()
        {

            resetCMOS();


            writeFPGA(0x0, 0x0);             //reg0置0， CMOS复位信号XCLR拉低复位
            writeFPGA(0x0, 0x1);             //reg0置1， CMOS复位信号XCLR拉高
            Thread.Sleep(100);              //Accroding to Power-ON sequence Figure on page 114 in the Datasheet of IMX410CQK, Register setting should be delayed at least 10us.


            writeCMOS(0x00E9, 0x80);
            writeCMOS(0x00D9, 0x60);
            writeCMOS(0x04E6, 0x00);
            Thread.Sleep(100);


            writeCMOS(0x00C5, 0x00);
            writeCMOS(0x00C6, 0x00);
            writeCMOS(0x0000, 0X04);
            writeCMOS(0x00CA, 0x01);
            Thread.Sleep(100);


            //All-pixel readout mode 16bit ADC
            writeCMOS(0x0001, 0x29);
            writeCMOS(0x0005, 0x02);
            writeCMOS(0x0016, 0x07);
            writeCMOS(0x001C, 0x05);
            writeCMOS(0x0043, 0x02);
            writeCMOS(0x0058, 0x70);
            writeCMOS(0x0089, 0x66);
            writeCMOS(0x00C5, 0x00);
            writeCMOS(0x00C6, 0x00);
            writeCMOS(0x00CA, 0x01);
            writeCMOS(0x00CE, 0x00);			//Without CRC and ECC insertion
            writeCMOS(0x00E9, 0x80);
            writeCMOS(0x041B, 0x03);
            writeCMOS(0x044D, 0x00);
            writeCMOS(0x04E6, 0x00);
            writeCMOS(0x0569, 0x09);
            writeCMOS(0x0590, 0x01);
            writeCMOS(0x0591, 0x03);
            writeCMOS(0x0592, 0x01);
            writeCMOS(0x0593, 0x03);
            writeCMOS(0x0594, 0x01);
            writeCMOS(0x0595, 0x03);
            writeCMOS(0x059A, 0x02);
            writeCMOS(0x059B, 0x02);
            writeCMOS(0x059C, 0x02);
            writeCMOS(0x059D, 0x02);
            writeCMOS(0x06D0, 0x01);
            writeCMOS(0x06D3, 0x01);
            writeCMOS(0x06D6, 0x01);
            writeCMOS(0x0716, 0x01);
            writeCMOS(0x0717, 0x02);
            writeCMOS(0x071E, 0x01);
            writeCMOS(0x0720, 0x01);
            writeCMOS(0x0727, 0x01);
            writeCMOS(0x0728, 0x3B);
            writeCMOS(0x072A, 0xE5);
            writeCMOS(0x072B, 0x07);
            writeCMOS(0x072C, 0x02);
            writeCMOS(0x0734, 0x01);
            writeCMOS(0x0735, 0x01);
            writeCMOS(0x0790, 0x01);
            writeCMOS(0x0792, 0x10);
            writeCMOS(0x0793, 0x01);
            writeCMOS(0x0794, 0x01);
            writeCMOS(0x0795, 0x01);
            writeCMOS(0x0796, 0x19);
            writeCMOS(0x0797, 0x10);
            writeCMOS(0x0798, 0x2C);
            writeCMOS(0x079A, 0x25);
            writeCMOS(0x079C, 0x3B);
            writeCMOS(0x079E, 0x08);
            writeCMOS(0x079F, 0x10);



            writeCMOS(0x00ce, 0x00);  //disable ECC 

            writeCMOS(0x0043, 0x02);          //SDO_ACT = 1;SDO输出使能

            setIDLE();
            setHMAX(5000);
            setVMAX(7000);

            releaseIDLE();


        }

        void initCMOS_IMX492()
        {


            resetCMOS();

            Thread.Sleep(100);

            writeFPGA(0x0, 0x0);             //reg0置0， CMOS复位信号XCLR拉低复位
            writeFPGA(0x0, 0x1);             //reg0置1， CMOS复位信号XCLR拉高
            Thread.Sleep(100);

            // writeCMOS(0x0000, 0x09);
            Thread.Sleep(100);


            setIDLE();
            setHMAX(12000);
            setVMAX(10000);

            releaseIDLE();




            /*
            setIDLE();
            setHMAX(15000);
            setVMAX(20000);

            releaseIDLE();
            */

            //stage one
            writeCMOS(0x0033, 0x10);
            writeCMOS(0x003c, 0x0);
            writeCMOS(0x01E8, 0x10);
            writeCMOS(0x01E9, 0x00);
            writeCMOS(0x0122, 0x00);
            writeCMOS(0x0129, 0x0C);
            writeCMOS(0x012A, 0x00);
            writeCMOS(0x011F, 0x01);
            writeCMOS(0x0123, 0x01);
            writeCMOS(0x0124, 0x01);
            writeCMOS(0x0125, 0x00);
            writeCMOS(0x0127, 0x06);
            writeCMOS(0x012D, 0x03);

            writeCMOS(0x0000, 0x1A);

            writeCMOS(0x010B, 0x00);






            Thread.Sleep(100);



            // stage two
            writeCMOS(0x0000, 0x0A);
            writeCMOS(0x05E5, 0x92);
            writeCMOS(0x05E5, 0x9A);
            writeCMOS(0x0000, 0x08);

            Thread.Sleep(100);



            //state three
            writeCMOS(0x0001, 0x11);

            ///
            writeCMOS(0x0003, 0x00);
            writeCMOS(0x0004, 0x1C);
            writeCMOS(0x0005, 0x06);
            writeCMOS(0x0006, 0x00);
            writeCMOS(0x0007, 0xA7);
            writeCMOS(0x000A, 0xA5);        // Analog gain setting. 000Ah[7:0] 000Bh[2:0]. setting range 000h-7A5h 
            writeCMOS(0x000B, 0x03);        //
            writeCMOS(0x000E, 0xFF);        // specifies the integration shutdown vertical period. seeting range: 0000h-FFFFh
            writeCMOS(0x000F, 0x00);        // specifies the integration shutdown vertical period. seeting range: 0000h-FFFFh
            writeCMOS(0x0012, 0x01);        // Digital gain setting 0h:0dB, 1h:+6dB, 2h: +12dB, 3h: +18dB, others: Prohibited  setting range:0h-3h
            //writeCMOS(0x0017, 0x *);
            writeCMOS(0x002C, 0xFF);        // specifies the integration start horizontal period
            writeCMOS(0x002D, 0x00);        // specifies the integration start horizontal period
            //writeCMOS(0x0033, 0x *);//*
            //writeCMOS(0x003C, 0x00);//*
            writeCMOS(0x0042, 0x77);        //Digital black level offset setting setting range: 0h-FFh
            writeCMOS(0x0043, 0x00);
            writeCMOS(0x0047, 0x02);
            writeCMOS(0x004E, 0x0B);
            writeCMOS(0x004F, 0x2A);
            writeCMOS(0x0052, 0xEE);
            writeCMOS(0x0062, 0x25);
            writeCMOS(0x0064, 0x78);
            writeCMOS(0x0065, 0x33);
            writeCMOS(0x0066, 0x64);
            writeCMOS(0x0067, 0x71);
            writeCMOS(0x0081, 0x00);
            writeCMOS(0x0084, 0x00);
            writeCMOS(0x0085, 0x00);
            writeCMOS(0x0086, 0x00);
            writeCMOS(0x0087, 0x00);
            writeCMOS(0x0088, 0x75);
            writeCMOS(0x008A, 0x09);
            writeCMOS(0x008C, 0x61);
            writeCMOS(0x00E5, 0x00);
            //writeCMOS(0x011F, 0x *);//*
            //writeCMOS(0x0122, 0x *);//*
            //writeCMOS(0x0123, 0x *);//*
            //writeCMOS(0x0124, 0x *);//*
            //writeCMOS(0x0125, 0x *);//*
            //writeCMOS(0x0127, 0x *);//*
            //writeCMOS(0x0129, 0x *);//*
            //writeCMOS(0x012A, 0x *);//*
            //writeCMOS(0x012D, 0x *);//*
            writeCMOS(0x0146, 0x00);
            //writeCMOS(0x01E8, 0x *);//*
            //writeCMOS(0x01E9, 0x *);//*
            writeCMOS(0x01F5, 0x01);
            writeCMOS(0x0234, 0x32);
            writeCMOS(0x0248, 0xBC);
            writeCMOS(0x0250, 0xBC);
            writeCMOS(0x0258, 0xBC);
            writeCMOS(0x0260, 0xBC);
            writeCMOS(0x0274, 0x13);
            writeCMOS(0x0276, 0x00);
            writeCMOS(0x0277, 0x00);
            writeCMOS(0x027C, 0x13);
            writeCMOS(0x027E, 0x00);
            writeCMOS(0x027F, 0x00);
            writeCMOS(0x0284, 0x13);
            writeCMOS(0x0286, 0x00);
            writeCMOS(0x0287, 0x00);
            writeCMOS(0x028C, 0x13);
            writeCMOS(0x028E, 0x00);
            writeCMOS(0x028F, 0x00);
            writeCMOS(0x02AE, 0x00);
            writeCMOS(0x02AF, 0x00);
            writeCMOS(0x02CA, 0x5A);
            writeCMOS(0x032C, 0x00);
            writeCMOS(0x032D, 0x00);
            writeCMOS(0x032F, 0x00);
            writeCMOS(0x034A, 0x00);
            writeCMOS(0x034B, 0x00);
            writeCMOS(0x034C, 0x01);
            writeCMOS(0x0352, 0x50);
            writeCMOS(0x0356, 0x4F);
            writeCMOS(0x035A, 0x79);
            writeCMOS(0x035E, 0x56);
            writeCMOS(0x0360, 0x6A);
            writeCMOS(0x036A, 0x56);
            writeCMOS(0x03D6, 0x79);
            writeCMOS(0x040C, 0x6E);
            writeCMOS(0x0448, 0x7E);
            writeCMOS(0x048E, 0x6F);
            writeCMOS(0x0492, 0x11);
            writeCMOS(0x04C4, 0x5A);
            writeCMOS(0x0506, 0x56);
            writeCMOS(0x050C, 0x56);
            writeCMOS(0x050E, 0x58);
            writeCMOS(0x053D, 0x10);
            writeCMOS(0x0549, 0x04);
            writeCMOS(0x055D, 0x03);
            writeCMOS(0x055E, 0x03);
            writeCMOS(0x0574, 0x56);
            writeCMOS(0x057F, 0x0C);
            writeCMOS(0x0580, 0x0A);
            writeCMOS(0x0581, 0x08);
            writeCMOS(0x0583, 0x72);
            writeCMOS(0x0587, 0x01);
            writeCMOS(0x05B6, 0x00);
            writeCMOS(0x05B7, 0x00);
            writeCMOS(0x05B8, 0x00);
            writeCMOS(0x05B9, 0x00);
            writeCMOS(0x05D0, 0x5E);
            writeCMOS(0x05D4, 0x63);

            //writeCMOS(0x05E5, 0x9A);//*

            writeCMOS(0x066A, 0x04);
            writeCMOS(0x066B, 0x04);
            writeCMOS(0x066C, 0x00);
            writeCMOS(0x066D, 0x00);
            writeCMOS(0x066E, 0x00);
            writeCMOS(0x066F, 0x00);
            writeCMOS(0x0670, 0x00);
            writeCMOS(0x0671, 0x05);
            writeCMOS(0x0676, 0x83);
            writeCMOS(0x0677, 0x03);
            writeCMOS(0x0678, 0x00);
            writeCMOS(0x0679, 0x04);
            writeCMOS(0x067A, 0x2C);
            writeCMOS(0x067B, 0x05);
            writeCMOS(0x067D, 0x06);
            writeCMOS(0x067E, 0xFF);
            writeCMOS(0x067F, 0x06);
            writeCMOS(0x0680, 0x4B);
            writeCMOS(0x0688, 0x05);
            writeCMOS(0x0690, 0x27);
            writeCMOS(0x0692, 0x65);
            writeCMOS(0x0694, 0x4F);
            writeCMOS(0x0696, 0xA1);
            writeCMOS(0x06BC, 0x00);
            writeCMOS(0x06BD, 0x00);
            writeCMOS(0x071C, 0x02);
            writeCMOS(0x072F, 0x3C);
            writeCMOS(0x0730, 0x01);
            writeCMOS(0x0732, 0xB8);
            writeCMOS(0x0734, 0x4A);
            writeCMOS(0x0736, 0x57);
            writeCMOS(0x0738, 0x4D);
            writeCMOS(0x0744, 0x0F);
            writeCMOS(0x075B, 0x01);
            writeCMOS(0x082B, 0x68);
            writeCMOS(0x0836, 0x34);
            writeCMOS(0x08B3, 0x00);



            writeCMOS(0x00F5, 0x00);













            writeCMOS(0x0AC4, 0x00);
            writeCMOS(0x0C08, 0x3F);
            writeCMOS(0x0C0C, 0x1B);
            writeCMOS(0x0E80, 0x14);
            writeCMOS(0x0E82, 0x30);
            writeCMOS(0x0E84, 0x04);
            writeCMOS(0x0E85, 0x01);
            writeCMOS(0x0E86, 0x10);
            writeCMOS(0x0E87, 0x16);
            writeCMOS(0x0E88, 0x03);
            writeCMOS(0x0E89, 0xFE);
            writeCMOS(0x0E8A, 0x01);
            writeCMOS(0x0E8B, 0x06);
            writeCMOS(0x0E8E, 0x03);
            writeCMOS(0x0E8F, 0xFE);
            writeCMOS(0x0E90, 0x01);
            writeCMOS(0x0E91, 0x06);
            writeCMOS(0x0E94, 0x33);
            writeCMOS(0x0E95, 0x01);
            writeCMOS(0x0E96, 0x19);
            writeCMOS(0x0E98, 0x30);
            writeCMOS(0x0E9A, 0x09);
            writeCMOS(0x0E9C, 0x10);
            writeCMOS(0x0E9D, 0x16);
            writeCMOS(0x0E9E, 0xFE);
            writeCMOS(0x0E9F, 0x03);
            writeCMOS(0x0EA0, 0x06);
            writeCMOS(0x0EA3, 0x01);
            writeCMOS(0x0EA4, 0xFE);
            writeCMOS(0x0EA5, 0x03);
            writeCMOS(0x0EA6, 0x06);
            writeCMOS(0x0EA9, 0x33);
            writeCMOS(0x0EAA, 0x00);
            writeCMOS(0x0EAB, 0x08);
            writeCMOS(0x0EAC, 0x08);
            writeCMOS(0x0EAD, 0x01);
            writeCMOS(0x0EAE, 0x08);
            writeCMOS(0x0EAF, 0x08);
            writeCMOS(0x0EB0, 0x00);
            writeCMOS(0x0EB1, 0x10);
            writeCMOS(0x0EB2, 0x10);
            writeCMOS(0x0EB3, 0x01);
            writeCMOS(0x0EB4, 0x10);
            writeCMOS(0x0EB5, 0x10);
            writeCMOS(0x0EB6, 0x00);
            writeCMOS(0x0EB7, 0x00);
            writeCMOS(0x0EB8, 0x00);
            writeCMOS(0x0EB9, 0x00);
            writeCMOS(0x0EBA, 0x00);
            writeCMOS(0x0EBB, 0x00);
            writeCMOS(0x0EC0, 0x54);
            writeCMOS(0x0ECC, 0x04);
            writeCMOS(0x0ECD, 0x04);
            writeCMOS(0x0ED0, 0xF0);
            writeCMOS(0x0ED1, 0x20);
            writeCMOS(0x0ED2, 0x0B);
            writeCMOS(0x0ED3, 0x04);
            writeCMOS(0x0ED5, 0x13);
            writeCMOS(0x0ED6, 0x00);
            writeCMOS(0x0ED9, 0x0F);
            writeCMOS(0x0EE4, 0x02);
            writeCMOS(0x0EE5, 0x02);
            writeCMOS(0x0EE7, 0x00);
            writeCMOS(0x0EF6, 0x00);
            writeCMOS(0x0EF8, 0x10);
            writeCMOS(0x0EFA, 0x00);
            writeCMOS(0x0EFC, 0x10);


            Thread.Sleep(100);


            Thread.Sleep(100);

            writeCMOS(0x0000, 0x01);


            Thread.Sleep(100);
            Thread.Sleep(100);
            Thread.Sleep(100);
            Thread.Sleep(100);
            writeCMOS(0x0000, 0x00);
            Thread.Sleep(100);

            setIDLE();
            setHMAX(2000);
            setVMAX(8000);

            releaseIDLE();

        }








        void initCMOS_IMX533()
        {
            resetCMOS();

            writeFPGA(0x0, 0x0);             //reg0置1， CMOS复位信号XCLR拉高
            writeFPGA(0x0, 0x1);             //reg0置1， CMOS复位信号XCLR拉高
            Thread.Sleep(100);              //Accroding to Power-ON sequence Figure on page 254 in the Datasheet of IMX411ALR, Register setting should be delayed at least 10us.
            Thread.Sleep(100);



            /*
Standby Cancel Sequence
After the power-on sequence is performed, this sensor is in standby mode. Follow the sequence below 
to cancel standby and start normal operation. Also perform the same sequence after shifting from 
normal operation to standby, and you want to go back to normal operation later.

1.After 10 µs or more from XCLR=H, perform the following register settings. 
	1-1. Set address 019Eh to 01h (PWR_AUTO=1)
	1-2. (Option) Set half rate setting when baud rate of 1.152 Gbps is used. 
	1-3. Set address 0000h to 04h (STANDBY=0 and WAKEUP=1)
	1-4. Set initial settings. 
	1-5. Set mode settings.
*/









            //1-1. Set address 019Eh to 01h (PWR_AUTO=1)
            //1-2. (Option) Set half rate setting when baud rate of 1.152 Gbps is used. 
            //1-3. Set address 0000h to 04h (STANDBY=0 and WAKEUP=1)
            writeCMOS(0x019E, 0x01);
            //writeCMOS(0x0133, 0x8D);		//half rate
            //writeCMOS(0x0368, 0xE1);		//half rate
            writeCMOS(0x0000, 0x04);



            //1-4. Set initial settings. 
            writeCMOS(0x0028, 0x04);
            writeCMOS(0x0029, 0x00);
            writeCMOS(0x00C6, 0x08);
            writeCMOS(0x01C0, 0x0A);
            writeCMOS(0x01C5, 0x12);
            writeCMOS(0x01C6, 0x12);
            writeCMOS(0x01C9, 0xDF);
            writeCMOS(0x04AA, 0x03);
            writeCMOS(0x04AB, 0x28);
            writeCMOS(0x04CF, 0x02);
            writeCMOS(0x067A, 0x33);


            //1-5. Set mode settings.	mode0 All-pixel scan mode (14-bit)
            writeCMOS(0x0001, 0x40);

            writeCMOS(0x0025, 0x0A);

            writeCMOS(0x0052, 0x9B);
            writeCMOS(0x0053, 0x04);
            writeCMOS(0x0058, 0x8A);
            writeCMOS(0x0059, 0x04);
            writeCMOS(0x005A, 0x5F);
            writeCMOS(0x005B, 0x00);
            writeCMOS(0x0060, 0xE0);
            writeCMOS(0x0061, 0x05);
            writeCMOS(0x00AB, 0x01);
            writeCMOS(0x00CC, 0x3C);
            writeCMOS(0x00D1, 0x44);
            writeCMOS(0x00D5, 0x01);
            writeCMOS(0x00D7, 0x20);
            writeCMOS(0x0111, 0x00);

            writeCMOS(0x0112, 0x03);

            writeCMOS(0x0113, 0x00);        //Without CRC insertion Without ECC insertion

            writeCMOS(0x02F0, 0x89);
            writeCMOS(0x02F4, 0x57);
            writeCMOS(0x02F6, 0x78);
            writeCMOS(0x040E, 0x06);
            writeCMOS(0x041F, 0x66);
            writeCMOS(0x048F, 0x5A);
            writeCMOS(0x049A, 0x5A);
            writeCMOS(0x04C3, 0x00);
            writeCMOS(0x04C7, 0x00);
            writeCMOS(0x0509, 0x55);
            writeCMOS(0x050F, 0x34);
            writeCMOS(0x0512, 0x70);
            writeCMOS(0x0513, 0x70);
            writeCMOS(0x0514, 0x70);
            writeCMOS(0x051F, 0x43);
            writeCMOS(0x0553, 0x5A);
            writeCMOS(0x055C, 0x37);
            writeCMOS(0x055D, 0x35);
            writeCMOS(0x0562, 0x37);
            writeCMOS(0x0563, 0x35);
            writeCMOS(0x056B, 0x3E);
            writeCMOS(0x056C, 0x3C);
            writeCMOS(0x056E, 0x3E);
            writeCMOS(0x056F, 0x3C);
            writeCMOS(0x0575, 0x02);
            writeCMOS(0x0581, 0x02);
            writeCMOS(0x0583, 0x21);
            writeCMOS(0x0596, 0x37);
            writeCMOS(0x0597, 0x32);
            writeCMOS(0x0598, 0x37);
            writeCMOS(0x0599, 0x32);
            writeCMOS(0x059A, 0x00);
            writeCMOS(0x0603, 0x34);
            writeCMOS(0x062A, 0x87);
            writeCMOS(0x0630, 0x85);
            writeCMOS(0x0635, 0x37);
            writeCMOS(0x0636, 0x33);
            writeCMOS(0x0637, 0x37);
            writeCMOS(0x0638, 0x33);
            writeCMOS(0x063A, 0x37);
            writeCMOS(0x063B, 0x33);
            writeCMOS(0x063C, 0x37);
            writeCMOS(0x063D, 0x33);
            writeCMOS(0x063F, 0x37);
            writeCMOS(0x0640, 0x33);
            writeCMOS(0x0641, 0x37);
            writeCMOS(0x0642, 0x33);
            writeCMOS(0x0646, 0x5E);
            writeCMOS(0x064A, 0x5E);
            writeCMOS(0x066D, 0x77);
            writeCMOS(0x066E, 0x77);
            writeCMOS(0x0673, 0x77);
            writeCMOS(0x0674, 0x77);
            writeCMOS(0x0676, 0x77);
            writeCMOS(0x0677, 0x77);
            writeCMOS(0x0679, 0x05);
            writeCMOS(0x068A, 0x20);
            writeCMOS(0x0A2F, 0x73);
            writeCMOS(0x0A36, 0x73);

            writeCMOS(0x00A4, 0x00);    //Horizontal readout direction. 0h:normal 1h:inverted
            writeCMOS(0x00D4, 0x00);
            writeCMOS(0x0501, 0x08);
            writeCMOS(0x0502, 0x05);
            writeCMOS(0x0503, 0x00);

            setIDLE();
            setHMAX(878);
            setVMAX(3048);



            releaseIDLE();


        }


        void initCMOS_IMX485_10BIT()
        {

            writeFPGA(49, 1); //ampv normal
            writeFPGA(11, 2);
            writeFPGA(2, 0);  //decode 10bit mode

            writeFPGA(0, 0);
            Thread.Sleep(10);
            writeFPGA(0, 1);


            writeCMOS(0X3000, 0x01);
            writeCMOS(0X3002, 0X01);


            writeCMOS(0x3008, 0x7F); // BCWAIT_TIME[9:0]

            writeCMOS(0x300A, 0x5B); // CPWAIT_TIME[9:0]

            writeCMOS(0x300B, 0x50); //

            writeCMOS(0x3028, 0x4C); // HMAX[15:0]

            writeCMOS(0x3029, 0x04); //

            writeCMOS(0x3031, 0x00); // ADBIT

            writeCMOS(0x3032, 0x00); // MDBIT

            writeCMOS(0x30A5, 0x00); // XVS_DRV[1:0]

            writeCMOS(0x3114, 0x02); // INCKSEL1[1:0]

            writeCMOS(0x3119, 0x01); // INCKSEL2[7:0]

            writeCMOS(0x3260, 0x22); // -

            writeCMOS(0x3262, 0x02); // -

            writeCMOS(0x3278, 0xA2); // -

            writeCMOS(0x3324, 0x00); // -

            writeCMOS(0x3366, 0x31); // -

            writeCMOS(0x340C, 0x4D); // -

            writeCMOS(0x3416, 0x10);// -

            writeCMOS(0x3417, 0x13); // -

            writeCMOS(0x3432, 0x93); // -

            writeCMOS(0x34CE, 0x1E); // -

            writeCMOS(0x34CF, 0x1E); // -

            writeCMOS(0x34DC, 0x80); // -

            writeCMOS(0x351C, 0x03); // -

            writeCMOS(0x359E, 0x70); // -

            writeCMOS(0x35A2, 0x9C); // -

            writeCMOS(0x35AC, 0x08); // -

            writeCMOS(0x35C0, 0xFA); // -

            writeCMOS(0x35C2, 0x4E);// -

            writeCMOS(0x3608, 0x41);// -

            writeCMOS(0x360A, 0x47); // -

            writeCMOS(0x361E, 0x4A); // -

            writeCMOS(0x3630, 0x43);// -

            writeCMOS(0x3632, 0x47); // -

            writeCMOS(0x363C, 0x41); // -

            writeCMOS(0x363E, 0x4A);// -

            writeCMOS(0x3648, 0x41);// -

            writeCMOS(0x364A, 0x47);// -

            writeCMOS(0x3660, 0x04); // -

            writeCMOS(0x3676, 0x3F); // -

            writeCMOS(0x367A, 0x3F); // -

            writeCMOS(0x36A4, 0x41);// -

            writeCMOS(0x3798, 0x82); // -

            writeCMOS(0x379A, 0x82);// -

            writeCMOS(0x379C, 0x82); // -

            writeCMOS(0x379E, 0x82); // -

            writeCMOS(0x3804, 0x22); // INCKSEL4[1:0]

            writeCMOS(0x3888, 0xA8); // -

            writeCMOS(0x388C, 0xA6); // -

            writeCMOS(0x3914, 0x15); // -

            writeCMOS(0x3915, 0x15); // -

            writeCMOS(0x3916, 0x15); // -

            writeCMOS(0x3917, 0x14); // -

            writeCMOS(0x3918, 0x14); // -

            writeCMOS(0x3919, 0x14); // -

            writeCMOS(0x391A, 0x13); // -

            writeCMOS(0x391B, 0x13);// -

            writeCMOS(0x391C, 0x13); // -

            writeCMOS(0x391E, 0x00); // -

            writeCMOS(0x391F, 0xA5); // -

            writeCMOS(0x3920, 0xED); // -

            writeCMOS(0x3921, 0x0E);// -

            writeCMOS(0x39A2, 0x0C); // -

            writeCMOS(0x39A4, 0x16); // -

            writeCMOS(0x39A6, 0x2B); // -

            writeCMOS(0x39A7, 0x01);// -

            writeCMOS(0x39D2, 0x2D); // -

            writeCMOS(0x39D3, 0x00);// -

            writeCMOS(0x39D8, 0x37); // -

            writeCMOS(0x39D9, 0x00); // -

            writeCMOS(0x39DA, 0x9B); // -

            writeCMOS(0x39DB, 0x01);// -

            writeCMOS(0x39E0, 0x28); // -

            writeCMOS(0x39E1, 0x00); // -

            writeCMOS(0x39E2, 0x2C); // -

            writeCMOS(0x39E3, 0x00); // -

            writeCMOS(0x39E8, 0x96);// -

            writeCMOS(0x39EA, 0x9A); // -

            writeCMOS(0x39EB, 0x01); // -

            writeCMOS(0x39F2, 0x27); // -

            writeCMOS(0x39F3, 0x00); // -

            writeCMOS(0x3A00, 0x38); // -

            writeCMOS(0x3A01, 0x00); // -

            writeCMOS(0x3A02, 0x95);// -

            writeCMOS(0x3A03, 0x01); // -

            writeCMOS(0x3A18, 0x9B); // -

            writeCMOS(0x3A2A, 0x0C); // -

            writeCMOS(0x3A30, 0x15); // -

            writeCMOS(0x3A32, 0x31);// -

            writeCMOS(0x3A33, 0x01); // -

            writeCMOS(0x3A36, 0x4D); // -

            writeCMOS(0x3A3E, 0x11); // -

            writeCMOS(0x3A40, 0x31); // -

            writeCMOS(0x3A42, 0x4C);// -

            writeCMOS(0x3A43, 0x01); // -

            writeCMOS(0x3A44, 0x47); // -

            writeCMOS(0x3A46, 0x4B); // -

            writeCMOS(0x3A4E, 0x11); // -

            writeCMOS(0x3A50, 0x32); // -

            writeCMOS(0x3A52, 0x46); // -

            writeCMOS(0x3A53, 0x01);// -

            writeCMOS(0x3D04, 0x48); // TXCLKESC_FREQ[15:0]

            writeCMOS(0x3D05, 0x09); //

            writeCMOS(0x3D0C, 0x00); // INCKSEL6

            writeCMOS(0x3D18, 0x7F); // TCLKPOST[15:0]

            writeCMOS(0x3D1A, 0x37); // TCLKPREPARE[15:0]

            writeCMOS(0x3D1C, 0x37); // TCLKTRAIL[15:0]

            writeCMOS(0x3D1E, 0xF7); // TCLKZERO[15:0]

            writeCMOS(0x3D1F, 0x00);//

            writeCMOS(0x3D20, 0x3F); // THSPREPARE[15:0]

            writeCMOS(0x3D22, 0x6F); // THSZERO[15:0]

            writeCMOS(0x3D24, 0x3F); // THSTRAIL [15:0]

            writeCMOS(0x3D26, 0x5F);// THSEXIT [15:0]

            writeCMOS(0x3D28, 0x2F); // TLPX[15:0]

            setIDLE();

            setHMAX(3000);
            setVMAX(5000);


            releaseIDLE();



            Thread.Sleep(100);
            writeCMOS(0X3000, 0X00);
            Thread.Sleep(100);

            Thread.Sleep(100);
            writeCMOS(0X3002, 0X00);


            ushort vmax, hmax;
            vmax = 2208;
            hmax = 842;

            writeCMOS(0x3026, MSB2(vmax));
            writeCMOS(0x3025, MSB1(vmax));
            writeCMOS(0x3024, MSB0(vmax));

            writeCMOS(0x3028, LSB(hmax));
            writeCMOS(0x3029, MSB(hmax));


            LowLevelA4(0, 64, 0, 64, 0, 64);
            enableDDR(false);
        }



        void initCMOS_IMX485_10BIT_8CH()
        {

            writeFPGA(49, 1); //ampv normal
            writeFPGA(11, 2);
            writeFPGA(2, 0);  //decode 10bit mode

            writeFPGA(0, 0);
            Thread.Sleep(10);
            writeFPGA(0, 1);


            writeCMOS(0X3000, 0x01);
            writeCMOS(0X3002, 0X01);


            writeCMOS(0x3008, 0x7F); // BCWAIT_TIME[9:0]

            writeCMOS(0x300A, 0x5B); // CPWAIT_TIME[9:0]

            writeCMOS(0x300B, 0x50); //

            writeCMOS(0x3028, 0x4C); // HMAX[15:0]

            writeCMOS(0x3029, 0x04); //

            writeCMOS(0x3031, 0x00); // ADBIT

            writeCMOS(0x3032, 0x00); // MDBIT

            writeCMOS(0x30A5, 0x00); // XVS_DRV[1:0]

            writeCMOS(0x3114, 0x02); // INCKSEL1[1:0]

            writeCMOS(0x3119, 0x01); // INCKSEL2[7:0]

            writeCMOS(0x3260, 0x22); // -

            writeCMOS(0x3262, 0x02); // -

            writeCMOS(0x3278, 0xA2); // -

            writeCMOS(0x3324, 0x00); // -

            writeCMOS(0x3366, 0x31); // -

            writeCMOS(0x340C, 0x4D); // -

            writeCMOS(0x3416, 0x10);// -

            writeCMOS(0x3417, 0x13); // -

            writeCMOS(0x3432, 0x93); // -

            writeCMOS(0x34CE, 0x1E); // -

            writeCMOS(0x34CF, 0x1E); // -

            writeCMOS(0x34DC, 0x80); // -

            writeCMOS(0x351C, 0x03); // -

            writeCMOS(0x359E, 0x70); // -

            writeCMOS(0x35A2, 0x9C); // -

            writeCMOS(0x35AC, 0x08); // -

            writeCMOS(0x35C0, 0xFA); // -

            writeCMOS(0x35C2, 0x4E);// -

            writeCMOS(0x3608, 0x41);// -

            writeCMOS(0x360A, 0x47); // -

            writeCMOS(0x361E, 0x4A); // -

            writeCMOS(0x3630, 0x43);// -

            writeCMOS(0x3632, 0x47); // -

            writeCMOS(0x363C, 0x41); // -

            writeCMOS(0x363E, 0x4A);// -

            writeCMOS(0x3648, 0x41);// -

            writeCMOS(0x364A, 0x47);// -

            writeCMOS(0x3660, 0x04); // -

            writeCMOS(0x3676, 0x3F); // -

            writeCMOS(0x367A, 0x3F); // -

            writeCMOS(0x36A4, 0x41);// -

            writeCMOS(0x3798, 0x82); // -

            writeCMOS(0x379A, 0x82);// -

            writeCMOS(0x379C, 0x82); // -

            writeCMOS(0x379E, 0x82); // -

            writeCMOS(0x3804, 0x22); // INCKSEL4[1:0]

            writeCMOS(0x3888, 0xA8); // -

            writeCMOS(0x388C, 0xA6); // -

            writeCMOS(0x3914, 0x15); // -

            writeCMOS(0x3915, 0x15); // -

            writeCMOS(0x3916, 0x15); // -

            writeCMOS(0x3917, 0x14); // -

            writeCMOS(0x3918, 0x14); // -

            writeCMOS(0x3919, 0x14); // -

            writeCMOS(0x391A, 0x13); // -

            writeCMOS(0x391B, 0x13);// -

            writeCMOS(0x391C, 0x13); // -

            writeCMOS(0x391E, 0x00); // -

            writeCMOS(0x391F, 0xA5); // -

            writeCMOS(0x3920, 0xED); // -

            writeCMOS(0x3921, 0x0E);// -

            writeCMOS(0x39A2, 0x0C); // -

            writeCMOS(0x39A4, 0x16); // -

            writeCMOS(0x39A6, 0x2B); // -

            writeCMOS(0x39A7, 0x01);// -

            writeCMOS(0x39D2, 0x2D); // -

            writeCMOS(0x39D3, 0x00);// -

            writeCMOS(0x39D8, 0x37); // -

            writeCMOS(0x39D9, 0x00); // -

            writeCMOS(0x39DA, 0x9B); // -

            writeCMOS(0x39DB, 0x01);// -

            writeCMOS(0x39E0, 0x28); // -

            writeCMOS(0x39E1, 0x00); // -

            writeCMOS(0x39E2, 0x2C); // -

            writeCMOS(0x39E3, 0x00); // -

            writeCMOS(0x39E8, 0x96);// -

            writeCMOS(0x39EA, 0x9A); // -

            writeCMOS(0x39EB, 0x01); // -

            writeCMOS(0x39F2, 0x27); // -

            writeCMOS(0x39F3, 0x00); // -

            writeCMOS(0x3A00, 0x38); // -

            writeCMOS(0x3A01, 0x00); // -

            writeCMOS(0x3A02, 0x95);// -

            writeCMOS(0x3A03, 0x01); // -

            writeCMOS(0x3A18, 0x9B); // -

            writeCMOS(0x3A2A, 0x0C); // -

            writeCMOS(0x3A30, 0x15); // -

            writeCMOS(0x3A32, 0x31);// -

            writeCMOS(0x3A33, 0x01); // -

            writeCMOS(0x3A36, 0x4D); // -

            writeCMOS(0x3A3E, 0x11); // -

            writeCMOS(0x3A40, 0x31); // -

            writeCMOS(0x3A42, 0x4C);// -

            writeCMOS(0x3A43, 0x01); // -

            writeCMOS(0x3A44, 0x47); // -

            writeCMOS(0x3A46, 0x4B); // -

            writeCMOS(0x3A4E, 0x11); // -

            writeCMOS(0x3A50, 0x32); // -

            writeCMOS(0x3A52, 0x46); // -

            writeCMOS(0x3A53, 0x01);// -

            writeCMOS(0x3D01, 0x06); // 8CH


            writeCMOS(0x3D04, 0x90); // TXCLKESC_FREQ[15:0]

            writeCMOS(0x3D05, 0x12); //

            writeCMOS(0x3D0C, 0x01); // INCKSEL6

            writeCMOS(0x3D18, 0x67); // TCLKPOST[15:0]

            writeCMOS(0x3D1A, 0x27); // TCLKPREPARE[15:0]

            writeCMOS(0x3D1C, 0x27); // TCLKTRAIL[15:0]

            writeCMOS(0x3D1E, 0xB7); // TCLKZERO[15:0]

            writeCMOS(0x3D1F, 0x00);//

            writeCMOS(0x3D20, 0x2F); // THSPREPARE[15:0]

            writeCMOS(0x3D22, 0x4F); // THSZERO[15:0]

            writeCMOS(0x3D24, 0x2F); // THSTRAIL [15:0]

            writeCMOS(0x3D26, 0x47);// THSEXIT [15:0]

            writeCMOS(0x3D28, 0x27); // TLPX[15:0]

            setIDLE();

            setHMAX(3000);
            setVMAX(5000);


            releaseIDLE();



            Thread.Sleep(100);
            writeCMOS(0X3000, 0X00);
            Thread.Sleep(100);

            Thread.Sleep(100);
            writeCMOS(0X3002, 0X00);


            ushort vmax, hmax;
            vmax = 2208;
            hmax = 842;
            hmax = 432;
            writeCMOS(0x3026, MSB2(vmax));
            writeCMOS(0x3025, MSB1(vmax));
            writeCMOS(0x3024, MSB0(vmax));

            writeCMOS(0x3028, LSB(hmax));
            writeCMOS(0x3029, MSB(hmax));


            LowLevelA4(0, 64, 0, 64, 0, 64);
            enableDDR(false);

            writeCMOS(0x3D01, 0x07); // 8CH

        }

        void initCMOS_IMX485_10BIT_2X4CH()
        {

            writeFPGA(49, 1); //ampv normal
            writeFPGA(11, 3);
            writeFPGA(2, 0);  //decode 10bit mode

            writeFPGA(0, 0);
            Thread.Sleep(10);
            writeFPGA(0, 1);


            writeCMOS(0X3000, 0x01);
            writeCMOS(0X3002, 0X01);


            writeCMOS(0x3008, 0x7F); // BCWAIT_TIME[9:0]

            writeCMOS(0x300A, 0x5B); // CPWAIT_TIME[9:0]

            writeCMOS(0x300B, 0x50); //

            writeCMOS(0x3028, 0x4C); // HMAX[15:0]

            writeCMOS(0x3029, 0x04); //

            writeCMOS(0x3031, 0x00); // ADBIT

            writeCMOS(0x3032, 0x00); // MDBIT

            writeCMOS(0x30A5, 0x00); // XVS_DRV[1:0]

            writeCMOS(0x3114, 0x02); // INCKSEL1[1:0]

            writeCMOS(0x3119, 0x01); // INCKSEL2[7:0]

            writeCMOS(0x3260, 0x22); // -

            writeCMOS(0x3262, 0x02); // -

            writeCMOS(0x3278, 0xA2); // -

            writeCMOS(0x3324, 0x00); // -

            writeCMOS(0x3366, 0x31); // -

            writeCMOS(0x340C, 0x4D); // -

            writeCMOS(0x3416, 0x10);// -

            writeCMOS(0x3417, 0x13); // -

            writeCMOS(0x3432, 0x93); // -

            writeCMOS(0x34CE, 0x1E); // -

            writeCMOS(0x34CF, 0x1E); // -

            writeCMOS(0x34DC, 0x80); // -

            writeCMOS(0x351C, 0x03); // -

            writeCMOS(0x359E, 0x70); // -

            writeCMOS(0x35A2, 0x9C); // -

            writeCMOS(0x35AC, 0x08); // -

            writeCMOS(0x35C0, 0xFA); // -

            writeCMOS(0x35C2, 0x4E);// -

            writeCMOS(0x3608, 0x41);// -

            writeCMOS(0x360A, 0x47); // -

            writeCMOS(0x361E, 0x4A); // -

            writeCMOS(0x3630, 0x43);// -

            writeCMOS(0x3632, 0x47); // -

            writeCMOS(0x363C, 0x41); // -

            writeCMOS(0x363E, 0x4A);// -

            writeCMOS(0x3648, 0x41);// -

            writeCMOS(0x364A, 0x47);// -

            writeCMOS(0x3660, 0x04); // -

            writeCMOS(0x3676, 0x3F); // -

            writeCMOS(0x367A, 0x3F); // -

            writeCMOS(0x36A4, 0x41);// -

            writeCMOS(0x3798, 0x82); // -

            writeCMOS(0x379A, 0x82);// -

            writeCMOS(0x379C, 0x82); // -

            writeCMOS(0x379E, 0x82); // -

            writeCMOS(0x3804, 0x22); // INCKSEL4[1:0]

            writeCMOS(0x3888, 0xA8); // -

            writeCMOS(0x388C, 0xA6); // -

            writeCMOS(0x3914, 0x15); // -

            writeCMOS(0x3915, 0x15); // -

            writeCMOS(0x3916, 0x15); // -

            writeCMOS(0x3917, 0x14); // -

            writeCMOS(0x3918, 0x14); // -

            writeCMOS(0x3919, 0x14); // -

            writeCMOS(0x391A, 0x13); // -

            writeCMOS(0x391B, 0x13);// -

            writeCMOS(0x391C, 0x13); // -

            writeCMOS(0x391E, 0x00); // -

            writeCMOS(0x391F, 0xA5); // -

            writeCMOS(0x3920, 0xED); // -

            writeCMOS(0x3921, 0x0E);// -

            writeCMOS(0x39A2, 0x0C); // -

            writeCMOS(0x39A4, 0x16); // -

            writeCMOS(0x39A6, 0x2B); // -

            writeCMOS(0x39A7, 0x01);// -

            writeCMOS(0x39D2, 0x2D); // -

            writeCMOS(0x39D3, 0x00);// -

            writeCMOS(0x39D8, 0x37); // -

            writeCMOS(0x39D9, 0x00); // -

            writeCMOS(0x39DA, 0x9B); // -

            writeCMOS(0x39DB, 0x01);// -

            writeCMOS(0x39E0, 0x28); // -

            writeCMOS(0x39E1, 0x00); // -

            writeCMOS(0x39E2, 0x2C); // -

            writeCMOS(0x39E3, 0x00); // -

            writeCMOS(0x39E8, 0x96);// -

            writeCMOS(0x39EA, 0x9A); // -

            writeCMOS(0x39EB, 0x01); // -

            writeCMOS(0x39F2, 0x27); // -

            writeCMOS(0x39F3, 0x00); // -

            writeCMOS(0x3A00, 0x38); // -

            writeCMOS(0x3A01, 0x00); // -

            writeCMOS(0x3A02, 0x95);// -

            writeCMOS(0x3A03, 0x01); // -

            writeCMOS(0x3A18, 0x9B); // -

            writeCMOS(0x3A2A, 0x0C); // -

            writeCMOS(0x3A30, 0x15); // -

            writeCMOS(0x3A32, 0x31);// -

            writeCMOS(0x3A33, 0x01); // -

            writeCMOS(0x3A36, 0x4D); // -

            writeCMOS(0x3A3E, 0x11); // -

            writeCMOS(0x3A40, 0x31); // -

            writeCMOS(0x3A42, 0x4C);// -

            writeCMOS(0x3A43, 0x01); // -

            writeCMOS(0x3A44, 0x47); // -

            writeCMOS(0x3A46, 0x4B); // -

            writeCMOS(0x3A4E, 0x11); // -

            writeCMOS(0x3A50, 0x32); // -

            writeCMOS(0x3A52, 0x46); // -

            writeCMOS(0x3A53, 0x01);// -

            writeCMOS(0x3D01, 0x06); // 2x4CH


            writeCMOS(0x3D04, 0x90); // TXCLKESC_FREQ[15:0]

            writeCMOS(0x3D05, 0x12); //

            writeCMOS(0x3D0C, 0x01); // INCKSEL6

            writeCMOS(0x3D18, 0x67); // TCLKPOST[15:0]

            writeCMOS(0x3D1A, 0x27); // TCLKPREPARE[15:0]

            writeCMOS(0x3D1C, 0x27); // TCLKTRAIL[15:0]

            writeCMOS(0x3D1E, 0xb7); // TCLKZERO[15:0]

            writeCMOS(0x3D1F, 0x00);//

            writeCMOS(0x3D20, 0x2F); // THSPREPARE[15:0]

            writeCMOS(0x3D22, 0x4F); // THSZERO[15:0]

            writeCMOS(0x3D24, 0x2F); // THSTRAIL [15:0]

            writeCMOS(0x3D26, 0x47);// THSEXIT [15:0]

            writeCMOS(0x3D28, 0x27); // TLPX[15:0]

            setIDLE();

            setHMAX(3000);
            setVMAX(5000);


            releaseIDLE();



            Thread.Sleep(100);
            writeCMOS(0X3000, 0X00);
            Thread.Sleep(100);

            Thread.Sleep(100);
            writeCMOS(0X3002, 0X00);


            ushort vmax, hmax;
            vmax = 2208;
            hmax = 842;

            writeCMOS(0x3026, MSB2(vmax));
            writeCMOS(0x3025, MSB1(vmax));
            writeCMOS(0x3024, MSB0(vmax));

            writeCMOS(0x3028, LSB(hmax));
            writeCMOS(0x3029, MSB(hmax));


            LowLevelA4(0, 64, 0, 64, 0, 64);
            enableDDR(false);
        }


        void initCMOS_IMX492_MIPI_12BIT()
        {

            writeFPGA(49, 1); // ampv voltage return to default 2.9V

            writeFPGA(11, 3);
            writeFPGA(2, 1);  //mipi decoder = 12bit

            writeFPGA(0, 0);
            Thread.Sleep(10);
            writeFPGA(0, 1);







            writeCMOS(0x3033, 0x30);         //1.1
            writeCMOS(0x303c, 0x01);        //1.2

            //1.3

            writeCMOS(0x31E8, 0x20);
            writeCMOS(0x31E9, 0x01);
            writeCMOS(0x3122, 0x03);  //to reduce the speed, set to 0x03, before it is    0x01
            writeCMOS(0x312A, 0x02);
            writeCMOS(0x3123, 0x00);
            writeCMOS(0x3125, 0x01);
            writeCMOS(0x312D, 0x02);


            //1.4
            writeCMOS(0x3000, 0x12);
            //1.5
            writeCMOS(0x310b, 0x00);


            //1.6
            writeCMOS(0x3004, 0x1C);
            writeCMOS(0x3005, 0x05);       //in the table it is 0x06.  it will cause the over-exposure part return darker. change to 0x05 to solve this .
            writeCMOS(0x3006, 0x00);
            writeCMOS(0x3007, 0xA7);
            writeCMOS(0x300A, 0x50);
            writeCMOS(0x300B, 0x03);
            writeCMOS(0x300E, 0x0);
            writeCMOS(0x300F, 0x0);
            writeCMOS(0x3012, 0x0);
            writeCMOS(0x3017, 0x0);
            writeCMOS(0x302C, 0x10);
            writeCMOS(0x302D, 0x00);
            writeCMOS(0x3033, 0x01);//xmaster stop
            writeCMOS(0x303C, 0x01);
            writeCMOS(0x3042, 0x10);
            writeCMOS(0x3043, 0x0);
            writeCMOS(0x3047, 0x02);
            writeCMOS(0x304E, 0x0B);
            writeCMOS(0x304F, 0x2A);
            writeCMOS(0x3052, 0xEE);
            writeCMOS(0x3062, 0x25);
            writeCMOS(0x3064, 0x78);
            writeCMOS(0x3065, 0x33);
            writeCMOS(0x3066, 0x64);
            writeCMOS(0x3067, 0x71);
            writeCMOS(0x3081, 0x00);
            writeCMOS(0x3084, 0x00);
            writeCMOS(0x3085, 0x00);
            writeCMOS(0x3086, 0x00);
            writeCMOS(0x3087, 0x00);
            writeCMOS(0x3088, 0x75);
            writeCMOS(0x308A, 0x09);
            writeCMOS(0x308C, 0x61);
            writeCMOS(0x30A9, 0xf2);
            writeCMOS(0x30AA, 0x2);
            writeCMOS(0x30AB, 0x0);
            writeCMOS(0x30AC, 0xE6);
            writeCMOS(0x30AD, 0x2);
            writeCMOS(0x30E5, 0x00);
            writeCMOS(0x30EF, 0x01);
            writeCMOS(0x311F, 0x00);


            writeCMOS(0x3124, 0x00);

            writeCMOS(0x3127, 0x02);
            writeCMOS(0x3129, 0x90);


            writeCMOS(0x312F, 0x20);
            writeCMOS(0x3130, 0x30);
            writeCMOS(0x3131, 0x16);
            writeCMOS(0x3132, 0x10);
            writeCMOS(0x3133, 0x16);
            writeCMOS(0x3134, 0xAF);
            writeCMOS(0x3136, 0xC7);
            writeCMOS(0x3138, 0x7F);
            writeCMOS(0x313A, 0x6F);
            writeCMOS(0x313C, 0x6F);
            writeCMOS(0x313E, 0xCF);
            writeCMOS(0x3140, 0x77);
            writeCMOS(0x3142, 0x5F);
            writeCMOS(0x3146, 0x00);

            writeCMOS(0x31F5, 0x01);
            writeCMOS(0x3234, 0x32);
            writeCMOS(0x3248, 0xBC);
            writeCMOS(0x3250, 0xBC);
            writeCMOS(0x3258, 0xBC);
            writeCMOS(0x3260, 0xBC);
            writeCMOS(0x3274, 0x13);
            writeCMOS(0x3276, 0x00);
            writeCMOS(0x3277, 0x00);
            writeCMOS(0x327C, 0x13);
            writeCMOS(0x327E, 0x00);
            writeCMOS(0x327F, 0x00);
            writeCMOS(0x3284, 0x13);
            writeCMOS(0x3286, 0x00);
            writeCMOS(0x3287, 0x00);
            writeCMOS(0x328C, 0x13);
            writeCMOS(0x328E, 0x00);
            writeCMOS(0x328F, 0x00);
            writeCMOS(0x32AE, 0x00);
            writeCMOS(0x32AF, 0x00);
            writeCMOS(0x32CA, 0x5A);
            writeCMOS(0x332C, 0xff); //low power consumption period lentth 1
            writeCMOS(0x332D, 0x00);   //low power consumption period lentth 1
            writeCMOS(0x332F, 0x00);
            writeCMOS(0x334A, 0xff);       //low power consumption period lentth 1
            writeCMOS(0x334B, 0x00);        //low power consumption period lentth 1
            writeCMOS(0x334C, 0x01);
            writeCMOS(0x3352, 0x50);
            writeCMOS(0x3356, 0x4F);
            writeCMOS(0x335A, 0x79);
            writeCMOS(0x335E, 0x56);
            writeCMOS(0x3360, 0x6A);
            writeCMOS(0x336A, 0x56);
            writeCMOS(0x33D6, 0x79);
            writeCMOS(0x340C, 0x6E);
            writeCMOS(0x3448, 0x7E);
            writeCMOS(0x348E, 0x6F);
            writeCMOS(0x3492, 0x11);
            writeCMOS(0x34C4, 0x5A);
            writeCMOS(0x3506, 0x56);
            writeCMOS(0x350C, 0x56);
            writeCMOS(0x350E, 0x58);
            writeCMOS(0x353D, 0x10);
            writeCMOS(0x3549, 0x04);
            writeCMOS(0x355D, 0x03);
            writeCMOS(0x355E, 0x03);
            writeCMOS(0x3574, 0x56);
            writeCMOS(0x357F, 0x0C);
            writeCMOS(0x3580, 0x0A);
            writeCMOS(0x3581, 0x08);
            writeCMOS(0x3583, 0x72);
            writeCMOS(0x3587, 0x01);
            writeCMOS(0x35B6, 0xff);        //low power consumption period lentth 3
            writeCMOS(0x35B7, 0x00);           //low power consumption period lentth 3
            writeCMOS(0x35B8, 0xfa);            //low power consumption period lentth 4
            writeCMOS(0x35B9, 0x00);             //low power consumption period lentth 5
            writeCMOS(0x35D0, 0x5E);
            writeCMOS(0x35D4, 0x63);
            writeCMOS(0x35E5, 0x9A);
            writeCMOS(0x366A, 0x04);
            writeCMOS(0x366B, 0x04);
            writeCMOS(0x366C, 0x00);
            writeCMOS(0x366D, 0x00);
            writeCMOS(0x366E, 0x00);
            writeCMOS(0x366F, 0x00);
            writeCMOS(0x3670, 0x00);
            writeCMOS(0x3671, 0x05);
            writeCMOS(0x3676, 0x83);
            writeCMOS(0x3677, 0x03);
            writeCMOS(0x3678, 0x00);
            writeCMOS(0x3679, 0x04);
            writeCMOS(0x367A, 0x2C);
            writeCMOS(0x367B, 0x05);
            writeCMOS(0x367D, 0x06);
            writeCMOS(0x367E, 0xFF);
            writeCMOS(0x367F, 0x06);
            writeCMOS(0x3680, 0x4B);
            writeCMOS(0x3688, 0x05);
            writeCMOS(0x3690, 0x27);
            writeCMOS(0x3692, 0x65);
            writeCMOS(0x3694, 0x4F);
            writeCMOS(0x3696, 0xA1);
            writeCMOS(0x36BC, 0x01);          //low power consumption period lentth 0
            writeCMOS(0x36BD, 0x00);          //low power consumption period lentth 0
            writeCMOS(0x371C, 0x02);
            writeCMOS(0x372F, 0x3C);
            writeCMOS(0x3730, 0x01);
            writeCMOS(0x3732, 0xB8);
            writeCMOS(0x3734, 0x4A);
            writeCMOS(0x3736, 0x57);
            writeCMOS(0x3738, 0x4D);
            writeCMOS(0x3744, 0x0F);
            writeCMOS(0x375B, 0x01);
            writeCMOS(0x382B, 0x68);
            writeCMOS(0x3836, 0x34);
            writeCMOS(0x38B3, 0x00);
            writeCMOS(0x3a43, 0x00);
            writeCMOS(0x3a54, 0x00);
            writeCMOS(0x3a55, 0x1E);
            writeCMOS(0x3aC4, 0x00);
            writeCMOS(0x3c08, 0x3F);
            writeCMOS(0x3c0C, 0x1B);
            writeCMOS(0x3e80, 0x14);
            writeCMOS(0x3e82, 0x30);
            writeCMOS(0x3e84, 0x04);
            writeCMOS(0x3e85, 0x01);
            writeCMOS(0x3e86, 0x10);
            writeCMOS(0x3e87, 0x16);
            writeCMOS(0x3e88, 0x03);
            writeCMOS(0x3e89, 0xFE);
            writeCMOS(0x3e8A, 0x01);
            writeCMOS(0x3e8B, 0x06);
            writeCMOS(0x3e8E, 0x03);
            writeCMOS(0x3e8F, 0xFE);
            writeCMOS(0x3e90, 0x01);
            writeCMOS(0x3e91, 0x06);
            writeCMOS(0x3e94, 0x33);
            writeCMOS(0x3e95, 0x01);
            writeCMOS(0x3e96, 0x19);
            writeCMOS(0x3e98, 0x30);
            writeCMOS(0x3e9A, 0x09);
            writeCMOS(0x3e9C, 0x10);
            writeCMOS(0x3e9D, 0x16);
            writeCMOS(0x3e9E, 0xFE);
            writeCMOS(0x3e9F, 0x03);
            writeCMOS(0x3eA0, 0x06);
            writeCMOS(0x3eA3, 0x01);
            writeCMOS(0x3eA4, 0xFE);
            writeCMOS(0x3eA5, 0x03);
            writeCMOS(0x3eA6, 0x06);
            writeCMOS(0x3eA9, 0x33);
            writeCMOS(0x3eAA, 0x00);
            writeCMOS(0x3eAB, 0x08);
            writeCMOS(0x3eAC, 0x08);
            writeCMOS(0x3eAD, 0x01);
            writeCMOS(0x3eAE, 0x08);
            writeCMOS(0x3eAF, 0x08);
            writeCMOS(0x3eB0, 0x00);
            writeCMOS(0x3eB1, 0x10);
            writeCMOS(0x3eB2, 0x10);
            writeCMOS(0x3eB3, 0x01);
            writeCMOS(0x3eB4, 0x10);
            writeCMOS(0x3eB5, 0x10);
            writeCMOS(0x3eB6, 0x00);
            writeCMOS(0x3eB7, 0x00);
            writeCMOS(0x3eB8, 0x00);
            writeCMOS(0x3eB9, 0x00);
            writeCMOS(0x3eBA, 0x00);
            writeCMOS(0x3eBB, 0x00);
            writeCMOS(0x3eC0, 0x54);
            writeCMOS(0x3eCC, 0x04);
            writeCMOS(0x3eCD, 0x04);
            writeCMOS(0x3eD0, 0xF0);
            writeCMOS(0x3eD1, 0x20);
            writeCMOS(0x3eD2, 0x0B);
            writeCMOS(0x3eD3, 0x04);
            writeCMOS(0x3eD5, 0x13);
            writeCMOS(0x3eD6, 0x00);
            writeCMOS(0x3eD9, 0x0F);
            writeCMOS(0x3eE4, 0x02);
            writeCMOS(0x3eE5, 0x02);
            writeCMOS(0x3eE7, 0x00);
            writeCMOS(0x3eF6, 0x00);
            writeCMOS(0x3eF8, 0x10);
            writeCMOS(0x3eFA, 0x00);
            writeCMOS(0x3eFC, 0x10);




            writeCMOS(0x3000, 0x02);              //2.1
            writeCMOS(0x35e5, 0x92);             //2,2
            writeCMOS(0x35e5, 0x9a);               //2.3
            writeCMOS(0x3000, 0x00);                //2.4



            writeCMOS(0x3033, 0x20);   //3.1
            writeCMOS(0x3017, 0xa8);    //3.2




            ushort hmax, vmax;
            hmax = 1080;
            vmax = 5720;
            writeCMOS(0x30ac, LSB(hmax));
            writeCMOS(0x30ad, MSB(hmax));

            writeCMOS(0x30ab, MSB2(vmax));
            writeCMOS(0x30aa, MSB1(vmax));
            writeCMOS(0x30a9, MSB0(vmax));


            LowLevelA4(0, 8, 0, 8, 0, 8);
            enableDDR(false);

            setQHY492AnalogGain(1320);

        }




        private void button76_Click(object sender, EventArgs e)
        {

            //init IMX455 sensor
            //digital gain
            //writeFPGA( 18, 16 );
            //writeFPGA( 19, 16 );
            //writeFPGA( 20, 16 );

            initCMOS_IMX455(0x0a);


        }




        void TestPatten_IMX571(ushort mode)
        {
            //mode 0: normal mode
            //mode 1: shading mode
            //mode 2: color bars mode


            if (mode == 0)
            {
                writeCMOS(0xD1, 0x00);
            }

            else if (mode == 1)
            {
                writeCMOS(0XD1, 0X01);
                writeCMOS(0XD2, 0X00);
            }

            else if (mode == 2)
            {
                writeCMOS(0XD1, 0X01);
                writeCMOS(0XD2, 0X02);
            }

            else
            {
                writeCMOS(0xd1, 0x00);
            }
        }




        void TestPatten_IMX410(ushort mode)
        {
            //mode 0: normal mode
            //mode 1: shading mode
            //mode 2: color bars mode


            if (mode == 0)
            {
                writeCMOS(0x2B2, 0x00);
            }

            else if (mode == 1)
            {
                writeCMOS(0X2B2, 0X01);
            }

            else if (mode == 2)
            {
                writeCMOS(0X2B2, 0X0F);
            }

            else
            {
                writeCMOS(0X2B2, 0x00);
            }
        }



        void TestPatten_IMX492(ushort mode)
        {
            if (mode == 0)
            {
                writeCMOS(0x303a, 0x00);
            }

            else if (mode == 1)
            {
                writeCMOS(0X303a, 0X11);
                writeCMOS(0x303b, 0x02);
            }

            else if (mode == 2)
            {
                writeCMOS(0X303a, 0x11);
                writeCMOS(0x303b, 0x0a);
            }

            else
            {
                writeCMOS(0X303a, 0x11);
                writeCMOS(0x303b, 0x0b);
            }

        }



        void TestPatten_IMX485(ushort mode)
        {
            if (mode == 0)
            {
                writeCMOS(0x30E0, 0x00);
                writeCMOS(0X3110, 0X00);
            }

            else if (mode == 1)
            {
                writeCMOS(0x30E0, 0x01);
                writeCMOS(0X3110, 0X20);

                writeCMOS(0x30E2, 0x02);
            }

            else if (mode == 2)
            {
                writeCMOS(0x30E0, 0x01);
                writeCMOS(0X3110, 0X20);

                writeCMOS(0x30E2, 0x0a);
            }

            else
            {
                writeCMOS(0x30E0, 0x01);
                writeCMOS(0X3110, 0X20);

                writeCMOS(0x30E2, 0x0b);
            }

        }


        private void button77_Click(object sender, EventArgs e)
        {
            //TEST PATTERN : SHADING
            if (sensorModel == 455)
                writeCMOS(0X0C4, 0X01);
            if (sensorModel == 571)
                TestPatten_IMX571(1);
            if (sensorModel == 410)
                TestPatten_IMX410(1);
            if (sensorModel == 492)
                TestPatten_IMX492(1);
            if (sensorModel == 485)
                TestPatten_IMX485(1);
            if (sensorModel == 2110)//sc2210
            {
                writeCMOS(0x4501, 0xcc);
                writeCMOS(0x3902, 0x05);
                writeCMOS(0x3e06, 0x03);
                writeCMOS(0x3980, 0x60);

            }
            if (sensorModel == 585)
            {
                writeCMOS(0x30e0, 0x01);
                writeCMOS(0x30e2, 0x04);

            }
        }

        private void button79_Click(object sender, EventArgs e)
        {
            //TEST PATTERN : NORMAL IMAGE
            if (sensorModel == 455)
                writeCMOS(0X0C4, 0X00);
            if (sensorModel == 571)
                TestPatten_IMX571(0);
            if (sensorModel == 410)
                TestPatten_IMX410(0);
            if (sensorModel == 492)
                TestPatten_IMX492(0);
            if (sensorModel == 485)
                TestPatten_IMX485(0);
            if (sensorModel == 2110)//sc2210
            {
                writeCMOS(0x4501, 0xb4);
                writeCMOS(0x3902, 0x45);
                writeCMOS(0x3e06, 0x00);
                writeCMOS(0x3980, 0x61);
            } if (sensorModel == 585)
            {
                writeCMOS(0x30e0, 0x00);
                writeCMOS(0x30e2, 0x00);

            }
        }

        private void button78_Click(object sender, EventArgs e)
        {
            //TEST PATTERN :  COLOR BAR
            if (sensorModel == 455)
                writeCMOS(0X0C4, 0X03);
            if (sensorModel == 571)
                TestPatten_IMX571(2);
            if (sensorModel == 410)
                TestPatten_IMX410(2);
            if (sensorModel == 492)
                TestPatten_IMX492(2);
            if (sensorModel == 485)
                TestPatten_IMX485(2);
            if (sensorModel == 585)
            {
                writeCMOS(0x30e0, 0x01);
                writeCMOS(0x30e2, 0x02);

            }
        }

        private void button80_Click(object sender, EventArgs e)
        {
            writeFPGA(96, 0x01);
            writeFPGA(96, 0x00);
        }

        private void button81_Click(object sender, EventArgs e)
        {
            writeFPGA(96, 0x02);
            writeFPGA(96, 0x00);
        }

        private void button82_Click(object sender, EventArgs e)
        {
            writeFPGA(96, 0x04);
            writeFPGA(96, 0x00);
        }

        private void button83_Click(object sender, EventArgs e)
        {
            writeFPGA(96, 0x08);
            writeFPGA(96, 0x00);
        }

        private void button84_Click(object sender, EventArgs e)
        {
            writeFPGA(96, 0x10);
            writeFPGA(96, 0x00);
        }

        private void button85_Click(object sender, EventArgs e)
        {
            writeFPGA(96, 0x20);
            writeFPGA(96, 0x00);
        }

        private void button86_Click(object sender, EventArgs e)
        {
            writeFPGA(96, 0x40);
            writeFPGA(96, 0x00);
        }

        private void button87_Click(object sender, EventArgs e)
        {
            writeFPGA(96, 0x80);
            writeFPGA(96, 0x00);
        }


        void setIDLECODE(UInt32 value)
        {

            writeCMOS(0X0120, MSB3(value));
            writeCMOS(0X0122, MSB2(value));
            writeCMOS(0X0124, MSB1(value));
            writeCMOS(0X0126, MSB0(value));

            writeCMOS(0X0121, 0);
            writeCMOS(0X0123, 0);
            writeCMOS(0X0125, 0);
            writeCMOS(0X0127, 0);

        }

        private void button88_Click(object sender, EventArgs e)
        {
            setIDLECODE(0x12345678);
        }

        private void button89_Click(object sender, EventArgs e)
        {
            setIDLECODE(0x00000000);
        }




        private void button90_Click(object sender, EventArgs e)
        {
            writeFPGA(89, 1);
            Thread.Sleep(100);
            writeFPGA(89, 0);
        }

        void wordAlign()
        {
            writeFPGA(91, 0X00);
            writeFPGA(91, 0XFF);
            Thread.Sleep(100);
            writeFPGA(91, 0X00);
        }

        private void button91_Click(object sender, EventArgs e)
        {
            wordAlign();

        }


        void setByteDelay(ushort channel, ushort value)
        {
            writeFPGA(93, value);   //BD_DELAY
            writeFPGA(88, channel);//BD_CS
            writeFPGA(92, 0);
            writeFPGA(92, 1);//BD_WR
            writeFPGA(92, 0);
        }


        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            setByteDelay(0, (ushort)numericUpDown1.Value);

        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            setByteDelay(1, (ushort)numericUpDown2.Value);

        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            setByteDelay(2, (ushort)numericUpDown3.Value);

        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            setByteDelay(3, (ushort)numericUpDown4.Value);
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            setByteDelay(4, (ushort)numericUpDown5.Value);

        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            setByteDelay(5, (ushort)numericUpDown6.Value);
        }

        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            setByteDelay(6, (ushort)numericUpDown7.Value);

        }

        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {
            setByteDelay(7, (ushort)numericUpDown8.Value);

        }

        void AutoChannelAlign()
        {         //send the sample pulse  to sample all channel alignment data to store in the register
            ushort[] ChannelAlignData = new ushort[8];

            //clear the byteDelay at first
            for (ushort i = 0; i < 8; i++)
            {
                setByteDelay(i, 0);
            }

            Thread.Sleep(1);
            //Send the sample pulse to obtain the data of each channel
            writeFPGA(94, 0);
            writeFPGA(94, 1);
            Thread.Sleep(1);
            writeFPGA(94, 0);


            //get delay value of each value
            for (ushort i = 0; i < 8; i++)
            {
                writeFPGA(88, i);  //set channel cs
                ChannelAlignData[i] = (ushort)(256 * readFPGA(33) + readFPGA(32));
                richTextBox1.AppendText(ChannelAlignData[i].ToString() + " ");
            }

            ushort maxPosition = 0;

            for (ushort i = 0; i < 8; i++)
            {
                if (ChannelAlignData[i] > maxPosition)
                    maxPosition = ChannelAlignData[i];
            }

            ushort[] ChannelAlignDelta = new ushort[8];


            richTextBox1.AppendText(Environment.NewLine);

            for (ushort i = 0; i < 8; i++)
            {
                ChannelAlignDelta[i] = (ushort)(maxPosition - ChannelAlignData[i]);
                richTextBox1.AppendText(ChannelAlignDelta[i].ToString() + " ");
            }

            richTextBox1.AppendText(Environment.NewLine);

            for (ushort i = 0; i < 8; i++)
            {
                setByteDelay(i, ChannelAlignDelta[i]);

            }

        }

        private void button92_Click(object sender, EventArgs e)
        {         
            AutoChannelAlign();     
        }

        void setSLVSEC_DecodeBit(byte i)
        {
            if (i == 16)
                writeFPGA(62, 0);
            else if (i == 14)
                writeFPGA(62, 1);
            else if (i == 12)
                writeFPGA(62, 2);

        }

        void SetCmosCtrlMode(byte value)
        {
            writeFPGA(79, value);
        }

        void MIPIRstn()
        {
            writeFPGA(54, 0);
            Thread.Sleep(10);
            writeFPGA(54, 1);
            Thread.Sleep(10);
        }
        byte masterslave;
        private void button94_Click(object sender, EventArgs e)
        {
            byte mode;
            enableDDR(true);
            resetCMOS();
            Thread.Sleep(10);

            mode = 0x0a;
            if (comboBox2.SelectedIndex == 0) mode = 0x0a;//16bit
            else if (comboBox2.SelectedIndex == 1) mode = 0x0b;//14bit
            else if (comboBox2.SelectedIndex == 2) mode = 0x0c;//12bit  raw8
            else if (comboBox2.SelectedIndex == 3) mode = 0x0d;//10bit
            else if (comboBox2.SelectedIndex == 4) mode = 0x82;//8bit 2*2bin
            else if (comboBox2.SelectedIndex == 5) mode = 0xc2;//12bit 2*2bin_raw8
            else if (comboBox2.SelectedIndex == 6) mode = 0x0e;//8BIT
            else if (comboBox2.SelectedIndex == 7) mode = 0xaa;//12bit_raw16
            else if (comboBox2.SelectedIndex == 8) mode = 0xbb;//12bit 2*2bin_raw16

            if (comboBox3.SelectedIndex == 0) masterslave = 0;//master
            else masterslave = 1;//slave 

           
            richTextBox1.AppendText("initialize Cmos mode is :" + mode.ToString("x") + Environment.NewLine);
            richTextBox1.AppendText("initialize Cmos mode is :" + masterslave.ToString("x") + "; 0 master;1 slave" + Environment.NewLine);


            if (sensorModel == 455)
            {
                initCMOS_IMX455 (mode);
                setSLVSEC_DecodeBit(16);

                setQHY600AnalogGain(3800);

                setQHY600OFFSET(50);

                setQHY600SHR(3490);

                Thread.Sleep(2000);
                wordAlign();
                Thread.Sleep(1000);
                AutoChannelAlign();
                Thread.Sleep(1000);

                writeFPGA(21, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(20, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(19, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(18, 255);//SET DIGITAL GAIN TO MAX
            }

            else if (sensorModel == 571)
            {
                initCMOS_IMX571(mode);
                setSLVSEC_DecodeBit(16);

                Thread.Sleep(2000);
                wordAlign();
                Thread.Sleep(1000);
                AutoChannelAlign();
                Thread.Sleep(1000);

                writeFPGA(21, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(20, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(19, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(18, 255);//SET DIGITAL GAIN TO MAX
            }
            else if (sensorModel == 410)
            {
                initCMOS_IMX410();
                setSLVSEC_DecodeBit(14);

                Thread.Sleep(2000);
                wordAlign();
                Thread.Sleep(1000);
                AutoChannelAlign();
                Thread.Sleep(1000);

                writeFPGA(21, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(20, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(19, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(18, 255);//SET DIGITAL GAIN TO MAX
            }
            else if (sensorModel == 533)
            {
                initCMOS_IMX533();
                setSLVSEC_DecodeBit(14);

                Thread.Sleep(2000);
                wordAlign();
                Thread.Sleep(1000);
                AutoChannelAlign();
                Thread.Sleep(1000);

                writeFPGA(21, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(20, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(19, 255);//SET DIGITAL GAIN TO MAX
                writeFPGA(18, 255);//SET DIGITAL GAIN TO MAX
            }

            else if (sensorModel == 492)
            {

               
                initCMOS_IMX492_MIPI_12BIT();

            }

            else if (sensorModel == 485)
            {
                // initCMOS_IMX485_10BIT();
                initCMOS_IMX485_10BIT_8CH();

                richTextBox1.AppendText("485 10bit 8ch" + Environment.NewLine);
            }
            else if (sensorModel == 530)
            {

                PCA953XWrite(0x03);//SET PCA953X io1 IO2 IO3 IO4 high

                initCMOS_IMX530();

                setSLVSEC_Decode(12);


                richTextBox1.AppendText("  INIT QHY530  " + Environment.NewLine);
            }

            else if (sensorModel == 487)  //PFY
            {

                PCA953XWrite(0x03);//SET PCA953X io1 IO2 IO3 IO4 high
                writeFPGA(8, 0);   //AMPV_enable
                writeFPGA(49, 1);  //AMPV_MANUA
                initCMOS_IMX487(mode);

                writeFPGA(30, 1);//EnableDDR

                setSLVSEC_Decode(12);

                Thread.Sleep(100);
                wordAlign();
                Thread.Sleep(100);
                AutoChannelAlign();
                Thread.Sleep(100);

                richTextBox1.AppendText("  INIT QHY487  " + Environment.NewLine);
            }
            else if (sensorModel == 661)  //PFY
            {

                PCA953XWrite(0x03);//SET PCA953X io1 IO2 IO3 IO4 high
                writeFPGA(8, 0);   //AMPV_enable
                writeFPGA(49, 1);  //AMPV_MANUA
                initCMOS_IMX661();

                writeFPGA(30, 1);//EnableDDR

                setSLVSEC_Decode(14);

                Thread.Sleep(100);
                //wordAlign();
                Thread.Sleep(100);
                AutoChannelAlign();
                Thread.Sleep(100);

                richTextBox1.AppendText("  INIT QHY661  " + Environment.NewLine);
            }
            else if (sensorModel == 811)  //PFY
            {

                PCA953XWrite(0x03);//SET PCA953X io1 IO2 IO3 IO4 high
                writeFPGA(8, 0);   //AMPV_enable
                writeFPGA(49, 1);  //AMPV_MANUA
                initCMOS_IMX811();
                writeFPGA(30, 1);//EnableDDR
                setSLVSEC_Decode(16);

                Thread.Sleep(1300);
                AutoChannelAlign();
                Thread.Sleep(1300);

                richTextBox1.AppendText("  INIT QHY811  " + Environment.NewLine);
            }
            else if (sensorModel == 990)
            {
                initCMOS_IMX990();
            }
            else if (sensorModel == 585)
            {
                MIPIRstn();
               if (masterslave == 0) SetCmosCtrlMode(0x02);//master
                else SetCmosCtrlMode(0x01);//slave 

                initCMOS_IMX585(mode, masterslave);
                richTextBox1.AppendText("  INIT IMX585 " + Environment.NewLine);
            }

            else if (sensorModel == 678)
            {
                MIPIRstn();
                if (masterslave == 0) SetCmosCtrlMode(0x02);//master
                else SetCmosCtrlMode(0x01);//slave 

                initCMOS_IMX678(mode, masterslave);
                richTextBox1.AppendText("INIT IMX678 " + Environment.NewLine);

            }
            else if (sensorModel == 2110)
            {
                MIPIRstn();
                Thread.Sleep(100);
                SetCmosCtrlMode(0x03);
                resetCMOS();
                Thread.Sleep(100);
                init_sc2210(mode);
                richTextBox1.AppendText("  INIT SC2210  " + Environment.NewLine);

            }

            else if (sensorModel == 132)
            {
                MIPIRstn();
                Thread.Sleep(100);
                SetCmosCtrlMode(0x03);
                resetCMOS();
                Thread.Sleep(100);
                init_sc132gs(mode);
                richTextBox1.AppendText("  INIT SC132GS  " + Environment.NewLine);

            }

            else if (sensorModel == 715)
            {
                MIPIRstn();
                if (masterslave == 0) SetCmosCtrlMode(0x02);//master
                else SetCmosCtrlMode(0x01);//slave 

                //IMX715
                initCMOS_IMX715(mode, masterslave);
                //TEST 415 CMOS
                //initCMOS_IMX415_TEST();
                richTextBox1.AppendText("  INIT IMX415 " + Environment.NewLine);
            }

            else if (sensorModel == 568)
            {
                MIPIRstn();
                //if (masterslave == 0) SetCmosCtrlMode(0x02);//master
                //else 
                SetCmosCtrlMode(0x01);//slave 
                writeFPGA(152, 0);   //set xtrig reset 
                resetCMOS();

                InitCMOS_IMX568(mode, masterslave);
             
                richTextBox1.AppendText("  INIT IMX568 " + Environment.NewLine);
            }
            else if (sensorModel == 6060)
            {
                init_gsense6060ERIS();
                SetVmaxMax10(3072);
                setSHR(20);
                scanAllPhase();
                FpgaAutoChannelAlign();
                writeFPGA2(35, 1);
                richTextBox1.AppendText("init 6060 " + Environment.NewLine);



            }
        }

        void resettrigmode()
        {
            writeFPGA(58, 0);//set trigmode is 0
            writeFPGA(158, 0);//set trigmodeA is 0
            writeFPGA(39, 0);//reset GPIO MODE 
        }


        void init_sc132gs(byte mode)
        {

            UInt16 hmax = 0;
            UInt16 vmax = 0;

            resettrigmode();
            setIDLE();
           // writeCMOS(0x0103, 0x01);
           // Thread.Sleep(10);
           // writeCMOS(0x0100, 0x00);

           

          
       /*
            //Preview Type:0:DVP Raw 10 bit// 1:Raw 8 bit// 2:YUV422// 3:RAW16
            //Preview Type:4:RGB565// 5:Pixart SPI// 6:MIPI 10bit// 7:MIPI 12bit// 8: MTK SPI
            //port  0:MIPI// 1:Parallel// 2:MTK// 3:SPI// 4:TEST// 5: HISPI// 6 : Z2P/Z4P
            //I2C Mode    :0:Normal 8Addr,8Data//  1:Samsung 8 Addr,8Data// 2:Micron 8 Addr,16Data
            //I2C Mode    :3:Stmicro 16Addr,8Data//4:Micron2 16 Addr,16Data
            //Out Format  :0:YCbYCr/RG_GB// 1:YCrYCb/GR_BG// 2:CbYCrY/GB_RG// 3:CrYCbY/BG_GR
            //MCLK Speed  :0:6M//1:8M//2:10M//3:11.4M//4:12M//5:12.5M//6:13.5M//7:15M//8:18M//9:24M
            //pin  :BIT0 pwdn// BIT1:reset
            //avdd  0:2.8V// 1:2.5V// 2:1.8V
            //dovdd  0:2.8V// 1:2.5V// 2:1.8V
            //dvdd  0:1.8V// 1:1.5V// 2:1.2V


            [database]
            DBName=Dothinkey

            [vendor]
            VendorName=SmartSens

            [sensor]
            SensorName=132
            width=1080
            height=1280
            port=0
            type=7
            pin=3
            SlaveID=0x60
            mode=3
            FlagReg=0x36FF
            FlagMask=0xff
            FlagData=0x00
            FlagReg1=0x36FF
            FlagMask1=0xff
            FlagData1=0x00
            outformat=3
            mclk=27
            avdd=2.500000
            dovdd=1.800000
            dvdd=1.200000

            Ext0=0
            Ext1=0
            Ext2=0
            AFVCC=0.5000
            VPP=0.000000
*/
            //[paralist]

            writeCMOS(0x0103, 0x01);//writeCMOS
            writeCMOS(0x0100, 0x00);
            writeCMOS(0x36e9, 0x80);
            writeCMOS(0x36f9, 0x80);
            writeCMOS(0x301a, 0xb4);
            writeCMOS(0x301f, 0x9f);
            writeCMOS(0x3031, 0x0c);
            writeCMOS(0x3032, 0x60);
            writeCMOS(0x3038, 0x44);
            writeCMOS(0x3207, 0x17);
            writeCMOS(0x320c, 0x02);
            writeCMOS(0x320d, 0xee);
            writeCMOS(0x3250, 0xcc);
            writeCMOS(0x3251, 0x02);
            writeCMOS(0x3252, 0x05);
            writeCMOS(0x3253, 0x41);
            writeCMOS(0x3254, 0x05);
            writeCMOS(0x3255, 0x3b);
            writeCMOS(0x3306, 0x78);
            writeCMOS(0x330a, 0x00);
            writeCMOS(0x330b, 0xc8);
            writeCMOS(0x330f, 0x24);
            writeCMOS(0x3314, 0x80);
            writeCMOS(0x3315, 0x40);
            writeCMOS(0x3317, 0xf0);
            writeCMOS(0x331f, 0x12);
            writeCMOS(0x3364, 0x00);
            writeCMOS(0x3385, 0x41);
            writeCMOS(0x3387, 0x41);
            writeCMOS(0x3389, 0x09);
            writeCMOS(0x33ab, 0x00);
            writeCMOS(0x33ac, 0x00);
            writeCMOS(0x33b1, 0x03);
            writeCMOS(0x33b2, 0x12);
            writeCMOS(0x33f8, 0x02);
            writeCMOS(0x33fa, 0x01);
            writeCMOS(0x3409, 0x08);
            writeCMOS(0x34f0, 0xc0);
            writeCMOS(0x34f1, 0x20);
            writeCMOS(0x34f2, 0x03);
            writeCMOS(0x3622, 0xf5);
            writeCMOS(0x3630, 0x5c);
            writeCMOS(0x3631, 0x80);
            writeCMOS(0x3632, 0xc8);
            writeCMOS(0x3633, 0x32);
            writeCMOS(0x3638, 0x2a);
            writeCMOS(0x3639, 0x07);
            writeCMOS(0x363b, 0x48);
            writeCMOS(0x363c, 0x83);
            writeCMOS(0x363d, 0x10);
            writeCMOS(0x36ea, 0x37);
            writeCMOS(0x36eb, 0x14);
            writeCMOS(0x36ec, 0x03);
            writeCMOS(0x36ed, 0x24);
            writeCMOS(0x36fa, 0x25);
            writeCMOS(0x36fb, 0x15);
            writeCMOS(0x36fc, 0x10);
            writeCMOS(0x36fd, 0x04);
            writeCMOS(0x3900, 0x11);
            writeCMOS(0x3901, 0x05);
            writeCMOS(0x3902, 0xc5);
            writeCMOS(0x3904, 0x04);
            writeCMOS(0x3908, 0x91);
            writeCMOS(0x391e, 0x00);
            writeCMOS(0x3e01, 0x53);
            writeCMOS(0x3e02, 0xe0);
            writeCMOS(0x3e09, 0x20);
            writeCMOS(0x3e0e, 0xd2);
            writeCMOS(0x3e14, 0xb0);
            writeCMOS(0x3e1e, 0x7c);
            writeCMOS(0x3e26, 0x20);
            writeCMOS(0x4418, 0x38);
            writeCMOS(0x4503, 0x10);
            writeCMOS(0x4837, 0x35);
            writeCMOS(0x5000, 0x0e);
            writeCMOS(0x540c, 0x51);
            writeCMOS(0x550f, 0x38);
            writeCMOS(0x5780, 0x67);
            writeCMOS(0x5784, 0x10);
            writeCMOS(0x5785, 0x06);
            writeCMOS(0x5787, 0x02);
            writeCMOS(0x5788, 0x00);
            writeCMOS(0x5789, 0x00);
            writeCMOS(0x578a, 0x02);
            writeCMOS(0x578b, 0x00);
            writeCMOS(0x578c, 0x00);
            writeCMOS(0x5790, 0x00);
            writeCMOS(0x5791, 0x00);
            writeCMOS(0x5792, 0x00);
            writeCMOS(0x5793, 0x00);
            writeCMOS(0x5794, 0x00);
            writeCMOS(0x5795, 0x00);
            writeCMOS(0x5799, 0x04);
            writeCMOS(0x36e9, 0x20);
            writeCMOS(0x36f9, 0x24);
            writeCMOS(0x0100, 0x01);
            //[gain<2]
            writeCMOS(0x33fa, 0x01);
            writeCMOS(0x3317, 0xf0);
            //
            //[gain>=2]
            //writeCMOS(0x33fa,0x02);
            //writeCMOS(0x3317,0x14);


            //MIPI Data timing adjustment 
            //writeCMOS(0x481b, 0x9c);  //set Ths-trail
            //writeCMOS(0x4821, 0x65);
            //writeCMOS(0x4829, 0x33);

            writeCMOS(0x481a, 0x01);
            writeCMOS(0x481B, 0xC0);

            writeCMOS(0x3222, 0x01);
            
            hmax = 800;
            vmax = 1338;


            setHMAX(hmax);//360   748  1999
            setVMAX(vmax);//1154 1112 1112
            releaseIDLE();

        }

        void init_sc2210(byte mode )
        {
            UInt16 hmax = 0;
            UInt16 vmax = 0;

            resettrigmode();
            setIDLE();
            writeCMOS(0x0103, 0x01);
            Thread.Sleep(10);
            writeCMOS(0x0100, 0x00);

            writeCMOS(0x481b, 0x9c);  //set Ths-trail
            writeCMOS(0x4821, 0x65);
            writeCMOS(0x4829, 0x33);

            if (mode == 0x0c)
            {
                writeCMOS(0x36e9, 0x80);
                writeCMOS(0x36f9, 0x80);
                writeCMOS(0x3001, 0x07);
                writeCMOS(0x3002, 0xc0);
                writeCMOS(0x300a, 0x2c);
                writeCMOS(0x300f, 0x00);
                writeCMOS(0x3018, 0x73);
                writeCMOS(0x3019, 0x00);
                writeCMOS(0x301f, 0x20);
                writeCMOS(0x3031, 0x0c);
                writeCMOS(0x3033, 0x20);
                writeCMOS(0x3038, 0x22);
                writeCMOS(0x3106, 0x81);
                writeCMOS(0x3201, 0x04);
                writeCMOS(0x3203, 0x04);
                writeCMOS(0x3204, 0x07);
                writeCMOS(0x3205, 0x8b);
                writeCMOS(0x3206, 0x04);
                writeCMOS(0x3207, 0x43);
                writeCMOS(0x320c, 0x04);
                writeCMOS(0x320d, 0x37);
                writeCMOS(0x320e, 0x04); ////?????????????  
                writeCMOS(0x320f, 0x58);
                writeCMOS(0x3211, 0x04);
                writeCMOS(0x3213, 0x04);
                writeCMOS(0x3231, 0x02);
                writeCMOS(0x3253, 0x04);
                writeCMOS(0x3301, 0x0a);
                writeCMOS(0x3302, 0x10);
                writeCMOS(0x3304, 0x58);
                writeCMOS(0x3305, 0x00);
                writeCMOS(0x3306, 0xb0);
                writeCMOS(0x3308, 0x20);
                writeCMOS(0x3309, 0x98);
                writeCMOS(0x330a, 0x01);
                writeCMOS(0x330b, 0x68);
                writeCMOS(0x330e, 0x48);
                writeCMOS(0x3314, 0x92);
                writeCMOS(0x331e, 0x49);
                writeCMOS(0x3000, 0xc0);
                writeCMOS(0x331f, 0x89);
                writeCMOS(0x334c, 0x10);
                writeCMOS(0x335d, 0x60);
                writeCMOS(0x335e, 0x02);
                writeCMOS(0x335f, 0x06);
                writeCMOS(0x3364, 0x16);
                writeCMOS(0x3366, 0x92);
                writeCMOS(0x3367, 0x10);
                writeCMOS(0x3368, 0x04);
                writeCMOS(0x3369, 0x00);
                writeCMOS(0x336a, 0x00);
                writeCMOS(0x336b, 0x00);
                writeCMOS(0x336d, 0x03);
                writeCMOS(0x337c, 0x08);
                writeCMOS(0x337d, 0x0e);
                writeCMOS(0x337f, 0x33);
                writeCMOS(0x3390, 0x10);
                writeCMOS(0x3391, 0x30);
                writeCMOS(0x3392, 0x40);
                writeCMOS(0x3393, 0x0a);
                writeCMOS(0x3394, 0x0a);
                writeCMOS(0x3395, 0x0a);
                writeCMOS(0x3396, 0x08);
                writeCMOS(0x3397, 0x30);
                writeCMOS(0x3398, 0x3f);
                writeCMOS(0x3399, 0x30);
                writeCMOS(0x339a, 0x30);
                writeCMOS(0x339b, 0x30);
                writeCMOS(0x339c, 0x30);
                writeCMOS(0x33a2, 0x0a);
                writeCMOS(0x33b9, 0x0e);
                writeCMOS(0x33e1, 0x08);
                writeCMOS(0x33e2, 0x18);
                writeCMOS(0x33e3, 0x18);
                writeCMOS(0x33e4, 0x18);
                writeCMOS(0x33e5, 0x10);
                writeCMOS(0x33e6, 0x06);
                writeCMOS(0x33e7, 0x02);
                writeCMOS(0x33e8, 0x18);
                writeCMOS(0x33e9, 0x10);
                writeCMOS(0x33ea, 0x0c);
                writeCMOS(0x33eb, 0x10);
                writeCMOS(0x33ec, 0x04);
                writeCMOS(0x33ed, 0x02);
                writeCMOS(0x33ee, 0xa0);
                writeCMOS(0x33ef, 0x08);
                writeCMOS(0x33f4, 0x18);
                writeCMOS(0x33f5, 0x10);
                writeCMOS(0x33f6, 0x0c);
                writeCMOS(0x33f7, 0x10);
                writeCMOS(0x33f8, 0x06);
                writeCMOS(0x33f9, 0x02);
                writeCMOS(0x33fa, 0x18);
                writeCMOS(0x33fb, 0x10);
                writeCMOS(0x33fc, 0x0c);
                writeCMOS(0x33fd, 0x10);
                writeCMOS(0x33fe, 0x04);
                writeCMOS(0x33ff, 0x02);
                writeCMOS(0x360f, 0x01);
                writeCMOS(0x3622, 0xf7);
                writeCMOS(0x3625, 0x0a);
                writeCMOS(0x3627, 0x02);
                writeCMOS(0x3630, 0xa2);
                writeCMOS(0x3631, 0x00);
                writeCMOS(0x3632, 0xd8);
                writeCMOS(0x3633, 0x43);
                writeCMOS(0x3635, 0x20);
                writeCMOS(0x3638, 0x24);
                writeCMOS(0x363a, 0x80);
                writeCMOS(0x363b, 0x02);
                writeCMOS(0x363e, 0x22);
                writeCMOS(0x3670, 0x48);
                writeCMOS(0x3671, 0xf7);
                writeCMOS(0x3672, 0xf7);
                writeCMOS(0x3673, 0x07);
                writeCMOS(0x367a, 0x40);
                writeCMOS(0x367b, 0x7f);
                writeCMOS(0x3690, 0x42);
                writeCMOS(0x3691, 0x43);
                writeCMOS(0x3692, 0x54);
                writeCMOS(0x369c, 0x40);
                writeCMOS(0x369d, 0x7f);
                writeCMOS(0x36b5, 0x40);
                writeCMOS(0x36b6, 0x7f);
                writeCMOS(0x36c0, 0x80);
                writeCMOS(0x36c1, 0x9f);
                writeCMOS(0x36c2, 0x9f);
                writeCMOS(0x36cc, 0x20);
                writeCMOS(0x36cd, 0x20);
                writeCMOS(0x36ce, 0x30);
                writeCMOS(0x36d0, 0x20);
                writeCMOS(0x36d1, 0x40);
                writeCMOS(0x36d2, 0x7f);
                writeCMOS(0x36ea, 0x38);
                writeCMOS(0x36eb, 0x0e);
                writeCMOS(0x36ec, 0x13);
                writeCMOS(0x36ed, 0x14);
                writeCMOS(0x36fa, 0x3a);
                writeCMOS(0x36fb, 0x15);
                writeCMOS(0x36fc, 0x01);
                writeCMOS(0x36fd, 0x14);
                writeCMOS(0x3905, 0xd8);
                writeCMOS(0x3907, 0x01);
                writeCMOS(0x3908, 0x11);
                writeCMOS(0x391b, 0x83);
                writeCMOS(0x391f, 0x00);
                writeCMOS(0x3933, 0x28);
                writeCMOS(0x3934, 0xa6);
                writeCMOS(0x3940, 0x70);
                writeCMOS(0x3942, 0x08);
                writeCMOS(0x3943, 0xbc);
                writeCMOS(0x3958, 0x02);
                writeCMOS(0x3959, 0x04);
                writeCMOS(0x3980, 0x61);
                writeCMOS(0x3987, 0x0b);
                writeCMOS(0x3990, 0x00);
                writeCMOS(0x3991, 0x00);
                writeCMOS(0x3992, 0x00);
                writeCMOS(0x3993, 0x00);
                writeCMOS(0x3994, 0x00);
                writeCMOS(0x3995, 0x00);
                writeCMOS(0x3996, 0x00);
                writeCMOS(0x3997, 0x00);
                writeCMOS(0x3998, 0x00);
                writeCMOS(0x3999, 0x00);
                writeCMOS(0x399a, 0x00);
                writeCMOS(0x399b, 0x00);
                writeCMOS(0x399c, 0x00);
                writeCMOS(0x399d, 0x00);
                writeCMOS(0x399e, 0x00);
                writeCMOS(0x399f, 0x00);
                writeCMOS(0x39a0, 0x00);
                writeCMOS(0x39a1, 0x00);
                writeCMOS(0x39a2, 0x03);
                writeCMOS(0x39a3, 0x30);
                writeCMOS(0x39a4, 0x03);
                writeCMOS(0x39a5, 0x60);
                writeCMOS(0x39a6, 0x03);
                writeCMOS(0x39a7, 0xa0);
                writeCMOS(0x39a8, 0x03);
                writeCMOS(0x39a9, 0xb0);
                writeCMOS(0x39aa, 0x00);
                writeCMOS(0x39ab, 0x00);
                writeCMOS(0x39ac, 0x00);
                writeCMOS(0x39ad, 0x20);
                writeCMOS(0x39ae, 0x00);
                writeCMOS(0x39af, 0x40);
                writeCMOS(0x39b0, 0x00);
                writeCMOS(0x39b1, 0x60);
                writeCMOS(0x39b2, 0x00);
                writeCMOS(0x39b3, 0x00);
                writeCMOS(0x39b4, 0x08);
                writeCMOS(0x39b5, 0x14);
                writeCMOS(0x39b6, 0x20);
                writeCMOS(0x39b7, 0x38);
                writeCMOS(0x39b8, 0x38);
                writeCMOS(0x39b9, 0x20);
                writeCMOS(0x39ba, 0x14);
                writeCMOS(0x39bb, 0x08);
                writeCMOS(0x39bc, 0x08);
                writeCMOS(0x39bd, 0x10);
                writeCMOS(0x39be, 0x20);
                writeCMOS(0x39bf, 0x30);
                writeCMOS(0x39c0, 0x30);
                writeCMOS(0x39c1, 0x20);
                writeCMOS(0x39c2, 0x10);
                writeCMOS(0x39c3, 0x08);
                writeCMOS(0x39c4, 0x00);
                writeCMOS(0x39c5, 0x80);
                writeCMOS(0x39c6, 0x00);
                writeCMOS(0x39c7, 0x80);
                writeCMOS(0x39c8, 0x00);
                writeCMOS(0x39c9, 0x00);
                writeCMOS(0x39ca, 0x80);
                writeCMOS(0x39cb, 0x00);
                writeCMOS(0x39cc, 0x00);
                writeCMOS(0x39cd, 0x00);
                writeCMOS(0x39ce, 0x00);
                writeCMOS(0x39cf, 0x00);
                writeCMOS(0x39d0, 0x00);
                writeCMOS(0x39d1, 0x00);
                writeCMOS(0x39e2, 0x05);
                writeCMOS(0x39e3, 0xeb);
                writeCMOS(0x39e4, 0x07);
                writeCMOS(0x39e5, 0xb6);
                writeCMOS(0x39e6, 0x00);
                writeCMOS(0x39e7, 0x3a);
                writeCMOS(0x39e8, 0x3f);
                writeCMOS(0x39e9, 0xb7);
                writeCMOS(0x39ea, 0x02);
                writeCMOS(0x39eb, 0x4f);
                writeCMOS(0x39ec, 0x08);
                writeCMOS(0x39ed, 0x00);
                writeCMOS(0x3e00, 0x00);
                writeCMOS(0x3e01, 0x45);
                writeCMOS(0x3e02, 0x40);
                writeCMOS(0x3e03, 0x0b);
                writeCMOS(0x3e06, 0x00);
                writeCMOS(0x3e07, 0x80);
                writeCMOS(0x3e08, 0x03);
                writeCMOS(0x3e09, 0x40);
                writeCMOS(0x3e14, 0x31);
                writeCMOS(0x3e1b, 0x3a);
                writeCMOS(0x3e26, 0x40);
                writeCMOS(0x3f08, 0x08);
                writeCMOS(0x4401, 0x1a);
                writeCMOS(0x4407, 0xc0);
                writeCMOS(0x4418, 0x34);
                writeCMOS(0x4500, 0x18);
                writeCMOS(0x4501, 0xb4);
                writeCMOS(0x4509, 0x20);
                writeCMOS(0x4603, 0x00);
                writeCMOS(0x4800, 0x24); ////  
                writeCMOS(0x4837, 0x25);
                writeCMOS(0x5000, 0x0e);
                writeCMOS(0x550f, 0x20);
                writeCMOS(0x36e9, 0x24);
                writeCMOS(0x36f9, 0x14);
                //writeCMOS(0x0100, 0x01);
                hmax = 374;
                vmax = 1111;
            }
            else if (mode == 0x0d)
            {
                writeCMOS(0x36e9, 0x80);
                writeCMOS(0x36f9, 0x80);
                writeCMOS(0x3001, 0x00);
                writeCMOS(0x3002, 0x00);
                writeCMOS(0x300a, 0x2c);
                writeCMOS(0x300f, 0x00);
                writeCMOS(0x3018, 0x73);
                writeCMOS(0x3019, 0x00);
                writeCMOS(0x301a, 0xf0);
                writeCMOS(0x301c, 0x78);
                writeCMOS(0x301f, 0x5c);
                writeCMOS(0x3031, 0x0a);
                writeCMOS(0x3032, 0x20);
                writeCMOS(0x3038, 0x22);
                writeCMOS(0x3106, 0x81);
                writeCMOS(0x3201, 0x04);
                writeCMOS(0x3203, 0x04);
                writeCMOS(0x3205, 0x8b);
                writeCMOS(0x3207, 0x43);
                writeCMOS(0x3208, 0x07);
                writeCMOS(0x3209, 0x80);
                writeCMOS(0x320a, 0x04);
                writeCMOS(0x320b, 0x38);
                writeCMOS(0x320c, 0x04);
                writeCMOS(0x320d, 0x4c);
                writeCMOS(0x320e, 0x04);
                writeCMOS(0x320f, 0xb0);
                writeCMOS(0x3211, 0x04);
                writeCMOS(0x3213, 0x04);
                writeCMOS(0x3215, 0x11);
                writeCMOS(0x3220, 0x13);
                writeCMOS(0x3221, 0x00);
                writeCMOS(0x3222, 0x00);
                writeCMOS(0x3225, 0x04);
                writeCMOS(0x322e, 0x00);
                writeCMOS(0x322f, 0x02);
                writeCMOS(0x3230, 0x00);
                writeCMOS(0x3231, 0x01);
                writeCMOS(0x3248, 0x0c);
                writeCMOS(0x3000, 0x00);
                writeCMOS(0x3249, 0x18);
                writeCMOS(0x3250, 0x00);
                writeCMOS(0x3253, 0x04);
                writeCMOS(0x3301, 0x14);
                writeCMOS(0x3302, 0x13);
                writeCMOS(0x3304, 0x48);
                writeCMOS(0x3305, 0x00);
                writeCMOS(0x3306, 0x88);
                writeCMOS(0x3308, 0x20);
                writeCMOS(0x3309, 0x60);
                writeCMOS(0x330a, 0x01);
                writeCMOS(0x330b, 0x18);
                writeCMOS(0x330d, 0x58);
                writeCMOS(0x330e, 0x70);
                writeCMOS(0x3314, 0x92);
                writeCMOS(0x331e, 0x39);
                writeCMOS(0x331f, 0x51);
                writeCMOS(0x3320, 0x09);
                writeCMOS(0x3332, 0x54);
                writeCMOS(0x334c, 0x10);
                writeCMOS(0x3350, 0x54);
                writeCMOS(0x3358, 0x54);
                writeCMOS(0x335c, 0x54);
                writeCMOS(0x335d, 0x60);
                writeCMOS(0x335e, 0x02);
                writeCMOS(0x335f, 0x04);
                writeCMOS(0x3364, 0x16);
                writeCMOS(0x3366, 0x92);
                writeCMOS(0x3367, 0x01);
                writeCMOS(0x337c, 0x06);
                writeCMOS(0x337d, 0x0a);
                writeCMOS(0x337e, 0x80);
                writeCMOS(0x3390, 0x08);
                writeCMOS(0x3391, 0x18);
                writeCMOS(0x3392, 0x38);
                writeCMOS(0x3393, 0x14);
                writeCMOS(0x3394, 0x14);
                writeCMOS(0x3395, 0x14);
                writeCMOS(0x3396, 0x08);
                writeCMOS(0x3397, 0x18);
                writeCMOS(0x3398, 0x38);
                writeCMOS(0x3399, 0x14);
                writeCMOS(0x339a, 0x30);
                writeCMOS(0x339b, 0x30);
                writeCMOS(0x339c, 0x30);
                writeCMOS(0x339e, 0x54);
                writeCMOS(0x33a0, 0x54);
                writeCMOS(0x33a2, 0x08);
                writeCMOS(0x33a4, 0x54);
                writeCMOS(0x33a8, 0x54);
                writeCMOS(0x33aa, 0x54);
                writeCMOS(0x33b0, 0x0f);
                writeCMOS(0x33b9, 0x11);
                writeCMOS(0x33e0, 0xc8);
                writeCMOS(0x33e1, 0x08);
                writeCMOS(0x33e2, 0x18);
                writeCMOS(0x33e3, 0x10);
                writeCMOS(0x33e4, 0x08);
                writeCMOS(0x33e5, 0x10);
                writeCMOS(0x33e6, 0x08);
                writeCMOS(0x33e7, 0x04);
                writeCMOS(0x33e8, 0x18);
                writeCMOS(0x33e9, 0x10);
                writeCMOS(0x33ea, 0x08);
                writeCMOS(0x33eb, 0x18);
                writeCMOS(0x33ec, 0x10);
                writeCMOS(0x33ed, 0x08);
                writeCMOS(0x33ee, 0xc8);
                writeCMOS(0x33ef, 0x08);
                writeCMOS(0x33f4, 0x18);
                writeCMOS(0x33f5, 0x10);
                writeCMOS(0x33f6, 0x08);
                writeCMOS(0x33f7, 0x10);
                writeCMOS(0x33f8, 0x08);
                writeCMOS(0x33f9, 0x04);
                writeCMOS(0x33fa, 0x18);
                writeCMOS(0x33fb, 0x10);
                writeCMOS(0x33fc, 0x08);
                writeCMOS(0x33fd, 0x18);
                writeCMOS(0x33fe, 0x10);
                writeCMOS(0x33ff, 0x08);
                writeCMOS(0x360f, 0x01);
                writeCMOS(0x3622, 0xf7);
                writeCMOS(0x3625, 0x0a);
                writeCMOS(0x3627, 0x82);
                writeCMOS(0x3630, 0xb8);
                writeCMOS(0x3631, 0x00);
                writeCMOS(0x3632, 0xd8);
                writeCMOS(0x3633, 0x43);
                writeCMOS(0x3635, 0x20);
                writeCMOS(0x3638, 0x27);
                writeCMOS(0x363a, 0x80);
                writeCMOS(0x363b, 0x02);
                writeCMOS(0x363e, 0x22);
                writeCMOS(0x3670, 0x4a);
                writeCMOS(0x3671, 0xf7);
                writeCMOS(0x3672, 0xf7);
                writeCMOS(0x3673, 0xf7);
                writeCMOS(0x3674, 0xd0);
                writeCMOS(0x3675, 0x90);
                writeCMOS(0x3676, 0x8a);
                writeCMOS(0x367a, 0x40);
                writeCMOS(0x367b, 0x78);
                writeCMOS(0x367c, 0x40);
                writeCMOS(0x367d, 0x78);
                writeCMOS(0x3690, 0x43);
                writeCMOS(0x3691, 0x54);
                writeCMOS(0x3692, 0x54);
                writeCMOS(0x369c, 0x40);
                writeCMOS(0x369d, 0x78);
                writeCMOS(0x36b5, 0x40);
                writeCMOS(0x36b6, 0x78);
                writeCMOS(0x36c0, 0x9f);
                writeCMOS(0x36c1, 0x9f);
                writeCMOS(0x36c2, 0x9f);
                writeCMOS(0x36cc, 0x22);
                writeCMOS(0x36cd, 0x22);
                writeCMOS(0x36ce, 0x28);
                writeCMOS(0x36d0, 0x20);
                writeCMOS(0x36d1, 0x40);
                writeCMOS(0x36d2, 0x78);
                writeCMOS(0x36ea, 0x35);
                writeCMOS(0x36eb, 0x04);
                writeCMOS(0x36ec, 0x0a);
                writeCMOS(0x36ed, 0x14);
                writeCMOS(0x36fa, 0x35);
                writeCMOS(0x36fb, 0x04);
                writeCMOS(0x36fc, 0x00);
                writeCMOS(0x36fd, 0x14);
                writeCMOS(0x3901, 0x02);
                writeCMOS(0x3902, 0x45);
                writeCMOS(0x3904, 0x08);
                writeCMOS(0x3905, 0x98);
                writeCMOS(0x3907, 0x01);
                writeCMOS(0x3908, 0x11);
                writeCMOS(0x391b, 0x87);
                writeCMOS(0x391d, 0x2c);
                writeCMOS(0x391f, 0x00);
                writeCMOS(0x3933, 0x28);
                writeCMOS(0x3934, 0x2c);
                writeCMOS(0x3940, 0x6f);
                writeCMOS(0x3942, 0x08);
                writeCMOS(0x3943, 0x2c);
                writeCMOS(0x3958, 0x04);
                writeCMOS(0x3959, 0x02);
                writeCMOS(0x3980, 0x61);
                writeCMOS(0x3987, 0x0b);
                writeCMOS(0x3990, 0x00);
                writeCMOS(0x3991, 0x00);
                writeCMOS(0x3992, 0x00);
                writeCMOS(0x3993, 0x00);
                writeCMOS(0x3994, 0x00);
                writeCMOS(0x3995, 0x00);
                writeCMOS(0x3996, 0x00);
                writeCMOS(0x3997, 0x00);
                writeCMOS(0x3998, 0x00);
                writeCMOS(0x3999, 0x00);
                writeCMOS(0x399a, 0x00);
                writeCMOS(0x399b, 0x00);
                writeCMOS(0x399c, 0x00);
                writeCMOS(0x399d, 0x00);
                writeCMOS(0x399e, 0x00);
                writeCMOS(0x399f, 0x00);
                writeCMOS(0x39a0, 0x00);
                writeCMOS(0x39a1, 0x00);
                writeCMOS(0x39a2, 0x03);
                writeCMOS(0x39a3, 0x30);
                writeCMOS(0x39a4, 0x03);
                writeCMOS(0x39a5, 0x60);
                writeCMOS(0x39a6, 0x03);
                writeCMOS(0x39a7, 0xa0);
                writeCMOS(0x39a8, 0x03);
                writeCMOS(0x39a9, 0xb0);
                writeCMOS(0x39aa, 0x00);
                writeCMOS(0x39ab, 0x00);
                writeCMOS(0x39ac, 0x00);
                writeCMOS(0x39ad, 0x20);
                writeCMOS(0x39ae, 0x00);
                writeCMOS(0x39af, 0x40);
                writeCMOS(0x39b0, 0x00);
                writeCMOS(0x39b1, 0x60);
                writeCMOS(0x39b2, 0x00);
                writeCMOS(0x39b3, 0x00);
                writeCMOS(0x39b4, 0x08);
                writeCMOS(0x39b5, 0x14);
                writeCMOS(0x39b6, 0x20);
                writeCMOS(0x39b7, 0x38);
                writeCMOS(0x39b8, 0x38);
                writeCMOS(0x39b9, 0x20);
                writeCMOS(0x39ba, 0x14);
                writeCMOS(0x39bb, 0x08);
                writeCMOS(0x39bc, 0x08);
                writeCMOS(0x39bd, 0x10);
                writeCMOS(0x39be, 0x20);
                writeCMOS(0x39bf, 0x30);
                writeCMOS(0x39c0, 0x30);
                writeCMOS(0x39c1, 0x20);
                writeCMOS(0x39c2, 0x10);
                writeCMOS(0x39c3, 0x08);
                writeCMOS(0x39c4, 0x00);
                writeCMOS(0x39c5, 0x80);
                writeCMOS(0x39c6, 0x00);
                writeCMOS(0x39c7, 0x80);
                writeCMOS(0x39c8, 0x00);
                writeCMOS(0x39c9, 0x00);
                writeCMOS(0x39ca, 0x80);
                writeCMOS(0x39cb, 0x00);
                writeCMOS(0x39cc, 0x00);
                writeCMOS(0x39cd, 0x00);
                writeCMOS(0x39ce, 0x00);
                writeCMOS(0x39cf, 0x00);
                writeCMOS(0x39d0, 0x00);
                writeCMOS(0x39d1, 0x00);
                writeCMOS(0x39e2, 0x05);
                writeCMOS(0x39e3, 0xeb);
                writeCMOS(0x39e4, 0x07);
                writeCMOS(0x39e5, 0xb6);
                writeCMOS(0x39e6, 0x00);
                writeCMOS(0x39e7, 0x3a);
                writeCMOS(0x39e8, 0x3f);
                writeCMOS(0x39e9, 0xb7);
                writeCMOS(0x39ea, 0x02);
                writeCMOS(0x39eb, 0x4f);
                writeCMOS(0x39ec, 0x08);
                writeCMOS(0x39ed, 0x00);
                writeCMOS(0x3e00, 0x00);
                writeCMOS(0x3e01, 0x4a);
                writeCMOS(0x3e02, 0x80);
                writeCMOS(0x3e04, 0x00);
                writeCMOS(0x3e05, 0xc0);
                writeCMOS(0x3e06, 0x00);
                writeCMOS(0x3e07, 0x80);
                writeCMOS(0x3e08, 0x03);
                writeCMOS(0x3e09, 0x40);
                writeCMOS(0x3e10, 0x00);
                writeCMOS(0x3e11, 0x80);
                writeCMOS(0x3e12, 0x03);
                writeCMOS(0x3e13, 0x40);
                writeCMOS(0x3e14, 0x31);
                writeCMOS(0x3e16, 0x00);
                writeCMOS(0x3e17, 0x80);
                writeCMOS(0x3e18, 0x00);
                writeCMOS(0x3e19, 0x80);
                writeCMOS(0x3e1b, 0x3a);
                writeCMOS(0x3e22, 0x00);
                writeCMOS(0x3e23, 0x00);
                writeCMOS(0x3e24, 0xed);
                writeCMOS(0x3e26, 0x40);
                writeCMOS(0x3e50, 0x00);
                writeCMOS(0x3e51, 0x0c);
                writeCMOS(0x3e52, 0x00);
                writeCMOS(0x3e53, 0x00);
                writeCMOS(0x3e54, 0xc9);
                writeCMOS(0x3e56, 0x00);
                writeCMOS(0x3e57, 0x80);
                writeCMOS(0x3e58, 0x03);
                writeCMOS(0x3e59, 0x40);
                writeCMOS(0x3f05, 0x18);
                writeCMOS(0x3f08, 0x10);
                writeCMOS(0x4401, 0x1a);
                writeCMOS(0x4407, 0xc0);
                writeCMOS(0x4418, 0x68);
                writeCMOS(0x4500, 0x18);
                writeCMOS(0x4501, 0xa4);
                writeCMOS(0x4503, 0xc0);
                writeCMOS(0x4505, 0x12);
                writeCMOS(0x4509, 0x10);
                writeCMOS(0x4825, 0x36);
                writeCMOS(0x4837, 0x1b);
                writeCMOS(0x4853, 0xf0);
                writeCMOS(0x5000, 0x0e);
                writeCMOS(0x550f, 0x20);
                writeCMOS(0x5900, 0x01);
                writeCMOS(0x5901, 0x00);
                writeCMOS(0x36e9, 0x20);
                writeCMOS(0x36f9, 0x30);
                hmax = 232;
                vmax = 1111;
            }


            //writeCMOS(0x36e9, 0x24);   
            //writeCMOS(0x36f9, 0x14);
            //writeCMOS(0x3037, 0x2);
            //writeCMOS(0x3651, 0x78);   

            if (masterslave == 0) writeCMOS(0x0100, 0x01);//master
            else//slave 
            {
                writeCMOS(0X3222, 0X02);
                writeCMOS(0X3230, 0X00);
                writeCMOS(0X3231, 0X04);

                writeCMOS(0X3225, 0X10);
                writeCMOS(0X321F, 0X0B);
                writeCMOS(0X3223, 0XD0);
                writeCMOS(0X3224, 0X82);

                writeCMOS(0X322E, 0X04);
                writeCMOS(0X322F, 0X54);

                writeCMOS(0x0100, 0x01);



                setHMAX(hmax);//360   748  1999
                setVMAX(vmax);//1154 1112 1112
                releaseIDLE();

            }
            //MIPIRstn();



        }
        void initCMOS_IMX990()
        {

            resetCMOS();

            Thread.Sleep(100);

            writeCMOS(0x3000, 0x01);

            // All-pixel scan  mode 12 bit

            writeCMOS(0x3006, 0x80);
            writeCMOS(0x3007, 0x01);
            writeCMOS(0x3040, 0x02);
            writeCMOS(0x3054, 0x11);
            writeCMOS(0x3056, 0x1B);
            writeCMOS(0x3058, 0x21);
            writeCMOS(0x30D0, 0x70);
            writeCMOS(0x30D4, 0x2C);
            writeCMOS(0x30D5, 0x04);
            writeCMOS(0x30D6, 0x00);
            writeCMOS(0x30D8, 0xCC);
            writeCMOS(0x30D9, 0x03);
            writeCMOS(0x30DC, 0x00);
            writeCMOS(0x30F0, 0x01);
            writeCMOS(0x3200, 0x25);
            writeCMOS(0x3204, 0x00);

            //74.25MHz
            /*
            writeCMOS(0x3220, 0x52);
            writeCMOS(0x3221, 0x20);
            writeCMOS(0x3224, 0x52);
            writeCMOS(0x3225, 0x20);
            */

            // 54MHz
            writeCMOS(0x3220, 0x50);
            writeCMOS(0x3221, 0x16);
            writeCMOS(0x3224, 0x50);
            writeCMOS(0x3225, 0x16);



            writeCMOS(0x3226, 0x93);
            writeCMOS(0x3240, 0x18);
            writeCMOS(0x3241, 0x00);
            writeCMOS(0x3242, 0x00);
            writeCMOS(0x3244, 0x06);
            writeCMOS(0x3248, 0x06);
            writeCMOS(0x324C, 0x06);
            writeCMOS(0x32D0, 0x06);
            writeCMOS(0x32E0, 0x06);
            writeCMOS(0x3302, 0x03);
            writeCMOS(0x3380, 0x07);
            writeCMOS(0x3390, 0x07);
            writeCMOS(0x3430, 0x01);
            writeCMOS(0x3444, 0x02);
            writeCMOS(0x3445, 0x03);
            writeCMOS(0x3502, 0x00);
            writeCMOS(0x3514, 0x88);		// gain setting
            writeCMOS(0x3515, 0x00);
            writeCMOS(0x3528, 0x1C);
            writeCMOS(0x352A, 0x1C);
            writeCMOS(0x352C, 0x1C);
            writeCMOS(0x352E, 0x1C);
            writeCMOS(0x3540, 0x01);
            writeCMOS(0x3544, 0x33);
            writeCMOS(0x354A, 0xA0);
            writeCMOS(0x3588, 0x32);
            writeCMOS(0x3598, 0x00);
            writeCMOS(0x35C0, 0xF0);
            writeCMOS(0x35EE, 0x08);
            writeCMOS(0x3604, 0x23);
            writeCMOS(0x3606, 0x08);
            writeCMOS(0x3608, 0x3D);
            writeCMOS(0x360A, 0x0C);
            writeCMOS(0x360C, 0x52);
            writeCMOS(0x360E, 0x02);
            writeCMOS(0x3616, 0x05);
            writeCMOS(0x3626, 0x46);
            writeCMOS(0x363C, 0x23);
            writeCMOS(0x363E, 0x2C);
            writeCMOS(0x3640, 0x3D);
            writeCMOS(0x3644, 0x52);
            writeCMOS(0x3646, 0x0C);
            writeCMOS(0x364E, 0x3D);
            writeCMOS(0x3652, 0x1F);
            writeCMOS(0x365E, 0x3E);
            writeCMOS(0x3674, 0x23);
            writeCMOS(0x3676, 0x08);
            writeCMOS(0x3678, 0x3D);
            writeCMOS(0x367A, 0x0C);
            writeCMOS(0x367C, 0x52);
            writeCMOS(0x367E, 0x02);
            writeCMOS(0x3686, 0x05);
            writeCMOS(0x3696, 0x16);
            writeCMOS(0x3762, 0x2D);
            writeCMOS(0x3e60, 0x60);
            writeCMOS(0x3e61, 0x00);
            writeCMOS(0x3e62, 0x8C);
            writeCMOS(0x3e63, 0x00);
            writeCMOS(0x3e70, 0x60);
            writeCMOS(0x3e71, 0x00);
            writeCMOS(0x3e72, 0x8C);
            writeCMOS(0x3e73, 0x00);
            writeCMOS(0x3e92, 0x00);
            writeCMOS(0x3e93, 0x00);
            writeCMOS(0x3eF0, 0x61);
            writeCMOS(0x3eF1, 0x00);
            writeCMOS(0x3eF2, 0x8B);
            writeCMOS(0x3eF3, 0x00);
            writeCMOS(0x3f04, 0x61);
            writeCMOS(0x3f05, 0x00);
            writeCMOS(0x3f06, 0x8B);
            writeCMOS(0x3f07, 0x00);
            writeCMOS(0x3f24, 0xB2);
            writeCMOS(0x3f26, 0xD0);
            writeCMOS(0x3f64, 0xB2);
            writeCMOS(0x3f66, 0xD0);
            writeCMOS(0x3f74, 0x01);
            writeCMOS(0x3f76, 0x53);
            writeCMOS(0x3fB6, 0x00);
            writeCMOS(0x3fB7, 0x00);
            writeCMOS(0x4018, 0x02);
            writeCMOS(0x401A, 0x52);
            writeCMOS(0x4048, 0xFF);
            writeCMOS(0x4049, 0x0F);
            writeCMOS(0x404E, 0xFF);
            writeCMOS(0x404F, 0x0F);
            writeCMOS(0x4058, 0xFF);
            writeCMOS(0x4059, 0x0F);
            writeCMOS(0x405E, 0xFF);
            writeCMOS(0x405F, 0x0F);
            writeCMOS(0x406A, 0x40);
            writeCMOS(0x406B, 0x00);
            writeCMOS(0x406C, 0x82);
            writeCMOS(0x406D, 0x00);
            writeCMOS(0x4098, 0x00);
            writeCMOS(0x4099, 0x00);
            writeCMOS(0x409A, 0x02);
            writeCMOS(0x409B, 0x00);
            writeCMOS(0x409C, 0x03);
            writeCMOS(0x409D, 0x00);
            writeCMOS(0x40C8, 0x00);
            writeCMOS(0x40C9, 0x00);
            writeCMOS(0x40CA, 0x15);
            writeCMOS(0x40CB, 0x00);
            writeCMOS(0x40CC, 0x16);
            writeCMOS(0x40CD, 0x00);
            writeCMOS(0x41BC, 0x15);
            writeCMOS(0x41BD, 0x09);
            writeCMOS(0x41C0, 0x41);
            writeCMOS(0x41C1, 0x41);
            writeCMOS(0x41C2, 0x41);
            writeCMOS(0x4207, 0x1F);
            writeCMOS(0x426F, 0x22);
            writeCMOS(0x4288, 0x97);
            writeCMOS(0x428C, 0x28);
            writeCMOS(0x4290, 0x97);
            writeCMOS(0x4294, 0x28);
            writeCMOS(0x4298, 0x97);
            writeCMOS(0x429C, 0x28);
            writeCMOS(0x42A0, 0x97);
            writeCMOS(0x42A4, 0x28);
            writeCMOS(0x42A8, 0x97);
            writeCMOS(0x42AC, 0x28);
            writeCMOS(0x42B0, 0x97);
            writeCMOS(0x42B4, 0x28);
            writeCMOS(0x42B8, 0x97);
            writeCMOS(0x42BC, 0x28);
            writeCMOS(0x42C0, 0x97);
            writeCMOS(0x42C4, 0x28);
            writeCMOS(0x42D4, 0x34);
            writeCMOS(0x4300, 0x3B);
            writeCMOS(0x4322, 0x3F);
            writeCMOS(0x4342, 0x3F);
            writeCMOS(0x4360, 0x65);
            writeCMOS(0x4362, 0x74);
            writeCMOS(0x4364, 0xF6);
            writeCMOS(0x4366, 0x05);
            writeCMOS(0x4370, 0x54);
            writeCMOS(0x4380, 0x40);
            writeCMOS(0x4388, 0x41);
            writeCMOS(0x43A1, 0x15);
            writeCMOS(0x43A8, 0x34);
            writeCMOS(0x43AB, 0x40);
            writeCMOS(0x43B0, 0x38);
            writeCMOS(0x43BE, 0x3D);
            writeCMOS(0x43C0, 0x34);
            writeCMOS(0x43C8, 0x38);
            writeCMOS(0x43DB, 0x1F);
            writeCMOS(0x4418, 0x76);
            writeCMOS(0x441A, 0x62);
            writeCMOS(0x4428, 0x77);
            writeCMOS(0x442A, 0x63);
            writeCMOS(0x4438, 0x78);
            writeCMOS(0x443A, 0x64);
            writeCMOS(0x4444, 0x65);
            writeCMOS(0x4446, 0x5D);
            writeCMOS(0x444C, 0x66);
            writeCMOS(0x444E, 0x5E);
            writeCMOS(0x4454, 0x67);
            writeCMOS(0x4456, 0x5F);
            writeCMOS(0x4468, 0x5A);
            writeCMOS(0x446A, 0x50);
            writeCMOS(0x4478, 0x4E);
            writeCMOS(0x447A, 0x46);
            writeCMOS(0x4488, 0x65);
            writeCMOS(0x4492, 0x52);
            writeCMOS(0x44B0, 0x31);
            writeCMOS(0x44B2, 0x39);
            writeCMOS(0x44C0, 0x31);
            writeCMOS(0x44C2, 0x39);
            writeCMOS(0x44CD, 0x20);
            writeCMOS(0x44EC, 0x44);
            writeCMOS(0x4564, 0x00);
            writeCMOS(0x4566, 0x2C);
            writeCMOS(0x456C, 0x01);
            writeCMOS(0x456E, 0x2D);
            writeCMOS(0x4573, 0xFF);
            writeCMOS(0x4628, 0xFF);
            writeCMOS(0x4629, 0x0F);
            writeCMOS(0x462A, 0x00);
            writeCMOS(0x4630, 0xFF);
            writeCMOS(0x4631, 0x0F);
            writeCMOS(0x4632, 0x00);
            writeCMOS(0x4638, 0xFF);
            writeCMOS(0x4639, 0x0F);
            writeCMOS(0x463A, 0x00);
            writeCMOS(0x4640, 0xFF);
            writeCMOS(0x4641, 0x0F);
            writeCMOS(0x4642, 0x00);
            writeCMOS(0x4648, 0xFF);
            writeCMOS(0x4649, 0x0F);
            writeCMOS(0x464A, 0x00);
            writeCMOS(0x4650, 0xFF);
            writeCMOS(0x4651, 0x0F);
            writeCMOS(0x4652, 0x00);
            writeCMOS(0x4658, 0xFF);
            writeCMOS(0x4659, 0x0F);
            writeCMOS(0x465A, 0x00);
            writeCMOS(0x4660, 0xFF);
            writeCMOS(0x4661, 0x0F);
            writeCMOS(0x4662, 0x00);
            writeCMOS(0x46A8, 0xFF);
            writeCMOS(0x46A9, 0x0F);
            writeCMOS(0x46AA, 0x00);
            writeCMOS(0x46B0, 0xFF);
            writeCMOS(0x46B1, 0x0F);
            writeCMOS(0x46B2, 0x00);
            writeCMOS(0x46B8, 0xFF);
            writeCMOS(0x46B9, 0x0F);
            writeCMOS(0x46BA, 0x00);
            writeCMOS(0x46C0, 0xFF);
            writeCMOS(0x46C1, 0x0F);
            writeCMOS(0x46C2, 0x00);
            writeCMOS(0x46C8, 0xFF);
            writeCMOS(0x46C9, 0x0F);
            writeCMOS(0x46CA, 0x00);
            writeCMOS(0x46D0, 0xFF);
            writeCMOS(0x46D1, 0x0F);
            writeCMOS(0x46D2, 0x00);
            writeCMOS(0x46D8, 0xFF);
            writeCMOS(0x46D9, 0x0F);
            writeCMOS(0x46DA, 0x00);
            writeCMOS(0x46E0, 0xFF);
            writeCMOS(0x46E1, 0x0F);
            writeCMOS(0x46E2, 0x00);
            writeCMOS(0x46E9, 0x78);
            writeCMOS(0x46EC, 0x9A);
            writeCMOS(0x46EE, 0x00);
            writeCMOS(0x46F2, 0x28);
            writeCMOS(0x46F3, 0x01);
            writeCMOS(0x46F4, 0x00);
            writeCMOS(0x46F6, 0x98);
            writeCMOS(0x46F8, 0x2A);
            writeCMOS(0x46F9, 0x01);
            writeCMOS(0x4700, 0x65);
            writeCMOS(0x470A, 0xF6);
            writeCMOS(0x4718, 0x65);
            writeCMOS(0x471A, 0x6A);
            writeCMOS(0x4720, 0xF6);
            writeCMOS(0x4722, 0xFB);
            writeCMOS(0x4723, 0x00);
            writeCMOS(0x472E, 0x73);
            writeCMOS(0x4730, 0x18);
            writeCMOS(0x4732, 0x42);
            writeCMOS(0x4738, 0x42);
            writeCMOS(0x473A, 0x18);
            writeCMOS(0x4750, 0x08);
            writeCMOS(0x4751, 0x0A);
            writeCMOS(0x4752, 0x0A);
            writeCMOS(0x4753, 0x0A);
            writeCMOS(0x4754, 0x0A);
            writeCMOS(0x4755, 0x08);
            writeCMOS(0x4756, 0x08);
            writeCMOS(0x4757, 0x08);
            writeCMOS(0x4758, 0x08);
            writeCMOS(0x4759, 0x0A);
            writeCMOS(0x475A, 0x0A);
            writeCMOS(0x475B, 0x0A);
            writeCMOS(0x475C, 0x0A);
            writeCMOS(0x475D, 0x08);
            writeCMOS(0x475E, 0x08);
            writeCMOS(0x475F, 0x08);
            writeCMOS(0x4760, 0x1A);
            writeCMOS(0x4761, 0x1C);
            writeCMOS(0x4762, 0x1C);
            writeCMOS(0x4763, 0x1C);
            writeCMOS(0x4778, 0x26);
            writeCMOS(0x477A, 0x00);
            writeCMOS(0x477B, 0x00);
            writeCMOS(0x4782, 0x2B);
            writeCMOS(0x4788, 0xB6);
            writeCMOS(0x4789, 0x00);
            writeCMOS(0x478A, 0x2B);
            writeCMOS(0x478B, 0x01);
            writeCMOS(0x4798, 0x0A);
            writeCMOS(0x47B8, 0x45);
            writeCMOS(0x47C2, 0x46);


            Thread.Sleep(100);

            writeCMOS(0x3000, 0x0);

            Thread.Sleep(100);
            writeCMOS(0x300C, 0x00);

            //init on_chip temperature sensor
            writeCMOS(0X3588, 0X30);
            Thread.Sleep(100);
            writeCMOS(0X3588, 0X31);


        }

        byte[] CMOSREG = new byte[48];

void init_gsense6060ERIS()
  {
      writeFPGA2(0, 0);
      Thread.Sleep(10);
           
    CMOSREG[0]=0x43;
    CMOSREG[1]=0Xb0;
    CMOSREG[2]=0X03;
    CMOSREG[3]=0X42;
    CMOSREG[4]=0X31;

    CMOSREG[5]=0X1b;
    CMOSREG[6]=0X38;
    CMOSREG[7]=0Xfe;
    CMOSREG[8]=0Xdd;
    CMOSREG[9]=0X3e; //2019.4.26 by Qiu  reduce the vertical FPN noise. Change from 0x3e to 0x32.

    CMOSREG[10]=0X0a;
    CMOSREG[11]=0X63;
    CMOSREG[12]=0Xe6;
    CMOSREG[13]=0X20;
    CMOSREG[14]=0X80;

    CMOSREG[15]=0X08;//2019.3.17 by Qiu lvds current . increase from 0X0a to 0x0F
    CMOSREG[16]=0Xd1;
    CMOSREG[17]=0X00;
    CMOSREG[18]=0X00;
    CMOSREG[19]=0Xa4;

    CMOSREG[20]=0Xb8;
    CMOSREG[21]=0X00;
    CMOSREG[22]=0X00;
    CMOSREG[23]=0X20;
    CMOSREG[24]=0Xc8;

    CMOSREG[25]=0X29;  //change from 0x29 to 0x28 to rmeove the background glow
    CMOSREG[26]=0X14;  //2-CMS   // change from 0x0c to 0x8c to remove the background glow
    CMOSREG[27]=0X80;
    CMOSREG[28]=0X80;
    CMOSREG[29]=0X02; //  2018.9.8 changed from 0x12 to 0x32 to enable the CMOS DDR CLOCK output. DDRCLOCK=1/2 input LVDS clock

    CMOSREG[30]=0X9a;
    CMOSREG[31]=0X30;
    CMOSREG[32]=0X52;
    CMOSREG[33]=0X9f;
    CMOSREG[34]=0X31;
    CMOSREG[35]=0X04;
    CMOSREG[36]=0Xff;
    CMOSREG[37]=0xe2;
    CMOSREG[38]=0X20;
    CMOSREG[39]=0X00;
    CMOSREG[40]=0X00;
    CMOSREG[41]=0Xea;
    CMOSREG[42]=0X1f;
    CMOSREG[43]=0X87;
    CMOSREG[44]=0X90;
    CMOSREG[45]=0X00;
    CMOSREG[46]=0X00;
    CMOSREG[47]=0X00;

    for (byte i = 0; i < 48; i++)
    {
        writeCMOS(i,CMOSREG[i]);
    }

    Thread.Sleep(10);
    writeFPGA2(0, 1);

        }

        void initCMOS_IMX415_TEST()
        {



            /*
        IMX415-AAQR All-pixel scan CSI-2_4lane 37.125Mhz AD:12bit Output:12bit 891Mbps Master Mode 30fps Integration Time 10.003ms Gain:6dB
        Ver7.0

        */


            writeFPGA(0x0, 0x0);
            writeFPGA(0x0, 0x1);             //reg0置1， CMOS复位信号XCLR拉高


            writeCMOS(0x3008, 0x7F);    // BCWAIT_TIME[9:0]
            writeCMOS(0x300A, 0x5B);    // CPWAIT_TIME[9:0]
            writeCMOS(0x3028, 0x4C);    // HMAX[15:0]
            writeCMOS(0x3029, 0x04);    //
            writeCMOS(0x3033, 0x05);    // SYS_MODE[3:0]
            writeCMOS(0x3050, 0x27);    // SHR0[19:0]
            writeCMOS(0x3051, 0x06);    //
            writeCMOS(0x3090, 0x14);    // GAIN_PCG_0[8:0]
            writeCMOS(0x30C1, 0x00);    // XVS_DRV[1:0]
            writeCMOS(0x3116, 0x24);    // INCKSEL2[7:0]
            writeCMOS(0x311E, 0x24);    // INCKSEL5[7:0]
            writeCMOS(0x32D4, 0x21);    // -
            writeCMOS(0x32EC, 0xA1);    // -
            writeCMOS(0x3452, 0x7F);    // -
            writeCMOS(0x3453, 0x03);    // -
            writeCMOS(0x358A, 0x04);    // -
            writeCMOS(0x35A1, 0x02);    // -
            writeCMOS(0x36BC, 0x0C);    // -
            writeCMOS(0x36CC, 0x53);    // -
            writeCMOS(0x36CD, 0x00);    // -
            writeCMOS(0x36CE, 0x3C);    // -
            writeCMOS(0x36D0, 0x8C);    // -
            writeCMOS(0x36D1, 0x00);    // -
            writeCMOS(0x36D2, 0x71);    // -
            writeCMOS(0x36D4, 0x3C);    // -
            writeCMOS(0x36D6, 0x53);    // -
            writeCMOS(0x36D7, 0x00);    // -
            writeCMOS(0x36D8, 0x71);    // -
            writeCMOS(0x36DA, 0x8C);    // -
            writeCMOS(0x36DB, 0x00);    // -
            writeCMOS(0x3724, 0x02);    // -
            writeCMOS(0x3726, 0x02);    // -
            writeCMOS(0x3732, 0x02);    // -
            writeCMOS(0x3734, 0x03);    // -
            writeCMOS(0x3736, 0x03);    // -
            writeCMOS(0x3742, 0x03);    // -
            writeCMOS(0x3862, 0xE0);    // -
            writeCMOS(0x38CC, 0x30);    // -
            writeCMOS(0x38CD, 0x2F);    // -
            writeCMOS(0x395C, 0x0C);    // -
            writeCMOS(0x3A42, 0xD1);    // -
            writeCMOS(0x3A4C, 0x77);    // -
            writeCMOS(0x3AE0, 0x02);    // -
            writeCMOS(0x3AEC, 0x0C);    // -
            writeCMOS(0x3B00, 0x2E);    // -
            writeCMOS(0x3B06, 0x29);    // -
            writeCMOS(0x3B98, 0x25);    // -
            writeCMOS(0x3B99, 0x21);    // -
            writeCMOS(0x3B9B, 0x13);    // -
            writeCMOS(0x3B9C, 0x13);    // -
            writeCMOS(0x3B9D, 0x13);    // -
            writeCMOS(0x3B9E, 0x13);    // -
            writeCMOS(0x3BA1, 0x00);    // -
            writeCMOS(0x3BA2, 0x06);    // -
            writeCMOS(0x3BA3, 0x0B);    // -
            writeCMOS(0x3BA4, 0x10);    // -
            writeCMOS(0x3BA5, 0x14);    // -
            writeCMOS(0x3BA6, 0x18);    // -
            writeCMOS(0x3BA7, 0x1A);    // -
            writeCMOS(0x3BA8, 0x1A);    // -
            writeCMOS(0x3BA9, 0x1A);    // -
            writeCMOS(0x3BAC, 0xED);    // -
            writeCMOS(0x3BAD, 0x01);    // -
            writeCMOS(0x3BAE, 0xF6);    // -
            writeCMOS(0x3BAF, 0x02);    // -
            writeCMOS(0x3BB0, 0xA2);    // -
            writeCMOS(0x3BB1, 0x03);    // -
            writeCMOS(0x3BB2, 0xE0);    // -
            writeCMOS(0x3BB3, 0x03);    // -
            writeCMOS(0x3BB4, 0xE0);    // -
            writeCMOS(0x3BB5, 0x03);    // -
            writeCMOS(0x3BB6, 0xE0);    // -
            writeCMOS(0x3BB7, 0x03);    // -
            writeCMOS(0x3BB8, 0xE0);    // -
            writeCMOS(0x3BBA, 0xE0);    // -
            writeCMOS(0x3BBC, 0xDA);    // -
            writeCMOS(0x3BBE, 0x88);    // -
            writeCMOS(0x3BC0, 0x44);    // -
            writeCMOS(0x3BC2, 0x7B);    // -
            writeCMOS(0x3BC4, 0xA2);    // -
            writeCMOS(0x3BC8, 0xBD);    // -
            writeCMOS(0x3BCA, 0xBD);    // -
            writeCMOS(0x4004, 0x48);    // TXCLKESC_FREQ[15:0]
            writeCMOS(0x4005, 0x09);    //
            writeCMOS(0x400C, 0x00);    // INCKSEL6
            writeCMOS(0x4018, 0x7F);    // TCLKPOST[15:0]
            writeCMOS(0x401A, 0x37);    // TCLKPREPARE[15:0]
            writeCMOS(0x401C, 0x37);    // TCLKTRAIL[15:0]
            writeCMOS(0x401E, 0xF7);    // TCLKZERO[15:0]
            writeCMOS(0x401F, 0x00);    //
            writeCMOS(0x4020, 0x3F);    // THSPREPARE[15:0]
            writeCMOS(0x4022, 0x6F);    // THSZERO[15:0]
            writeCMOS(0x4024, 0x3F);    // THSTRAIL[15:0]
            writeCMOS(0x4026, 0x5F);    // THSEXIT[15:0]
            writeCMOS(0x4028, 0x2F);    // TLPX[15:0]
            writeCMOS(0x4074, 0x01);    // INCKSEL7 [2:0]

            writeCMOS(0x3000, 0x00);
            Thread.Sleep(30);
            writeCMOS(0x3002, 0x00);
            setIDLE();
            setHMAX(1000);//224
            setVMAX(3000);//2250

            releaseIDLE();

        }

        void InitCMOS_IMX568(byte mode, byte masterslave)
        {
            writeFPGA(0x03, 0x00);

            if (mode == 0x0e)//8bit
            {
                writeFPGA(0x03, 0x01);
                //writeCMOS(0x3014, 0x*)
                //writeCMOS(0x3015, 0x*)
                //writeCMOS(0x3016, 0x*)
                //writeCMOS(0x3018, 0x*)
                //writeCMOS(0x3019, 0x*)
                //writeCMOS(0x301B, 0x*)
                writeCMOS(0x30D4, 0xA8);
                writeCMOS(0x30D5, 0x08);
                writeCMOS(0x30D6, 0x00);
                writeCMOS(0x30D8, 0x5C);
                writeCMOS(0x30D9, 0x01);

                //SET gmrwt
                writeCMOS(0x30E2, 0x04);// writeCMOS(0x30E2, 0x06);
                //set gmtwt
                writeCMOS(0x30E3, 0x1e);// writeCMOS(0x30E3, 0x2A);

                writeCMOS(0x30E6, 0x12);
                writeCMOS(0x3200, 0x25);
                //writeCMOS(0x321C, 0x*)
                //writeCMOS(0x321E, 0x*)
                //writeCMOS(0x321F, 0x*)
                //writeCMOS(0x3224, 0x*)
                writeCMOS(0x3226, 0x80);
                writeCMOS(0x3227, 0x80);

                //writeCMOS(0x3240, 0x*)
                //writeCMOS(0x3241, 0x*)
                //writeCMOS(0x3242, 0x*)
                writeCMOS(0x3430, 0x02);
                writeCMOS(0x3502, 0x08);
                //writeCMOS(0x3514, 0x*)
                //writeCMOS(0x3515, 0x*)
                writeCMOS(0x3542, 0x27);
                writeCMOS(0x354A, 0x20);
                writeCMOS(0x359C, 0x0F);
                writeCMOS(0x35A4, 0x62);
                writeCMOS(0x35A5, 0x12);
                writeCMOS(0x35A8, 0x62);
                writeCMOS(0x35A9, 0x42);
                writeCMOS(0x35AC, 0x62);
                writeCMOS(0x35B4, 0x0F);
                writeCMOS(0x35B6, 0x02);
                writeCMOS(0x35EC, 0x62);
                writeCMOS(0x35ED, 0x12);
                writeCMOS(0x35F0, 0xFB);
                writeCMOS(0x35F1, 0x0B);
                writeCMOS(0x35F2, 0xFB);
                writeCMOS(0x35F3, 0x0B);
                writeCMOS(0x3904, 0x02);
                //writeCMOS(0x3cA4, 0x);
                //writeCMOS(0x3cA5, 0x*)
                //writeCMOS(0x3cB4, 0x*)
                //writeCMOS(0x3cB5, 0x*)
                //writeCMOS(0x3cB6, 0x*)
                //writeCMOS(0x3cB7, 0x*)
                //writeCMOS(0x3cB8, 0x*)
                //writeCMOS(0x3cB9, 0x*)
                //writeCMOS(0x3cBA, 0x*)
                //writeCMOS(0x3cBB, 0x*)
                //writeCMOS(0x3cBC, 0x*)
                //writeCMOS(0x3cBD, 0x*)
                //writeCMOS(0x3cBE, 0x*)
                //writeCMOS(0x3cBF, 0x*)
                //writeCMOS(0x3cC0, 0x*)
                //writeCMOS(0x3cC1, 0x*)
                //writeCMOS(0x3cC2, 0x*)
                //writeCMOS(0x3cC3, 0x*)
                //writeCMOS(0x3cC4, 0x*)
                //writeCMOS(0x3cC5, 0x*)
                //writeCMOS(0x3cC6, 0x*)
                //writeCMOS(0x3cC7, 0x*)
                writeCMOS(0x3e30, 0x4E);
                writeCMOS(0x3e96, 0x01);
                writeCMOS(0x3eA0, 0x4C);
                writeCMOS(0x3f3A, 0x04);
                writeCMOS(0x4056, 0x23);
                writeCMOS(0x4096, 0x23);
                writeCMOS(0x4182, 0x00);
                writeCMOS(0x41A2, 0x03);
                writeCMOS(0x4232, 0x3C);
                writeCMOS(0x4306, 0x00);
                writeCMOS(0x4307, 0x00);
                writeCMOS(0x4308, 0x00);
                writeCMOS(0x4309, 0x00);
                writeCMOS(0x4310, 0x04);
                writeCMOS(0x4311, 0x04);
                writeCMOS(0x4312, 0x04);
                writeCMOS(0x4313, 0x04);
                writeCMOS(0x433C, 0x8A);
                writeCMOS(0x433D, 0x02);
                writeCMOS(0x433E, 0xE8);
                writeCMOS(0x433F, 0x05);
                writeCMOS(0x4340, 0x9E);
                writeCMOS(0x4341, 0x0C);
                writeCMOS(0x4467, 0x83);
                writeCMOS(0x446A, 0x4C);
                writeCMOS(0x446E, 0x51);
                writeCMOS(0x4472, 0x57);
                writeCMOS(0x44EC, 0x3F);
                writeCMOS(0x44F0, 0x44);
                writeCMOS(0x44F4, 0x4A);
                writeCMOS(0x4749, 0x9F);
                writeCMOS(0x474A, 0x99);
                writeCMOS(0x474B, 0x09);
                writeCMOS(0x4788, 0x04);
                writeCMOS(0x479C, 0x40);
                writeCMOS(0x4864, 0xDC);
                writeCMOS(0x4868, 0xDC);
                writeCMOS(0x486C, 0xDC);
                writeCMOS(0x48A4, 0xF4);
                writeCMOS(0x48A8, 0xF4);
                writeCMOS(0x48AC, 0xF4);

                //others
                writeCMOS(0x3942, 0x01);
                //trig mode disable 
                writeCMOS(0x3400, 0x00);
                //  slave mode
                writeCMOS(0x343c, 0xF0);
                // release standby 
                writeCMOS(0x3000, 0x00);

                setIDLE();
                setHMAX(154);//
                setVMAX(2240);//2250
                releaseIDLE();
            }
            else if (mode == 0x0c)//12bit adc
            {

                // The initialization register modification by qhyccd ysk

                //SET INCKSEL 74.25mhz is default value 
                //writeCMOS(0x3014, 0x0A);
                //writeCMOS(0x3015, 0x22);
                //writeCMOS(0x3016, 0xB1);
                //writeCMOS(0x3018, 0x40);
                //writeCMOS(0x3019, 0x04);
                //writeCMOS(0x301B, 0x3A);

                //set  VMAX 
                writeCMOS(0x30D4, 0x96);
                writeCMOS(0x30D5, 0x08);
                writeCMOS(0x30D6, 0x00);
                //set HMAX
                writeCMOS(0x30D8, 0xF6);
                writeCMOS(0x30D9, 0x01);
                //SET GMRWT 
                writeCMOS(0x30E2, 0x04);
                //SET GMTWT
                writeCMOS(0x30E3, 0x1E);
                //SET GSDLY
                writeCMOS(0x30E6, 0x0E);

                writeCMOS(0x3200, 0x15);

                //SET INCKSEL 74.25mhz is default value 
                //writeCMOS(0x321C, 0x80);
                //writeCMOS(0x321E, 0x05);
                //writeCMOS(0x321F, 0x00);
                //writeCMOS(0x3224, 0x80);
                writeCMOS(0x3226, 0x80);
                writeCMOS(0x3227, 0x80);

                //SET LLBLANK,we use default value
                //writeCMOS(0x323C, 0x19);//
                //writeCMOS(0x323D, 0x00);//
                //set shs ,we use default value
                //writeCMOS(0x3240, 0x*);
                //writeCMOS(0x3241, 0x*);
                //writeCMOS(0x3242, 0x*);

                writeCMOS(0x3430, 0x01);
                //gain rts
                writeCMOS(0x3502, 0x08);
                //set gain 
                //writeCMOS(0x3514, 0x*);
                //writeCMOS(0x3515, 0x*);

                writeCMOS(0x3542, 0x27);
                writeCMOS(0x354A, 0x20);
                writeCMOS(0x359C, 0x0F);
                writeCMOS(0x35A4, 0x30);
                writeCMOS(0x35A5, 0x12);
                writeCMOS(0x35A8, 0x30);
                writeCMOS(0x35A9, 0x42);
                writeCMOS(0x35AC, 0x62);
                writeCMOS(0x35B4, 0xF0);
                writeCMOS(0x35B6, 0x02);
                writeCMOS(0x35EC, 0x30);
                writeCMOS(0x35ED, 0x12);
                writeCMOS(0x35F0, 0xFB);
                writeCMOS(0x35F1, 0x0B);
                writeCMOS(0x35F2, 0xFB);
                writeCMOS(0x35F3, 0x0B);
                // LANESEL 
                writeCMOS(0x3904, 0x02);
                //SET INCKSEL 74.25mhz is default value 
                //writeCMOS(0x3cA4, 0x*);
                //writeCMOS(0x3cA5, 0x*);

                // Global timing register  1118Mbs IS default value 
                //writeCMOS(0x3cB4, 0x*);
                //writeCMOS(0x3cB5, 0x*);
                //writeCMOS(0x3cB6, 0x*);
                //writeCMOS(0x3cB7, 0x*);
                //writeCMOS(0x3cB8, 0x*);
                //writeCMOS(0x3cB9, 0x*);
                //writeCMOS(0x3cBA, 0x*);
                //writeCMOS(0x3cBB, 0x*);
                //writeCMOS(0x3cBC, 0x*);
                //writeCMOS(0x3cBD, 0x*);
                //writeCMOS(0x3cBE, 0x*);
                //writeCMOS(0x3cBF, 0x*);
                //writeCMOS(0x3cC0, 0x*);
                //writeCMOS(0x3cC1, 0x*);
                //writeCMOS(0x3cC2, 0x*);
                //writeCMOS(0x3cC3, 0x*);
                //writeCMOS(0x3cC4, 0x*);
                //writeCMOS(0x3cC5, 0x*);
                //writeCMOS(0x3cC6, 0x*);
                //writeCMOS(0x3cC7, 0x*);

                writeCMOS(0x3e30, 0x4E);
                writeCMOS(0x3e96, 0x01);
                writeCMOS(0x3eA0, 0x4C);
                writeCMOS(0x3f3A, 0x04);
                writeCMOS(0x4056, 0x23);
                writeCMOS(0x4096, 0x23);
                writeCMOS(0x4182, 0x00);
                writeCMOS(0x41A2, 0x03);
                writeCMOS(0x4232, 0x3C);
                writeCMOS(0x4306, 0x00);
                writeCMOS(0x4307, 0x00);
                writeCMOS(0x4308, 0x00);
                writeCMOS(0x4309, 0x00);
                writeCMOS(0x4310, 0x04);
                writeCMOS(0x4311, 0x04);
                writeCMOS(0x4312, 0x04);
                writeCMOS(0x4313, 0x04);
                writeCMOS(0x4467, 0x83);
                writeCMOS(0x4749, 0x9F);
                writeCMOS(0x474A, 0x99);
                writeCMOS(0x474B, 0x09);
                writeCMOS(0x4788, 0x04);
                writeCMOS(0x479C, 0x40);
                writeCMOS(0x4864, 0xDC);
                writeCMOS(0x4868, 0xDC);
                writeCMOS(0x486C, 0xDC);
                writeCMOS(0x48A4, 0xF4);
                writeCMOS(0x48A8, 0xF4);
                writeCMOS(0x48AC, 0xF4);

                //others
                writeCMOS(0x3942, 0x01);

                // trig mode en 
                //writeCMOS(0x3400, 0x09); 

                //trig mode disable 
                writeCMOS(0x3400, 0x00);

                //  slave mode
                writeCMOS(0x343c, 0xF0);
                // release standby 
                writeCMOS(0x3000, 0x00);  //

                setIDLE();
                setHMAX(171);//
                setVMAX(2240);//2250
                releaseIDLE();

            }
            else if (mode == 0x82)//RAW8 2*2 BIN
            {

                writeFPGA(0x03, 0x01);//RAW8 need set reg03(is16bit) 1
                // The initialization register modification by qhyccd ysk

                //SET INCKSEL 74.25mhz is default value 
                //writeCMOS(0x3014, 0x0A);
                //writeCMOS(0x3015, 0x22);
                //writeCMOS(0x3016, 0xB1);
                //writeCMOS(0x3018, 0x40);
                //writeCMOS(0x3019, 0x04);
                //writeCMOS(0x301B, 0x3A);

                //set HVMODE 
                writeCMOS(0X303C, 0x10);//2*2 mode only  
                //set vopb_vblk_hwidth
                writeCMOS(0x30d0, 0xd4);//2*2 mode only  
                writeCMOS(0x30d1, 0x04);//2*2 mode only  
                //set finfo_hwidth
                writeCMOS(0x30d2, 0xd4);//2*2 mode only  
                writeCMOS(0x30d3, 0x04);//2*2 mode only  

                //set  VMAX 
                writeCMOS(0x30D4, 0xb8);
                writeCMOS(0x30D5, 0x04);
                writeCMOS(0x30D6, 0x00);
                //set HMAX
                writeCMOS(0x30D8, 0xc2);
                writeCMOS(0x30D9, 0x00);
                //SET GMRWT 
                writeCMOS(0x30E2, 0x0c);
                //SET GMTWT
                writeCMOS(0x30E3, 0x4C);

                //SET GAINDLY
                writeCMOS(0x30E5, 0x04);//2*2 mode only 

                //SET GSDLY
                writeCMOS(0x30E6, 0x20);

                //SET  ADBIT RAW8
                writeCMOS(0x3200, 0x25);

                //SET INCKSEL 74.25mhz is default value 
                //writeCMOS(0x321C, 0x80);
                //writeCMOS(0x321E, 0x05);
                //writeCMOS(0x321F, 0x00);
                //writeCMOS(0x3224, 0x80);
                //writeCMOS(0x3225, 0x14);
                writeCMOS(0x3226, 0x80);
                writeCMOS(0x3227, 0x80);

                //SET LLBLANK 
                writeCMOS(0x323C, 0x19);//
                writeCMOS(0x323D, 0x00);//

                //SET OTHERS 
                writeCMOS(0x323E, 0x2B);//  

                //set ODbit
                writeCMOS(0x3430, 0x02);
                writeCMOS(0x3521, 0x79);
                writeCMOS(0x3546, 0x1e);
                //writeCMOS(0x35b4, 0x0f);
                //writeCMOS(0x35b5, 0x00);

                //writeCMOS(0x3cA4, 0xC0);
                //writeCMOS(0x3cA5, 0x12);



                //set shs ,we use default value
                //writeCMOS(0x3240, 0x*);
                //writeCMOS(0x3241, 0x*);
                //writeCMOS(0x3242, 0x*);



                //gain rts
                writeCMOS(0x3502, 0x08);
                //set gain 
                //writeCMOS(0x3514, 0x*);
                //writeCMOS(0x3515, 0x*);

                writeCMOS(0x3542, 0x27);
                writeCMOS(0x354A, 0x20);
                writeCMOS(0x359C, 0x0F);
                writeCMOS(0x35A4, 0x62);//**
                writeCMOS(0x35A5, 0x12);
                writeCMOS(0x35A8, 0x62);//**
                writeCMOS(0x35A9, 0x42);
                writeCMOS(0x35AC, 0x62);

                writeCMOS(0x35B6, 0x02);
                writeCMOS(0x35EC, 0x62);
                writeCMOS(0x35ED, 0x12);
                writeCMOS(0x35F0, 0xFB);
                writeCMOS(0x35F1, 0x0B);
                writeCMOS(0x35F2, 0xFB);
                writeCMOS(0x35F3, 0x0B);
                // LANESEL 
                writeCMOS(0x3904, 0x02);
                //SET INCKSEL 74.25mhz is default value 
                //writeCMOS(0x3cA4, 0x*);
                //writeCMOS(0x3cA5, 0x*);

                // Global timing register  1118Mbs IS default value 
                //writeCMOS(0x3cB4, 0x*);
                //writeCMOS(0x3cB5, 0x*);
                //writeCMOS(0x3cB6, 0x*);
                //writeCMOS(0x3cB7, 0x*);
                //writeCMOS(0x3cB8, 0x*);
                //writeCMOS(0x3cB9, 0x*);
                //writeCMOS(0x3cBA, 0x*);
                //writeCMOS(0x3cBB, 0x*);
                //writeCMOS(0x3cBC, 0x*);
                //writeCMOS(0x3cBD, 0x*);
                //writeCMOS(0x3cBE, 0x*);
                //writeCMOS(0x3cBF, 0x*);
                //writeCMOS(0x3cC0, 0x*);
                //writeCMOS(0x3cC1, 0x*);
                //writeCMOS(0x3cC2, 0x*);
                //writeCMOS(0x3cC3, 0x*);
                //writeCMOS(0x3cC4, 0x*);
                //writeCMOS(0x3cC5, 0x*);
                //writeCMOS(0x3cC6, 0x*);
                //writeCMOS(0x3cC7, 0x*);
                writeCMOS(0x3e30, 0x4E);
                writeCMOS(0x3e96, 0x01);
                writeCMOS(0x3eA0, 0x4C);
                writeCMOS(0x3f3A, 0x04);
                writeCMOS(0x4056, 0x23);
                writeCMOS(0x4096, 0x23);
                writeCMOS(0x4182, 0x00);
                writeCMOS(0x41A2, 0x03);
                writeCMOS(0x4232, 0x3C);
                writeCMOS(0x4306, 0x00);
                writeCMOS(0x4307, 0x00);
                writeCMOS(0x4308, 0x00);
                writeCMOS(0x4309, 0x00);
                writeCMOS(0x4310, 0x04);
                writeCMOS(0x4311, 0x04);
                writeCMOS(0x4312, 0x04);
                writeCMOS(0x4313, 0x04);
                writeCMOS(0x433C, 0x8A);
                writeCMOS(0x433D, 0x02);
                writeCMOS(0x433E, 0xE8);
                writeCMOS(0x433F, 0x05);
                writeCMOS(0x4340, 0x9E);
                writeCMOS(0x4341, 0x0C);
                writeCMOS(0x4467, 0x83);
                writeCMOS(0x446A, 0x4C);
                writeCMOS(0x446E, 0x51);
                writeCMOS(0x4472, 0x57);
                writeCMOS(0x44EC, 0x3F);
                writeCMOS(0x44F0, 0x44);
                writeCMOS(0x44F4, 0x4A);
                writeCMOS(0x4749, 0x9F);
                writeCMOS(0x474A, 0x99);
                writeCMOS(0x474B, 0x09);
                writeCMOS(0x4788, 0x04);
                writeCMOS(0x479C, 0x40);
                writeCMOS(0x4864, 0xDC);
                writeCMOS(0x4868, 0xDC);
                writeCMOS(0x486C, 0xDC);
                writeCMOS(0x48A4, 0xF4);
                writeCMOS(0x48A8, 0xF4);
                writeCMOS(0x48AC, 0xF4);
                //others
                writeCMOS(0x3942, 0x01);

                // trig mode en 
                //writeCMOS(0x3400, 0x09); 

                //trig mode disable 
                writeCMOS(0x3400, 0x00);

                //  slave mode
                writeCMOS(0x343c, 0xF0);
                // release standby 
                writeCMOS(0x3000, 0x00);  //

                setIDLE();
                setHMAX(72);//
                setVMAX(1208);//2250
                releaseIDLE();

            }
            else if (mode == 0xC2) //RAW12 2*2 BIN
            {

                // The initialization register modification by qhyccd ysk
                //SET INCKSEL 74.25mhz is default value 
                //writeCMOS(0x3014, 0x0A);
                //writeCMOS(0x3015, 0x22);
                //writeCMOS(0x3016, 0xB1);
                //writeCMOS(0x3018, 0x40);
                //writeCMOS(0x3019, 0x04);
                //writeCMOS(0x301B, 0x3A);

                //set HVMODE 
                writeCMOS(0X303C, 0x10);//2*2 mode only  
                //set vopb_vblk_hwidth
                writeCMOS(0x30d0, 0xd4);//2*2 mode only  
                writeCMOS(0x30d1, 0x04);//2*2 mode only  
                //set finfo_hwidth
                writeCMOS(0x30d2, 0xd4);//2*2 mode only  
                writeCMOS(0x30d3, 0x04);//2*2 mode only  

                //set  VMAX 
                writeCMOS(0x30D4, 0x98);
                writeCMOS(0x30D5, 0x04);
                writeCMOS(0x30D6, 0x00);
                //set HMAX
                writeCMOS(0x30D8, 0x0f);
                writeCMOS(0x30D9, 0x01);
                //SET GMRWT 
                writeCMOS(0x30E2, 0x08);
                //SET GMTWT
                writeCMOS(0x30E3, 0x38);

                //SET GAINDLY
                writeCMOS(0x30E5, 0x04);//2*2 mode only 

                //SET GSDLY
                writeCMOS(0x30E6, 0x18);

           

                writeCMOS(0x3200, 0x15);

                //SET INCKSEL 74.25mhz is default value 
                //writeCMOS(0x321C, 0x80);
                //writeCMOS(0x321E, 0x05);
                //writeCMOS(0x321F, 0x00);
                //writeCMOS(0x3224, 0x80);
                writeCMOS(0x3226, 0x80);
                writeCMOS(0x3227, 0x80);

                //SET LLBLANK,we use default value
                //writeCMOS(0x323C, 0x19);//
                //writeCMOS(0x323D, 0x00);//
                writeCMOS(0x323E, 0x2B);//  
                //set shs ,we use default value
                //writeCMOS(0x3240, 0x*);
                //writeCMOS(0x3241, 0x*);
                //writeCMOS(0x3242, 0x*);
                writeCMOS(0x3521, 0x79);
                writeCMOS(0x3546, 0x1e);

                writeCMOS(0x3430, 0x01);
                //gain rts
                writeCMOS(0x3502, 0x08);
                //set gain 
                //writeCMOS(0x3514, 0x*);
                //writeCMOS(0x3515, 0x*);

                writeCMOS(0x3542, 0x27);
                writeCMOS(0x354A, 0x20);
                writeCMOS(0x359C, 0x0F);
                writeCMOS(0x35A4, 0x30);
                writeCMOS(0x35A5, 0x12);
                writeCMOS(0x35A8, 0x30);
                writeCMOS(0x35A9, 0x42);
                writeCMOS(0x35AC, 0x62);
                writeCMOS(0x35B4, 0xF0);
                writeCMOS(0x35B6, 0x02);
                writeCMOS(0x35EC, 0x30);
                writeCMOS(0x35ED, 0x12);
                writeCMOS(0x35F0, 0xFB);
                writeCMOS(0x35F1, 0x0B);
                writeCMOS(0x35F2, 0xFB);
                writeCMOS(0x35F3, 0x0B);
                // LANESEL 
                writeCMOS(0x3904, 0x02);
                //SET INCKSEL 74.25mhz is default value 
                //writeCMOS(0x3cA4, 0x*);
                //writeCMOS(0x3cA5, 0x*);

                // Global timing register  1118Mbs IS default value 
                //writeCMOS(0x3cB4, 0x*);
                //writeCMOS(0x3cB5, 0x*);
                //writeCMOS(0x3cB6, 0x*);
                //writeCMOS(0x3cB7, 0x*);
                //writeCMOS(0x3cB8, 0x*);
                //writeCMOS(0x3cB9, 0x*);
                //writeCMOS(0x3cBA, 0x*);
                //writeCMOS(0x3cBB, 0x*);
                //writeCMOS(0x3cBC, 0x*);
                //writeCMOS(0x3cBD, 0x*);
                //writeCMOS(0x3cBE, 0x*);
                //writeCMOS(0x3cBF, 0x*);
                //writeCMOS(0x3cC0, 0x*);
                //writeCMOS(0x3cC1, 0x*);
                //writeCMOS(0x3cC2, 0x*);
                //writeCMOS(0x3cC3, 0x*);
                //writeCMOS(0x3cC4, 0x*);
                //writeCMOS(0x3cC5, 0x*);
                //writeCMOS(0x3cC6, 0x*);
                //writeCMOS(0x3cC7, 0x*);

                writeCMOS(0x3e30, 0x4E);
                writeCMOS(0x3e96, 0x01);
                writeCMOS(0x3eA0, 0x4C);
                writeCMOS(0x3f3A, 0x04);
                writeCMOS(0x4056, 0x23);
                writeCMOS(0x4096, 0x23);
                writeCMOS(0x4182, 0x00);
                writeCMOS(0x41A2, 0x03);
                writeCMOS(0x4232, 0x3C);
                writeCMOS(0x4306, 0x00);
                writeCMOS(0x4307, 0x00);
                writeCMOS(0x4308, 0x00);
                writeCMOS(0x4309, 0x00);
                writeCMOS(0x4310, 0x04);
                writeCMOS(0x4311, 0x04);
                writeCMOS(0x4312, 0x04);
                writeCMOS(0x4313, 0x04);
                writeCMOS(0x4467, 0x83);
                writeCMOS(0x4749, 0x9F);
                writeCMOS(0x474A, 0x99);
                writeCMOS(0x474B, 0x09);
                writeCMOS(0x4788, 0x04);
                writeCMOS(0x479C, 0x40);
                writeCMOS(0x4864, 0xDC);
                writeCMOS(0x4868, 0xDC);
                writeCMOS(0x486C, 0xDC);
                writeCMOS(0x48A4, 0xF4);
                writeCMOS(0x48A8, 0xF4);
                writeCMOS(0x48AC, 0xF4);

                //others
                writeCMOS(0x3942, 0x01);

                // trig mode en 
                //writeCMOS(0x3400, 0x09); 

                //trig mode disable 
                writeCMOS(0x3400, 0x00);

                //  slave mode
                writeCMOS(0x343c, 0xF0);
                // release standby 
                writeCMOS(0x3000, 0x00);  //

                setIDLE();
                setHMAX(144);//
                setVMAX(1208);//2250
                releaseIDLE();

            }

 
        }

        void initCMOS_IMX715(byte mode, byte masterslave)
        {
            //resetCMOS();
           // Thread.Sleep(100);
            writeCMOS(0x3000, 0x01);

            /*
        IMX415-AAQR All-pixel scan CSI-2_4lane 37.125Mhz AD:12bit Output:12bit 891Mbps Master Mode 30fps Integration Time 10.003ms Gain:6dB
        Ver7.0
            */

            /*
  
            writeCMOS(0x3008, 0x7F);    // BCWAIT_TIME[9:0]
            writeCMOS(0x300A, 0x5B);    // CPWAIT_TIME[9:0]
            writeCMOS(0x3028, 0x4C);    // HMAX[15:0]
            writeCMOS(0x3029, 0x04);    //
            writeCMOS(0x3033, 0x05);    // SYS_MODE[3:0]
            writeCMOS(0x3050, 0x27);    // SHR0[19:0]
            writeCMOS(0x3051, 0x06);    //
            writeCMOS(0x3090, 0x14);    // GAIN_PCG_0[8:0]
            writeCMOS(0x30C1, 0x00);    // XVS_DRV[1:0]
            writeCMOS(0x3116, 0x24);    // INCKSEL2[7:0]
            writeCMOS(0x311E, 0x24);    // INCKSEL5[7:0]
            writeCMOS(0x32D4, 0x21);    // -
            writeCMOS(0x32EC, 0xA1);    // -
            writeCMOS(0x3452, 0x7F);    // -
            writeCMOS(0x3453, 0x03);    // -
            writeCMOS(0x358A, 0x04);    // -
            writeCMOS(0x35A1, 0x02);    // -
            writeCMOS(0x36BC, 0x0C);    // -
            writeCMOS(0x36CC, 0x53);    // -
            writeCMOS(0x36CD, 0x00);    // -
            writeCMOS(0x36CE, 0x3C);    // -
            writeCMOS(0x36D0, 0x8C);    // -
            writeCMOS(0x36D1, 0x00);    // -
            writeCMOS(0x36D2, 0x71);    // -
            writeCMOS(0x36D4, 0x3C);    // -
            writeCMOS(0x36D6, 0x53);    // -
            writeCMOS(0x36D7, 0x00);    // -
            writeCMOS(0x36D8, 0x71);    // -
            writeCMOS(0x36DA, 0x8C);    // -
            writeCMOS(0x36DB, 0x00);    // -
            writeCMOS(0x3724, 0x02);    // -
            writeCMOS(0x3726, 0x02);    // -
            writeCMOS(0x3732, 0x02);    // -
            writeCMOS(0x3734, 0x03);    // -
            writeCMOS(0x3736, 0x03);    // -
            writeCMOS(0x3742, 0x03);    // -
            writeCMOS(0x3862, 0xE0);    // -
            writeCMOS(0x38CC, 0x30);    // -
            writeCMOS(0x38CD, 0x2F);    // -
            writeCMOS(0x395C, 0x0C);    // -
            writeCMOS(0x3A42, 0xD1);    // -
            writeCMOS(0x3A4C, 0x77);    // -
            writeCMOS(0x3AE0, 0x02);    // -
            writeCMOS(0x3AEC, 0x0C);    // -
            writeCMOS(0x3B00, 0x2E);    // -
            writeCMOS(0x3B06, 0x29);    // -
            writeCMOS(0x3B98, 0x25);    // -
            writeCMOS(0x3B99, 0x21);    // -
            writeCMOS(0x3B9B, 0x13);    // -
            writeCMOS(0x3B9C, 0x13);    // -
            writeCMOS(0x3B9D, 0x13);    // -
            writeCMOS(0x3B9E, 0x13);    // -
            writeCMOS(0x3BA1, 0x00);    // -
            writeCMOS(0x3BA2, 0x06);    // -
            writeCMOS(0x3BA3, 0x0B);    // -
            writeCMOS(0x3BA4, 0x10);    // -
            writeCMOS(0x3BA5, 0x14);    // -
            writeCMOS(0x3BA6, 0x18);    // -
            writeCMOS(0x3BA7, 0x1A);    // -
            writeCMOS(0x3BA8, 0x1A);    // -
            writeCMOS(0x3BA9, 0x1A);    // -
            writeCMOS(0x3BAC, 0xED);    // -
            writeCMOS(0x3BAD, 0x01);    // -
            writeCMOS(0x3BAE, 0xF6);    // -
            writeCMOS(0x3BAF, 0x02);    // -
            writeCMOS(0x3BB0, 0xA2);    // -
            writeCMOS(0x3BB1, 0x03);    // -
            writeCMOS(0x3BB2, 0xE0);    // -
            writeCMOS(0x3BB3, 0x03);    // -
            writeCMOS(0x3BB4, 0xE0);    // -
            writeCMOS(0x3BB5, 0x03);    // -
            writeCMOS(0x3BB6, 0xE0);    // -
            writeCMOS(0x3BB7, 0x03);    // -
            writeCMOS(0x3BB8, 0xE0);    // -
            writeCMOS(0x3BBA, 0xE0);    // -
            writeCMOS(0x3BBC, 0xDA);    // -
            writeCMOS(0x3BBE, 0x88);    // -
            writeCMOS(0x3BC0, 0x44);    // -
            writeCMOS(0x3BC2, 0x7B);    // -
            writeCMOS(0x3BC4, 0xA2);    // -
            writeCMOS(0x3BC8, 0xBD);    // -
            writeCMOS(0x3BCA, 0xBD);    // -
            writeCMOS(0x4004, 0x48);    // TXCLKESC_FREQ[15:0]
            writeCMOS(0x4005, 0x09);    //
            writeCMOS(0x400C, 0x00);    // INCKSEL6
            writeCMOS(0x4018, 0x7F);    // TCLKPOST[15:0]
            writeCMOS(0x401A, 0x37);    // TCLKPREPARE[15:0]
            writeCMOS(0x401C, 0x37);    // TCLKTRAIL[15:0]
            writeCMOS(0x401E, 0xF7);    // TCLKZERO[15:0]
            writeCMOS(0x401F, 0x00);    //
            writeCMOS(0x4020, 0x3F);    // THSPREPARE[15:0]
            writeCMOS(0x4022, 0x6F);    // THSZERO[15:0]
            writeCMOS(0x4024, 0x3F);    // THSTRAIL[15:0]
            writeCMOS(0x4026, 0x5F);    // THSEXIT[15:0]
            writeCMOS(0x4028, 0x2F);    // TLPX[15:0]
            writeCMOS(0x4074, 0x01);    // INCKSEL7 [2:0]

            writeCMOS(0x3000, 0x00);
            Thread.Sleep(30);
            writeCMOS(0x3002, 0x00);

             */




            /*
IMX715-AAQR1 All-pixel scan CSI-2_4lane 74.25MHz AD:12bit Output:12bit 1485Mbps Slave Mode 49.991fps Integration Time 9.997ms Gain:6dB
Ver1.0  2022 08 30 
            */

            

            writeCMOS(0x3003, 0x01);  // XMASTER set slavemode
            writeCMOS(0x3033, 0x08);  // SYS_MODE[3:0] set MIPI speed ;8 is 1440Mbps
            writeCMOS(0x3050, 0x92);  // SHR0[19:0]
            writeCMOS(0x3051, 0x04);  // 
            writeCMOS(0x3090, 0x14);  // GAIN_PCG_0[8:0]
            writeCMOS(0x3118, 0xA0);  // INCKSEL3[10:0]
            writeCMOS(0x32D4, 0x21);  // -
            writeCMOS(0x32EC, 0xA1);  // -
            writeCMOS(0x344C, 0x2B);  // -
            writeCMOS(0x344D, 0x01);  // -
            writeCMOS(0x344E, 0xED);  // -
            writeCMOS(0x344F, 0x01);  // -
            writeCMOS(0x3450, 0xF6);  // -
            writeCMOS(0x3451, 0x02);  // -
            writeCMOS(0x3452, 0x7F);  // -
            writeCMOS(0x3453, 0x03);  // -
            writeCMOS(0x358A, 0x04);  // -
            writeCMOS(0x35A1, 0x02);  // -
            writeCMOS(0x35EC, 0x27);  // -
            writeCMOS(0x35EE, 0x8D);  // -
            writeCMOS(0x35F0, 0x8D);  // -
            writeCMOS(0x35F2, 0x29);  // -
            writeCMOS(0x36BC, 0x0C);  // -
            writeCMOS(0x36CC, 0x53);  // -
            writeCMOS(0x36CD, 0x00);  // -
            writeCMOS(0x36CE, 0x3C);  // -
            writeCMOS(0x36D0, 0x8C);  // -
            writeCMOS(0x36D1, 0x00);  // -
            writeCMOS(0x36D2, 0x71);  // -
            writeCMOS(0x36D4, 0x3C);  // -
            writeCMOS(0x36D6, 0x53);  // -
            writeCMOS(0x36D7, 0x00);  // -
            writeCMOS(0x36D8, 0x71);  // -
            writeCMOS(0x36DA, 0x8C);  // -
            writeCMOS(0x36DB, 0x00);  // -
            writeCMOS(0x3720, 0x00);  // -
            writeCMOS(0x3724, 0x02);  // -
            writeCMOS(0x3726, 0x02);  // -
            writeCMOS(0x3732, 0x02);  // -
            writeCMOS(0x3734, 0x03);  // -
            writeCMOS(0x3736, 0x03);  // -
            writeCMOS(0x3742, 0x03);  // -
            writeCMOS(0x3862, 0xE0);  // -
            writeCMOS(0x38CC, 0x30);  // -
            writeCMOS(0x38CD, 0x2F);  // -
            writeCMOS(0x395C, 0x0C);  // -
            writeCMOS(0x39A4, 0x07);  // -
            writeCMOS(0x39A8, 0x32);  // -
            writeCMOS(0x39AA, 0x32);  // -
            writeCMOS(0x39AC, 0x32);  // -
            writeCMOS(0x39AE, 0x32);  // -
            writeCMOS(0x39B0, 0x32);  // -
            writeCMOS(0x39B2, 0x2F);  // -
            writeCMOS(0x39B4, 0x2D);  // -
            writeCMOS(0x39B6, 0x28);  // -
            writeCMOS(0x39B8, 0x30);  // -
            writeCMOS(0x39BA, 0x30);  // -
            writeCMOS(0x39BC, 0x30);  // -
            writeCMOS(0x39BE, 0x30);  // -
            writeCMOS(0x39C0, 0x30);  // -
            writeCMOS(0x39C2, 0x2E);  // -
            writeCMOS(0x39C4, 0x2B);  // -
            writeCMOS(0x39C6, 0x25);  // -
            writeCMOS(0x3A42, 0xD1);  // -
            writeCMOS(0x3A4C, 0x77);  // -
            writeCMOS(0x3AE0, 0x02);  // -
            writeCMOS(0x3AEC, 0x0C);  // -
            writeCMOS(0x3B00, 0x2E);  // -
            writeCMOS(0x3B06, 0x29);  // -
            writeCMOS(0x3B98, 0x25);  // -
            writeCMOS(0x3B99, 0x21);  // -
            writeCMOS(0x3B9B, 0x13);  // -
            writeCMOS(0x3B9C, 0x13);  // -
            writeCMOS(0x3B9D, 0x13);  // -
            writeCMOS(0x3B9E, 0x13);  // -
            writeCMOS(0x3BA1, 0x00);  // -
            writeCMOS(0x3BA2, 0x06);  // -
            writeCMOS(0x3BA3, 0x0B);  // -
            writeCMOS(0x3BA4, 0x10);  // -
            writeCMOS(0x3BA5, 0x14);  // -
            writeCMOS(0x3BA6, 0x18);  // -
            writeCMOS(0x3BA7, 0x1A);  // -
            writeCMOS(0x3BA8, 0x1A);  // -
            writeCMOS(0x3BA9, 0x1A);  // -
            writeCMOS(0x3BAC, 0xED);  // -
            writeCMOS(0x3BAD, 0x01);  // -
            writeCMOS(0x3BAE, 0xF6);  // -
            writeCMOS(0x3BAF, 0x02);  // -
            writeCMOS(0x3BB0, 0xA2);  // -
            writeCMOS(0x3BB1, 0x03);  // -
            writeCMOS(0x3BB2, 0xE0);  // -
            writeCMOS(0x3BB3, 0x03);  // -
            writeCMOS(0x3BB4, 0xE0);  // -
            writeCMOS(0x3BB5, 0x03);  // -
            writeCMOS(0x3BB6, 0xE0);  // -
            writeCMOS(0x3BB7, 0x03);  // -
            writeCMOS(0x3BB8, 0xE0);  // -
            writeCMOS(0x3BBA, 0xE0);  // -
            writeCMOS(0x3BBC, 0xDA);  // -
            writeCMOS(0x3BBE, 0x88);  // -
            writeCMOS(0x3BC0, 0x44);  // -
            writeCMOS(0x3BC2, 0x7B);  // -
            writeCMOS(0x3BC4, 0xA2);  // -
            writeCMOS(0x3BC8, 0xBD);  // -
            writeCMOS(0x3BCA, 0xBD);  // -
            writeCMOS(0x4018, 0xA7);  // TCLKPOST[15:0]
            writeCMOS(0x401A, 0x57);  // TCLKPREPARE[15:0]
            writeCMOS(0x401C, 0x5F);  // TCLKTRAIL[15:0]
            writeCMOS(0x401E, 0x97);  // TCLKZERO[15:0]
            writeCMOS(0x4020, 0x5F);  // THSPREPARE[15:0]
            writeCMOS(0x4022, 0xAF);  // THSZERO[15:0]
            writeCMOS(0x4024, 0x5F);  // THSTRAIL[15:0]
            writeCMOS(0x4026, 0x9F);  // THSEXIT[15:0]
            writeCMOS(0x4028, 0x4F);  // TLPX[15:0]
            
            
           
      
            
            /*
IMX715-AAQR1 All-pixel scan CSI-2_4lane 74.25MHz AD:12bit Output:12bit 891Mbps Slave Mode  Integration Time 9.997ms Gain:6dB
Ver1.0  2022 08 30  ysk 
            */

            /*
            writeCMOS(0x3003, 0x01);  // XMASTER set slavemode
            writeCMOS(0x3033, 0x05);  // SYS_MODE[3:0] set MIPI speed ;5 is 891Mbps
            writeCMOS(0x3050, 0x92);  // SHR0[19:0]
            writeCMOS(0x3051, 0x04);  // 
            writeCMOS(0x3090, 0x14);  // GAIN_PCG_0[8:0]
            //writeCMOS(0x3118,0xA0);  // INCKSEL3[10:0]
            writeCMOS(0x400c, 0x00);  //INCKSEL6
            writeCMOS(0x4074, 0x01);  //INCKSEL7
            writeCMOS(0x32D4, 0x21);  // -
            writeCMOS(0x32EC, 0xA1);  // -
            writeCMOS(0x344C, 0x2B);  // -
            writeCMOS(0x344D, 0x01);  // -
            writeCMOS(0x344E, 0xED);  // -
            writeCMOS(0x344F, 0x01);  // -
            writeCMOS(0x3450, 0xF6);  // -
            writeCMOS(0x3451, 0x02);  // -
            writeCMOS(0x3452, 0x7F);  // -
            writeCMOS(0x3453, 0x03);  // -
            writeCMOS(0x358A, 0x04);  // -
            writeCMOS(0x35A1, 0x02);  // -
            writeCMOS(0x35EC, 0x27);  // -
            writeCMOS(0x35EE, 0x8D);  // -
            writeCMOS(0x35F0, 0x8D);  // -
            writeCMOS(0x35F2, 0x29);  // -
            writeCMOS(0x36BC, 0x0C);  // -
            writeCMOS(0x36CC, 0x53);  // -
            writeCMOS(0x36CD, 0x00);  // -
            writeCMOS(0x36CE, 0x3C);  // -
            writeCMOS(0x36D0, 0x8C);  // -
            writeCMOS(0x36D1, 0x00);  // -
            writeCMOS(0x36D2, 0x71);  // -
            writeCMOS(0x36D4, 0x3C);  // -
            writeCMOS(0x36D6, 0x53);  // -
            writeCMOS(0x36D7, 0x00);  // -
            writeCMOS(0x36D8, 0x71);  // -
            writeCMOS(0x36DA, 0x8C);  // -
            writeCMOS(0x36DB, 0x00);  // -
            writeCMOS(0x3720, 0x00);  // -
            writeCMOS(0x3724, 0x02);  // -
            writeCMOS(0x3726, 0x02);  // -
            writeCMOS(0x3732, 0x02);  // -
            writeCMOS(0x3734, 0x03);  // -
            writeCMOS(0x3736, 0x03);  // -
            writeCMOS(0x3742, 0x03);  // -
            writeCMOS(0x3862, 0xE0);  // -
            writeCMOS(0x38CC, 0x30);  // -
            writeCMOS(0x38CD, 0x2F);  // -
            writeCMOS(0x395C, 0x0C);  // -
            writeCMOS(0x39A4, 0x07);  // -
            writeCMOS(0x39A8, 0x32);  // -
            writeCMOS(0x39AA, 0x32);  // -
            writeCMOS(0x39AC, 0x32);  // -
            writeCMOS(0x39AE, 0x32);  // -
            writeCMOS(0x39B0, 0x32);  // -
            writeCMOS(0x39B2, 0x2F);  // -
            writeCMOS(0x39B4, 0x2D);  // -
            writeCMOS(0x39B6, 0x28);  // -
            writeCMOS(0x39B8, 0x30);  // -
            writeCMOS(0x39BA, 0x30);  // -
            writeCMOS(0x39BC, 0x30);  // -
            writeCMOS(0x39BE, 0x30);  // -
            writeCMOS(0x39C0, 0x30);  // -
            writeCMOS(0x39C2, 0x2E);  // -
            writeCMOS(0x39C4, 0x2B);  // -
            writeCMOS(0x39C6, 0x25);  // -
            writeCMOS(0x3A42, 0xD1);  // -
            writeCMOS(0x3A4C, 0x77);  // -
            writeCMOS(0x3AE0, 0x02);  // -
            writeCMOS(0x3AEC, 0x0C);  // -
            writeCMOS(0x3B00, 0x2E);  // -
            writeCMOS(0x3B06, 0x29);  // -
            writeCMOS(0x3B98, 0x25);  // -
            writeCMOS(0x3B99, 0x21);  // -
            writeCMOS(0x3B9B, 0x13);  // -
            writeCMOS(0x3B9C, 0x13);  // -
            writeCMOS(0x3B9D, 0x13);  // -
            writeCMOS(0x3B9E, 0x13);  // -
            writeCMOS(0x3BA1, 0x00);  // -
            writeCMOS(0x3BA2, 0x06);  // -
            writeCMOS(0x3BA3, 0x0B);  // -
            writeCMOS(0x3BA4, 0x10);  // -
            writeCMOS(0x3BA5, 0x14);  // -
            writeCMOS(0x3BA6, 0x18);  // -
            writeCMOS(0x3BA7, 0x1A);  // -    
            writeCMOS(0x3BA8, 0x1A);  // -
            writeCMOS(0x3BA9, 0x1A);  // -
            writeCMOS(0x3BAC, 0xED);  // -
            writeCMOS(0x3BAD, 0x01);  // -
            writeCMOS(0x3BAE, 0xF6);  // -
            writeCMOS(0x3BAF, 0x02);  // -
            writeCMOS(0x3BB0, 0xA2);  // -
            writeCMOS(0x3BB1, 0x03);  // -
            writeCMOS(0x3BB2, 0xE0);  // -
            writeCMOS(0x3BB3, 0x03);  // -
            writeCMOS(0x3BB4, 0xE0);  // -
            writeCMOS(0x3BB5, 0x03);  // -
            writeCMOS(0x3BB6, 0xE0);  // -
            writeCMOS(0x3BB7, 0x03);  // -
            writeCMOS(0x3BB8, 0xE0);  // -
            writeCMOS(0x3BBA, 0xE0);  // -
            writeCMOS(0x3BBC, 0xDA);  // -
            writeCMOS(0x3BBE, 0x88);  // -
            writeCMOS(0x3BC0, 0x44);  // -
            writeCMOS(0x3BC2, 0x7B);  // -
            writeCMOS(0x3BC4, 0xA2);  // -
            writeCMOS(0x3BC8, 0xBD);  // -
            writeCMOS(0x3BCA, 0xBD);  // -
            writeCMOS(0x4018, 0xA7);  // TCLKPOST[15:0]
            writeCMOS(0x401A, 0x57);  // TCLKPREPARE[15:0]
            writeCMOS(0x401C, 0x5F);  // TCLKTRAIL[15:0]
            writeCMOS(0x401E, 0x97);  // TCLKZERO[15:0]
            writeCMOS(0x4020, 0x5F);  // THSPREPARE[15:0]
            writeCMOS(0x4022, 0xAF);  // THSZERO[15:0]
            writeCMOS(0x4024, 0x5F);  // THSTRAIL[15:0]
            writeCMOS(0x4026, 0x9F);  // THSEXIT[15:0]
            writeCMOS(0x4028, 0x4F);  // TLPX[15:0]
          
            */

            writeCMOS(0x3000, 0x00);  //
            setIDLE();
            setHMAX(262);//
            setVMAX(2250);//2250

            releaseIDLE();

        }

        void initCMOS_IMX678(byte mode, byte masterslave)
        {

            UInt16 hmax_init = 1100;
            UInt16 hmax;
            UInt16 vmax;

            resetCMOS();
            Thread.Sleep(50);
            //writeCMOS(0x3000, 0x01);

            if (mode == 0x0a)//16bit
            { }
            else if (mode == 0x0b)//14bit
            { }
            else if (mode == 0x0c)//12bit
            {
                
                
                //"All-pixel scan CSI-2_4lane
                //74.25MHz
                //AD:12bit Output:12bit
                //1188Mbps
                //Slave  Mode
                //LCG Mode
                //30fps
                //Integration Time
                //33.289ms"

                writeCMOS(0x3015, 0x04);
                writeCMOS(0x3050, 0x03);
                //writeCMOS(0x30A6, 0x00);
                writeCMOS(0x3460, 0x22);
                writeCMOS(0x355A, 0x64);
                writeCMOS(0x3A02, 0x7A);
                writeCMOS(0x3A10, 0xEC);
                writeCMOS(0x3A12, 0x71);
                writeCMOS(0x3A14, 0xDE);
                writeCMOS(0x3A20, 0x2B);
                writeCMOS(0x3A24, 0x22);
                writeCMOS(0x3A25, 0x25);
                writeCMOS(0x3A26, 0x2A);
                writeCMOS(0x3A27, 0x2C);
                writeCMOS(0x3A28, 0x39);
                writeCMOS(0x3A29, 0x38);
                writeCMOS(0x3A30, 0x04);
                writeCMOS(0x3A31, 0x04);
                writeCMOS(0x3A32, 0x03);
                writeCMOS(0x3A33, 0x03);
                writeCMOS(0x3A34, 0x09);
                writeCMOS(0x3A35, 0x06);
                writeCMOS(0x3A38, 0xCD);
                writeCMOS(0x3A3A, 0x4C);
                writeCMOS(0x3A3C, 0xB9);
                writeCMOS(0x3A3E, 0x30);
                writeCMOS(0x3A40, 0x2C);
                writeCMOS(0x3A42, 0x39);
                writeCMOS(0x3A4E, 0x00);
                writeCMOS(0x3A52, 0x00);
                writeCMOS(0x3A56, 0x00);
                writeCMOS(0x3A5A, 0x00);
                writeCMOS(0x3A5E, 0x00);
                writeCMOS(0x3A62, 0x00);
                writeCMOS(0x3A6E, 0xA0);
                writeCMOS(0x3A70, 0x50);
                writeCMOS(0x3A8C, 0x04);
                writeCMOS(0x3A8D, 0x03);
                writeCMOS(0x3A8E, 0x09);
                writeCMOS(0x3A90, 0x38);
                writeCMOS(0x3A91, 0x42);
                writeCMOS(0x3A92, 0x3C);
                writeCMOS(0x3B0E, 0xF3);
                writeCMOS(0x3B12, 0xE5);
                writeCMOS(0x3B27, 0xC0);
                writeCMOS(0x3B2E, 0xEF);
                writeCMOS(0x3B30, 0x6A);
                writeCMOS(0x3B32, 0xF6);
                writeCMOS(0x3B36, 0xE1);
                writeCMOS(0x3B3A, 0xE8);
                writeCMOS(0x3B5A, 0x17);
                writeCMOS(0x3B5E, 0xEF);
                writeCMOS(0x3B60, 0x6A);
                writeCMOS(0x3B62, 0xF6);
                writeCMOS(0x3B66, 0xE1);
                writeCMOS(0x3B6A, 0xE8);
                writeCMOS(0x3B88, 0xEC);
                writeCMOS(0x3B8A, 0xED);
                writeCMOS(0x3B94, 0x71);
                writeCMOS(0x3B96, 0x72);
                writeCMOS(0x3B98, 0xDE);
                writeCMOS(0x3B9A, 0xDF);
                writeCMOS(0x3C0F, 0x06);
                writeCMOS(0x3C10, 0x06);
                writeCMOS(0x3C11, 0x06);
                writeCMOS(0x3C12, 0x06);
                writeCMOS(0x3C13, 0x06);
                writeCMOS(0x3C18, 0x20);
                writeCMOS(0x3C3A, 0x7A);
                writeCMOS(0x3C40, 0xF4);
                writeCMOS(0x3C48, 0xE6);
                writeCMOS(0x3C54, 0xCE);
                writeCMOS(0x3C56, 0xD0);
                writeCMOS(0x3C6C, 0x53);
                writeCMOS(0x3C6E, 0x55);
                writeCMOS(0x3C70, 0xC0);
                writeCMOS(0x3C72, 0xC2);
                writeCMOS(0x3C7E, 0xCE);
                writeCMOS(0x3C8C, 0xCF);
                writeCMOS(0x3C8E, 0xEB);
                writeCMOS(0x3C98, 0x54);
                writeCMOS(0x3C9A, 0x70);
                writeCMOS(0x3C9C, 0xC1);
                writeCMOS(0x3C9E, 0xDD);
                writeCMOS(0x3CB0, 0x7A);
                writeCMOS(0x3CB2, 0xBA);
                writeCMOS(0x3CC8, 0xBC);
                writeCMOS(0x3CCA, 0x7C);
                writeCMOS(0x3CD4, 0xEA);
                writeCMOS(0x3CD5, 0x01);
                writeCMOS(0x3CD6, 0x4A);
                writeCMOS(0x3CD8, 0x00);
                writeCMOS(0x3CD9, 0x00);
                writeCMOS(0x3CDA, 0xFF);
                writeCMOS(0x3CDB, 0x03);
                writeCMOS(0x3CDC, 0x00);
                writeCMOS(0x3CDD, 0x00);
                writeCMOS(0x3CDE, 0xFF);
                writeCMOS(0x3CDF, 0x03);
                writeCMOS(0x3CE4, 0x4C);
                writeCMOS(0x3CE6, 0xEC);
                writeCMOS(0x3CE7, 0x01);
                writeCMOS(0x3CE8, 0xFF);
                writeCMOS(0x3CE9, 0x03);
                writeCMOS(0x3CEA, 0x00);
                writeCMOS(0x3CEB, 0x00);
                writeCMOS(0x3CEC, 0xFF);
                writeCMOS(0x3CED, 0x03);
                writeCMOS(0x3CEE, 0x00);
                writeCMOS(0x3CEF, 0x00);
                writeCMOS(0x3E28, 0x82);
                writeCMOS(0x3E2A, 0x80);
                writeCMOS(0x3E30, 0x85);
                writeCMOS(0x3E32, 0x7D);
                writeCMOS(0x3E5C, 0xCE);
                writeCMOS(0x3E5E, 0xD3);
                writeCMOS(0x3E70, 0x53);
                writeCMOS(0x3E72, 0x58);
                writeCMOS(0x3E74, 0xC0);
                writeCMOS(0x3E76, 0xC5);
                writeCMOS(0x3E78, 0xC0);
                writeCMOS(0x3E79, 0x01);
                writeCMOS(0x3E7A, 0xD4);
                writeCMOS(0x3E7B, 0x01);
                writeCMOS(0x3EB4, 0x0B);
                writeCMOS(0x3EB5, 0x02);
                writeCMOS(0x3EB6, 0x4D);
                writeCMOS(0x3EEC, 0xF3);
                writeCMOS(0x3EEE, 0xE7);
                writeCMOS(0x3F01, 0x01);
                writeCMOS(0x3F24, 0x10);
                writeCMOS(0x3F28, 0x2D);
                writeCMOS(0x3F2A, 0x2D);
                writeCMOS(0x3F2C, 0x2D);
                writeCMOS(0x3F2E, 0x2D);
                writeCMOS(0x3F30, 0x23);
                writeCMOS(0x3F38, 0x2D);
                writeCMOS(0x3F3A, 0x2D);
                writeCMOS(0x3F3C, 0x2D);
                writeCMOS(0x3F3E, 0x28);
                writeCMOS(0x3F40, 0x1E);
                writeCMOS(0x3F48, 0x2D);
                writeCMOS(0x3F4A, 0x2D);
                writeCMOS(0x4004, 0xE4);
                writeCMOS(0x4006, 0xFF);
                writeCMOS(0x4018, 0x69);
                writeCMOS(0x401A, 0x84);
                writeCMOS(0x401C, 0xD6);
                writeCMOS(0x401E, 0xF1);
                writeCMOS(0x4038, 0xDE);
                writeCMOS(0x403A, 0x00);
                writeCMOS(0x403B, 0x01);
                writeCMOS(0x404C, 0x63);
                writeCMOS(0x404E, 0x85);
                writeCMOS(0x4050, 0xD0);
                writeCMOS(0x4052, 0xF2);
                writeCMOS(0x4108, 0xDD);
                writeCMOS(0x410A, 0xF7);
                writeCMOS(0x411C, 0x62);
                writeCMOS(0x411E, 0x7C);
                writeCMOS(0x4120, 0xCF);
                writeCMOS(0x4122, 0xE9);
                writeCMOS(0x4138, 0xE6);
                writeCMOS(0x413A, 0xF1);
                writeCMOS(0x414C, 0x6B);
                writeCMOS(0x414E, 0x76);
                writeCMOS(0x4150, 0xD8);
                writeCMOS(0x4152, 0xE3);
                writeCMOS(0x417E, 0x03);
                writeCMOS(0x417F, 0x01);
                writeCMOS(0x4186, 0xE0);
                writeCMOS(0x4190, 0xF3);
                writeCMOS(0x4192, 0xF7);
                writeCMOS(0x419C, 0x78);
                writeCMOS(0x419E, 0x7C);
                writeCMOS(0x41A0, 0xE5);
                writeCMOS(0x41A2, 0xE9);
                writeCMOS(0x41C8, 0xE2);
                writeCMOS(0x41CA, 0xFD);
                writeCMOS(0x41DC, 0x67);
                writeCMOS(0x41DE, 0x82);
                writeCMOS(0x41E0, 0xD4);
                writeCMOS(0x41E2, 0xEF);
                writeCMOS(0x4200, 0xDE);
                writeCMOS(0x4202, 0xDA);
                writeCMOS(0x4218, 0x63);
                writeCMOS(0x421A, 0x5F);
                writeCMOS(0x421C, 0xD0);
                writeCMOS(0x421E, 0xCC);
                writeCMOS(0x425A, 0x82);
                writeCMOS(0x425C, 0xEF);
                writeCMOS(0x4348, 0xFE);
                writeCMOS(0x4349, 0x06);
                writeCMOS(0x4352, 0xCE);
                writeCMOS(0x4420, 0x0B);
                writeCMOS(0x4421, 0x02);
                writeCMOS(0x4422, 0x4D);
                writeCMOS(0x4426, 0xF5);
                writeCMOS(0x442A, 0xE7);
                writeCMOS(0x4432, 0xF5);
                writeCMOS(0x4436, 0xE7);
                writeCMOS(0x4466, 0xB4);
                writeCMOS(0x446E, 0x32);
                writeCMOS(0x449F, 0x1C);
                writeCMOS(0x44A4, 0x2C);
                writeCMOS(0x44A6, 0x2C);
                writeCMOS(0x44A8, 0x2C);
                writeCMOS(0x44AA, 0x2C);
                writeCMOS(0x44B4, 0x2C);
                writeCMOS(0x44B6, 0x2C);
                writeCMOS(0x44B8, 0x2C);
                writeCMOS(0x44BA, 0x2C);
                writeCMOS(0x44C4, 0x2C);
                writeCMOS(0x44C6, 0x2C);
                writeCMOS(0x44C8, 0x2C);
                writeCMOS(0x4506, 0xF3);
                writeCMOS(0x450E, 0xE5);
                writeCMOS(0x4516, 0xF3);
                writeCMOS(0x4522, 0xE5);
                writeCMOS(0x4524, 0xF3);
                writeCMOS(0x452C, 0xE5);
                writeCMOS(0x453C, 0x22);
                writeCMOS(0x453D, 0x1B);
                writeCMOS(0x453E, 0x1B);
                writeCMOS(0x453F, 0x15);
                writeCMOS(0x4540, 0x15);
                writeCMOS(0x4541, 0x15);
                writeCMOS(0x4542, 0x15);
                writeCMOS(0x4543, 0x15);
                writeCMOS(0x4544, 0x15);
                writeCMOS(0x4548, 0x00);
                writeCMOS(0x4549, 0x01);
                writeCMOS(0x454A, 0x01);
                writeCMOS(0x454B, 0x06);
                writeCMOS(0x454C, 0x06);
                writeCMOS(0x454D, 0x06);
                writeCMOS(0x454E, 0x06);
                writeCMOS(0x454F, 0x06);
                writeCMOS(0x4550, 0x06);
                writeCMOS(0x4554, 0x55);
                writeCMOS(0x4555, 0x02);
                writeCMOS(0x4556, 0x42);
                writeCMOS(0x4557, 0x05);
                writeCMOS(0x4558, 0xFD);
                writeCMOS(0x4559, 0x05);
                writeCMOS(0x455A, 0x94);
                writeCMOS(0x455B, 0x06);
                writeCMOS(0x455D, 0x06);
                writeCMOS(0x455E, 0x49);
                writeCMOS(0x455F, 0x07);
                writeCMOS(0x4560, 0x7F);
                writeCMOS(0x4561, 0x07);
                writeCMOS(0x4562, 0xA5);
                writeCMOS(0x4564, 0x55);
                writeCMOS(0x4565, 0x02);
                writeCMOS(0x4566, 0x42);
                writeCMOS(0x4567, 0x05);
                writeCMOS(0x4568, 0xFD);
                writeCMOS(0x4569, 0x05);
                writeCMOS(0x456A, 0x94);
                writeCMOS(0x456B, 0x06);
                writeCMOS(0x456D, 0x06);
                writeCMOS(0x456E, 0x49);
                writeCMOS(0x456F, 0x07);
                writeCMOS(0x4572, 0xA5);
                writeCMOS(0x460C, 0x7D);
                writeCMOS(0x460E, 0xB1);
                writeCMOS(0x4614, 0xA8);
                writeCMOS(0x4616, 0xB2);
                writeCMOS(0x461C, 0x7E);
                writeCMOS(0x461E, 0xA7);
                writeCMOS(0x4624, 0xA8);
                writeCMOS(0x4626, 0xB2);
                writeCMOS(0x462C, 0x7E);
                writeCMOS(0x462E, 0x8A);
                writeCMOS(0x4630, 0x94);
                writeCMOS(0x4632, 0xA7);
                writeCMOS(0x4634, 0xFB);
                writeCMOS(0x4636, 0x2F);
                writeCMOS(0x4638, 0x81);
                writeCMOS(0x4639, 0x01);
                writeCMOS(0x463A, 0xB5);
                writeCMOS(0x463B, 0x01);
                writeCMOS(0x463C, 0x26);
                writeCMOS(0x463E, 0x30);
                writeCMOS(0x4640, 0xAC);
                writeCMOS(0x4641, 0x01);
                writeCMOS(0x4642, 0xB6);
                writeCMOS(0x4643, 0x01);
                writeCMOS(0x4644, 0xFC);
                writeCMOS(0x4646, 0x25);
                writeCMOS(0x4648, 0x82);
                writeCMOS(0x4649, 0x01);
                writeCMOS(0x464A, 0xAB);
                writeCMOS(0x464B, 0x01);
                writeCMOS(0x464C, 0x26);
                writeCMOS(0x464E, 0x30);
                writeCMOS(0x4654, 0xFC);
                writeCMOS(0x4656, 0x08);
                writeCMOS(0x4658, 0x12);
                writeCMOS(0x465A, 0x25);
                writeCMOS(0x4662, 0xFC);
                writeCMOS(0x46A2, 0xFB);
                writeCMOS(0x46D6, 0xF3);
                writeCMOS(0x46E6, 0x00);
                writeCMOS(0x46E8, 0xFF);
                writeCMOS(0x46E9, 0x03);
                writeCMOS(0x46EC, 0x7A);
                writeCMOS(0x46EE, 0xE5);
                writeCMOS(0x46F4, 0xEE);
                writeCMOS(0x46F6, 0xF2);
                writeCMOS(0x470C, 0xFF);
                writeCMOS(0x470D, 0x03);
                writeCMOS(0x470E, 0x00);
                writeCMOS(0x4714, 0xE0);
                writeCMOS(0x4716, 0xE4);
                writeCMOS(0x471E, 0xED);
                writeCMOS(0x472E, 0x00);
                writeCMOS(0x4730, 0xFF);
                writeCMOS(0x4731, 0x03);
                writeCMOS(0x4734, 0x7B);
                writeCMOS(0x4736, 0xDF);
                writeCMOS(0x4754, 0x7D);
                writeCMOS(0x4756, 0x8B);
                writeCMOS(0x4758, 0x93);
                writeCMOS(0x475A, 0xB1);
                writeCMOS(0x475C, 0xFB);
                writeCMOS(0x475E, 0x09);
                writeCMOS(0x4760, 0x11);
                writeCMOS(0x4762, 0x2F);
                writeCMOS(0x4766, 0xCC);
                writeCMOS(0x4776, 0xCB);
                writeCMOS(0x477E, 0x4A);
                writeCMOS(0x478E, 0x49);
                writeCMOS(0x4794, 0x7C);
                writeCMOS(0x4796, 0x8F);
                writeCMOS(0x4798, 0xB3);
                writeCMOS(0x4799, 0x00);
                writeCMOS(0x479A, 0xCC);
                writeCMOS(0x479C, 0xC1);
                writeCMOS(0x479E, 0xCB);
                writeCMOS(0x47A4, 0x7D);
                writeCMOS(0x47A6, 0x8E);
                writeCMOS(0x47A8, 0xB4);
                writeCMOS(0x47A9, 0x00);
                writeCMOS(0x47AA, 0xC0);
                writeCMOS(0x47AC, 0xFA);
                writeCMOS(0x47AE, 0x0D);
                writeCMOS(0x47B0, 0x31);
                writeCMOS(0x47B1, 0x01);
                writeCMOS(0x47B2, 0x4A);
                writeCMOS(0x47B3, 0x01);
                writeCMOS(0x47B4, 0x3F);
                writeCMOS(0x47B6, 0x49);
                writeCMOS(0x47BC, 0xFB);
                writeCMOS(0x47BE, 0x0C);
                writeCMOS(0x47C0, 0x32);
                writeCMOS(0x47C1, 0x01);
                writeCMOS(0x47C2, 0x3E);
                writeCMOS(0x47C3, 0x01);
                hmax_init = 1100;
             


                /*
              
                //"All-pixel scan CSI-2_4lane 74.25Mz AD:12bit Output:12bit 1440Mbps
                //slave Mode
                //LCG Mode
                //50fps
                //Integration Time 19.973ms"
                //orizontal Clock	 660 (74.25MZ)
                //Vertical line	 2250
                //"Frame rate(after combined)"	 50
                // 2022 11 24 YSK 


                writeCMOS(0x3015, 0x03);
                //writeCMOS(0x302C, 0x94);
                //writeCMOS(0x302D, 0x02);
                writeCMOS(0x3050, 0x03);
                //writeCMOS(0x30A6, 0x00);
                writeCMOS(0x3460, 0x22);
                writeCMOS(0x355A, 0x64);
                writeCMOS(0x3A02, 0x7A);
                writeCMOS(0x3A10, 0xEC);
                writeCMOS(0x3A12, 0x71);
                writeCMOS(0x3A14, 0xDE);
                writeCMOS(0x3A20, 0x2B);
                writeCMOS(0x3A24, 0x22);
                writeCMOS(0x3A25, 0x25);
                writeCMOS(0x3A26, 0x2A);
                writeCMOS(0x3A27, 0x2C);
                writeCMOS(0x3A28, 0x39);
                writeCMOS(0x3A29, 0x38);
                writeCMOS(0x3A30, 0x04);
                writeCMOS(0x3A31, 0x04);
                writeCMOS(0x3A32, 0x03);
                writeCMOS(0x3A33, 0x03);
                writeCMOS(0x3A34, 0x09);
                writeCMOS(0x3A35, 0x06);
                writeCMOS(0x3A38, 0xCD);
                writeCMOS(0x3A3A, 0x4C);
                writeCMOS(0x3A3C, 0xB9);
                writeCMOS(0x3A3E, 0x30);
                writeCMOS(0x3A40, 0x2C);
                writeCMOS(0x3A42, 0x39);
                writeCMOS(0x3A4E, 0x00);
                writeCMOS(0x3A52, 0x00);
                writeCMOS(0x3A56, 0x00);
                writeCMOS(0x3A5A, 0x00);
                writeCMOS(0x3A5E, 0x00);
                writeCMOS(0x3A62, 0x00);
                writeCMOS(0x3A6E, 0xA0);
                writeCMOS(0x3A70, 0x50);
                writeCMOS(0x3A8C, 0x04);
                writeCMOS(0x3A8D, 0x03);
                writeCMOS(0x3A8E, 0x09);
                writeCMOS(0x3A90, 0x38);
                writeCMOS(0x3A91, 0x42);
                writeCMOS(0x3A92, 0x3C);
                writeCMOS(0x3B0E, 0xF3);
                writeCMOS(0x3B12, 0xE5);
                writeCMOS(0x3B27, 0xC0);
                writeCMOS(0x3B2E, 0xEF);
                writeCMOS(0x3B30, 0x6A);
                writeCMOS(0x3B32, 0xF6);
                writeCMOS(0x3B36, 0xE1);
                writeCMOS(0x3B3A, 0xE8);
                writeCMOS(0x3B5A, 0x17);
                writeCMOS(0x3B5E, 0xEF);
                writeCMOS(0x3B60, 0x6A);
                writeCMOS(0x3B62, 0xF6);
                writeCMOS(0x3B66, 0xE1);
                writeCMOS(0x3B6A, 0xE8);
                writeCMOS(0x3B88, 0xEC);
                writeCMOS(0x3B8A, 0xED);
                writeCMOS(0x3B94, 0x71);
                writeCMOS(0x3B96, 0x72);
                writeCMOS(0x3B98, 0xDE);
                writeCMOS(0x3B9A, 0xDF);
                writeCMOS(0x3C0F, 0x06);
                writeCMOS(0x3C10, 0x06);
                writeCMOS(0x3C11, 0x06);
                writeCMOS(0x3C12, 0x06);
                writeCMOS(0x3C13, 0x06);
                writeCMOS(0x3C18, 0x20);
                writeCMOS(0x3C3A, 0x7A);
                writeCMOS(0x3C40, 0xF4);
                writeCMOS(0x3C48, 0xE6);
                writeCMOS(0x3C54, 0xCE);
                writeCMOS(0x3C56, 0xD0);
                writeCMOS(0x3C6C, 0x53);
                writeCMOS(0x3C6E, 0x55);
                writeCMOS(0x3C70, 0xC0);
                writeCMOS(0x3C72, 0xC2);
                writeCMOS(0x3C7E, 0xCE);
                writeCMOS(0x3C8C, 0xCF);
                writeCMOS(0x3C8E, 0xEB);
                writeCMOS(0x3C98, 0x54);
                writeCMOS(0x3C9A, 0x70);
                writeCMOS(0x3C9C, 0xC1);
                writeCMOS(0x3C9E, 0xDD);
                writeCMOS(0x3CB0, 0x7A);
                writeCMOS(0x3CB2, 0xBA);
                writeCMOS(0x3CC8, 0xBC);
                writeCMOS(0x3CCA, 0x7C);
                writeCMOS(0x3CD4, 0xEA);
                writeCMOS(0x3CD5, 0x01);
                writeCMOS(0x3CD6, 0x4A);
                writeCMOS(0x3CD8, 0x00);
                writeCMOS(0x3CD9, 0x00);
                writeCMOS(0x3CDA, 0xFF);
                writeCMOS(0x3CDB, 0x03);
                writeCMOS(0x3CDC, 0x00);
                writeCMOS(0x3CDD, 0x00);
                writeCMOS(0x3CDE, 0xFF);
                writeCMOS(0x3CDF, 0x03);
                writeCMOS(0x3CE4, 0x4C);
                writeCMOS(0x3CE6, 0xEC);
                writeCMOS(0x3CE7, 0x01);
                writeCMOS(0x3CE8, 0xFF);
                writeCMOS(0x3CE9, 0x03);
                writeCMOS(0x3CEA, 0x00);
                writeCMOS(0x3CEB, 0x00);
                writeCMOS(0x3CEC, 0xFF);
                writeCMOS(0x3CED, 0x03);
                writeCMOS(0x3CEE, 0x00);
                writeCMOS(0x3CEF, 0x00);
                writeCMOS(0x3E28, 0x82);
                writeCMOS(0x3E2A, 0x80);
                writeCMOS(0x3E30, 0x85);
                writeCMOS(0x3E32, 0x7D);
                writeCMOS(0x3E5C, 0xCE);
                writeCMOS(0x3E5E, 0xD3);
                writeCMOS(0x3E70, 0x53);
                writeCMOS(0x3E72, 0x58);
                writeCMOS(0x3E74, 0xC0);
                writeCMOS(0x3E76, 0xC5);
                writeCMOS(0x3E78, 0xC0);
                writeCMOS(0x3E79, 0x01);
                writeCMOS(0x3E7A, 0xD4);
                writeCMOS(0x3E7B, 0x01);
                writeCMOS(0x3EB4, 0x0B);
                writeCMOS(0x3EB5, 0x02);
                writeCMOS(0x3EB6, 0x4D);
                writeCMOS(0x3EEC, 0xF3);
                writeCMOS(0x3EEE, 0xE7);
                writeCMOS(0x3F01, 0x01);
                writeCMOS(0x3F24, 0x10);
                writeCMOS(0x3F28, 0x2D);
                writeCMOS(0x3F2A, 0x2D);
                writeCMOS(0x3F2C, 0x2D);
                writeCMOS(0x3F2E, 0x2D);
                writeCMOS(0x3F30, 0x23);
                writeCMOS(0x3F38, 0x2D);
                writeCMOS(0x3F3A, 0x2D);
                writeCMOS(0x3F3C, 0x2D);
                writeCMOS(0x3F3E, 0x28);
                writeCMOS(0x3F40, 0x1E);
                writeCMOS(0x3F48, 0x2D);
                writeCMOS(0x3F4A, 0x2D);
                writeCMOS(0x4004, 0xE4);
                writeCMOS(0x4006, 0xFF);
                writeCMOS(0x4018, 0x69);
                writeCMOS(0x401A, 0x84);
                writeCMOS(0x401C, 0xD6);
                writeCMOS(0x401E, 0xF1);
                writeCMOS(0x4038, 0xDE);
                writeCMOS(0x403A, 0x00);
                writeCMOS(0x403B, 0x01);
                writeCMOS(0x404C, 0x63);
                writeCMOS(0x404E, 0x85);
                writeCMOS(0x4050, 0xD0);
                writeCMOS(0x4052, 0xF2);
                writeCMOS(0x4108, 0xDD);
                writeCMOS(0x410A, 0xF7);
                writeCMOS(0x411C, 0x62);
                writeCMOS(0x411E, 0x7C);
                writeCMOS(0x4120, 0xCF);
                writeCMOS(0x4122, 0xE9);
                writeCMOS(0x4138, 0xE6);
                writeCMOS(0x413A, 0xF1);
                writeCMOS(0x414C, 0x6B);
                writeCMOS(0x414E, 0x76);
                writeCMOS(0x4150, 0xD8);
                writeCMOS(0x4152, 0xE3);
                writeCMOS(0x417E, 0x03);
                writeCMOS(0x417F, 0x01);
                writeCMOS(0x4186, 0xE0);
                writeCMOS(0x4190, 0xF3);
                writeCMOS(0x4192, 0xF7);
                writeCMOS(0x419C, 0x78);
                writeCMOS(0x419E, 0x7C);
                writeCMOS(0x41A0, 0xE5);
                writeCMOS(0x41A2, 0xE9);
                writeCMOS(0x41C8, 0xE2);
                writeCMOS(0x41CA, 0xFD);
                writeCMOS(0x41DC, 0x67);
                writeCMOS(0x41DE, 0x82);
                writeCMOS(0x41E0, 0xD4);
                writeCMOS(0x41E2, 0xEF);
                writeCMOS(0x4200, 0xDE);
                writeCMOS(0x4202, 0xDA);
                writeCMOS(0x4218, 0x63);
                writeCMOS(0x421A, 0x5F);
                writeCMOS(0x421C, 0xD0);
                writeCMOS(0x421E, 0xCC);
                writeCMOS(0x425A, 0x82);
                writeCMOS(0x425C, 0xEF);
                writeCMOS(0x4348, 0xFE);
                writeCMOS(0x4349, 0x06);
                writeCMOS(0x4352, 0xCE);
                writeCMOS(0x4420, 0x0B);
                writeCMOS(0x4421, 0x02);
                writeCMOS(0x4422, 0x4D);
                writeCMOS(0x4426, 0xF5);
                writeCMOS(0x442A, 0xE7);
                writeCMOS(0x4432, 0xF5);
                writeCMOS(0x4436, 0xE7);
                writeCMOS(0x4466, 0xB4);
                writeCMOS(0x446E, 0x32);
                writeCMOS(0x449F, 0x1C);
                writeCMOS(0x44A4, 0x2C);
                writeCMOS(0x44A6, 0x2C);
                writeCMOS(0x44A8, 0x2C);
                writeCMOS(0x44AA, 0x2C);
                writeCMOS(0x44B4, 0x2C);
                writeCMOS(0x44B6, 0x2C);
                writeCMOS(0x44B8, 0x2C);
                writeCMOS(0x44BA, 0x2C);
                writeCMOS(0x44C4, 0x2C);
                writeCMOS(0x44C6, 0x2C);
                writeCMOS(0x44C8, 0x2C);
                writeCMOS(0x4506, 0xF3);
                writeCMOS(0x450E, 0xE5);
                writeCMOS(0x4516, 0xF3);
                writeCMOS(0x4522, 0xE5);
                writeCMOS(0x4524, 0xF3);
                writeCMOS(0x452C, 0xE5);
                writeCMOS(0x453C, 0x22);
                writeCMOS(0x453D, 0x1B);
                writeCMOS(0x453E, 0x1B);
                writeCMOS(0x453F, 0x15);
                writeCMOS(0x4540, 0x15);
                writeCMOS(0x4541, 0x15);
                writeCMOS(0x4542, 0x15);
                writeCMOS(0x4543, 0x15);
                writeCMOS(0x4544, 0x15);
                writeCMOS(0x4548, 0x00);
                writeCMOS(0x4549, 0x01);
                writeCMOS(0x454A, 0x01);
                writeCMOS(0x454B, 0x06);
                writeCMOS(0x454C, 0x06);
                writeCMOS(0x454D, 0x06);
                writeCMOS(0x454E, 0x06);
                writeCMOS(0x454F, 0x06);
                writeCMOS(0x4550, 0x06);
                writeCMOS(0x4554, 0x55);
                writeCMOS(0x4555, 0x02);
                writeCMOS(0x4556, 0x42);
                writeCMOS(0x4557, 0x05);
                writeCMOS(0x4558, 0xFD);
                writeCMOS(0x4559, 0x05);
                writeCMOS(0x455A, 0x94);
                writeCMOS(0x455B, 0x06);
                writeCMOS(0x455D, 0x06);
                writeCMOS(0x455E, 0x49);
                writeCMOS(0x455F, 0x07);
                writeCMOS(0x4560, 0x7F);
                writeCMOS(0x4561, 0x07);
                writeCMOS(0x4562, 0xA5);
                writeCMOS(0x4564, 0x55);
                writeCMOS(0x4565, 0x02);
                writeCMOS(0x4566, 0x42);
                writeCMOS(0x4567, 0x05);
                writeCMOS(0x4568, 0xFD);
                writeCMOS(0x4569, 0x05);
                writeCMOS(0x456A, 0x94);
                writeCMOS(0x456B, 0x06);
                writeCMOS(0x456D, 0x06);
                writeCMOS(0x456E, 0x49);
                writeCMOS(0x456F, 0x07);
                writeCMOS(0x4572, 0xA5);
                writeCMOS(0x460C, 0x7D);
                writeCMOS(0x460E, 0xB1);
                writeCMOS(0x4614, 0xA8);
                writeCMOS(0x4616, 0xB2);
                writeCMOS(0x461C, 0x7E);
                writeCMOS(0x461E, 0xA7);
                writeCMOS(0x4624, 0xA8);
                writeCMOS(0x4626, 0xB2);
                writeCMOS(0x462C, 0x7E);
                writeCMOS(0x462E, 0x8A);
                writeCMOS(0x4630, 0x94);
                writeCMOS(0x4632, 0xA7);
                writeCMOS(0x4634, 0xFB);
                writeCMOS(0x4636, 0x2F);
                writeCMOS(0x4638, 0x81);
                writeCMOS(0x4639, 0x01);
                writeCMOS(0x463A, 0xB5);
                writeCMOS(0x463B, 0x01);
                writeCMOS(0x463C, 0x26);
                writeCMOS(0x463E, 0x30);
                writeCMOS(0x4640, 0xAC);
                writeCMOS(0x4641, 0x01);
                writeCMOS(0x4642, 0xB6);
                writeCMOS(0x4643, 0x01);
                writeCMOS(0x4644, 0xFC);
                writeCMOS(0x4646, 0x25);
                writeCMOS(0x4648, 0x82);
                writeCMOS(0x4649, 0x01);
                writeCMOS(0x464A, 0xAB);
                writeCMOS(0x464B, 0x01);
                writeCMOS(0x464C, 0x26);
                writeCMOS(0x464E, 0x30);
                writeCMOS(0x4654, 0xFC);
                writeCMOS(0x4656, 0x08);
                writeCMOS(0x4658, 0x12);
                writeCMOS(0x465A, 0x25);
                writeCMOS(0x4662, 0xFC);
                writeCMOS(0x46A2, 0xFB);
                writeCMOS(0x46D6, 0xF3);
                writeCMOS(0x46E6, 0x00);
                writeCMOS(0x46E8, 0xFF);
                writeCMOS(0x46E9, 0x03);
                writeCMOS(0x46EC, 0x7A);
                writeCMOS(0x46EE, 0xE5);
                writeCMOS(0x46F4, 0xEE);
                writeCMOS(0x46F6, 0xF2);
                writeCMOS(0x470C, 0xFF);
                writeCMOS(0x470D, 0x03);
                writeCMOS(0x470E, 0x00);
                writeCMOS(0x4714, 0xE0);
                writeCMOS(0x4716, 0xE4);
                writeCMOS(0x471E, 0xED);
                writeCMOS(0x472E, 0x00);
                writeCMOS(0x4730, 0xFF);
                writeCMOS(0x4731, 0x03);
                writeCMOS(0x4734, 0x7B);
                writeCMOS(0x4736, 0xDF);
                writeCMOS(0x4754, 0x7D);
                writeCMOS(0x4756, 0x8B);
                writeCMOS(0x4758, 0x93);
                writeCMOS(0x475A, 0xB1);
                writeCMOS(0x475C, 0xFB);
                writeCMOS(0x475E, 0x09);
                writeCMOS(0x4760, 0x11);
                writeCMOS(0x4762, 0x2F);
                writeCMOS(0x4766, 0xCC);
                writeCMOS(0x4776, 0xCB);
                writeCMOS(0x477E, 0x4A);
                writeCMOS(0x478E, 0x49);
                writeCMOS(0x4794, 0x7C);
                writeCMOS(0x4796, 0x8F);
                writeCMOS(0x4798, 0xB3);
                writeCMOS(0x4799, 0x00);
                writeCMOS(0x479A, 0xCC);
                writeCMOS(0x479C, 0xC1);
                writeCMOS(0x479E, 0xCB);
                writeCMOS(0x47A4, 0x7D);
                writeCMOS(0x47A6, 0x8E);
                writeCMOS(0x47A8, 0xB4);
                writeCMOS(0x47A9, 0x00);
                writeCMOS(0x47AA, 0xC0);
                writeCMOS(0x47AC, 0xFA);
                writeCMOS(0x47AE, 0x0D);
                writeCMOS(0x47B0, 0x31);
                writeCMOS(0x47B1, 0x01);
                writeCMOS(0x47B2, 0x4A);
                writeCMOS(0x47B3, 0x01);
                writeCMOS(0x47B4, 0x3F);
                writeCMOS(0x47B6, 0x49);
                writeCMOS(0x47BC, 0xFB);
                writeCMOS(0x47BE, 0x0C);
                writeCMOS(0x47C0, 0x32);
                writeCMOS(0x47C1, 0x01);
                writeCMOS(0x47C2, 0x3E);
                writeCMOS(0x47C3, 0x01);
                //hmax_init = 660;
                hmax_init = 1100;

               */

           
           
               
       
            }
            else if (mode == 0x0d)//10bit
            {

                //"All-pixel scanCSI-2_4lane
                //74.25MHz
                //AD:10bit Output:10bit
                //891Mbps
                //Slave Mode
                //LCG Mode
                //30fps
                //Integration Time
                //33.289ms"


                writeCMOS(0x3015, 0x05);
                writeCMOS(0x3022, 0x00);
                writeCMOS(0x3023, 0x00);
                writeCMOS(0x3050, 0x03);
                //writeCMOS(0x30A6, 0x00);
                writeCMOS(0x3460, 0x22);
                writeCMOS(0x355A, 0x64);
                writeCMOS(0x3A02, 0x7A);
                writeCMOS(0x3A10, 0xEC);
                writeCMOS(0x3A12, 0x71);
                writeCMOS(0x3A14, 0xDE);
                writeCMOS(0x3A20, 0x2B);
                writeCMOS(0x3A24, 0x22);
                writeCMOS(0x3A25, 0x25);
                writeCMOS(0x3A26, 0x2A);
                writeCMOS(0x3A27, 0x2C);
                writeCMOS(0x3A28, 0x39);
                writeCMOS(0x3A29, 0x38);
                writeCMOS(0x3A30, 0x04);
                writeCMOS(0x3A31, 0x04);
                writeCMOS(0x3A32, 0x03);
                writeCMOS(0x3A33, 0x03);
                writeCMOS(0x3A34, 0x09);
                writeCMOS(0x3A35, 0x06);
                writeCMOS(0x3A38, 0xCD);
                writeCMOS(0x3A3A, 0x4C);
                writeCMOS(0x3A3C, 0xB9);
                writeCMOS(0x3A3E, 0x30);
                writeCMOS(0x3A40, 0x2C);
                writeCMOS(0x3A42, 0x39);
                writeCMOS(0x3A4E, 0x00);
                writeCMOS(0x3A52, 0x00);
                writeCMOS(0x3A56, 0x00);
                writeCMOS(0x3A5A, 0x00);
                writeCMOS(0x3A5E, 0x00);
                writeCMOS(0x3A62, 0x00);
                writeCMOS(0x3A6E, 0xA0);
                writeCMOS(0x3A70, 0x50);
                writeCMOS(0x3A8C, 0x04);
                writeCMOS(0x3A8D, 0x03);
                writeCMOS(0x3A8E, 0x09);
                writeCMOS(0x3A90, 0x38);
                writeCMOS(0x3A91, 0x42);
                writeCMOS(0x3A92, 0x3C);
                writeCMOS(0x3B0E, 0xF3);
                writeCMOS(0x3B12, 0xE5);
                writeCMOS(0x3B27, 0xC0);
                writeCMOS(0x3B2E, 0xEF);
                writeCMOS(0x3B30, 0x6A);
                writeCMOS(0x3B32, 0xF6);
                writeCMOS(0x3B36, 0xE1);
                writeCMOS(0x3B3A, 0xE8);
                writeCMOS(0x3B5A, 0x17);
                writeCMOS(0x3B5E, 0xEF);
                writeCMOS(0x3B60, 0x6A);
                writeCMOS(0x3B62, 0xF6);
                writeCMOS(0x3B66, 0xE1);
                writeCMOS(0x3B6A, 0xE8);
                writeCMOS(0x3B88, 0xEC);
                writeCMOS(0x3B8A, 0xED);
                writeCMOS(0x3B94, 0x71);
                writeCMOS(0x3B96, 0x72);
                writeCMOS(0x3B98, 0xDE);
                writeCMOS(0x3B9A, 0xDF);
                writeCMOS(0x3C0F, 0x06);
                writeCMOS(0x3C10, 0x06);
                writeCMOS(0x3C11, 0x06);
                writeCMOS(0x3C12, 0x06);
                writeCMOS(0x3C13, 0x06);
                writeCMOS(0x3C18, 0x20);
                writeCMOS(0x3C3A, 0x7A);
                writeCMOS(0x3C40, 0xF4);
                writeCMOS(0x3C48, 0xE6);
                writeCMOS(0x3C54, 0xCE);
                writeCMOS(0x3C56, 0xD0);
                writeCMOS(0x3C6C, 0x53);
                writeCMOS(0x3C6E, 0x55);
                writeCMOS(0x3C70, 0xC0);
                writeCMOS(0x3C72, 0xC2);
                writeCMOS(0x3C7E, 0xCE);
                writeCMOS(0x3C8C, 0xCF);
                writeCMOS(0x3C8E, 0xEB);
                writeCMOS(0x3C98, 0x54);
                writeCMOS(0x3C9A, 0x70);
                writeCMOS(0x3C9C, 0xC1);
                writeCMOS(0x3C9E, 0xDD);
                writeCMOS(0x3CB0, 0x7A);
                writeCMOS(0x3CB2, 0xBA);
                writeCMOS(0x3CC8, 0xBC);
                writeCMOS(0x3CCA, 0x7C);
                writeCMOS(0x3CD4, 0xEA);
                writeCMOS(0x3CD5, 0x01);
                writeCMOS(0x3CD6, 0x4A);
                writeCMOS(0x3CD8, 0x00);
                writeCMOS(0x3CD9, 0x00);
                writeCMOS(0x3CDA, 0xFF);
                writeCMOS(0x3CDB, 0x03);
                writeCMOS(0x3CDC, 0x00);
                writeCMOS(0x3CDD, 0x00);
                writeCMOS(0x3CDE, 0xFF);
                writeCMOS(0x3CDF, 0x03);
                writeCMOS(0x3CE4, 0x4C);
                writeCMOS(0x3CE6, 0xEC);
                writeCMOS(0x3CE7, 0x01);
                writeCMOS(0x3CE8, 0xFF);
                writeCMOS(0x3CE9, 0x03);
                writeCMOS(0x3CEA, 0x00);
                writeCMOS(0x3CEB, 0x00);
                writeCMOS(0x3CEC, 0xFF);
                writeCMOS(0x3CED, 0x03);
                writeCMOS(0x3CEE, 0x00);
                writeCMOS(0x3CEF, 0x00);
                writeCMOS(0x3E28, 0x82);
                writeCMOS(0x3E2A, 0x80);
                writeCMOS(0x3E30, 0x85);
                writeCMOS(0x3E32, 0x7D);
                writeCMOS(0x3E5C, 0xCE);
                writeCMOS(0x3E5E, 0xD3);
                writeCMOS(0x3E70, 0x53);
                writeCMOS(0x3E72, 0x58);
                writeCMOS(0x3E74, 0xC0);
                writeCMOS(0x3E76, 0xC5);
                writeCMOS(0x3E78, 0xC0);
                writeCMOS(0x3E79, 0x01);
                writeCMOS(0x3E7A, 0xD4);
                writeCMOS(0x3E7B, 0x01);
                writeCMOS(0x3EB4, 0x0B);
                writeCMOS(0x3EB5, 0x02);
                writeCMOS(0x3EB6, 0x4D);
                writeCMOS(0x3EEC, 0xF3);
                writeCMOS(0x3EEE, 0xE7);
                writeCMOS(0x3F01, 0x01);
                writeCMOS(0x3F24, 0x10);
                writeCMOS(0x3F28, 0x2D);
                writeCMOS(0x3F2A, 0x2D);
                writeCMOS(0x3F2C, 0x2D);
                writeCMOS(0x3F2E, 0x2D);
                writeCMOS(0x3F30, 0x23);
                writeCMOS(0x3F38, 0x2D);
                writeCMOS(0x3F3A, 0x2D);
                writeCMOS(0x3F3C, 0x2D);
                writeCMOS(0x3F3E, 0x28);
                writeCMOS(0x3F40, 0x1E);
                writeCMOS(0x3F48, 0x2D);
                writeCMOS(0x3F4A, 0x2D);
                writeCMOS(0x4004, 0xE4);
                writeCMOS(0x4006, 0xFF);
                writeCMOS(0x4018, 0x69);
                writeCMOS(0x401A, 0x84);
                writeCMOS(0x401C, 0xD6);
                writeCMOS(0x401E, 0xF1);
                writeCMOS(0x4038, 0xDE);
                writeCMOS(0x403A, 0x00);
                writeCMOS(0x403B, 0x01);
                writeCMOS(0x404C, 0x63);
                writeCMOS(0x404E, 0x85);
                writeCMOS(0x4050, 0xD0);
                writeCMOS(0x4052, 0xF2);
                writeCMOS(0x4108, 0xDD);
                writeCMOS(0x410A, 0xF7);
                writeCMOS(0x411C, 0x62);
                writeCMOS(0x411E, 0x7C);
                writeCMOS(0x4120, 0xCF);
                writeCMOS(0x4122, 0xE9);
                writeCMOS(0x4138, 0xE6);
                writeCMOS(0x413A, 0xF1);
                writeCMOS(0x414C, 0x6B);
                writeCMOS(0x414E, 0x76);
                writeCMOS(0x4150, 0xD8);
                writeCMOS(0x4152, 0xE3);
                writeCMOS(0x417E, 0x03);
                writeCMOS(0x417F, 0x01);
                writeCMOS(0x4186, 0xE0);
                writeCMOS(0x4190, 0xF3);
                writeCMOS(0x4192, 0xF7);
                writeCMOS(0x419C, 0x78);
                writeCMOS(0x419E, 0x7C);
                writeCMOS(0x41A0, 0xE5);
                writeCMOS(0x41A2, 0xE9);
                writeCMOS(0x41C8, 0xE2);
                writeCMOS(0x41CA, 0xFD);
                writeCMOS(0x41DC, 0x67);
                writeCMOS(0x41DE, 0x82);
                writeCMOS(0x41E0, 0xD4);
                writeCMOS(0x41E2, 0xEF);
                writeCMOS(0x4200, 0xDE);
                writeCMOS(0x4202, 0xDA);
                writeCMOS(0x4218, 0x63);
                writeCMOS(0x421A, 0x5F);
                writeCMOS(0x421C, 0xD0);
                writeCMOS(0x421E, 0xCC);
                writeCMOS(0x425A, 0x82);
                writeCMOS(0x425C, 0xEF);
                writeCMOS(0x4348, 0xFE);
                writeCMOS(0x4349, 0x06);
                writeCMOS(0x4352, 0xCE);
                writeCMOS(0x4420, 0x0B);
                writeCMOS(0x4421, 0x02);
                writeCMOS(0x4422, 0x4D);
                writeCMOS(0x4426, 0xF5);
                writeCMOS(0x442A, 0xE7);
                writeCMOS(0x4432, 0xF5);
                writeCMOS(0x4436, 0xE7);
                writeCMOS(0x4466, 0xB4);
                writeCMOS(0x446E, 0x32);
                writeCMOS(0x449F, 0x1C);
                writeCMOS(0x44A4, 0x2C);
                writeCMOS(0x44A6, 0x2C);
                writeCMOS(0x44A8, 0x2C);
                writeCMOS(0x44AA, 0x2C);
                writeCMOS(0x44B4, 0x2C);
                writeCMOS(0x44B6, 0x2C);
                writeCMOS(0x44B8, 0x2C);
                writeCMOS(0x44BA, 0x2C);
                writeCMOS(0x44C4, 0x2C);
                writeCMOS(0x44C6, 0x2C);
                writeCMOS(0x44C8, 0x2C);
                writeCMOS(0x4506, 0xF3);
                writeCMOS(0x450E, 0xE5);
                writeCMOS(0x4516, 0xF3);
                writeCMOS(0x4522, 0xE5);
                writeCMOS(0x4524, 0xF3);
                writeCMOS(0x452C, 0xE5);
                writeCMOS(0x453C, 0x22);
                writeCMOS(0x453D, 0x1B);
                writeCMOS(0x453E, 0x1B);
                writeCMOS(0x453F, 0x15);
                writeCMOS(0x4540, 0x15);
                writeCMOS(0x4541, 0x15);
                writeCMOS(0x4542, 0x15);
                writeCMOS(0x4543, 0x15);
                writeCMOS(0x4544, 0x15);
                writeCMOS(0x4548, 0x00);
                writeCMOS(0x4549, 0x01);
                writeCMOS(0x454A, 0x01);
                writeCMOS(0x454B, 0x06);
                writeCMOS(0x454C, 0x06);
                writeCMOS(0x454D, 0x06);
                writeCMOS(0x454E, 0x06);
                writeCMOS(0x454F, 0x06);
                writeCMOS(0x4550, 0x06);
                writeCMOS(0x4554, 0x55);
                writeCMOS(0x4555, 0x02);
                writeCMOS(0x4556, 0x42);
                writeCMOS(0x4557, 0x05);
                writeCMOS(0x4558, 0xFD);
                writeCMOS(0x4559, 0x05);
                writeCMOS(0x455A, 0x94);
                writeCMOS(0x455B, 0x06);
                writeCMOS(0x455D, 0x06);
                writeCMOS(0x455E, 0x49);
                writeCMOS(0x455F, 0x07);
                writeCMOS(0x4560, 0x7F);
                writeCMOS(0x4561, 0x07);
                writeCMOS(0x4562, 0xA5);
                writeCMOS(0x4564, 0x55);
                writeCMOS(0x4565, 0x02);
                writeCMOS(0x4566, 0x42);
                writeCMOS(0x4567, 0x05);
                writeCMOS(0x4568, 0xFD);
                writeCMOS(0x4569, 0x05);
                writeCMOS(0x456A, 0x94);
                writeCMOS(0x456B, 0x06);
                writeCMOS(0x456D, 0x06);
                writeCMOS(0x456E, 0x49);
                writeCMOS(0x456F, 0x07);
                writeCMOS(0x4572, 0xA5);
                writeCMOS(0x460C, 0x7D);
                writeCMOS(0x460E, 0xB1);
                writeCMOS(0x4614, 0xA8);
                writeCMOS(0x4616, 0xB2);
                writeCMOS(0x461C, 0x7E);
                writeCMOS(0x461E, 0xA7);
                writeCMOS(0x4624, 0xA8);
                writeCMOS(0x4626, 0xB2);
                writeCMOS(0x462C, 0x7E);
                writeCMOS(0x462E, 0x8A);
                writeCMOS(0x4630, 0x94);
                writeCMOS(0x4632, 0xA7);
                writeCMOS(0x4634, 0xFB);
                writeCMOS(0x4636, 0x2F);
                writeCMOS(0x4638, 0x81);
                writeCMOS(0x4639, 0x01);
                writeCMOS(0x463A, 0xB5);
                writeCMOS(0x463B, 0x01);
                writeCMOS(0x463C, 0x26);
                writeCMOS(0x463E, 0x30);
                writeCMOS(0x4640, 0xAC);
                writeCMOS(0x4641, 0x01);
                writeCMOS(0x4642, 0xB6);
                writeCMOS(0x4643, 0x01);
                writeCMOS(0x4644, 0xFC);
                writeCMOS(0x4646, 0x25);
                writeCMOS(0x4648, 0x82);
                writeCMOS(0x4649, 0x01);
                writeCMOS(0x464A, 0xAB);
                writeCMOS(0x464B, 0x01);
                writeCMOS(0x464C, 0x26);
                writeCMOS(0x464E, 0x30);
                writeCMOS(0x4654, 0xFC);
                writeCMOS(0x4656, 0x08);
                writeCMOS(0x4658, 0x12);
                writeCMOS(0x465A, 0x25);
                writeCMOS(0x4662, 0xFC);
                writeCMOS(0x46A2, 0xFB);
                writeCMOS(0x46D6, 0xF3);
                writeCMOS(0x46E6, 0x00);
                writeCMOS(0x46E8, 0xFF);
                writeCMOS(0x46E9, 0x03);
                writeCMOS(0x46EC, 0x7A);
                writeCMOS(0x46EE, 0xE5);
                writeCMOS(0x46F4, 0xEE);
                writeCMOS(0x46F6, 0xF2);
                writeCMOS(0x470C, 0xFF);
                writeCMOS(0x470D, 0x03);
                writeCMOS(0x470E, 0x00);
                writeCMOS(0x4714, 0xE0);
                writeCMOS(0x4716, 0xE4);
                writeCMOS(0x471E, 0xED);
                writeCMOS(0x472E, 0x00);
                writeCMOS(0x4730, 0xFF);
                writeCMOS(0x4731, 0x03);
                writeCMOS(0x4734, 0x7B);
                writeCMOS(0x4736, 0xDF);
                writeCMOS(0x4754, 0x7D);
                writeCMOS(0x4756, 0x8B);
                writeCMOS(0x4758, 0x93);
                writeCMOS(0x475A, 0xB1);
                writeCMOS(0x475C, 0xFB);
                writeCMOS(0x475E, 0x09);
                writeCMOS(0x4760, 0x11);
                writeCMOS(0x4762, 0x2F);
                writeCMOS(0x4766, 0xCC);
                writeCMOS(0x4776, 0xCB);
                writeCMOS(0x477E, 0x4A);
                writeCMOS(0x478E, 0x49);
                writeCMOS(0x4794, 0x7C);
                writeCMOS(0x4796, 0x8F);
                writeCMOS(0x4798, 0xB3);
                writeCMOS(0x4799, 0x00);
                writeCMOS(0x479A, 0xCC);
                writeCMOS(0x479C, 0xC1);
                writeCMOS(0x479E, 0xCB);
                writeCMOS(0x47A4, 0x7D);
                writeCMOS(0x47A6, 0x8E);
                writeCMOS(0x47A8, 0xB4);
                writeCMOS(0x47A9, 0x00);
                writeCMOS(0x47AA, 0xC0);
                writeCMOS(0x47AC, 0xFA);
                writeCMOS(0x47AE, 0x0D);
                writeCMOS(0x47B0, 0x31);
                writeCMOS(0x47B1, 0x01);
                writeCMOS(0x47B2, 0x4A);
                writeCMOS(0x47B3, 0x01);
                writeCMOS(0x47B4, 0x3F);
                writeCMOS(0x47B6, 0x49);
                writeCMOS(0x47BC, 0xFB);
                writeCMOS(0x47BE, 0x0C);
                writeCMOS(0x47C0, 0x32);
                writeCMOS(0x47C1, 0x01);
                writeCMOS(0x47C2, 0x3E);
                writeCMOS(0x47C3, 0x01);

                
    /*            
                
                //"All-pixel scan CSI-2_4lane
                //74.25MHz
                //AD:10bit Output:12bit
                //1188Mbps
                //Slave  Mode
                //LCG Mode
                //30fps
                //Integration Time
                //33.289ms"

                writeCMOS(0x3015, 0x04);
                writeCMOS(0x3022, 0x00);
                writeCMOS(0x3023, 0x00);
                writeCMOS(0x3050, 0x03);
                //writeCMOS(0x30A6, 0x00);
                writeCMOS(0x3460, 0x22);
                writeCMOS(0x355A, 0x64);
                writeCMOS(0x3A02, 0x7A);
                writeCMOS(0x3A10, 0xEC);
                writeCMOS(0x3A12, 0x71);
                writeCMOS(0x3A14, 0xDE);
                writeCMOS(0x3A20, 0x2B);
                writeCMOS(0x3A24, 0x22);
                writeCMOS(0x3A25, 0x25);
                writeCMOS(0x3A26, 0x2A);
                writeCMOS(0x3A27, 0x2C);
                writeCMOS(0x3A28, 0x39);
                writeCMOS(0x3A29, 0x38);
                writeCMOS(0x3A30, 0x04);
                writeCMOS(0x3A31, 0x04);
                writeCMOS(0x3A32, 0x03);
                writeCMOS(0x3A33, 0x03);
                writeCMOS(0x3A34, 0x09);
                writeCMOS(0x3A35, 0x06);
                writeCMOS(0x3A38, 0xCD);
                writeCMOS(0x3A3A, 0x4C);
                writeCMOS(0x3A3C, 0xB9);
                writeCMOS(0x3A3E, 0x30);
                writeCMOS(0x3A40, 0x2C);
                writeCMOS(0x3A42, 0x39);
                writeCMOS(0x3A4E, 0x00);
                writeCMOS(0x3A52, 0x00);
                writeCMOS(0x3A56, 0x00);
                writeCMOS(0x3A5A, 0x00);
                writeCMOS(0x3A5E, 0x00);
                writeCMOS(0x3A62, 0x00);
                writeCMOS(0x3A6E, 0xA0);
                writeCMOS(0x3A70, 0x50);
                writeCMOS(0x3A8C, 0x04);
                writeCMOS(0x3A8D, 0x03);
                writeCMOS(0x3A8E, 0x09);
                writeCMOS(0x3A90, 0x38);
                writeCMOS(0x3A91, 0x42);
                writeCMOS(0x3A92, 0x3C);
                writeCMOS(0x3B0E, 0xF3);
                writeCMOS(0x3B12, 0xE5);
                writeCMOS(0x3B27, 0xC0);
                writeCMOS(0x3B2E, 0xEF);
                writeCMOS(0x3B30, 0x6A);
                writeCMOS(0x3B32, 0xF6);
                writeCMOS(0x3B36, 0xE1);
                writeCMOS(0x3B3A, 0xE8);
                writeCMOS(0x3B5A, 0x17);
                writeCMOS(0x3B5E, 0xEF);
                writeCMOS(0x3B60, 0x6A);
                writeCMOS(0x3B62, 0xF6);
                writeCMOS(0x3B66, 0xE1);
                writeCMOS(0x3B6A, 0xE8);
                writeCMOS(0x3B88, 0xEC);
                writeCMOS(0x3B8A, 0xED);
                writeCMOS(0x3B94, 0x71);
                writeCMOS(0x3B96, 0x72);
                writeCMOS(0x3B98, 0xDE);
                writeCMOS(0x3B9A, 0xDF);
                writeCMOS(0x3C0F, 0x06);
                writeCMOS(0x3C10, 0x06);
                writeCMOS(0x3C11, 0x06);
                writeCMOS(0x3C12, 0x06);
                writeCMOS(0x3C13, 0x06);
                writeCMOS(0x3C18, 0x20);
                writeCMOS(0x3C3A, 0x7A);
                writeCMOS(0x3C40, 0xF4);
                writeCMOS(0x3C48, 0xE6);
                writeCMOS(0x3C54, 0xCE);
                writeCMOS(0x3C56, 0xD0);
                writeCMOS(0x3C6C, 0x53);
                writeCMOS(0x3C6E, 0x55);
                writeCMOS(0x3C70, 0xC0);
                writeCMOS(0x3C72, 0xC2);
                writeCMOS(0x3C7E, 0xCE);
                writeCMOS(0x3C8C, 0xCF);
                writeCMOS(0x3C8E, 0xEB);
                writeCMOS(0x3C98, 0x54);
                writeCMOS(0x3C9A, 0x70);
                writeCMOS(0x3C9C, 0xC1);
                writeCMOS(0x3C9E, 0xDD);
                writeCMOS(0x3CB0, 0x7A);
                writeCMOS(0x3CB2, 0xBA);
                writeCMOS(0x3CC8, 0xBC);
                writeCMOS(0x3CCA, 0x7C);
                writeCMOS(0x3CD4, 0xEA);
                writeCMOS(0x3CD5, 0x01);
                writeCMOS(0x3CD6, 0x4A);
                writeCMOS(0x3CD8, 0x00);
                writeCMOS(0x3CD9, 0x00);
                writeCMOS(0x3CDA, 0xFF);
                writeCMOS(0x3CDB, 0x03);
                writeCMOS(0x3CDC, 0x00);
                writeCMOS(0x3CDD, 0x00);
                writeCMOS(0x3CDE, 0xFF);
                writeCMOS(0x3CDF, 0x03);
                writeCMOS(0x3CE4, 0x4C);
                writeCMOS(0x3CE6, 0xEC);
                writeCMOS(0x3CE7, 0x01);
                writeCMOS(0x3CE8, 0xFF);
                writeCMOS(0x3CE9, 0x03);
                writeCMOS(0x3CEA, 0x00);
                writeCMOS(0x3CEB, 0x00);
                writeCMOS(0x3CEC, 0xFF);
                writeCMOS(0x3CED, 0x03);
                writeCMOS(0x3CEE, 0x00);
                writeCMOS(0x3CEF, 0x00);
                writeCMOS(0x3E28, 0x82);
                writeCMOS(0x3E2A, 0x80);
                writeCMOS(0x3E30, 0x85);
                writeCMOS(0x3E32, 0x7D);
                writeCMOS(0x3E5C, 0xCE);
                writeCMOS(0x3E5E, 0xD3);
                writeCMOS(0x3E70, 0x53);
                writeCMOS(0x3E72, 0x58);
                writeCMOS(0x3E74, 0xC0);
                writeCMOS(0x3E76, 0xC5);
                writeCMOS(0x3E78, 0xC0);
                writeCMOS(0x3E79, 0x01);
                writeCMOS(0x3E7A, 0xD4);
                writeCMOS(0x3E7B, 0x01);
                writeCMOS(0x3EB4, 0x0B);
                writeCMOS(0x3EB5, 0x02);
                writeCMOS(0x3EB6, 0x4D);
                writeCMOS(0x3EEC, 0xF3);
                writeCMOS(0x3EEE, 0xE7);
                writeCMOS(0x3F01, 0x01);
                writeCMOS(0x3F24, 0x10);
                writeCMOS(0x3F28, 0x2D);
                writeCMOS(0x3F2A, 0x2D);
                writeCMOS(0x3F2C, 0x2D);
                writeCMOS(0x3F2E, 0x2D);
                writeCMOS(0x3F30, 0x23);
                writeCMOS(0x3F38, 0x2D);
                writeCMOS(0x3F3A, 0x2D);
                writeCMOS(0x3F3C, 0x2D);
                writeCMOS(0x3F3E, 0x28);
                writeCMOS(0x3F40, 0x1E);
                writeCMOS(0x3F48, 0x2D);
                writeCMOS(0x3F4A, 0x2D);
                writeCMOS(0x4004, 0xE4);
                writeCMOS(0x4006, 0xFF);
                writeCMOS(0x4018, 0x69);
                writeCMOS(0x401A, 0x84);
                writeCMOS(0x401C, 0xD6);
                writeCMOS(0x401E, 0xF1);
                writeCMOS(0x4038, 0xDE);
                writeCMOS(0x403A, 0x00);
                writeCMOS(0x403B, 0x01);
                writeCMOS(0x404C, 0x63);
                writeCMOS(0x404E, 0x85);
                writeCMOS(0x4050, 0xD0);
                writeCMOS(0x4052, 0xF2);
                writeCMOS(0x4108, 0xDD);
                writeCMOS(0x410A, 0xF7);
                writeCMOS(0x411C, 0x62);
                writeCMOS(0x411E, 0x7C);
                writeCMOS(0x4120, 0xCF);
                writeCMOS(0x4122, 0xE9);
                writeCMOS(0x4138, 0xE6);
                writeCMOS(0x413A, 0xF1);
                writeCMOS(0x414C, 0x6B);
                writeCMOS(0x414E, 0x76);
                writeCMOS(0x4150, 0xD8);
                writeCMOS(0x4152, 0xE3);
                writeCMOS(0x417E, 0x03);
                writeCMOS(0x417F, 0x01);
                writeCMOS(0x4186, 0xE0);
                writeCMOS(0x4190, 0xF3);
                writeCMOS(0x4192, 0xF7);
                writeCMOS(0x419C, 0x78);
                writeCMOS(0x419E, 0x7C);
                writeCMOS(0x41A0, 0xE5);
                writeCMOS(0x41A2, 0xE9);
                writeCMOS(0x41C8, 0xE2);
                writeCMOS(0x41CA, 0xFD);
                writeCMOS(0x41DC, 0x67);
                writeCMOS(0x41DE, 0x82);
                writeCMOS(0x41E0, 0xD4);
                writeCMOS(0x41E2, 0xEF);
                writeCMOS(0x4200, 0xDE);
                writeCMOS(0x4202, 0xDA);
                writeCMOS(0x4218, 0x63);
                writeCMOS(0x421A, 0x5F);
                writeCMOS(0x421C, 0xD0);
                writeCMOS(0x421E, 0xCC);
                writeCMOS(0x425A, 0x82);
                writeCMOS(0x425C, 0xEF);
                writeCMOS(0x4348, 0xFE);
                writeCMOS(0x4349, 0x06);
                writeCMOS(0x4352, 0xCE);
                writeCMOS(0x4420, 0x0B);
                writeCMOS(0x4421, 0x02);
                writeCMOS(0x4422, 0x4D);
                writeCMOS(0x4426, 0xF5);
                writeCMOS(0x442A, 0xE7);
                writeCMOS(0x4432, 0xF5);
                writeCMOS(0x4436, 0xE7);
                writeCMOS(0x4466, 0xB4);
                writeCMOS(0x446E, 0x32);
                writeCMOS(0x449F, 0x1C);
                writeCMOS(0x44A4, 0x2C);
                writeCMOS(0x44A6, 0x2C);
                writeCMOS(0x44A8, 0x2C);
                writeCMOS(0x44AA, 0x2C);
                writeCMOS(0x44B4, 0x2C);
                writeCMOS(0x44B6, 0x2C);
                writeCMOS(0x44B8, 0x2C);
                writeCMOS(0x44BA, 0x2C);
                writeCMOS(0x44C4, 0x2C);
                writeCMOS(0x44C6, 0x2C);
                writeCMOS(0x44C8, 0x2C);
                writeCMOS(0x4506, 0xF3);
                writeCMOS(0x450E, 0xE5);
                writeCMOS(0x4516, 0xF3);
                writeCMOS(0x4522, 0xE5);
                writeCMOS(0x4524, 0xF3);
                writeCMOS(0x452C, 0xE5);
                writeCMOS(0x453C, 0x22);
                writeCMOS(0x453D, 0x1B);
                writeCMOS(0x453E, 0x1B);
                writeCMOS(0x453F, 0x15);
                writeCMOS(0x4540, 0x15);
                writeCMOS(0x4541, 0x15);
                writeCMOS(0x4542, 0x15);
                writeCMOS(0x4543, 0x15);
                writeCMOS(0x4544, 0x15);
                writeCMOS(0x4548, 0x00);
                writeCMOS(0x4549, 0x01);
                writeCMOS(0x454A, 0x01);
                writeCMOS(0x454B, 0x06);
                writeCMOS(0x454C, 0x06);
                writeCMOS(0x454D, 0x06);
                writeCMOS(0x454E, 0x06);
                writeCMOS(0x454F, 0x06);
                writeCMOS(0x4550, 0x06);
                writeCMOS(0x4554, 0x55);
                writeCMOS(0x4555, 0x02);
                writeCMOS(0x4556, 0x42);
                writeCMOS(0x4557, 0x05);
                writeCMOS(0x4558, 0xFD);
                writeCMOS(0x4559, 0x05);
                writeCMOS(0x455A, 0x94);
                writeCMOS(0x455B, 0x06);
                writeCMOS(0x455D, 0x06);
                writeCMOS(0x455E, 0x49);
                writeCMOS(0x455F, 0x07);
                writeCMOS(0x4560, 0x7F);
                writeCMOS(0x4561, 0x07);
                writeCMOS(0x4562, 0xA5);
                writeCMOS(0x4564, 0x55);
                writeCMOS(0x4565, 0x02);
                writeCMOS(0x4566, 0x42);
                writeCMOS(0x4567, 0x05);
                writeCMOS(0x4568, 0xFD);
                writeCMOS(0x4569, 0x05);
                writeCMOS(0x456A, 0x94);
                writeCMOS(0x456B, 0x06);
                writeCMOS(0x456D, 0x06);
                writeCMOS(0x456E, 0x49);
                writeCMOS(0x456F, 0x07);
                writeCMOS(0x4572, 0xA5);
                writeCMOS(0x460C, 0x7D);
                writeCMOS(0x460E, 0xB1);
                writeCMOS(0x4614, 0xA8);
                writeCMOS(0x4616, 0xB2);
                writeCMOS(0x461C, 0x7E);
                writeCMOS(0x461E, 0xA7);
                writeCMOS(0x4624, 0xA8);
                writeCMOS(0x4626, 0xB2);
                writeCMOS(0x462C, 0x7E);
                writeCMOS(0x462E, 0x8A);
                writeCMOS(0x4630, 0x94);
                writeCMOS(0x4632, 0xA7);
                writeCMOS(0x4634, 0xFB);
                writeCMOS(0x4636, 0x2F);
                writeCMOS(0x4638, 0x81);
                writeCMOS(0x4639, 0x01);
                writeCMOS(0x463A, 0xB5);
                writeCMOS(0x463B, 0x01);
                writeCMOS(0x463C, 0x26);
                writeCMOS(0x463E, 0x30);
                writeCMOS(0x4640, 0xAC);
                writeCMOS(0x4641, 0x01);
                writeCMOS(0x4642, 0xB6);
                writeCMOS(0x4643, 0x01);
                writeCMOS(0x4644, 0xFC);
                writeCMOS(0x4646, 0x25);
                writeCMOS(0x4648, 0x82);
                writeCMOS(0x4649, 0x01);
                writeCMOS(0x464A, 0xAB);
                writeCMOS(0x464B, 0x01);
                writeCMOS(0x464C, 0x26);
                writeCMOS(0x464E, 0x30);
                writeCMOS(0x4654, 0xFC);
                writeCMOS(0x4656, 0x08);
                writeCMOS(0x4658, 0x12);
                writeCMOS(0x465A, 0x25);
                writeCMOS(0x4662, 0xFC);
                writeCMOS(0x46A2, 0xFB);
                writeCMOS(0x46D6, 0xF3);
                writeCMOS(0x46E6, 0x00);
                writeCMOS(0x46E8, 0xFF);
                writeCMOS(0x46E9, 0x03);
                writeCMOS(0x46EC, 0x7A);
                writeCMOS(0x46EE, 0xE5);
                writeCMOS(0x46F4, 0xEE);
                writeCMOS(0x46F6, 0xF2);
                writeCMOS(0x470C, 0xFF);
                writeCMOS(0x470D, 0x03);
                writeCMOS(0x470E, 0x00);
                writeCMOS(0x4714, 0xE0);
                writeCMOS(0x4716, 0xE4);
                writeCMOS(0x471E, 0xED);
                writeCMOS(0x472E, 0x00);
                writeCMOS(0x4730, 0xFF);
                writeCMOS(0x4731, 0x03);
                writeCMOS(0x4734, 0x7B);
                writeCMOS(0x4736, 0xDF);
                writeCMOS(0x4754, 0x7D);
                writeCMOS(0x4756, 0x8B);
                writeCMOS(0x4758, 0x93);
                writeCMOS(0x475A, 0xB1);
                writeCMOS(0x475C, 0xFB);
                writeCMOS(0x475E, 0x09);
                writeCMOS(0x4760, 0x11);
                writeCMOS(0x4762, 0x2F);
                writeCMOS(0x4766, 0xCC);
                writeCMOS(0x4776, 0xCB);
                writeCMOS(0x477E, 0x4A);
                writeCMOS(0x478E, 0x49);
                writeCMOS(0x4794, 0x7C);
                writeCMOS(0x4796, 0x8F);
                writeCMOS(0x4798, 0xB3);
                writeCMOS(0x4799, 0x00);
                writeCMOS(0x479A, 0xCC);
                writeCMOS(0x479C, 0xC1);
                writeCMOS(0x479E, 0xCB);
                writeCMOS(0x47A4, 0x7D);
                writeCMOS(0x47A6, 0x8E);
                writeCMOS(0x47A8, 0xB4);
                writeCMOS(0x47A9, 0x00);
                writeCMOS(0x47AA, 0xC0);
                writeCMOS(0x47AC, 0xFA);
                writeCMOS(0x47AE, 0x0D);
                writeCMOS(0x47B0, 0x31);
                writeCMOS(0x47B1, 0x01);
                writeCMOS(0x47B2, 0x4A);
                writeCMOS(0x47B3, 0x01);
                writeCMOS(0x47B4, 0x3F);
                writeCMOS(0x47B6, 0x49);
                writeCMOS(0x47BC, 0xFB);
                writeCMOS(0x47BE, 0x0C);
                writeCMOS(0x47C0, 0x32);
                writeCMOS(0x47C1, 0x01);
                writeCMOS(0x47C2, 0x3E);
                writeCMOS(0x47C3, 0x01);

             */
                hmax_init = 1500;
                


            
            }
            else
            {
                //"All-pixel scan CSI-2_4lane
                //74.25MHz
                //AD:12bit Output:12bit
                //1188Mbps
                //Slave  Mode
                //LCG Mode
                //30fps
                //Integration Time
                //33.289ms"

                writeCMOS(0x3015, 0x04);
                writeCMOS(0x3050, 0x03);
                //writeCMOS(0x30A6, 0x00);
                writeCMOS(0x3460, 0x22);
                writeCMOS(0x355A, 0x64);
                writeCMOS(0x3A02, 0x7A);
                writeCMOS(0x3A10, 0xEC);
                writeCMOS(0x3A12, 0x71);
                writeCMOS(0x3A14, 0xDE);
                writeCMOS(0x3A20, 0x2B);
                writeCMOS(0x3A24, 0x22);
                writeCMOS(0x3A25, 0x25);
                writeCMOS(0x3A26, 0x2A);
                writeCMOS(0x3A27, 0x2C);
                writeCMOS(0x3A28, 0x39);
                writeCMOS(0x3A29, 0x38);
                writeCMOS(0x3A30, 0x04);
                writeCMOS(0x3A31, 0x04);
                writeCMOS(0x3A32, 0x03);
                writeCMOS(0x3A33, 0x03);
                writeCMOS(0x3A34, 0x09);
                writeCMOS(0x3A35, 0x06);
                writeCMOS(0x3A38, 0xCD);
                writeCMOS(0x3A3A, 0x4C);
                writeCMOS(0x3A3C, 0xB9);
                writeCMOS(0x3A3E, 0x30);
                writeCMOS(0x3A40, 0x2C);
                writeCMOS(0x3A42, 0x39);
                writeCMOS(0x3A4E, 0x00);
                writeCMOS(0x3A52, 0x00);
                writeCMOS(0x3A56, 0x00);
                writeCMOS(0x3A5A, 0x00);
                writeCMOS(0x3A5E, 0x00);
                writeCMOS(0x3A62, 0x00);
                writeCMOS(0x3A6E, 0xA0);
                writeCMOS(0x3A70, 0x50);
                writeCMOS(0x3A8C, 0x04);
                writeCMOS(0x3A8D, 0x03);
                writeCMOS(0x3A8E, 0x09);
                writeCMOS(0x3A90, 0x38);
                writeCMOS(0x3A91, 0x42);
                writeCMOS(0x3A92, 0x3C);
                writeCMOS(0x3B0E, 0xF3);
                writeCMOS(0x3B12, 0xE5);
                writeCMOS(0x3B27, 0xC0);
                writeCMOS(0x3B2E, 0xEF);
                writeCMOS(0x3B30, 0x6A);
                writeCMOS(0x3B32, 0xF6);
                writeCMOS(0x3B36, 0xE1);
                writeCMOS(0x3B3A, 0xE8);
                writeCMOS(0x3B5A, 0x17);
                writeCMOS(0x3B5E, 0xEF);
                writeCMOS(0x3B60, 0x6A);
                writeCMOS(0x3B62, 0xF6);
                writeCMOS(0x3B66, 0xE1);
                writeCMOS(0x3B6A, 0xE8);
                writeCMOS(0x3B88, 0xEC);
                writeCMOS(0x3B8A, 0xED);
                writeCMOS(0x3B94, 0x71);
                writeCMOS(0x3B96, 0x72);
                writeCMOS(0x3B98, 0xDE);
                writeCMOS(0x3B9A, 0xDF);
                writeCMOS(0x3C0F, 0x06);
                writeCMOS(0x3C10, 0x06);
                writeCMOS(0x3C11, 0x06);
                writeCMOS(0x3C12, 0x06);
                writeCMOS(0x3C13, 0x06);
                writeCMOS(0x3C18, 0x20);
                writeCMOS(0x3C3A, 0x7A);
                writeCMOS(0x3C40, 0xF4);
                writeCMOS(0x3C48, 0xE6);
                writeCMOS(0x3C54, 0xCE);
                writeCMOS(0x3C56, 0xD0);
                writeCMOS(0x3C6C, 0x53);
                writeCMOS(0x3C6E, 0x55);
                writeCMOS(0x3C70, 0xC0);
                writeCMOS(0x3C72, 0xC2);
                writeCMOS(0x3C7E, 0xCE);
                writeCMOS(0x3C8C, 0xCF);
                writeCMOS(0x3C8E, 0xEB);
                writeCMOS(0x3C98, 0x54);
                writeCMOS(0x3C9A, 0x70);
                writeCMOS(0x3C9C, 0xC1);
                writeCMOS(0x3C9E, 0xDD);
                writeCMOS(0x3CB0, 0x7A);
                writeCMOS(0x3CB2, 0xBA);
                writeCMOS(0x3CC8, 0xBC);
                writeCMOS(0x3CCA, 0x7C);
                writeCMOS(0x3CD4, 0xEA);
                writeCMOS(0x3CD5, 0x01);
                writeCMOS(0x3CD6, 0x4A);
                writeCMOS(0x3CD8, 0x00);
                writeCMOS(0x3CD9, 0x00);
                writeCMOS(0x3CDA, 0xFF);
                writeCMOS(0x3CDB, 0x03);
                writeCMOS(0x3CDC, 0x00);
                writeCMOS(0x3CDD, 0x00);
                writeCMOS(0x3CDE, 0xFF);
                writeCMOS(0x3CDF, 0x03);
                writeCMOS(0x3CE4, 0x4C);
                writeCMOS(0x3CE6, 0xEC);
                writeCMOS(0x3CE7, 0x01);
                writeCMOS(0x3CE8, 0xFF);
                writeCMOS(0x3CE9, 0x03);
                writeCMOS(0x3CEA, 0x00);
                writeCMOS(0x3CEB, 0x00);
                writeCMOS(0x3CEC, 0xFF);
                writeCMOS(0x3CED, 0x03);
                writeCMOS(0x3CEE, 0x00);
                writeCMOS(0x3CEF, 0x00);
                writeCMOS(0x3E28, 0x82);
                writeCMOS(0x3E2A, 0x80);
                writeCMOS(0x3E30, 0x85);
                writeCMOS(0x3E32, 0x7D);
                writeCMOS(0x3E5C, 0xCE);
                writeCMOS(0x3E5E, 0xD3);
                writeCMOS(0x3E70, 0x53);
                writeCMOS(0x3E72, 0x58);
                writeCMOS(0x3E74, 0xC0);
                writeCMOS(0x3E76, 0xC5);
                writeCMOS(0x3E78, 0xC0);
                writeCMOS(0x3E79, 0x01);
                writeCMOS(0x3E7A, 0xD4);
                writeCMOS(0x3E7B, 0x01);
                writeCMOS(0x3EB4, 0x0B);
                writeCMOS(0x3EB5, 0x02);
                writeCMOS(0x3EB6, 0x4D);
                writeCMOS(0x3EEC, 0xF3);
                writeCMOS(0x3EEE, 0xE7);
                writeCMOS(0x3F01, 0x01);
                writeCMOS(0x3F24, 0x10);
                writeCMOS(0x3F28, 0x2D);
                writeCMOS(0x3F2A, 0x2D);
                writeCMOS(0x3F2C, 0x2D);
                writeCMOS(0x3F2E, 0x2D);
                writeCMOS(0x3F30, 0x23);
                writeCMOS(0x3F38, 0x2D);
                writeCMOS(0x3F3A, 0x2D);
                writeCMOS(0x3F3C, 0x2D);
                writeCMOS(0x3F3E, 0x28);
                writeCMOS(0x3F40, 0x1E);
                writeCMOS(0x3F48, 0x2D);
                writeCMOS(0x3F4A, 0x2D);
                writeCMOS(0x4004, 0xE4);
                writeCMOS(0x4006, 0xFF);
                writeCMOS(0x4018, 0x69);
                writeCMOS(0x401A, 0x84);
                writeCMOS(0x401C, 0xD6);
                writeCMOS(0x401E, 0xF1);
                writeCMOS(0x4038, 0xDE);
                writeCMOS(0x403A, 0x00);
                writeCMOS(0x403B, 0x01);
                writeCMOS(0x404C, 0x63);
                writeCMOS(0x404E, 0x85);
                writeCMOS(0x4050, 0xD0);
                writeCMOS(0x4052, 0xF2);
                writeCMOS(0x4108, 0xDD);
                writeCMOS(0x410A, 0xF7);
                writeCMOS(0x411C, 0x62);
                writeCMOS(0x411E, 0x7C);
                writeCMOS(0x4120, 0xCF);
                writeCMOS(0x4122, 0xE9);
                writeCMOS(0x4138, 0xE6);
                writeCMOS(0x413A, 0xF1);
                writeCMOS(0x414C, 0x6B);
                writeCMOS(0x414E, 0x76);
                writeCMOS(0x4150, 0xD8);
                writeCMOS(0x4152, 0xE3);
                writeCMOS(0x417E, 0x03);
                writeCMOS(0x417F, 0x01);
                writeCMOS(0x4186, 0xE0);
                writeCMOS(0x4190, 0xF3);
                writeCMOS(0x4192, 0xF7);
                writeCMOS(0x419C, 0x78);
                writeCMOS(0x419E, 0x7C);
                writeCMOS(0x41A0, 0xE5);
                writeCMOS(0x41A2, 0xE9);
                writeCMOS(0x41C8, 0xE2);
                writeCMOS(0x41CA, 0xFD);
                writeCMOS(0x41DC, 0x67);
                writeCMOS(0x41DE, 0x82);
                writeCMOS(0x41E0, 0xD4);
                writeCMOS(0x41E2, 0xEF);
                writeCMOS(0x4200, 0xDE);
                writeCMOS(0x4202, 0xDA);
                writeCMOS(0x4218, 0x63);
                writeCMOS(0x421A, 0x5F);
                writeCMOS(0x421C, 0xD0);
                writeCMOS(0x421E, 0xCC);
                writeCMOS(0x425A, 0x82);
                writeCMOS(0x425C, 0xEF);
                writeCMOS(0x4348, 0xFE);
                writeCMOS(0x4349, 0x06);
                writeCMOS(0x4352, 0xCE);
                writeCMOS(0x4420, 0x0B);
                writeCMOS(0x4421, 0x02);
                writeCMOS(0x4422, 0x4D);
                writeCMOS(0x4426, 0xF5);
                writeCMOS(0x442A, 0xE7);
                writeCMOS(0x4432, 0xF5);
                writeCMOS(0x4436, 0xE7);
                writeCMOS(0x4466, 0xB4);
                writeCMOS(0x446E, 0x32);
                writeCMOS(0x449F, 0x1C);
                writeCMOS(0x44A4, 0x2C);
                writeCMOS(0x44A6, 0x2C);
                writeCMOS(0x44A8, 0x2C);
                writeCMOS(0x44AA, 0x2C);
                writeCMOS(0x44B4, 0x2C);
                writeCMOS(0x44B6, 0x2C);
                writeCMOS(0x44B8, 0x2C);
                writeCMOS(0x44BA, 0x2C);
                writeCMOS(0x44C4, 0x2C);
                writeCMOS(0x44C6, 0x2C);
                writeCMOS(0x44C8, 0x2C);
                writeCMOS(0x4506, 0xF3);
                writeCMOS(0x450E, 0xE5);
                writeCMOS(0x4516, 0xF3);
                writeCMOS(0x4522, 0xE5);
                writeCMOS(0x4524, 0xF3);
                writeCMOS(0x452C, 0xE5);
                writeCMOS(0x453C, 0x22);
                writeCMOS(0x453D, 0x1B);
                writeCMOS(0x453E, 0x1B);
                writeCMOS(0x453F, 0x15);
                writeCMOS(0x4540, 0x15);
                writeCMOS(0x4541, 0x15);
                writeCMOS(0x4542, 0x15);
                writeCMOS(0x4543, 0x15);
                writeCMOS(0x4544, 0x15);
                writeCMOS(0x4548, 0x00);
                writeCMOS(0x4549, 0x01);
                writeCMOS(0x454A, 0x01);
                writeCMOS(0x454B, 0x06);
                writeCMOS(0x454C, 0x06);
                writeCMOS(0x454D, 0x06);
                writeCMOS(0x454E, 0x06);
                writeCMOS(0x454F, 0x06);
                writeCMOS(0x4550, 0x06);
                writeCMOS(0x4554, 0x55);
                writeCMOS(0x4555, 0x02);
                writeCMOS(0x4556, 0x42);
                writeCMOS(0x4557, 0x05);
                writeCMOS(0x4558, 0xFD);
                writeCMOS(0x4559, 0x05);
                writeCMOS(0x455A, 0x94);
                writeCMOS(0x455B, 0x06);
                writeCMOS(0x455D, 0x06);
                writeCMOS(0x455E, 0x49);
                writeCMOS(0x455F, 0x07);
                writeCMOS(0x4560, 0x7F);
                writeCMOS(0x4561, 0x07);
                writeCMOS(0x4562, 0xA5);
                writeCMOS(0x4564, 0x55);
                writeCMOS(0x4565, 0x02);
                writeCMOS(0x4566, 0x42);
                writeCMOS(0x4567, 0x05);
                writeCMOS(0x4568, 0xFD);
                writeCMOS(0x4569, 0x05);
                writeCMOS(0x456A, 0x94);
                writeCMOS(0x456B, 0x06);
                writeCMOS(0x456D, 0x06);
                writeCMOS(0x456E, 0x49);
                writeCMOS(0x456F, 0x07);
                writeCMOS(0x4572, 0xA5);
                writeCMOS(0x460C, 0x7D);
                writeCMOS(0x460E, 0xB1);
                writeCMOS(0x4614, 0xA8);
                writeCMOS(0x4616, 0xB2);
                writeCMOS(0x461C, 0x7E);
                writeCMOS(0x461E, 0xA7);
                writeCMOS(0x4624, 0xA8);
                writeCMOS(0x4626, 0xB2);
                writeCMOS(0x462C, 0x7E);
                writeCMOS(0x462E, 0x8A);
                writeCMOS(0x4630, 0x94);
                writeCMOS(0x4632, 0xA7);
                writeCMOS(0x4634, 0xFB);
                writeCMOS(0x4636, 0x2F);
                writeCMOS(0x4638, 0x81);
                writeCMOS(0x4639, 0x01);
                writeCMOS(0x463A, 0xB5);
                writeCMOS(0x463B, 0x01);
                writeCMOS(0x463C, 0x26);
                writeCMOS(0x463E, 0x30);
                writeCMOS(0x4640, 0xAC);
                writeCMOS(0x4641, 0x01);
                writeCMOS(0x4642, 0xB6);
                writeCMOS(0x4643, 0x01);
                writeCMOS(0x4644, 0xFC);
                writeCMOS(0x4646, 0x25);
                writeCMOS(0x4648, 0x82);
                writeCMOS(0x4649, 0x01);
                writeCMOS(0x464A, 0xAB);
                writeCMOS(0x464B, 0x01);
                writeCMOS(0x464C, 0x26);
                writeCMOS(0x464E, 0x30);
                writeCMOS(0x4654, 0xFC);
                writeCMOS(0x4656, 0x08);
                writeCMOS(0x4658, 0x12);
                writeCMOS(0x465A, 0x25);
                writeCMOS(0x4662, 0xFC);
                writeCMOS(0x46A2, 0xFB);
                writeCMOS(0x46D6, 0xF3);
                writeCMOS(0x46E6, 0x00);
                writeCMOS(0x46E8, 0xFF);
                writeCMOS(0x46E9, 0x03);
                writeCMOS(0x46EC, 0x7A);
                writeCMOS(0x46EE, 0xE5);
                writeCMOS(0x46F4, 0xEE);
                writeCMOS(0x46F6, 0xF2);
                writeCMOS(0x470C, 0xFF);
                writeCMOS(0x470D, 0x03);
                writeCMOS(0x470E, 0x00);
                writeCMOS(0x4714, 0xE0);
                writeCMOS(0x4716, 0xE4);
                writeCMOS(0x471E, 0xED);
                writeCMOS(0x472E, 0x00);
                writeCMOS(0x4730, 0xFF);
                writeCMOS(0x4731, 0x03);
                writeCMOS(0x4734, 0x7B);
                writeCMOS(0x4736, 0xDF);
                writeCMOS(0x4754, 0x7D);
                writeCMOS(0x4756, 0x8B);
                writeCMOS(0x4758, 0x93);
                writeCMOS(0x475A, 0xB1);
                writeCMOS(0x475C, 0xFB);
                writeCMOS(0x475E, 0x09);
                writeCMOS(0x4760, 0x11);
                writeCMOS(0x4762, 0x2F);
                writeCMOS(0x4766, 0xCC);
                writeCMOS(0x4776, 0xCB);
                writeCMOS(0x477E, 0x4A);
                writeCMOS(0x478E, 0x49);
                writeCMOS(0x4794, 0x7C);
                writeCMOS(0x4796, 0x8F);
                writeCMOS(0x4798, 0xB3);
                writeCMOS(0x4799, 0x00);
                writeCMOS(0x479A, 0xCC);
                writeCMOS(0x479C, 0xC1);
                writeCMOS(0x479E, 0xCB);
                writeCMOS(0x47A4, 0x7D);
                writeCMOS(0x47A6, 0x8E);
                writeCMOS(0x47A8, 0xB4);
                writeCMOS(0x47A9, 0x00);
                writeCMOS(0x47AA, 0xC0);
                writeCMOS(0x47AC, 0xFA);
                writeCMOS(0x47AE, 0x0D);
                writeCMOS(0x47B0, 0x31);
                writeCMOS(0x47B1, 0x01);
                writeCMOS(0x47B2, 0x4A);
                writeCMOS(0x47B3, 0x01);
                writeCMOS(0x47B4, 0x3F);
                writeCMOS(0x47B6, 0x49);
                writeCMOS(0x47BC, 0xFB);
                writeCMOS(0x47BE, 0x0C);
                writeCMOS(0x47C0, 0x32);
                writeCMOS(0x47C1, 0x01);
                writeCMOS(0x47C2, 0x3E);
                writeCMOS(0x47C3, 0x01);
                hmax_init = 1100;
                

            }

            writeCMOS(0x3000, 0x00);
           
            if (masterslave == 0x00)//master
            {
                /*
                //master mode 
                writeCMOS(0x3002, 0x00);
                writeCMOS(0x30a4, 0xaa);
                writeCMOS(0x30a6, 0x00);
               */
            }
            else
            {
                //SLAVE MODE 
                writeCMOS(0x30A6, 0x0F);
            }

            setIDLE();


            hmax = 262;//(UInt16)(((100000 / 7425) * hmax_init) / 40 + 2);//hmax_init
            vmax = 2220;// 2250;

            richTextBox1.AppendText("Hmax :  " + hmax.ToString("d") +"Vmax :" + vmax.ToString("D")+  Environment.NewLine);

            setHMAX(hmax);
            setVMAX(vmax);

            releaseIDLE();
        }

        void initCMOS_IMX585(byte mode, byte masterslave)
        {
            //ALL PIXEL 74.25mHZ 12bit OUTPUT 1440Mbps
            resetCMOS();
            Thread.Sleep(200);
            writeCMOS(0x3000, 0x01);
            if (mode == 0x0a)//16bit
            { }
            else if (mode == 0x0b)//14bit
            { }
            else if (mode == 0x0c)
            {
                
                //12bit ALL PIXEL 74.25mHZ 12bit OUTPUT 1440Mbps
                writeCMOS(0x3015, 0x03);
                writeCMOS(0x302C, 0x94);
                writeCMOS(0x3040, 0x03);
                writeCMOS(0x3050, 0x08);
                writeCMOS(0x30A6, 0x00);
                writeCMOS(0x3460, 0x21);
                writeCMOS(0x3478, 0xA1);
                writeCMOS(0x347C, 0x01);
                writeCMOS(0x3480, 0x01);
                writeCMOS(0x3930, 0x0C);
                writeCMOS(0x3931, 0x01);
                writeCMOS(0x3A4C, 0x39);
                writeCMOS(0x3A4D, 0x01);
                writeCMOS(0x3A4E, 0x14);
                writeCMOS(0x3A50, 0x48);
                writeCMOS(0x3A51, 0x01);
                writeCMOS(0x3A52, 0x14);
                writeCMOS(0x3A56, 0x00);
                writeCMOS(0x3A5A, 0x00);
                writeCMOS(0x3A5E, 0x00);
                writeCMOS(0x3A62, 0x00);
                writeCMOS(0x3A6A, 0x20);
                writeCMOS(0x3A6C, 0x42);
                writeCMOS(0x3A6E, 0xA0);
                writeCMOS(0x3B2C, 0x0C);
                writeCMOS(0x3B30, 0x1C);
                writeCMOS(0x3B34, 0x0C);
                writeCMOS(0x3B38, 0x1C);
                writeCMOS(0x3BA0, 0x0C);
                writeCMOS(0x3BA4, 0x1C);
                writeCMOS(0x3BA8, 0x0C);
                writeCMOS(0x3BAC, 0x1C);
                writeCMOS(0x3D3C, 0x11);
                writeCMOS(0x3D46, 0x0B);
                writeCMOS(0x3DE0, 0x3F);
                writeCMOS(0x3DE1, 0x08);
                writeCMOS(0x3E10, 0x10);
                writeCMOS(0x3E14, 0x87);
                writeCMOS(0x3E16, 0x91);
                writeCMOS(0x3E18, 0x91);
                writeCMOS(0x3E1A, 0x87);
                writeCMOS(0x3E1C, 0x78);
                writeCMOS(0x3E1E, 0x50);
                writeCMOS(0x3E20, 0x50);
                writeCMOS(0x3E22, 0x50);
                writeCMOS(0x3E24, 0x87);
                writeCMOS(0x3E26, 0x91);
                writeCMOS(0x3E28, 0x91);
                writeCMOS(0x3E2A, 0x87);
                writeCMOS(0x3E2C, 0x78);
                writeCMOS(0x3E2E, 0x50);
                writeCMOS(0x3E30, 0x50);
                writeCMOS(0x3E32, 0x50);
                writeCMOS(0x3E34, 0x87);
                writeCMOS(0x3E36, 0x91);
                writeCMOS(0x3E38, 0x91);
                writeCMOS(0x3E3A, 0x87);
                writeCMOS(0x3E3C, 0x78);
                writeCMOS(0x3E3E, 0x50);
                writeCMOS(0x3E40, 0x50);
                writeCMOS(0x3E42, 0x50);
                writeCMOS(0x4054, 0x64);
                writeCMOS(0x4148, 0xFE);
                writeCMOS(0x4149, 0x05);
                writeCMOS(0x414A, 0xFF);
                writeCMOS(0x414B, 0x05);
                writeCMOS(0x420A, 0x03);
                writeCMOS(0x4231, 0x08);
                writeCMOS(0x423D, 0x9C);
                writeCMOS(0x4242, 0xB4);
                writeCMOS(0x4246, 0xB4);
                writeCMOS(0x424E, 0xB4);
                writeCMOS(0x425C, 0xB4);
                writeCMOS(0x425E, 0xB6);
                writeCMOS(0x426C, 0xB4);
                writeCMOS(0x426E, 0xB6);
                writeCMOS(0x428C, 0xB4);
                writeCMOS(0x428E, 0xB6);
                writeCMOS(0x4708, 0x00);
                writeCMOS(0x4709, 0x00);
                writeCMOS(0x470A, 0xFF);
                writeCMOS(0x470B, 0x03);
                writeCMOS(0x470C, 0x00);
                writeCMOS(0x470D, 0x00);
                writeCMOS(0x470E, 0xFF);
                writeCMOS(0x470F, 0x03);
                writeCMOS(0x47EB, 0x1C);
                writeCMOS(0x47F0, 0xA6);
                writeCMOS(0x47F2, 0xA6);
                writeCMOS(0x47F4, 0xA0);
                writeCMOS(0x47F6, 0x96);
                writeCMOS(0x4808, 0xA6);
                writeCMOS(0x480A, 0xA6);
                writeCMOS(0x480C, 0xA0);
                writeCMOS(0x480E, 0x96);
                writeCMOS(0x492C, 0xB2);
                writeCMOS(0x4930, 0x03);
                writeCMOS(0x4932, 0x03);
                writeCMOS(0x4936, 0x5B);
                writeCMOS(0x4938, 0x82);
                writeCMOS(0x493C, 0x23);
                writeCMOS(0x493E, 0x23);
                writeCMOS(0x4940, 0x23);
                writeCMOS(0x4BA8, 0x1C);
                writeCMOS(0x4BA9, 0x03);
                writeCMOS(0x4BAC, 0x1C);
                writeCMOS(0x4BAD, 0x1C);
                writeCMOS(0x4BAE, 0x1C);
                writeCMOS(0x4BAF, 0x1C);
                writeCMOS(0x4BB0, 0x1C);
                writeCMOS(0x4BB1, 0x1C);
                writeCMOS(0x4BB2, 0x1C);
                writeCMOS(0x4BB3, 0x1C);
                writeCMOS(0x4BB4, 0x1C);
                writeCMOS(0x4BB8, 0x03);
                writeCMOS(0x4BB9, 0x03);
                writeCMOS(0x4BBA, 0x03);
                writeCMOS(0x4BBB, 0x03);
                writeCMOS(0x4BBC, 0x03);
                writeCMOS(0x4BBD, 0x03);
                writeCMOS(0x4BBE, 0x03);
                writeCMOS(0x4BBF, 0x03);
                writeCMOS(0x4BC0, 0x03);
                writeCMOS(0x4C14, 0x87);
                writeCMOS(0x4C16, 0x91);
                writeCMOS(0x4C18, 0x91);
                writeCMOS(0x4C1A, 0x87);
                writeCMOS(0x4C1C, 0x78);
                writeCMOS(0x4C1E, 0x50);
                writeCMOS(0x4C20, 0x50);
                writeCMOS(0x4C22, 0x50);
                writeCMOS(0x4C24, 0x87);
                writeCMOS(0x4C26, 0x91);
                writeCMOS(0x4C28, 0x91);
                writeCMOS(0x4C2A, 0x87);
                writeCMOS(0x4C2C, 0x78);
                writeCMOS(0x4C2E, 0x50);
                writeCMOS(0x4C30, 0x50);
                writeCMOS(0x4C32, 0x50);
                writeCMOS(0x4C34, 0x87);
                writeCMOS(0x4C36, 0x91);
                writeCMOS(0x4C38, 0x91);
                writeCMOS(0x4C3A, 0x87);
                writeCMOS(0x4C3C, 0x78);
                writeCMOS(0x4C3E, 0x50);
                writeCMOS(0x4C40, 0x50);
                writeCMOS(0x4C42, 0x50);
                writeCMOS(0x4D12, 0x1F);
                writeCMOS(0x4D13, 0x1E);
                writeCMOS(0x4D26, 0x33);
                writeCMOS(0x4E0E, 0x59);
                writeCMOS(0x4E14, 0x55);
                writeCMOS(0x4E16, 0x59);
                writeCMOS(0x4E1E, 0x3B);
                writeCMOS(0x4E20, 0x47);
                writeCMOS(0x4E22, 0x54);
                writeCMOS(0x4E26, 0x81);
                writeCMOS(0x4E2C, 0x7D);
                writeCMOS(0x4E2E, 0x81);
                writeCMOS(0x4E36, 0x63);
                writeCMOS(0x4E38, 0x6F);
                writeCMOS(0x4E3A, 0x7C);
                writeCMOS(0x4F3A, 0x3C);
                writeCMOS(0x4F3C, 0x46);
                writeCMOS(0x4F3E, 0x59);
                writeCMOS(0x4F42, 0x64);
                writeCMOS(0x4F44, 0x6E);
                writeCMOS(0x4F46, 0x81);
                writeCMOS(0x4F4A, 0x82);
                writeCMOS(0x4F5A, 0x81);
                writeCMOS(0x4F62, 0xAA);
                writeCMOS(0x4F72, 0xA9);
                writeCMOS(0x4F78, 0x36);
                writeCMOS(0x4F7A, 0x41);
                writeCMOS(0x4F7C, 0x61);
                writeCMOS(0x4F7D, 0x01);
                writeCMOS(0x4F7E, 0x7C);
                writeCMOS(0x4F7F, 0x01);
                writeCMOS(0x4F80, 0x77);
                writeCMOS(0x4F82, 0x7B);
                writeCMOS(0x4F88, 0x37);
                writeCMOS(0x4F8A, 0x40);
                writeCMOS(0x4F8C, 0x62);
                writeCMOS(0x4F8D, 0x01);
                writeCMOS(0x4F8E, 0x76);
                writeCMOS(0x4F8F, 0x01);
                writeCMOS(0x4F90, 0x5E);
                writeCMOS(0x4F91, 0x02);
                writeCMOS(0x4F92, 0x69);
                writeCMOS(0x4F93, 0x02);
                writeCMOS(0x4F94, 0x89);
                writeCMOS(0x4F95, 0x02);
                writeCMOS(0x4F96, 0xA4);
                writeCMOS(0x4F97, 0x02);
                writeCMOS(0x4F98, 0x9F);
                writeCMOS(0x4F99, 0x02);
                writeCMOS(0x4F9A, 0xA3);
                writeCMOS(0x4F9B, 0x02);
                writeCMOS(0x4FA0, 0x5F);
                writeCMOS(0x4FA1, 0x02);
                writeCMOS(0x4FA2, 0x68);
                writeCMOS(0x4FA3, 0x02);
                writeCMOS(0x4FA4, 0x8A);
                writeCMOS(0x4FA5, 0x02);
                writeCMOS(0x4FA6, 0x9E);
                writeCMOS(0x4FA7, 0x02);
                writeCMOS(0x519E, 0x79);
                writeCMOS(0x51A6, 0xA1);
                writeCMOS(0x51F0, 0xAC);
                writeCMOS(0x51F2, 0xAA);
                writeCMOS(0x51F4, 0xA5);
                writeCMOS(0x51F6, 0xA0);
                writeCMOS(0x5200, 0x9B);
                writeCMOS(0x5202, 0x91);
                writeCMOS(0x5204, 0x87);
                writeCMOS(0x5206, 0x82);
                writeCMOS(0x5208, 0xAC);
                writeCMOS(0x520A, 0xAA);
                writeCMOS(0x520C, 0xA5);
                writeCMOS(0x520E, 0xA0);
                writeCMOS(0x5210, 0x9B);
                writeCMOS(0x5212, 0x91);
                writeCMOS(0x5214, 0x87);
                writeCMOS(0x5216, 0x82);
                writeCMOS(0x5218, 0xAC);
                writeCMOS(0x521A, 0xAA);
                writeCMOS(0x521C, 0xA5);
                writeCMOS(0x521E, 0xA0);
                writeCMOS(0x5220, 0x9B);
                writeCMOS(0x5222, 0x91);
                writeCMOS(0x5224, 0x87);
                writeCMOS(0x5226, 0x82);
                

                /*
                // 12bit 1118 MBPS
                writeCMOS(0x3015, 0x04);//1118 MBPS
                writeCMOS(0x302C, 0x4C);
                writeCMOS(0x302D, 0x04);
                writeCMOS(0x3040, 0x03);
                writeCMOS(0x3050, 0x08);
                writeCMOS(0x30A6, 0x00);
                writeCMOS(0x3460, 0x21);
                writeCMOS(0x3478, 0xA1);
                writeCMOS(0x347C, 0x01);
                writeCMOS(0x3480, 0x01);
                writeCMOS(0x3930, 0x0C);
                writeCMOS(0x3931, 0x01);
                writeCMOS(0x3A4C, 0x39);
                writeCMOS(0x3A4D, 0x01);
                writeCMOS(0x3A4E, 0x14);
                writeCMOS(0x3A50, 0x48);
                writeCMOS(0x3A51, 0x01);
                writeCMOS(0x3A52, 0x14);
                writeCMOS(0x3A56, 0x00);
                writeCMOS(0x3A5A, 0x00);
                writeCMOS(0x3A5E, 0x00);
                writeCMOS(0x3A62, 0x00);
                writeCMOS(0x3A6A, 0x20);
                writeCMOS(0x3A6C, 0x42);
                writeCMOS(0x3A6E, 0xA0);
                writeCMOS(0x3B2C, 0x0C);
                writeCMOS(0x3B30, 0x1C);
                writeCMOS(0x3B34, 0x0C);
                writeCMOS(0x3B38, 0x1C);
                writeCMOS(0x3BA0, 0x0C);
                writeCMOS(0x3BA4, 0x1C);
                writeCMOS(0x3BA8, 0x0C);
                writeCMOS(0x3BAC, 0x1C);
                writeCMOS(0x3D3C, 0x11);
                writeCMOS(0x3D46, 0x0B);
                writeCMOS(0x3DE0, 0x3F);
                writeCMOS(0x3DE1, 0x08);
                writeCMOS(0x3E10, 0x10);
                writeCMOS(0x3E14, 0x87);
                writeCMOS(0x3E16, 0x91);
                writeCMOS(0x3E18, 0x91);
                writeCMOS(0x3E1A, 0x87);
                writeCMOS(0x3E1C, 0x78);
                writeCMOS(0x3E1E, 0x50);
                writeCMOS(0x3E20, 0x50);
                writeCMOS(0x3E22, 0x50);
                writeCMOS(0x3E24, 0x87);
                writeCMOS(0x3E26, 0x91);
                writeCMOS(0x3E28, 0x91);
                writeCMOS(0x3E2A, 0x87);
                writeCMOS(0x3E2C, 0x78);
                writeCMOS(0x3E2E, 0x50);
                writeCMOS(0x3E30, 0x50);
                writeCMOS(0x3E32, 0x50);
                writeCMOS(0x3E34, 0x87);
                writeCMOS(0x3E36, 0x91);
                writeCMOS(0x3E38, 0x91);
                writeCMOS(0x3E3A, 0x87);
                writeCMOS(0x3E3C, 0x78);
                writeCMOS(0x3E3E, 0x50);
                writeCMOS(0x3E40, 0x50);
                writeCMOS(0x3E42, 0x50);
                writeCMOS(0x4054, 0x64);
                writeCMOS(0x4148, 0xFE);
                writeCMOS(0x4149, 0x05);
                writeCMOS(0x414A, 0xFF);
                writeCMOS(0x414B, 0x05);
                writeCMOS(0x420A, 0x03);
                writeCMOS(0x4231, 0x08);
                writeCMOS(0x423D, 0x9C);
                writeCMOS(0x4242, 0xB4);
                writeCMOS(0x4246, 0xB4);
                writeCMOS(0x424E, 0xB4);
                writeCMOS(0x425C, 0xB4);
                writeCMOS(0x425E, 0xB6);
                writeCMOS(0x426C, 0xB4);
                writeCMOS(0x426E, 0xB6);
                writeCMOS(0x428C, 0xB4);
                writeCMOS(0x428E, 0xB6);
                writeCMOS(0x4708, 0x00);
                writeCMOS(0x4709, 0x00);
                writeCMOS(0x470A, 0xFF);
                writeCMOS(0x470B, 0x03);
                writeCMOS(0x470C, 0x00);
                writeCMOS(0x470D, 0x00);
                writeCMOS(0x470E, 0xFF);
                writeCMOS(0x470F, 0x03);
                writeCMOS(0x47EB, 0x1C);
                writeCMOS(0x47F0, 0xA6);
                writeCMOS(0x47F2, 0xA6);
                writeCMOS(0x47F4, 0xA0);
                writeCMOS(0x47F6, 0x96);
                writeCMOS(0x4808, 0xA6);
                writeCMOS(0x480A, 0xA6);
                writeCMOS(0x480C, 0xA0);
                writeCMOS(0x480E, 0x96);
                writeCMOS(0x492C, 0xB2);
                writeCMOS(0x4930, 0x03);
                writeCMOS(0x4932, 0x03);
                writeCMOS(0x4936, 0x5B);
                writeCMOS(0x4938, 0x82);
                writeCMOS(0x493C, 0x23);
                writeCMOS(0x493E, 0x23);
                writeCMOS(0x4940, 0x23);
                writeCMOS(0x4BA8, 0x1C);
                writeCMOS(0x4BA9, 0x03);
                writeCMOS(0x4BAC, 0x1C);
                writeCMOS(0x4BAD, 0x1C);
                writeCMOS(0x4BAE, 0x1C);
                writeCMOS(0x4BAF, 0x1C);
                writeCMOS(0x4BB0, 0x1C);
                writeCMOS(0x4BB1, 0x1C);
                writeCMOS(0x4BB2, 0x1C);
                writeCMOS(0x4BB3, 0x1C);
                writeCMOS(0x4BB4, 0x1C);
                writeCMOS(0x4BB8, 0x03);
                writeCMOS(0x4BB9, 0x03);
                writeCMOS(0x4BBA, 0x03);
                writeCMOS(0x4BBB, 0x03);
                writeCMOS(0x4BBC, 0x03);
                writeCMOS(0x4BBD, 0x03);
                writeCMOS(0x4BBE, 0x03);
                writeCMOS(0x4BBF, 0x03);
                writeCMOS(0x4BC0, 0x03);
                writeCMOS(0x4C14, 0x87);
                writeCMOS(0x4C16, 0x91);
                writeCMOS(0x4C18, 0x91);
                writeCMOS(0x4C1A, 0x87);
                writeCMOS(0x4C1C, 0x78);
                writeCMOS(0x4C1E, 0x50);
                writeCMOS(0x4C20, 0x50);
                writeCMOS(0x4C22, 0x50);
                writeCMOS(0x4C24, 0x87);
                writeCMOS(0x4C26, 0x91);
                writeCMOS(0x4C28, 0x91);
                writeCMOS(0x4C2A, 0x87);
                writeCMOS(0x4C2C, 0x78);
                writeCMOS(0x4C2E, 0x50);
                writeCMOS(0x4C30, 0x50);
                writeCMOS(0x4C32, 0x50);
                writeCMOS(0x4C34, 0x87);
                writeCMOS(0x4C36, 0x91);
                writeCMOS(0x4C38, 0x91);
                writeCMOS(0x4C3A, 0x87);
                writeCMOS(0x4C3C, 0x78);
                writeCMOS(0x4C3E, 0x50);
                writeCMOS(0x4C40, 0x50);
                writeCMOS(0x4C42, 0x50);
                writeCMOS(0x4D12, 0x1F);
                writeCMOS(0x4D13, 0x1E);
                writeCMOS(0x4D26, 0x33);
                writeCMOS(0x4E0E, 0x59);
                writeCMOS(0x4E14, 0x55);
                writeCMOS(0x4E16, 0x59);
                writeCMOS(0x4E1E, 0x3B);
                writeCMOS(0x4E20, 0x47);
                writeCMOS(0x4E22, 0x54);
                writeCMOS(0x4E26, 0x81);
                writeCMOS(0x4E2C, 0x7D);
                writeCMOS(0x4E2E, 0x81);
                writeCMOS(0x4E36, 0x63);
                writeCMOS(0x4E38, 0x6F);
                writeCMOS(0x4E3A, 0x7C);
                writeCMOS(0x4F3A, 0x3C);
                writeCMOS(0x4F3C, 0x46);
                writeCMOS(0x4F3E, 0x59);
                writeCMOS(0x4F42, 0x64);
                writeCMOS(0x4F44, 0x6E);
                writeCMOS(0x4F46, 0x81);
                writeCMOS(0x4F4A, 0x82);
                writeCMOS(0x4F5A, 0x81);
                writeCMOS(0x4F62, 0xAA);
                writeCMOS(0x4F72, 0xA9);
                writeCMOS(0x4F78, 0x36);
                writeCMOS(0x4F7A, 0x41);
                writeCMOS(0x4F7C, 0x61);
                writeCMOS(0x4F7D, 0x01);
                writeCMOS(0x4F7E, 0x7C);
                writeCMOS(0x4F7F, 0x01);
                writeCMOS(0x4F80, 0x77);
                writeCMOS(0x4F82, 0x7B);
                writeCMOS(0x4F88, 0x37);
                writeCMOS(0x4F8A, 0x40);
                writeCMOS(0x4F8C, 0x62);
                writeCMOS(0x4F8D, 0x01);
                writeCMOS(0x4F8E, 0x76);
                writeCMOS(0x4F8F, 0x01);
                writeCMOS(0x4F90, 0x5E);
                writeCMOS(0x4F91, 0x02);
                writeCMOS(0x4F92, 0x69);
                writeCMOS(0x4F93, 0x02);
                writeCMOS(0x4F94, 0x89);
                writeCMOS(0x4F95, 0x02);
                writeCMOS(0x4F96, 0xA4);
                writeCMOS(0x4F97, 0x02);
                writeCMOS(0x4F98, 0x9F);
                writeCMOS(0x4F99, 0x02);
                writeCMOS(0x4F9A, 0xA3);
                writeCMOS(0x4F9B, 0x02);
                writeCMOS(0x4FA0, 0x5F);
                writeCMOS(0x4FA1, 0x02);
                writeCMOS(0x4FA2, 0x68);
                writeCMOS(0x4FA3, 0x02);
                writeCMOS(0x4FA4, 0x8A);
                writeCMOS(0x4FA5, 0x02);
                writeCMOS(0x4FA6, 0x9E);
                writeCMOS(0x4FA7, 0x02);
                writeCMOS(0x519E, 0x79);
                writeCMOS(0x51A6, 0xA1);
                writeCMOS(0x51F0, 0xAC);
                writeCMOS(0x51F2, 0xAA);
                writeCMOS(0x51F4, 0xA5);
                writeCMOS(0x51F6, 0xA0);
                writeCMOS(0x5200, 0x9B);
                writeCMOS(0x5202, 0x91);
                writeCMOS(0x5204, 0x87);
                writeCMOS(0x5206, 0x82);
                writeCMOS(0x5208, 0xAC);
                writeCMOS(0x520A, 0xAA);
                writeCMOS(0x520C, 0xA5);
                writeCMOS(0x520E, 0xA0);
                writeCMOS(0x5210, 0x9B);
                writeCMOS(0x5212, 0x91);
                writeCMOS(0x5214, 0x87);
                writeCMOS(0x5216, 0x82);
                writeCMOS(0x5218, 0xAC);
                writeCMOS(0x521A, 0xAA);
                writeCMOS(0x521C, 0xA5);
                writeCMOS(0x521E, 0xA0);
                writeCMOS(0x5220, 0x9B);
                writeCMOS(0x5222, 0x91);
                writeCMOS(0x5224, 0x87);
                writeCMOS(0x5226, 0x82);
                //////
                 * */
            }
            else if (mode == 0x0d)//10bit 594Mbps 
            {
                writeCMOS(0x3015, 0x07);
                writeCMOS(0x3022, 0x00);
                writeCMOS(0x3023, 0x00);
                writeCMOS(0x302C, 0x28);
                writeCMOS(0x302D, 0x05);
                writeCMOS(0x3040, 0x03);
                writeCMOS(0x3050, 0x08);
                writeCMOS(0x30A6, 0x00);
                writeCMOS(0x3460, 0x21);
                writeCMOS(0x3478, 0xA1);
                writeCMOS(0x347C, 0x01);
                writeCMOS(0x3480, 0x01);
                writeCMOS(0x3930, 0x66);
                writeCMOS(0x3A4C, 0x39);
                writeCMOS(0x3A4D, 0x01);
                writeCMOS(0x3A4E, 0x14);
                writeCMOS(0x3A50, 0x48);
                writeCMOS(0x3A51, 0x01);
                writeCMOS(0x3A52, 0x14);
                writeCMOS(0x3A56, 0x00);
                writeCMOS(0x3A5A, 0x00);
                writeCMOS(0x3A5E, 0x00);
                writeCMOS(0x3A62, 0x00);
                writeCMOS(0x3A6A, 0x20);
                writeCMOS(0x3A6C, 0x42);
                writeCMOS(0x3A6E, 0xA0);
                writeCMOS(0x3B2C, 0x0C);
                writeCMOS(0x3B30, 0x1C);
                writeCMOS(0x3B34, 0x0C);
                writeCMOS(0x3B38, 0x1C);
                writeCMOS(0x3BA0, 0x0C);
                writeCMOS(0x3BA4, 0x1C);
                writeCMOS(0x3BA8, 0x0C);
                writeCMOS(0x3BAC, 0x1C);
                writeCMOS(0x3D3C, 0x11);
                writeCMOS(0x3D46, 0x0B);
                writeCMOS(0x3DE0, 0x3F);
                writeCMOS(0x3DE1, 0x08);
                writeCMOS(0x3E10, 0x10);
                writeCMOS(0x3E14, 0x87);
                writeCMOS(0x3E16, 0x91);
                writeCMOS(0x3E18, 0x91);
                writeCMOS(0x3E1A, 0x87);
                writeCMOS(0x3E1C, 0x78);
                writeCMOS(0x3E1E, 0x50);
                writeCMOS(0x3E20, 0x50);
                writeCMOS(0x3E22, 0x50);
                writeCMOS(0x3E24, 0x87);
                writeCMOS(0x3E26, 0x91);
                writeCMOS(0x3E28, 0x91);
                writeCMOS(0x3E2A, 0x87);
                writeCMOS(0x3E2C, 0x78);
                writeCMOS(0x3E2E, 0x50);
                writeCMOS(0x3E30, 0x50);
                writeCMOS(0x3E32, 0x50);
                writeCMOS(0x3E34, 0x87);
                writeCMOS(0x3E36, 0x91);
                writeCMOS(0x3E38, 0x91);
                writeCMOS(0x3E3A, 0x87);
                writeCMOS(0x3E3C, 0x78);
                writeCMOS(0x3E3E, 0x50);
                writeCMOS(0x3E40, 0x50);
                writeCMOS(0x3E42, 0x50);
                writeCMOS(0x4054, 0x64);
                writeCMOS(0x4148, 0xFE);
                writeCMOS(0x4149, 0x05);
                writeCMOS(0x414A, 0xFF);
                writeCMOS(0x414B, 0x05);
                writeCMOS(0x420A, 0x03);
                writeCMOS(0x4231, 0x18);
                writeCMOS(0x423D, 0x9C);
                writeCMOS(0x4242, 0xB4);
                writeCMOS(0x4246, 0xB4);
                writeCMOS(0x424E, 0xB4);
                writeCMOS(0x425C, 0xB4);
                writeCMOS(0x425E, 0xB6);
                writeCMOS(0x426C, 0xB4);
                writeCMOS(0x426E, 0xB6);
                writeCMOS(0x428C, 0xB4);
                writeCMOS(0x428E, 0xB6);
                writeCMOS(0x4708, 0x00);
                writeCMOS(0x4709, 0x00);
                writeCMOS(0x470A, 0xFF);
                writeCMOS(0x470B, 0x03);
                writeCMOS(0x470C, 0x00);
                writeCMOS(0x470D, 0x00);
                writeCMOS(0x470E, 0xFF);
                writeCMOS(0x470F, 0x03);
                writeCMOS(0x47EB, 0x1C);
                writeCMOS(0x47F0, 0xA6);
                writeCMOS(0x47F2, 0xA6);
                writeCMOS(0x47F4, 0xA0);
                writeCMOS(0x47F6, 0x96);
                writeCMOS(0x4808, 0xA6);
                writeCMOS(0x480A, 0xA6);
                writeCMOS(0x480C, 0xA0);
                writeCMOS(0x480E, 0x96);
                writeCMOS(0x492C, 0xB2);
                writeCMOS(0x4930, 0x03);
                writeCMOS(0x4932, 0x03);
                writeCMOS(0x4936, 0x5B);
                writeCMOS(0x4938, 0x82);
                writeCMOS(0x493C, 0x23);
                writeCMOS(0x493E, 0x23);
                writeCMOS(0x4940, 0x23);
                writeCMOS(0x4BA8, 0x1C);
                writeCMOS(0x4BA9, 0x03);
                writeCMOS(0x4BAC, 0x1C);
                writeCMOS(0x4BAD, 0x1C);
                writeCMOS(0x4BAE, 0x1C);
                writeCMOS(0x4BAF, 0x1C);
                writeCMOS(0x4BB0, 0x1C);
                writeCMOS(0x4BB1, 0x1C);
                writeCMOS(0x4BB2, 0x1C);
                writeCMOS(0x4BB3, 0x1C);
                writeCMOS(0x4BB4, 0x1C);
                writeCMOS(0x4BB8, 0x03);
                writeCMOS(0x4BB9, 0x03);
                writeCMOS(0x4BBA, 0x03);
                writeCMOS(0x4BBB, 0x03);
                writeCMOS(0x4BBC, 0x03);
                writeCMOS(0x4BBD, 0x03);
                writeCMOS(0x4BBE, 0x03);
                writeCMOS(0x4BBF, 0x03);
                writeCMOS(0x4BC0, 0x03);
                writeCMOS(0x4C14, 0x87);
                writeCMOS(0x4C16, 0x91);
                writeCMOS(0x4C18, 0x91);
                writeCMOS(0x4C1A, 0x87);
                writeCMOS(0x4C1C, 0x78);
                writeCMOS(0x4C1E, 0x50);
                writeCMOS(0x4C20, 0x50);
                writeCMOS(0x4C22, 0x50);
                writeCMOS(0x4C24, 0x87);
                writeCMOS(0x4C26, 0x91);
                writeCMOS(0x4C28, 0x91);
                writeCMOS(0x4C2A, 0x87);
                writeCMOS(0x4C2C, 0x78);
                writeCMOS(0x4C2E, 0x50);
                writeCMOS(0x4C30, 0x50);
                writeCMOS(0x4C32, 0x50);
                writeCMOS(0x4C34, 0x87);
                writeCMOS(0x4C36, 0x91);
                writeCMOS(0x4C38, 0x91);
                writeCMOS(0x4C3A, 0x87);
                writeCMOS(0x4C3C, 0x78);
                writeCMOS(0x4C3E, 0x50);
                writeCMOS(0x4C40, 0x50);
                writeCMOS(0x4C42, 0x50);
                writeCMOS(0x4D12, 0x1F);
                writeCMOS(0x4D13, 0x1E);
                writeCMOS(0x4D26, 0x33);
                writeCMOS(0x4E0E, 0x59);
                writeCMOS(0x4E14, 0x55);
                writeCMOS(0x4E16, 0x59);
                writeCMOS(0x4E1E, 0x3B);
                writeCMOS(0x4E20, 0x47);
                writeCMOS(0x4E22, 0x54);
                writeCMOS(0x4E26, 0x81);
                writeCMOS(0x4E2C, 0x7D);
                writeCMOS(0x4E2E, 0x81);
                writeCMOS(0x4E36, 0x63);
                writeCMOS(0x4E38, 0x6F);
                writeCMOS(0x4E3A, 0x7C);
                writeCMOS(0x4F3A, 0x3C);
                writeCMOS(0x4F3C, 0x46);
                writeCMOS(0x4F3E, 0x59);
                writeCMOS(0x4F42, 0x64);
                writeCMOS(0x4F44, 0x6E);
                writeCMOS(0x4F46, 0x81);
                writeCMOS(0x4F4A, 0x82);
                writeCMOS(0x4F5A, 0x81);
                writeCMOS(0x4F62, 0xAA);
                writeCMOS(0x4F72, 0xA9);
                writeCMOS(0x4F78, 0x36);
                writeCMOS(0x4F7A, 0x41);
                writeCMOS(0x4F7C, 0x61);
                writeCMOS(0x4F7D, 0x01);
                writeCMOS(0x4F7E, 0x7C);
                writeCMOS(0x4F7F, 0x01);
                writeCMOS(0x4F80, 0x77);
                writeCMOS(0x4F82, 0x7B);
                writeCMOS(0x4F88, 0x37);
                writeCMOS(0x4F8A, 0x40);
                writeCMOS(0x4F8C, 0x62);
                writeCMOS(0x4F8D, 0x01);
                writeCMOS(0x4F8E, 0x76);
                writeCMOS(0x4F8F, 0x01);
                writeCMOS(0x4F90, 0x5E);
                writeCMOS(0x4F91, 0x02);
                writeCMOS(0x4F92, 0x69);
                writeCMOS(0x4F93, 0x02);
                writeCMOS(0x4F94, 0x89);
                writeCMOS(0x4F95, 0x02);
                writeCMOS(0x4F96, 0xA4);
                writeCMOS(0x4F97, 0x02);
                writeCMOS(0x4F98, 0x9F);
                writeCMOS(0x4F99, 0x02);
                writeCMOS(0x4F9A, 0xA3);
                writeCMOS(0x4F9B, 0x02);
                writeCMOS(0x4FA0, 0x5F);
                writeCMOS(0x4FA1, 0x02);
                writeCMOS(0x4FA2, 0x68);
                writeCMOS(0x4FA3, 0x02);
                writeCMOS(0x4FA4, 0x8A);
                writeCMOS(0x4FA5, 0x02);
                writeCMOS(0x4FA6, 0x9E);
                writeCMOS(0x4FA7, 0x02);
                writeCMOS(0x519E, 0x79);
                writeCMOS(0x51A6, 0xA1);
                writeCMOS(0x51F0, 0xAC);
                writeCMOS(0x51F2, 0xAA);
                writeCMOS(0x51F4, 0xA5);
                writeCMOS(0x51F6, 0xA0);
                writeCMOS(0x5200, 0x9B);
                writeCMOS(0x5202, 0x91);
                writeCMOS(0x5204, 0x87);
                writeCMOS(0x5206, 0x82);
                writeCMOS(0x5208, 0xAC);
                writeCMOS(0x520A, 0xAA);
                writeCMOS(0x520C, 0xA5);
                writeCMOS(0x520E, 0xA0);
                writeCMOS(0x5210, 0x9B);
                writeCMOS(0x5212, 0x91);
                writeCMOS(0x5214, 0x87);
                writeCMOS(0x5216, 0x82);
                writeCMOS(0x5218, 0xAC);
                writeCMOS(0x521A, 0xAA);
                writeCMOS(0x521C, 0xA5);
                writeCMOS(0x521E, 0xA0);
                writeCMOS(0x5220, 0x9B);
                writeCMOS(0x5222, 0x91);
                writeCMOS(0x5224, 0x87);
                writeCMOS(0x5226, 0x82);
            }
            else
            {

                writeCMOS(0x3015, 0x03);
                writeCMOS(0x302C, 0x94);
                writeCMOS(0x3040, 0x03);
                writeCMOS(0x3050, 0x08);
                writeCMOS(0x30A6, 0x00);
                writeCMOS(0x3460, 0x21);
                writeCMOS(0x3478, 0xA1);
                writeCMOS(0x347C, 0x01);
                writeCMOS(0x3480, 0x01);
                writeCMOS(0x3930, 0x0C);
                writeCMOS(0x3931, 0x01);
                writeCMOS(0x3A4C, 0x39);
                writeCMOS(0x3A4D, 0x01);
                writeCMOS(0x3A4E, 0x14);
                writeCMOS(0x3A50, 0x48);
                writeCMOS(0x3A51, 0x01);
                writeCMOS(0x3A52, 0x14);
                writeCMOS(0x3A56, 0x00);
                writeCMOS(0x3A5A, 0x00);
                writeCMOS(0x3A5E, 0x00);
                writeCMOS(0x3A62, 0x00);
                writeCMOS(0x3A6A, 0x20);
                writeCMOS(0x3A6C, 0x42);
                writeCMOS(0x3A6E, 0xA0);
                writeCMOS(0x3B2C, 0x0C);
                writeCMOS(0x3B30, 0x1C);
                writeCMOS(0x3B34, 0x0C);
                writeCMOS(0x3B38, 0x1C);
                writeCMOS(0x3BA0, 0x0C);
                writeCMOS(0x3BA4, 0x1C);
                writeCMOS(0x3BA8, 0x0C);
                writeCMOS(0x3BAC, 0x1C);
                writeCMOS(0x3D3C, 0x11);
                writeCMOS(0x3D46, 0x0B);
                writeCMOS(0x3DE0, 0x3F);
                writeCMOS(0x3DE1, 0x08);
                writeCMOS(0x3E10, 0x10);
                writeCMOS(0x3E14, 0x87);
                writeCMOS(0x3E16, 0x91);
                writeCMOS(0x3E18, 0x91);
                writeCMOS(0x3E1A, 0x87);
                writeCMOS(0x3E1C, 0x78);
                writeCMOS(0x3E1E, 0x50);
                writeCMOS(0x3E20, 0x50);
                writeCMOS(0x3E22, 0x50);
                writeCMOS(0x3E24, 0x87);
                writeCMOS(0x3E26, 0x91);
                writeCMOS(0x3E28, 0x91);
                writeCMOS(0x3E2A, 0x87);
                writeCMOS(0x3E2C, 0x78);
                writeCMOS(0x3E2E, 0x50);
                writeCMOS(0x3E30, 0x50);
                writeCMOS(0x3E32, 0x50);
                writeCMOS(0x3E34, 0x87);
                writeCMOS(0x3E36, 0x91);
                writeCMOS(0x3E38, 0x91);
                writeCMOS(0x3E3A, 0x87);
                writeCMOS(0x3E3C, 0x78);
                writeCMOS(0x3E3E, 0x50);
                writeCMOS(0x3E40, 0x50);
                writeCMOS(0x3E42, 0x50);
                writeCMOS(0x4054, 0x64);
                writeCMOS(0x4148, 0xFE);
                writeCMOS(0x4149, 0x05);
                writeCMOS(0x414A, 0xFF);
                writeCMOS(0x414B, 0x05);
                writeCMOS(0x420A, 0x03);
                writeCMOS(0x4231, 0x08);
                writeCMOS(0x423D, 0x9C);
                writeCMOS(0x4242, 0xB4);
                writeCMOS(0x4246, 0xB4);
                writeCMOS(0x424E, 0xB4);
                writeCMOS(0x425C, 0xB4);
                writeCMOS(0x425E, 0xB6);
                writeCMOS(0x426C, 0xB4);
                writeCMOS(0x426E, 0xB6);
                writeCMOS(0x428C, 0xB4);
                writeCMOS(0x428E, 0xB6);
                writeCMOS(0x4708, 0x00);
                writeCMOS(0x4709, 0x00);
                writeCMOS(0x470A, 0xFF);
                writeCMOS(0x470B, 0x03);
                writeCMOS(0x470C, 0x00);
                writeCMOS(0x470D, 0x00);
                writeCMOS(0x470E, 0xFF);
                writeCMOS(0x470F, 0x03);
                writeCMOS(0x47EB, 0x1C);
                writeCMOS(0x47F0, 0xA6);
                writeCMOS(0x47F2, 0xA6);
                writeCMOS(0x47F4, 0xA0);
                writeCMOS(0x47F6, 0x96);
                writeCMOS(0x4808, 0xA6);
                writeCMOS(0x480A, 0xA6);
                writeCMOS(0x480C, 0xA0);
                writeCMOS(0x480E, 0x96);
                writeCMOS(0x492C, 0xB2);
                writeCMOS(0x4930, 0x03);
                writeCMOS(0x4932, 0x03);
                writeCMOS(0x4936, 0x5B);
                writeCMOS(0x4938, 0x82);
                writeCMOS(0x493C, 0x23);
                writeCMOS(0x493E, 0x23);
                writeCMOS(0x4940, 0x23);
                writeCMOS(0x4BA8, 0x1C);
                writeCMOS(0x4BA9, 0x03);
                writeCMOS(0x4BAC, 0x1C);
                writeCMOS(0x4BAD, 0x1C);
                writeCMOS(0x4BAE, 0x1C);
                writeCMOS(0x4BAF, 0x1C);
                writeCMOS(0x4BB0, 0x1C);
                writeCMOS(0x4BB1, 0x1C);
                writeCMOS(0x4BB2, 0x1C);
                writeCMOS(0x4BB3, 0x1C);
                writeCMOS(0x4BB4, 0x1C);
                writeCMOS(0x4BB8, 0x03);
                writeCMOS(0x4BB9, 0x03);
                writeCMOS(0x4BBA, 0x03);
                writeCMOS(0x4BBB, 0x03);
                writeCMOS(0x4BBC, 0x03);
                writeCMOS(0x4BBD, 0x03);
                writeCMOS(0x4BBE, 0x03);
                writeCMOS(0x4BBF, 0x03);
                writeCMOS(0x4BC0, 0x03);
                writeCMOS(0x4C14, 0x87);
                writeCMOS(0x4C16, 0x91);
                writeCMOS(0x4C18, 0x91);
                writeCMOS(0x4C1A, 0x87);
                writeCMOS(0x4C1C, 0x78);
                writeCMOS(0x4C1E, 0x50);
                writeCMOS(0x4C20, 0x50);
                writeCMOS(0x4C22, 0x50);
                writeCMOS(0x4C24, 0x87);
                writeCMOS(0x4C26, 0x91);
                writeCMOS(0x4C28, 0x91);
                writeCMOS(0x4C2A, 0x87);
                writeCMOS(0x4C2C, 0x78);
                writeCMOS(0x4C2E, 0x50);
                writeCMOS(0x4C30, 0x50);
                writeCMOS(0x4C32, 0x50);
                writeCMOS(0x4C34, 0x87);
                writeCMOS(0x4C36, 0x91);
                writeCMOS(0x4C38, 0x91);
                writeCMOS(0x4C3A, 0x87);
                writeCMOS(0x4C3C, 0x78);
                writeCMOS(0x4C3E, 0x50);
                writeCMOS(0x4C40, 0x50);
                writeCMOS(0x4C42, 0x50);
                writeCMOS(0x4D12, 0x1F);
                writeCMOS(0x4D13, 0x1E);
                writeCMOS(0x4D26, 0x33);
                writeCMOS(0x4E0E, 0x59);
                writeCMOS(0x4E14, 0x55);
                writeCMOS(0x4E16, 0x59);
                writeCMOS(0x4E1E, 0x3B);
                writeCMOS(0x4E20, 0x47);
                writeCMOS(0x4E22, 0x54);
                writeCMOS(0x4E26, 0x81);
                writeCMOS(0x4E2C, 0x7D);
                writeCMOS(0x4E2E, 0x81);
                writeCMOS(0x4E36, 0x63);
                writeCMOS(0x4E38, 0x6F);
                writeCMOS(0x4E3A, 0x7C);
                writeCMOS(0x4F3A, 0x3C);
                writeCMOS(0x4F3C, 0x46);
                writeCMOS(0x4F3E, 0x59);
                writeCMOS(0x4F42, 0x64);
                writeCMOS(0x4F44, 0x6E);
                writeCMOS(0x4F46, 0x81);
                writeCMOS(0x4F4A, 0x82);
                writeCMOS(0x4F5A, 0x81);
                writeCMOS(0x4F62, 0xAA);
                writeCMOS(0x4F72, 0xA9);
                writeCMOS(0x4F78, 0x36);
                writeCMOS(0x4F7A, 0x41);
                writeCMOS(0x4F7C, 0x61);
                writeCMOS(0x4F7D, 0x01);
                writeCMOS(0x4F7E, 0x7C);
                writeCMOS(0x4F7F, 0x01);
                writeCMOS(0x4F80, 0x77);
                writeCMOS(0x4F82, 0x7B);
                writeCMOS(0x4F88, 0x37);
                writeCMOS(0x4F8A, 0x40);
                writeCMOS(0x4F8C, 0x62);
                writeCMOS(0x4F8D, 0x01);
                writeCMOS(0x4F8E, 0x76);
                writeCMOS(0x4F8F, 0x01);
                writeCMOS(0x4F90, 0x5E);
                writeCMOS(0x4F91, 0x02);
                writeCMOS(0x4F92, 0x69);
                writeCMOS(0x4F93, 0x02);
                writeCMOS(0x4F94, 0x89);
                writeCMOS(0x4F95, 0x02);
                writeCMOS(0x4F96, 0xA4);
                writeCMOS(0x4F97, 0x02);
                writeCMOS(0x4F98, 0x9F);
                writeCMOS(0x4F99, 0x02);
                writeCMOS(0x4F9A, 0xA3);
                writeCMOS(0x4F9B, 0x02);
                writeCMOS(0x4FA0, 0x5F);
                writeCMOS(0x4FA1, 0x02);
                writeCMOS(0x4FA2, 0x68);
                writeCMOS(0x4FA3, 0x02);
                writeCMOS(0x4FA4, 0x8A);
                writeCMOS(0x4FA5, 0x02);
                writeCMOS(0x4FA6, 0x9E);
                writeCMOS(0x4FA7, 0x02);
                writeCMOS(0x519E, 0x79);
                writeCMOS(0x51A6, 0xA1);
                writeCMOS(0x51F0, 0xAC);
                writeCMOS(0x51F2, 0xAA);
                writeCMOS(0x51F4, 0xA5);
                writeCMOS(0x51F6, 0xA0);
                writeCMOS(0x5200, 0x9B);
                writeCMOS(0x5202, 0x91);
                writeCMOS(0x5204, 0x87);
                writeCMOS(0x5206, 0x82);
                writeCMOS(0x5208, 0xAC);
                writeCMOS(0x520A, 0xAA);
                writeCMOS(0x520C, 0xA5);
                writeCMOS(0x520E, 0xA0);
                writeCMOS(0x5210, 0x9B);
                writeCMOS(0x5212, 0x91);
                writeCMOS(0x5214, 0x87);
                writeCMOS(0x5216, 0x82);
                writeCMOS(0x5218, 0xAC);
                writeCMOS(0x521A, 0xAA);
                writeCMOS(0x521C, 0xA5);
                writeCMOS(0x521E, 0xA0);
                writeCMOS(0x5220, 0x9B);
                writeCMOS(0x5222, 0x91);
                writeCMOS(0x5224, 0x87);
                writeCMOS(0x5226, 0x82);

            }

            writeCMOS(0x3000, 0x00);

            if (masterslave == 0x00)//master
            {
                //master mode 
                writeCMOS(0x3002, 0x00);
                writeCMOS(0x30a4, 0xaa);
                writeCMOS(0x30a6, 0x00);
                writeCMOS(0x3002, 0x00);
            }
            else
            {
                //SLAVE MODE 
                writeCMOS(0x30A6, 0x0F);
            }




            setIDLE();

            //hmax = 224;
            //vmax = 2250;

            setHMAX(270);//224
            setVMAX(2220);//2250

            releaseIDLE();
        }



        void PCA953XWrite(ushort value)
        {

            byte[] xdata = new byte[10];
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0x9A, value, value, 1, xdata);

        }

        void initCMOS_IMX530()
        {
            writeFPGA(0, 0);
            Thread.Sleep(100);
            writeFPGA(0, 1);

            // writeFPGA( 0x0, 0x0 );
            // writeFPGA( 0x0, 0x1 );
            Thread.Sleep(200);
            //    CyU3PThreadSleep(100);

            /*
        Standby Cancel Sequence
        After the power-on sequence is performed, this sensor is in standby mode. Follow the sequence below
        to cancel standby and start normal operation. Also perform the same sequence after shifting from
        normal operation to standby, and you want to go back to normal operation later.

        1.After 10s or more from XCLR=H, perform the following register settings.
        1-1. Set address 019Eh to 01h (PWR_AUTO=1)
        1-2. (Option) Set half rate setting when baud rate of 1.152 Gbps is used.
        1-3. Set address 0000h to 04h (STANDBY=0 and WAKEUP=1)
        1-4. Set initial settings.
        1-5. Set mode settings.
        */

            //1-1. Set address 019Eh to 01h (PWR_AUTO=1)
            //1-2. (Option) Set half rate setting when baud rate of 1.152 Gbps is used.
            //1-3. Set address 0000h to 04h (STANDBY=0 and WAKEUP=1)
            //    writeCMOS( 0x019E, 0x01 );
            //writeCMOS(0x0133, 0x8D);		//half rate
            //writeCMOS(0x0368, 0xE1);		//half rate
            //    writeCMOS( 0x0000, 0x04 );



            //1-4. Set initial settings.
            writeCMOS(0x0200, 0x01);
            writeCMOS(0x0210, 0x01);

            writeCMOS(0x0214, 0x0a);
            writeCMOS(0x0215, 0x22);
            writeCMOS(0x0216, 0xb1);
            writeCMOS(0x0218, 0x3f);
            writeCMOS(0x0219, 0x04);
            writeCMOS(0x021B, 0x3a);

            writeCMOS(0x02D0, 0xD0);
            writeCMOS(0x02D1, 0x14);
            writeCMOS(0x02D2, 0xD0);
            writeCMOS(0x02D3, 0x14);

            writeCMOS(0x02D4, 0xa8);
            writeCMOS(0x02D5, 0x12);
            writeCMOS(0x02D6, 0x00);
            writeCMOS(0x02D8, 0x62);
            writeCMOS(0x02D9, 0x01);
            writeCMOS(0x02DC, 0x01);
            writeCMOS(0x02E2, 0x08);
            writeCMOS(0x02E3, 0x28);
            writeCMOS(0x02E6, 0x14);

            writeCMOS(0x0400, 0x14);
            writeCMOS(0x041C, 0x80);
            writeCMOS(0x041E, 0xe0);
            writeCMOS(0x041F, 0x00);
            writeCMOS(0x0420, 0x80);
            writeCMOS(0x0422, 0xe0);
            writeCMOS(0x0423, 0x00);
            writeCMOS(0x0426, 0x20);
            writeCMOS(0x0427, 0xd0);
            writeCMOS(0x0433, 0x50);  //

            //shs
            writeCMOS(0x0440, 0x48);
            writeCMOS(0x0441, 0x00);
            writeCMOS(0x0442, 0x00);

            writeCMOS(0x04AC, 0x4B);

            writeCMOS(0x0508, 0x4A);
            writeCMOS(0x0544, 0x0F);
            writeCMOS(0x0545, 0x00);
            writeCMOS(0x0546, 0x14);
            writeCMOS(0x0548, 0x0F);
            writeCMOS(0x0549, 0x00);
            writeCMOS(0x054A, 0x14);
            writeCMOS(0x054C, 0x0F);
            writeCMOS(0x054D, 0x00);
            writeCMOS(0x054E, 0x14);
            writeCMOS(0x0564, 0x0F);
            writeCMOS(0x0565, 0x00);
            writeCMOS(0x0566, 0x14);
            writeCMOS(0x0568, 0x0F);
            writeCMOS(0x0569, 0x00);
            writeCMOS(0x056A, 0x14);
            writeCMOS(0x056C, 0x0F);
            writeCMOS(0x056D, 0x00);
            writeCMOS(0x056E, 0x14);

            writeCMOS(0x0630, 0x01);

            //gain_rts
            writeCMOS(0x0702, 0x08);

            writeCMOS(0x0714, 0x00);
            writeCMOS(0x0715, 0x00);
            writeCMOS(0x0735, 0x00);
            writeCMOS(0x0742, 0x27);
            writeCMOS(0x074A, 0x20);
            writeCMOS(0x07A4, 0x96);
            writeCMOS(0x07A8, 0x96);
            writeCMOS(0x07B4, 0xF0);
            writeCMOS(0x07BE, 0x62);
            writeCMOS(0x07EC, 0x96);
            writeCMOS(0x07F0, 0xFB);
            writeCMOS(0x07F1, 0x0B);
            writeCMOS(0x07F2, 0xFB);
            writeCMOS(0x07F3, 0x0B);

            writeCMOS(0x0800, 0x55);
            writeCMOS(0x080C, 0x0D);
            writeCMOS(0x080D, 0x0F);
            writeCMOS(0x0810, 0xFF);
            writeCMOS(0x0812, 0x00);
            writeCMOS(0x0830, 0x5E);
            writeCMOS(0x0832, 0x17);
            writeCMOS(0x0834, 0xE0);
            writeCMOS(0x0835, 0x01);
            writeCMOS(0x0836, 0xDC);
            writeCMOS(0x0837, 0x1F);
            writeCMOS(0x0838, 0x76);
            writeCMOS(0x0839, 0x02);
            writeCMOS(0x083C, 0x1C);
            writeCMOS(0x083E, 0xEB);
            writeCMOS(0x083F, 0x1F);
            writeCMOS(0x084C, 0x00);
            writeCMOS(0x084E, 0x00);
            writeCMOS(0x084F, 0x00);
            writeCMOS(0x0858, 0x5E);
            writeCMOS(0x085A, 0x17);
            writeCMOS(0x085C, 0xE0);
            writeCMOS(0x085D, 0x01);
            writeCMOS(0x085E, 0xEE);
            writeCMOS(0x085F, 0x1F);
            writeCMOS(0x0860, 0x4F);
            writeCMOS(0x0861, 0x02);
            writeCMOS(0x0862, 0x8F);
            writeCMOS(0x0864, 0x1C);
            writeCMOS(0x0866, 0xEB);
            writeCMOS(0x0867, 0x1F);
            writeCMOS(0x0868, 0xCC);
            writeCMOS(0x086A, 0xFA);
            writeCMOS(0x086B, 0x1F);
            writeCMOS(0x086C, 0xF5);
            writeCMOS(0x086E, 0xF5);
            writeCMOS(0x086F, 0x1F);
            writeCMOS(0x0870, 0x33);
            writeCMOS(0x0871, 0x02);
            writeCMOS(0x0872, 0xF5);
            writeCMOS(0x0873, 0x1F);
            writeCMOS(0x0874, 0x00);
            writeCMOS(0x0876, 0x00);
            writeCMOS(0x0877, 0x00);
            writeCMOS(0x0880, 0x5E);
            writeCMOS(0x0882, 0x17);
            writeCMOS(0x0884, 0xE0);
            writeCMOS(0x0885, 0x01);
            writeCMOS(0x0886, 0xDC);
            writeCMOS(0x0888, 0x50);
            writeCMOS(0x0889, 0x02);
            writeCMOS(0x088A, 0xE5);
            writeCMOS(0x088B, 0x1F);
            writeCMOS(0x088C, 0x1C);
            writeCMOS(0x088E, 0xEB);
            writeCMOS(0x088F, 0x1F);
            writeCMOS(0x089C, 0x00);
            writeCMOS(0x089E, 0x00);
            writeCMOS(0x089F, 0x00);
            writeCMOS(0x08E8, 0x17);
            writeCMOS(0x0b04, 0x00);
            writeCMOS(0x0b52, 0x08);
            writeCMOS(0x0b53, 0x08);
            writeCMOS(0x0c0C, 0x0B);
            writeCMOS(0x0c00, 0xc1);
            writeCMOS(0x102E, 0x3B);
            writeCMOS(0x1030, 0x85);
            writeCMOS(0x106E, 0x3B);
            writeCMOS(0x1070, 0x6E);
            writeCMOS(0x108E, 0xA3);
            writeCMOS(0x1090, 0xDB);
            writeCMOS(0x1096, 0x01);
            writeCMOS(0x1098, 0x37);
            writeCMOS(0x109E, 0x71);
            writeCMOS(0x10A0, 0x82);
            writeCMOS(0x10BE, 0xDC);
            writeCMOS(0x113C, 0x2E);
            writeCMOS(0x1144, 0xC8);
            writeCMOS(0x1184, 0xC5);
            writeCMOS(0x11A2, 0x1C);
            writeCMOS(0x11A4, 0x5B);
            writeCMOS(0x11A6, 0xA3);
            writeCMOS(0x1246, 0xA3);
            writeCMOS(0x1298, 0x39);
            writeCMOS(0x12B6, 0x1C);
            writeCMOS(0x12B8, 0x6F);
            writeCMOS(0x12C8, 0x2C);
            writeCMOS(0x12C9, 0x01);
            writeCMOS(0x12CA, 0xC8);
            writeCMOS(0x12CB, 0x01);
            writeCMOS(0x12D8, 0x2C);
            writeCMOS(0x12D9, 0x01);
            writeCMOS(0x12DA, 0xC8);
            writeCMOS(0x12DB, 0x01);
            writeCMOS(0x137A, 0xA2);
            writeCMOS(0x1382, 0x00);
            writeCMOS(0x1384, 0x38);
            writeCMOS(0x13A4, 0x2F);
            writeCMOS(0x13BA, 0x1B);
            writeCMOS(0x13BC, 0x5C);
            writeCMOS(0x13BE, 0xA2);
            writeCMOS(0x13EA, 0xA2);
            writeCMOS(0x140E, 0x1B);
            writeCMOS(0x1410, 0x70);
            writeCMOS(0x1432, 0x0C);
            writeCMOS(0x1435, 0x21);
            writeCMOS(0x1437, 0x0C);
            writeCMOS(0x14E4, 0xFF);
            writeCMOS(0x14E5, 0x0F);
            writeCMOS(0x14E6, 0x00);
            writeCMOS(0x14E7, 0x00);
            writeCMOS(0x14F6, 0x89);
            writeCMOS(0x14F7, 0x02);
            writeCMOS(0x1506, 0x00);
            writeCMOS(0x1507, 0x00);
            writeCMOS(0x1508, 0x00);
            writeCMOS(0x1509, 0x00);
            writeCMOS(0x1510, 0x04);
            writeCMOS(0x1511, 0x04);
            writeCMOS(0x1512, 0x04);
            writeCMOS(0x1513, 0x04);
            writeCMOS(0x151E, 0x14);
            writeCMOS(0x151F, 0x14);
            writeCMOS(0x153C, 0x8A);
            writeCMOS(0x153D, 0x02);
            writeCMOS(0x153E, 0xE8);
            writeCMOS(0x153F, 0x05);
            writeCMOS(0x1540, 0x9E);
            writeCMOS(0x1541, 0x0C);
            writeCMOS(0x1570, 0x2C);
            writeCMOS(0x1571, 0x01);
            writeCMOS(0x1572, 0xC8);
            writeCMOS(0x1573, 0x01);
            writeCMOS(0x1580, 0x2C);
            writeCMOS(0x1581, 0x01);
            writeCMOS(0x1582, 0xC8);
            writeCMOS(0x1583, 0x01);
            writeCMOS(0x165E, 0x2C);
            writeCMOS(0x165F, 0x01);
            writeCMOS(0x1660, 0xC8);
            writeCMOS(0x1661, 0x01);
            writeCMOS(0x1667, 0x83);
            writeCMOS(0x1668, 0xCC);
            writeCMOS(0x1669, 0x02);
            writeCMOS(0x166A, 0x58);
            writeCMOS(0x166E, 0x5F);
            writeCMOS(0x1672, 0x64);
            writeCMOS(0x1676, 0xC6);
            writeCMOS(0x167A, 0xC6);
            writeCMOS(0x167E, 0xC6);
            writeCMOS(0x1682, 0xC6);
            writeCMOS(0x1686, 0xC6);
            writeCMOS(0x168A, 0x58);
            writeCMOS(0x168E, 0x5F);
            writeCMOS(0x1692, 0x64);
            writeCMOS(0x1696, 0xC6);
            writeCMOS(0x169A, 0xC6);
            writeCMOS(0x169E, 0xC6);
            writeCMOS(0x16A2, 0xC6);
            writeCMOS(0x16A6, 0xC6);
            writeCMOS(0x16EC, 0x4B);
            writeCMOS(0x16F0, 0x52);
            writeCMOS(0x16F4, 0x57);
            writeCMOS(0x16F8, 0xB9);
            writeCMOS(0x1700, 0xB9);
            writeCMOS(0x1704, 0xB9);
            writeCMOS(0x1708, 0xB9);
            writeCMOS(0x170C, 0xB9);
            writeCMOS(0x1710, 0x4B);
            writeCMOS(0x1714, 0x52);
            writeCMOS(0x1718, 0x57);
            writeCMOS(0x171C, 0xB9);
            writeCMOS(0x1720, 0xB9);
            writeCMOS(0x1724, 0xB9);
            writeCMOS(0x1728, 0xB9);
            writeCMOS(0x172C, 0xB9);
            writeCMOS(0x1776, 0xF2);
            writeCMOS(0x1778, 0xDE);
            writeCMOS(0x177A, 0x30);
            writeCMOS(0x177C, 0x1C);
            writeCMOS(0x177D, 0x02);
            writeCMOS(0x177E, 0xE0);
            writeCMOS(0x1780, 0xF0);
            writeCMOS(0x1782, 0x1E);
            writeCMOS(0x1784, 0x2E);
            writeCMOS(0x1788, 0xF4);
            writeCMOS(0x178C, 0x32);
            writeCMOS(0x1798, 0xE3);
            writeCMOS(0x17AE, 0x0E);
            writeCMOS(0x17AF, 0x00);
            writeCMOS(0x17B0, 0x8D);
            writeCMOS(0x17B1, 0x02);
            writeCMOS(0x17B6, 0x0E);
            writeCMOS(0x17B7, 0x00);
            writeCMOS(0x17B8, 0x8D);
            writeCMOS(0x17B9, 0x02);
            writeCMOS(0x17CE, 0x18);
            writeCMOS(0x17CF, 0x00);
            writeCMOS(0x17D0, 0x8D);
            writeCMOS(0x17D1, 0x02);
            writeCMOS(0x17D6, 0x18);
            writeCMOS(0x17D7, 0x00);
            writeCMOS(0x17D8, 0x8D);
            writeCMOS(0x17D9, 0x02);
            writeCMOS(0x17E6, 0x53);
            writeCMOS(0x17F0, 0x90);
            writeCMOS(0x17F2, 0x8A);
            writeCMOS(0x17F8, 0x8E);
            writeCMOS(0x17FA, 0x90);
            writeCMOS(0x1804, 0x8E);
            writeCMOS(0x1806, 0x90);
            writeCMOS(0x180C, 0x8A);
            writeCMOS(0x180E, 0xBB);
            writeCMOS(0x1814, 0x90);
            writeCMOS(0x1816, 0x8A);
            writeCMOS(0x1834, 0x4A);
            writeCMOS(0x1836, 0x90);
            writeCMOS(0x183C, 0x4C);
            writeCMOS(0x183E, 0x92);
            writeCMOS(0x1844, 0x4E);
            writeCMOS(0x1846, 0x94);
            writeCMOS(0x184C, 0x47);
            writeCMOS(0x184E, 0x4D);
            writeCMOS(0x1854, 0x49);
            writeCMOS(0x1856, 0x50);
            writeCMOS(0x185C, 0x4B);
            writeCMOS(0x185E, 0x52);
            writeCMOS(0x186A, 0x9E);
            writeCMOS(0x1870, 0x98);
            writeCMOS(0x1876, 0x96);
            writeCMOS(0x1878, 0xBA);
            writeCMOS(0x1898, 0x93);
            writeCMOS(0x189A, 0xB9);
            writeCMOS(0x18A0, 0x67);
            writeCMOS(0x18A2, 0x8D);
            writeCMOS(0x18F6, 0xE1);
            writeCMOS(0x1908, 0xE5);
            writeCMOS(0x190A, 0xE1);
            writeCMOS(0x1910, 0x23);
            writeCMOS(0x1912, 0x1F);
            writeCMOS(0x1918, 0x6D);
            writeCMOS(0x1922, 0x6D);
            writeCMOS(0x1928, 0x34);
            writeCMOS(0x1929, 0x0E);
            writeCMOS(0x192A, 0x8A);
            writeCMOS(0x192B, 0x0E);
            writeCMOS(0x192C, 0xE1);
            writeCMOS(0x192D, 0x0E);
            writeCMOS(0x192E, 0x06);
            writeCMOS(0x1930, 0x04);
            writeCMOS(0x1931, 0x04);
            writeCMOS(0x193C, 0x06);
            writeCMOS(0x193E, 0x04);
            writeCMOS(0x193F, 0x04);
            writeCMOS(0x1944, 0x20);
            writeCMOS(0x1945, 0x0A);
            writeCMOS(0x1946, 0xF4);
            writeCMOS(0x1947, 0x0C);
            writeCMOS(0x1948, 0x3A);
            writeCMOS(0x1949, 0x9E);
            writeCMOS(0x194A, 0xCA);
            writeCMOS(0x194B, 0x0E);
            writeCMOS(0x1953, 0x90);
            writeCMOS(0x1954, 0xCA);
            writeCMOS(0x1955, 0x0E);
            writeCMOS(0x195A, 0x53);
            writeCMOS(0x195B, 0x0A);
            writeCMOS(0x195C, 0xFB);
            writeCMOS(0x195D, 0xFB);
            writeCMOS(0x1964, 0xDC);
            writeCMOS(0x1965, 0xFE);
            writeCMOS(0x1968, 0xDC);
            writeCMOS(0x1969, 0xFE);
            writeCMOS(0x1970, 0x6D);
            writeCMOS(0x1976, 0x6D);
            writeCMOS(0x1980, 0x1F);
            writeCMOS(0x1987, 0x8F);
            writeCMOS(0x1988, 0x04);
            writeCMOS(0x19A3, 0x0C);
            writeCMOS(0x19A4, 0x80);
            writeCMOS(0x19D5, 0x3F);
            writeCMOS(0x19F7, 0xF2);
            writeCMOS(0x1a64, 0x20);
            writeCMOS(0x1a68, 0x21);
            writeCMOS(0x1a6C, 0x22);
            writeCMOS(0x1a74, 0x20);
            writeCMOS(0x1a78, 0x21);
            writeCMOS(0x1a7C, 0x22);
            writeCMOS(0x1aA4, 0x83);
            writeCMOS(0x1aA8, 0x84);
            writeCMOS(0x1aAC, 0x85);
            writeCMOS(0x1aB4, 0x83);
            writeCMOS(0x1aB8, 0x84);
            writeCMOS(0x1aBC, 0x85);

            writeCMOS(0x0200, 0x00);
            //    writeCMOS(0x0c00, 0xC0);
            ushort hmax;
            ushort vmax;
            setIDLE();
            hmax = 5328;
            vmax = 4772;
            setHMAX(hmax);
            setVMAX(vmax);
            releaseIDLE();


        }

        void initCMOS_IMX487(UInt32 mode)  //PFY
        {
            writeFPGA(0, 0);
            Thread.Sleep(100);
            writeFPGA(0, 1);

            Thread.Sleep(200);

            //1-4. Set initial settings.
            writeCMOS(0x0200, 0x01);//0工作1待机 
            writeCMOS(0x0210, 0x01);//0主机1从机	 
            //
            if (mode == 0x0c)       //ALL 12bit
            {
                writeCMOS(0x0214, 0x0A);
                writeCMOS(0x0215, 0x22);
                writeCMOS(0x0216, 0xB1);
                writeCMOS(0x0218, 0x40);
                writeCMOS(0x0219, 0x04);
                // writeCMOS(0x0234, 0x00);//Register hold
                writeCMOS(0x021B, 0x3A);
                writeCMOS(0x023C, 0x02);
                writeCMOS(0x02D0, 0x28);
                writeCMOS(0x02D1, 0x0B);
                writeCMOS(0x02D2, 0x28);
                writeCMOS(0x02D3, 0x0B);
                //VMAX
                writeCMOS(0x02D4, 0xFC);
                writeCMOS(0x02D5, 0x0B);
                writeCMOS(0x02D6, 0x00);
                //HMAX
                writeCMOS(0x02D8, 0xC8);
                writeCMOS(0x02D9, 0x00);
                //FREQ
                writeCMOS(0x02DC, 0x01);
                //
                writeCMOS(0x02E0, 0x08);
                writeCMOS(0x02E1, 0x08);
                writeCMOS(0x02E2, 0x0C);
                writeCMOS(0x02E3, 0x48);
                //writeCMOS(0x02E5, 0x04);
                writeCMOS(0x02E6, 0x20);
                //ROI
                writeCMOS(0x0300, 0x00);//
                writeCMOS(0x0304, 0x02);//
                writeCMOS(0x0322, 0x00);//
                writeCMOS(0x0323, 0x00);//
                writeCMOS(0x0326, 0x20);//
                writeCMOS(0x0327, 0x0B);//
                //
                writeCMOS(0x0400, 0x14);//ADC位数设置
                //writeCMOS(0x0404, 0x00);
                writeCMOS(0x041C, 0x80);
                //writeCMOS(0x041D, 0x05); 
                writeCMOS(0x041E, 0xE0);
                writeCMOS(0x041F, 0x00);
                writeCMOS(0x0420, 0x80);
                //writeCMOS(0x0421,0x05);  
                writeCMOS(0x0422, 0xE0);
                writeCMOS(0x0423, 0x00);
                writeCMOS(0x0424, 0x10);
                //writeCMOS(0x0425, 0x14); 
                writeCMOS(0x0426, 0x20);
                writeCMOS(0x0427, 0xD0);
                //writeCMOS(0x042B, 0x02);
                writeCMOS(0x0433, 0x50);
                //writeCMOS(0x043C, 0x31);
                //writeCMOS(0x043D, 0x00);
                writeCMOS(0x043E, 0x58);//中断
                //shs
                writeCMOS(0x0440, 0x70);
                writeCMOS(0x0441, 0x00);
                writeCMOS(0x0442, 0x00);
                //
                writeCMOS(0x0600, 0x00);//trig
                writeCMOS(0x0630, 0x01);//12bit 
                writeCMOS(0x063C, 0xC0);
                //
                writeCMOS(0x0702, 0x09);//增益反射
                //增益设置
                writeCMOS(0x0714, 0x00);
                writeCMOS(0x0715, 0x00);
                //
                writeCMOS(0x0721, 0x41);
                writeCMOS(0x0735, 0x00);
                writeCMOS(0x0742, 0x27);
                writeCMOS(0x0746, 0x10);
                writeCMOS(0x074A, 0x20);
                writeCMOS(0x079C, 0x0F);
                writeCMOS(0x079D, 0x01);
                writeCMOS(0x07A4, 0x08);
                writeCMOS(0x07A5, 0x12);
                writeCMOS(0x07A8, 0x08);
                writeCMOS(0x07A9, 0x52);
                //offset
                writeCMOS(0x07B4, 0xF0);//Black level offset 12 bit
                writeCMOS(0x07B5, 0x00);
                //
                writeCMOS(0x07CE, 0x0E);
                writeCMOS(0x07EC, 0x08);
                writeCMOS(0x07ED, 0x12);
                writeCMOS(0x07F0, 0xFB);
                writeCMOS(0x07F1, 0x0B);
                writeCMOS(0x07F2, 0xFB);
                writeCMOS(0x07F3, 0x0B);
                //
                writeCMOS(0x086A, 0x1B);
                writeCMOS(0x0870, 0xC3);
                writeCMOS(0x0872, 0x05);
                writeCMOS(0x0874, 0xB6);
                writeCMOS(0x0875, 0x01);
                writeCMOS(0x0876, 0x05);
                writeCMOS(0x08E8, 0x13);
                writeCMOS(0x08F5, 0x0F);
                //
                writeCMOS(0x0997, 0x00);//增益范围
                //
                writeCMOS(0x0b04, 0x00);//8lane
                //
                writeCMOS(0x0c00, 0xC1);//not crc 
                writeCMOS(0x0c0C, 0x0B);
                //
                writeCMOS(0x102E, 0x07);
                writeCMOS(0x1030, 0x4E);
                writeCMOS(0x106E, 0x07);
                writeCMOS(0x1070, 0x35);
                writeCMOS(0x1096, 0x01);
                writeCMOS(0x109E, 0x38);
                writeCMOS(0x10A0, 0x4C);
                //
                writeCMOS(0x113A, 0x04);
                //
                writeCMOS(0x1256, 0x23);
                writeCMOS(0x1296, 0x23);
                //
                writeCMOS(0x1382, 0x00);
                writeCMOS(0x13A2, 0x03);
                //
                writeCMOS(0x1432, 0x3C);
                writeCMOS(0x1435, 0x22);
                //
                writeCMOS(0x1506, 0x00);
                writeCMOS(0x1507, 0x00);
                writeCMOS(0x1508, 0x00);
                writeCMOS(0x1509, 0x00);
                writeCMOS(0x1510, 0x04);
                writeCMOS(0x1511, 0x04);
                writeCMOS(0x1512, 0x04);
                writeCMOS(0x1513, 0x04);
                writeCMOS(0x153C, 0x8A);
                writeCMOS(0x153D, 0x02);
                writeCMOS(0x153E, 0xE8);
                writeCMOS(0x153F, 0x05);
                writeCMOS(0x1540, 0x9E);
                writeCMOS(0x1541, 0x0C);
                //
                writeCMOS(0x1667, 0x83);
                writeCMOS(0x166A, 0x4C);
                writeCMOS(0x166E, 0x51);
                writeCMOS(0x1672, 0x57);
                writeCMOS(0x1676, 0x79);
                writeCMOS(0x168A, 0x4C);
                writeCMOS(0x168E, 0x51);
                writeCMOS(0x1692, 0x57);
                writeCMOS(0x1696, 0x79);
                writeCMOS(0x16EC, 0x3F);
                writeCMOS(0x16F0, 0x44);
                writeCMOS(0x16F4, 0x4A);
                //
                writeCMOS(0x1710, 0x3F);
                writeCMOS(0x1714, 0x44);
                writeCMOS(0x1718, 0x4A);
                writeCMOS(0x1776, 0xBE);
                writeCMOS(0x177A, 0xB1);
                writeCMOS(0x1780, 0xBC);
                writeCMOS(0x1784, 0xAF);
                //
                writeCMOS(0x192E, 0x06);
                writeCMOS(0x192F, 0x06);
                writeCMOS(0x1930, 0x06);
                writeCMOS(0x1931, 0x06);
                writeCMOS(0x193C, 0x06);
                writeCMOS(0x193D, 0x06);
                writeCMOS(0x193E, 0x06);
                writeCMOS(0x193F, 0x06);
                writeCMOS(0x1949, 0x9F);
                writeCMOS(0x194A, 0x99);
                writeCMOS(0x194B, 0x09);
                writeCMOS(0x1953, 0x90);
                writeCMOS(0x1954, 0x99);
                writeCMOS(0x1955, 0x09);
                writeCMOS(0x1988, 0x04);
                //
                writeCMOS(0x1a64, 0xDC);
                writeCMOS(0x1a68, 0xDC);
                writeCMOS(0x1a6C, 0xDC);
                writeCMOS(0x1a74, 0xDC);
                writeCMOS(0x1a78, 0xDC);
                writeCMOS(0x1a7C, 0xDC);
                writeCMOS(0x1aA4, 0xF4);
                writeCMOS(0x1aA8, 0xF4);
                writeCMOS(0x1aAC, 0xF4);
                writeCMOS(0x1aB4, 0xF4);
                writeCMOS(0x1aB8, 0xF4);
                writeCMOS(0x1aBC, 0xF4);
                //
                writeCMOS(0x1b00, 0x6C);
                writeCMOS(0x1b01, 0x0A);
                writeCMOS(0x1b02, 0x01);
                writeCMOS(0x1b08, 0x68);
                writeCMOS(0x1b16, 0x00);
                writeCMOS(0x1b17, 0x00);
                writeCMOS(0x1b18, 0xFF);
                writeCMOS(0x1b19, 0x0F);
                writeCMOS(0x1b1E, 0xFF);
                writeCMOS(0x1b1F, 0x0F);
                writeCMOS(0x1b20, 0x00);
                writeCMOS(0x1b21, 0x00);
                writeCMOS(0x1b26, 0xFF);
                writeCMOS(0x1b27, 0x0F);
                writeCMOS(0x1b28, 0x00);
                writeCMOS(0x1b29, 0x00);
                //
                writeCMOS(0x1c34, 0x0A);
                //
                writeCMOS(0x0200, 0x00);

                ushort hmax;
                ushort vmax;
                setIDLE();
                hmax = 573;     
                vmax = 3082;
                setHMAX(hmax);
                setVMAX(vmax);
                releaseIDLE();

            }
            else if (mode == 0xaa)      //ALL 12bit_RAW16    PFY
            {
                writeCMOS(0x0214, 0x0A);
                writeCMOS(0x0215, 0x22);
                writeCMOS(0x0216, 0xB1);
                writeCMOS(0x0218, 0x40);
                writeCMOS(0x0219, 0x04);
                writeCMOS(0x021B, 0x3A);
                // writeCMOS(0x0234, 0x00);//Register hold
                writeCMOS(0x023C, 0x02);
                writeCMOS(0x02D0, 0x28);
                writeCMOS(0x02D1, 0x0B);
                writeCMOS(0x02D2, 0x28);
                writeCMOS(0x02D3, 0x0B);
                //VMAX
                writeCMOS(0x02D4, 0xFC);
                writeCMOS(0x02D5, 0x0B);
                writeCMOS(0x02D6, 0x00);
                //HMAX
                writeCMOS(0x02D8, 0xC8);
                writeCMOS(0x02D9, 0x00);
                //FREQ
                writeCMOS(0x02DC, 0x01);
                //
                writeCMOS(0x02E0, 0x08);
                writeCMOS(0x02E1, 0x08);
                writeCMOS(0x02E2, 0x0C);
                writeCMOS(0x02E3, 0x48);
                //writeCMOS(0x02E5, 0x04);
                writeCMOS(0x02E6, 0x20);
                //ROI
                writeCMOS(0x0300, 0x00);//
                writeCMOS(0x0304, 0x02);//
                writeCMOS(0x0322, 0x00);//
                writeCMOS(0x0323, 0x00);//
                writeCMOS(0x0326, 0x20);//
                writeCMOS(0x0327, 0x0B);//
                //
                writeCMOS(0x0400, 0x14);//ADC
                //writeCMOS(0x0404, 0x00);
                writeCMOS(0x041C, 0x80);
                //writeCMOS(0x041D, 0x05); 
                writeCMOS(0x041E, 0xE0);
                writeCMOS(0x041F, 0x00);
                writeCMOS(0x0420, 0x80);
                //writeCMOS(0x0421,0x05);  
                writeCMOS(0x0422, 0xE0);
                writeCMOS(0x0423, 0x00);
                writeCMOS(0x0424, 0x10);
                //writeCMOS(0x0425, 0x14); 
                writeCMOS(0x0426, 0x20);
                writeCMOS(0x0427, 0xD0);
                //writeCMOS(0x042B, 0x02);
                writeCMOS(0x0433, 0x50);
                //writeCMOS(0x043C, 0x31);
                //writeCMOS(0x043D, 0x00);
                writeCMOS(0x043E, 0x58);// 中断
                //shs
                writeCMOS(0x0440, 0x70);
                writeCMOS(0x0441, 0x00);
                writeCMOS(0x0442, 0x00);
                //
                //writeCMOS(0x0600, 0x00);//trig
                writeCMOS(0x0630, 0x01);//12bit 
                writeCMOS(0x063C, 0xC0);
                //
                writeCMOS(0x0702, 0x09);//增益反射
                //增益设置
                writeCMOS(0x0714, 0x00);
                writeCMOS(0x0715, 0x00);
                //
                writeCMOS(0x0721, 0x41);
                writeCMOS(0x0735, 0x00);
                writeCMOS(0x0742, 0x27);
                writeCMOS(0x0746, 0x10);
                writeCMOS(0x074A, 0x20);
                writeCMOS(0x079C, 0x0F);
                writeCMOS(0x079D, 0x01);
                writeCMOS(0x07A4, 0x08);
                writeCMOS(0x07A5, 0x12);
                writeCMOS(0x07A8, 0x08);
                writeCMOS(0x07A9, 0x52);
                //offset
                writeCMOS(0x07B4, 0xF0);//Black level offset 12 bit
                writeCMOS(0x07B5, 0x00);  
                //
                writeCMOS(0x07CE, 0x0E);
                writeCMOS(0x07EC, 0x08);
                writeCMOS(0x07ED, 0x12);
                writeCMOS(0x07F0, 0xFB);
                writeCMOS(0x07F1, 0x0B);
                writeCMOS(0x07F2, 0xFB);
                writeCMOS(0x07F3, 0x0B);
                //
                writeCMOS(0x086A, 0x1B);
                writeCMOS(0x0870, 0xC3);
                writeCMOS(0x0872, 0x05);
                writeCMOS(0x0874, 0xB6);
                writeCMOS(0x0875, 0x01);
                writeCMOS(0x0876, 0x05);
                writeCMOS(0x08E8, 0x13);
                writeCMOS(0x08F5, 0x0F);
                //
                writeCMOS(0x0997, 0x00);//增益范围
                //
                writeCMOS(0x0b04, 0x00);//8lane
                //
                writeCMOS(0x0c00, 0xC1);//not crc 
                writeCMOS(0x0c0C, 0x0B);
                //
                writeCMOS(0x102E, 0x07);
                writeCMOS(0x1030, 0x4E);
                writeCMOS(0x106E, 0x07);
                writeCMOS(0x1070, 0x35);
                writeCMOS(0x1096, 0x01);
                writeCMOS(0x109E, 0x38);
                writeCMOS(0x10A0, 0x4C);
                //
                writeCMOS(0x113A, 0x04);
                //
                writeCMOS(0x1256, 0x23);
                writeCMOS(0x1296, 0x23);
                //
                writeCMOS(0x1382, 0x00);
                writeCMOS(0x13A2, 0x03);
                //
                writeCMOS(0x1432, 0x3C);
                writeCMOS(0x1435, 0x22);
                //
                writeCMOS(0x1506, 0x00);
                writeCMOS(0x1507, 0x00);
                writeCMOS(0x1508, 0x00);
                writeCMOS(0x1509, 0x00);
                writeCMOS(0x1510, 0x04);
                writeCMOS(0x1511, 0x04);
                writeCMOS(0x1512, 0x04);
                writeCMOS(0x1513, 0x04);
                writeCMOS(0x153C, 0x8A);
                writeCMOS(0x153D, 0x02);
                writeCMOS(0x153E, 0xE8);
                writeCMOS(0x153F, 0x05);
                writeCMOS(0x1540, 0x9E);
                writeCMOS(0x1541, 0x0C);
                //
                writeCMOS(0x1667, 0x83);
                writeCMOS(0x166A, 0x4C);
                writeCMOS(0x166E, 0x51);
                writeCMOS(0x1672, 0x57);
                writeCMOS(0x1676, 0x79);
                writeCMOS(0x168A, 0x4C);
                writeCMOS(0x168E, 0x51);
                writeCMOS(0x1692, 0x57);
                writeCMOS(0x1696, 0x79);
                writeCMOS(0x16EC, 0x3F);
                writeCMOS(0x16F0, 0x44);
                writeCMOS(0x16F4, 0x4A);
                //
                writeCMOS(0x1710, 0x3F);
                writeCMOS(0x1714, 0x44);
                writeCMOS(0x1718, 0x4A);
                writeCMOS(0x1776, 0xBE);
                writeCMOS(0x177A, 0xB1);
                writeCMOS(0x1780, 0xBC);
                writeCMOS(0x1784, 0xAF);
                //
                writeCMOS(0x192E, 0x06);
                writeCMOS(0x192F, 0x06);
                writeCMOS(0x1930, 0x06);
                writeCMOS(0x1931, 0x06);
                writeCMOS(0x193C, 0x06);
                writeCMOS(0x193D, 0x06);
                writeCMOS(0x193E, 0x06);
                writeCMOS(0x193F, 0x06);
                writeCMOS(0x1949, 0x9F);
                writeCMOS(0x194A, 0x99);
                writeCMOS(0x194B, 0x09);
                writeCMOS(0x1953, 0x90);
                writeCMOS(0x1954, 0x99);
                writeCMOS(0x1955, 0x09);
                writeCMOS(0x1988, 0x04);
                //
                writeCMOS(0x1a64, 0xDC);
                writeCMOS(0x1a68, 0xDC);
                writeCMOS(0x1a6C, 0xDC);
                writeCMOS(0x1a74, 0xDC);
                writeCMOS(0x1a78, 0xDC);
                writeCMOS(0x1a7C, 0xDC);
                writeCMOS(0x1aA4, 0xF4);
                writeCMOS(0x1aA8, 0xF4);
                writeCMOS(0x1aAC, 0xF4);
                writeCMOS(0x1aB4, 0xF4);
                writeCMOS(0x1aB8, 0xF4);
                writeCMOS(0x1aBC, 0xF4);
                //
                writeCMOS(0x1b00, 0x6C);
                writeCMOS(0x1b01, 0x0A);
                writeCMOS(0x1b02, 0x01);
                writeCMOS(0x1b08, 0x68);
                writeCMOS(0x1b16, 0x00);
                writeCMOS(0x1b17, 0x00);
                writeCMOS(0x1b18, 0xFF);
                writeCMOS(0x1b19, 0x0F);
                writeCMOS(0x1b1E, 0xFF);
                writeCMOS(0x1b1F, 0x0F);
                writeCMOS(0x1b20, 0x00);
                writeCMOS(0x1b21, 0x00);
                writeCMOS(0x1b26, 0xFF);
                writeCMOS(0x1b27, 0x0F);
                writeCMOS(0x1b28, 0x00);
                writeCMOS(0x1b29, 0x00);
                //
                writeCMOS(0x1c34, 0x0A);
                //
                writeCMOS(0x0200, 0x00);

                ushort hmax;
                ushort vmax;
                setIDLE();
                hmax = 1149;   
                vmax = 3082;
                setHMAX(hmax);
                setVMAX(vmax);
                releaseIDLE();

                writeFPGA(3, 1); //16bit

            }
            else if (mode == 0xc2)              //2*2bin_RAW8    PFY
            {
                writeCMOS(0x0214, 0x0A);
                writeCMOS(0x0215, 0x22);
                writeCMOS(0x0216, 0xB1);
                writeCMOS(0x0218, 0x40);
                writeCMOS(0x0219, 0x04);
                writeCMOS(0x021B, 0x3A);
                //  writeCMOS(0x0234, 0x00);//Register hold
                writeCMOS(0x023C, 0x12);//
                writeCMOS(0x02D0, 0x94);//
                writeCMOS(0x02D1, 0x05);//
                writeCMOS(0x02D2, 0x94);//
                writeCMOS(0x02D3, 0x05);//
                //VMAX
                writeCMOS(0x02D4, 0xA8);//
                writeCMOS(0x02D5, 0x06);//
                writeCMOS(0x02D6, 0x00);//
                //HMAX
                writeCMOS(0x02D8, 0x6F);//
                writeCMOS(0x02D9, 0x00);//
                //FREQ
                writeCMOS(0x02DC, 0x01);//
                //
                writeCMOS(0x02E0, 0x08);//
                writeCMOS(0x02E1, 0x08);//
                writeCMOS(0x02E2, 0x18);//
                writeCMOS(0x02E3, 0x80);//
                writeCMOS(0x02E5, 0x08);//
                writeCMOS(0x02E6, 0x38);//
                //ROI
                writeCMOS(0x0300, 0x00);//
                writeCMOS(0x0304, 0x02);//
                writeCMOS(0x0322, 0x00);//
                writeCMOS(0x0323, 0x00);//
                writeCMOS(0x0326, 0x90);//
                writeCMOS(0x0327, 0x05);//
                //12bit
                writeCMOS(0x0400, 0x14);
                writeCMOS(0x041C, 0x80);//
                writeCMOS(0x041D, 0x05);//
                writeCMOS(0x041E, 0xE0);//
                writeCMOS(0x041F, 0x00);//
                writeCMOS(0x0420, 0x80);//
                writeCMOS(0x0421, 0x05);//  
                writeCMOS(0x0422, 0xE0);//
                writeCMOS(0x0423, 0x00);//
                writeCMOS(0x0424, 0x10);//
                writeCMOS(0x0425, 0x14);//
                writeCMOS(0x0426, 0x20);//
                writeCMOS(0x0427, 0xD0);//
                writeCMOS(0x042B, 0x02);// 
                writeCMOS(0x0433, 0x50);//
                writeCMOS(0x043C, 0x31);// 
                writeCMOS(0x043D, 0x00);// 
                writeCMOS(0x043E, 0x58);// 
                //中断
                writeCMOS(0x043E, 0x48);//
                //shs
                writeCMOS(0x0440, 0x70);
                writeCMOS(0x0441, 0x00);
                writeCMOS(0x0442, 0x00);
                //
                //writeCMOS(0x0600, 0x00);
                //12bit
                writeCMOS(0x0630, 0x01);
                writeCMOS(0x063C, 0xC0);
                //gain反射
                writeCMOS(0x0702, 0x09);
                //增益
                writeCMOS(0x0714, 0x00);
                writeCMOS(0x0715, 0x00);
                //
                writeCMOS(0x0721, 0x21);//
                writeCMOS(0x0735, 0x00);
                writeCMOS(0x0742, 0x27);
                writeCMOS(0x0746, 0x08);//  
                writeCMOS(0x074A, 0x20);
                writeCMOS(0x079C, 0x0F);
                writeCMOS(0x079D, 0x01);
                writeCMOS(0x07A4, 0x08);
                writeCMOS(0x07A5, 0x12);
                writeCMOS(0x07A8, 0x08);
                writeCMOS(0x07A9, 0x52);
                //offset
                writeCMOS(0x07B4, 0xF0);
                writeCMOS(0x07B5, 0x00); 
                //
                writeCMOS(0x07CE, 0x0E);
                writeCMOS(0x07EC, 0x08);
                writeCMOS(0x07ED, 0x12);
                writeCMOS(0x07F0, 0xFB);
                writeCMOS(0x07F1, 0x0B);
                writeCMOS(0x07F2, 0xFB);
                writeCMOS(0x07F3, 0x0B);
                //
                writeCMOS(0x086A, 0x1B);
                writeCMOS(0x0870, 0xC3);
                writeCMOS(0x0872, 0x05);
                writeCMOS(0x0874, 0xB6);
                writeCMOS(0x0875, 0x01);
                writeCMOS(0x0876, 0x05);
                writeCMOS(0x08E8, 0x13);
                writeCMOS(0x08F5, 0x0F);
                //
                writeCMOS(0x0997, 0x00);
                //8lane
                writeCMOS(0x0b04, 0x00);
                //crc
                writeCMOS(0x0c00, 0xC1);
                writeCMOS(0x0c0C, 0x0B);
                //
                writeCMOS(0x102E, 0x07);
                writeCMOS(0x1030, 0x4E);
                writeCMOS(0x106E, 0x07);
                writeCMOS(0x1070, 0x35);
                writeCMOS(0x1096, 0x01);
                writeCMOS(0x109E, 0x38);
                writeCMOS(0x10A0, 0x4C);
                //
                writeCMOS(0x113A, 0x04);
                //
                writeCMOS(0x1256, 0x23);
                writeCMOS(0x1296, 0x23);
                //
                writeCMOS(0x1382, 0x00);
                writeCMOS(0x13A2, 0x03);
                //
                writeCMOS(0x1432, 0x3C);
                writeCMOS(0x1435, 0x22);
                //
                writeCMOS(0x1506, 0x00);
                writeCMOS(0x1507, 0x00);
                writeCMOS(0x1508, 0x00);
                writeCMOS(0x1509, 0x00);
                writeCMOS(0x1510, 0x04);
                writeCMOS(0x1511, 0x04);
                writeCMOS(0x1512, 0x04);
                writeCMOS(0x1513, 0x04);
                writeCMOS(0x153C, 0x8A);
                writeCMOS(0x153D, 0x02);
                writeCMOS(0x153E, 0xE8);
                writeCMOS(0x153F, 0x05);
                writeCMOS(0x1540, 0x9E);
                writeCMOS(0x1541, 0x0C);
                //
                writeCMOS(0x1667, 0x83);
                writeCMOS(0x166A, 0x4C);
                writeCMOS(0x166E, 0x51);
                writeCMOS(0x1672, 0x57);
                writeCMOS(0x1676, 0x79);
                writeCMOS(0x168A, 0x4C);
                writeCMOS(0x168E, 0x51);
                writeCMOS(0x1692, 0x57);
                writeCMOS(0x1696, 0x79);
                writeCMOS(0x16EC, 0x3F);
                writeCMOS(0x16F0, 0x44);
                writeCMOS(0x16F4, 0x4A);
                //
                writeCMOS(0x1710, 0x3F);
                writeCMOS(0x1714, 0x44);
                writeCMOS(0x1718, 0x4A);
                writeCMOS(0x1776, 0xBE);
                writeCMOS(0x177A, 0xB1);
                writeCMOS(0x1780, 0xBC);
                writeCMOS(0x1784, 0xAF);
                //
                writeCMOS(0x192E, 0x06);
                writeCMOS(0x192F, 0x06);
                writeCMOS(0x1930, 0x06);
                writeCMOS(0x1931, 0x06);
                writeCMOS(0x193C, 0x06);
                writeCMOS(0x193D, 0x06);
                writeCMOS(0x193E, 0x06);
                writeCMOS(0x193F, 0x06);
                writeCMOS(0x1949, 0x9F);
                writeCMOS(0x194A, 0x99);
                writeCMOS(0x194B, 0x09);
                writeCMOS(0x1953, 0x90);
                writeCMOS(0x1954, 0x99);
                writeCMOS(0x1955, 0x09);
                writeCMOS(0x1988, 0x04);
                //
                writeCMOS(0x1a64, 0xDC);
                writeCMOS(0x1a68, 0xDC);
                writeCMOS(0x1a6C, 0xDC);
                writeCMOS(0x1a74, 0xDC);
                writeCMOS(0x1a78, 0xDC);
                writeCMOS(0x1a7C, 0xDC);
                writeCMOS(0x1aA4, 0xF4);
                writeCMOS(0x1aA8, 0xF4);
                writeCMOS(0x1aAC, 0xF4);
                writeCMOS(0x1aB4, 0xF4);
                writeCMOS(0x1aB8, 0xF4);
                writeCMOS(0x1aBC, 0xF4);
                //
                writeCMOS(0x1b00, 0x6C);
                writeCMOS(0x1b01, 0x0A);
                writeCMOS(0x1b02, 0x01);
                writeCMOS(0x1b08, 0x68);
                writeCMOS(0x1b16, 0x00);
                writeCMOS(0x1b17, 0x00);
                writeCMOS(0x1b18, 0xFF);
                writeCMOS(0x1b19, 0x0F);
                writeCMOS(0x1b1E, 0xFF);
                writeCMOS(0x1b1F, 0x0F);
                writeCMOS(0x1b20, 0x00);
                writeCMOS(0x1b21, 0x00);
                writeCMOS(0x1b26, 0xFF);
                writeCMOS(0x1b27, 0x0F);
                writeCMOS(0x1b28, 0x00);
                writeCMOS(0x1b29, 0x00);
                //
                writeCMOS(0x1c34, 0x0A);
                //
                writeCMOS(0x0200, 0x00);

                ushort hmax;
                ushort vmax;
                setIDLE();
                hmax = 255;     
                vmax = 1740;
                setHMAX(hmax);
                setVMAX(vmax);
                releaseIDLE();

            }
            else if (mode == 0xbb)          //2*2bin_RAW16     PFY
            {
                writeCMOS(0x0214, 0x0A);
                writeCMOS(0x0215, 0x22);
                writeCMOS(0x0216, 0xB1);
                writeCMOS(0x0218, 0x40);
                writeCMOS(0x0219, 0x04);
                writeCMOS(0x021B, 0x3A);
                // writeCMOS(0x0234, 0x00);//Register hold
                writeCMOS(0x023C, 0x12);//
                writeCMOS(0x02D0, 0x94);//
                writeCMOS(0x02D1, 0x05);//
                writeCMOS(0x02D2, 0x94);//
                writeCMOS(0x02D3, 0x05);//
                //VMAX
                writeCMOS(0x02D4, 0xA8);//
                writeCMOS(0x02D5, 0x06);//
                writeCMOS(0x02D6, 0x00);//
                //HMAX
                writeCMOS(0x02D8, 0x6F);//
                writeCMOS(0x02D9, 0x00);//
                //FREQ
                writeCMOS(0x02DC, 0x01);//
                //
                writeCMOS(0x02E0, 0x08);//
                writeCMOS(0x02E1, 0x08);//
                writeCMOS(0x02E2, 0x18);//
                writeCMOS(0x02E3, 0x80);//
                writeCMOS(0x02E5, 0x08);//
                writeCMOS(0x02E6, 0x38);//
                //ROI
                writeCMOS(0x0300, 0x00);//
                writeCMOS(0x0304, 0x02);//
                writeCMOS(0x0322, 0x00);//
                writeCMOS(0x0323, 0x00);//
                writeCMOS(0x0326, 0x90);//
                writeCMOS(0x0327, 0x05);//
                //12bit
                writeCMOS(0x0400, 0x14);
                writeCMOS(0x041C, 0x80);//
                writeCMOS(0x041D, 0x05);//
                writeCMOS(0x041E, 0xE0);//
                writeCMOS(0x041F, 0x00);//
                writeCMOS(0x0420, 0x80);//
                writeCMOS(0x0421, 0x05);//  
                writeCMOS(0x0422, 0xE0);//
                writeCMOS(0x0423, 0x00);//
                writeCMOS(0x0424, 0x10);//
                writeCMOS(0x0425, 0x14);//
                writeCMOS(0x0426, 0x20);//
                writeCMOS(0x0427, 0xD0);//
                writeCMOS(0x042B, 0x02);// 
                writeCMOS(0x0433, 0x50);//
                writeCMOS(0x043C, 0x31);// 
                writeCMOS(0x043D, 0x00);// 
                writeCMOS(0x043E, 0x58);// 
                //中断
                writeCMOS(0x043E, 0x48);//
                //shs
                writeCMOS(0x0440, 0x70);
                writeCMOS(0x0441, 0x00);
                writeCMOS(0x0442, 0x00);
                //
                //writeCMOS(0x0600, 0x00);
                //12bit
                writeCMOS(0x0630, 0x01);
                writeCMOS(0x063C, 0xC0);
                //gain反射
                writeCMOS(0x0702, 0x09);
                //gain
                writeCMOS(0x0714, 0x00);
                writeCMOS(0x0715, 0x00);
                //
                writeCMOS(0x0721, 0x21);//
                writeCMOS(0x0735, 0x00);
                writeCMOS(0x0742, 0x27);
                writeCMOS(0x0746, 0x08);//  
                writeCMOS(0x074A, 0x20);
                writeCMOS(0x079C, 0x0F);
                writeCMOS(0x079D, 0x01);
                writeCMOS(0x07A4, 0x08);
                writeCMOS(0x07A5, 0x12);
                writeCMOS(0x07A8, 0x08);
                writeCMOS(0x07A9, 0x52);
                //offset
                writeCMOS(0x07B4, 0xF0);
                writeCMOS(0x07B5, 0x00); 
                //
                writeCMOS(0x07CE, 0x0E);
                writeCMOS(0x07EC, 0x08);
                writeCMOS(0x07ED, 0x12);
                writeCMOS(0x07F0, 0xFB);
                writeCMOS(0x07F1, 0x0B);
                writeCMOS(0x07F2, 0xFB);
                writeCMOS(0x07F3, 0x0B);
                //
                writeCMOS(0x086A, 0x1B);
                writeCMOS(0x0870, 0xC3);
                writeCMOS(0x0872, 0x05);
                writeCMOS(0x0874, 0xB6);
                writeCMOS(0x0875, 0x01);
                writeCMOS(0x0876, 0x05);
                writeCMOS(0x08E8, 0x13);
                writeCMOS(0x08F5, 0x0F);
                //
                writeCMOS(0x0997, 0x00);
                //8lane
                writeCMOS(0x0b04, 0x00);
                //crc
                writeCMOS(0x0c00, 0xC1);
                writeCMOS(0x0c0C, 0x0B);
                //
                writeCMOS(0x102E, 0x07);
                writeCMOS(0x1030, 0x4E);
                writeCMOS(0x106E, 0x07);
                writeCMOS(0x1070, 0x35);
                writeCMOS(0x1096, 0x01);
                writeCMOS(0x109E, 0x38);
                writeCMOS(0x10A0, 0x4C);
                //
                writeCMOS(0x113A, 0x04);
                //
                writeCMOS(0x1256, 0x23);
                writeCMOS(0x1296, 0x23);
                //
                writeCMOS(0x1382, 0x00);
                writeCMOS(0x13A2, 0x03);
                //
                writeCMOS(0x1432, 0x3C);
                writeCMOS(0x1435, 0x22);
                //
                writeCMOS(0x1506, 0x00);
                writeCMOS(0x1507, 0x00);
                writeCMOS(0x1508, 0x00);
                writeCMOS(0x1509, 0x00);
                writeCMOS(0x1510, 0x04);
                writeCMOS(0x1511, 0x04);
                writeCMOS(0x1512, 0x04);
                writeCMOS(0x1513, 0x04);
                writeCMOS(0x153C, 0x8A);
                writeCMOS(0x153D, 0x02);
                writeCMOS(0x153E, 0xE8);
                writeCMOS(0x153F, 0x05);
                writeCMOS(0x1540, 0x9E);
                writeCMOS(0x1541, 0x0C);
                //
                writeCMOS(0x1667, 0x83);
                writeCMOS(0x166A, 0x4C);
                writeCMOS(0x166E, 0x51);
                writeCMOS(0x1672, 0x57);
                writeCMOS(0x1676, 0x79);
                writeCMOS(0x168A, 0x4C);
                writeCMOS(0x168E, 0x51);
                writeCMOS(0x1692, 0x57);
                writeCMOS(0x1696, 0x79);
                writeCMOS(0x16EC, 0x3F);
                writeCMOS(0x16F0, 0x44);
                writeCMOS(0x16F4, 0x4A);
                //
                writeCMOS(0x1710, 0x3F);
                writeCMOS(0x1714, 0x44);
                writeCMOS(0x1718, 0x4A);
                writeCMOS(0x1776, 0xBE);
                writeCMOS(0x177A, 0xB1);
                writeCMOS(0x1780, 0xBC);
                writeCMOS(0x1784, 0xAF);
                //
                writeCMOS(0x192E, 0x06);
                writeCMOS(0x192F, 0x06);
                writeCMOS(0x1930, 0x06);
                writeCMOS(0x1931, 0x06);
                writeCMOS(0x193C, 0x06);
                writeCMOS(0x193D, 0x06);
                writeCMOS(0x193E, 0x06);
                writeCMOS(0x193F, 0x06);
                writeCMOS(0x1949, 0x9F);
                writeCMOS(0x194A, 0x99);
                writeCMOS(0x194B, 0x09);
                writeCMOS(0x1953, 0x90);
                writeCMOS(0x1954, 0x99);
                writeCMOS(0x1955, 0x09);
                writeCMOS(0x1988, 0x04);
                //
                writeCMOS(0x1a64, 0xDC);
                writeCMOS(0x1a68, 0xDC);
                writeCMOS(0x1a6C, 0xDC);
                writeCMOS(0x1a74, 0xDC);
                writeCMOS(0x1a78, 0xDC);
                writeCMOS(0x1a7C, 0xDC);
                writeCMOS(0x1aA4, 0xF4);
                writeCMOS(0x1aA8, 0xF4);
                writeCMOS(0x1aAC, 0xF4);
                writeCMOS(0x1aB4, 0xF4);
                writeCMOS(0x1aB8, 0xF4);
                writeCMOS(0x1aBC, 0xF4);
                //
                writeCMOS(0x1b00, 0x6C);
                writeCMOS(0x1b01, 0x0A);
                writeCMOS(0x1b02, 0x01);
                writeCMOS(0x1b08, 0x68);
                writeCMOS(0x1b16, 0x00);
                writeCMOS(0x1b17, 0x00);
                writeCMOS(0x1b18, 0xFF);
                writeCMOS(0x1b19, 0x0F);
                writeCMOS(0x1b1E, 0xFF);
                writeCMOS(0x1b1F, 0x0F);
                writeCMOS(0x1b20, 0x00);
                writeCMOS(0x1b21, 0x00);
                writeCMOS(0x1b26, 0xFF);
                writeCMOS(0x1b27, 0x0F);
                writeCMOS(0x1b28, 0x00);
                writeCMOS(0x1b29, 0x00);
                //
                writeCMOS(0x1c34, 0x0A);
                //
                writeCMOS(0x0200, 0x00);

                ushort hmax;
                ushort vmax;
                setIDLE();
                hmax = 508;    
                vmax = 1740;
                setHMAX(hmax);
                setVMAX(vmax);
                releaseIDLE();

                writeFPGA(3, 1); //16bit
            }

        }//PFY

        void initCMOS_IMX661()
        {
            writeFPGA(0, 0);
            Thread.Sleep(100);
            writeFPGA(0, 1);
            Thread.Sleep(200);

            //VRL----internal or external
            writeCMOS(0x0D07, 0x06);                                                                              
            writeCMOS(0x0D08, 0x12);
            writeCMOS(0x0D09, 0x01);//external	011206
            writeCMOS(0x02D6, 0x3C);//VRL----external

            //writeCMOS(0x0D07, 0xC8);
            //writeCMOS(0x0D08, 0xA8);//internal  16A8C8
            //writeCMOS(0x0D09, 0x16);
            //writeCMOS(0x02D6, 0x00);//VRL----internal

            //1-4. Set initial settings.
            writeCMOS(0x0100, 0x01);//STANDBY
            writeCMOS(0x0101, 0x01);//XMSTA
            writeCMOS(0x0102, 0x08);//INCK  FREQ
            writeCMOS(0x0103, 0x41);//SLVS_EN
            //
            writeCMOS(0x0104, 0x20);//INCK
            writeCMOS(0x0105, 0x00);
            //
            writeCMOS(0x0106, 0x35);
            writeCMOS(0x0107, 0x00);
            writeCMOS(0x0108, 0x31);//READMODE 31 : Parallel Read2     30:Parallel Read1  32: Sequential Read 
            writeCMOS(0x010C, 0x01);//TRIGMODE
            writeCMOS(0x010D, 0x00);//LESS_SHUT
            //
            writeCMOS(0x010E, 0x8C);//VMAX      Sequential Read 002670--->9840
            writeCMOS(0x010F, 0x13);//          Parallel   Read 00138C--->5004
            writeCMOS(0x0110, 0x00);
            //
            writeCMOS(0x0111, 0xF4);//HMAX
            writeCMOS(0x0112, 0x03);
            //
            writeCMOS(0x0113, 0x1C);
            writeCMOS(0x0114, 0x44);
            writeCMOS(0x0115, 0x2C);
            //
            writeCMOS(0x011E, 0x64);//SHR
            writeCMOS(0x011F, 0x00);
            writeCMOS(0x0120, 0x00);
            //
            writeCMOS(0x012A, 0x00);//AGAIN
            writeCMOS(0x012B, 0x00);
            //
            writeCMOS(0x0133, 0x10);//AGAIN_RTS
            writeCMOS(0x0134, 0x00);//interrupt
            //
            writeCMOS(0x0138, 0xFF);//OFFSET
            writeCMOS(0x0139, 0x03);
            //
            writeCMOS(0x014E, 0x01);
            //
            writeCMOS(0x0187, 0x00);//CRC / ECC insertion
            //
            writeCMOS(0x0200, 0x00);//ROI
            writeCMOS(0x0201, 0x00);
            writeCMOS(0x0206, 0x00);
            writeCMOS(0x0207, 0x00);
            writeCMOS(0x020A, 0x00);
            writeCMOS(0x020B, 0x00);
            //
            writeCMOS(0x02B4, 0x40);
            //
            writeCMOS(0x02B6, 0x12);//INCK
            writeCMOS(0x02B7, 0x00);
            //
            writeCMOS(0x0D04, 0xA6);//SLVS-EC
            writeCMOS(0x0D05, 0x0E);
            writeCMOS(0x0D32, 0xA6);
            writeCMOS(0x0D33, 0x0E);
            //
            writeCMOS(0x0D67, 0x10);
            writeCMOS(0x0D7E, 0xBC);
            writeCMOS(0x0D7F, 0x02);
            writeCMOS(0x0D82, 0x60);
            writeCMOS(0x0D83, 0x04);
            writeCMOS(0x0D86, 0x60);
            writeCMOS(0x0D87, 0x04);
            //
            writeCMOS(0x1312, 0x20);
            writeCMOS(0x1319, 0x10);
            writeCMOS(0x134A, 0x01);
            writeCMOS(0x1532, 0x44);
            writeCMOS(0x1533, 0x14);
            writeCMOS(0x1602, 0x20);
            writeCMOS(0x1603, 0xC8);
            writeCMOS(0x1604, 0x82);
            writeCMOS(0x1605, 0x2C);
            writeCMOS(0x1606, 0xC8);
            writeCMOS(0x1620, 0x0C);
            writeCMOS(0x1621, 0x0C);
            writeCMOS(0x1623, 0x1E);
            writeCMOS(0x1624, 0x0C);
            writeCMOS(0x1625, 0x0C);
            writeCMOS(0x1627, 0x1E);
            writeCMOS(0x1628, 0x08);
            writeCMOS(0x1629, 0x08);
            writeCMOS(0x1711, 0xB4);
            writeCMOS(0x175F, 0x00);
            writeCMOS(0x198B, 0x64);
            writeCMOS(0x198E, 0xFF);
            writeCMOS(0x198F, 0x0F);
            writeCMOS(0x1990, 0x00);
            writeCMOS(0x1991, 0x00);
            writeCMOS(0x1992, 0xFF);
            writeCMOS(0x1993, 0x0F);
            writeCMOS(0x1994, 0x00);
            writeCMOS(0x1995, 0x00);
            writeCMOS(0x1996, 0xFF);
            writeCMOS(0x1997, 0x0F);
            writeCMOS(0x1998, 0x00);
            writeCMOS(0x1999, 0x00);
            writeCMOS(0x199A, 0xFF);
            writeCMOS(0x199B, 0x0F);
            writeCMOS(0x199C, 0x00);
            writeCMOS(0x199D, 0x00);
            writeCMOS(0x19C1, 0x38);
            writeCMOS(0x19C4, 0x11);
            writeCMOS(0x19C5, 0x01);
            writeCMOS(0x19CC, 0x10);
            writeCMOS(0x19CF, 0x77);
            writeCMOS(0x19D0, 0x73);
            writeCMOS(0x19D1, 0x37);
            writeCMOS(0x1A0C, 0x67);
            writeCMOS(0x1A49, 0x82);
            writeCMOS(0x1A4A, 0x00);
            writeCMOS(0x1A51, 0x46);
            writeCMOS(0x1A52, 0x00);
            writeCMOS(0x1A57, 0x0C);
            writeCMOS(0x1A58, 0x01);
            writeCMOS(0x1A5F, 0x2A);
            writeCMOS(0x1A60, 0x01);
            writeCMOS(0x1AD7, 0x00);
            writeCMOS(0x1FE4, 0x12);

            

            //Test pattern
           // writeCMOS(0x01A0, 0x01);//[5:4]FIXDTSEL  [0]FIXDTON
            //writeCMOS(0X01A1, 0X00);//[2:0]FIXDTSFT  [4]COLORBARCEL

            //***************************
            writeCMOS(0x0100, 0x00);

            ushort hmax;
            ushort vmax;
            setIDLE();
            hmax = 5200;        //2692;    //5200;      //Sequential Read  2611,Parallel Read 5094
            vmax = 5100;        //9850;   //5100;      //Sequential Read  9950,Parallel Read 5100
            setHMAX(hmax);
            setVMAX(vmax);
            releaseIDLE();

           

        }

        void initCMOS_IMX811()
        {
            writeFPGA(0, 0);
            Thread.Sleep(100);
            writeFPGA(0, 1);
            Thread.Sleep(200);

            writeCMOS(0x00F0, 0x01);//STANDBY ON
            writeCMOS(0x0111, 0x00);
            writeCMOS(0x2071, 0x02);//FREQ   2.304G
            writeCMOS(0x8E1E, 0x02);//PL_RG_DIVPLA_IF  2.304Gbps
            writeCMOS(0x4040, 0x01);
            writeCMOS(0x4051, 0x20);
            writeCMOS(0x4052, 0x1C);
            writeCMOS(0x0052, 0x48);
            writeCMOS(0x0053, 0x00);
            writeCMOS(0x0054, 0x00);
            writeCMOS(0x0055, 0xC0);
            writeCMOS(0x0056, 0x4B);
            writeCMOS(0x0057, 0x03);
            writeCMOS(0x0058, 0x48);
            writeCMOS(0x0059, 0x00);
            writeCMOS(0x005A, 0x00);
            writeCMOS(0x005B, 0x80);
            writeCMOS(0x005C, 0x32);
            writeCMOS(0x005D, 0x02);
            writeCMOS(0x00B2, 0x00);
            writeCMOS(0x00B3, 0x34);
            writeCMOS(0x00B4, 0x11);
            writeCMOS(0x00B5, 0x00);
            writeCMOS(0x00B6, 0x34);
            writeCMOS(0x00B7, 0x11);
            writeCMOS(0x00B8, 0x00);
            writeCMOS(0x00B9, 0x30);
            writeCMOS(0x00BA, 0x11);
            writeCMOS(0x00BB, 0x00);
            writeCMOS(0x00BC, 0x30);
            writeCMOS(0x00BD, 0x11);
            writeCMOS(0x00BE, 0x00);
            writeCMOS(0x00BF, 0x60);
            writeCMOS(0x00C0, 0x11);
            writeCMOS(0x2947, 0x01);
            writeCMOS(0x9003, 0x16);
            writeCMOS(0x9005, 0x58);
            writeCMOS(0x22DE, 0x01);
            writeCMOS(0x0013, 0x00);//SVR
            writeCMOS(0x0018, 0x00);//SMD  Shutter mode 0h: Rolling shutter  1h: Global reset
            writeCMOS(0x0019, 0x01);//SDO_ACT  0h: Hi-Z  1h: Register data output
            writeCMOS(0x00F1, 0x00);//SLEEP 
            writeCMOS(0x00F7, 0x01);//PL_SLP_IF
            writeCMOS(0x0300, 0x00);//MODE
            writeCMOS(0x0301, 0x00);//H3MODE 
            writeCMOS(0x0302, 0x00);//BITMODE  
            writeCMOS(0x03A7, 0x03);//LANE_SEL_0 
            writeCMOS(0x03E6, 0x01);//CLPOFF
            writeCMOS(0x1804, 0x00);//TRIG_MODE 
            writeCMOS(0x1805, 0x00);//Slow Shutter Break
            writeCMOS(0x1811, 0x00);
            writeCMOS(0x1812, 0x00);//SPL
            writeCMOS(0x1816, 0x00);//MSMD AND OPMODE
            writeCMOS(0x2003, 0x20);
            writeCMOS(0x2004, 0x00);//CMP_THRE1
            writeCMOS(0x2005, 0x40);
            writeCMOS(0x2006, 0xFF);//CMP_THRE2
            writeCMOS(0x2007, 0x60);
            writeCMOS(0x2008, 0x00);//CMP_THRE3
            writeCMOS(0x2009, 0x80);
            writeCMOS(0x200A, 0x00);//CMP_THRE4
            writeCMOS(0x200B, 0xA0);
            writeCMOS(0x200C, 0x00);//CMP_THRE5
            writeCMOS(0x200D, 0xC0);
            writeCMOS(0x200E, 0x00);//CMP_THRE6
            writeCMOS(0x200F, 0xF0);
            writeCMOS(0x2010, 0x00);//CMP_THRE7
            writeCMOS(0x205A, 0x00);//Without CRC / ECC
            writeCMOS(0x2062, 0x12);//SLVSEC_VER_SEL AND LINELENGTH_H_CHG_OFF
            writeCMOS(0x2066, 0x00);//FS_FE_MASK_EN
            writeCMOS(0x2067, 0x00);//MDCHG_FS_FE_MASK 		
            writeCMOS(0x21A0, 0x01);//STRCLAMP_EN 
            writeCMOS(0x4013, 0x00);//8b10b encoding
            writeCMOS(0x5000, 0x00);
            writeCMOS(0x5001, 0x18);//TMONSEL_TOUT0
            writeCMOS(0x5002, 0x00);
            writeCMOS(0x5003, 0x18);//TMONSEL_TOUT1 
            writeCMOS(0x015D, 0x00);
            writeCMOS(0x031F, 0x00);
            writeCMOS(0x0321, 0x10);
            writeCMOS(0x0327, 0x04);
            writeCMOS(0x032A, 0x00);
            writeCMOS(0x0367, 0x10);
            writeCMOS(0x1838, 0x04);
            writeCMOS(0x183A, 0x07);
            writeCMOS(0x1849, 0x01);
            writeCMOS(0x184B, 0x01);
            writeCMOS(0x184D, 0x01);
            writeCMOS(0x184F, 0x01);
            writeCMOS(0x1851, 0x01);
            writeCMOS(0x1853, 0x01);
            writeCMOS(0x1857, 0x07);
            writeCMOS(0x2000, 0x11);
            writeCMOS(0x2014, 0xD4);
            writeCMOS(0x2015, 0x03);
            writeCMOS(0x2016, 0x09);
            writeCMOS(0x2017, 0x01);
            writeCMOS(0x2018, 0x09);
            writeCMOS(0x2019, 0x01);
            writeCMOS(0x2098, 0xE8);
            writeCMOS(0x2099, 0x03);
            writeCMOS(0x209A, 0xE8);
            writeCMOS(0x209B, 0x03);
            writeCMOS(0x209C, 0x4D);
            writeCMOS(0x209D, 0x01);
            writeCMOS(0x209E, 0x4D);
            writeCMOS(0x209F, 0x01);
            writeCMOS(0x20A0, 0xE8);
            writeCMOS(0x20A1, 0x03);
            writeCMOS(0x20A2, 0xE8);
            writeCMOS(0x20A3, 0x03);
            writeCMOS(0x20A4, 0x4D);
            writeCMOS(0x20A5, 0x01);
            writeCMOS(0x20A6, 0x4D);
            writeCMOS(0x20A7, 0x01);
            writeCMOS(0x20A8, 0xE8);
            writeCMOS(0x20A9, 0x03);
            writeCMOS(0x20AA, 0xE8);
            writeCMOS(0x20AB, 0x03);
            writeCMOS(0x20AC, 0x4D);
            writeCMOS(0x20AD, 0x01);
            writeCMOS(0x20AE, 0x4D);
            writeCMOS(0x20AF, 0x01);
            writeCMOS(0x20C7, 0xE8);
            writeCMOS(0x20C8, 0x03);
            writeCMOS(0x20C9, 0xE8);
            writeCMOS(0x20CA, 0x03);
            writeCMOS(0x20CB, 0x4D);
            writeCMOS(0x20CC, 0x01);
            writeCMOS(0x20CD, 0x4D);
            writeCMOS(0x20CE, 0x01);
            writeCMOS(0x20CF, 0xE8);
            writeCMOS(0x20D0, 0x03);
            writeCMOS(0x20D1, 0xE8);
            writeCMOS(0x20D2, 0x03);
            writeCMOS(0x20D3, 0x4D);
            writeCMOS(0x20D4, 0x01);
            writeCMOS(0x20D5, 0x4D);
            writeCMOS(0x20D6, 0x01);
            writeCMOS(0x2186, 0x01);
            writeCMOS(0x2187, 0xE8);
            writeCMOS(0x2188, 0x03);
            writeCMOS(0x218A, 0x18);
            writeCMOS(0x218B, 0xFC);
            writeCMOS(0x218C, 0x01);
            writeCMOS(0x21A2, 0x00);
            writeCMOS(0x2268, 0x00);
            writeCMOS(0x233E, 0x01);
            writeCMOS(0x802B, 0x1F);
            writeCMOS(0x802E, 0x1F);
            writeCMOS(0x8031, 0x1F);
            writeCMOS(0x8034, 0x1F);
            writeCMOS(0x8037, 0x1F);
            writeCMOS(0x803A, 0x1F);
            writeCMOS(0x803F, 0x23);
            writeCMOS(0x8045, 0x2B);
            writeCMOS(0x8048, 0x30);
            writeCMOS(0x804B, 0x3C);
            writeCMOS(0x8053, 0x0A);
            writeCMOS(0x8059, 0x05);
            writeCMOS(0x805F, 0x0A);
            writeCMOS(0x8067, 0x28);
            writeCMOS(0x8070, 0x46);
            writeCMOS(0x8073, 0x5A);
            writeCMOS(0x807B, 0x1E);
            writeCMOS(0x8081, 0x05);
            writeCMOS(0x8084, 0x14);
            writeCMOS(0x8087, 0x23);
            writeCMOS(0x809B, 0x43);
            writeCMOS(0x809C, 0x01);
            writeCMOS(0x809E, 0x43);
            writeCMOS(0x809F, 0x01);
            writeCMOS(0x80A3, 0x1F);
            writeCMOS(0x80A6, 0x1F);
            writeCMOS(0x80A9, 0x1F);
            writeCMOS(0x80AC, 0x1F);
            writeCMOS(0x80AF, 0x1F);
            writeCMOS(0x80B2, 0x1F);
            writeCMOS(0x8102, 0x50);
            writeCMOS(0x8105, 0x5A);
            writeCMOS(0x8108, 0x97);
            writeCMOS(0x810B, 0xA6);
            writeCMOS(0x810E, 0xBF);
            writeCMOS(0x8111, 0xC9);
            writeCMOS(0x812A, 0x56);
            writeCMOS(0x812D, 0x41);
            writeCMOS(0x8130, 0x3C);
            writeCMOS(0x8133, 0x05);
            writeCMOS(0x8136, 0x7D);
            writeCMOS(0x8139, 0x40);
            writeCMOS(0x813E, 0x56);
            writeCMOS(0x8144, 0x41);
            writeCMOS(0x8147, 0x27);
            writeCMOS(0x814A, 0x82);
            writeCMOS(0x815E, 0xB1);
            writeCMOS(0x8161, 0xB1);
            writeCMOS(0x850C, 0xE4);
            writeCMOS(0x850D, 0x83);
            writeCMOS(0x850E, 0x83);
            writeCMOS(0x850F, 0x83);
            writeCMOS(0x8510, 0xAE);
            writeCMOS(0x8511, 0xAE);
            writeCMOS(0x8512, 0xAE);
            writeCMOS(0x8513, 0xAE);
            writeCMOS(0x8518, 0x83);
            writeCMOS(0x8519, 0x83);
            writeCMOS(0x851A, 0x83);
            writeCMOS(0x851B, 0x83);
            writeCMOS(0x851C, 0xAE);
            writeCMOS(0x851D, 0xAE);
            writeCMOS(0x851E, 0xAE);
            writeCMOS(0x851F, 0x2E);
            writeCMOS(0x8524, 0x03);
            writeCMOS(0x8525, 0x03);
            writeCMOS(0x8526, 0x03);
            writeCMOS(0x8527, 0x03);
            writeCMOS(0x8528, 0x0E);
            writeCMOS(0x8529, 0x0E);
            writeCMOS(0x852A, 0x0E);
            writeCMOS(0x852B, 0x0E);
            writeCMOS(0x860A, 0x14);
            writeCMOS(0x8616, 0x14);
            writeCMOS(0x8622, 0x14);
            writeCMOS(0x862E, 0x14);
            writeCMOS(0x863A, 0x14);
            writeCMOS(0x8646, 0x14);
            writeCMOS(0x8652, 0x14);
            writeCMOS(0x865E, 0x14);
            writeCMOS(0x866A, 0x14);
            writeCMOS(0x8676, 0x14);
            writeCMOS(0x8682, 0x14);
            writeCMOS(0x868E, 0x14);
            writeCMOS(0x8720, 0x01);
            writeCMOS(0x8913, 0x0F);
            writeCMOS(0x897C, 0x40);
            writeCMOS(0x897D, 0x51);
            writeCMOS(0x897F, 0x50);
            writeCMOS(0x8980, 0x14);
            writeCMOS(0x8982, 0x14);
            writeCMOS(0x8986, 0xA2);
            writeCMOS(0x8988, 0xA0);
            writeCMOS(0x8989, 0x28);
            writeCMOS(0x898B, 0x28);
            writeCMOS(0x898C, 0x0A);
            writeCMOS(0x898F, 0x45);
            writeCMOS(0x8991, 0x40);
            writeCMOS(0x8992, 0x51);
            writeCMOS(0x8994, 0x50);
            writeCMOS(0x8995, 0x14);
            writeCMOS(0x8A14, 0x18);
            writeCMOS(0x8A16, 0x18);
            writeCMOS(0x8A18, 0x17);
            writeCMOS(0x8A1A, 0x15);
            writeCMOS(0x8A1C, 0x15);
            writeCMOS(0x8A1E, 0x15);
            writeCMOS(0x8A20, 0x13);
            writeCMOS(0x8A22, 0x10);
            writeCMOS(0x8A24, 0x23);
            writeCMOS(0x8A26, 0x23);
            writeCMOS(0x8A28, 0x2A);
            writeCMOS(0x8A2A, 0x27);
            writeCMOS(0x8A2C, 0x18);
            writeCMOS(0x8A2E, 0x18);
            writeCMOS(0x8A30, 0x17);
            writeCMOS(0x8A32, 0x15);
            writeCMOS(0x8A34, 0x15);
            writeCMOS(0x8A36, 0x15);
            writeCMOS(0x8A38, 0x13);
            writeCMOS(0x8A3A, 0x10);
            writeCMOS(0x8A3C, 0x23);
            writeCMOS(0x8A3E, 0x23);
            writeCMOS(0x8A40, 0x2A);
            writeCMOS(0x8A42, 0x27);
            writeCMOS(0x8C0C, 0x20);
            writeCMOS(0x8C0D, 0x20);
            writeCMOS(0x8F06, 0x0A);
            writeCMOS(0x8F08, 0x15);
            writeCMOS(0x8F0A, 0x1C);
            writeCMOS(0x9031, 0x42);
            writeCMOS(0x903F, 0x00);
            writeCMOS(0x9040, 0x00);
            writeCMOS(0x906C, 0x40);
            writeCMOS(0x9070, 0x02);
            writeCMOS(0x908F, 0x81);
            writeCMOS(0x9092, 0x01);
            writeCMOS(0x9142, 0xFF);
            writeCMOS(0x914B, 0xFF);
            writeCMOS(0x9154, 0xFF);
            writeCMOS(0x915D, 0xFF);
            writeCMOS(0x915F, 0x00);
            writeCMOS(0x9162, 0x00);
            writeCMOS(0x9165, 0x00);
            writeCMOS(0x9168, 0x00);
            writeCMOS(0x916B, 0x00);
            writeCMOS(0x916E, 0x00);
            writeCMOS(0x9290, 0x00);
            writeCMOS(0xB51E, 0xF5);
            writeCMOS(0xB520, 0xF7);
            writeCMOS(0xB526, 0xF5);
            writeCMOS(0xB528, 0xF7);

            writeCMOS(0x2240, 0x01);//test pattern
            writeCMOS(0x00F0, 0x00);//STANDBY OFF	
   

            ushort hmax;
            ushort vmax;
            setIDLE();
            hmax = 3750;//3708        
            vmax = 13300;      
            setHMAX(hmax);
            setVMAX(vmax);
            releaseIDLE();



        }
        ////***************************************

        private void button95_Click(object sender, EventArgs e)
        {
            ushort x, y;
            x = (ushort)(readFPGA(0) * 256 + readFPGA(1));
            y = (ushort)(readFPGA(2) * 256 + readFPGA(3));
            button95.Text = "Detected Image Size x=" + x.ToString() + " y=" + y.ToString();
        }

        private void button96_Click(object sender, EventArgs e)
        {

            resetCMOS();
            Thread.Sleep(1000);
            initCMOS_IMX571(0x0a);
            Thread.Sleep(2000);
            wordAlign();
            Thread.Sleep(1000);
            AutoChannelAlign();
            Thread.Sleep(1000);
            writeFPGA(62, 0);//SET 16bit



        }

       


        void setSLVSEC_Decode(int i)
        {

            if (i == 16)
                writeFPGA(62, 0);
            else if (i == 14)
                writeFPGA(62, 1);
            else if (i == 12)
                writeFPGA(62, 2);
            else
                writeFPGA(62, 0);



        }

        private void button97_Click(object sender, EventArgs e)
        {
            resetCMOS();
            Thread.Sleep(1000);
            initCMOS_IMX410();
            Thread.Sleep(2000);
            wordAlign();
            Thread.Sleep(1000);
            AutoChannelAlign();
            Thread.Sleep(1000);

            setSLVSEC_Decode(14);

            writeFPGA(21, 255);//SET DIGITAL GAIN TO MAX
            writeFPGA(20, 255);//SET DIGITAL GAIN TO MAX
            writeFPGA(19, 255);//SET DIGITAL GAIN TO MAX
            writeFPGA(18, 255);//SET DIGITAL GAIN TO MAX

            setQHY410AnalogGain(4086);

            setQHY410OFFSET(20);



            setHMAX(911);
            setVMAX(5100);
            setQHY410SHR(2550);

            setIDLE();
            Thread.Sleep(100);
            releaseIDLE();
            enableDDR(true);

        }

        double readTemperature_ErisFPGA()
        {
            int C;
            writeFPGA(150, 0);
            writeFPGA(150, 1);
            Thread.Sleep(100);
            writeFPGA(150, 0);

            C = readFPGA(209) * 256 + readFPGA(208);


            double A = 693;
            double B = 265;
            double temperature;

            temperature = A * (double)C / 1024 - B;
            return temperature;

        }

        private void button99_Click(object sender, EventArgs e)
        {
            button99.Text = readTemperature_ErisFPGA().ToString();
        }

        private void button98_Click(object sender, EventArgs e)
        {
            resetDDR();
        }

        private void button100_Click(object sender, EventArgs e)
        {
            //  button100.ForeColor = Color.Blue;
            //  Application.DoEvents();

            ////  writeCMOS( 0x019e, 0x04 );
            //  writeCMOS( 0x0000, 0x01 );
            ////  writeFPGA( 49, 0 );



            //  Thread.Sleep( Convert.ToInt32( textBox14.Text ) * 1000 );




            ////  writeFPGA( 49, 1 );
            //  writeCMOS( 0x00, 0x00 );
            //  wordAlign();
            //  AutoChannelAlign();
            ////  writeCMOS( 0x019e, 0x00 );
            //  button100.ForeColor = Color.Red;

        }

        private void label57_Click(object sender, EventArgs e)
        {

        }

        private void button101_Click(object sender, EventArgs e)
        {


            resetCMOS();
            Thread.Sleep(1000);
            if (sensorModel == 455) initCMOS_IMX455(0x0b);
            else if (sensorModel == 571) initCMOS_IMX571(0x0b);



            Thread.Sleep(1000);
            wordAlign();
            Thread.Sleep(1000);
            AutoChannelAlign();
            Thread.Sleep(1000);


            // writeFPGA( 21, 8 );//SET DIGITAL GAIN TO MAX
            // writeFPGA( 20, 8 );//SET DIGITAL GAIN TO MAX
            // writeFPGA( 19, 8 );//SET DIGITAL GAIN TO MAX
            // writeFPGA( 18, 8 );//SET DIGITAL GAIN TO MAX

            // setQHY600AnalogGain( 3800 );

            // setQHY600OFFSET( 50 );

            // setQHY600SHR( 3490 );

            // writeCMOS( 0X0C4, 0X01 ); //test pattern 1
            setSLVSEC_DecodeBit(14);
        }

        private void button102_Click(object sender, EventArgs e)
        {


            resetCMOS();
            Thread.Sleep(1000);
            if (sensorModel == 455) initCMOS_IMX455(0x0c);
            else if (sensorModel == 571) initCMOS_IMX571(0x0c);



            Thread.Sleep(1000);
            wordAlign();
            Thread.Sleep(1000);
            AutoChannelAlign();
            Thread.Sleep(1000);


            // writeFPGA( 21, 8 );//SET DIGITAL GAIN TO MAX
            // writeFPGA( 20, 8 );//SET DIGITAL GAIN TO MAX
            // writeFPGA( 19, 8 );//SET DIGITAL GAIN TO MAX
            // writeFPGA( 18, 8 );//SET DIGITAL GAIN TO MAX

            // setQHY600AnalogGain( 3800 );

            // setQHY600OFFSET( 50 );

            // setQHY600SHR( 3490 );

            // writeCMOS( 0X0C4, 0X01 ); //test pattern 1

            setSLVSEC_DecodeBit(12);
        }


        void enableLockFrame(bool i)
        {
            if (i == true)
                writeFPGA(36, 1);
            else
                writeFPGA(36, 0);
        }

        void setLockFrame(ushort i)
        {
            writeFPGA(37, MSB(i));
            writeFPGA(38, LSB(i));
        }

        void setIgnoreFrame(byte i)
        {
            writeFPGA(55, i);
        }

        void setSleepFrames(ushort i)
        {
            writeFPGA(56, MSB(i));
            writeFPGA(57, LSB(i));
        }

        private void button103_Click(object sender, EventArgs e)
        {
            setIDLE();

            int m = 168;


            if (m == 367)
            {

                setVMAX(5100);          //for QHY168C
                ushort vmax3 = 5050 - 8 - 2;        //for QHY367PRO-C

                writeFPGA(59, MSB(vmax3));
                writeFPGA(60, LSB(vmax3));


                ushort vmax1 = 5050 - 8;     //5042
                writeFPGA(50, MSB(vmax1));
                writeFPGA(51, LSB(vmax1));

                ushort vmax2 = 58 - 30;           //5042
                writeFPGA(52, MSB(vmax2));
                writeFPGA(53, LSB(vmax2));


                ushort patchNumber = 0x400;
                setPatchNumber(patchNumber);

                enableLockFrame(true);

                setIgnoreFrame(1);


            }


            else if (m == 168)
            {




                // setVMAX( 5100 );          //for QHY168C
                setVMAX(3408);          //for QHY168C
                // ushort vmax3 = 5050 - 8 - 2;        //for QHY367PRO-C

                ushort vmax3 = 3400;       //for QHY168C
                writeFPGA(59, MSB(vmax3));
                writeFPGA(60, LSB(vmax3));


                ushort vmax1 = 3382 - 12;     //3370
                writeFPGA(50, MSB(vmax1));
                writeFPGA(51, LSB(vmax1));

                ushort vmax2 = 8 + 50 - 30;       //3350
                writeFPGA(52, MSB(vmax2));
                writeFPGA(53, LSB(vmax2));


                ushort patchNumber = 0x400;
                setPatchNumber(patchNumber);

                enableLockFrame(true);

                setIgnoreFrame(1);

            }

        }

      

        private void button106_Click(object sender, EventArgs e)
        {

            //AutoChannelAlign();
        }

        private void button105_Click(object sender, EventArgs e)
        {

            //byte[] command = new byte[ 10 ];
            //UInt32 command_length = 10;
            byte[] result = new byte[10];
            UInt32 result_length = 0;



            string c;
            c = "setvmax3aa";
            byte[] command = Encoding.ASCII.GetBytes(c);
            UInt32 command_length = (UInt32)c.Length;


            UInt32 ret = ASCOM.QHYCCD.libqhyccd.SetQHYCCDAdvancedCommand(camhandle, command_length, command, ref result_length, result);






            string x;
            x = Encoding.ASCII.GetString(result);

            richTextBox1.AppendText("ret=" + ret.ToString() + "results=");
            richTextBox1.AppendText(x);
            richTextBox1.AppendText(Environment.NewLine);




        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
        void EnableLiveModeAntiRBI(bool value)
        {
            UInt16 vmax2;

            if (SDKAPI == false)
            {
                if (value == true)
                    vmax2 = 0x1c00;
                else
                    vmax2 = 0;


                writeFPGA(45, MSB(vmax2));
                writeFPGA(46, LSB(vmax2));

            }

            else
            {
                if (value == true)
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDEnableLiveModeAntiRBI(camhandle, 0x1c00);
                else
                    ASCOM.QHYCCD.libqhyccd.SetQHYCCDEnableLiveModeAntiRBI(camhandle, 0);

            }
        }


        private void button109_Click(object sender, EventArgs e)
        {
            ASCOM.QHYCCD.libqhyccd.InitQHYCCD(camhandle);
        }

        private void button112_Click(object sender, EventArgs e)
        {


            writeCMOS(0x0200, 0x01);	// standby	
            writeCMOS(0x0213, 0x00);
            writeCMOS(0x021d, 0x00);
            writeCMOS(0x0228, 0x30);
            writeCMOS(0x02bc, 0x30);
            writeCMOS(0x02be, 0x45);
            writeCMOS(0x02bf, 0x40);
            writeCMOS(0x02c0, 0x01);
            writeCMOS(0x02c2, 0xa0);
            writeCMOS(0x02c6, 0x01);
            writeCMOS(0x02D2, 0x05);
            writeCMOS(0x02d7, 0x00);
            writeCMOS(0x027e, 0x08);

            writeCMOS(0x0412, 0x40);
            writeCMOS(0x0413, 0x40);
            writeCMOS(0x041a, 0x0F);
            writeCMOS(0x0458, 0x3C);
            writeCMOS(0x0567, 0x04);
            writeCMOS(0x0568, 0x22);
            writeCMOS(0x056c, 0x05);
            writeCMOS(0x0573, 0x0c);
            writeCMOS(0x0575, 0x0B);
            writeCMOS(0x058f, 0x7c);

            //chip id 07
            writeCMOS(0x07b7, 0x04);
            writeCMOS(0x07c5, 0x85);
            writeCMOS(0x07d5, 0x5a);

            //chip id 08
            writeCMOS(0x0825, 0x10);
            writeCMOS(0x082b, 0xe0);
            writeCMOS(0x082c, 0x0a);
            writeCMOS(0x0830, 0xaf);
            writeCMOS(0x0831, 0x10);

            // mode setting for Normal mode,37.125MHZ
            writeCMOS(0x0205, 0x00);	// LVDS 8ch
            writeCMOS(0x0214, 0x00);	// ADC10Bit
            writeCMOS(0x0215, 0x00);	// Drive mode,0h,WUXGA,4h,1080p
            //	0x0217,0x8e	// VMAX
            //	0x0218,0x0c	// VMAX
            //	0x021a,0x68	// HMAX
            //	0x021b,0x01	// HMAX
            writeCMOS(0x021c, 0x10);// oportsel
            writeCMOS(0x021f, 0x00);	// CKSEL,1,WUXGA,0,1080p
            writeCMOS(0x0221, 0x00);	// FREQ,data rate
            writeCMOS(0x0228, 0x30);
            writeCMOS(0x022e, 0x00);	// XVS/XHS OUTSEL
            writeCMOS(0x0292, 0x10);
            writeCMOS(0x0293, 0x04);
            writeCMOS(0x0294, 0x10);
            writeCMOS(0x0295, 0x04);
            writeCMOS(0x02a0, 0xa4);	// GTWAIT
            writeCMOS(0x02a5, 0x08);	// GSDLY
            writeCMOS(0x02a9, 0x0c);// 8CH LVDS OUT TIMING
            writeCMOS(0X0200, 0X00);


            writeCMOS(0x0292, 0x10);
            writeCMOS(0x0293, 0x00);
            writeCMOS(0x0294, 0x20);
            writeCMOS(0x021a, 0x70);

        }

        private void button113_Click(object sender, EventArgs e)
        {

            //LowLevelReadD5( 0x0008 );

            byte[] xdata = new byte[64];

            for (int i = 0; i < 64; i++)
            {
                xdata[i] = 0;
            }
            richTextBox1.Clear();



            LowLevelReadD5(0x0002, xdata);
            richTextBox1.AppendText("has humidity sensor" + xdata[0].ToString());
            richTextBox1.AppendText(Environment.NewLine);



            for (int i = 0; i < 64; i++)
            {
                xdata[i] = 0;
            }



            LowLevelReadD5(0x0007, xdata);
            richTextBox1.AppendText("has pressure sensor:" + xdata[0].ToString());
            richTextBox1.AppendText(Environment.NewLine);


            for (int i = 0; i < 64; i++)
            {
                xdata[i] = 0;
            }


            LowLevelReadD5(0x0001, xdata);


            UInt16 humidity;
            humidity = (UInt16)(xdata[0] * 256 + xdata[1]);
            double humidity_final;
            humidity_final = (double)humidity / 100;

            richTextBox1.AppendText("humidity value" + humidity_final.ToString("f") + "%");
            richTextBox1.AppendText(Environment.NewLine);


            for (int i = 0; i < 64; i++)
            {
                xdata[i] = 0;
            }

            LowLevelReadD5(0x0008, xdata);

            UInt16 pressure;
            pressure = (UInt16)(xdata[0] * 256 + xdata[1]);
            double pressure_final;
            pressure_final = (double)pressure / 10;


            richTextBox1.AppendText("pressure value:" + pressure_final.ToString("f"));
            richTextBox1.AppendText(Environment.NewLine);

            UInt16 temperature;
            temperature = (UInt16)(xdata[2] * 256 + xdata[3]);
            double temperature_final;
            temperature_final = ((double)temperature - 27315) / 100;


            richTextBox1.AppendText("temperature value:" + temperature_final.ToString("f"));
            richTextBox1.AppendText(Environment.NewLine);
        }

        private void vScrollBar0_Scroll(object sender, ScrollEventArgs e)
        {
            //   vScrollBar0.Enabled = false;
            setDelay(0, (byte)vScrollBar0.Value);
            //  vScrollBar0.Enabled = true;
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            //   vScrollBar1.Enabled = false;
            setDelay(1, (byte)vScrollBar1.Value);
            //  vScrollBar1.Enabled = true;
        }

        private void vScrollBar2_Scroll(object sender, ScrollEventArgs e)
        {
            //  vScrollBar2.Enabled = false;
            setDelay(2, (byte)vScrollBar2.Value);
            //   vScrollBar2.Enabled = true;
        }

        private void vScrollBar3_Scroll(object sender, ScrollEventArgs e)
        {
            //   vScrollBar3.Enabled = false;
            setDelay(3, (byte)vScrollBar3.Value);
            //   vScrollBar3.Enabled = true;
        }

        private void vScrollBar4_Scroll(object sender, ScrollEventArgs e)
        {
            //  vScrollBar4.Enabled = false;
            setDelay(4, (byte)vScrollBar4.Value);
            // vScrollBar4.Enabled = true;
        }

        private void vScrollBar5_Scroll(object sender, ScrollEventArgs e)
        {
            // vScrollBar5.Enabled = false;
            setDelay(5, (byte)vScrollBar5.Value);
            // vScrollBar5.Enabled = true;
        }

        private void vScrollBar6_Scroll(object sender, ScrollEventArgs e)
        {
            //vScrollBar6.Enabled = false;
            setDelay(6, (byte)vScrollBar6.Value);
            //vScrollBar6.Enabled = true;
        }

        private void vScrollBar7_Scroll(object sender, ScrollEventArgs e)
        {
            //   vScrollBar7.Enabled = false;
            setDelay(7, (byte)vScrollBar7.Value);
            // vScrollBar7.Enabled = true;
        }

        private void vScrollBar8_Scroll(object sender, ScrollEventArgs e)
        {
            //  vScrollBar8.Enabled = false;
            setDelay(8, (byte)vScrollBar8.Value);
            // vScrollBar8.Enabled = true;
        }

        private void vScrollBar9_Scroll(object sender, ScrollEventArgs e)
        {
            //  vScrollBar9.Enabled = false;
            setDelay(9, (byte)vScrollBar9.Value);
            //  vScrollBar9.Enabled = true;
        }

        private void vScrollBar10_Scroll(object sender, ScrollEventArgs e)
        {
            //vScrollBar10.Enabled = false;
            setDelay(10, (byte)vScrollBar10.Value);
            // vScrollBar10.Enabled = true;
        }

        private void vScrollBar11_Scroll(object sender, ScrollEventArgs e)
        {
            // vScrollBar11.Enabled = false;
            setDelay(11, (byte)vScrollBar11.Value);
            // vScrollBar11.Enabled = true;
        }

        private void vScrollBar12_Scroll(object sender, ScrollEventArgs e)
        {
            // vScrollBar12.Enabled = false;
            setDelay(12, (byte)vScrollBar12.Value);
            //  vScrollBar12.Enabled = true;
        }

        private void vScrollBar13_Scroll(object sender, ScrollEventArgs e)
        {
            //vScrollBar13.Enabled = false;
            setDelay(13, (byte)vScrollBar13.Value);
            //vScrollBar13.Enabled = true;
        }

        private void vScrollBar14_Scroll(object sender, ScrollEventArgs e)
        {
            // vScrollBar14.Enabled = false;
            setDelay(14, (byte)vScrollBar14.Value);
            //vScrollBar14.Enabled = true;
        }

        private void vScrollBar15_Scroll(object sender, ScrollEventArgs e)
        {
            // vScrollBar15.Enabled = false;
            setDelay(15, (byte)vScrollBar15.Value);
            // vScrollBar15.Enabled = true;
        }

        private void button116_Click(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            ushort value = 0;
            ushort index = 0;
            int total_channles = 16;



            index = 0x100;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd4, value, index, 64, xdata);
            for (int i = 0; i < total_channles; i++)
                InputDelayValue[i] = xdata[i];


            vScrollBar0.Value = InputDelayValue[0];
            vScrollBar1.Value = InputDelayValue[1];
            vScrollBar2.Value = InputDelayValue[2];
            vScrollBar3.Value = InputDelayValue[3];
            vScrollBar4.Value = InputDelayValue[4];
            vScrollBar5.Value = InputDelayValue[5];
            vScrollBar6.Value = InputDelayValue[6];
            vScrollBar7.Value = InputDelayValue[7];

            vScrollBar8.Value = InputDelayValue[8];
            vScrollBar9.Value = InputDelayValue[9];
            vScrollBar10.Value = InputDelayValue[10];
            vScrollBar11.Value = InputDelayValue[11];
            vScrollBar12.Value = InputDelayValue[12];
            vScrollBar13.Value = InputDelayValue[13];
            vScrollBar14.Value = InputDelayValue[14];
            vScrollBar15.Value = InputDelayValue[15];



        }

        private void button117_Click(object sender, EventArgs e)
        {
            LowLevelA1(0);

            button116.PerformClick();
            LowLevelReadD4();

        }

        private void button118_Click(object sender, EventArgs e)
        {
            LowLevelA1(1);
            button116.PerformClick();
            LowLevelReadD4();
        }

        private void button119_Click(object sender, EventArgs e)
        {
            LowLevelA1(2);
            button116.PerformClick();
            LowLevelReadD4();
        }

        private void button120_Click(object sender, EventArgs e)
        {
            LowLevelA1(3);
            button116.PerformClick();
            LowLevelReadD4();
        }

        private void button114_Click(object sender, EventArgs e)
        {

            //SET TO THE 6PIN GPS MODE AND ENABLE THE OUTPUT
            writeFPGA(142, 1);        //ENABLE OUTPUT
            writeFPGA(39, 1);          //SET TO 6PIN GPS MODE
            writeFPGA(56, 1);    //enable frame number
            writeFPGA(35, 1);    //frame counter begin to increase
            writeFPGA(58, 2);    //enable frame number


        }

        private void hScrollBar10_Scroll_1(object sender, ScrollEventArgs e)
        {

        }

        private void hScrollBar10_QHY4040HDR_Scroll(object sender, ScrollEventArgs e)
        {

            if (hScrollBar10_QHY4040HDR.Value == 0)
                setHDRMode(1);
            else
                setHDRMode(2);

        }


        void writeQmmRegister10GTranceiver(UInt32 address, UInt32 data)
        {

            writeFPGA(230, 0X02);  //set reset to non-reset status

            writeFPGA(220, MSB3(address));
            writeFPGA(221, MSB2(address));
            writeFPGA(222, MSB1(address));
            writeFPGA(223, MSB0(address));

            writeFPGA(224, MSB3(data));
            writeFPGA(225, MSB2(data));
            writeFPGA(226, MSB1(data));
            writeFPGA(227, MSB0(data));


            //generate the writen signal
            writeFPGA(228, 0x02);
            writeFPGA(228, 0x00);

        }


        void writeQmmRegisterDSP(UInt32 address, UInt32 data)
        {
            writeFPGA(220, MSB3(address));
            writeFPGA(221, MSB2(address));
            writeFPGA(222, MSB1(address));
            writeFPGA(223, MSB0(address));

            writeFPGA(224, MSB3(data));
            writeFPGA(225, MSB2(data));
            writeFPGA(226, MSB1(data));
            writeFPGA(227, MSB0(data));


            //generate the writen signal
            writeFPGA(228, 0x01);
            writeFPGA(228, 0x00);

        }


        void setDSP(double x, double al, double ah, double bl, double bh)
        {




            UInt32 xx, aah, aal, bbh, bbl;
            xx = (UInt32)x;
            aal = (UInt32)(al * 256);
            aah = (UInt32)(ah * 256);
            bbl = (UInt32)((bl + 30000.0) * 256);
            bbh = (UInt32)((bh + 30000.0) * 256);


            writeQmmRegisterDSP(0, xx);
            writeQmmRegisterDSP(1, aah);
            writeQmmRegisterDSP(2, aal);
            writeQmmRegisterDSP(3, bbh);
            writeQmmRegisterDSP(4, bbl);



            richTextBox1.AppendText("SetDSP  X=" + xx.ToString() + Environment.NewLine);
            richTextBox1.AppendText("ah=" + ah.ToString() + "         ah*256=" + aah.ToString() + Environment.NewLine);
            richTextBox1.AppendText("al=" + al.ToString() + "         al*256=" + aal.ToString() + Environment.NewLine);
            richTextBox1.AppendText("bh=" + bh.ToString() + "           bh*256=" + bbh.ToString() + Environment.NewLine);
            richTextBox1.AppendText("bl=" + bl.ToString() + "           bl*256=" + bbl.ToString() + Environment.NewLine);




        }

        private void button121_Click(object sender, EventArgs e)
        {


            UInt16 X;
            double al, ah, bl, bh;


            X = Convert.ToUInt16(textBoxDSPX.Text);
            al = Convert.ToDouble(textBoxDSPal.Text);
            ah = Convert.ToDouble(textBoxDSPah.Text);
            bl = Convert.ToDouble(textBoxDSPbl.Text);
            bh = Convert.ToDouble(textBoxDSPbh.Text);


            if (SDKAPI == false)
            {
                setDSP((double)X, al, ah, bl, bh);
            }

            else
            {
                ASCOM.QHYCCD.libqhyccd.SetQHYCCDTwoChannelCombineParameter(camhandle, (double)X, ah, bh, al, bl);
            }

        }

        

        private void textBox18_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox17_TextChanged(object sender, EventArgs e)
        {

        }


        void setTrigControllerExpTimeMAX10(UInt32 exptime)
        {
            writeFPGA2(144, 0);
            writeFPGA2(145, MSB3(exptime));
            writeFPGA2(146, MSB2(exptime));
            writeFPGA2(147, MSB1(exptime));
            writeFPGA2(148, MSB0(exptime));
        }

        void setTrigControllerExpTimeTitan(UInt32 exptime)
        {
            writeFPGA(144, 0);
            writeFPGA(145, MSB3(exptime));
            writeFPGA(146, MSB2(exptime));
            writeFPGA(147, MSB1(exptime));
            writeFPGA(148, MSB0(exptime));
        }


        private void button125_Click(object sender, EventArgs e)
        {
            writeCMOS(0X0213, 0X00);  //enable cmos trig mode 
        }

        private void buttonGPSBOXTOGPS_Click(object sender, EventArgs e)
        {
            writeFPGA(4, 0);
            labelBDGPS.Text = "Current is GPS mode";
        }

        private void buttonGPSBOXTOBD_Click(object sender, EventArgs e)
        {
            writeFPGA(4, 1);
            labelBDGPS.Text = "Current is BD mode";
        }

        private void button127_Click(object sender, EventArgs e)
        {
            writeCMOS(0x0200, 0x01);	// standby	
            writeCMOS(0x0213, 0x00);
            writeCMOS(0x021d, 0x00);
            writeCMOS(0x0228, 0x30);
            writeCMOS(0x02bc, 0x30);
            writeCMOS(0x02be, 0x45);
            writeCMOS(0x02bf, 0x40);
            writeCMOS(0x02c0, 0x01);
            writeCMOS(0x02c2, 0xa0);
            writeCMOS(0x02c6, 0x01);
            writeCMOS(0x02D2, 0x05);
            writeCMOS(0x02d7, 0x00);
            writeCMOS(0x027e, 0x08);

            writeCMOS(0x0412, 0x40);
            writeCMOS(0x0413, 0x40);
            writeCMOS(0x041a, 0x0F);
            writeCMOS(0x0458, 0x3C);
            writeCMOS(0x0567, 0x04);
            writeCMOS(0x0568, 0x22);
            writeCMOS(0x056c, 0x05);
            writeCMOS(0x0573, 0x0c);
            writeCMOS(0x0575, 0x0B);
            writeCMOS(0x058f, 0x7c);

            //chip id 07
            writeCMOS(0x07b7, 0x04);
            writeCMOS(0x07c5, 0x85);
            writeCMOS(0x07d5, 0x5a);

            //chip id 08
            writeCMOS(0x0825, 0x10);
            writeCMOS(0x082b, 0xe0);
            writeCMOS(0x082c, 0x0a);
            writeCMOS(0x0830, 0xaf);
            writeCMOS(0x0831, 0x10);

            // mode setting for Normal mode,37.125MHZ
            writeCMOS(0x0205, 0x00);	// LVDS 8ch
            writeCMOS(0x0214, 0x00);	// ADC10Bit
            writeCMOS(0x0215, 0x00);	// Drive mode,0h,WUXGA,4h,1080p
            //	0x0217,0x8e	// VMAX
            //	0x0218,0x0c	// VMAX
            //	0x021a,0x68	// HMAX
            //	0x021b,0x01	// HMAX
            writeCMOS(0x021c, 0x10);// oportsel
            writeCMOS(0x021f, 0x00);	// CKSEL,1,WUXGA,0,1080p
            writeCMOS(0x0221, 0x00);	// FREQ,data rate
            writeCMOS(0x0228, 0x30);
            writeCMOS(0x022e, 0x00);	// XVS/XHS OUTSEL
            writeCMOS(0x0292, 0x10);
            writeCMOS(0x0293, 0x04);
            writeCMOS(0x0294, 0x10);
            writeCMOS(0x0295, 0x04);
            writeCMOS(0x02a0, 0xa4);	// GTWAIT
            writeCMOS(0x02a5, 0x08);	// GSDLY
            writeCMOS(0x02a9, 0x0c);// 8CH LVDS OUT TIMING
            writeCMOS(0X0200, 0X00);


            writeCMOS(0x0292, 0x0f);       //CKADJ  ORGINAL IS 0X10, but it has a banding on left .  change to 0X0F will reduce a little frame rate but no this banding
            writeCMOS(0x0293, 0x00);
            writeCMOS(0x0294, 0x20);
            writeCMOS(0x021a, 0x63);     //HMAX LSB  , original is 0x68,  but with the CKADJ=0X10, it can be reduce a little more to 0x63 to increase some frame rate. final speed is 154fps
        }

        private void label82_Click(object sender, EventArgs e)
        {

        }

        private void hScrollBar10_Scroll_2(object sender, ScrollEventArgs e)
        {

        }

        private void button129_Click(object sender, EventArgs e)
        {

            richTextBox1.Clear();


            for (ushort j = 0; j < 15; j++)
            {

                Application.DoEvents();
                //select lane
                writeFPGA(88, j);




                //reset detector
                writeFPGA(95, 0);
                writeFPGA(95, 1);



                //
                richTextBox1.AppendText(Environment.NewLine);

                int value = 0;


                for (ushort i = 0; i < 23; i++)
                {
                    writeFPGA(91, i);     //select bit position

                    value = 256 * readFPGA(51) + readFPGA(52);
                    richTextBox1.AppendText(value.ToString() + " ");
                }
            }


            for (ushort j = 99; j < 100; j++)
            {

                Application.DoEvents();
                //select lane
                writeFPGA(88, j);




                //reset detector
                writeFPGA(95, 0);
                writeFPGA(95, 1);



                //
                richTextBox1.AppendText(Environment.NewLine);

                int value = 0;


                for (ushort i = 0; i < 23; i++)
                {
                    writeFPGA(91, i);     //select bit position

                    value = 256 * readFPGA(51) + readFPGA(52);
                    richTextBox1.AppendText(value.ToString() + " ");
                }
            }

        }

        private void button130_Click(object sender, EventArgs e)
        {


            UInt32 address;
            UInt32 value;

            address = Convert.ToUInt32(textBoxTranceiverRegisterAddress.Text, 16);
            value = Convert.ToUInt32(textBoxTranceiverRegisterValue.Text, 16);

            writeQmmRegister10GTranceiver(address, value);



        }

        private void button133_Click(object sender, EventArgs e)
        {
            UInt32 year, month, day, subday;
            year = 0;
            month = 0;
            day = 0;
            subday = 0;
            ASCOM.QHYCCD.libqhyccd.GetQHYCCDSDKVersion(ref year, ref month, ref day, ref subday);
            richTextBox1.AppendText("uint32_t GetQHYCCDSDKVersion(uint32_t *year,uint32_t *month,uint32_t *day,uint32_t *subday)" + "\r");
            richTextBox1.AppendText("ret=" + ret.ToString() + " year-month-day-subday" + year.ToString() + "-" + month.ToString() + "-" + day.ToString() + "-" + subday.ToString() + "\r");

        }

        private void button135_Click(object sender, EventArgs e)
        {
            textBoxDSPX.Text = "0";
            textBoxDSPah.Text = "0";
            textBoxDSPal.Text = "16";
            textBoxDSPbh.Text = "30000";
            textBoxDSPbl.Text = "30000";

            buttonSetDSP.PerformClick();


        }

        private void button136_Click(object sender, EventArgs e)
        {
            textBoxDSPX.Text = "4096";
            textBoxDSPah.Text = "16";
            textBoxDSPal.Text = "0";
            textBoxDSPbh.Text = "30000";
            textBoxDSPbl.Text = "30000";

            buttonSetDSP.PerformClick();

        }

        private void button138_Click(object sender, EventArgs e)
        {
            textBoxDSPX.Text = "4000";
            textBoxDSPah.Text = "1.0";
            textBoxDSPal.Text = "11.22254";
            textBoxDSPbh.Text = "0";
            textBoxDSPbl.Text = "-400";

            //todo : need to set the analog gain to fixed value.


            buttonSetDSP.PerformClick();
        }

        private void button121_Click_1(object sender, EventArgs e)
        {
            initCMOS_IMX533();

        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void button142_Click(object sender, EventArgs e)
        {
            initCMOS_IMX492();
        }

        private void button147_Click(object sender, EventArgs e)
        {

        }
        private void button145_Click(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            LowLevelReadD5(0x0001, xdata);

            ushort humidity;
            humidity = (ushort)(xdata[0] * 256 + xdata[1]);

            double humidity_double;

            humidity_double = ((double)humidity) / 100;

            richTextBox1.AppendText(humidity_double.ToString() + "%");
        }
       

        private void button157_Click(object sender, EventArgs e)
        {
            writeCMOS(0X300B, 0X00);  //enable cmos trig mode 
        }

        private void buttonSerialPortOpen_Click(object sender, EventArgs e)
        {
            //counter_timer = 0;
            timer1.Enabled = false;
        }

        private void button143_Click(object sender, EventArgs e)
        {
            byte command;

            command = Convert.ToByte(textBoxLLAE.Text);
            LowLevelAE(command);
        }

        private void button158_Click(object sender, EventArgs e)
        {
            //button158.Enabled = false;

            byte[] powerData = null;

            //int i;
            //  for( i = 0; i < 100; i++ )
            // {
            // writeCMOS( 0x019e, 0x04 );
            //writeCMOS( 0x0000, 0x01 );
            //      Thread.Sleep( 10 );
            //  }

            /*
            Thread.Sleep( 1500 );

            writeCMOS( 0x043e, 0x01 );
            writeCMOS( 0x0443, 0x01 );
            writeCMOS( 0x052e, 0x01 );
            writeCMOS( 0x0505, 0x10 );
            writeCMOS( 0x0501, 0x00 );
            writeCMOS( 0x0506, 0x00 );
            writeCMOS( 0x0522, 0x30 );
            writeCMOS(0x0525,0x03);
            writeCMOS(0x0528,0x03);
            writeCMOS(0x052b,0x03);
            writeCMOS(0x045c,0x03);

            writeCMOS( 0x019e, 0x04);
            writeCMOS( 0X01A7, 0X00 );
             */
            writeFPGA(49, 0);
            //powerData = Encoding.ASCII.GetBytes( "VSET1:1.8" );

            //SendData( powerData );


            Thread.Sleep(96 * 1000);
            // writeCMOS( 0x0000, 0x00 );
            // writeCMOS( 0x019e, 0x00 );

            // writeCMOS( 0x019e, 0x01 );
            /*
            writeCMOS( 0x019e, 0x00 );


            writeCMOS( 0x0522, 0x00 );
            writeCMOS( 0x0525, 0x00 );
            writeCMOS( 0x0528, 0x00 );
            writeCMOS( 0x052b, 0x00 );
            writeCMOS( 0x045c, 0x00 );
            writeCMOS( 0x043e, 0x03 );
            writeCMOS( 0x0443, 0x03 );
            writeCMOS( 0x052e, 0x00 );
            writeCMOS( 0x0505, 0x00 );
            writeCMOS( 0x0501, 0x08 );
            writeCMOS( 0x0506, 0xff );

             */
            writeFPGA(49, 1);
            //powerData = Encoding.ASCII.GetBytes( "VSET1:4.6" );
            //SendData( powerData );
            // writeCMOS( 0X01A7, 0X03 );

            /*
            byte[] xdata = new byte[ 64 ];
            LowLevelReadD5( 0x000b, xdata );
             */

            // button158.Enabled = true;
        }

        private void button159_Click(object sender, EventArgs e)
        {
            writeCMOS(0x0000, 0x0001);
            writeFPGA(49, 0);
            Thread.Sleep(5 * 1000);
            writeCMOS(0x0000, 0x0000);
            writeFPGA(49, 1);

            byte[] xdata = new byte[64];
            LowLevelReadD5(0x000b, xdata);

        }

        private void button160_Click(object sender, EventArgs e)
        {
            //button158.Enabled = false;

            byte[] powerData = null;

            //int i;

            writeFPGA(49, 0);
            powerData = Encoding.ASCII.GetBytes("VSET1:2.5");

            SendData(powerData);


            Thread.Sleep(196 * 1000);

            writeFPGA(49, 1);
            powerData = Encoding.ASCII.GetBytes("VSET1:4.6");
            SendData(powerData);

            //button158.Enabled = true;
        }

        private void button165_Click(object sender, EventArgs e)
        {
            byte trig_en;
            byte trig_in_mode;
            UInt16 fiter_times;
            byte trig_out_mode;
            byte trig_in_source;

            trig_en = Convert.ToByte(textBoxLLAD_MODE.Text);//textBox40
            trig_in_mode = Convert.ToByte(textBox40.Text);//textBox40
            fiter_times = Convert.ToUInt16(textBox31.Text);
            trig_in_source = Convert.ToByte(textBox33.Text);
            trig_out_mode = Convert.ToByte(textBox32.Text);


            LowLevelAC(trig_en, trig_in_mode, fiter_times, trig_in_source, trig_out_mode);
        }

        private void label89_Click(object sender, EventArgs e)
        {

        }
   
        private void textBoxStartX_TextChanged(object sender, EventArgs e)
        {

        }

        private void button174_Click(object sender, EventArgs e)
        {
            UInt32 ret;
            ret = ASCOM.QHYCCD.libqhyccd.BeginQHYCCDLive(camhandle);
            richTextBox1.AppendText("uint32_t STDCALL BeginQHYCCDLive(qhyccd_handle *handle)");
            richTextBox1.AppendText(Environment.NewLine);
            richTextBox1.AppendText(" ret=" + ret.ToString());
            richTextBox1.AppendText(Environment.NewLine);
        }

        private void button177_Click(object sender, EventArgs e)
        {
            SDK_LIVESTOP = true;
        }

        private void button179_Click(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            LowLevelReadD5(0x000e, xdata);

            UInt32 PixelPeriod_ps;  //unit: ns*1000
            UInt32 linePeroid_ns;   //unit:  ns
            UInt32 framePeriod_us;
            UInt32 hmax;
            UInt32 vmax;
            UInt32 actualExpTime;
            byte isLongExpMode;


            PixelPeriod_ps = (uint)(xdata[0] * 256 * 256 * 256 + xdata[1] * 256 * 256 + xdata[2] * 256 + xdata[3]);
            linePeroid_ns = (uint)(xdata[4] * 256 * 256 * 256 + xdata[5] * 256 * 256 + xdata[6] * 256 + xdata[7]);
            framePeriod_us = (uint)(xdata[8] * 256 * 256 * 256 + xdata[9] * 256 * 256 + xdata[10] * 256 + xdata[11]);
            hmax = (uint)(xdata[12] * 256 * 256 * 256 + xdata[13] * 256 * 256 + xdata[14] * 256 + xdata[15]);
            vmax = (uint)(xdata[16] * 256 * 256 * 256 + xdata[17] * 256 * 256 + xdata[18] * 256 + xdata[19]);
            actualExpTime = (uint)(xdata[20] * 256 * 256 * 256 + xdata[21] * 256 * 256 + xdata[22] * 256 + xdata[23]);
            isLongExpMode = xdata[32];

            richTextBox1.AppendText("PixelPeriod_ps=" + PixelPeriod_ps.ToString() + Environment.NewLine);
            richTextBox1.AppendText("linePeroid_ns=" + linePeroid_ns.ToString() + Environment.NewLine);
            richTextBox1.AppendText("framePeriod_us=" + framePeriod_us.ToString() + Environment.NewLine);
            richTextBox1.AppendText("hmax=" + hmax.ToString() + Environment.NewLine);
            richTextBox1.AppendText("vmax=" + vmax.ToString() + Environment.NewLine);
            richTextBox1.AppendText("actualExpTime(us)=" + actualExpTime.ToString() + Environment.NewLine);
            richTextBox1.AppendText("isLongExpMode=" + isLongExpMode.ToString() + Environment.NewLine);
        }

        private void button180_Click(object sender, EventArgs e)
        {
            UInt32 PixelPeriod_ps = 0;  //unit: ns*1000
            UInt32 LinePeroid_ns = 0;   //unit:  ns
            UInt32 FramePeriod_us = 0;
            UInt32 ClocksPerLine = 0;
            UInt32 LinesPerFrame = 0;
            UInt32 ActualExpTime = 0;
            byte isLongExpMode = 0;

            UInt32 ret;
            ret = ASCOM.QHYCCD.libqhyccd.GetQHYCCDPreciseExposureInfo(camhandle, ref PixelPeriod_ps, ref  LinePeroid_ns, ref FramePeriod_us, ref ClocksPerLine, ref LinesPerFrame, ref ActualExpTime, ref isLongExpMode);

            richTextBox1.AppendText(" EXPORTFUNC uint32_t STDCALL GetQHYCCDPreciseExposureInfo(qhyccd_handle *h,uint32_t *PixelPeriod_ps,uint32_t *LinePeriod_ns,uint32_t *FramePeriod_us,uint32_t *ClocksPerLine, uint32_t *LinesPerFrame,uint32_t *ActualExposureTime,uint8_t  *isLongExposureMode)  " + Environment.NewLine);
            richTextBox1.AppendText("PixelPeriod_ps=" + PixelPeriod_ps.ToString() + Environment.NewLine);
            richTextBox1.AppendText("linePeroid_ns=" + LinePeroid_ns.ToString() + Environment.NewLine);
            richTextBox1.AppendText("framePeriod_us=" + FramePeriod_us.ToString() + Environment.NewLine);
            richTextBox1.AppendText("hmax=" + ClocksPerLine.ToString() + Environment.NewLine);
            richTextBox1.AppendText("vmax=" + LinesPerFrame.ToString() + Environment.NewLine);
            richTextBox1.AppendText("ActualExpTime(us)=" + ActualExpTime.ToString() + Environment.NewLine);
            richTextBox1.AppendText("isLongExpMode=" + isLongExpMode.ToString() + Environment.NewLine);

            richTextBox1.AppendText("ret=" + ret.ToString() + Environment.NewLine);

        }



        private void button181_Click(object sender, EventArgs e)
        {
            initCMOS_IMX485_10BIT();

        }

        private void button182_Click(object sender, EventArgs e)
        {
            initCMOS_IMX492_MIPI_12BIT();
        }

        private void button183_Click(object sender, EventArgs e)
        {
            //AspectratioofALL
            //ModeNo.:1
            //SensorMode:All-pixelscanmode(AD10-bit,12-bitlengthoutput)
            //ViewAngleMode:8192x5556
            //Interface:CSI-24-lane
            //Framerate:1.45
            //DataRate:720Mbps
            //
            //Setting1
            //
            //1-1\

            writeFPGA(49, 1); // ampv voltage return to default 2.9V

            writeFPGA(11, 3);

            writeFPGA(0, 0);
            Thread.Sleep(10);
            writeFPGA(0, 1);

            writeCMOS(0x3033, 0x10);
            //1-2
            writeCMOS(0x303C, 0x02);
            //1-3PLLsetting
            writeCMOS(0x311F, 0x00);//PLRD10
            writeCMOS(0x3122, 0x02);//PLRD2
            writeCMOS(0x3123, 0x01);//PLRD11
            writeCMOS(0x3124, 0x00);//PLRD12
            writeCMOS(0x3125, 0x01);//PLRD13
            writeCMOS(0x3127, 0x02);//PLRD14
            writeCMOS(0x3129, 0x90);//PLRD3
            writeCMOS(0x312A, 0x02);//PLRD4
            writeCMOS(0x312D, 0x02);//PLRD15
            writeCMOS(0x31E8, 0xf0);//PLRD1
            writeCMOS(0x31E9, 0x00);//PLRD1
            writeCMOS(0x3ac4, 0x01);//MIPI_HALF_EN
            //1-4Standbyrelease
            writeCMOS(0x3000, 0x12);
            //1-5PLLrelease
            writeCMOS(0x310B, 0x00);
            //1-6.InitilizeCommunication
            writeCMOS(0x3004, 0x1C);//MDSEL1
            writeCMOS(0x3005, 0x01);//MDSEL2
            writeCMOS(0x3006, 0x00);//MDSEL3
            writeCMOS(0x3007, 0xA7);//MDSEL4
            writeCMOS(0x300E, 0x00);//SVR
            writeCMOS(0x300F, 0x00);//SVR
            writeCMOS(0x302C, 0x0F);//SHR
            writeCMOS(0x302D, 0x00);//SHR
            writeCMOS(0x3042, 0x32);//BLKLEVEL
            writeCMOS(0x3047, 0x01);//
            writeCMOS(0x304E, 0x0B);//
            writeCMOS(0x304F, 0x2A);//
            writeCMOS(0x3062, 0x25);//
            writeCMOS(0x3064, 0x78);//
            writeCMOS(0x3065, 0x33);//
            writeCMOS(0x3068, 0x44);//
            writeCMOS(0x3067, 0x71);//
            writeCMOS(0x3081, 0x00);//
            writeCMOS(0x3084, 0x00);//HCOUNT1
            writeCMOS(0x3085, 0x00);//HCOUNT1
            writeCMOS(0x3086, 0x00);//HCOUNT2
            writeCMOS(0x3087, 0x00);//HCOUNT2
            writeCMOS(0x3088, 0x75);//
            writeCMOS(0x308A, 0x09);//
            writeCMOS(0x308C, 0x61);//
            writeCMOS(0x30A9, 0x40);//VMAX
            writeCMOS(0x30AA, 0x17);//VMAX
            writeCMOS(0x30AB, 0x00);//VMAX
            writeCMOS(0x30AC, 0xB0);//HMAX
            writeCMOS(0x30AD, 0x0a);//HMAX
            writeCMOS(0x30E5, 0x00);//
            writeCMOS(0x30EF, 0x01);//
            writeCMOS(0x312F, 0x20);//OPB_SIZE_V
            writeCMOS(0x3130, 0x30);//WRITE_VSIZE
            writeCMOS(0x3131, 0x16);//WRITE_VSIZE
            writeCMOS(0x3132, 0x10);//Y_OUT_SIZE
            writeCMOS(0x3133, 0x16);//Y_OUT_SIZE
            writeCMOS(0x31F5, 0x01);//
            writeCMOS(0x3146, 0x00);//
            writeCMOS(0x3234, 0x32);//
            writeCMOS(0x3248, 0xBC);//
            writeCMOS(0x3250, 0xBC);//
            writeCMOS(0x3258, 0xBC);//
            writeCMOS(0x3260, 0xBC);//
            writeCMOS(0x3274, 0x13);//
            writeCMOS(0x3276, 0x00);//
            writeCMOS(0x3277, 0x00);//
            writeCMOS(0x327C, 0x13);//
            writeCMOS(0x327E, 0x00);//
            writeCMOS(0x327F, 0x00);//
            writeCMOS(0x3284, 0x13);//
            writeCMOS(0x3286, 0x00);//
            writeCMOS(0x3287, 0x00);//
            writeCMOS(0x328C, 0x13);//
            writeCMOS(0x328E, 0x00);//
            writeCMOS(0x328F, 0x00);//
            writeCMOS(0x328E, 0x00);//
            writeCMOS(0x328F, 0x00);//
            writeCMOS(0x32AE, 0x00);//
            writeCMOS(0x32AF, 0x00);//
            writeCMOS(0x32CA, 0x5a);//
            writeCMOS(0x332C, 0x00);//PSSLVS1=Vblk
            writeCMOS(0x332D, 0x00);//PSSLVS1=Vblk
            writeCMOS(0x332F, 0x00);//
            writeCMOS(0x334A, 0x00);//PSSLVS2=Vblk
            writeCMOS(0x334B, 0x00);//PSSLVS2=Vblk
            writeCMOS(0x334C, 0x01);//
            writeCMOS(0x335A, 0x79);//
            writeCMOS(0x335E, 0x56);//
            writeCMOS(0x3360, 0x6A);//
            writeCMOS(0x336A, 0x56);//
            writeCMOS(0x33D6, 0x79);//
            writeCMOS(0x340C, 0x6E);//
            writeCMOS(0x3448, 0x7E);//
            writeCMOS(0x348E, 0x6F);//
            writeCMOS(0x3492, 0x11);//
            writeCMOS(0x34C4, 0x5A);//
            writeCMOS(0x3506, 0x56);//
            writeCMOS(0x350C, 0x56);//
            writeCMOS(0x350e, 0x58);//
            writeCMOS(0x353D, 0x10);//
            writeCMOS(0x3549, 0x04);//
            writeCMOS(0x355D, 0x03);//
            writeCMOS(0x355E, 0x03);//
            writeCMOS(0x3574, 0x56);//
            writeCMOS(0x357F, 0x0C);//
            writeCMOS(0x3580, 0x0A);//
            writeCMOS(0x3581, 0x0a);//
            writeCMOS(0x3583, 0x75);//
            writeCMOS(0x3587, 0x01);//
            writeCMOS(0x35B6, 0x00);//PSSLVS3=Vblk
            writeCMOS(0x35B7, 0x00);//PSSLVS3=Vblk
            writeCMOS(0x35B8, 0x00);//PSSLVS4=Vblk-5
            writeCMOS(0x35B9, 0x00);//PSSLVS4=Vblk-5
            writeCMOS(0x35D0, 0x5E);//
            writeCMOS(0x35D4, 0x63);//
            writeCMOS(0x35E5, 0x9A);//
            writeCMOS(0x366A, 0x1a);//
            writeCMOS(0x366B, 0x16);//
            writeCMOS(0x366C, 0x10);//
            writeCMOS(0x366D, 0x09);//
            writeCMOS(0x366E, 0x00);//
            writeCMOS(0x366F, 0x00);//
            writeCMOS(0x3670, 0x00);//
            writeCMOS(0x3671, 0x00);//
            writeCMOS(0x3676, 0x83);//
            writeCMOS(0x3677, 0x03);//
            writeCMOS(0x3678, 0x00);//
            writeCMOS(0x3679, 0x04);//
            writeCMOS(0x367A, 0x2C);//
            writeCMOS(0x367B, 0x05);//
            writeCMOS(0x367D, 0x06);//
            writeCMOS(0x367E, 0x00);//
            writeCMOS(0x3680, 0x4B);//
            writeCMOS(0x3688, 0x05);//
            writeCMOS(0x3690, 0x27);//
            writeCMOS(0x3692, 0x65);//
            writeCMOS(0x3694, 0x4F);//
            writeCMOS(0x3696, 0xA1);//
            writeCMOS(0x36BC, 0x00);//PSSLVS0=Vblk
            writeCMOS(0x36BD, 0x00);//PSSLVS0=Vblk
            writeCMOS(0x371C, 0x02);//
            writeCMOS(0x372F, 0x3C);//
            writeCMOS(0x3730, 0x01);//
            writeCMOS(0x3732, 0xB8);//
            writeCMOS(0x3744, 0x0F);//
            writeCMOS(0x375B, 0x01);//
            writeCMOS(0x382B, 0x68);//
            writeCMOS(0x3836, 0x34);//
            writeCMOS(0x38B3, 0x00);//
            writeCMOS(0x3A43, 0x00);//
            writeCMOS(0x3A54, 0x00);//
            writeCMOS(0x3A55, 0x1E);//
            writeCMOS(0x3C00, 0x01);//
            writeCMOS(0x3C01, 0x01);//
            writeCMOS(0x3E80, 0x14);//
            writeCMOS(0x3E82, 0x30);//
            writeCMOS(0x3E84, 0x04);//
            writeCMOS(0x3E85, 0x01);//
            writeCMOS(0x3E86, 0x10);//
            writeCMOS(0x3E87, 0x16);//
            writeCMOS(0x3E88, 0x03);//
            writeCMOS(0x3E89, 0xFE);//
            writeCMOS(0x3E8A, 0x01);//
            writeCMOS(0x3E8B, 0x06);//
            writeCMOS(0x3E8E, 0x03);//
            writeCMOS(0x3E8F, 0xFE);//
            writeCMOS(0x3E90, 0x01);//
            writeCMOS(0x3E91, 0x06);//
            writeCMOS(0x3E94, 0x33);//
            writeCMOS(0x3E95, 0x01);//
            writeCMOS(0x3E96, 0x19);//
            writeCMOS(0x3E98, 0x30);//
            writeCMOS(0x3E9A, 0x09);//
            writeCMOS(0x3E9C, 0x10);//
            writeCMOS(0x3E9D, 0x16);//
            writeCMOS(0x3E9E, 0xFE);//
            writeCMOS(0x3E9F, 0x03);//
            writeCMOS(0x3EA0, 0x06);//
            writeCMOS(0x3EA3, 0x01);//
            writeCMOS(0x3EA4, 0xFE);//
            writeCMOS(0x3EA5, 0x03);//
            writeCMOS(0x3EA6, 0x06);//
            writeCMOS(0x3EA9, 0x33);//
            writeCMOS(0x3EAA, 0x00);//
            writeCMOS(0x3EAB, 0x08);//
            writeCMOS(0x3EAC, 0x08);//
            writeCMOS(0x3EAD, 0x01);//
            writeCMOS(0x3EAE, 0x08);//
            writeCMOS(0x3EAF, 0x08);//
            writeCMOS(0x3EB0, 0x00);//
            writeCMOS(0x3EB1, 0x10);//
            writeCMOS(0x3EB2, 0x10);//
            writeCMOS(0x3EB3, 0x01);//
            writeCMOS(0x3EB4, 0x10);//
            writeCMOS(0x3EB5, 0x10);//
            writeCMOS(0x3EB6, 0x00);//
            writeCMOS(0x3EB7, 0x00);//
            writeCMOS(0x3EB8, 0x00);//
            writeCMOS(0x3EB9, 0x00);//
            writeCMOS(0x3EBA, 0x00);//
            writeCMOS(0x3EBB, 0x00);//
            writeCMOS(0x3EC0, 0x54);//
            writeCMOS(0x3ECC, 0x04);//
            writeCMOS(0x3ECD, 0x04);//
            writeCMOS(0x3ED0, 0xF0);//
            writeCMOS(0x3ED1, 0x20);//
            writeCMOS(0x3ED2, 0x0B);//
            writeCMOS(0x3ED3, 0x04);//
            writeCMOS(0x3ED5, 0x13);//
            writeCMOS(0x3ED6, 0x00);//
            writeCMOS(0x3ED9, 0x0F);//
            writeCMOS(0x3EE4, 0x02);//
            writeCMOS(0x3EE5, 0x02);//
            writeCMOS(0x3EE7, 0x00);//
            writeCMOS(0x3EF6, 0x00);//
            writeCMOS(0x3EF8, 0x10);//
            writeCMOS(0x3EFA, 0x00);//
            writeCMOS(0x3EFC, 0x10);//
            writeCMOS(0x3134, 0x77);//tclk_post
            writeCMOS(0x3135, 0x00);//
            writeCMOS(0x3136, 0x67);//ths_zero_min
            writeCMOS(0x3137, 0x00);//
            writeCMOS(0x3138, 0x37);//ths_prepare
            writeCMOS(0x3139, 0x00);//
            writeCMOS(0x313A, 0x37);//tclk_trail_min
            writeCMOS(0x313B, 0x00);//
            writeCMOS(0x313C, 0x37);//ths_trail
            writeCMOS(0x313D, 0x00);//
            writeCMOS(0x313E, 0xDF);//tclk_zero
            writeCMOS(0x313F, 0x00);//
            writeCMOS(0x3140, 0x37);//tclk_prepare
            writeCMOS(0x3141, 0x00);//
            writeCMOS(0x3142, 0x2F);//tlpx
            writeCMOS(0x3143, 0x00);//
            //LPCSetting
            //Vblk=VMAXx(1+SVR)-MinXVS=
            //V=
            //MinXVS=
            Thread.Sleep(10);
            //Setting2
            //2-1StandbyforSTBDVrelease
            writeCMOS(0x3000, 0x02);
            //2-2
            writeCMOS(0x35E5, 0x92);
            writeCMOS(0x35E5, 0x9A);
            //2-3StandbyforSTBLOGICrelease
            writeCMOS(0x3000, 0x00);
            //
            Thread.Sleep(7);
            //Setting3
            //3-1XMSTA
            writeCMOS(0x3001, 0x10);
            //3-2SYNCDRV


            writeCMOS(0x3000, 0x02);              //2.1
            writeCMOS(0x35e5, 0x92);             //2,2
            writeCMOS(0x35e5, 0x9a);               //2.3
            writeCMOS(0x3000, 0x00);                //2.4



            writeCMOS(0x3033, 0x20);   //3.1
            writeCMOS(0x3017, 0xa8);    //3.2




            ushort hmax, vmax;
            hmax = 2000;
            vmax = 7000;
            writeCMOS(0x30ac, LSB(hmax));
            writeCMOS(0x30ad, MSB(hmax));

            writeCMOS(0x30ab, MSB2(vmax));
            writeCMOS(0x30aa, MSB1(vmax));
            writeCMOS(0x30a9, MSB0(vmax));



        }

        private void button184_Click(object sender, EventArgs e)
        {
            writeCMOS(0x3031, 0x01);
            writeCMOS(0x3032, 0x01);

            writeFPGA(2, 1);  //decode 12bit mode


            ushort vmax, hmax;
            vmax = 2450;
            hmax = 1080;

            writeCMOS(0x3026, MSB2(vmax));
            writeCMOS(0x3025, MSB1(vmax));
            writeCMOS(0x3024, MSB0(vmax));

            writeCMOS(0x3028, LSB(hmax));
            writeCMOS(0x3029, MSB(hmax));

        }

        private void button185_Click(object sender, EventArgs e)
        {
            //   initCMOS_IMX485_10BIT_2X4CH();
            initCMOS_IMX485_10BIT_8CH();
        }

        void setHCROP_6060TEST(ushort XSTART, ushort XEND)
        {
            writeFPGA(149, MSB(XSTART));
            writeFPGA(150, LSB(XSTART));
            writeFPGA(151, MSB(XEND));
            writeFPGA(152, LSB(XEND));
        }



        private void button186_Click(object sender, EventArgs e)
        {



            setVMAX(6000);
            setHCROP_6060TEST(10, 40);

            ushort pixelPerLine = 6000;
            ushort ImageContextSize = 2000;

            writeFPGA(41, MSB(pixelPerLine));
            writeFPGA(42, LSB(pixelPerLine));
            writeFPGA(43, MSB(ImageContextSize));
            writeFPGA(44, LSB(ImageContextSize));

            setIDLE();
            releaseIDLE();
        }

        private void button187_Click(object sender, EventArgs e)
        {
            ushort pixelPerLine = 2048;
            ushort ImageContextSize = 2000;

            writeFPGA(41, MSB(pixelPerLine));
            writeFPGA(42, LSB(pixelPerLine));
            writeFPGA(43, MSB(ImageContextSize));
            writeFPGA(44, LSB(ImageContextSize));

            setIDLE();
            releaseIDLE();
        }

        private void button188_Click(object sender, EventArgs e)
        {
            ushort pixelPerLine = 6000;
            ushort ImageContextSize = 1999;

            writeFPGA(41, MSB(pixelPerLine));
            writeFPGA(42, LSB(pixelPerLine));
            writeFPGA(43, MSB(ImageContextSize));
            writeFPGA(44, LSB(ImageContextSize));

            setIDLE();
            releaseIDLE();
        }

        private void button189_Click(object sender, EventArgs e)
        {
            ushort pixelPerLine = 500;
            ushort ImageContextSize = 2000;

            writeFPGA(41, MSB(pixelPerLine));
            writeFPGA(42, LSB(pixelPerLine));
            writeFPGA(43, MSB(ImageContextSize));
            writeFPGA(44, LSB(ImageContextSize));

            setIDLE();
            releaseIDLE();

        }


        private void button200_Click(object sender, EventArgs e)
        {
            ushort x, y;
            x = (ushort)(readFPGA(1) * 256 + readFPGA(0));
            y = (ushort)(readFPGA(3) * 256 + readFPGA(2));
            richTextBox1.AppendText("Detected Image Size x=" + x.ToString() + " y=" + y.ToString());

        }

        private void button201_Click(object sender, EventArgs e)
        {
            writeFPGA(30, 1);//ddr en 

            writeFPGA(1, 1);//ResetDDR
            writeFPGA(1, 0);//ResetDDR
            writeFPGA(1, 1);//ResetDDR

            writeFPGA(63, 0);//ClearDDR
            writeFPGA(63, 1);//ClearDDR
            writeFPGA(63, 0);//ClearDDR

            writeFPGA(56, 0);//reg43 [15:8]
            writeFPGA(56, 1);//reg43 [15:8]
            writeFPGA(56, 0);//reg43 [15:8]
        }


        private void button203_Click(object sender, EventArgs e)
        {
            UInt32 ret;
            uint length = 2048;  //16k

            byte[] read_data;
            byte[] addtionalPacket = new byte[2048];
            byte[] LineBuffer = new byte[12000];

            rawArray = new byte[6000 * 6000 * 2];

            read_data = new byte[2048];

            richTextBox1.Clear();

            uint total_read;

            UInt32 x;
            x = (UInt32)(readFPGA(4) + readFPGA(5) * 256 + readFPGA(6) * 256 * 256 + readFPGA(7) * 256 * 256 * 256);
            x = x * 32;

            total_read = x / 2048;


            richTextBox1.AppendText("total_read=" + total_read.ToString() + Environment.NewLine);



            bool isFrameHead = false;
            uint packetCounter = 0;

            long k = 0;
            long s = 0;
            long m = 0;



            while (k < total_read)
            {
                ret = ASCOM.QHYCCD.libqhyccd.C_QHYCCDReadUSB_SYNC(camhandle, 0x81, length, read_data, 200);
                k++;


                if (isFrameHead == true)
                {
                    packetCounter = packetCounter + 1;
                    if (packetCounter == 1)
                    {

                        Array.Copy(read_data, 0, addtionalPacket, 0, ret);
                        richTextBox1.AppendText("copy addtionalPacket" + Environment.NewLine);

                        richTextBox1.AppendText("=========Addtional Infomtion Packet 0-25=========" + Environment.NewLine);
                        for (int i = 0; i < 25; i++)
                        {
                            richTextBox1.AppendText(addtionalPacket[i].ToString("x") + " ");
                        }

                        richTextBox1.AppendText(Environment.NewLine);

                        richTextBox1.AppendText("Camera Work Status:" + addtionalPacket[4].ToString("x") + Environment.NewLine);
                        richTextBox1.AppendText("Error Code:" + addtionalPacket[5].ToString("x") + Environment.NewLine);
                        richTextBox1.AppendText("Working Mode:" + addtionalPacket[6].ToString("x") + Environment.NewLine);
                        richTextBox1.AppendText("Sensor Temperature:" + ((double)(addtionalPacket[7] * 256 + addtionalPacket[8]) * 0.0625 * 0.8317 - 54.631).ToString("f") + Environment.NewLine);
                        richTextBox1.AppendText("FPGA Temperature:" + ((double)(addtionalPacket[9] * 256 + addtionalPacket[10]) * 503.975 / 4096.0 - 273.15).ToString("f") + Environment.NewLine);
                        richTextBox1.AppendText("Gain:" + addtionalPacket[11].ToString("x") + Environment.NewLine);
                        richTextBox1.AppendText("Scattering Identity Word:" + (addtionalPacket[12] * 256 + addtionalPacket[13]).ToString("x") + Environment.NewLine);
                        richTextBox1.AppendText("Exposure Start Time:" + addtionalPacket[14].ToString("x") + addtionalPacket[15].ToString("x") + addtionalPacket[16].ToString("x") + addtionalPacket[17].ToString("x") + addtionalPacket[18].ToString("x") + addtionalPacket[19].ToString("x") + Environment.NewLine);
                        richTextBox1.AppendText("Exposure End Time:" + addtionalPacket[20].ToString("x") + addtionalPacket[21].ToString("x") + addtionalPacket[22].ToString("x") + addtionalPacket[23].ToString("x") + addtionalPacket[24].ToString("x") + addtionalPacket[25].ToString("x") + Environment.NewLine);

                        richTextBox1.AppendText(Environment.NewLine);
                        for (int i = 26; i < 2048; i++)
                        {
                            richTextBox1.AppendText(addtionalPacket[i].ToString("x") + " ");
                        }

                        richTextBox1.AppendText(Environment.NewLine);
                        richTextBox1.AppendText("===========END OF Addtional Info Packet==========" + Environment.NewLine);
                    }

                    else
                    {
                        if (m < 6000 * 6000 * 2)
                        {

                            Array.Copy(read_data, 40, rawArray, m, 2000);
                            m = m + 2000;
                        }

                    }



                }

                if (m < 50000) richTextBox1.AppendText(ret.ToString() + " ");

                if ((read_data[ret - 1] == 0x22) && (read_data[ret - 2] == 0xdd) && (read_data[ret - 3] == 0x11) && (read_data[ret - 4] == 0xee))
                {
                    isFrameHead = true;

                    richTextBox1.AppendText("found frame head");
                    Application.DoEvents();
                }





            }

            richTextBox1.AppendText("read finished");
            Application.DoEvents();


            /*
             ret = ASCOM.QHYCCD.libqhyccd.C_QHYCCDReadUSB_SYNC( camhandle, 0x81, length, read_data, 0 );
             richTextBox1.AppendText("ret="+ret.ToString() + " " );
            
             for( int i = 0; i < ret; i++ )
             {
                 richTextBox1.AppendText( read_data[ i ].ToString( "x" ) + " " );
             }
            
             */




            bitmap = new Bitmap((int)6000, (int)6000);
            rectangle = new Rectangle(0, 0, (int)6000, (int)6000);
            bmpData = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            ptr = bmpData.Scan0;

            s = 0;
            index = 0;
            pixData = 0;
            h = 6000;
            x = 6000;
            rgbArray = new Byte[6000 * 6000 * 4];
            for (int i = 0; i < h; i++)
            {
                for (int y = 0; y < x; y++)
                {
                    rgbArray[s] = rawArray[index + 0];
                    rgbArray[s + 1] = rawArray[index + 0];
                    rgbArray[s + 2] = rawArray[index + 0];
                    rgbArray[s + 3] = 255;

                    s += 4;
                    index += 2;
                }
            }

            Marshal.Copy(rgbArray, 0, ptr, (int)(6000 * 6000 * 4));

            bitmap.UnlockBits(bmpData);

            //pictureBox1.Image = bitmap;




        }


        void gettrigftame()
        {
            setIDLE();
            Thread.Sleep(200);
            releaseIDLE();
        }



        private void button204_Click(object sender, EventArgs e)
        {
            trig_mode = 0;

            writeFPGA(45, 0X00);
            writeFPGA(46, 0X00);//VMAX_2 ==0 ; 
            writeFPGA(58, trig_mode);//filter disable , RBI disable,mode b disable,

            writeFPGA(39, 2);//set to mode 2
            writeFPGA(142, 1);//TrigSignalEn

            writeFPGA(50, 1);
            writeFPGA(51, 0);
            writeFPGA(52, 3);

            writeFPGA(57, 1);

            //set filter times 100ms
            writeFPGA(144, 0x00);
            writeFPGA(145, 0x00);
            writeFPGA(146, 0x26);
            writeFPGA(147, 0x25);
            writeFPGA(148, 0xA0);

            gettrigftame();
            Thread.Sleep(1000);
            gettrigftame();
        }

        private void button205_Click(object sender, EventArgs e)
        {
            gettrigftame();
        }

        private void label91_Click(object sender, EventArgs e)
        {

        }


        private void mode6_ysk_Click(object sender, EventArgs e)
        {
            writeFPGA(39, 6);
        }


        private void button204_Click_1(object sender, EventArgs e)
        {
            writeFPGA(125, 1);
        }

        private void button205_Click_1(object sender, EventArgs e)
        {
            writeFPGA(125, 0);
        }


        private void button208_Click(object sender, EventArgs e)
        {
            writeFPGA(0x0B, 0x2);             //reg0置1， CMOS复位信号XCLR拉高
            writeFPGA(0x0, 0x0);             //reg0置1， CMOS复位信号XCLR拉高
            writeFPGA(0x0, 0x1);             //reg0置1， CMOS复位信号XCLR拉高
            Thread.Sleep(200);

            writeCMOS(0x3008, 0x7F);    // BCWAIT_TIME[9:0]
            writeCMOS(0x300A, 0x5B);    // CPWAIT_TIME[9:0]
            writeCMOS(0x3028, 0x4C);    // HMAX[15:0]
            writeCMOS(0x3029, 0x04);    // 
            writeCMOS(0x3033, 0x05);    // SYS_MODE[3:0]
            writeCMOS(0x3050, 0x27);    // SHR0[19:0]
            writeCMOS(0x3051, 0x06);    // 
            writeCMOS(0x3090, 0x14);    // GAIN_PCG_0[8:0]
            writeCMOS(0x30C1, 0x00);    // XVS_DRV[1:0]
            writeCMOS(0x3116, 0x24);    // INCKSEL2[7:0]
            writeCMOS(0x311E, 0x24);    // INCKSEL5[7:0]
            writeCMOS(0x32D4, 0x21);    // -
            writeCMOS(0x32EC, 0xA1);    // -
            writeCMOS(0x3452, 0x7F);    // -
            writeCMOS(0x3453, 0x03);    // -
            writeCMOS(0x358A, 0x04);    // -
            writeCMOS(0x35A1, 0x02);    // -
            writeCMOS(0x36BC, 0x0C);    // -
            writeCMOS(0x36CC, 0x53);    // -
            writeCMOS(0x36CD, 0x00);    // -
            writeCMOS(0x36CE, 0x3C);    // -
            writeCMOS(0x36D0, 0x8C);    // -
            writeCMOS(0x36D1, 0x00);    // -
            writeCMOS(0x36D2, 0x71);    // -
            writeCMOS(0x36D4, 0x3C);    // -
            writeCMOS(0x36D6, 0x53);    // -
            writeCMOS(0x36D7, 0x00);    // -
            writeCMOS(0x36D8, 0x71);    // -
            writeCMOS(0x36DA, 0x8C);    // -
            writeCMOS(0x36DB, 0x00);    // -
            writeCMOS(0x3724, 0x02);    // -
            writeCMOS(0x3726, 0x02);    // -
            writeCMOS(0x3732, 0x02);    // -
            writeCMOS(0x3734, 0x03);    // -
            writeCMOS(0x3736, 0x03);    // -
            writeCMOS(0x3742, 0x03);    // -
            writeCMOS(0x3862, 0xE0);    // -
            writeCMOS(0x38CC, 0x30);    // -
            writeCMOS(0x38CD, 0x2F);    // -
            writeCMOS(0x395C, 0x0C);    // -
            writeCMOS(0x3A42, 0xD1);    // -
            writeCMOS(0x3A4C, 0x77);    // -
            writeCMOS(0x3AE0, 0x02);    // -
            writeCMOS(0x3AEC, 0x0C);    // -
            writeCMOS(0x3B00, 0x2E);    // -
            writeCMOS(0x3B06, 0x29);    // -
            writeCMOS(0x3B98, 0x25);    // -
            writeCMOS(0x3B99, 0x21);    // -
            writeCMOS(0x3B9B, 0x13);    // -
            writeCMOS(0x3B9C, 0x13);    // -
            writeCMOS(0x3B9D, 0x13);    // -
            writeCMOS(0x3B9E, 0x13);    // -
            writeCMOS(0x3BA1, 0x00);    // -
            writeCMOS(0x3BA2, 0x06);    // -
            writeCMOS(0x3BA3, 0x0B);    // -
            writeCMOS(0x3BA4, 0x10);    // -
            writeCMOS(0x3BA5, 0x14);    // -
            writeCMOS(0x3BA6, 0x18);    // -
            writeCMOS(0x3BA7, 0x1A);    // -
            writeCMOS(0x3BA8, 0x1A);    // -
            writeCMOS(0x3BA9, 0x1A);    // -
            writeCMOS(0x3BAC, 0xED);    // -
            writeCMOS(0x3BAD, 0x01);    // -
            writeCMOS(0x3BAE, 0xF6);    // -
            writeCMOS(0x3BAF, 0x02);    // -
            writeCMOS(0x3BB0, 0xA2);    // -
            writeCMOS(0x3BB1, 0x03);    // -
            writeCMOS(0x3BB2, 0xE0);    // -
            writeCMOS(0x3BB3, 0x03);    // -
            writeCMOS(0x3BB4, 0xE0);    // -
            writeCMOS(0x3BB5, 0x03);    // -
            writeCMOS(0x3BB6, 0xE0);    // -
            writeCMOS(0x3BB7, 0x03);    // -
            writeCMOS(0x3BB8, 0xE0);    // -
            writeCMOS(0x3BBA, 0xE0);    // -
            writeCMOS(0x3BBC, 0xDA);    // -
            writeCMOS(0x3BBE, 0x88);    // -
            writeCMOS(0x3BC0, 0x44);    // -
            writeCMOS(0x3BC2, 0x7B);    // -
            writeCMOS(0x3BC4, 0xA2);    // -
            writeCMOS(0x3BC8, 0xBD);    // -
            writeCMOS(0x3BCA, 0xBD);    // -
            writeCMOS(0x4004, 0x48);    // TXCLKESC_FREQ[15:0]
            writeCMOS(0x4005, 0x09);    // 
            writeCMOS(0x400C, 0x00);    // INCKSEL6
            writeCMOS(0x4018, 0x7F);    // TCLKPOST[15:0]
            writeCMOS(0x401A, 0x37);    // TCLKPREPARE[15:0]
            writeCMOS(0x401C, 0x37);    // TCLKTRAIL[15:0]
            writeCMOS(0x401E, 0xF7);    // TCLKZERO[15:0]
            writeCMOS(0x401F, 0x00);    // 
            writeCMOS(0x4020, 0x3F);    // THSPREPARE[15:0]
            writeCMOS(0x4022, 0x6F);    // THSZERO[15:0]
            writeCMOS(0x4024, 0x3F);    // THSTRAIL[15:0]
            writeCMOS(0x4026, 0x5F);    // THSEXIT[15:0]
            writeCMOS(0x4028, 0x2F);    // TLPX[15:0]
            writeCMOS(0x4074, 0x01);    // INCKSEL7 [2:0]

            writeCMOS(0x3000, 0x00);
            Thread.Sleep(100);
            writeCMOS(0x3002, 0x00);
        }

      

      

        private void button211_Click(object sender, EventArgs e)
        {
            counter_timer = 0;
            timer1.Enabled = true;
        }

        private void button213_Click(object sender, EventArgs e)
        {
            writeFPGA_EXtend(282, 0x03);//write trigmode reg 

            //writeFPGA(57, 1);//set burst mode
            //writeFPGA(50, 1);//burst start
            //writeFPGA(51, 0);//*****
            //writeFPGA(52, 3);//burst end

            //setPatchNumber(32001);//Supplement package

            if (checkBox21.Checked == true)
            {
                writeFPGA_EXtend(284, 0x02);//mode b 
                LowLevelAB(0X01);//Set Minimum frame period
            }
            else
            {
                writeFPGA_EXtend(284, 0x03);//mode a
                writeFPGA_EXtend(287, 0x64);//trigoutlong   default:100us 
            }
        }

        private void button214_Click(object sender, EventArgs e)
        {
            writeFPGA(45, 0X00);
            writeFPGA(46, 0X00);//VMAX_2 ==0 ; 
            writeFPGA(57, 0);//EnableBurstMode
            writeFPGA(50, 1);
            writeFPGA(51, 0);
            writeFPGA(52, 3);
            setPatchNumber(0);//Supplement package
            writeFPGA_EXtend(282, 0x00);//write trigmode reg 

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label96_Click(object sender, EventArgs e)
        {

        }

        private void label94_Click(object sender, EventArgs e)
        {

        }

        private void button222_Click(object sender, EventArgs e)
        {
            //LowLevelA6(0x00);if ((trig_mode & 0x04) == 4)//mode B 
            if ((trig_mode & 0x04) == 4)//mode B {
            {
                byte trigout_mode;
                trigout_mode = (byte)(trig_mode & 0XF7);//mode b software trig in 
                writeFPGA(58, trigout_mode);//mode b 
                trig_mode = trigout_mode;
            }
            setIDLE();
            Thread.Sleep(1600);
            releaseIDLE();
        }



        private void textBoxSingleFrameStart_TextChanged(object sender, EventArgs e)
        {

        }

        private void button220_Click(object sender, EventArgs e)
        {

            UInt16 startFrame;

            writeFPGA(57, 1);//EnableBurstMode
            startFrame = Convert.ToUInt16(textBox26.Text, 10);

            writeFPGA(50, startFrame);

        }



        private void button221_Click(object sender, EventArgs e)
        {
            UInt16 EndFrame;
            EndFrame = (UInt16)((Convert.ToUInt16(textBox27.Text, 10)));

            writeFPGA(51, MSB(EndFrame));
            writeFPGA(52, LSB(EndFrame));

        }

        private void label102_Click(object sender, EventArgs e)
        {

        }

        private void button218_Click(object sender, EventArgs e)
        {
            UInt16 TIMES;
            TIMES = Convert.ToUInt16(textBox28.Text, 10);
         
            writeFPGA_EXtend(288, TIMES);
            

        }

        private void tabPageLowLevel3_Click(object sender, EventArgs e)
        {

        }

        private void checkBoxEnableDDR_CheckedChanged(object sender, EventArgs e)
        {

        }



        private void button216_Click(object sender, EventArgs e)
        {
            writeFPGA_EXtend(281, 0x02);//write trigsource      reg
            writeFPGA_EXtend(282, 0x01);//write trigmode        reg
            writeFPGA_EXtend(283, 0x02);//write triginfunction  reg
            //writeFPGA_EXtend(288, 0x02);//write filter          reg
            writeFPGA(57, 1);//set burst mode
            writeFPGA(50, 1);//burst start
            writeFPGA(51, 0);//*****
            writeFPGA(52, 3);//burst end

            setPatchNumber(32001);//Supplement package

        }

        private void button213_Click_1(object sender, EventArgs e)
        {
            trig_mode = 0;
            writeFPGA(58, trig_mode);//filter disable , RBI disable,mode a
            writeFPGA(45, 0X00);
            writeFPGA(46, 0X00);//VMAX_2 ==0 ; 

            writeFPGA(39, 2);
            writeFPGA(142, 1);

        }

        private void button224_Click(object sender, EventArgs e)
        {
            if (SDKAPI == false)
            {
                writeFPGA(35, 0);
                writeFPGA(35, 1);
            }
            else
            {
                ASCOM.QHYCCD.libqhyccd.ResetQHYCCDFrameCounter(camhandle);
            }
        }

        private void tabPageLowLevel2_Click(object sender, EventArgs e)
        {

        }

        private void checkBox17_CheckedChanged(object sender, EventArgs e)
        {
            byte trigout_mode;
            //richTextBox1.AppendText("mode  b  trig_mode =" + trig_mode.ToString() + Environment.NewLine);  
            if (checkBox17.Checked == true)
            {
                if ((trig_mode & 0x04) == 4)//mode B {
                {
                    trigout_mode = (byte)(trig_mode | 0x02);//mode b rbi en 
                    writeFPGA(58, trigout_mode);//mode b rbi en 
                    trig_mode = trigout_mode;
                }
                LowLevelAB(0x01);

                //richTextBox1.AppendText("mode  b  burst rbi en trig_mode =" + trig_mode.ToString() + Environment.NewLine);

            }
            else if ((trig_mode & 0x04) == 0)//mode a 
            {
                LowLevelAB(0x00);
                trig_mode = 0;
                writeFPGA(58, trig_mode);//mode b 
                //richTextBox1.AppendText("mode  a burst rbi disable trig_mode =" + trig_mode.ToString() + Environment.NewLine);  
            }
            else
            {
                trigout_mode = (byte)(trig_mode & 0xfd);//mode b rbi disable
                writeFPGA(58, trigout_mode);//mode b 
                trig_mode = trigout_mode;

                //richTextBox1.AppendText("mode  b  burst rbi disable trig_mode =" + trig_mode.ToString() + Environment.NewLine); 

            }


        }

        private void checkBox18_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox18.Checked == true)
                enableStampFrameCounter(true);
            else
                enableStampFrameCounter(false);

        }

     

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            byte trigout_mode;
            //  richTextBox1.AppendText("filter  trig_mode =" + trig_mode.ToString() + Environment.NewLine);
            if (checkBox15.Checked == true)
            {

                trigout_mode = (byte)(trig_mode | 0x01);//filter en 
                //     richTextBox1.AppendText("filter true trigout_mode =" + trigout_mode.ToString() + Environment.NewLine);
                writeFPGA(58, trigout_mode);//filter en 

            }
            else
            {

                trigout_mode = (byte)(trig_mode & 0xFE);//filter disable 
                writeFPGA(58, trigout_mode);//filter disable 
                //    richTextBox1.AppendText("filter false trigout_mode =" + trigout_mode.ToString() + Environment.NewLine);

            }
            trig_mode = trigout_mode;
        }

        private void button223_Click(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            ushort value = 0x000E;
            ushort index = 0X000E;

            UInt32 ActualExposureTime;
            UInt32 isLongExposureMode;
            UInt32 switchpoint;

            UInt32 TB3_TIMES;
            UInt32 TB2_TImes;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd5, value, index, 64, xdata);

            richTextBox1.Clear();



            ActualExposureTime = (UInt32)(xdata[20] * 256 * 256 * 256 + xdata[21] * 256 * 256 + xdata[22] * 256 + xdata[23]);
            switchpoint = (UInt32)(xdata[24] * 256 * 256 * 256 + xdata[25] * 256 * 256 + xdata[26] * 256 + xdata[27]);
            isLongExposureMode = xdata[32];

            if (isLongExposureMode == 1) TB3_TIMES = ActualExposureTime - switchpoint;
            else TB3_TIMES = 0;

            if (isLongExposureMode == 1) TB2_TImes = ActualExposureTime;
            else TB2_TImes = switchpoint;


            //label102.Text = "TB5 is  " + switchpoint.ToString() + "us";
            //label131.Text = "TB3 is  " + TB3_TIMES.ToString() + "us";
            //label132.Text = "TB2 is  " + TB2_TImes.ToString() + "us";

            //  richTextBox1.AppendText("SwitchPoint=" + SwitchPoint.ToString() + Environment.NewLine);


        }

        private void button225_Click(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            LowLevelReadD5(0x000e, xdata);

            UInt32 PixelPeriod_ps;  //unit: ns*1000
            UInt32 linePeroid_ns;   //unit:  ns
            UInt32 framePeriod_us;
            UInt32 hmax;
            UInt32 vmax;
            UInt32 actualExpTime;
            byte isLongExpMode;


            PixelPeriod_ps = (uint)(xdata[0] * 256 * 256 * 256 + xdata[1] * 256 * 256 + xdata[2] * 256 + xdata[3]);
            linePeroid_ns = (uint)(xdata[4] * 256 * 256 * 256 + xdata[5] * 256 * 256 + xdata[6] * 256 + xdata[7]);
            framePeriod_us = (uint)(xdata[8] * 256 * 256 * 256 + xdata[9] * 256 * 256 + xdata[10] * 256 + xdata[11]);
            hmax = (uint)(xdata[12] * 256 * 256 * 256 + xdata[13] * 256 * 256 + xdata[14] * 256 + xdata[15]);
            vmax = (uint)(xdata[16] * 256 * 256 * 256 + xdata[17] * 256 * 256 + xdata[18] * 256 + xdata[19]);
            actualExpTime = (uint)(xdata[20] * 256 * 256 * 256 + xdata[21] * 256 * 256 + xdata[22] * 256 + xdata[23]);
            isLongExpMode = xdata[32];

            richTextBox1.AppendText("PixelPeriod_ps=" + PixelPeriod_ps.ToString() + Environment.NewLine);
            richTextBox1.AppendText("linePeroid_ns=" + linePeroid_ns.ToString() + Environment.NewLine);
            richTextBox1.AppendText("framePeriod_us=" + framePeriod_us.ToString() + Environment.NewLine);
            richTextBox1.AppendText("hmax=" + hmax.ToString() + Environment.NewLine);
            richTextBox1.AppendText("vmax=" + vmax.ToString() + Environment.NewLine);
            richTextBox1.AppendText("actualExpTime(us)=" + actualExpTime.ToString() + Environment.NewLine);
            richTextBox1.AppendText("isLongExpMode=" + isLongExpMode.ToString() + Environment.NewLine);

        }

        private void button226_Click(object sender, EventArgs e)
        {
            trig_mode = 0;
        }

        private void button227_Click(object sender, EventArgs e)
        {
            trig_mode = 0;
            writeFPGA(39, 5);//set to mode 5

            writeFPGA(50, 1);
            writeFPGA(51, 0);
            writeFPGA(52, 3);


            writeFPGA(58, trig_mode);//filter disable  , RBI disable ,mode b disable,trig in disable

        }

        private void button228_Click(object sender, EventArgs e)
        {
            trig_mode = 0;
            writeFPGA(39, 5);//set to mode 5

            writeFPGA(50, 1);
            writeFPGA(51, 0);
            writeFPGA(52, 3);

            //writeFPGA(57, 0);//EnableBurstMode
            writeFPGA(58, trig_mode);//filter disable  , RBI disable ,mode b disable,trig in disable
        }

        private void button227_Click_1(object sender, EventArgs e)
        {
            trig_mode = 0;
            writeFPGA(39, 5);//set to mode 5

            writeFPGA(50, 1);
            writeFPGA(51, 0);
            writeFPGA(52, 3);

            // writeFPGA(57, 0);//EnableBurstMode
            writeFPGA(58, trig_mode);//filter disable  , RBI disable ,mode b disable,trig in disable

        }

        private void button228_Click_1(object sender, EventArgs e)
        {

            writeFPGA_EXtend(282, 0x00);//write trigmode reg 
        }

        private void button229_Click(object sender, EventArgs e)
        {
            SDKAPI = true;
            writeFPGA(39, 2);
            writeFPGA(58, 3);
            writeFPGA2(58, 3);
            writeFPGA(142, 0);
            SetBurstMode((ushort)Convert.ToUInt16(textBox29.Text, 10), (ushort)Convert.ToUInt16(textBox30.Text, 10), true);
            //SetBurstMode( ( ushort ) Convert.ToUInt16( textBox29.Text ,10), ( ushort ) Convert.ToUInt16( textBox30.Text,10 ), true );


        }

        private void button230_Click(object sender, EventArgs e)
        {
            SDKAPI = true;
            writeFPGA(39, 2);
            writeFPGA(58, 2);
            writeFPGA2(58, 2);
            writeFPGA(142, 1);
            SetBurstMode((ushort)Convert.ToUInt16(textBox29.Text, 10), (ushort)Convert.ToUInt16(textBox30.Text, 10), true);
            //SetBurstMode( ( ushort ) Convert.ToUInt16( textBox29.Text, 10 ), ( ushort ) Convert.ToUInt16( textBox30.Text, 10 ), true );

        }

        private void button231_Click(object sender, EventArgs e)
        {
            SDKAPI = true;
            byte[] xdata = new byte[64];
            ushort value = 0;
            ushort index = 0;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd3, value, index, 64, xdata);

            writeFPGA(39, 0);
            writeFPGA(58, 0);
            writeFPGA2(58, 0);
            if (xdata[39] == 1)
            {
                SetBurstMode(0, 2, true);
                textBox29.Text = "0";
                textBox30.Text = "2";
            }
            else
            {
                SetBurstMode(0, 2, false);
                textBox29.Text = "0";
                textBox30.Text = "2";
            }
            writeFPGA(142, 0);


            //SetBurstMode( 0, 2, false );
        }

        private void button232_Click(object sender, EventArgs e)
        {
            SDKAPI = true;
            writeFPGA2(8, 0);
            writeFPGA2(8, 1);

        }

        private void button233_Click(object sender, EventArgs e)
        {
            SDKAPI = false;  //true;
            SetBurstMode((ushort)Convert.ToUInt16(textBox29.Text, 10), (ushort)Convert.ToUInt16(textBox30.Text, 10), true);

        }

        private void button235_Click(object sender, EventArgs e)
        {

        }

        private void button234_Click(object sender, EventArgs e)
        {

        }

        private void button234_Click_1(object sender, EventArgs e)
        {
            SDKAPI = true;
            byte[] xdata = new byte[64];
            ushort value = 0;
            ushort index = 0x0e;
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd5, value, index, 64, xdata);
            UInt32 SwitchPoint;  //unit: ns*1000     
            SwitchPoint = (uint)((xdata[24] * 256 * 256 * 256 + xdata[25] * 256 * 256 + xdata[26] * 256 + xdata[27]));
            label104.Text = "T2 = " + SwitchPoint.ToString() + "us";

            int actual_expTime;  //unit: ns*1000     
            actual_expTime = xdata[20] * 256 * 256 * 256 + xdata[21] * 256 * 256 + xdata[22] * 256 + xdata[23];
            label103.Text = "T1 = " + actual_expTime.ToString() + "us";

            label105.Text = "T3 = " + ((actual_expTime - SwitchPoint > 0) ? (actual_expTime - SwitchPoint) : 0).ToString() + "us";

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void button235_Click_1(object sender, EventArgs e)
        {

        }

        private void checkBox19_CheckedChanged(object sender, EventArgs e)
        {
            SDKAPI = true;
            if (checkBox19.Checked)
            {
                writeFPGA2(45, MSB1(4128));
                writeFPGA2(46, MSB0(4128));
            }
            else
            {
                writeFPGA2(45, MSB1(0));
                writeFPGA2(46, MSB0(0));
            }
        }

        private void panel5_MouseHover(object sender, EventArgs e)
        {

        }

        private void tabPage3_MouseHover(object sender, EventArgs e)
        {
            richTextBox1.Text = " QHY4040pro同一时刻只支持trigIn或TrigOut模式。TrigIn模式支持硬件TrigIn，没有TrigOut输出，TrigOut模式支持软件触发+TrigOut信号输出。\r\n";
            richTextBox1.AppendText("QHY4040pro only support TrigIn or TrigOut at the same time.TrigIn mode only get the Extirg signal without trigout signal output. TrigOut mode only support trigIn signal from software and output the TrigOut signal.");

        }

        private void panel3_MouseHover(object sender, EventArgs e)
        {

        }

        private void button229_MouseHover(object sender, EventArgs e)
        {
            richTextBox1.Text = "TrigIn Mode(TrigIn 模式)\r\n";
            richTextBox1.AppendText("点击ExtrigIn按钮，相机进入单帧硬件外触发模式。\r\n");
            richTextBox1.AppendText("Click ExTrigIn Button to Enable SingleTrigMode by hardware.\r\n\r\n");
            richTextBox1.AppendText("一个硬件外触发信号输出一帧图像数据，时序图中对应T1-T3时间由TrigOut_T1-T3按钮获得。\r\n");
            richTextBox1.AppendText("One frame capture with one trig signal by hardware.You can click the TrigOut_T1-T3 button to get the time T1-T3.\r\n");

        }

        private void button230_MouseHover(object sender, EventArgs e)
        {
            richTextBox1.Text = "TrigOut Mode(TirgOut模式)\r\n";
            richTextBox1.AppendText("点击TrigOut按钮，相机进入单帧软件触发模式。\r\n");
            richTextBox1.AppendText("Click TrigOutButton to Enable SingleTrigMode by software. \r\n\r\n");
            richTextBox1.AppendText("一次触发信号输出一帧图像数据。每次触发相机都会输出一个trigout信号，时序图中对应T1-T3时间由TrigOut_T1-T3按钮获得。\r\n");
            richTextBox1.AppendText("One frame capture with one trig signal by SoftwareTrig button. Each capture will output one trigOut signal.You can click the TrigOut_T1-T3 button to get the time T1-T3.\r\n");

        }

        private void button231_MouseHover(object sender, EventArgs e)
        {
            richTextBox1.Text = "点击退出外触发模式!\r\n";
            richTextBox1.AppendText("Click to exit trigmode!\r\n");

        }

        private void button232_MouseHover(object sender, EventArgs e)
        {
            richTextBox1.Text = "点击开始软件触发!\r\n";
            richTextBox1.AppendText("Click to trigger by coftware!\r\n\r\n");
            richTextBox1.AppendText("在使用软件外触发之前需要使能trigout模式。!\r\n");
            richTextBox1.AppendText("Before click this button, please make sure the camera is working on trigout mode.!\r\n");
        }

        private void button233_MouseHover(object sender, EventArgs e)
        {
            richTextBox1.Text = "点击设置burst模式开始和能结束帧，默认开始帧为0，结束帧为2!\r\n";
            richTextBox1.AppendText("Click to set burst start and burst end! Default start is 0 and default end is 2.\r\n\r\n");
            richTextBox1.AppendText("当burstend 减去burststart大于2时，相机将工作在burst mode，此时，如果cleanmode被选中，则相机工作在消残影模式。\r\n");
            richTextBox1.AppendText("when burstend - burststat >2, the camera will work on burst mode. If cleanmode is checked at the same time, the camera will work on burst clean mode.\r\n");

        }

        private void textBox29_MouseHover(object sender, EventArgs e)
        {
            richTextBox1.Text = "Burst Start\r\n";
        }

        private void textBox30_MouseHover(object sender, EventArgs e)
        {
            richTextBox1.Text = "Burst End\r\n";
        }

        private void button234_MouseHover(object sender, EventArgs e)
        {
            richTextBox1.Text = "点击获得T1,T2 和T3时间。\r\n";
            richTextBox1.AppendText("Click to to get the time T1,T2 and T2.!\r\n");
        }

        private void checkBox19_MouseHover(object sender, EventArgs e)
        {
            richTextBox1.Text = "当相机工作在burst模式时，您可以勾选该复选框进入消残影模式。\r\n";
            richTextBox1.AppendText("When the camera is working on burst mode. You can check this checkbox to enable burstcleanmode. \r\n");
        }

        private void button235_Click_2(object sender, EventArgs e)
        {
            tabLVDS.SelectedIndex = 16;
        }

        private void button236_Click(object sender, EventArgs e)
        {
            tabLVDS.SelectedIndex = 9;
        }

        private void button237_Click(object sender, EventArgs e)
        {
            writeFPGA_EXtend(281, 0x03);//GPS trig in en
        }

        private void button238_Click(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            ushort value = 0x000E;
            ushort index = 0X000E;
            UInt32 PixelPeriod_ps;
            UInt32 LinePeriod_ns;
            UInt32 FramePeriod_us;
            UInt32 expTime;
            UInt32 ActualExposureTime;
            UInt32 switchpoint;
            UInt32 isSingleFrameMode;
            UInt32 isRollingShutter;
            UInt32 is16bit;
            UInt32 enable_ddr;
            UInt32 isTrigMode;
            UInt32 hmax_ref;
            UInt32 usb_traffic;
            UInt32 hmax;
            UInt32 vmax_ref_roi;
            UInt32 vmax_ref;
            UInt32 vmax;
            UInt32 shr;
            UInt32 isFDBIN22;
            
            
            
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd5, value, index, 64, xdata);

            richTextBox1.Clear();

            PixelPeriod_ps = (UInt32)(xdata[0] * 256 * 256 * 256 + xdata[1] * 256 * 256 + xdata[2] * 256 + xdata[3]);
            LinePeriod_ns = (UInt32)(xdata[4] * 256 * 256 * 256 + xdata[5] * 256 * 256 + xdata[6] * 256 + xdata[7]);
            FramePeriod_us = (UInt32)(xdata[8] * 256 * 256 * 256 + xdata[9] * 256 * 256 + xdata[10] * 256 + xdata[11]);
            expTime = (UInt32)(xdata[45] * 256 * 256 * 256 + xdata[46] * 256 * 256 + xdata[47] * 256 + xdata[48]);
            ActualExposureTime = (UInt32)(xdata[20] * 256 * 256 * 256 + xdata[21] * 256 * 256 + xdata[22] * 256 + xdata[23]);
            switchpoint = (UInt32)(xdata[24] * 256 * 256 * 256 + xdata[25] * 256 * 256 + xdata[26] * 256 + xdata[27]);
            isSingleFrameMode = xdata[49];
            isRollingShutter = xdata[50];
            is16bit = xdata[43];
            enable_ddr = xdata[44];
            isTrigMode = xdata[54];
            hmax_ref = (UInt32)(xdata[39] * 256 + xdata[40]);
            usb_traffic = (UInt32)(xdata[35] * 256 + xdata[36]);
            hmax = (UInt32)(xdata[12] * 256 * 256 * 256 + xdata[13] * 256 * 256 + xdata[14] * 256 + xdata[15]);
            vmax_ref_roi = (UInt32)(xdata[37] * 256 + xdata[38]);
            vmax_ref = (UInt32)(xdata[41] * 256 + xdata[42]);
            vmax = (UInt32)(xdata[16] * 256 * 256 * 256 + xdata[17] * 256 * 256 + xdata[18] * 256 + xdata[19]);
            shr = (UInt32)(xdata[28] * 256 + xdata[29]);
            isFDBIN22 = xdata[55];
            

            richTextBox1.AppendText("LOW LEVEL D 5 PixelPeriod_ps :   " + PixelPeriod_ps.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 LinePeriod_ns :   " + LinePeriod_ns.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 FramePeriod_us :   " + FramePeriod_us.ToString() + Environment.NewLine);
            //
            richTextBox1.AppendText("LOW LEVEL D 5 expTime :   " + expTime.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 ActualExposureTime :   " + ActualExposureTime.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 switchpoint :   " + switchpoint.ToString() + Environment.NewLine);
            //
            richTextBox1.AppendText("LOW LEVEL D 5 isSingleFrameMode :   " + isSingleFrameMode.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 isRollingShutter :   " + isRollingShutter.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 is16bit :   " + is16bit.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 enable_ddr :   " + enable_ddr.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 isTrigMode  :   " + isTrigMode.ToString() + Environment.NewLine);
            //
            richTextBox1.AppendText("LOW LEVEL D 5 hmax_ref :   " + hmax_ref.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 usb_traffic :   " + usb_traffic.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 hmax :   " + hmax.ToString() + Environment.NewLine);
            //
            richTextBox1.AppendText("LOW LEVEL D 5 vmax_ref_roi :   " + vmax_ref_roi.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 vmax_ref :   " + vmax_ref.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 vmax :   " + vmax.ToString() + Environment.NewLine);
            //
            richTextBox1.AppendText("LOW LEVEL D 5 shr  :   " + shr.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 isFDBIN22  :   " + isFDBIN22.ToString() + Environment.NewLine);
            
        }


        private void button45_Click_3(object sender, EventArgs e)
        {
            writeFPGA(83, 1);
            writeFPGA(83, 0);
        }

        private void textBox28_TextChanged(object sender, EventArgs e)
        {

        }

        private void tabPageQHY600PRO_Click(object sender, EventArgs e)
        {

        }


        private void label117_Click(object sender, EventArgs e)
        {

        }

        private void button115_Click(object sender, EventArgs e)
        {

        }

        private void button241_Click(object sender, EventArgs e)
        {
            writeFPGA(4, 0);
            label130.Text = "GPS";
        }

        private void button242_Click(object sender, EventArgs e)
        {
            writeFPGA(4, 0);
            label130.Text = "BD";
        }

        private void button243_Click(object sender, EventArgs e)
        {
            LowLevelA6(0x22);
            LowLevelA6(0x44);
        }

        private void button46_Click_1(object sender, EventArgs e)
        {
            writeFPGA(83, 1);
            writeFPGA(83, 0);
        }


        private void button213_Click_3(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            ushort value = 0x0004;
            ushort index = 0X0004;
            byte cam_name1;
            byte cam_name2;
            byte year;
            byte momth;
            byte day;
            byte subversion1;
            byte board_tp;
            byte subversion2;
            Int16 fpga_temp;
            //T35 22 08 11
            byte fpga_states_num;
            byte fpga_state1;
            byte fpga_state2;
            byte fpga_state3;
            byte cmos_alive;
            byte cmos_work_state;
            UInt16 Detected_X;
            UInt16 Detected_Y;
            UInt16 Detected_BandWidth;



            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd5, value, index, 64, xdata);

            richTextBox1.Clear();

            year = (byte)(xdata[5]);
            momth = (byte)(xdata[6]);
            day = (byte)(xdata[7]);
            subversion1 = (byte)(xdata[8]);
            board_tp = (byte)(xdata[9]);
            subversion2 = (byte)(xdata[10]);
            fpga_temp = (Int16)(((readFPGA(208) + readFPGA(209) * 256) * 693 / 1024) - 265);
            cam_name1 = (byte)(xdata[2]);
            cam_name2 = (byte)(xdata[3]);

            //T35 22 08 11 
            fpga_states_num = (byte)(xdata[14]);
            fpga_state1 = (byte)(xdata[15]);
            fpga_state2 =(byte)(xdata[16]);
            fpga_state3 = (byte)(xdata[17]);
            cmos_alive = (byte)(xdata[23]);
            cmos_work_state = (byte)(xdata[25]);
            Detected_X=(UInt16)(xdata[26]+xdata[27]*256);
            Detected_Y=(UInt16)(xdata[28]+xdata[29]*256);
            Detected_BandWidth=(UInt16)(xdata[30]+xdata[31]*256);


            richTextBox1.AppendText("LOW LEVEL D5 0004 camera(HEX) :  " + cam_name1.ToString("X2") + "-" + cam_name2.ToString("X2") + Environment.NewLine);

            richTextBox1.AppendText("LOW LEVEL D5 0004 FPGA version(D) :  " + year.ToString() + "-" + momth.ToString() + "-" + day.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D5 0004 FPGA subversion(D) :  " + subversion1.ToString() + "-" + subversion2.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D5 0004 FPGA board type(D):  " + board_tp.ToString() + Environment.NewLine);

            if (fpga_temp != -265)
            {
                richTextBox1.AppendText("LOW LEVEL D5 FPGA temperature(D) :  " + fpga_temp.ToString() + Environment.NewLine);
            }
            else
            {
                richTextBox1.AppendText("LOW LEVEL D5 FPGA temperature (D):  " + fpga_temp.ToString("D") + "\r\n"+"CMOS nonsupport read  FPGA temperature  " + Environment.NewLine);
            }

            if (fpga_states_num > 0)
            {
                richTextBox1.AppendText("LOW LEVEL D5 fpga_state1(HEX) :  " + fpga_state1.ToString("x") + Environment.NewLine +
                                        "LOW LEVEL D5 fpga_state2(HEX):  " + fpga_state2.ToString("x") + Environment.NewLine +
                                        "LOW LEVEL D5 fpga_state3(HEX) :  " + fpga_state3.ToString("x") + Environment.NewLine);
            }
            else
            {
                richTextBox1.AppendText("LOW LEVEL D5 fpga_states_num (D):  " + cmos_alive.ToString("D") + "\r\n" + "CMOS nonsupport read  FPGA   STATES  " + Environment.NewLine);
            }

            if (cmos_alive != 0)
            {
                if (cmos_alive == 0x80)
                    richTextBox1.AppendText("LOW LEVEL D5 CMOS communication(HEX) :  " + cmos_alive.ToString("x")  + " CMOS communication failure." + Environment.NewLine);
                else
                    richTextBox1.AppendText("LOW LEVEL D5 CMOS communication(HEX) :  " + cmos_alive.ToString("x")  + " CMOS communication successful." + Environment.NewLine);
            }
            else
            {
                richTextBox1.AppendText("LOW LEVEL D5 cmos_alive (hex):  " + cmos_alive.ToString("x") + "\r\n"+ "CMOS nonsupport read  CMOS communication STATES  " + Environment.NewLine);
            }
            

              if ((cmos_work_state &0x03) == 0x03)
              {
                  //richTextBox1.AppendText("LOW LEVEL D5 cmos_work_state (hex):  " + cmos_work_state.ToString("x") +  + Environment.NewLine);
                 
                  richTextBox1.AppendText("LOW LEVEL D5 CMOS Detected X (D):  " + Detected_X.ToString("d")  +Environment.NewLine);
                  richTextBox1.AppendText("LOW LEVEL D5 CMOS Detected Y (D):  " + Detected_Y.ToString("d") +Environment.NewLine);
              }
              else
              {
                  richTextBox1.AppendText("LOW LEVEL D5 cmos_work_state (hex):  " + cmos_work_state.ToString("x") + "\r\n" + "CMOS nonsupport read  CMOS Detected X Y" + Environment.NewLine);
              }

            if ((cmos_work_state & 0x02) == 0x02)
            {
                richTextBox1.AppendText("LOW LEVEL D5 CMOS Detected_BandWidth  (D):  " + Detected_BandWidth.ToString("d") + Environment.NewLine);
            }
            else
            {
                richTextBox1.AppendText("LOW LEVEL D5 cmos_work_state (hex):  " + cmos_work_state.ToString("x") + "\r\n" + "CMOS nonsupport read  CMOS Detected_BandWidth " + Environment.NewLine);
            }





            richTextBox1.AppendText("\r\n");
            richTextBox1.AppendText("fpga_state1 [7]:  mipierr_bit0_5 " + "\r\n" + "fpga_state1 [6]: MIPI Ecc no error" + "\r\n" + "fpga_state1 [5]: cfg_ERROR" + "\r\n" + "fpga_state1 [4]: ddr_alempty" + "\r\n" + "fpga_state1 [3]: ddr_alfull" + "\r\n" + "fpga_state1 [2]: addr_err" + "\r\n" + "fpga_state1 [1]: check_fail" + "\r\n" + "fpga_state1 [0]: init_done_flag" + "\r\n");
            richTextBox1.AppendText("\r\n");
            richTextBox1.AppendText("mipierr_bit0_5[0]: Escape Entry Error. Asserted when an unrecognized escape entrycommand is received " + "\r\n" +
                "mipierr_bit0_5[1]:CRC Error VC0. Set to 1 when a checksum error occurs. " + "\r\n" +
                "mipierr_bit0_5[2]:CRC Error VC1. Set to 1 when a checksum error occurs. " + "\r\n" +
                "mipierr_bit0_5[3]:CRC Error VC2. Set to 1 when a checksum error occurs. " + "\r\n" +
                "mipierr_bit0_5[4]: CRC Error VC3. Set to 1 when a checksum error occurs." + "\r\n" +
                "mipierr_bit0_5[5]: HS RX Timeout Error. The protocol should time out when no EoT is received within a certain period in HS RX mode." + "\r\n");
            
            richTextBox1.AppendText("\r\n");
            richTextBox1.AppendText("fpga state 2:  mipi_rx_inst1_ERROR[17:10]" + "\r\n" + "\r\n" +
               "mipi_rx_inst1_ERROR [10]: Frame Sync Error. Asserted when a frame end is not paired with a frame start on the same virtual channel." + "\r\n" +
               "mipi_rx_inst1_ERROR [11]: Invalid Packet Length. Set to 1 if there is an invalid packet length." + "\r\n" +
               "mipi_rx_inst1_ERROR [12]: Invalid VC ID. Set to 1 if there is an invalid CSI VC ID." + "\r\n" +
               "mipi_rx_inst1_ERROR [13]: Invalid Data Type. Set to 1 if the received data is invalid." + "\r\n" +
               "mipi_rx_inst1_ERROR [14]: Error In Frame. Asserted when VSYNC END received when CRC error is present in the data packet." + "\r\n" +
               "mipi_rx_inst1_ERROR [15]: Control Error. Asserted when an incorrect line state sequence is detected." + "\r\n" +
               "mipi_rx_inst1_ERROR [16]: Start-of-Transmission (SoT) Error. Corrupted high-speed SoT leader sequence while proper synchronization can still be achieved." + "\r\n" +
               "mipi_rx_inst1_ERROR [17]: SoT Synchronization Error. Corrupted high-speed SoT leader sequence while proper synchronization cannot be expected." + "\r\n");
        

        }

        private void button219_Click(object sender, EventArgs e)
        {
            byte buf1 = 0;
            byte buf2 = 0;
            byte buf3 = 0;
            byte buf4 = 0;
            byte buf5 = 0;
            byte buf6 = 0;
            byte buf7 = 0;
            byte buf8 = 0;

            if (xid.Contains("QHY990"))
            {

                richTextBox1.AppendText("QHY990 TRIG " + "\r\n");

                //if (checkBox_trigin_en.Checked == true) buf2 = 1; else buf2 = 0;

                if (trig_exposeure.Checked == true) buf1 = 2; else buf1 = 1;

                if (OPTIC_check.Checked == true) buf4 = 1;
                else if (cl_check.Checked == true) buf4 = 2;
                else if (GPIO_check.Checked == true) buf4 = 3;
                else buf4 = 0;

                if (continuous_check.Checked == true) buf5 = 1;
                else buf5 = 0;

                if (AMPVcheck.Checked == true) buf3 = 1; else buf3 = 0;
            }
 
            else if (xid.Contains("QHY268"))
            {
                richTextBox1.AppendText("QHY268 TRIG " + "\r\n");

                UInt16 filter_times_us = 0;

                if (OPTIC_check.Checked == true) buf6 = 2;
                else if (cl_check.Checked == true) buf6 = 1;
                else if (GPIO_check.Checked == true) { buf6 = 0; writeFPGA(142, 1); }
                else buf6 = 2;

                if (AMPVcheck.Checked == true) buf8 = 1; else buf8 = 0;

                filter_times_us = Convert.ToUInt16(textBox_filtertimes.Text);
                if (filter_times_us > 65534) filter_times_us = 65535;

                buf3 = LSB(filter_times_us);
                buf4 = MSB(filter_times_us);

                buf1 = 1;

                if (checkBox_trigin_en.Checked == true) buf2 = 1;
                else buf2 = 0;

                if (checkBox_trig_out.Checked == true)
                {
                    buf7 = 1;
                }
                else buf7 = 0;


            }
              
            else                              //Global shutter camera external trigger  530  487  661
            {
                richTextBox1.AppendText(" TRIG_MODE " + "\r\n");

                UInt16 filter_times_us = 0;

                if (OPTIC_check.Checked == true) buf6 = 2;
                else if (cl_check.Checked == true) buf6 = 1;
                else if (GPIO_check.Checked == true) buf6 = 0;
                else buf6 = 2;


                if (continuous_check.Checked == true) buf5 = 1; else buf5 = 0;

                if (AMPVcheck.Checked == true) buf8 = 1; else buf8 = 0;

                filter_times_us = Convert.ToUInt16(textBox_filtertimes.Text);
                if (filter_times_us > 65534) filter_times_us = 65535;

                buf3 = LSB(filter_times_us);
                buf4 = MSB(filter_times_us);

                buf1 = 1;

                if (checkBox_trigin_en.Checked == false) buf2 = 0;
                else
                {
                    if (trig_exposeure.Checked == true) buf2 = 2; else buf2 = 1;//2:脉冲宽度设置曝光时间    1:脉冲+SC设置曝光时间
                }

            }


            LowLevelAC_QJ(buf1, buf2, buf3, buf4, buf5, buf6, buf7, buf8);


        }

        private void button129_Click_1(object sender, EventArgs e)
        {
            LowLevelAC_QJ(0, 0, 0, 0, 0, 0, 0, 0);
        }

        private void button214_Click_2(object sender, EventArgs e)
        {

        }

        private void AMPVcheck_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button180_Click_1(object sender, EventArgs e)
        {
            ushort year, month, day, subversion1, subversion2, board_tp, rstn_num;
            UInt16 fpga_temp;
            //rstn_num = 0;
            year = readFPGA(200);
            month = readFPGA(201);
            day = readFPGA(202);
            subversion1 = readFPGA(203);
            subversion2 = readFPGA(207);
            board_tp = readFPGA(204);
            rstn_num = readFPGA(210);

            fpga_temp = (UInt16)(((readFPGA(208) + readFPGA(209) * 256) * 693 / 1024) - 265);
            FPGA_VERSION_YSK.Text = year.ToString() + "-" + month.ToString() + "-" + day.ToString() + "-" + subversion1.ToString();

            richTextBox1.AppendText("FPGA data : " + year.ToString() + "-" + month.ToString() + "-" + day.ToString() + "\r\n");
            richTextBox1.AppendText("FPGA version : " + subversion1.ToString() + "-" + subversion2.ToString() + "\r\n");
            richTextBox1.AppendText("FPGA board type  : " + board_tp.ToString() + "\r\n");
            richTextBox1.AppendText("FPGA temperature   : " + fpga_temp.ToString() + "\r\n");
            richTextBox1.AppendText("FPGA rstn_num   : " + rstn_num.ToString() + "\r\n");
            //


        }

        private void button214_Click_3(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            ushort value = 0x00;
            ushort index = 0x00;
            //UInt32 ret;


            byte command;
            command = Convert.ToByte(textBox45_YSK.Text);

            xdata[0] = command;
            xdata[1] = command;

            for (byte i = 0; i < 64; i++)
            {
                xdata[i] = (byte)(command + i);
            }



            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xE5, value, index, 64, xdata);

        }

        private void button180_Click_2(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            ushort value = 64;
            ushort index = 64;




            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xE4, value, index, 64, xdata);
            richTextBox1.AppendText(" pcie send  E4  READ: " + Environment.NewLine);
            for (int i = 0; i < 64; i++)
            {
                richTextBox1.AppendText(xdata[i].ToString("x2") + " ");

            }
            richTextBox1.AppendText(Environment.NewLine);



        }

        private void button227_Click_2(object sender, EventArgs e)
        {
            UInt32 ddr_num;
            ddr_num = (UInt32)(readFPGA(4) + readFPGA(5) * 256 + readFPGA(6) * 256 * 256);

            richTextBox1.AppendText(" DDR NUM : " + ddr_num.ToString() + Environment.NewLine);

        }

        private void continuous_check_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button245_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void OPTIC_check_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox_trigin_en_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button246_Click(object sender, EventArgs e)
        {
            writeFPGA(9, 0);
            writeFPGA(10, 0);
            writeFPGA(12, 0);
            writeFPGA(13, 0);
            writeFPGA(14, 0);
            writeFPGA(15, 0);
            writeFPGA(16, 0);
            writeFPGA(17, 0);
            writeFPGA(49, 1);
        }

        private void button247_Click(object sender, EventArgs e)
        {
            ushort index;
            byte[] xdata = new byte[64];
            index = Convert.ToByte(textBox25.Text);
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd5, 0X00, index, 64, xdata);

            richTextBox1.Clear();

            richTextBox1.AppendText("LOW LEVEL D5 :  " + index.ToString("X2") + Environment.NewLine);

            for (byte i = 0; i < 63; i++)
            {
                richTextBox1.AppendText(xdata[i].ToString("X2") + " ");
            }

            richTextBox1.AppendText(Environment.NewLine);

        }

        private void button248_Click(object sender, EventArgs e)
        {
            UInt32 ret;
            UInt32 readbytenum;
            UInt16 readtime;
            readtime = 100;
            readbytenum = 65536 * 4;//65536 * 4



            byte[] read_data;
            read_data = new byte[65536 * 4];
            UInt16 data16up1 = 0;
            UInt16 data16dowm1 = 0;

            UInt16 data16up2 = 0;
            UInt16 data16dowm2 = 0;
            richTextBox1.Clear();
            richTextBox1.AppendText("===========test fx3 data start ==========" + Environment.NewLine);
            for (uint a = 0; a < readtime; a++)
            {
                ret = ASCOM.QHYCCD.libqhyccd.C_QHYCCDReadUSB_SYNC(camhandle, 0x81, (uint)(read_data.Length), read_data, 200);


                Application.DoEvents();//可执行某无聊的操作

                // richTextBox1.AppendText("rd usb" + ret.ToString() +" "+a.ToString()+ Environment.NewLine);

                for (uint i = 0; i < readbytenum; i = i + 8)
                {
                    Application.DoEvents();//可执行某无聊的操作
                    data16dowm1 = (UInt16)(read_data[i] * 256 + read_data[i + 1]);
                    data16up1 = (UInt16)(read_data[i + 2] * 256 + read_data[i + 3]);

                    data16dowm2 = (UInt16)(read_data[i + 4] * 256 + read_data[i + 5]);
                    data16up2 = (UInt16)(read_data[i + 6] * 256 + read_data[i + 7]);
                    Application.DoEvents();//可执行某无聊的操作
                    //richTextBox1.AppendText("fx3 data :" + data16dowm1.ToString() + "  " + data16up1.ToString() + "  " + data16dowm2.ToString() + "  " + data16up2.ToString() + Environment.NewLine);

                    if (data16dowm1 != data16up1) richTextBox1.AppendText("fx3 data err1:  " + data16dowm1.ToString("X2") + "-" + data16up1.ToString("X2") + " i = " + i.ToString() + Environment.NewLine);
                    //else richTextBox1.AppendText("ok "+i.ToString()+ Environment.NewLine);
                    if (data16dowm2 != data16up2) richTextBox1.AppendText("fx3 data err2:  " + data16dowm2.ToString("X2") + "-" + data16up2.ToString("X2") + " i = " + i.ToString() + Environment.NewLine);
                    //else richTextBox1.AppendText("ok " + i.ToString()+Environment.NewLine);
                    Application.DoEvents();//可执行某无聊的操作

                    //if (i < 16)
                    //{
                    if ((data16dowm2 - data16dowm1) != 1) richTextBox1.AppendText("fx3 data err3:  " + data16dowm2.ToString("X2") + "-" + data16dowm1.ToString("X2") + " i = " + i.ToString() + Environment.NewLine);
                    Application.DoEvents();//可执行某无聊的操作
                    // }
                    // if (i < 16000&&i >15900) 
                    // richTextBox1.AppendText("fx3 data:  " + data16dowm1.ToString("X2") + data16dowm2.ToString("X2") + Environment.NewLine);

                    //if (i <16) 

                    //{ richTextBox1.AppendText("test fx3 data 0-7 :  " + read_data[i].ToString("X2") + "  "+

                    //    read_data[i+1].ToString("X2")+"  "+
                    //     read_data[i+2].ToString("X2") + "  " +
                    //      read_data[i+3].ToString("X2") + "  " +
                    //       read_data[i + 4].ToString("X2") + "  " +
                    //        read_data[i + 5].ToString("X2") + "  " +
                    //         read_data[i + 6].ToString("X2") + "  " +
                    //          read_data[i + 7].ToString("X2") + "  " +

                    //    Environment.NewLine);

                    //richTextBox1.AppendText("data16dowm1:  " + data16dowm1.ToString("X2") + Environment.NewLine);
                    //richTextBox1.AppendText("data16up1:  " + data16up1.ToString("X2") + Environment.NewLine);
                    //richTextBox1.AppendText("data16dowm2:  " + data16dowm2.ToString("X2") + Environment.NewLine);
                    //richTextBox1.AppendText("data16up2:  " + data16up2.ToString("X2") + Environment.NewLine);

                    //}


                    Application.DoEvents();//可执行某无聊的操作
                }
            }
            Application.DoEvents();//可执行某无聊的操作
            richTextBox1.AppendText("=============test fx3 data end ==========" + Environment.NewLine);

        }

        private void tabPageFPGA_Click(object sender, EventArgs e)
        {

        }

        private void button249_ysk_Click(object sender, EventArgs e)
        {
            byte watchdogen;
            byte sdkdogen;
            byte feedog;
            watchdogen = Convert.ToByte(textBox41ysk.Text);
            sdkdogen = Convert.ToByte(textBox42ysk.Text);
            feedog = Convert.ToByte(textBox43ysk.Text);

            LowLevelB5(watchdogen, sdkdogen, feedog);

        }

        private void label98_Click(object sender, EventArgs e)
        {

        }



        private void label10_Click_1(object sender, EventArgs e)
        {

        }

        private void button158YSK_Click(object sender, EventArgs e)
        {
            writeCMOS(0X3000, 0x01);//SET STANDBY

            writeCMOS(0X31e8, 0x20);
            writeCMOS(0X31e9, 0x01);//PLRD1
            writeCMOS(0X3122, 0x00);//PLRD2
            writeCMOS(0X3129, 0x90);//PLRD3
            writeCMOS(0X312a, 0x00);//PLRD4


            writeCMOS(0X311f, 0x00);//PLRD10
            writeCMOS(0X3123, 0x00);//PLRD11
            writeCMOS(0X3124, 0x00);//PLRD12
            writeCMOS(0X3125, 0x01);//PLRD13
            writeCMOS(0X3127, 0x02);//PLRD14
            writeCMOS(0X312D, 0x02);//PLRD15

            writeCMOS(0X3000, 0x00);//RELEASE STANDBY


        }

        private void button159YSK_Click(object sender, EventArgs e)
        {
            writeCMOS(0X3000, 0x01);//SET STANDBY

            writeCMOS(0X31e8, 0x20);
            writeCMOS(0X31e9, 0x01);//PLRD1
            writeCMOS(0X3122, 0x01);//PLRD2
            writeCMOS(0X3129, 0x90);//PLRD3
            writeCMOS(0X312a, 0x01);//PLRD4


            writeCMOS(0X311f, 0x00);//PLRD10
            writeCMOS(0X3123, 0x00);//PLRD11
            writeCMOS(0X3124, 0x00);//PLRD12
            writeCMOS(0X3125, 0x01);//PLRD13
            writeCMOS(0X3127, 0x02);//PLRD14
            writeCMOS(0X312D, 0x02);//PLRD15

            writeCMOS(0X3000, 0x00);//RELEASE STANDBY
        }

        private void button249YSK_Click(object sender, EventArgs e)
        {
            writeCMOS(0X3000, 0x01);//SET STANDBY

            writeCMOS(0X31e8, 0xc0);
            writeCMOS(0X31e9, 0x00);//PLRD1
            writeCMOS(0X3122, 0x01);//PLRD2
            writeCMOS(0X3129, 0x60);//PLRD3
            writeCMOS(0X312a, 0x01);//PLRD4


            writeCMOS(0X311f, 0x00);//PLRD10
            writeCMOS(0X3123, 0x00);//PLRD11
            writeCMOS(0X3124, 0x00);//PLRD12
            writeCMOS(0X3125, 0x01);//PLRD13
            writeCMOS(0X3127, 0x02);//PLRD14
            writeCMOS(0X312D, 0x02);//PLRD15

            writeCMOS(0X3000, 0x00);//RELEASE STANDBY
        }

        private void button250YSK_Click(object sender, EventArgs e)
        {
            writeCMOS(0X3000, 0x01);//SET STANDBY

            writeCMOS(0X31e8, 0x20);
            writeCMOS(0X31e9, 0x01);//PLRD1
            writeCMOS(0X3122, 0x02);//PLRD2
            writeCMOS(0X3129, 0x90);//PLRD3
            writeCMOS(0X312a, 0x02);//PLRD4


            writeCMOS(0X311f, 0x00);//PLRD10
            writeCMOS(0X3123, 0x00);//PLRD11
            writeCMOS(0X3124, 0x00);//PLRD12
            writeCMOS(0X3125, 0x01);//PLRD13
            writeCMOS(0X3127, 0x02);//PLRD14
            writeCMOS(0X312D, 0x02);//PLRD15

            writeCMOS(0X3000, 0x00);//RELEASE STANDBY
        }


        private void button158_Click_2(object sender, EventArgs e)
        {
            EnableTrain(true);
        }

        private void button159_Click_1(object sender, EventArgs e)
        {
            resetDDR_skipCheck();

        }

        private void label9911YSK_Click(object sender, EventArgs e)
        {

        }

        private void button76_Click_1(object sender, EventArgs e)
        {
            ushort addr;
            byte value;

            if (radioButtonHEX.Checked == true)
                addr = Convert.ToUInt16(textBox41.Text, 16);
            else
                addr = Convert.ToUInt16(textBox41.Text, 10);

            value = readCmos(addr);

            richTextBox1.AppendText("readCmos addr(HEX): " + addr.ToString("x") + " " + "read Cmos data (HEX):" + value.ToString("x") + "\r\n");

        }

        private void button96_Click_1(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            byte fpga_state1;
            byte fpga_state2;
            fpga_state1 = (byte)readFPGA(52);
            fpga_state2 = (byte)readFPGA(53);
            richTextBox1.AppendText("===============================================" + "\r\n");
            richTextBox1.AppendText("************fpga_state1: " + fpga_state1.ToString("x") + "***************** " + "\r\n");
            richTextBox1.AppendText("fpga_state1[7]: mipierr_bit0_5 " + "\r\n" + "fpga_state1[6]:MIPI Ecc no error" + "\r\n" + "fpga_state1[5]:cfg_ERROR" + "\r\n" + "fpga_state1[4]:ddr_alempty" + "\r\n" + "fpga_state1[3]:ddr_alfull" + "\r\n" + "fpga_state1[2]:addr_err" + "\r\n" + "fpga_state1[1]:check_fail" + "\r\n" + "fpga_state1[0]:init_done_flag" + "\r\n");
            richTextBox1.AppendText("\r\n");
            richTextBox1.AppendText("mipierr_bit0_5[0]: Escape Entry Error. Asserted when an unrecognized escape entrycommand is received " + "\r\n" +
                "mipierr_bit0_5[1]:CRC Error VC0. Set to 1 when a checksum error occurs. " + "\r\n" +
                "mipierr_bit0_5[2]:CRC Error VC1. Set to 1 when a checksum error occurs. " + "\r\n" +
                "mipierr_bit0_5[3]:CRC Error VC2. Set to 1 when a checksum error occurs. " + "\r\n" +
                "mipierr_bit0_5[4]: CRC Error VC3. Set to 1 when a checksum error occurs." + "\r\n" +
                "mipierr_bit0_5[5]: HS RX Timeout Error. The protocol should time out when no EoT is received within a certain period in HS RX mode." + "\r\n");

            richTextBox1.AppendText("===============================================" + "\r\n");

            richTextBox1.AppendText("************fpga_state2: " + fpga_state2.ToString("x") + "***************** " + "\r\n");
            richTextBox1.AppendText("fpga_state2:  mipi_rx_inst1_ERROR[17:10]" + "\r\n" + "\r\n" +
                "mipi_rx_inst1_ERROR[10]: Frame Sync Error. Asserted when a frame end is not paired with a frame start on the same virtual channel." + "\r\n" +
                "mipi_rx_inst1_ERROR[11]: Invalid Packet Length. Set to 1 if there is an invalid packet length." + "\r\n" +
                "mipi_rx_inst1_ERROR[12]: Invalid VC ID. Set to 1 if there is an invalid CSI VC ID." + "\r\n" +
                "mipi_rx_inst1_ERROR[13]: Invalid Data Type. Set to 1 if the received data is invalid." + "\r\n" +
                "mipi_rx_inst1_ERROR[14]: Error In Frame. Asserted when VSYNC END received when CRC error is present in the data packet." + "\r\n" +
                "mipi_rx_inst1_ERROR[15]: Control Error. Asserted when an incorrect line state sequence is detected." + "\r\n" +
                "mipi_rx_inst1_ERROR[16]: Start-of-Transmission (SoT) Error. Corrupted high-speed SoT leader sequence while proper synchronization can still be achieved." + "\r\n" +
                "mipi_rx_inst1_ERROR[17]: SoT Synchronization Error. Corrupted high-speed SoT leader sequence while proper synchronization cannot be expected." + "\r\n");
        
        }

        private void button97_Click_1(object sender, EventArgs e)
        {

            setIDLE();
            releaseIDLE();



        }

        private void button121_Click_2(object sender, EventArgs e)
        {


            writeCMOS(0X3222, 0X02);
            writeCMOS(0X3230, 0X00);
            writeCMOS(0X3231, 0X04);

            writeCMOS(0X3225, 0X10);
            writeCMOS(0X321F, 0X0B);
            writeCMOS(0X3223, 0XD0);
            writeCMOS(0X3224, 0X92);

            writeCMOS(0X322E, 0X04);
            writeCMOS(0X322F, 0X54);

            setHMAX(1998);
            setVMAX(1116);
            releaseIDLE();
            // MIPIRstn();

        }

        private void button142_Click_1(object sender, EventArgs e)
        {
            MIPIRstn();

        }

        private void button208_Click_1(object sender, EventArgs e)
        {
            ushort x, y;
            x = (ushort)(readFPGA(0) * 256 + readFPGA(1));
            y = (ushort)(readFPGA(2) * 256 + readFPGA(3));
            button208.Text = "Detected Image x=" + x.ToString() + " y=" + y.ToString();
        }

        private void button121_Click_3(object sender, EventArgs e)
        {
            ushort BW;
            BW = (ushort)(readFPGA(42) * 256 + readFPGA(41));

            button121.Text = "Detected Band Width =" + BW.ToString();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            if (sensorModel == 2110)
                writeCMOS(0x0100, 0x00);
            else
                writeCMOS(0x3000, 0x01);
        }

        private void button39_Click_1(object sender, EventArgs e)
        {
            if (sensorModel == 2110)
                writeCMOS(0x0100, 0x01);
            else
                writeCMOS(0x3000, 0x00);
        }

        private void button40_Click_1(object sender, EventArgs e)
        {
            for (byte i = 0; i < 255; i++)
            {
                writeFPGA(40, 1);
                Thread.Sleep(3000);
                writeFPGA(40, 0);
                Thread.Sleep(3000);
                //
                //writeFPGA(40, 1);
                //Thread.Sleep(100);
                //writeFPGA(40, 0);
                //Thread.Sleep(100);

            }

        }

        private void button53_Click_1(object sender, EventArgs e)
        {
            byte resmode;
            Int16 ysize;//Window Height
            Int16 ystart;//Row start

            resmode = Convert.ToByte(textBoxLLA2_MODE.Text);
            ysize = Convert.ToInt16(textBoxLLA2_YSIZE.Text);
            ystart = Convert.ToInt16(textBoxLLA2_YSTART.Text);


            Int16 VTS = 1112;
            Int16 ActiveRows = 1108;
            Int16 ActiveRowEnd = 1091;
            Int16 ActiveRowStart = 4;

            Int16 PixelArray_hight = 1096;//fixed parameter 

            byte X3202 = 0X00;
            byte X3203 = 0X04;
            byte X3206 = 0X04;
            byte X3207 = 0X43;

            ActiveRowStart = (Int16)((PixelArray_hight - ysize - 4 * 2) / 2);// ActiveRowStart = (Int16)((PixelArray_hight - ysize - ystart * 2) / 2);
            ActiveRowEnd = (Int16)(PixelArray_hight - ActiveRowStart - 1);
            VTS = (Int16)(ActiveRowEnd - ActiveRowStart + 25);
            richTextBox1.AppendText(" first set  : "  + "\r\n");

            richTextBox1.AppendText(" set ysize : " + ysize.ToString("D") + "    " + " set ystart :" + ystart.ToString("d") + "\r\n");
            richTextBox1.AppendText(" ActiveRowStart : " + ActiveRowStart.ToString("D") + "      " + " ActiveRowEnd : " + ActiveRowEnd.ToString("d") + "      " + " VTS :" + VTS.ToString("d") + "\r\n");

            X3202 = (byte)(ActiveRowStart / 256);
            X3203 = (byte)(ActiveRowStart % 256);
            X3206 = (byte)(ActiveRowEnd / 256);
            X3207 = (byte)(ActiveRowEnd % 256);

            richTextBox1.AppendText(" X3202: " + X3202.ToString("X2") + "    " + " X3203 :" + X3203.ToString("X2") + "\r\n");
            richTextBox1.AppendText(" X3206: " + X3206.ToString("X2") + "    " + " X3207 :" + X3207.ToString("X2") + "\r\n");
            ActiveRows = (short)(VTS - 4);
             writeCMOS(0X3202, X3202);
             writeCMOS(0X3203, X3203);
             writeCMOS(0X3206, X3206);
             writeCMOS(0X3207, X3207);

             writeCMOS(0X320A, MSB((ushort)ysize));
             writeCMOS(0X320B, LSB((ushort)ysize));
             //writeCMOS(0X3212, MSB((ushort)ystart));
             //writeCMOS(0X3213, LSB((ushort)ystart));

             writeCMOS(0X320E, MSB((ushort)VTS));
             writeCMOS(0X320F, LSB((ushort)VTS));

             writeCMOS(0X322E, MSB((ushort)ActiveRows));
             writeCMOS(0X322F, LSB((ushort)ActiveRows));

            //
             if (ActiveRowStart > ystart)
                 ActiveRowEnd = (Int16)(ActiveRowEnd - (ActiveRowStart-ystart));
             else
                 ActiveRowEnd = (Int16)(ActiveRowEnd+(ystart-ActiveRowStart));

             ActiveRowStart = (Int16)ystart;
             //ActiveRowEnd = (Int16)(PixelArray_hight - ActiveRowStart - 1);
             X3202 = (byte)(ActiveRowStart / 256);
             X3203 = (byte)(ActiveRowStart % 256);
             X3206 = (byte)(ActiveRowEnd / 256);
             X3207 = (byte)(ActiveRowEnd % 256);
             writeCMOS(0X3202, X3202);
             writeCMOS(0X3203, X3203);
             writeCMOS(0X3206, X3206);
             writeCMOS(0X3207, X3207);
             richTextBox1.AppendText(" second set  : " + "\r\n");
             richTextBox1.AppendText(" ActiveRowStart2 : " + ActiveRowStart.ToString("D") + "      " + " ActiveRowEnd2 : " + ActiveRowEnd.ToString("d") + "      " + " VTS :" + VTS.ToString("d") + "\r\n");

             setVMAX((UInt32)(VTS - 1));        

        }

        private void radioButtonDEC_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox42_TextChanged(object sender, EventArgs e)
        {

        }

        private void button100_Click_1(object sender, EventArgs e)
        {
            byte addr;
          


            addr = Convert.ToByte(textBox42.Text, 16);

                byte[] xdata = new byte[10];
                ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xce, addr, addr, 1, xdata);


                richTextBox1.AppendText("read Tps65400 addr(HEX): " + addr.ToString("x") + " " + "read Tps65400 data (HEX):" + xdata[0].ToString("x") + "  " + xdata[1].ToString("X") + "  " + xdata[2].ToString("X") + "  " + xdata[3].ToString("X") + "  " + xdata[4].ToString("X") + "\r\n");
        }

       

        private void button249_Click(object sender, EventArgs e)
        {
            byte bursten;
            byte burst_start;
            UInt16 burst_end;

            bursten = Convert.ToByte(textBox44.Text, 10);
            burst_start = Convert.ToByte(textBox45.Text, 10);
            burst_end = Convert.ToUInt16(textBox46.Text, 10);

           // LowLevelB2(bursten, burst_start, burst_end);
        }

        private void button105_Click_1(object sender, EventArgs e)
        {
            EnableTrain(false);
        }

        void FpgaAutoChannelAlign( )
        {
            writeFPGA2(2, 0);//disable  max10 train
            writeFPGA(54, 2);//RST eris delaycnt       
            Thread.Sleep(50);
            writeFPGA(54, 0);//RST eris delaycnt 

            writeFPGA2(2, 1); //enable max10 train
            Thread.Sleep(50);
            writeFPGA2(2, 0); ;//disable  max10 train

        }
        private void button106_Click_1(object sender, EventArgs e)
        {
            writeFPGA2(2, 0);//disable  max10 train
            writeFPGA(54, 2);//RST eris delaycnt       
            Thread.Sleep(100);
            writeFPGA(54, 0);//RST eris delaycnt 

            writeFPGA2(2, 1); //enable max10 train
            Thread.Sleep(100);
            writeFPGA2(2, 0); ;//disable  max10 train

        }

        private void button250_Click(object sender, EventArgs e)
        {
            UInt16 ERR_num;
            writeFPGA(95, 0);                  //clear all counter in detector
            writeFPGA(95, 1);                  //execute   
            Thread.Sleep(50);
            ERR_num=(UInt16)(readFPGA(30) * 256 + readFPGA(31));
            richTextBox1.AppendText((ERR_num).ToString("d") + " "+Environment.NewLine);

        }

        private void button251_Click(object sender, EventArgs e)
        {
            writeFPGA2(35, 0);
            writeFPGA2(35, 1);
        }

        private void button252_Click(object sender, EventArgs e)
        {
            writeFPGA2(35, 0);
        }

        private void button253_Click(object sender, EventArgs e)
        {
            writeFPGA2(35, 1);
        }

        private void button254_Click(object sender, EventArgs e)
        {
            ushort againR;
            ushort HGCLGC;
            HGCLGC = (ushort)((~(Convert.ToUInt16(textBoxLLA4_HGCLCG.Text))) & 0x01);

            againR = Convert.ToUInt16(textBoxLLA4_AGAINR.Text);

            /////write top gain 
            byte reg10;
            int temp;
            //range 0-63, 6bit
            if (againR > 31) againR = 31;
            else if (againR < 1) againR = 1;
            richTextBox1.AppendText("againR :" + againR.ToString("x2") + Environment.NewLine);
            richTextBox1.AppendText("HGCLGC :" + HGCLGC.ToString("x2") + Environment.NewLine);
            //left shift 4bit
            temp = againR;

            reg10 = (byte)((((temp << 1) + (HGCLGC)) << 1) & 0x7e);

            writeCMOS(10, reg10);

            richTextBox1.AppendText("gain top reg10:" + reg10.ToString("x2") + Environment.NewLine);


 
            /////write bottom gain
             byte reg31,reg32;
             int temp2;
      //range 0-63, 6bit

      temp2 = againR;
      reg31=(byte)((temp2>>3)+0x30);

      reg32 = (byte)((((temp2 << 1) + (HGCLGC)) <<4 )+ 0x02);
      writeCMOS( 31, reg31 );
      
      writeCMOS( 32, reg32 );

      richTextBox1.AppendText("gain bottom reg31:" + reg31.ToString("x2") + Environment.NewLine + " gain bottom  reg32: " + reg32.ToString("x2") + Environment.NewLine);

  
        }

        private void button255_Click(object sender, EventArgs e)
        {
            byte value;
            for (byte i = 0; i < 48; i++)
            {
                value = readCmos(i);
                richTextBox1.AppendText("read cmos ADDRESS :" + i.ToString("D2") + "value:   " + value.ToString("x2") + Environment.NewLine);
            }
        }


        double abs(double a, double b)
    {
        double c =0;
        if(a>b) c = a-b;
        else  c = b - a;

        return c;
    }

      

        private void button268_Click(object sender, EventArgs e)
        {
            LowLevelReadF2();
        }

        private void button269_Click(object sender, EventArgs e)
        {
            scanAllPhase();
        }

        private void button270pfy_Click(object sender, EventArgs e)
        {
            byte value;
            byte index;
            byte req;
            byte[] xdata = new byte[16];
            req = Convert.ToByte(textBox49pfy.Text,16);
            value = Convert.ToByte(textBox51pfy.Text,16);
            index = Convert.ToByte(textBox50pfy.Text,16);  
            
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, req, value, index, 16, xdata);
        }

        private void label111_Click(object sender, EventArgs e)
        {

        }

        private void label59_Click(object sender, EventArgs e)
        {

        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            ASCOM.QHYCCD.libqhyccd.CloseQHYCCD(camhandle);
            //释放资源
            //release resource
            ret = ASCOM.QHYCCD.libqhyccd.ReleaseQHYCCDResource();
            //将连接状态的值改为0
            //The connection state to change the value of 0
            isConnect = 0;

            //richTextBox1.Clear();
            // byte[] cam = new byte[15];

            // cam = id[comboBox2.SelectedIndex];
            //if (isConnect == 0)
            //{
            //根据ID打开相机
            //open the camera depend on ID
            camhandle = ASCOM.QHYCCD.libqhyccd.OpenQHYCCD(id[comboBox4.SelectedIndex]);

            //根据ID赋给相机一个handle
            //According to a handle ID is assigned to the camera
            ASCOM.QHYCCD.libqhyccd.SetQHYCCDStreamMode(camhandle, 0);
            //初始化相机
            //Init camera
            //    ASCOM.QHYCCD.libqhyccd.InitQHYCCD(camhandle);
            //button3.Text = ret.ToString();
            //获取相机的碎片信息
            //Camera fragments of information
            //            ASCOM.QHYCCD.libqhyccd.GetQHYCCDChipInfo(camhandle, ref chipw, ref chiph, ref x, ref h, ref pixelw, ref pixelh, ref bpp);
            //设置相机的bin
            //set bin mode
            //            ASCOM.QHYCCD.libqhyccd.SetQHYCCDBinMode(camhandle, 1, 1);
            //设置相机分辨率
            //set resolution
            //            ASCOM.QHYCCD.libqhyccd.SetQHYCCDResolution(camhandle, 0, 0, x, h);
            //获取照片所占用的空间大小
            //To get photos occupied space size
            //            length = ASCOM.QHYCCD.libqhyccd.GetQHYCCDMemLength(camhandle);
            //将照片所占用的空间大小放入byte数组中
            //Put pictures occupied space in a byte array
            //            rawArray = new byte[length];

            string x;
            x = Encoding.ASCII.GetString(id[comboBox4.SelectedIndex]);

            this.Text = " QHYCCD CAMERA TOOLS  USB  V250219   ------   " + x.ToString();
            //弹出一个提示框，提示连接成功
            //Bring up a prompt box, suggesting the connection is successful
            //DialogResult dr = MessageBox.Show("connect success");

            //将是否连接值改为1，表示已经连接
            //Connect whether value is changed to 1, says it has connections
            xid = x;
            isConnect = 1;
        }

        private void textBox15_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox14_TextChanged(object sender, EventArgs e)
        {

        }

        private void label56_Click(object sender, EventArgs e)
        {

        }

        private void textBoxWordDelay_TextChanged(object sender, EventArgs e)
        {

        }

        private void button150_Click(object sender, EventArgs e)
        {

        }

        private void button149_Click(object sender, EventArgs e)
        {

        }

        private void button151_Click(object sender, EventArgs e)
        {

        }

        private void button152_Click(object sender, EventArgs e)
        {

        }

        private void button153_Click(object sender, EventArgs e)
        {

        }

        private void button146_Click(object sender, EventArgs e)
        {

        }

        private void button148_Click(object sender, EventArgs e)
        {

        }

        private void textBox25_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox43_TextChanged(object sender, EventArgs e)
        {

        }

       

        void SetXtrigExpTimes(UInt32 times_us)// 
        {
            //uint is us     input max value is 2^32=  4294sec

            UInt64 times_40ns = times_us * 1000 / 40;

            UInt64 BYTE4 = ( times_40ns & ~0xFFFFFF00FFFFFFFF ) >> 32;
            UInt64 BYTE3 = ( times_40ns & ~0xFFFFFFFF00FFFFFF ) >> 24;
            UInt64 BYTE2 = ( times_40ns & ~0xFFFFFFFFFF00FFFF ) >> 16;
            UInt64 BYTE1 = ( times_40ns & ~0xFFFFFFFFFFFF00FF ) >> 8;
            UInt64 BYTE0 = ( times_40ns & ~0xFFFFFFFFFFFFFF00 );


            byte usByte4 = (byte) BYTE4;
            byte usByte3 = (byte)BYTE3;
            byte usByte2 = (byte)BYTE2;
            byte usByte1 = (byte)BYTE1;
            byte usByte0 = (byte)BYTE0;

            writeFPGA(154, usByte4 ); // for T35+SONY GLOBAL only
            writeFPGA(155, usByte3); // for T35+SONY GLOBAL only
            writeFPGA(156, usByte2); // for T35+SONY GLOBAL only
            writeFPGA(157, usByte1); // for T35+SONY GLOBAL only
            writeFPGA(158, usByte0); // for T35+SONY GLOBAL only


        }

        void setSingleFrameEnable(bool i)
        {
            if (i == true) writeFPGA(57, 1);    //单帧
            else writeFPGA(57, 0);
        }

        void setSingleFrameStartEnd(byte start, byte end)
        {
            writeFPGA(50, start);
            writeFPGA(51, MSB(end));
            writeFPGA(52, LSB(end));
        }

        void SetTfppTimes(UInt32 time)
        {
            writeFPGA(162, MSB3(time));
            writeFPGA(161, MSB2(time));
            writeFPGA(160, MSB1(time));
            writeFPGA(159, MSB0(time));
        }

        void SetTrppTimes(UInt32 time)
        {
            writeFPGA(166, MSB3(time));
            writeFPGA(165, MSB2(time));
            writeFPGA(164, MSB1(time));
            writeFPGA(163, MSB0(time));

        }
        private void button16_Click_1(object sender, EventArgs e)
        {
            
            UInt32  xtrigtimes_ms = Convert.ToUInt32(textBox13.Text, 10);
            setIDLE();

            setHMAX(172);//
            setVMAX(2194);//2250

            writeFPGA(152, 0);   //set xtrig reset 
            SetTfppTimes(6725);
            SetTrppTimes(377540);
            writeFPGA(36, 0);   
            SetXtrigExpTimes(xtrigtimes_ms*1000);//SET exposure times                    
            writeCMOS(0x3400, 0x09);//set CMOS register is xtrig mode 

             writeFPGA(152, 1);   //disable xtrig reset 
           
           setSingleFrameStartEnd(0,2);
           setPatchNumber(32001);
           setSingleFrameEnable(true);

        }

       

        private void button19_Click_1(object sender, EventArgs e)
        {


         
            //writeFPGA(152, 0);   //set xtrig reset 
            setIDLE();
            releaseIDLE();
            //writeFPGA(152, 1);   //set xtrig reset 

           
            
            //setIDLE();
           // writeFPGA(152, 0);   //set xtrig reset 


        }

        private void button40_Click_2(object sender, EventArgs e)
        {
            if (checkBox22.Checked == true)
            {
                writeFPGA(39, 2);//set gpio trig 
                writeFPGA(58, 1);//enable trig in only
                writeFPGA(57, 1);//EnableBurstMode 
                writeFPGA(50, 1);//BurstStart  1
                writeFPGA(51, 0);//BurstEnd[15..8] 0
                writeFPGA(52, 3);//BurstEnd[7:0] 3
                writeFPGA(44, 0x80);//PATCHVnumber 
                writeFPGA(43, 0x0c);//PATCHVnumber 
                writeFPGA(42, 0x00);//PATCHVnumber 
                writeFPGA(41, 0x00);//PATCHVnumber 
            }
            else
            {
                writeFPGA(39, 5);//close GPIO TRIG  
                writeFPGA(58, 0);//disable trig in/ trig out 
                writeFPGA(57, 0);//DISABEL BurstMode 
                writeFPGA(50, 0);//BurstStart  0
                writeFPGA(51, 0);//BurstEnd[15..8] 0
                writeFPGA(52, 0);//BurstEnd[7:0] 3
                writeFPGA(44, 0x00);//PATCHVnumber 
                writeFPGA(43, 0x00);//PATCHVnumber 
                writeFPGA(42, 0x00);//PATCHVnumber 
                writeFPGA(41, 0x00);//PATCHVnumber 
            }


        }

        private void button41_Click_1(object sender, EventArgs e)
        {
            if (checkBox8.Checked == true)
            {
                writeFPGA(39, 2);//set gpio trig 
                writeFPGA(58, 2);//enable trig out only
            }
            else
            {
                writeFPGA(39, 5);//disable  gpio trig 
                writeFPGA(58, 0);//disable trig in/ trig out 

            }
        }

        private void button46_Click_2(object sender, EventArgs e)
        {
            if (checkBox14.Checked == true)
            {
                writeFPGA(39, 2);//set gpio trig 
                writeFPGA(58, 3);//enable trig in and trig out
                writeFPGA(57, 1);//EnableBurstMode 
                writeFPGA(50, 1);//BurstStart  1
                writeFPGA(51, 0);//BurstEnd[15..8] 0
                writeFPGA(52, 3);//BurstEnd[7:0] 3
                writeFPGA(44, 0x80);//PATCHVnumber 
                writeFPGA(43, 0x0c);//PATCHVnumber 
                writeFPGA(42, 0x00);//PATCHVnumber 
                writeFPGA(41, 0x00);//PATCHVnumber 
            }
            else
            {
                writeFPGA(39, 5);//close GPIO TRIG  
                writeFPGA(58, 0);//disable trig in/ trig out 
                writeFPGA(57, 0);//DISABEL BurstMode 
                writeFPGA(50, 0);//BurstStart  0
                writeFPGA(51, 0);//BurstEnd[15..8] 0
                writeFPGA(52, 0);//BurstEnd[7:0] 3
                writeFPGA(44, 0x00);//PATCHVnumber 
                writeFPGA(43, 0x00);//PATCHVnumber 
                writeFPGA(42, 0x00);//PATCHVnumber 
                writeFPGA(41, 0x00);//PATCHVnumber 
            }
        }

        private void button53_Click_2(object sender, EventArgs e)
        {

            UInt32 value;
            UInt16 TIMES;
            TIMES = Convert.ToUInt16(textBox28.Text, 10);

            value = (UInt32)((TIMES * 1000000) / 40);

            if (TIMES < 100000)//max 100s
            {
                writeFPGA(144, 0X00);
                writeFPGA(145, MSB3(value));
                writeFPGA(146, MSB2(value));
                writeFPGA(147, MSB1(value));
                writeFPGA(148, MSB0(value));
            }
        }

        private void button101_Click_1(object sender, EventArgs e)
        {


            CMOSREG[0] = 0x43;
            CMOSREG[1] = 0Xb0;
            CMOSREG[2] = 0X03;
            CMOSREG[3] = 0X42;
            CMOSREG[4] = 0X31;

            CMOSREG[5] = 0X1b;
            CMOSREG[6] = 0X38;
            CMOSREG[7] = 0Xfe;
            CMOSREG[8] = 0Xdd;
            CMOSREG[9] = 0X3e; //2019.4.26 by Qiu  reduce the vertical FPN noise. Change from 0x3e to 0x32.

            CMOSREG[10] = 0X06;
            CMOSREG[11] = 0X63;
            CMOSREG[12] = 0XFF;
            CMOSREG[13] = 0X20;
            CMOSREG[14] = 0X80;

            CMOSREG[15] = 0X08;//2019.3.17 by Qiu lvds current . increase from 0X0a to 0x0F
            CMOSREG[16] = 0Xd1;
            CMOSREG[17] = 0X00;
            CMOSREG[18] = 0X00;
            CMOSREG[19] = 0Xa4;

            CMOSREG[20] = 0Xb8;
            CMOSREG[21] = 0X00;
            CMOSREG[22] = 0X00;
            CMOSREG[23] = 0X20;
            CMOSREG[24] = 0Xc8;

            CMOSREG[25] = 0X29;  //change from 0x29 to 0x28 to rmeove the background glow
            CMOSREG[26] = 0X14;  //2-CMS   // change from 0x0c to 0x8c to remove the background glow
            CMOSREG[27] = 0X80;
            CMOSREG[28] = 0X80;
            CMOSREG[29] = 0X02; //  2018.9.8 changed from 0x12 to 0x32 to enable the CMOS DDR CLOCK output. DDRCLOCK=1/2 input LVDS clock

            CMOSREG[30] = 0X9a;
            CMOSREG[31] = 0X30;
            CMOSREG[32] = 0X32;
            CMOSREG[33] = 0X9f;
            CMOSREG[34] = 0XF9;
            CMOSREG[35] = 0X04;
            CMOSREG[36] = 0Xff;
            CMOSREG[37] = 0xe2;
            CMOSREG[38] = 0X20;
            CMOSREG[39] = 0X00;
            CMOSREG[40] = 0X00;
            CMOSREG[41] = 0Xea;
            CMOSREG[42] = 0X1f;
            CMOSREG[43] = 0X87;
            CMOSREG[44] = 0X90;
            CMOSREG[45] = 0X00;
            CMOSREG[46] = 0X00;
            CMOSREG[47] = 0X00;

            for (byte i = 0; i < 48; i++)
            {
                writeCMOS(i, CMOSREG[i]);
            }
        }

        private void button102_Click_1(object sender, EventArgs e)
        {

            CMOSREG[0] = 0x43;
            CMOSREG[1] = 0X81;//lp 0x81;normal 0Xb0;
            CMOSREG[2] = 0X03;
            CMOSREG[3] = 0X42;
            CMOSREG[4] = 0X20;//lp 0x20;normal 0X31;

            CMOSREG[5] = 0X0B;//lp 0x0b;normal 0X1b;
            CMOSREG[6] = 0X38;
            CMOSREG[7] = 0Xfe;
            CMOSREG[8] = 0Xdd;
            CMOSREG[9] = 0X2C;//lp 0X2C;//normal 0X3e; //2019.4.26 by Qiu  reduce the vertical FPN noise. Change from 0x3e to 0x32.

            CMOSREG[10] = 0X06;
            CMOSREG[11] = 0X23;//lp 0X23;//normal 0X63;
            CMOSREG[12] = 0XFF;
            CMOSREG[13] = 0X20;
            CMOSREG[14] = 0X80;

            CMOSREG[15] = 0X08;//2019.3.17 by Qiu lvds current . increase from 0X0a to 0x0F
            CMOSREG[16] = 0Xd1;
            CMOSREG[17] = 0X00;
            CMOSREG[18] = 0X00;
            CMOSREG[19] = 0Xa4;

            CMOSREG[20] = 0Xb8;
            CMOSREG[21] = 0X00;
            CMOSREG[22] = 0X00;
            CMOSREG[23] = 0X20;
            CMOSREG[24] = 0Xc8;

            CMOSREG[25] = 0X29;  //change from 0x29 to 0x28 to rmeove the background glow
            CMOSREG[26] = 0X14;
            CMOSREG[27] = 0x87;//lp 0X87;//normal 0X80;
            CMOSREG[28] = 0X9e;//lp 0X9E;//normal 0x80
            CMOSREG[29] = 0x79;//lp 0X79;//normal 0X02; //  2018.9.8 changed from 0x12 to 0x32 to enable the CMOS DDR CLOCK output. DDRCLOCK=1/2 input LVDS clock

            CMOSREG[30] = 0X9a;
            CMOSREG[31] = 0x00;//lp 0X00;//normal 0X30;
            CMOSREG[32] = 0x50;//lp 0X50;//normal 0X32;
            CMOSREG[33] = 0X9f;
            CMOSREG[34] = 0XF9;
            CMOSREG[35] = 0X04;
            CMOSREG[36] = 0Xff;
            CMOSREG[37] = 0xc0;// lp 0XC0;//normal 0xe2;
            CMOSREG[38] = 0X20;
            CMOSREG[39] = 0X00;
            CMOSREG[40] = 0X00;
            CMOSREG[41] = 0Xea;
            CMOSREG[42] = 0X1f;
            CMOSREG[43] = 0x81;//lp 0X81;//normal 0X87;
            CMOSREG[44] = 0X90;
            CMOSREG[45] = 0X00;
            CMOSREG[46] = 0X00;
            CMOSREG[47] = 0X00;

            for (byte i = 0; i < 48; i++)
            {
                writeCMOS(i, CMOSREG[i]);
            }
        }

        private void button103_Click_1(object sender, EventArgs e)
        {

            ushort index;
            index = Convert.ToByte(textBox43.Text);
            byte[] xdata = new byte[64];
            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestWrite(camhandle, 0xd5, 0, index, 1, xdata);

            richTextBox1.AppendText("LOW LEVEL D5 :  " + index.ToString("") + Environment.NewLine);

        }

        private void button104_Click(object sender, EventArgs e)
        {
            ushort index;       
            index = Convert.ToUInt16(textBoxFpgaIndex.Text, 10);
           
            byte value;// = new byte[6];

            value = readFPGA((ushort)(index));
            richTextBox1.AppendText("read fpga" + index.ToString() + ":     " + (value).ToString() + Environment.NewLine);
           
           
        }

        private void button146_Click_1(object sender, EventArgs e)
        {
            /*
            LowLevelA3(0);

            Thread.Sleep(200);

            byte resmode;
            ushort xsize;
            ushort xstart;
            ushort ysize;
            ushort ystart;

            resmode = 0;
            xsize = 0;
            xstart =0;
            ysize =4;
            ystart = 0;

            LowLevelA2(resmode, xsize, xstart, ysize, ystart);
            Thread.Sleep(200);
            */
            setIDLE();
            setHMAX(500);
            setVMAX(50);
            releaseIDLE();

            writeFPGA(99, 1);
            writeFPGA(100, 0);
            writeFPGA(101, 4);
            writeFPGA(102, 0);

            writeFPGA(150, 1);




        }

        private void button147_Click_1(object sender, EventArgs e)
        {
            InitCMOS_IMX568(0, 0);
        }

        private void button148_Click_1(object sender, EventArgs e)
        {
            UInt32 xtrigtimes_ms = Convert.ToUInt32(textBox13.Text, 10);
            setIDLE();

          
            writeFPGA(152, 0);   //set xtrig reset 
           
            writeFPGA(36, 0);
                           
            //writeCMOS(0x3400, 0x09);//set CMOS register is xtrig mode 

     

            setSingleFrameStartEnd(0, 2);
            setPatchNumber(32001);
            setSingleFrameEnable(false);
        }

        private void button149_Click_1(object sender, EventArgs e)
        {
            setSingleFrameStartEnd(0, 2);
            setPatchNumber(32001);
            setSingleFrameEnable(true);
        }

        private void textBox52_TextChanged(object sender, EventArgs e)
        {

        }

        private void label129_Click(object sender, EventArgs e)
        {

        }

        private void textBox42ysk_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox45_YSK_TextChanged(object sender, EventArgs e)
        {

        }

        private void label113_Click(object sender, EventArgs e)
        {

        }

        private void textBox43ysk_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox41ysk_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxLLA0_MODE_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxLLA0_XBIN_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox50pfy_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox51pfy_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox49pfy_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox14_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void textBoxLLA0_YBIN_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButtonHEX_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void radioButton8bit_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBoxLLA8_OFFSET1R_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxLLA4_DGAINR_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxLLA4_EGAIN_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox40_TextChanged(object sender, EventArgs e)
        {

        }

        private void label60_Click(object sender, EventArgs e)
        {

        }



        private void label28_Click(object sender, EventArgs e)
        {

        }

        private void textBoxLLBD_EN_Click(object sender, EventArgs e)
        {

        }

        private void textBoxLLA2_YSIZE_TextChanged(object sender, EventArgs e)
        {

        }

        private void trig_exposeure_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox44_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxSingleFrameEnd_TextChanged(object sender, EventArgs e)
        {

        }

        private void label152_Click(object sender, EventArgs e)
        {

        }

        private void textBox47_TextChanged(object sender, EventArgs e)
        {

        }

        private void button211PFY_Click(object sender, EventArgs e)
        {
            BurstModeRun(); 
        }

        private void button212_pfy_Click(object sender, EventArgs e)
        {
            AutoChannelAlign();
        }

        private void textBox26_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox75_TextChanged(object sender, EventArgs e)
        {

        }

        private void tabPageLVDS_Click(object sender, EventArgs e)
        {

        }

        private void textBox48_TextChanged(object sender, EventArgs e)
        {

        }

        private void label134_Click(object sender, EventArgs e)
        {

        }

        private void label153_Click(object sender, EventArgs e)
        {

        }

        private void tabPageLowLevel1_Click(object sender, EventArgs e)
        {

        }

        private void button211_Click_1(object sender, EventArgs e)
        {
            byte bursten;
            byte burst_start;
            UInt16 burst_end;
            UInt16 hsync_stled;
            UInt16 hsync_edled;
            byte m6inline_leden;
            UInt16 m6inline_st;
            UInt16 m6inline_ed;
            byte test_mode;

            bursten = Convert.ToByte(textBox80.Text, 10);
            burst_start = Convert.ToByte(textBox79.Text, 10);
            burst_end = Convert.ToUInt16(textBox78.Text, 10);
            hsync_stled = Convert.ToUInt16(textBox77.Text, 10);
            hsync_edled = Convert.ToUInt16(textBox76.Text, 10);
            m6inline_leden = Convert.ToByte(textBox85.Text, 10);
            m6inline_st = Convert.ToUInt16(textBox84.Text, 10);
            m6inline_ed = Convert.ToUInt16(textBox83.Text, 10);
            test_mode = Convert.ToByte(textBox82.Text, 10);

            writeFPGA(50, burst_start);
            writeFPGA(51, MSB1(burst_end));
            writeFPGA(52, MSB0(burst_end));
            writeFPGA(57, bursten);

            writeFPGA(121, MSB0(hsync_stled));
            writeFPGA(122, MSB1(hsync_stled));

            writeFPGA(123, MSB0(hsync_edled));
            writeFPGA(124, MSB1(hsync_edled));

            writeFPGA(125, m6inline_leden);

            writeFPGA(126, MSB0(m6inline_st));
            writeFPGA(127, MSB1(m6inline_st));

            writeFPGA(128, MSB0(m6inline_ed));
            writeFPGA(149, MSB1(m6inline_ed));

            writeFPGA(39, test_mode);


//            LowLevelB2(bursten, burst_start, burst_end, hsync_stled, hsync_edled, m6inline_leden, m6inline_st, hsync_edled, test_mode);
        }

        private void label155_Click(object sender, EventArgs e)
        {

        }

        private void label154_Click(object sender, EventArgs e)
        {

        }

        private void label161_Click(object sender, EventArgs e)
        {

        }

        private void textBox80_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox79_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox78_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }
        void writeFPGA_EXtend(UInt16 index, UInt32 value)
        {
            // Disable write enable
            writeFPGA(228, 0x00);
            // Disable read enable
            writeFPGA(229, 0x00);
            // Write address using address_mask
            byte index_mask = 0x03;
            byte value_mask = 0x0f;
            byte address = 223;

            while (index_mask > 0)
            {
                writeFPGA(address, (byte)(index & 0x00FF));
                address--;
                index_mask >>= 1;
                index >>= 8;
            }

            // Write data using data_mask
            address = 227;
            while (value_mask > 0)
            {
                writeFPGA(address, (byte)(value & 0x000000FF));
                address--;
                value_mask >>= 1;
                value >>= 8;
            }

            // Enable write enable
            writeFPGA(228, 0x08);
            //Disable write enable
            writeFPGA(228, 0x00);
        }
        private void button246_Click_1(object sender, EventArgs e)
        {
            UInt16 index;
            UInt32 value;
            //byte value_mask;

            index = Convert.ToUInt16(textBoxFpgaIndex.Text, 10);
            value = Convert.ToUInt32(textBoxFpgaValue.Text, 16);
            writeFPGA_EXtend(index, value);
        }
        UInt32 readFPGA_EXtend(UInt16 index)
        {

            UInt32 value = 0;
            byte value8 = 0;
            byte index_mask = 0x03;
            //Disable write enable
            writeFPGA(228, 0x00);
            // Disable read enable
            writeFPGA(229, 0x00);

            // Write address using address_mask
            byte address = 223;
            while (index_mask > 0)
            {
                writeFPGA(address, (byte)(index & 0x00FF));
                address--;
                index_mask >>= 1;
                index >>= 8;
            }

            // Enable read enable
            writeFPGA(229, 0x08);

            address = 60;
            for (byte i = 0; i < 4; i++)
            {
                value8 = readFPGA(address);
                address++;
                value = (value << 8) | value8;

            }


            // Disable read enable
            writeFPGA(229, 0x00);

            return value;
        }
        private void button212_Click(object sender, EventArgs e)
        {
            UInt32 value;// = new byte[6];
                         //byte index_mask;


            UInt16 index;
            index = Convert.ToUInt16(textBoxFpgaIndex.Text, 10);

            // index_mask = 0x03;      
            value = readFPGA_EXtend(index);
            richTextBox1.AppendText("read Expand fpga HEX: " + index.ToString() + ":     " + (value).ToString("X8") + Environment.NewLine);

        }

        private void checkBox20_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button180_Click_3(object sender, EventArgs e)
        {
            UInt32 PatchNumber;
            PatchNumber = Convert.ToUInt32(textBox81.Text);
            setPatchNumber(PatchNumber);
        }

        private void textBox81_TextChanged(object sender, EventArgs e)
        {

        }

        private void button144_Click(object sender, EventArgs e)
        {

        }

        private void tabPageQHY4040_Click(object sender, EventArgs e)
        {

        }
        void SetGpioConfig(byte value)
        {
            for (byte i = 0; i < 6; i++)
            {

                writeFPGA(61, value);
                Thread.Sleep(200);
                writeFPGA(61, 0xf0);
                Thread.Sleep(200);
            }
            writeFPGA(61, value);
        }
        private void button18_Click_1(object sender, EventArgs e)
        {
            SetGpioConfig(0x20);
        }

        private void button15_Click_2(object sender, EventArgs e)
        {
            SetGpioConfig(0x30);
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            SetGpioConfig(0x40);
        }

        private void button10_Click_1(object sender, EventArgs e)
        {
            SetGpioConfig(1);
        }

        private void button14_Click_2(object sender, EventArgs e)
        {
            SetGpioConfig(0x20);
            SetGpioConfig(0x30);
            SetGpioConfig(0x40);
            SetGpioConfig(1);
            writeFPGA(61, 0xf0);
        }

        private void button21_Click_1(object sender, EventArgs e)
        {
            writeFPGA(212, 0x01);
            Thread.Sleep(200);
            writeFPGA(61, 0x00);
        }

        private void checkBox21_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button22_Click_1(object sender, EventArgs e)
        {
            writeFPGA_EXtend(281, 0x02);//write trigsource      reg
            writeFPGA_EXtend(282, 0x02);//write trigmode        reg
            writeFPGA_EXtend(283, 0x02);//write triginfunction  reg
            //writeFPGA_EXtend(288, 0x02);//write filter          reg
            writeFPGA(57, 1);//set burst mode
            writeFPGA(50, 1);//burst start
            writeFPGA(51, 0);//*****
            writeFPGA(52, 3);//burst end
            setPatchNumber(32001);//Supplement package

            if (checkBox21.Checked == true)
            {
                writeFPGA_EXtend(284, 0x02);//write trigoutfunction reg   mode b
                LowLevelAB(0X01);//Set Minimum frame period
            }
            else
            {
                writeFPGA_EXtend(284, 0x03);//mode a
                writeFPGA_EXtend(287, 0x64);//trigoutlong   default:100us 
            }
        }

        private void textBoxLLA2_XSIZE_TextChanged(object sender, EventArgs e)
        {

        }

        private void label88_Click(object sender, EventArgs e)
        {

        }

        private void button43_Click(object sender, EventArgs e)
        {

        }

        private void button160_Click_1(object sender, EventArgs e)
        {

        }

        private void button102_Click_2(object sender, EventArgs e)
        {

        }

        private void button152_Click_1(object sender, EventArgs e)
        {

        }

        private void button151_Click_1(object sender, EventArgs e)
        {

        }

        private void button211_Click_2(object sender, EventArgs e)
        {

        }

        private void label146_Click(object sender, EventArgs e)
        {

        }

        private void button179_Click_1(object sender, EventArgs e)
        {

        }

        private void textBoxLLA5_USBTRAFFIC_TextChanged(object sender, EventArgs e)
        {

        }

        private void button270_Click(object sender, EventArgs e)
        {

        }

        private void label40_Click(object sender, EventArgs e)
        {

        }

        private void button274_Click(object sender, EventArgs e)
        {

        }

        private void label85_Click(object sender, EventArgs e)
        {

        }

        private void label84_Click(object sender, EventArgs e)
        {

        }

        private void textBoxTranceiverRegisterValue_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxTranceiverRegisterAddress_TextChanged(object sender, EventArgs e)
        {

        }

        private void label95_Click(object sender, EventArgs e)
        {

        }

        private void button155_Click_1(object sender, EventArgs e)
        {

        }

        private void button312_Click(object sender, EventArgs e)
        {

        }

        private void button311_Click(object sender, EventArgs e)
        {

        }

        private void button210_Click(object sender, EventArgs e)
        {

        }

        private void hScrollBar18_Scroll(object sender, ScrollEventArgs e)
        {

        }

        private void hScrollBar13_Scroll_1(object sender, ScrollEventArgs e)
        {

        }

        private void hScrollBar12_Scroll_1(object sender, ScrollEventArgs e)
        {

        }

        private void button154_Click(object sender, EventArgs e)
        {

        }

        private void button153_Click_2(object sender, EventArgs e)
        {

        }

        private void button150_Click_1(object sender, EventArgs e)
        {

        }

        private void button209_Click_1(object sender, EventArgs e)
        {

        }

        private void button55_Click_1(object sender, EventArgs e)
        {

        }

        private void button44_Click_1(object sender, EventArgs e)
        {

        }

        private void button31_Click_1(object sender, EventArgs e)
        {

        }

        private void button81_Click_1(object sender, EventArgs e)
        {

        }

        private void button52_Click(object sender, EventArgs e)
        {

        }

        private void button43_Click_1(object sender, EventArgs e)
        {
            writeFPGA_EXtend(281, 0x02);//optic trig in en
        }

        private void button44_Click_2(object sender, EventArgs e)
        {

        }

        private void button131_Click(object sender, EventArgs e)
        {


            byte trigout_mode;

            trig_mode = 0x0D;//filter en , RBI DISABLE  ,mode b ，trig in en

            if (checkBox28.Checked == true)
            {
                trigout_mode = (byte)(trig_mode | 0x01);//filter en             
            }
            else
            {
                trigout_mode = (byte)(trig_mode & 0xFE);//filter disable   
                writeFPGA(145, 0);
                writeFPGA(146, 0);
                writeFPGA(147, 0);
                writeFPGA(148, 10);
                //richTextBox1.AppendText(" is rig_mode b " + Environment.NewLine);
            }
            trig_mode = trigout_mode;

            writeFPGA(58, trig_mode);//filter en , RBI en,mode b 
            //LowLevelAB(0X01);//******

            writeFPGA(39, 2);//set to mode 2
            writeFPGA(142, 1);//TrigSignalEn 


            writeFPGA(50, 1);//*****
            writeFPGA(51, 0);//*****
            writeFPGA(52, 3);//*****

            writeFPGA(57, 1);//*****

            //set filter times 100ms

            writeFPGA(144, 0x00);
            writeFPGA(145, 0x00);
            writeFPGA(146, 0x26);
            writeFPGA(147, 0x25);
            writeFPGA(148, 0xA0);

            gettrigftame();
            Thread.Sleep(1000);
            gettrigftame();

            setPatchNumber(32001);
        }

        private void button139_Click(object sender, EventArgs e)
        {
            byte trigout_mode;
            //richTextBox1.AppendText("mode  b  trig_mode =" + trig_mode.ToString() + Environment.NewLine);

            if (checkBox27.Checked == true) trigout_mode = (byte)(trig_mode & 0xfb);// set trig_mode[2]==0
            else trigout_mode = (byte)(trig_mode | 0x04);//mode b, set trig_mode[2]==1

            writeFPGA(58, trigout_mode);//mode b 
            writeFPGA(39, 2);
            writeFPGA(142, 1);
            LowLevelAB(0X00);//******

            trig_mode = trigout_mode;
            richTextBox1.AppendText("mode b  trigout_mode = 0x " + trigout_mode.ToString("x2") + Environment.NewLine);

        }

        private void button134_Click(object sender, EventArgs e)
        {
            writeFPGA(45, 0X00);
            writeFPGA(46, 0X00);//VMAX_2 ==0 ; 
            trig_mode = 0;
            writeFPGA(39, 5);//set to mode 5

            writeFPGA(50, 1);
            writeFPGA(51, 0);
            writeFPGA(52, 3);

            writeFPGA(57, 0);//EnableBurstMode
            writeFPGA(58, trig_mode);//filter disable  , RBI disable ,mode b disable,trig in disable

        }

        private void button132_Click(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            ushort value = 0x000E;
            ushort index = 0X000E;

            UInt32 ActualExposureTime;
            UInt32 isLongExposureMode;
            UInt32 switchpoint;

            UInt32 TB3_TIMES;
            UInt32 TB2_TImes;

            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd5, value, index, 64, xdata);

            richTextBox1.Clear();



            ActualExposureTime = (UInt32)(xdata[20] * 256 * 256 * 256 + xdata[21] * 256 * 256 + xdata[22] * 256 + xdata[23]);
            switchpoint = (UInt32)(xdata[24] * 256 * 256 * 256 + xdata[25] * 256 * 256 + xdata[26] * 256 + xdata[27]);
            isLongExposureMode = xdata[32];

            if (isLongExposureMode == 1) TB3_TIMES = ActualExposureTime - switchpoint;
            else TB3_TIMES = 0;

            if (isLongExposureMode == 1) TB2_TImes = ActualExposureTime;
            else TB2_TImes = switchpoint;


            label68.Text = "TB5 is  " + switchpoint.ToString() + "us";
            label62.Text = "TB3 is  " + TB3_TIMES.ToString() + "us";
            label161.Text = "TB2 is  " + TB2_TImes.ToString() + "us";

            //  richTextBox1.AppendText("SwitchPoint=" + SwitchPoint.ToString() + Environment.NewLine);


        }

        private void button127_Click_1(object sender, EventArgs e)
        {
            byte trig_mode_t;
            trig_mode_t = (byte)(trig_mode | 0x10);//optic trig in  en 
            writeFPGA(58, trig_mode_t);//optic trig in  en 
            trig_mode = trig_mode_t;
        }

        private void button133_Click_1(object sender, EventArgs e)
        {
            UInt32 value;
            UInt16 TIMES;
            TIMES = Convert.ToUInt16(textBox28.Text, 10);

            value = (UInt32)((TIMES * 1000000) / 40);

            if (TIMES < 100000)//max 100s
            {
                writeFPGA(144, 0X00);
                writeFPGA(145, MSB3(value));
                writeFPGA(146, MSB2(value));
                writeFPGA(147, MSB1(value));
                writeFPGA(148, MSB0(value));
            }

        }

        private void checkBox28_CheckedChanged(object sender, EventArgs e)
        {
            byte trigout_mode;
            //  richTextBox1.AppendText("filter  trig_mode =" + trig_mode.ToString() + Environment.NewLine);
            if (checkBox28.Checked == true)
            {

                trigout_mode = (byte)(trig_mode | 0x01);//filter en 
                //     richTextBox1.AppendText("filter true trigout_mode =" + trigout_mode.ToString() + Environment.NewLine);
                writeFPGA(58, trigout_mode);//filter en 

            }
            else
            {

                trigout_mode = (byte)(trig_mode & 0xFE);//filter disable 
                writeFPGA(58, trigout_mode);//filter disable 
                //    richTextBox1.AppendText("filter false trigout_mode =" + trigout_mode.ToString() + Environment.NewLine);

            }
            trig_mode = trigout_mode;
        }

        private void button128_Click(object sender, EventArgs e)
        {
            trig_mode = 0;
            writeFPGA(39, 5);//set to mode 5

            writeFPGA(50, 1);
            writeFPGA(51, 0);
            writeFPGA(52, 3);

            //writeFPGA(57, 0);//EnableBurstMode
            writeFPGA(58, trig_mode);//filter disable  , RBI disable ,mode b disable,trig in disable

        }

        private void button126_Click(object sender, EventArgs e)
        {
            UInt32 PatchNumber;
            PatchNumber = Convert.ToUInt32(textBox21.Text);
            setPatchNumber(PatchNumber);
        }

        private void button146_Click_2(object sender, EventArgs e)
        {
            UInt16 startFrame;

            writeFPGA(57, 1);//EnableBurstMode

            //writeFPGA(45, 0X00);
            //writeFPGA(46, 0X00);//VMAX_2 ==0 ; 
            //writeFPGA(58, 0x00);//filter disable , RBI disable,mode a

            startFrame = Convert.ToUInt16(textBox34.Text, 10);


            writeFPGA(50, startFrame);
        }

        private void button149_Click_2(object sender, EventArgs e)
        {
            UInt16 EndFrame;
            EndFrame = (UInt16)((Convert.ToUInt16(textBox35.Text, 10)));

            writeFPGA(51, MSB(EndFrame));
            writeFPGA(52, LSB(EndFrame));
        }

        private void checkBox30_CheckedChanged(object sender, EventArgs e)
        {
            byte trigout_mode;
            //richTextBox1.AppendText("mode  b  trig_mode =" + trig_mode.ToString() + Environment.NewLine);  
            if (checkBox30.Checked == true)
            {
                if ((trig_mode & 0x04) == 4)//mode B {
                {
                    trigout_mode = (byte)(trig_mode | 0x02);//mode b rbi en 
                    writeFPGA(58, trigout_mode);//mode b rbi en 
                    trig_mode = trigout_mode;
                }
                LowLevelAB(0x01);

                //richTextBox1.AppendText("mode  b  burst rbi en trig_mode =" + trig_mode.ToString() + Environment.NewLine);

            }
        }
private void checkBox29_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox29.Checked == true)
                enableStampFrameCounter(true);
            else
                enableStampFrameCounter(false);
        }

        private void button117_Click_1(object sender, EventArgs e)
        {
            writeFPGA(4, 0);
            labelBDGPS.Text = "Current is GPS mode";
        }

        private void button122_Click(object sender, EventArgs e)
        {
            writeFPGA(39, 6);
        }

        private void button124_Click(object sender, EventArgs e)
        {
            UInt16 value;
            value = Convert.ToUInt16(textBox20.Text, 10);
            writeFPGA(121, LSB(value));
            writeFPGA(122, MSB(value));
        }

        private void button125_Click_1(object sender, EventArgs e)
        {
            UInt16 value;
            value = Convert.ToUInt16(textBox19.Text, 10);
            writeFPGA(123, LSB(value));
            writeFPGA(124, MSB(value));
        }

        private void button112_Click_1(object sender, EventArgs e)
        {
            UInt16 value;
            value = Convert.ToUInt16(textBox18.Text, 10);
            writeFPGA(126, LSB(value));
            writeFPGA(127, MSB(value));
        }

        private void button118_Click_1(object sender, EventArgs e)
        {
            UInt16 value;
            value = Convert.ToUInt16(textBox17.Text, 10);
            writeFPGA(128, LSB(value));
            writeFPGA(149, MSB(value));
        }

        private void checkBox26_CheckedChanged(object sender, EventArgs e)
        {
            byte trigout_mode;
            //richTextBox1.AppendText("RBI  trig_mode =" + trig_mode.ToString() + Environment.NewLine);

            if (checkBox26.Checked == true)
            {
                LowLevelAB(0x01);
                trigout_mode = (byte)(trig_mode | 0x02);//RBI en 
                writeFPGA(58, trigout_mode);//RBI  en 

                writeFPGA(50, 2);
                writeFPGA(51, 0);
                writeFPGA(52, 4);//RBI en
                                 //richTextBox1.AppendText("RBI true trigout_mode =" + trigout_mode.ToString() + Environment.NewLine);

            }
            else
            {
                // LowLevelAB(0x00);
                trigout_mode = (byte)(trig_mode & 0xFD);//RBI disable 
                writeFPGA(58, trigout_mode);//RBI disable 

                writeFPGA(50, 1);
                writeFPGA(51, 0);
                writeFPGA(52, 3);//RBI disable
                                 //richTextBox1.AppendText("RBI false trigout_mode =" + trigout_mode.ToString() + Environment.NewLine);

            }

            trig_mode = trigout_mode;

        }

        private void button141_Click(object sender, EventArgs e)
        {
            if (SDKAPI == false)
            {
                writeFPGA(35, 0);
                writeFPGA(35, 1);
            }
            else
            {
                ASCOM.QHYCCD.libqhyccd.ResetQHYCCDFrameCounter(camhandle);
            }
        }

        private void button140_Click(object sender, EventArgs e)
        {
            //LowLevelA6(0x00);if ((trig_mode & 0x04) == 4)//mode B 
            //clearDDR();
            if ((trig_mode & 0x04) == 4)//mode B {
            {
                byte trigout_mode;
                trigout_mode = (byte)(trig_mode & 0XF7);//mode b software trig in 
                writeFPGA(58, trigout_mode);//mode b 
                trig_mode = trigout_mode;
            }
            setIDLE();
            Thread.Sleep(100);
            releaseIDLE();
            richTextBox1.AppendText("  trigout_mode = 0x " + trig_mode.ToString("x2") + Environment.NewLine);
        
        }

        private void button119_Click_1(object sender, EventArgs e)
        {
            writeFPGA(4, 1);
            labelBDGPS.Text = "Current is BD mode";
        }

        private void button123_Click(object sender, EventArgs e)
        {
            writeFPGA(125, 1);
        }

        private void button120_Click_1(object sender, EventArgs e)
        {
            writeFPGA(125, 0);
        }

        private void button111_Click(object sender, EventArgs e)
        {
            gettrigftame();
        }

        private void tabPageLowLevelRead_Click(object sender, EventArgs e)
        {

        }

        private void button44_Click_3(object sender, EventArgs e)
        {
            byte[] xdata = new byte[64];
            ushort value = 0x000E;
            ushort index = 0X000E;
            UInt32 PixelPeriod_ps;
            UInt32 LinePeriod_ns;
            UInt32 FramePeriod_us;
            UInt32 expTime;
            UInt32 ActualExposureTime;
            UInt32 switchpoint;
            UInt32 isSingleFrameMode;
            UInt32 isRollingShutter;
            UInt32 is16bit;
            UInt32 enable_ddr;
            UInt32 isTrigMode;
            UInt32 hmax_ref;
            UInt32 usb_traffic;
            UInt32 hmax;
            UInt32 vmax_ref_roi;
            UInt32 vmax_ref;
            UInt32 vmax;
            UInt32 shr;
            UInt32 isFDBIN22;



            ASCOM.QHYCCD.libqhyccd.C_QHYCCDVendRequestRead(camhandle, 0xd5, value, index, 64, xdata);

            richTextBox1.Clear();

            PixelPeriod_ps = (UInt32)(xdata[0] * 256 * 256 * 256 + xdata[1] * 256 * 256 + xdata[2] * 256 + xdata[3]);
            LinePeriod_ns = (UInt32)(xdata[4] * 256 * 256 * 256 + xdata[5] * 256 * 256 + xdata[6] * 256 + xdata[7]);
            FramePeriod_us = (UInt32)(xdata[8] * 256 * 256 * 256 + xdata[9] * 256 * 256 + xdata[10] * 256 + xdata[11]);
            expTime = (UInt32)(xdata[45] * 256 * 256 * 256 + xdata[46] * 256 * 256 + xdata[47] * 256 + xdata[48]);
            ActualExposureTime = (UInt32)(xdata[20] * 256 * 256 * 256 + xdata[21] * 256 * 256 + xdata[22] * 256 + xdata[23]);
            switchpoint = (UInt32)(xdata[24] * 256 * 256 * 256 + xdata[25] * 256 * 256 + xdata[26] * 256 + xdata[27]);
            isSingleFrameMode = xdata[49];
            isRollingShutter = xdata[50];
            is16bit = xdata[43];
            enable_ddr = xdata[44];
            isTrigMode = xdata[54];
            hmax_ref = (UInt32)(xdata[39] * 256 + xdata[40]);
            usb_traffic = (UInt32)(xdata[35] * 256 + xdata[36]);
            hmax = (UInt32)(xdata[12] * 256 * 256 * 256 + xdata[13] * 256 * 256 + xdata[14] * 256 + xdata[15]);
            vmax_ref_roi = (UInt32)(xdata[37] * 256 + xdata[38]);
            vmax_ref = (UInt32)(xdata[41] * 256 + xdata[42]);
            vmax = (UInt32)(xdata[16] * 256 * 256 * 256 + xdata[17] * 256 * 256 + xdata[18] * 256 + xdata[19]);
            shr = (UInt32)(xdata[28] * 256 + xdata[29]);
            isFDBIN22 = xdata[55];


            richTextBox1.AppendText("LOW LEVEL D 5 PixelPeriod_ps :   " + PixelPeriod_ps.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 LinePeriod_ns :   " + LinePeriod_ns.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 FramePeriod_us :   " + FramePeriod_us.ToString() + Environment.NewLine);
            //
            richTextBox1.AppendText("LOW LEVEL D 5 expTime :   " + expTime.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 ActualExposureTime :   " + ActualExposureTime.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 switchpoint :   " + switchpoint.ToString() + Environment.NewLine);
            //
            richTextBox1.AppendText("LOW LEVEL D 5 isSingleFrameMode :   " + isSingleFrameMode.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 isRollingShutter :   " + isRollingShutter.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 is16bit :   " + is16bit.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 enable_ddr :   " + enable_ddr.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 isTrigMode  :   " + isTrigMode.ToString() + Environment.NewLine);
            //
            richTextBox1.AppendText("LOW LEVEL D 5 hmax_ref :   " + hmax_ref.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 usb_traffic :   " + usb_traffic.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 hmax :   " + hmax.ToString() + Environment.NewLine);
            //
            richTextBox1.AppendText("LOW LEVEL D 5 vmax_ref_roi :   " + vmax_ref_roi.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 vmax_ref :   " + vmax_ref.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 vmax :   " + vmax.ToString() + Environment.NewLine);
            //
            richTextBox1.AppendText("LOW LEVEL D 5 shr  :   " + shr.ToString() + Environment.NewLine);
            richTextBox1.AppendText("LOW LEVEL D 5 isFDBIN22  :   " + isFDBIN22.ToString() + Environment.NewLine);

        }

        private void comboBox_Port_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button45_Click_4(object sender, EventArgs e)
        {
            LowLevelReadD2();
        }

        private void textBox34_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox77_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox76_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox85_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox84_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox83_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox82_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox_trig_out_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBoxPatchNumber_TextChanged(object sender, EventArgs e)
        {

        }

    







        

       

   
        }
    }

    namespace StructModel
    {
        public enum CONTROL_ID
        {
            /*0*/
            CONTROL_BRIGHTNESS = 0, //!< image brightness
            /*1*/
            CONTROL_CONTRAST,       //!< image contrast
            /*2*/
            CONTROL_WBR,            //!< red of white balance
            /*3*/
            CONTROL_WBB,            //!< blue of white balance
            /*4*/
            CONTROL_WBG,            //!< the green of white balance
            /*5*/
            CONTROL_GAMMA,          //!< screen gamma
            /*6*/
            CONTROL_GAIN,           //!< camera gain
            /*7*/
            CONTROL_OFFSET,         //!< camera offset
            /*8*/
            CONTROL_EXPOSURE,       //!< expose time (us)
            /*9*/
            CONTROL_SPEED,          //!< transfer speed
            /*10*/
            CONTROL_TRANSFERBIT,    //!< image depth bits
            /*11*/
            CONTROL_CHANNELS,       //!< image channels
            /*12*/
            CONTROL_USBTRAFFIC,     //!< hblank
            /*13*/
            CONTROL_ROWNOISERE,     //!< row denoise
            /*14*/
            CONTROL_CURTEMP,        //!< current cmos or ccd temprature
            /*15*/
            CONTROL_CURPWM,         //!< current cool pwm
            /*16*/
            CONTROL_MANULPWM,       //!< set the cool pwm
            /*17*/
            CONTROL_CFWPORT,        //!< control camera color filter wheel port
            /*18*/
            CONTROL_COOLER,         //!< check if camera has cooler
            /*19*/
            CONTROL_ST4PORT,        //!< check if camera has st4port
            /*20*/
            CAM_COLOR,
            /*21*/
            CAM_BIN1X1MODE,         //!< check if camera has bin1x1 mode
            /*22*/
            CAM_BIN2X2MODE,         //!< check if camera has bin2x2 mode
            /*23*/
            CAM_BIN3X3MODE,         //!< check if camera has bin3x3 mode
            /*24*/
            CAM_BIN4X4MODE,         //!< check if camera has bin4x4 mode
            /*25*/
            CAM_MECHANICALSHUTTER,                   //!< mechanical shutter
            /*26*/
            CAM_TRIGER_INTERFACE,                    //!< triger
            /*27*/
            CAM_TECOVERPROTECT_INTERFACE,            //!< tec overprotect
            /*28*/
            CAM_SINGNALCLAMP_INTERFACE,              //!< singnal clamp
            /*29*/
            CAM_FINETONE_INTERFACE,                  //!< fine tone
            /*30*/
            CAM_SHUTTERMOTORHEATING_INTERFACE,       //!< shutter motor heating
            /*31*/
            CAM_CALIBRATEFPN_INTERFACE,              //!< calibrated frame
            /*32*/
            CAM_CHIPTEMPERATURESENSOR_INTERFACE,     //!< chip temperaure sensor
            /*33*/
            CAM_USBREADOUTSLOWEST_INTERFACE,         //!< usb readout slowest

            /*34*/
            CAM_8BITS,                               //!< 8bit depth
            /*35*/
            CAM_16BITS,                              //!< 16bit depth
            /*36*/
            CAM_GPS,                                 //!< check if camera has gps

            /*37*/
            CAM_IGNOREOVERSCAN_INTERFACE,            //!< ignore overscan area

            /*38*/
            QHYCCD_3A_AUTOBALANCE,
            /*39*/
            QHYCCD_3A_AUTOEXPOSURE,
            /*40*/
            QHYCCD_3A_AUTOFOCUS,
            /*41*/
            CONTROL_AMPV,                            //!< ccd or cmos ampv
            /*42*/
            CONTROL_VCAM,                            //!< Virtual Camera on off
            /*43*/
            CAM_VIEW_MODE,

            /*44*/
            CONTROL_CFWSLOTSNUM,         //!< check CFW slots number
            /*45*/
            IS_EXPOSING_DONE,
            /*46*/
            ScreenStretchB,
            /*47*/
            ScreenStretchW,
            /*48*/
            CONTROL_DDR,
            /*49*/
            CAM_LIGHT_PERFORMANCE_MODE,

            /*50*/
            CAM_QHY5II_GUIDE_MODE,
            /*51*/
            DDR_BUFFER_CAPACITY,
            /*52*/
            DDR_BUFFER_READ_THRESHOLD,
            /*53*/
            DefaultGain,
            /*54*/
            DefaultOffset,
            /*55*/
            OutputDataActualBits,
            /*56*/
            OutputDataAlignment,

            /*57*/
            CAM_SINGLEFRAMEMODE,
            /*58*/
            CAM_LIVEVIDEOMODE,
            /*59*/
            CAM_IS_COLOR,
            /*60*/
            hasHardwareFrameCounter,
            /*61*/
            CONTROL_MAX_ID,
            /*62*/
            CAM_HUMIDITY,			//!<check if camera has	 humidity sensor  20191021 LYL Unified humidity function
            /*63*/
            CAM_PRESSURE             //check if camera has pressure sensor 


        };

        public enum BAYER_ID
        {
            BAYER_GB = 1,
            BAYER_GR,
            BAYER_BG,
            BAYER_RG
        };
    }





    namespace ASCOM.QHYCCD
    {
        class libqhyccd
        {
            [DllImport("qhyccd.dll", EntryPoint = "InitQHYCCDResource",
                CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 InitQHYCCDResource();

            [DllImport("qhyccd.dll", EntryPoint = "ReleaseQHYCCDResource",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ReleaseQHYCCDResource();

            [DllImport("qhyccd.dll", EntryPoint = "ScanQHYCCD",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ScanQHYCCD();

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDId",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDId(int index, byte[] id);

            [DllImport("qhyccd.dll", EntryPoint = "OpenQHYCCD",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern IntPtr OpenQHYCCD(byte[] id);

            [DllImport("qhyccd.dll", EntryPoint = "InitQHYCCD",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 InitQHYCCD(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "CloseQHYCCD",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 CloseQHYCCD(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDBinMode",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDBinMode(IntPtr handle, UInt32 wbin, UInt32 hbin);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDParam",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDParam(IntPtr handle, CONTROL_ID controlid, double value);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDMemLength",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDMemLength(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "ExpQHYCCDSingleFrame",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ExpQHYCCDSingleFrame(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "CancelQHYCCDExposing",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 CancelQHYCCDExposing(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "CancelQHYCCDExposingAndReadout",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 CancelQHYCCDExposingAndReadout(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDSingleFrame",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDSingleFrame(IntPtr handle, ref UInt32 w, ref UInt32 h, ref UInt32 bpp, ref UInt32 channels, byte* rawArray);
            public unsafe static UInt32 C_GetQHYCCDSingleFrame(IntPtr handle, ref UInt32 w, ref UInt32 h, ref UInt32 bpp, ref UInt32 channels, byte[] rawArray)
            {
                UInt32 ret;
                fixed (byte* prawArray = rawArray)
                    ret = GetQHYCCDSingleFrame(handle, ref w, ref h, ref bpp, ref channels, prawArray);
                return ret;
            }
            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDChipInfo",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDChipInfo(IntPtr handle, ref double chipw, ref double chiph, ref UInt32 imagew, ref UInt32 imageh, ref double pixelw, ref double pixelh, ref UInt32 bpp);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDOverScanArea",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDOverScanArea(IntPtr handle, ref UInt32 startx, ref UInt32 starty, ref UInt32 sizex, ref UInt32 sizey);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDEffectiveArea",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDEffectiveArea(IntPtr handle, ref UInt32 startx, ref UInt32 starty, ref UInt32 sizex, ref UInt32 sizey);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDFWVersion",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDFWVersion(IntPtr handle, byte* verBuf);


            public unsafe static UInt32 C_GetQHYCCDFWVersion(IntPtr handle, byte[] verBuf)
            {
                fixed (byte* pverBuf = verBuf)
                    return GetQHYCCDFWVersion(handle, pverBuf);
            }

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDFPGAVersion",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDFPGAVersion(IntPtr handle, byte fpga_index, byte[] verBuf);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDSDKVersion",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDSDKVersion(ref UInt32 year, ref UInt32 month, ref UInt32 day, ref UInt32 subday);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDParam",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern double GetQHYCCDParam(IntPtr handle, CONTROL_ID controlid);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDParamMinMaxStep",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDParamMinMaxStep(IntPtr handle, CONTROL_ID controlid, ref double min, ref double max, ref double step);

            [DllImport("qhyccd.dll", EntryPoint = "ControlQHYCCDGuide",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ControlQHYCCDGuide(IntPtr handle, byte Direction, UInt16 PulseTime);

            [DllImport("qhyccd.dll", EntryPoint = "ControlQHYCCDTemp",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ControlQHYCCDTemp(IntPtr handle, double targettemp);

            [DllImport("qhyccd.dll", EntryPoint = "IsQHYCCDCFWPlugged",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 IsQHYCCDCFWPlugged(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "SendOrder2QHYCCDCFW",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SendOrder2QHYCCDCFW(IntPtr handle, String order, int length);

            [DllImport("qhyccd.dll", EntryPoint = "IsQHYCCDControlAvailable",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 IsQHYCCDControlAvailable(IntPtr handle, CONTROL_ID controlid);

            [DllImport("qhyccd.dll", EntryPoint = "ControlQHYCCDShutter",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ControlQHYCCDShutter(IntPtr handle, byte targettemp);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDResolution",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDResolution(IntPtr handle, UInt32 startx, UInt32 starty, UInt32 sizex, UInt32 sizey);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDStreamMode",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDStreamMode(IntPtr handle, UInt32 mode);

            //EXPORTFUNC uint32_t STDCALL GetQHYCCDCFWStatus(qhyccd_handle *handle,char *status)
            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDCFWStatus",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDCFWStatus(IntPtr handle, byte[] cfwStatus);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDBitsMode",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDBitsMode(IntPtr handle, UInt32 bits);

            [DllImport("qhyccd.dll", EntryPoint = "BeginQHYCCDLive",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 BeginQHYCCDLive(IntPtr handle);


            [DllImport("qhyccd.dll", EntryPoint = "QHYCCDVendRequestWrite",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 QHYCCDVendRequestWrite(IntPtr handle, byte req, UInt16 value, UInt16 index1, UInt32 length, byte* data);
            public unsafe static UInt32 C_QHYCCDVendRequestWrite(IntPtr handle, byte req, UInt16 value, UInt16 index1, UInt32 length, byte[] data)
            {
                UInt32 ret;
                fixed (byte* prawArray = data)
                    ret = QHYCCDVendRequestWrite(handle, req, value, index1, length, prawArray);
                return ret;

            }

            [DllImport("qhyccd.dll", EntryPoint = "QHYCCDVendRequestRead",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 QHYCCDVendRequestRead(IntPtr handle, byte req, UInt16 value, UInt16 index1, UInt32 length, byte* data);
            public unsafe static UInt32 C_QHYCCDVendRequestRead(IntPtr handle, byte req, UInt16 value, UInt16 index1, UInt32 length, byte[] data)
            {
                UInt32 ret;
                fixed (byte* prawArray = data)
                    ret = QHYCCDVendRequestRead(handle, req, value, index1, length, prawArray);
                return ret;

            }

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDLiveFrame",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDLiveFrame(IntPtr handle, ref UInt32 w, ref UInt32 h, ref UInt32 bpp, ref UInt32 channels, byte* imgdata);
            public unsafe static UInt32 C_GetQHYCCDLiveFrame(IntPtr handle, ref UInt32 w, ref UInt32 h, ref  UInt32 bpp, ref UInt32 channels, byte[] imgdata)
            {
                UInt32 ret;
                fixed (byte* prawArray = imgdata)
                    ret = GetQHYCCDLiveFrame(handle, ref w, ref h, ref bpp, ref channels, prawArray);
                return ret;
            }

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDDebayerOnOff",
             CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDDebayerOnOff(IntPtr handle, bool onoff);

            [DllImport("qhyccd.dll", EntryPoint = "EnableQHYCCDBurstMode",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 EnableQHYCCDBurstMode(IntPtr handle, bool i);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDBurstModeStartEnd",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDBurstModeStartEnd(IntPtr handle, ushort start, ushort end);

            [DllImport("qhyccd.dll", EntryPoint = "EnableQHYCCDBurstCountFun",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 EnableQHYCCDBurstCountFun(IntPtr handle, bool i);

            [DllImport("qhyccd.dll", EntryPoint = "ResetQHYCCDFrameCounter",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ResetQHYCCDFrameCounter(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDBurstIDLE",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDBurstIDLE(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "ReleaseQHYCCDBurstIDLE",
           CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 ReleaseQHYCCDBurstIDLE(IntPtr handle);

            [DllImport("qhyccd.dll", EntryPoint = "EnableQHYCCDImageOSD",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 EnableQHYCCDImageOSD(IntPtr handle, UInt32 i);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDBurstModePatchNumber",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDBurstModePatchNumber(IntPtr handle, UInt32 value);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDEnableLiveModeAntiRBI",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDEnableLiveModeAntiRBI(IntPtr handle, UInt32 value);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDWriteFPGA",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDWriteFPGA(IntPtr handle, byte number, byte regindex, byte regvalue);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDWriteCMOS",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDWriteCMOS(IntPtr handle, byte number, UInt16 regindex, UInt16 regvalue);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDAdvancedCommand",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDAdvancedCommand(IntPtr handle, UInt32 command_length, byte[] command, ref UInt32 result_length, byte[] result);
            // public unsafe static      UInt32 C_SetQHYCCDAdvancedCommand(IntPtr handle, UInt32 command_length, byte[] command, UInt32 results_length, byte[] result)

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDHumidity",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDHumidity(IntPtr handle, ref Double value);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDPressure",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDPressure(IntPtr handle, ref Double value);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDTwoChannelCombineParameter",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDTwoChannelCombineParameter(IntPtr handle, double x, double ah, double bh, double al, double bl);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDTrigerFunction",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDTrigerFunction(IntPtr handle, bool value);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDTrigerInput",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDTrigerInput(IntPtr handle, UInt32 phase, UInt32 source, UInt32 filter, UInt32 exposure_control, UInt32 loop);

            [DllImport("qhyccd.dll", EntryPoint = "SetQHYCCDTrigerMode",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 SetQHYCCDTrigerMode(IntPtr handle, UInt32 trigerMode);

            [DllImport("qhyccd.dll", EntryPoint = "GetQHYCCDPreciseExposureInfo",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 GetQHYCCDPreciseExposureInfo(IntPtr handle, ref UInt32 PixelPeriod_ps, ref UInt32 LinePeriod_ns, ref UInt32 FramePeriod_us, ref UInt32 ClocksPerLine, ref UInt32 LinesPerFrame, ref UInt32 ActualExposureTime, ref byte isLongExposureMode);

            [DllImport("qhyccd.dll", EntryPoint = "QHYCCDReadUSB_SYNC",
                         CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public unsafe static extern UInt32 QHYCCDReadUSB_SYNC(IntPtr handle, byte endpoint, UInt32 length, byte* testdata, UInt32 timeout);
            public unsafe static UInt32 C_QHYCCDReadUSB_SYNC(IntPtr handle, byte endpoint, UInt32 length, byte[] testdata, UInt32 timeout)
            {
                UInt32 ret;
                fixed (byte* data = testdata)
                    ret = QHYCCDReadUSB_SYNC(handle, endpoint, length, data, timeout);
                return ret;
            }
        }  
}
