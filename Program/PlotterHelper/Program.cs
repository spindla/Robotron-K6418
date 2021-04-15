using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace PlotterHelper
{
    internal class Program
    {
        private static SerialPort _serialPort;
        private static bool _fullBuffer;
        private static readonly string _version = "0.2";
        private static readonly string _year = "2021";
        private static string _hpglFile;
        private static bool _firstError;
        private static readonly bool _sw = false;
        private static readonly bool _saveFile = false;

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Invalid arguments");
                return;
            }

            string command = args[0];
            switch (command)
            {
                case "-v":
                    version();
                    break;
                case "-h":
                    help();
                    break;
                case "-s" when args.Length == 2:
                    openFile(args[1]);
                    communication();
                    break;

                case "-c" when args.Length == 3 && args[1] == "-s":
                    openFile(args[2]);
                    convert();
                    if (_saveFile) saveFile(args[2]);
                    communication();
                    break;
                case "-s" when args.Length == 3 && args[1] == "-c":
                    openFile(args[2]);
                    convert();
                    if (_saveFile) saveFile(args[2]);
                    communication();
                    break;
                default:
                    Console.WriteLine("Invalid command");
                    break;
            }
        }

        private static void initSerialPort(string portName, int baudRate, Parity parity, int dataBits,
            StopBits stopBits, Handshake handshake, int bufferSize, bool dtrControl, bool rtsControl)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _serialPort.Handshake = handshake;
            _serialPort.WriteBufferSize = bufferSize;
            _serialPort.DtrEnable = dtrControl;
            _serialPort.RtsEnable = rtsControl;
        }

        private static void version()
        {
            Console.WriteLine("Version {0}, Alexander Spindler {1}", _version, _year);
        }

        private static void help()
        {

        }

        private static void communication()
        {
            openPort();
            send();
            closePort();
        }

        private static void openPort()
        {
            try
            {
                initSerialPort("COM4", 9600, Parity.None, 8, StopBits.One, Handshake.None, 512, false, false);
                _serialPort.Open();
                _serialPort.DiscardInBuffer();
                _serialPort.DataReceived += _serialPort_DataReceived;
                Console.WriteLine(connectionLog("serial port opened"));
            }
            catch (Exception e)
            {
                Console.WriteLine(exceptionLog(e));
            }
        }

        private static void closePort()
        {
            try
            {
                _serialPort.Close();
                Console.WriteLine(connectionLog("serial port closed"));
            }
            catch (Exception e)
            {
                Console.WriteLine(exceptionLog(e));
            }
        }

        private static void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                switch (_serialPort.ReadByte())
                {
                    case 20: //0x14
                        if (!_firstError)
                        {
                            _firstError = true; //first time the plotter returns a plotter error, cant be ignored!
                        }
                        else
                        {
                            _fullBuffer = true;
                            _serialPort.DiscardInBuffer();
                            Console.WriteLine(connectionLog("a plotter error"));
                        }

                        break;
                    case 19: //0x13
                        Console.WriteLine(connectionLog("full buffer"));
                        _fullBuffer = true;
                        _serialPort.DiscardInBuffer();
                        break;
                    case 17: //0x11
                        Console.WriteLine(connectionLog("cleared buffer"));
                        _fullBuffer = false;
                        break;
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exceptionLog(exp));
            }
        }

        private static void send()
        {
            byte[] bytes = Encoding.ASCII.GetBytes(_hpglFile);
            int length = bytes.Length;
            ProgressBar progressBar = new ProgressBar();

            for (int i = 0; i < length; i++)
            {
                if (_fullBuffer) return;
                try
                {
                    _serialPort.Write(bytes, i, 1);
                }
                catch (Exception e)
                {
                    Console.WriteLine(exceptionLog(e));
                    return;
                }
                progressBar.Report(i / ((float)length - 1));
                Thread.Sleep(1);
            }
        }

        private static void convert()
        {
            Console.WriteLine(fileLog("convert file"));
            //must be implemented
            _hpglFile = new Regex("IN;", RegexOptions.IgnoreCase).Replace(_hpglFile, "", 1);
            if (_sw) _hpglFile = "SW;" + _hpglFile;
            _hpglFile = Regex.Replace(_hpglFile, "(?<!;PA)PD", "PD;PA");
            _hpglFile = Regex.Replace(_hpglFile, "(?<!;PA)PU", "PU;PA");
            _hpglFile = _hpglFile.Remove(_hpglFile.Length - 4, 4) + "NR;";
            Console.WriteLine(fileLog("file converted"));
        }

        private static void openFile(string filePath)
        {
            Console.WriteLine(fileLog("open file"));
            _hpglFile = File.ReadAllText(filePath);
            Console.WriteLine(fileLog("file opend"));
        }

        private static void saveFile(string filePath)
        {
            try
            {
                Console.WriteLine(fileLog("save file"));
                filePath = filePath.Remove(filePath.Length - 5, 5) + "_converted.hpgl";
                File.WriteAllText(filePath, _hpglFile);
                Console.WriteLine(fileLog("file saved"));
            }
            catch (Exception e)
            {
                Console.WriteLine(exceptionLog(e));
            }
        }

        private static string connectionLog(string value)
        {
            return "[conn]: " + value;
        }

        private static string fileLog(string value)
        {
            return "[file]: " + value;
        }

        private static string exceptionLog(Exception value)
        {
            return "[excp]: " + value;
        }
    }
}