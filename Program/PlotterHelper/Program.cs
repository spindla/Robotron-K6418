using PlotterHelper.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("PlotterHelper");
    config.SetApplicationVersion("2.0.0");

    config.AddCommand<ConvertCommand>("convert")
        .WithDescription("Konvertiert eine HPGL-Datei für den ROBOTRON K6418 / CM6415.")
        .WithExample(["convert", "C:\\example.hpgl"])
        .WithExample(["convert", "C:\\example.hpgl", "--output", "C:\\out.hpgl"]);

    config.AddCommand<SendCommand>("send")
        .WithDescription("Sendet eine HPGL-Datei über die serielle Schnittstelle an den Plotter.")
        .WithExample(["send", "C:\\example.hpgl", "--port", "COM1"])
        .WithExample(["send", "C:\\example.hpgl", "--port", "COM1", "--baud", "9600", "--convert", "--save"]);

#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
});

try
{
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return -1;
}
