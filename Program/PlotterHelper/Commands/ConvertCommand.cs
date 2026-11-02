using System.ComponentModel;
using System.Threading;
using PlotterHelper.Hpgl;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PlotterHelper.Commands;

[Description("Konvertiert eine HPGL-Datei für den ROBOTRON K6418 / CM6415 und speichert sie.")]
public sealed class ConvertCommand : Command<ConvertSettings>
{
    protected override int Execute(CommandContext context, ConvertSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            string converted = "";

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Lese Datei ein ...", ctx =>
                {
                    string original = File.ReadAllText(settings.FilePath);

                    ctx.Status("Konvertiere HPGL ...");
                    converted = HpglConverter.Convert(original);
                });

            string outputPath = settings.OutputPath ?? BuildDefaultOutputPath(settings.FilePath);
            File.WriteAllText(outputPath, converted);

            AnsiConsole.MarkupLine($"[green]✓[/] Konvertiert und gespeichert unter: [bold]{Markup.Escape(outputPath)}[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]✗ Konvertierung fehlgeschlagen[/]");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    internal static string BuildDefaultOutputPath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath) ?? "";
        string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        return Path.Combine(directory, $"{nameWithoutExt}_converted.hpgl");
    }
}
