using System.Text.RegularExpressions;

namespace PlotterHelper.Hpgl;

/// <summary>
/// Wandelt eine "normale" HPGL-Datei so um, dass sie vom vereinfachten
/// Kommando-Interpreter des ROBOTRON K6418 / CM6415 verstanden wird.
/// </summary>
public static class HpglConverter
{
    /// <param name="input">Roher Inhalt der .hpgl-Datei.</param>
    /// <param name="switchScaling">Entspricht dem alten "_sw"-Schalter: falls
    /// gesetzt, wird der Datei ein "SW;" vorangestellt.</param>
    public static string Convert(string input, bool switchScaling = false)
    {
        string hpgl = input;

        hpgl = new Regex("IN;", RegexOptions.IgnoreCase).Replace(hpgl, "", 1);

        if (switchScaling)
        {
            hpgl = "SW;" + hpgl;
        }

        hpgl = Regex.Replace(hpgl, "(?<!;PA)PD(?=[-\\d])", "PD;PA");
        hpgl = Regex.Replace(hpgl, "(?<!;PA)PU(?=[-\\d])", "PU;PA");
        
        hpgl = hpgl.TrimEnd();

        hpgl = hpgl.Length >= 4
            ? hpgl.Remove(hpgl.Length - 4, 4) + "NR;"
            : hpgl + "NR;";

        return hpgl;
    }
}
