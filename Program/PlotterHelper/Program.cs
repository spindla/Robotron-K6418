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
        private static readonly string _version = "1.0";
        private static readonly string _year = "2021";
        private static string _hpglFile;
        private static bool _firstError;
        private static readonly bool _sw = false;

        private static readonly string _helpString = "Send and convert files to your ROBOTRON K6418 / CM6415\n" +
                                                     "\n" +
                                                     "OPTIONS:\n" +
                                                     "   -h      Display this help message\n" +
                                                     "   -c      Convert *.hpgl\n" +
                                                     "   -cs     Convert *.hpgl and save a copy of the converted file\n" +
                                                     "   -s      send hpgl file to plotter, connection string:\n" +
                                                     "                                          [serial-port;baud-rate;parity;data-bits;stop-bit]\n" +
                                                     "\n" +
                                                     "USAGE:\n" +
                                                     "   PlotterHelper   [-h]\n" +
                                                     "                   [-c -s [connection] [file path] | -s -c [connection] [file path]]\n" +
                                                     "                   [-s [connection] [file path]]\n" +
                                                     "                   [-cs [file path]]\n" +
                                                     "\n" +
                                                     "EXAMPLE:\n" +
                                                     "   \"PlotterHelper -cs -s COM1;9600;N;8;1 C:\\example.hpgl\"\n" +
                                                     "   This will convert, save and send the example.hpgl from C:\\ to the \n" +
                                                     "   plotter on serialconnection COM1 with 9600 baud, no parity bit, 8 data-bits and one stop-bit.\n";

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Invalid arguments\n");
                help();
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
                case "-s" when args.Length == 3:
                    openFile(args[2]);
                    communication(args[1]);
                    break;
                case "-c" when args.Length == 4 && args[1] == "-s":
                    openFile(args[3]);
                    convertHPGL();
                    communication(args[2]);
                    break;
                case "-s" when args.Length == 4 && args[1] == "-c":
                    openFile(args[3]);
                    convertHPGL();
                    communication(args[2]);
                    break;
                case "-cs" when args.Length == 4 && args[1] == "-s":
                    openFile(args[3]);
                    convertHPGL();
                    saveFile(args[3]);
                    communication(args[2]);
                    break;
                case "-s" when args.Length == 4 && args[1] == "-cs":
                    openFile(args[3]);
                    convertHPGL();
                    saveFile(args[3]);
                    communication(args[2]);
                    break;
                case "-cs" when args.Length == 2:
                    openFile(args[1]);
                    convertHPGL();
                    saveFile(args[1]);
                    break;
                default:
                    Console.WriteLine("Invalid arguments\n");
                    help();
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
            Console.WriteLine(_helpString);
        }

        private static void communication(string connection)
        {
            string[] con = Regex.Split(connection, ";");
            initSerialPort(con[0], Convert.ToInt32(con[1]), convertParity(con[2]), Convert.ToInt32(con[3]),
                convertStopBits(con[4]), Handshake.None, 512, false, false);
            openPort();
            send();
            closePort();
        }

        private static void openPort()
        {
            try
            {
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
                    progressBar.Report(i / ((float) length - 1));
                }
                catch (Exception e)
                {
                    Console.WriteLine(exceptionLog(e));
                    return;
                }

                Thread.Sleep(1);
            }
        }

        private static void convertHPGL()
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

        private static Parity convertParity(string parity)
        {
            switch (parity)
            {
                case "N":
                    return Parity.None;
                case "O":
                    return Parity.Odd;
                case "E":
                    return Parity.Even;
                case "M":
                    return Parity.Mark;
                case "S":
                    return Parity.Space;
                default:
                    return Parity.None;
            }
        }

        private static StopBits convertStopBits(string stopBits)
        {
            switch (stopBits)
            {
                case "0":
                    return StopBits.None;
                case "1":
                    return StopBits.One;
                case "1.5":
                    return StopBits.OnePointFive;
                case "2":
                    return StopBits.Two;
                default:
                    return StopBits.One;
            }
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