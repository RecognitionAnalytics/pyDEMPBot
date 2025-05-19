using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PumpAdapter
{
    public enum SyringeVolumes : int
    {
        _250uL = 90,
        _1250uL = 91,
        _5000μL = 92,
        _50μL = 93,
        _100μL = 94,
        _500μL = 95,
        _1000μL = 96,
        _2500μL = 97,
        _12500μL = 98
    }

    public enum SyringeType : int
    {
        Ceramic_Syringe = 0,
        Glass_Syringe = 2000,
        Ceramic_Syringe_W_Wash = 2300,
        Large_Glass_Syringe = 4000,
    }
    public class Port
    {
        public string PortName { get; set; }
        public int PortNumber { get; set; }

        public int PortPump { get; set; }
    }
    public class Pump : IDisposable
    {
        private CommPort commPort;
        SyringeVolumes SyringeVolume;
        Dictionary<string, Port> AnalyteMap;

        public string SerialPort { get; private set; }
        SyringeType SyringeType;


        public double DeadVolume_uL { get; set; } = 0;
        public double OverDrawRatio { get; set; } = 1.1;

        public bool isTesting { get; set; } = false;

        public Pump(string serialPort, SyringeType syringeType, SyringeVolumes syringeVolume_microliters, int maxSteps = 181490)
        {
            SerialPort = serialPort;
            commPort = new CommPort();
            commPort.OpenSerialPort(serialPort, 9600);

            this.SyringeVolume = syringeVolume_microliters;
            AnalyteMap = new Dictionary<string, Port>();
        }

        public void ClearAnalytes()
        {
            this.AnalyteMap.Clear();
        }

        public void AddAnalytes(string[] analyteNames, int pumpAddress)
        {

            for (int i = 0; i < analyteNames.Length; i++)
            {
                if (analyteNames[i] != null && analyteNames[i] != "")
                {
                    this.AnalyteMap.Add(analyteNames[i].ToLower(),
                        new Port
                        {
                            PortName = analyteNames[i],
                            PortNumber = i + 1,
                            PortPump = pumpAddress
                        });
                }
            }

        }

        public float MaxSyringeVolume
        {
            get
            {
                switch (this.SyringeVolume)
                {
                    case SyringeVolumes._500μL:
                        return 500;
                    case SyringeVolumes._1250uL:
                        return 1250;
                    case SyringeVolumes._250uL:
                        return 250;
                    case SyringeVolumes._5000μL:
                        return 5000;
                    case SyringeVolumes._50μL:
                        return 50;
                    case SyringeVolumes._100μL:
                        return 100;
                    case SyringeVolumes._1000μL:
                        return 1000;
                    case SyringeVolumes._2500μL:
                        return 2500;
                    default:
                        return 1000;
                }

            }
        }

        public int MaxUsableVolume
        {
            get
            {
                return (int)(MaxSyringeVolume / OverDrawRatio - 5);
            }
        }

        public string AddAnalytes(string[,] analyteNames)
        {

            for (int pumpAddress = 0; pumpAddress < analyteNames.GetLength(0); pumpAddress++)
                for (int i = 0; i < analyteNames.GetLength(1); i++)
                {
                    this.AnalyteMap.Add(analyteNames[pumpAddress, i].ToLower(),
                        new Port
                        {
                            PortName = analyteNames[pumpAddress, i],
                            PortNumber = pumpAddress + 1,
                            PortPump = i + 1
                        }
                        );
                }
            if (this.AnalyteMap.ContainsKey("air") == false)
                throw new Exception("AIR is a required analyte");
            if (this.AnalyteMap.ContainsKey("dispense") == false)
                throw new Exception("DISPENSE is a required analyte");

            string indicator = "";
            foreach (var k in this.AnalyteMap.Keys)
                indicator += k + "=port:" + this.AnalyteMap[k].PortNumber + " pump:" + this.AnalyteMap[k].PortPump + "\n";

            return indicator;
        }

        public class CommandResponse
        {
            public string Description { get; set; }
            public string Response { get; set; }
        }
        public List<CommandResponse> PumpStatus()
        {
            var commands =
              @"?63 Reports the actual downloaded firmware version string. 
?64 Reports the boot firmware part number and revision string.
?0 Reports the absolute position of the plunger 
?1 Reports the current plunger position in increments.
?2 Reports the plunger encoder position in mm 
?3 Reports the plunger initialization gap steps in increments
?4 Reports the number of backlash steps in increments
?6 Reports the start speed in increments per second
?7 Reports the top speed in increments per second
?8 Reports the cutoff speed in increments per second
?9 Reports the plunger drive ramp-up slope code setting
?10 Reports the plunger drive ramp-down slope code setting
?16 Reports the plunger maximum range in increments
?17 Reports the syringe volume in micro-liters.
?18 Reports the plunger absolute position in microliters
?20 Reports valve position ([i], [o], [b] or [e]) or (“1” – <n>)
?23 Reports the firmware part number and revision string.
?29 Reports the pump status 
?30 Reports status of digital input #1 (0 = 0Volts, 1 = 5Volts).
?36 Reports start speed in micro-liters per second
?37 Reports top speed in micro-liters per second
?38 Reports cutoff speed in micro-liters per second
?40 Reports total operating time in minutes
?41 Reports number of device power-ups
?42 Reports the total number of pump initializations [Z], [Y], [W] (absolute)
?43 Reports the total number of pump initializations since last device power
?44 Reports the total distance of plunger movement in meters (calculated)
?45 Reports the total number of plunger movements (absolute).
?46 Reports the total number of plunger movements since the last device
?47 Reports the total number of valve movements (absolute). Initialization
?48 Reports the total number of valve movements since the last device
?50 Reports the current power supply voltage in volts
?51 Reports the highest recorded power supply voltage in volts
?52 Reports the lowest recorded power supply voltage in volts
?53 Reports the real-time pump temperature in °F
?54 Reports the highest recorded temperature in °F
?55 Reports the lowest recorded temperature in °F
?60 Reports the number of firmware downloads
?65 Reports the checksum of the application firmware
?67 Reports the command buffer status (
?76 Reports the pump configuration in ASCII text (see below)
?96 Reports the self-test command string
?99 Reports the currently loaded command string".Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            var matrix = commands.Select(x => x.Trim().Split(new string[] { "Reports" }, StringSplitOptions.RemoveEmptyEntries));
            var response = new List<CommandResponse>();
            foreach (var command in matrix)
            {
                commPort.SendPumpCmd(1, command[0], commPort.GetNextSeqNumber());
                Thread.Sleep(100);
                var resp = commPort.ReadOneMessage().Trim();

                response.Add(new CommandResponse { Description = command.Last(), Response = resp });
            }

            response.Add(new CommandResponse
            {
                Description = "Error log 1",
                Response = GetInfo(1, ":10") + " " +
             commPort.DecodeErrors() + " " + commPort.LastErrorCode
            });

            response.Add(new CommandResponse
            {
                Description = "Error log 2",
                Response = GetInfo(2, ":10") + " " +
             commPort.DecodeErrors() + " " + commPort.LastErrorCode
            });


            return response;
        }

        public void Close()
        {
            commPort.CloseSerialPort();
        }
        public void Dispose()
        {
            Close();
        }

        private class PumpDefaults
        {
            public int Output { get; set; }
            public int Input { get; set; }
        }
        private Dictionary<int, PumpDefaults> NamedPorts = new Dictionary<int, PumpDefaults>();
        public string Initalize(string pullAnalyte, string openAnalyte)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            openAnalyte = openAnalyte.ToLower();
            int pumpID = this.AnalyteMap[openAnalyte].PortPump;
            string log = commPort.SendPumpCmd(pumpID, "?23", commPort.GetNextSeqNumber()) + "->\n";
            Thread.Sleep(200);
            var version = commPort.ReadOneMessage();
            Thread.Sleep(200);
            WaitForAction(pumpID);

            log += commPort.SendPumpCmd(pumpID, "?23", commPort.GetNextSeqNumber()) + "->\n";
            Thread.Sleep(200);
            log = commPort.ReadOneMessage() + ";\n";
            Thread.Sleep(200);
            WaitForAction(pumpID);

            log += commPort.SendPumpCmd(pumpID, $"U{(int)(SyringeVolume)}", commPort.GetNextSeqNumber()) + "->\n";
            Thread.Sleep(200);
            log = commPort.ReadOneMessage() + ";\n";
            Thread.Sleep(200);
            WaitForAction(pumpID);

            log += commPort.SendPumpCmd(pumpID, $"K{(int)(SyringeType)}", commPort.GetNextSeqNumber()) + "->\n";
            Thread.Sleep(200);
            log = commPort.ReadOneMessage() + ";\n";
            Thread.Sleep(200);
            WaitForAction(pumpID);
            var v2 = this.AnalyteMap[pullAnalyte.Trim().ToLower()];
            var v1 = this.AnalyteMap[openAnalyte.Trim().ToLower()];

            if (NamedPorts.ContainsKey(pumpID))
                NamedPorts.Remove(pumpID);

            NamedPorts.Add(pumpID, new PumpDefaults { Input = v1.PortNumber, Output = v2.PortNumber });

            log += commPort.SendPumpCmd(pumpID, $"Z4,{v1.PortNumber},{v2.PortNumber}R", commPort.GetNextSeqNumber()) + "   <";
            Thread.Sleep(50);
            log += commPort.ReadOneMessage() + ";\n";

            //Thread.Sleep(200);
            WaitForAction(pumpID);

            log += commPort.SendPumpCmd(pumpID, $"A0R", commPort.GetNextSeqNumber()) + "   <";
            log += commPort.ReadOneMessage() + ";\n";
            Thread.Sleep(200);
            WaitForAction(pumpID);

            Debug.Print(log);
            return version;
        }
        public float CurrentPumpPosition(int pumpID)
        {
            commPort.SendPumpCmd(pumpID, $"?18R", commPort.GetNextSeqNumber());
            Thread.Sleep(100);
            var port = commPort.ReadOneMessage().Trim();
            return float.Parse(port);
        }
        public float CurrentPosition(string analyte)
        {
            var port = this.AnalyteMap[analyte.Trim().ToLower()];
            return CurrentPumpPosition(port.PortPump);
        }
        private string WaitForAction(int pumpID)
        {
            Thread.Sleep(100);
            commPort.SendPumpCmd(pumpID, $"QR", commPort.GetNextSeqNumber());
            Thread.Sleep(100);
            var message = commPort.ReadOneMessage(showStatus: true);

            while (message.StartsWith("0"))
            {
                commPort.SendPumpCmd(pumpID, $"QR", commPort.GetNextSeqNumber());
                Thread.Sleep(100);
                message = commPort.ReadOneMessage(showStatus: true);
            }
            Thread.Sleep(200);
            if (message.Length > 1)
                Debug.Print(message);
            return message;
        }
        private string DrivePump2(string analyte, string analyte2, double volume, double speed_uL_s)
        {
            analyte = analyte.ToLower().Trim();
            analyte2 = analyte2.ToLower().Trim();
            string log = analyte + " " + analyte2 + "\n";
            try
            {
                int pumpID1 = this.AnalyteMap[analyte].PortPump;
                int pumpID2 = this.AnalyteMap[analyte2].PortPump;

                log += SwitchPump(pumpID1, this.AnalyteMap[analyte].PortNumber, pumpID2, this.AnalyteMap[analyte2].PortNumber, 0);



                log += commPort.SendPumpCmd(pumpID1, $"V{speed_uL_s:000},1", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";

                log += commPort.SendPumpCmd(pumpID2, $"V{speed_uL_s * this.OverDrawRatio:000},1", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";

                if (volume < 0)
                {
                    log += commPort.SendPumpCmd(pumpID1, $"P{-1 * volume:000},1R", commPort.GetNextSeqNumber()) + "   <";
                    log += commPort.ReadOneMessage() + ";\n";

                    log += commPort.SendPumpCmd(pumpID2, $"D{-1 * volume:000},1R", commPort.GetNextSeqNumber()) + "   <";
                    log += commPort.ReadOneMessage() + ";\n";
                }
                else
                {
                    log += commPort.SendPumpCmd(pumpID1, $"D{volume:000},1R", commPort.GetNextSeqNumber()) + "   <";
                    log += commPort.ReadOneMessage() + ";\n";

                    log += commPort.SendPumpCmd(pumpID2, $"P{volume:000},1R", commPort.GetNextSeqNumber()) + "   <";
                    log += commPort.ReadOneMessage() + ";\n";
                }


                WaitForAction(pumpID1);
                WaitForAction(pumpID2);

                Thread.Sleep(100);
                log += commPort.SendPumpCmd(pumpID1, $"?1", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(300);
                log += commPort.ReadOneMessage() + ";\n";

                Debug.Print(log);
            }
            catch (Exception ex)
            {
                throw new Exception(log + "\n" + ex.Message + "\n" + ex.Message);
            }
            return log;
        }

        private string ReturnPull(string returnAnalyte, string pullAnalyte2, double volume, double speed_uL_s)
        {
            returnAnalyte = returnAnalyte.ToLower().Trim();
            pullAnalyte2 = pullAnalyte2.ToLower().Trim();
            string log = pullAnalyte2 + " " + returnAnalyte + "\n";
            try
            {
                int pumpIDPull = this.AnalyteMap[pullAnalyte2].PortPump;
                int pumpID2 = this.AnalyteMap[returnAnalyte].PortPump;

                //change to the correct ports
                log += SwitchPump(pumpIDPull, this.AnalyteMap[pullAnalyte2].PortNumber, pumpID2, this.AnalyteMap[returnAnalyte].PortNumber, 0);

                log += commPort.SendPumpCmd(pumpIDPull, $"V{speed_uL_s:000},1", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";

                log += commPort.SendPumpCmd(pumpID2, $"V{speed_uL_s:000},1", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";


                log += commPort.SendPumpCmd(pumpIDPull, $"P{volume:000},1R", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";

                log += commPort.SendPumpCmd(pumpID2, $"A0R", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";



                WaitForAction(pumpIDPull);
                WaitForAction(pumpID2);

                Thread.Sleep(100);
                log += commPort.SendPumpCmd(pumpIDPull, $"?1", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(300);
                log += commPort.ReadOneMessage() + ";\n";

                Debug.Print(log);
            }
            catch (Exception ex)
            {
                throw new Exception(log + "\n" + ex.Message + "\n" + ex.Message);
            }
            return log;
        }

        public string GetInfo(int pumpID, string query)
        {
            string log = commPort.SendPumpCmd(pumpID, query, commPort.GetNextSeqNumber()) + "   <";
            Thread.Sleep(100);
            return commPort.ReadOneMessage();

        }
        public string errors(int pumpID)
        {
            string log = GetInfo(pumpID, ":10");
            return log + commPort.DecodeErrors() + "\n";
        }

        private string SwitchPump(int pumpID, int portNumber, int attempt)
        {

            string log = "";
            log += commPort.SendPumpCmd(pumpID, $"I{portNumber}R", commPort.GetNextSeqNumber()) + "   <";
            Thread.Sleep(100);

            log += commPort.ReadOneMessage() + ";\n";

            Thread.Sleep(100);
            string errorCode = GetInfo(pumpID, "Q2");

            WaitForAction(pumpID);

            log += "Q2: " + errorCode + "\n";
            Thread.Sleep(300);
            log += commPort.LastErrorCode + "\n";
            log += commPort.SendPumpCmd(pumpID, $"?20R", commPort.GetNextSeqNumber()) + "   <";
            Thread.Sleep(100);
            var port = commPort.ReadOneMessage().Trim();
            log += port + "\n";
            if (port == "i" && NamedPorts.ContainsKey(pumpID) && NamedPorts[pumpID].Input == portNumber)
                port = NamedPorts[pumpID].Input.ToString();
            if (port == "o" && NamedPorts.ContainsKey(pumpID) && NamedPorts[pumpID].Output == portNumber)
                port = NamedPorts[pumpID].Output.ToString();

            if (errorCode != "@" || port != portNumber.ToString())
            {
                log += GetInfo(pumpID, ":10");
                log += commPort.DecodeErrors() + "\n";
                log += commPort.LastErrorCode + "\n";

                /*log += commPort.SendPumpCmd(pumpID, $"w1,0R", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + ";\n";
                Thread.Sleep(100);
                WaitForAction(pumpID);*/

                if (attempt == 0)
                {
                    Thread.Sleep(500);
                    log += SwitchPump(pumpID, portNumber, 1);
                }
                else
                    throw new Exception($"{pumpID} : {portNumber} - Port was not selected correctly \n{log}");
            }

            Thread.Sleep(100);
            log += commPort.SendPumpCmd(pumpID, $"?1", commPort.GetNextSeqNumber()) + "   <";
            Thread.Sleep(300);
            log += commPort.ReadOneMessage() + ";\n";
            return log;
        }

        private string SwitchPump(int pumpID, int portNumber, int pumpID2, int portNumber2, int attempt)
        {
            string log = SwitchPump(pumpID, portNumber, attempt) + "\n";
            log += SwitchPump(pumpID2, portNumber2, attempt) + "\n";

            return log;
        }

        private string DrivePump(string analyte, double volume, double speed_uL_s)
        {
            analyte = analyte.ToLower().Trim();
            string log = analyte + " ";
            try
            {
                int pumpID = this.AnalyteMap[analyte].PortPump;
                log += SwitchPump(pumpID, this.AnalyteMap[analyte].PortNumber, 0);

                Thread.Sleep(100);
                log += commPort.SendPumpCmd(pumpID, $"?1", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(300);
                log += commPort.ReadOneMessage() + ";\n";

                log += commPort.SendPumpCmd(pumpID, $"V{speed_uL_s:000},1", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";
                if (volume < 0)
                {
                    log += commPort.SendPumpCmd(pumpID, $"P{-1 * volume:000},1R", commPort.GetNextSeqNumber()) + "   <";
                    log += commPort.ReadOneMessage() + ";\n";
                }
                else
                {
                    log += commPort.SendPumpCmd(pumpID, $"D{volume:000},1R", commPort.GetNextSeqNumber()) + "   <";
                    log += commPort.ReadOneMessage() + ";\n";
                }

                Thread.Sleep(100);
                WaitForAction(pumpID);

                Thread.Sleep(100);
                log += commPort.SendPumpCmd(pumpID, $"?1", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(300);
                log += commPort.ReadOneMessage() + ";\n";

                Debug.Print(log);
            }
            catch (Exception ex)
            {
                throw new Exception(log + "\n" + ex.Message);
            }
            return log;
        }

        public string Dispense(string analyte, double volume_uL, double speed_uL_s)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            string log = "";
            log += Pull(analyte, volume_uL, speed_uL_s);
            log += Push("Dispense", volume_uL, speed_uL_s);
            return log;
        }

        public string DispenseToCell(string analyte, double volume_ul, double speed_ul_S, double airGap_uL = 0, string pushAnalyte = "water")
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            if (DeadVolume_uL == 0)
                throw new Exception("Dead volume must be set for this function to work");
            var log = Dispense(analyte, volume_ul, speed_ul_S);
            if (airGap_uL > 0)
                log += Dispense("air", airGap_uL, 50);
            log += Dispense(pushAnalyte, DeadVolume_uL, speed_ul_S);
            return log;
        }

        public string DispensePush2Pump(string analyte, double volume_uL, double speed_uL_s, double maxPumped_ul = 200, string pushAnalyte = "water")
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            string log = "";
            var nPumps = (int)Math.Floor(volume_uL / maxPumped_ul);
            var pumpedVolume = 0.0;
            for (int i = 0; i < nPumps; i++)
            {
                Waste(maxPumped_ul * OverDrawRatio, speed_uL_s);
                Pull(pushAnalyte, maxPumped_ul, speed_uL_s);
                Push(analyte, maxPumped_ul, speed_uL_s);
                pumpedVolume += maxPumped_ul;
            }
            var remaining = volume_uL - pumpedVolume;
            if (remaining > 0)
            {
                Waste(remaining * OverDrawRatio, speed_uL_s);
                Pull(pushAnalyte, maxPumped_ul, speed_uL_s);
                Push(analyte, maxPumped_ul, speed_uL_s);
            }
            return log;
        }

        public string DispensePushLimited(string analyte, double volume_uL, double speed_uL_s, double maxPumped_ul = 200, string pushAnalyte = "water")
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            string log = "";
            var nPumps = (int)Math.Floor(volume_uL / maxPumped_ul);
            var pumpedVolume = 0.0;
            for (int i = 0; i < nPumps; i++)
            {
                Waste(maxPumped_ul * OverDrawRatio, speed_uL_s);
                Pull(pushAnalyte, maxPumped_ul, speed_uL_s);
                Push(analyte, maxPumped_ul, speed_uL_s);
                pumpedVolume += maxPumped_ul;
            }
            var remaining = volume_uL - pumpedVolume;
            if (remaining > 0)
            {
                Waste(remaining * OverDrawRatio, speed_uL_s);
                Pull(pushAnalyte, maxPumped_ul, speed_uL_s);
                Push(analyte, maxPumped_ul, speed_uL_s);
            }
            return log;
        }

        public string DispenseLimited(string analyte, double volume_uL, double speed_uL_s, double maxPumped_ul = 200)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            string log = "";
            var nPumps = (int)Math.Floor(volume_uL / maxPumped_ul);
            var pumpedVolume = 0.0;
            for (int i = 0; i < nPumps; i++)
            {
                Waste(maxPumped_ul * OverDrawRatio, speed_uL_s);
                Dispense(analyte, maxPumped_ul, speed_uL_s);
                pumpedVolume += maxPumped_ul;
            }
            var remaining = volume_uL - pumpedVolume;
            if (remaining > 0)
            {
                Waste(maxPumped_ul * OverDrawRatio, speed_uL_s);
                Dispense(analyte, remaining, speed_uL_s);
            }
            return log;
        }

        public string PushLimited(string analyte, double volume_uL, double speed_uL_s, double maxPumped_ul = 200)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            string log = "";
            var nPumps = (int)Math.Floor(volume_uL / maxPumped_ul);
            var pumpedVolume = 0.0;
            for (int i = 0; i < nPumps; i++)
            {
                Dispense(analyte, maxPumped_ul, speed_uL_s);
                pumpedVolume += maxPumped_ul;
            }
            var remaining = volume_uL - pumpedVolume;
            if (remaining > 0)
            {
                Dispense(analyte, remaining, speed_uL_s);
            }
            return log;
        }

        public string DispenseLimitedBack2(string pushAnalyte, string pullAnalyte, double volume_uL, double speed_uL_s, double maxPumped_ul = 200)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            string log = "";
            var nPumps = (int)Math.Floor(volume_uL / maxPumped_ul);
            var pumpedVolume = 0.0;
            for (int i = 0; i < nPumps; i++)
            {
                Pull(pullAnalyte, maxPumped_ul * OverDrawRatio, speed_uL_s);
                Push("air", maxPumped_ul * OverDrawRatio, speed_uL_s);
                Pull(pushAnalyte, maxPumped_ul, speed_uL_s);
                Push("dispense", maxPumped_ul, speed_uL_s);
                pumpedVolume += maxPumped_ul;
            }
            var remaining = volume_uL - pumpedVolume;
            if (remaining > 0)
            {
                Pull(pullAnalyte, remaining * OverDrawRatio, speed_uL_s);
                Push("air", remaining * OverDrawRatio, speed_uL_s);
                Pull(pushAnalyte, remaining, speed_uL_s);
                Push("dispense", remaining, speed_uL_s);
            }
            return log;
        }

        public string DispenseToCellLimited(string analyte, double volume_ul, double speed_ul_S, double maxPumped_ul = 200, double airGap_uL = 0, string pushAnalyte = "water")
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            if (DeadVolume_uL == 0)
                throw new Exception("Dead volume must be set for this function to work");
            string log = "";
            try
            {
                log = DispenseLimited(analyte, volume_ul, speed_ul_S, maxPumped_ul);
                if (airGap_uL > 0)
                    log += DispenseLimited("air", airGap_uL, 50, maxPumped_ul);

                log += DispenseLimited(pushAnalyte, DeadVolume_uL, speed_ul_S, maxPumped_ul);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + ex.StackTrace, ex);
            }
            return log;
        }

        public string Waste(double volume_uL, double speed_uL_s)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            string log = Pull("Waste", volume_uL, speed_uL_s);
            log += Push("Air", volume_uL, speed_uL_s);
            return log;
        }

        public string Push(string analyte, double volume_uL, double speed_uL_s)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }

            return DrivePump(analyte, volume_uL, speed_uL_s);
        }
        public string Pull(string analyte, double volume_uL, double speed_uL_s)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }

            return DrivePump(analyte, -1 * volume_uL, speed_uL_s);
        }

        public string changedsgdtasub() { return "changed"; }

        public string PullPush(string pushAnalyte, string pullAnalyte, double volume_uL, double speed_uL_s)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            if (this.AnalyteMap[pushAnalyte.ToLower()].PortPump == this.AnalyteMap[pullAnalyte.ToLower()].PortPump)
            {
                string log = "Same\n" + DrivePump(pullAnalyte.ToLower(), -1 * volume_uL, speed_uL_s) + "\n";
                log += DrivePump(pushAnalyte.ToLower(), volume_uL, speed_uL_s);
                return log;
            }
            else
                return DrivePump2(pushAnalyte, pullAnalyte, volume_uL, speed_uL_s);
        }

        public bool IsCrossPumps(string analyte1, string analyte2)
        {
            return this.AnalyteMap[analyte1.ToLower()].PortPump == this.AnalyteMap[analyte2.ToLower()].PortPump;
        }

        public int WhichPump(string analyte1)
        {
            return this.AnalyteMap[analyte1.ToLower()].PortPump;
        }

        public string CrossDispense(string dispenseAnalyte, string wasteAnalyte, string wasteOutletAnalyte, double volume_uL, double speed_uL_s)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            string log = $"{dispenseAnalyte} {wasteAnalyte} {volume_uL} \n";
            log += ReturnPull(wasteOutletAnalyte, dispenseAnalyte, volume_uL, speed_uL_s) + "\n";


            return log + DrivePump2("dispense", wasteAnalyte, volume_uL, speed_uL_s);
        }

        public string Draw(string wasteAnalyte, string drawAnalyte, double drawVolume_ul, double speed_uL_s)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            var analyte = drawAnalyte.ToLower().Trim();
            var waste = wasteAnalyte.ToLower().Trim();
            string log = analyte + " " + waste + "\n";
            try
            {
                int pumpAnalyte = this.AnalyteMap[analyte].PortPump;
                int pumpWaste = this.AnalyteMap[waste].PortPump;

                //change to the correct ports
                log += SwitchPump(pumpAnalyte, this.AnalyteMap[analyte].PortNumber, pumpWaste, this.AnalyteMap[waste].PortNumber, 0);


                log += commPort.SendPumpCmd(pumpAnalyte, $"V{speed_uL_s:000},1", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";

                log += commPort.SendPumpCmd(pumpWaste, $"V{speed_uL_s * this.OverDrawRatio:000},1", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";


                log += commPort.SendPumpCmd(pumpAnalyte, $"P{drawVolume_ul:000},1R", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";

                log += commPort.SendPumpCmd(pumpWaste, $"A0R", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";

                WaitForAction(pumpAnalyte);
                WaitForAction(pumpWaste);

                log += commPort.SendPumpCmd(pumpAnalyte, $"?1", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + ";\n";

                Debug.Print(log);
            }
            catch (Exception ex)
            {
                throw new Exception(log + "\n" + ex.Message + "\n" + ex.Message);
            }
            return log;
        }

        public string Exchange(string pullAnalyte, string pushAnalyte, double drawVolume_ul, double speed_uL_s)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            pushAnalyte = pushAnalyte.ToLower().Trim();
            pullAnalyte = pullAnalyte.ToLower().Trim();
            string log = pushAnalyte + " " + pullAnalyte + "\n";
            try
            {
                int pumpPush = this.AnalyteMap[pushAnalyte].PortPump;
                int pumpPull = this.AnalyteMap[pullAnalyte].PortPump;

                log += SwitchPump(pumpPush, this.AnalyteMap[pushAnalyte].PortNumber, pumpPull, this.AnalyteMap[pullAnalyte].PortNumber, 0);

                log += commPort.SendPumpCmd(pumpPush, $"V{speed_uL_s:000},1", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";

                log += commPort.SendPumpCmd(pumpPull, $"V{speed_uL_s * this.OverDrawRatio:000},1", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";


                log += commPort.SendPumpCmd(pumpPush, $"D{drawVolume_ul:000},1R", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";

                log += commPort.SendPumpCmd(pumpPull, $"P{drawVolume_ul * this.OverDrawRatio:000},1R", commPort.GetNextSeqNumber()) + "   <";
                log += commPort.ReadOneMessage() + ";\n";

                WaitForAction(pumpPush);
                WaitForAction(pumpPull);

                log += commPort.SendPumpCmd(pumpPush, $"?1", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + ";\n";

                Debug.Print(log);
            }
            catch (Exception ex)
            {
                throw new Exception(log + "\n" + ex.Message + "\n" + ex.Message);
            }
            return log;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="analyte1"></param>
        /// <param name="drawVolume_ul1">posivite to push, negative to pull</param>
        /// <param name="speed_uL_s1"></param>
        /// <param name="analyte2"></param>
        /// <param name="drawVolume_ul2">posivite to push, negative to pull</param>
        /// <param name="speed_uL_s2"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string SyncPumps(string analyte1, double drawVolume_ul1, double speed_uL_s1, string analyte2, double drawVolume_ul2, double speed_uL_s2)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            analyte1 = analyte1.ToLower().Trim();
            analyte2 = analyte2.ToLower().Trim();
            string log = analyte1 + " " + analyte2 + "\n";
            try
            {
                int analyte1_n = this.AnalyteMap[analyte1].PortPump;
                int analyte2_n = this.AnalyteMap[analyte2].PortPump;
                log += commPort.SendPumpCmd(analyte1_n, $"CR", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";
                log += commPort.SendPumpCmd(analyte2_n, $"CR", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";

                WaitForAction(analyte1_n);
                WaitForAction(analyte2_n);
                //change to the correct ports

                log += SwitchPump(analyte1_n, this.AnalyteMap[analyte1].PortNumber, analyte2_n, this.AnalyteMap[analyte2].PortNumber, 0);

                Thread.Sleep(100);
                log += commPort.SendPumpCmd(analyte1_n, $"V{speed_uL_s1:000},1R", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";
                Thread.Sleep(100);
                log += commPort.SendPumpCmd(analyte2_n, $"V{speed_uL_s2:000},1R", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";
                Thread.Sleep(100);
                if (drawVolume_ul1 > 0)
                    log += commPort.SendPumpCmd(analyte1_n, $"D{drawVolume_ul1:000},1R", commPort.GetNextSeqNumber()) + "   <";
                else
                    log += commPort.SendPumpCmd(analyte1_n, $"P{-1 * drawVolume_ul1:000},1R", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";

                if (drawVolume_ul2 > 0)
                    log += commPort.SendPumpCmd(analyte2_n, $"D{drawVolume_ul2:000},1R", commPort.GetNextSeqNumber()) + "   <";
                else
                    log += commPort.SendPumpCmd(analyte2_n, $"P{-1 * drawVolume_ul2:000},1R", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";
                Thread.Sleep(100);
                log += commPort.SendPumpCmd(analyte1_n, $"A0R", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";
                Thread.Sleep(100);
                log += commPort.SendPumpCmd(analyte2_n, $"A0R", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";
                Thread.Sleep(100);

                WaitForAction(analyte1_n);

                Thread.Sleep(300);

                WaitForAction(analyte2_n);

                Thread.Sleep(100);
                log += commPort.SendPumpCmd(analyte1_n, $"?1R", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";

                Debug.Print(log);
            }
            catch (Exception ex)
            {
                log += commPort.SendPumpCmd(1, $":1", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";
                log += commPort.SendPumpCmd(2, $":1", commPort.GetNextSeqNumber()) + "   <";
                Thread.Sleep(100);
                log += commPort.ReadOneMessage() + "\n";
                throw new Exception(log + "\n" + ex.Message + "\n" + ex.StackTrace);
            }
            return log;
        }

        public string PullMore(string analyte, double desiredVolume, double pullVolume, double speed_uL_s)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            Waste(desiredVolume, speed_uL_s);
            string log = Pull(analyte, pullVolume, speed_uL_s);
            log += Push("air", pullVolume - desiredVolume, speed_uL_s);
            log += Push("dispense", desiredVolume, speed_uL_s);

            return log;
        }
        public void ZeroPosition(string expellPort, double speed_uL_s)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return;
            }
            expellPort = expellPort.ToLower().Trim();
            int pumpID = this.AnalyteMap[expellPort].PortPump;
            string log = SwitchPump(pumpID, this.AnalyteMap[expellPort.ToLower()].PortNumber, 0);

            Thread.Sleep(100);

            log += commPort.SendPumpCmd(pumpID, $"V{speed_uL_s:000},1R", commPort.GetNextSeqNumber()) + "   <";
            Thread.Sleep(100);
            log += commPort.ReadOneMessage() + "\n";

            Debug.Print(this.errors(pumpID));


            log += commPort.SendPumpCmd(pumpID, $"A0R", commPort.GetNextSeqNumber()) + "   <";
            log += commPort.ReadOneMessage() + ";\n";

            WaitForAction(pumpID);
        }
        public string PullPush(string pushAnalyte, string pullAnalyte, double volume_uL, double speed_uL_s, double maxVolume_ul)
        {
            if (isTesting)
            {
                Thread.Sleep(300);
                return "";
            }
            string log = "";
            int nPumps = (int)(volume_uL / maxVolume_ul);
            double pumped = 0;
            for (int i = 0; i < nPumps; i++)
            {
                log += DrivePump(pullAnalyte, -1 * maxVolume_ul, speed_uL_s);
                log += DrivePump(pushAnalyte, maxVolume_ul, speed_uL_s);
                log += pumped += maxVolume_ul;
            }
            pumped = volume_uL - pumped;
            log += DrivePump(pullAnalyte, -1 * pumped, speed_uL_s);
            log += DrivePump(pushAnalyte, pumped, speed_uL_s);
            return log;
        }

    }
}
