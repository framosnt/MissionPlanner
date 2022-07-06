namespace MissionPlanner.Controls
{
    //FERNANDO 04-07-2022
    partial class BonsVoosGPS
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
       private void InitializeComponent2()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BonsVoosGPS));
            this.CMB_serialportGPS = new System.Windows.Forms.ComboBox();
            this.CMB_baudrateGPS = new System.Windows.Forms.ComboBox();
            this.labelGPS = new System.Windows.Forms.Label();
            this.LBL_locationGPS = new System.Windows.Forms.Label();
            this.textBox1GPS = new System.Windows.Forms.TextBox();
            this.CMB_updaterateGPS = new System.Windows.Forms.ComboBox();
            this.label2GPS = new System.Windows.Forms.Label();
            this.BUT_connectGPS = new MissionPlanner.Controls.MyButton();
            this.SuspendLayout();
            // 
            // CMB_serialport
            // 
            this.CMB_serialportGPS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CMB_serialportGPS.FormattingEnabled = true;
            this.CMB_serialportGPS.Location = new System.Drawing.Point(13, 23);
            this.CMB_serialportGPS.Name = "CMB_serialport";
            this.CMB_serialportGPS.Size = new System.Drawing.Size(121, 21);
            this.CMB_serialportGPS.TabIndex = 0;
            // 
            // CMB_baudrate
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
            this.CMB_baudrateGPS.Location = new System.Drawing.Point(140, 22);
            this.CMB_baudrateGPS.Name = "CMB_baudrateGPS";
            this.CMB_baudrateGPS.Size = new System.Drawing.Size(97, 21);
            this.CMB_baudrateGPS.TabIndex = 2;
            // 
            // label1
            // 
            this.labelGPS.AutoSize = true;
            this.labelGPS.Location = new System.Drawing.Point(12, 6);
            this.labelGPS.Name = "label1";
            this.labelGPS.Size = new System.Drawing.Size(187, 13);
            this.labelGPS.TabIndex = 3;
            this.labelGPS.Text = "GPS Port                      Velocidade\r\n";
            // 
            // LBL_location
            // 
            this.LBL_locationGPS.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LBL_locationGPS.Location = new System.Drawing.Point(3, 64);
            this.LBL_locationGPS.Name = "LBL_location";
            this.LBL_locationGPS.Size = new System.Drawing.Size(425, 59);
            this.LBL_locationGPS.TabIndex = 4;
            this.LBL_locationGPS.Text = "0,0,0";
            // 
            // textBox1
            // 
            this.textBox1GPS.Enabled = false;
            this.textBox1GPS.Location = new System.Drawing.Point(19, 126);
            this.textBox1GPS.Multiline = true;
            this.textBox1GPS.Name = "textBox1";
            this.textBox1GPS.Size = new System.Drawing.Size(409, 162);
            this.textBox1GPS.TabIndex = 5;
            this.textBox1GPS.Text = resources.GetString("textBox1GPS.Text");
            // 
            // CMB_updaterate
            // 
            this.CMB_updaterateGPS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CMB_updaterateGPS.FormattingEnabled = true;
            this.CMB_updaterateGPS.Items.AddRange(new object[] {
            "2hz",
            "1hz",
            "0.5hz",
            "0.25hz"});
            this.CMB_updaterateGPS.Location = new System.Drawing.Point(279, 23);
            this.CMB_updaterateGPS.Name = "CMB_updaterateGPS";
            this.CMB_updaterateGPS.Size = new System.Drawing.Size(75, 21);
            this.CMB_updaterateGPS.TabIndex = 6;
            this.CMB_updaterateGPS.SelectedIndexChanged += new System.EventHandler(this.CMB_updaterate_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2GPS.AutoSize = true;
            this.label2GPS.Location = new System.Drawing.Point(252, 5);
            this.label2GPS.Name = "label2GPS";
            this.label2GPS.Size = new System.Drawing.Size(117, 13);
            this.label2GPS.TabIndex = 7;
            this.label2GPS.Text = "Taxa de Atualização\r\n";
            // 
            // BUT_connect
            // 
            this.BUT_connectGPS.Location = new System.Drawing.Point(360, 21);
            this.BUT_connectGPS.Name = "BUT_connectGPS";
            this.BUT_connectGPS.Size = new System.Drawing.Size(75, 23);
            this.BUT_connectGPS.TabIndex = 1;
            this.BUT_connectGPS.Text = global::MissionPlanner.Strings.Connect;
            this.BUT_connectGPS.UseVisualStyleBackColor = true;
            this.BUT_connectGPS.Click += new System.EventHandler(this.BUT_connect_Click_1);
            // 
            // BonsVoosGPS
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            
            this.ClientSize = new System.Drawing.Size(440, 300);
            this.Controls.Add(this.label2GPS);
            this.Controls.Add(this.CMB_updaterateGPS);
            this.Controls.Add(this.textBox1GPS);
            this.Controls.Add(this.LBL_locationGPS);
            this.Controls.Add(this.labelGPS);
            this.Controls.Add(this.CMB_baudrateGPS);
            this.Controls.Add(this.BUT_connectGPS);
            this.Controls.Add(this.CMB_serialportGPS);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "BonsVoosGPS";
            this.Text = "BONS VOOS - GPS LOCAL";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SerialOutput_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox CMB_serialportGPS;
        private Controls.MyButton BUT_connectGPS;
        private System.Windows.Forms.ComboBox CMB_baudrateGPS;
        private System.Windows.Forms.Label labelGPS;
        private System.Windows.Forms.Label LBL_locationGPS;
        private System.Windows.Forms.TextBox textBox1GPS;
        private System.Windows.Forms.ComboBox CMB_updaterateGPS;
        private System.Windows.Forms.Label label2GPS;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label4GPS;
        private System.Windows.Forms.Label label5GPS;
        private System.Windows.Forms.Label label6GPS;
    }
}