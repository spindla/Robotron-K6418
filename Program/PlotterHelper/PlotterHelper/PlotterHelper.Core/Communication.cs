using PlotterHelper.Cli;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace PlotterHelper.Core
{
    public class Communication
    {
        private static SerialPort? _serialPort;
        private static bool _fullBuffer;
        private static bool _firstError;
        private static string? _hpglFile;

        public Communication(string connection)
        {
            string[] con = Regex.Split(connection, ";");
            initSerialPort(con[0], System.Convert.ToInt32(con[1]), Convert.convertParity(con[2]), System.Convert.ToInt32(con[3]),
                Convert.convertStopBits(con[4]), Handshake.XOnXOff, 512, true, true);
        }

        public Communication(string portName, int baudRate, Parity parity, int dataBits,
    StopBits stopBits, Handshake handshake, int bufferSize, bool dtrControl, bool rtsControl)
        {
            initSerialPort(portName,baudRate,parity,dataBits,stopBits,handshake,bufferSize,dtrControl,rtsControl);
        }

        private static void initSerialPort(string portName, int baudRate, Parity parity, int dataBits,
    StopBits stopBits, Handshake handshake, int bufferSize, bool dtrControl, bool rtsControl)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                Handshake = handshake,
                WriteBufferSize = bufferSize,
                DtrEnable = dtrControl,
                RtsEnable = rtsControl
            };
        }

        public static void openPort()
        {
            try
            {
                _serialPort?.Open();
                _serialPort?.DiscardInBuffer();
                _serialPort.DataReceived += _serialPort_DataReceived;
            }
            catch (Exception e)
            {
            }
        }

        public static void closePort()
        {
            try
            {
                _serialPort.Close();
            }
            catch (Exception e)
            {
            }
        }
        private static void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                switch (_serialPort?.ReadByte())
                {
                    case 20: //0x14
                        if (!_firstError)
                        {
                            _firstError = true; //first time the plotter returns a plotter error, can be ignored!
                        }
                        else
                        {
                            _fullBuffer = true;
                            _serialPort.DiscardInBuffer();
                        }

                        break;
                    case 19: //0x13
                        _fullBuffer = true;
                        _serialPort.DiscardInBuffer();
                        break;
                    case 17: //0x11
                        _fullBuffer = false;
                        break;
                }
            }
            catch (Exception exp)
            {

            }
        }

        public static void send()
        {
            byte[] bytes = Encoding.ASCII.GetBytes(_hpglFile);
            int length = bytes.Length;

            for (int i = 0; i < length; i++)
            {
                if (_fullBuffer) return;
                try
                {
                    _serialPort?.Write(bytes, i, 1);
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }

        private static void sendWithProgress()
        {
            byte[] bytes = Encoding.ASCII.GetBytes(_hpglFile);
            int length = bytes.Length;
            ProgressBar progressBar = new ProgressBar();

            for (int i = 0; i < length; i++)
            {
                if (_fullBuffer) return;
                try
                {
                    _serialPort?.Write(bytes, i, 1);
                    progressBar.Report(i / ((float)length - 1));
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }
    }
}
