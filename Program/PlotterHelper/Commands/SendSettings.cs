using System.ComponentModel;
using System.IO.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PlotterHelper.Commands;

public sealed class SendSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("Pfad zur .hpgl-Datei.")]
    public string FilePath { get; init; } = string.Empty;

    [CommandOption("-p|--port <PORT>")]
    [Description("Serieller Port, z.B. COM1 oder /dev/ttyUSB0.")]
    public string Port { get; init; } = string.Empty;

    [CommandOption("-b|--baud <BAUD>")]
    [Description("Baudrate.")]
    [DefaultValue(9600)]
    public int BaudRate { get; init; } = 9600;

    [CommandOption("--parity <PARITY>")]
    [Description("N (none), O (odd), E (even), M (mark), S (space).")]
    [DefaultValue("N")]
    public string Parity { get; init; } = "N";

    [CommandOption("--data-bits <BITS>")]
    [DefaultValue(8)]
    public int DataBits { get; init; } = 8;

    [CommandOption("--stop-bits <BITS>")]
    [Description("0, 1, 1.5 oder 2.")]
    [DefaultValue("1")]
    public string StopBits { get; init; } = "1";

    [CommandOption("-c|--convert")]
    [Description("Datei vor dem Senden für den K6418 konvertieren.")]
    [DefaultValue(false)]
    public bool Convert { get; init; }

    [CommandOption("-s|--save")]
    [Description("Zusammen mit --convert: konvertierte Datei zusätzlich speichern.")]
    [DefaultValue(false)]
    public bool Save { get; init; }
    
    [CommandOption("--delay-ms <MS>")]
    [Description("Zusätzliche feste Pause pro gesendetem Byte in Millisekunden (Sicherheitsabstand gegen Pufferüberlauf). 0 = aus.")]
    [DefaultValue(2)]
    public int DelayMs { get; init; } = 2;

    public override ValidationResult Validate()
    {
        if (!File.Exists(FilePath))
        {
            return ValidationResult.Error($"Datei nicht gefunden: {FilePath}");
        }

        if (string.IsNullOrWhiteSpace(Port))
        {
            return ValidationResult.Error("Bitte einen seriellen Port mit --port angeben.");
        }

        if (ParseParity() is null)
        {
            return ValidationResult.Error($"Ungültige Parität: {Parity}");
        }

        if (ParseStopBits() is null)
        {
            return ValidationResult.Error($"Ungültige Stop-Bits: {StopBits}");
        }

        return ValidationResult.Success();
    }

    public Parity? ParseParity() => Parity.ToUpperInvariant() switch
    {
        "N" => System.IO.Ports.Parity.None,
        "O" => System.IO.Ports.Parity.Odd,
        "E" => System.IO.Ports.Parity.Even,
        "M" => System.IO.Ports.Parity.Mark,
        "S" => System.IO.Ports.Parity.Space,
        _ => null
    };

    public System.IO.Ports.StopBits? ParseStopBits() => StopBits switch
    {
        "0" => System.IO.Ports.StopBits.None,
        "1" => System.IO.Ports.StopBits.One,
        "1.5" => System.IO.Ports.StopBits.OnePointFive,
        "2" => System.IO.Ports.StopBits.Two,
        _ => null
    };
}
