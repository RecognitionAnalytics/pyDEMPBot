using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PumpAdapter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var ports = tbPorts.Text.Split(new string[] { ",", "\"", " " }, StringSplitOptions.RemoveEmptyEntries);
            Pump p = new Pump("COM7", SyringeType.Ceramic_Syringe, SyringeVolumes._1000μL);
            var pports = new string[,] { { "water", "water2" }, { "air", "air2" }, { "waste", "waste2" }, {  "dispense"   ,"dispense2" } };
            // var pports = tbPorts.Text.Split(new string[] { ",," }, StringSplitOptions.RemoveEmptyEntries);
            p.GetInfo(1,"?23");
            Debug.Print(p.errors(1));
            Debug.Print(p.errors(2));

            p.AddAnalytes(pports );
            p.CurrentPosition("water");

            p.Initalize("Water","Waste");
            Debug.Print(p.errors(1));
            p.Initalize("water2","waste2");
            Debug.Print(p.errors(2));

            p.ZeroPosition("waste",300);
            Debug.Print(p.errors(1));
            p.ZeroPosition("waste2",300);
            Debug.Print(p.errors(2));


            p.Pull("Water", 500, 100);
            Debug.Print(p.errors(1));
            var start = DateTime.Now;
            p.DeadVolume_uL = 3000;
            //  p.DispenseToCellLimited2("TimEtch", 300, 100,1000,100);
            var volume = double.Parse(tbAmount.Text);
            var maxvolume = double.Parse(tbMaxVolume.Text);

            p.DispenseLimited(tbFrom.Text, volume, double.Parse(tbSpeed.Text), maxvolume);
            var end = DateTime.Now;
            Debug.Print("" + end.Subtract(start).TotalSeconds);

            p.Close();
        }
        double volDsipense = 0;
        private void button2_Click(object sender, EventArgs e)
        {

            Pump p = new Pump("COM3", SyringeType.Ceramic_Syringe, SyringeVolumes._1000μL);


            var pports = tbPorts.Text.Split(new string[] { ",," }, StringSplitOptions.RemoveEmptyEntries);
            int cc = 0;
            foreach (var pp in pports)
            {
                var ports = pp.Split(new string[] { ",", "\"", " " }, StringSplitOptions.RemoveEmptyEntries);

                p.AddAnalytes(ports, cc + 1);
                cc++;
            }
            p.Initalize("Air","Air");

            var start = DateTime.Now;

            var volume = double.Parse(tbAmount.Text);
            var maxvolume = double.Parse(tbMaxVolume.Text);
            volDsipense += volume;

            p.PullPush(tbTo.Text, tbFrom.Text, volume, double.Parse(tbSpeed.Text), maxvolume);


            p.Close();
            Text = volDsipense + "";
        }

        private void tbPorts_TextChanged(object sender, EventArgs e)
        {

        }

        private void tbMaxVolume_TextChanged(object sender, EventArgs e)
        {

        }
    }
}