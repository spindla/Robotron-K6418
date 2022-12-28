using System;
using System.IO;

namespace PlotterHelper.Cli
{
    internal class Program
    {
        private static readonly string _version = "1.1";
        private static readonly string _year = "2022";
        private static string _hpglFile;

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
                    Core.Convert.convertHPGL(_hpglFile);
                    communication(args[2]);
                    break;
                case "-s" when args.Length == 4 && args[1] == "-c":
                    openFile(args[3]);
                    Core.Convert.convertHPGL(_hpglFile);
                    communication(args[2]);
                    break;
                case "-cs" when args.Length == 4 && args[1] == "-s":
                    openFile(args[3]);
                    Core.Convert.convertHPGL(_hpglFile);
                    saveFile(args[3]);
                    communication(args[2]);
                    break;
                case "-s" when args.Length == 4 && args[1] == "-cs":
                    openFile(args[3]);
                    Core.Convert.convertHPGL(_hpglFile);
                    saveFile(args[3]);
                    communication(args[2]);
                    break;
                case "-cs" when args.Length == 2:
                    openFile(args[1]);
                    Core.Convert.convertHPGL(_hpglFile);
                    saveFile(args[1]);
                    break;
                default:
                    Console.WriteLine("Invalid arguments\n");
                    help();
                    break;
            }
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
            new Core.Communication(connection);
            Core.Communication.openPort();
            Core.Communication.send();
            Core.Communication.closePort();
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