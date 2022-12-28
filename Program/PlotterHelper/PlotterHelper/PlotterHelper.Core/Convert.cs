using System.IO.Ports;
using System.Text.RegularExpressions;

namespace PlotterHelper.Core
{
    public class Convert
    {
        /// <summary>
        /// Convert the given hpgl string to ROBOTRON standard hpgl version
        /// </summary>
        /// <param name="hpglFile">String contains hpgl commands</param>
        /// <returns></returns>
        public static string convertHPGL(string hpglFile)
        {
            hpglFile = new Regex("IN;", RegexOptions.IgnoreCase).Replace(hpglFile, "", 1);
            //if (_sw) hpglFile = "SW;" + hpglFile;
            hpglFile = Regex.Replace(hpglFile, "(?<!;PA)PD", "PD;PA");
            hpglFile = Regex.Replace(hpglFile, "(?<!;PA)PU", "PU;PA");
            hpglFile = hpglFile.Remove(hpglFile.Length - 4, 4) + "NR;";
            return hpglFile;
        }

        internal static Parity convertParity(string parity)
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

        internal static StopBits convertStopBits(string stopBits)
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
    }
}