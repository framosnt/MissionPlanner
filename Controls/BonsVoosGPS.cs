using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using MissionPlanner.ArduPilot;
using MissionPlanner.Comms;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    public partial class BonsVoosGPS : Form
    {
        System.Threading.Thread t12;
        static bool threadrun = false;
        static BonsVoosGPS Instance;
        static internal ICommsSerial comPort = new SerialPort();
        static internal PointLatLngAlt lastgotolocation = new PointLatLngAlt(0, 0, 0, "Goto last");
        static internal PointLatLngAlt gotolocation = new PointLatLngAlt(0, 0, 0, "Goto");
        static internal int intalt = 100;
        static float updaterate = 0.5f;


        //private Label label2GPS;
        //private ComboBox CMB_updaterateGPS;
        //private TextBox textBox1GPS;
        //private Label LBL_locationGPS;
        //private Label labelGPS;
        //private ComboBox CMB_baudrateGPS;
        //private MyButton BUT_connectGPS;
        //private ComboBox CMB_serialportGPS;
        //private PictureBox pictureBox1;
        //private Label label3GPS;
        //private Label label4gps;
        //private Label label5GPS;


        public BonsVoosGPS()
        {
            Instance = this;

           InitializeComponent();

            CMB_serialportGPS.DataSource = SerialPort.GetPortNames();

            CMB_updaterateGPS.SelectedItem = updaterate;

            if (threadrun)
            {
                BUT_connectGPS.Text = Strings.Stop;
                CMB_baudrateGPS.Text = comPort.BaudRate.ToString();
                CMB_serialportGPS.Text = comPort.PortName;
                CMB_updaterateGPS.Text = updaterate.ToString();
            }

            MissionPlanner.Utilities.Tracking.AddPage(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString(),
                System.Reflection.MethodBase.GetCurrentMethod().Name);
        }

    

        void mainloop()
        {
            DateTime nextsend = DateTime.Now;

            StreamWriter sw = new StreamWriter(File.OpenWrite(Settings.GetUserDataDirectory() + "BonsVoosGPSraw.txt"));

            threadrun = true;
            while (threadrun)
            {
                try
                {
                    string line = comPort.ReadLine();

                    sw.WriteLine(line);

                    //string line = string.Format("$GP{0},{1:HHmmss},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},", "GGA", DateTime.Now.ToUniversalTime(), Math.Abs(lat * 100), MainV2.comPort.MAV.cs.lat < 0 ? "S" : "N", Math.Abs(lng * 100), MainV2.comPort.MAV.cs.lng < 0 ? "W" : "E", MainV2.comPort.MAV.cs.gpsstatus, MainV2.comPort.MAV.cs.satcount, MainV2.comPort.MAV.cs.gpshdop, MainV2.comPort.MAV.cs.alt, "M", 0, "M", "");
                    if (line.StartsWith("$GPGGA") || line.StartsWith("$GNGGA")) // 
                    {
                        string[] items = line.Trim().Split(',', '*');

                        if (items[items.Length - 1] != GetChecksum(line.Trim()))
                        {
                            Console.WriteLine("Bad Nmea line " + items[15] + " vs " + GetChecksum(line.Trim()));
                            continue;
                        }

                        if (items[6] == "0")
                        {
                            Console.WriteLine("No Fix");
                            continue;
                        }

                        gotolocation.Lat = double.Parse(items[2], CultureInfo.InvariantCulture) / 100.0;

                        gotolocation.Lat = (int)gotolocation.Lat + ((gotolocation.Lat - (int)gotolocation.Lat) / 0.60);

                        if (items[3] == "S")
                            gotolocation.Lat *= -1;

                        gotolocation.Lng = double.Parse(items[4], CultureInfo.InvariantCulture) / 100.0;

                        gotolocation.Lng = (int)gotolocation.Lng + ((gotolocation.Lng - (int)gotolocation.Lng) / 0.60);

                        if (items[5] == "W")
                            gotolocation.Lng *= -1;

                        gotolocation.Alt = intalt;

                        gotolocation.Tag = "Sats " + items[7] + " hdop " + items[8];
                    }


                    if (DateTime.Now > nextsend && gotolocation.Lat != 0 && gotolocation.Lng != 0 &&
                        gotolocation.Alt != 0) // 200 * 10 = 2 sec /// lastgotolocation != gotolocation && 
                    {
                        nextsend = DateTime.Now.AddMilliseconds(1000 / updaterate);
                        Console.WriteLine("Sending BonsVoosGPSLocal " + DateTime.Now.ToString("h:MM:ss") + " " +
                                          gotolocation.Lat + " " + gotolocation.Lng + " " + gotolocation.Alt);
                        lastgotolocation = new PointLatLngAlt(gotolocation);

                        Locationwp gotohere = new Locationwp();

                        gotohere.id = (ushort)MAVLink.MAV_CMD.WAYPOINT;
                        gotohere.alt = (float)(gotolocation.Alt);
                        gotohere.lat = (gotolocation.Lat);
                        gotohere.lng = (gotolocation.Lng);

                        try
                        {
                            updateLocationLabel(gotohere);
                        }
                        catch
                        {
                        }

                        if (MainV2.comPort.BaseStream.IsOpen && MainV2.comPort.giveComport == false)
                        {
                            try
                            {
                                //fernando - 26-06-2022 - Aqui faz o drone ir para o local do gps
                                //MainV2.comPort.setGuidedModeWP(gotohere, false);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch
                {
                    System.Threading.Thread.Sleep((int)(1000 / updaterate));
                }
            }

            sw.Close();
        }

        private void updateLocationLabel(Locationwp plla)
        {
            if (!Instance.IsDisposed)
            {
                Instance.BeginInvoke(
                    (MethodInvoker)
                        delegate
                        {
                            Instance.LBL_locationGPS.Text = gotolocation.Lat + " " + gotolocation.Lng + " " +
                                                         gotolocation.Alt + " " + gotolocation.Tag;
                        }
                    );
            }
        }

        private void SerialOutput_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        // Calculates the checksum for a sentence
        string GetChecksum(string sentence)
        {
            // Loop through all chars to get a checksum
            int Checksum = 0;
            foreach (char Character in sentence.ToCharArray())
            {
                switch (Character)
                {
                    case '$':
                        // Ignore the dollar sign
                        break;
                    case '*':
                        // Stop processing before the asterisk
                        return Checksum.ToString("X2");
                    default:
                        // Is this the first value for the checksum?
                        if (Checksum == 0)
                        {
                            // Yes. Set the checksum to the value
                            Checksum = Convert.ToByte(Character);
                        }
                        else
                        {
                            // No. XOR the checksum with this character's value
                            Checksum = Checksum ^ Convert.ToByte(Character);
                        }
                        break;
                }
            }
            // Return the checksum formatted as a two-character hexadecimal
            return Checksum.ToString("X2");
        }

        private void CMB_updaterate_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void InitializeComponent()
        {
            this.label2GPS = new System.Windows.Forms.Label();
            this.CMB_updaterateGPS = new System.Windows.Forms.ComboBox();
            this.textBox1GPS = new System.Windows.Forms.TextBox();
            this.LBL_locationGPS = new System.Windows.Forms.Label();
            this.labelGPS = new System.Windows.Forms.Label();
            this.CMB_baudrateGPS = new System.Windows.Forms.ComboBox();
            this.BUT_connectGPS = new MissionPlanner.Controls.MyButton();
            this.CMB_serialportGPS = new System.Windows.Forms.ComboBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label4GPS = new System.Windows.Forms.Label();
            this.label5GPS = new System.Windows.Forms.Label();
            this.label6GPS = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label2GPS
            // 
            this.label2GPS.AutoSize = true;
            this.label2GPS.Location = new System.Drawing.Point(263, 104);
            this.label2GPS.Name = "label2GPS";
            this.label2GPS.Size = new System.Drawing.Size(130, 16);
            this.label2GPS.TabIndex = 15;
            this.label2GPS.Text = "Taxa de Atualização";
            // 
            // CMB_updaterateGPS
            // 
            this.CMB_updaterateGPS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CMB_updaterateGPS.FormattingEnabled = true;
            this.CMB_updaterateGPS.Items.AddRange(new object[] {
            "2hz",
            "1hz",
            "0.5hz",
            "0.25hz"});
            this.CMB_updaterateGPS.Location = new System.Drawing.Point(266, 121);
            this.CMB_updaterateGPS.Name = "CMB_updaterateGPS";
            this.CMB_updaterateGPS.Size = new System.Drawing.Size(75, 24);
            this.CMB_updaterateGPS.TabIndex = 14;
            // 
            // textBox1GPS
            // 
            this.textBox1GPS.Enabled = false;
            this.textBox1GPS.Location = new System.Drawing.Point(19, 224);
            this.textBox1GPS.Multiline = true;
            this.textBox1GPS.Name = "textBox1GPS";
            this.textBox1GPS.Size = new System.Drawing.Size(409, 162);
            this.textBox1GPS.TabIndex = 13;
            // 
            // LBL_locationGPS
            // 
            this.LBL_locationGPS.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LBL_locationGPS.Location = new System.Drawing.Point(3, 162);
            this.LBL_locationGPS.Name = "LBL_locationGPS";
            this.LBL_locationGPS.Size = new System.Drawing.Size(425, 59);
            this.LBL_locationGPS.TabIndex = 12;
            this.LBL_locationGPS.Text = "0,0,0";
            // 
            // labelGPS
            // 
            this.labelGPS.AutoSize = true;
            this.labelGPS.Location = new System.Drawing.Point(12, 104);
            this.labelGPS.Name = "labelGPS";
            this.labelGPS.Size = new System.Drawing.Size(198, 16);
            this.labelGPS.TabIndex = 11;
            this.labelGPS.Text = "GPS Port                      Velocidade\r\n";
            // 
            // CMB_baudrateGPS
            // 
            this.CMB_baudrateGPS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CMB_baudrateGPS.FormattingEnabled = true;
            this.CMB_baudrateGPS.Items.AddRange(new object[] {
            "4800",
            "9600",
            "14400",
            "19200",
            "28800",
            "38400",
            "57600",
            "115200"});
            this.CMB_baudrateGPS.Location = new System.Drawing.Point(140, 120);
            this.CMB_baudrateGPS.Name = "CMB_baudrateGPS";
            this.CMB_baudrateGPS.Size = new System.Drawing.Size(97, 24);
            this.CMB_baudrateGPS.TabIndex = 10;
            // 
            // BUT_connectGPS
            // 
            this.BUT_connectGPS.Location = new System.Drawing.Point(351, 123);
            this.BUT_connectGPS.Name = "BUT_connectGPS";
            this.BUT_connectGPS.Size = new System.Drawing.Size(75, 40);
            this.BUT_connectGPS.TabIndex = 9;
            this.BUT_connectGPS.Text = "Conectar";
            this.BUT_connectGPS.UseVisualStyleBackColor = true;
            this.BUT_connectGPS.Click += new System.EventHandler(this.BUT_connect_Click_1);
            // 
            // CMB_serialportGPS
            // 
            this.CMB_serialportGPS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CMB_serialportGPS.FormattingEnabled = true;
            this.CMB_serialportGPS.Location = new System.Drawing.Point(13, 121);
            this.CMB_serialportGPS.Name = "CMB_serialportGPS";
            this.CMB_serialportGPS.Size = new System.Drawing.Size(121, 24);
            this.CMB_serialportGPS.TabIndex = 8;
            this.CMB_serialportGPS.SelectedIndexChanged += new System.EventHandler(this.CMB_serialport_SelectedIndexChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::MissionPlanner.Properties.Resources.LOGOBONSVOOS;
            this.pictureBox1.Location = new System.Drawing.Point(15, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(96, 78);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 16;
            this.pictureBox1.TabStop = false;
            // 
            // label4GPS
            // 
            this.label4GPS.AutoSize = true;
            this.label4GPS.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4GPS.Location = new System.Drawing.Point(302, 11);
            this.label4GPS.Name = "label4GPS";
            this.label4GPS.Size = new System.Drawing.Size(126, 25);
            this.label4GPS.TabIndex = 17;
            this.label4GPS.Text = "GPS LOCAL";
            this.label4GPS.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label5GPS
            // 
            this.label5GPS.AutoSize = true;
            this.label5GPS.Location = new System.Drawing.Point(236, 36);
            this.label5GPS.Name = "label5GPS";
            this.label5GPS.Size = new System.Drawing.Size(192, 16);
            this.label5GPS.TabIndex = 18;
            this.label5GPS.Text = "Utiliza o GPS interno do Tablet ";
            this.label5GPS.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label6GPS
            // 
            this.label6GPS.AutoSize = true;
            this.label6GPS.Location = new System.Drawing.Point(271, 57);
            this.label6GPS.Name = "label6GPS";
            this.label6GPS.Size = new System.Drawing.Size(157, 16);
            this.label6GPS.TabIndex = 19;
            this.label6GPS.Text = "ou use GPS Externo USB";
            this.label6GPS.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // BonsVoosGPS
            // 
            this.ClientSize = new System.Drawing.Size(491, 447);
            this.Controls.Add(this.label6GPS);
            this.Controls.Add(this.label5GPS);
            this.Controls.Add(this.label4GPS);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label2GPS);
            this.Controls.Add(this.CMB_updaterateGPS);
            this.Controls.Add(this.textBox1GPS);
            this.Controls.Add(this.LBL_locationGPS);
            this.Controls.Add(this.labelGPS);
            this.Controls.Add(this.CMB_baudrateGPS);
            this.Controls.Add(this.BUT_connectGPS);
            this.Controls.Add(this.CMB_serialportGPS);
            this.Name = "BonsVoosGPS";
            this.Load += new System.EventHandler(this.BonsVoosGPS_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void BUT_connect_Click_1(object sender, EventArgs e)
        {
            if (comPort.IsOpen)
            {
                threadrun = false;
                comPort.Close();
                BUT_connectGPS.Text = Strings.Connect;
            }
            else
            {
                try
                {
                    comPort.PortName = CMB_serialportGPS.Text;
                }
                catch
                {
                    CustomMessageBox.Show(Strings.InvalidPortName, Strings.ERROR);
                    return;
                }
                try
                {
                    comPort.BaudRate = int.Parse(CMB_baudrateGPS.Text);
                }
                catch
                {
                    CustomMessageBox.Show(Strings.InvalidBaudRate, Strings.ERROR);
                    return;
                }
                try
                {
                    comPort.Open();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(Strings.ErrorConnecting + "\n" + ex.ToString(), Strings.ERROR);
                    return;
                }


                string alt = "10";

                if (MainV2.comPort.MAV.cs.firmware == Firmwares.ArduCopter2)
                {
                    alt = (10 * CurrentState.multiplierdist).ToString("0");
                }
                else
                {
                    alt = (100 * CurrentState.multiplierdist).ToString("0");
                }
                //if (DialogResult.Cancel == InputBox.Show("Enter Alt", "Enter Alt (relative to home alt)", ref alt))
                //    return;

                intalt = (int)(100 * CurrentState.multiplierdist);
                if (!int.TryParse(alt, out intalt))
                {
                    CustomMessageBox.Show(Strings.InvalidAlt, Strings.ERROR);
                    return;
                }

                intalt = (int)(intalt / CurrentState.multiplierdist);

                t12 = new System.Threading.Thread(new System.Threading.ThreadStart(mainloop))
                {
                    IsBackground = true,
                    Name = "followme Input"
                };
                t12.Start();

                BUT_connectGPS.Text = Strings.Stop;
            }
        }

        private void CMB_serialport_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void BonsVoosGPS_Load(object sender, EventArgs e)
        {
            //fernando 28-07-2022 - não iniciar com ele ligado.
            CMB_baudrateGPS.Text = "9600";
            CMB_updaterateGPS.Text = "2hz";
            CMB_serialportGPS.Text = "GPS";
            //BUT_connect_Click_1( sender, e);

        }

        //private void InitializeComponent()
        //{
        //    this.SuspendLayout();
        //    // 
        //    // BonsVoosGPS
        //    // 
        //    this.ClientSize = new System.Drawing.Size(583, 343);
        //    this.Name = "BonsVoosGPS";
        //    this.Load += new System.EventHandler(this.BonsVoosGPS_Load_1);
        //    this.ResumeLayout(false);

        //}

  
    }
}