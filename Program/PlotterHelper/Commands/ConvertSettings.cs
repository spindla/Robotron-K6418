using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PlotterHelper.Commands;

public sealed class ConvertSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("Pfad zur .hpgl-Datei.")]
    public string FilePath { get; init; } = string.Empty;

    [CommandOption("-o|--output <PATH>")]
    [Description("Zielpfad für die konvertierte Datei. Standard: '<datei>_converted.hpgl'.")]
    public string? OutputPath { get; init; }

    public override ValidationResult Validate()
    {
        if (!File.Exists(FilePath))
        {
            return ValidationResult.Error($"Datei nicht gefunden: {FilePath}");
        }

        return ValidationResult.Success();
    }
}
