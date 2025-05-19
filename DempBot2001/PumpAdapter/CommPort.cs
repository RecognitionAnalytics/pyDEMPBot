using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
namespace PumpAdapter
{

    internal class CommPort
    {
        private const int max_input_buf_len = 2048;

        private const byte ETX = 3;

        private const byte STX = 2;

        private const byte FF = byte.MaxValue;

        private const byte CTRL_40 = 64;

        private const byte CTRL_30 = 48;

        internal SerialPort commPort;

        private byte[] input_data_buf = new byte[2048];

        private byte CmdSeqNumber;

        public CommPort()
        {
            var CustomErrorsModes = @"
  96 64 ’ @| No Error
  97 65 a A| Initialization Error
  98 66 b B| Invalid Command
  99 67 c C| Invalid Operand
 103 71 g G| Device Not Initialized
 104 72 h H| Invalid Valve Configuration
 105 73 i I| Plunger Overload
 106 74 j J| Valve Overload
 107 75 k K| Plunger Move Not Allowed
 108 76 l L| Extended Error Present
 109 77 m M| Nvmem Access Failure
 110 78 n N| Command Buffer Empty or Not Ready
 111 79 o O| Command Buffer Overflow".Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            int cc = 0;
            foreach (var error in CustomErrorsModes)
            {
                var parts = error.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var cose = byte.Parse(parts[0].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[0].Trim());
                StatusErrors.Add(cc, parts[1].Trim());
                Errors.Add(cose, parts[1].Trim());
                cose = byte.Parse(parts[0].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1].Trim());
                Errors.Add(cose, "Busy - " + parts[1].Trim());
                
                cc++;
            }
        }
        Dictionary<int, string> StatusErrors = new Dictionary<int, string>();
        Dictionary<byte, string> Errors = new Dictionary<byte, string>();
        public byte GetNextSeqNumber()
        {
            if (++CmdSeqNumber > 7)
            {
                CmdSeqNumber = 1;
            }
            return CmdSeqNumber;
        }

        public bool OpenSerialPort(string portName, int bauRate)
        {

            commPort = new SerialPort();
            commPort.PortName = portName;
            commPort.BaudRate = bauRate;
            commPort.Parity = Parity.None;
            commPort.DataBits = 8;
            commPort.StopBits = StopBits.One;
            commPort.Handshake = Handshake.None;

            commPort.Open();

            return true;
        }

        public string LastCommand { get; private set; }
        public string SendPumpCmd(int pumpID, string cmd, byte seq)
        {
            LastCommand = $"{pumpID}:{cmd}";
            Thread.Sleep(50);
            Encoding aSCII = Encoding.ASCII;
            byte[] bytes = aSCII.GetBytes(cmd);
            byte[] array = new byte[6 + bytes.Length];
            array[0] = byte.MaxValue;
            array[1] = 2;
            if (pumpID > 15)
            {
                array[2] = (byte)pumpID;
            }
            else
            {
                array[2] = (byte)(Convert.ToByte('0') + pumpID);
            }
            array[3] = (byte)(48 + seq);
            Array.Copy(bytes, 0, array, 4, bytes.Length);
            array[4 + bytes.Length] = 3;
            array[5 + bytes.Length] = 0;
            for (int i = 1; i < bytes.Length + 5; i++)
            {
                array[5 + bytes.Length] ^= array[i];
            }

            if (commPort == null || !commPort.IsOpen)
            {
                throw new Exception("COM port is not opened");
            }
            commPort.Write(array, 0, array.Length);

            return $"Pump {pumpID}: {cmd} ";
        }

        public bool SendRSPCmd(byte[] cmd, byte seq)
        {
            byte[] array = new byte[cmd.Length + 4];
            array[0] = 2;
            array[1] = (byte)(64 + seq);
            Array.Copy(cmd, 0, array, 2, cmd.Length);
            array[cmd.Length + 2] = 3;
            array[cmd.Length + 3] = (byte)(0x41u ^ seq);
            for (int i = 0; i < cmd.Length; i++)
            {
                array[cmd.Length + 3] ^= cmd[i];
            }

            if (commPort == null || !commPort.IsOpen)
            {
                throw new Exception("COM port is not opened");
            }
            commPort.Write(array, 0, array.Length);

            return true;
        }

        internal bool SendRspAck(byte[] cmd)
        {
            byte[] array = new byte[6];
            array[0] = 2;
            array[1] = 64;
            array[2] = cmd[0];
            array[3] = cmd[1];
            array[4] = 3;
            array[5] = (byte)(array[0] ^ array[1] ^ array[2] ^ array[3] ^ array[4]);
            try
            {
                if (commPort == null || !commPort.IsOpen)
                {
                    throw new Exception("COM port is not opened");
                }
                commPort.Write(array, 0, array.Length);
            }
            catch (Exception ex)
            {

                return false;
            }
            return true;
        }

        private byte ReadOneByte()
        {
            if (commPort == null || !commPort.IsOpen)
            {
                throw new Exception("COM port is not opened");
            }
            return (byte)commPort.ReadByte();
        }

        public int StatusOneMessageO(int timeout = 5000)
        {
            byte[] Message = null;
            byte[] array = new byte[1024];
            int num = 0;
            SystemError systemError = SystemError.No_Error;
            commPort.ReadTimeout = timeout;

            DateTime now = DateTime.Now;
            byte b;
            do
            {
                b = ReadOneByte();
                DateTime now2 = DateTime.Now;
                if ((now2 - now).TotalMilliseconds > (double)timeout)
                {
                    throw new Exception("SystemError.Timeout_Error");

                }
                Thread.Sleep(10);
            }
            while (b != 2);
            if (systemError != 0)
            {
                throw new Exception("SystemError.Timeout_Error");
            }
            byte b2 = b;
            array[num++] = b;
            do
            {
                b = ReadOneByte();
                b2 = (byte)(b2 ^ b);
                array[num++] = b;
            }
            while (b != 3 && num < array.Length - 1);
            if (b != 3)
            {
                throw new Exception("SystemError.Timeout_Error");
            }
            b = ReadOneByte();
            if (b != b2)
            {
                throw new Exception("SystemError.Timeout_Error");
            }
            Message = new byte[num + 1];
            Array.Copy(array, Message, Message.Length);

            var status = (Message[2] >> 5) & 1;
            byte errorMasked = (byte)(Message[2] & (byte)0x0F);
            if (errorMasked != 96 && errorMasked != 64 && errorMasked != 0)
                if (Errors.ContainsKey(errorMasked))
                    LastErrorCode = Errors[errorMasked];
                else
                    LastErrorCode = "UInknown";
            var errorCode = Message[2] & 0xF;

            if (Message.Length > 5)
            {
                string answer = Encoding.ASCII.GetString(Message, 3, Message.Length - 5);
                throw new Exception("Message returned" + answer);
            }

            return status;

        }

        public string ReadOneMessage(int timeout = 5000, bool showStatus = false)
        {
            byte[] Message = null;
            byte[] array = new byte[1024];
            int num = 0;
            SystemError systemError = SystemError.No_Error;
            commPort.ReadTimeout = timeout;

            DateTime now = DateTime.Now;
            byte b;
            do
            {
                b = ReadOneByte();
                DateTime now2 = DateTime.Now;
                if ((now2 - now).TotalMilliseconds > (double)timeout)
                {
                    throw new Exception("SystemError.Timeout_Error");

                }
                Thread.Sleep(10);
            }
            while (b != 2);
            if (systemError != 0)
            {
                throw new Exception("SystemError.Timeout_Error");
            }
            byte b2 = b;
            array[num++] = b;
            do
            {
                b = ReadOneByte();
                b2 = (byte)(b2 ^ b);
                array[num++] = b;
            }
            while (b != 3 && num < array.Length - 1);
            if (b != 3)
            {
                throw new Exception("SystemError.Timeout_Error");
            }
            b = ReadOneByte();
            if (b != b2)
            {
                throw new Exception("SystemError.Timeout_Error");
            }
            Message = new byte[num + 1];
            Array.Copy(array, Message, Message.Length);

            var status = (Message[2] >> 5) & 1;
            Debug.Print(status.ToString());
            int errorMasked = (Message[2] & (byte)0x0F);

            if (errorMasked!=0 && StatusErrors.ContainsKey(errorMasked))
                throw new Exception($"Status error {LastCommand} > {StatusErrors[errorMasked]}");
            else
                LastErrorCode = "Unknown";
            var errorCode = Message[2] & 0xF;
            string answer = showStatus ? status.ToString() : "";
            if (Message.Length > 5)
            {

                answer = Encoding.ASCII.GetString(Message, 3, Message.Length - 5);

            }
            AllChars = Message;
            return answer;

        }
        public string DecodeErrors()
        {
            string history = "";
            string error;
            for (int i = 2; i < AllChars.Length; i++)
            {
                if (Errors.ContainsKey(AllChars[i]))
                    error = Errors[AllChars[i]];
                else
                    error = "UInknown";
                history += error + "\n";
            }
            return history;
        }

        public byte[] AllChars { get; private set; }
        public string LastErrorCode { get; private set; }


        public void CloseSerialPort()
        {
            if (commPort.IsOpen)
            {
                commPort.Close();
            }
        }
    }
    public enum SystemError
    {
        No_Error = 0,
        Timeout_Error = 31,
        Checksum_Error = 32,
        Read_Port_Error = 33,
        Write_Port_Error = 34
    }

}


