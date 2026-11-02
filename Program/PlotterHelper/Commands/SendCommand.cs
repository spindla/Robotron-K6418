using System.ComponentModel;
using System.Text;
using System.Threading;
using PlotterHelper.Hpgl;
using PlotterHelper.Serial;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PlotterHelper.Commands;

[Description("Sendet eine (optional zu konvertierende) HPGL-Datei an den Plotter.")]
public sealed class SendCommand : AsyncCommand<SendSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, SendSettings settings, CancellationToken cancellationToken)
    {
        string content = await File.ReadAllTextAsync(settings.FilePath, cancellationToken);

        if (settings.Convert)
        {
            AnsiConsole.MarkupLine("[grey]Konvertiere HPGL ...[/]");
            content = HpglConverter.Convert(content);

            if (settings.Save)
            {
                string outputPath = ConvertCommand.BuildDefaultOutputPath(settings.FilePath);
                await File.WriteAllTextAsync(outputPath, content, cancellationToken);
                AnsiConsole.MarkupLine($"[grey]Konvertierte Kopie gespeichert: {Markup.Escape(outputPath)}[/]");
            }
        }

        byte[] bytes = Encoding.ASCII.GetBytes(content);

        var connectionSettings = new PlotterConnectionSettings
        {
            PortName = settings.Port,
            BaudRate = settings.BaudRate,
            Parity = settings.ParseParity()!.Value,
            DataBits = settings.DataBits,
            StopBits = settings.ParseStopBits()!.Value
        };

        using var client = new PlotterSerialClient(connectionSettings);
        client.SafetyDelayMs = settings.DelayMs;
        client.Log += message => AnsiConsole.MarkupLine($"[grey][[conn]] {Markup.Escape(message)}[/]");

        try
        { 
            client.Open();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]✗ Verbindung fehlgeschlagen[/]");
            AnsiConsole.WriteException(ex);
            return 1;
        }

        bool success;
        try
        {
            success = await AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Sende an Plotter[/]", maxValue: bytes.Length);
                    return await client.SendAsync(bytes, sent => task.Value = sent, cancellationToken);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]✗ Übertragung fehlgeschlagen[/]");
            AnsiConsole.WriteException(ex);
            success = false;
        }
        finally
        {
            client.Close();
        }

        if (success)
        {
            AnsiConsole.MarkupLine("[green]✓ Datei vollständig gesendet[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]✗ Übertragung nicht vollständig (siehe Meldungen oben)[/]");
        return 1;
    }
}
