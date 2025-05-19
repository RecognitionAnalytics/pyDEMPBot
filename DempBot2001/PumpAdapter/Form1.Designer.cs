namespace PumpAdapter
{
    partial class Form1
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
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.tbAmount = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.tbPorts = new System.Windows.Forms.TextBox();
            this.tbTo = new System.Windows.Forms.TextBox();
            this.tbFrom = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tbMaxVolume = new System.Windows.Forms.TextBox();
            this.tbSpeed = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(543, 319);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(178, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Dispense";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tbAmount
            // 
            this.tbAmount.Location = new System.Drawing.Point(544, 164);
            this.tbAmount.Name = "tbAmount";
            this.tbAmount.Size = new System.Drawing.Size(177, 20);
            this.tbAmount.TabIndex = 1;
            this.tbAmount.Text = "300";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(543, 401);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(178, 25);
            this.button2.TabIndex = 2;
            this.button2.Text = "Pull/Push";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // tbPorts
            // 
            this.tbPorts.Location = new System.Drawing.Point(21, 61);
            this.tbPorts.Name = "tbPorts";
            this.tbPorts.Size = new System.Drawing.Size(433, 20);
            this.tbPorts.TabIndex = 3;
            this.tbPorts.Text = "Water,Waste,Air,\"Dispense\",\"MPA\",\"NHSEDC\",\"BIOTIN\",\"SA\" ";
            this.tbPorts.TextChanged += new System.EventHandler(this.tbPorts_TextChanged);
            // 
            // tbTo
            // 
            this.tbTo.Location = new System.Drawing.Point(543, 117);
            this.tbTo.Name = "tbTo";
            this.tbTo.Size = new System.Drawing.Size(164, 20);
            this.tbTo.TabIndex = 4;
            this.tbTo.Text = "dispense";
            // 
            // tbFrom
            // 
            this.tbFrom.Location = new System.Drawing.Point(544, 70);
            this.tbFrom.Name = "tbFrom";
            this.tbFrom.Size = new System.Drawing.Size(167, 20);
            this.tbFrom.TabIndex = 5;
            this.tbFrom.Text = "air";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Ports";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(541, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(30, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "From";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(541, 101);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(20, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "To";
            // 
            // tbMaxVolume
            // 
            this.tbMaxVolume.Location = new System.Drawing.Point(544, 252);
            this.tbMaxVolume.Name = "tbMaxVolume";
            this.tbMaxVolume.Size = new System.Drawing.Size(177, 20);
            this.tbMaxVolume.TabIndex = 9;
            this.tbMaxVolume.Text = "200";
            this.tbMaxVolume.TextChanged += new System.EventHandler(this.tbMaxVolume_TextChanged);
            // 
            // tbSpeed
            // 
            this.tbSpeed.Location = new System.Drawing.Point(544, 204);
            this.tbSpeed.Name = "tbSpeed";
            this.tbSpeed.Size = new System.Drawing.Size(177, 20);
            this.tbSpeed.TabIndex = 10;
            this.tbSpeed.Text = "100";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tbSpeed);
            this.Controls.Add(this.tbMaxVolume);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbFrom);
            this.Controls.Add(this.tbTo);
            this.Controls.Add(this.tbPorts);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.tbAmount);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox tbAmount;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox tbPorts;
        private System.Windows.Forms.TextBox tbTo;
        private System.Windows.Forms.TextBox tbFrom;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbMaxVolume;
        private System.Windows.Forms.TextBox tbSpeed;
    }
}

