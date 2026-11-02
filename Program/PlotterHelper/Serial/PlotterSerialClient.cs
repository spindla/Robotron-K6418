using System.IO.Ports;

namespace PlotterHelper.Serial;

public sealed class PlotterConnectionSettings
{
    public required string PortName { get; init; }
    public int BaudRate { get; init; } = 9600;
    public Parity Parity { get; init; } = Parity.None;
    public int DataBits { get; init; } = 8;
    public StopBits StopBits { get; init; } = StopBits.One;
}

/// <summary>
/// Kapselt die serielle Kommunikation mit dem K6418/CM6415.
///
/// Der Plotter benutzt ein einfaches Software-Protokoll auf drei
/// Steuerbytes, die während der Übertragung jederzeit zurückkommen können:
///   0x11 (DC1/XON) - Empfangspuffer wieder frei, weiterschreiben erlaubt
///   0x13 (DC3/XOFF) - Empfangspuffer voll, sofort pausieren
///   0x14 (DC4)      - Plotterfehler (kein Standard-XON/XOFF-Zeichen)
///
/// Diese Klasse implementiert das Protokoll bewusst SELBST (Handshake.None
/// am SerialPort) statt sich auf Handshake.XOnXOff von .NET zu verlassen:
/// Sonst konkurrieren zwei Mechanismen um dieselben Steuerbytes, was zu
/// sporadisch verlorenen/verzögerten Bytes führt - und genau ein verlorenes
/// Byte mitten in einer Koordinate lässt den Plotter kurz an eine falsche
/// Position fahren, bis der nächste vollständige PA-Befehl es wieder
/// korrigiert.
/// </summary>
public sealed class PlotterSerialClient : IDisposable
{
    private const byte PlotterError = 20; // 0x14
    private const byte BufferFull = 19;   // 0x13 (XOFF)
    private const byte BufferClear = 17;  // 0x11 (XON)

    private readonly SerialPort _port;
    private volatile bool _bufferFull;
    private volatile bool _abort;
    private bool _firstErrorSeen;

    /// <summary>Statusmeldungen zum Protokollieren (z.B. per Spectre.Console ausgeben).</summary>
    public event Action<string>? Log;

    public PlotterSerialClient(PlotterConnectionSettings settings)
    {
        _port = new SerialPort(settings.PortName, settings.BaudRate, settings.Parity,
            settings.DataBits, settings.StopBits)
        {
            Handshake = Handshake.None,
            WriteBufferSize = 512,
            DtrEnable = false,
            RtsEnable = false,
            ReadTimeout = 200
        };
    }

    public void Open()
    {
        _bufferFull = false;
        _abort = false;
        _firstErrorSeen = false;

        _port.Open();
        _port.DiscardInBuffer();
        //_port.DataReceived += OnDataReceived;
        Log?.Invoke("serial port opened");
    }

    public void Close()
    {
        //_port.DataReceived -= OnDataReceived;
        if (_port.IsOpen) _port.Close();
        Log?.Invoke("serial port closed");
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        /*try
        {
            // Immer ALLE gerade verfügbaren Bytes abholen, nicht nur eins -
            // das DataReceived-Event garantiert nicht "ein Byte pro Aufruf".
            while (_port.BytesToRead > 0)
            {
                int b = _port.ReadByte();
                if (b < 0) break;

                switch ((byte) b)
                {
                    case PlotterError:
                        if (!_firstErrorSeen)
                        {
                            _firstErrorSeen = true;
                            Log?.Invoke("plotter error (first, ignored)");
                        }
                        else
                        {
                            _abort = true;
                            Log?.Invoke("plotter error (aborting)");
                        }

                        break;
                    case BufferFull:
                        _bufferFull = true;
                        Log?.Invoke("plotter buffer full, pausing");
                        break;
                    case BufferClear:
                        _bufferFull = false;
                        Log?.Invoke("plotter buffer clear, resuming");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Log?.Invoke($"exception while reading: {ex.Message}");
        }*/
        PollForStatusBytes();
    }
    
    private void HandleStatusByte(byte b)
    {
        switch (b)
        {
            case PlotterError:
                if (!_firstErrorSeen) { _firstErrorSeen = true; Log?.Invoke("plotter error (first, ignored)"); }
                else { _abort = true; Log?.Invoke("plotter error (aborting)"); }
                break;
            case BufferFull:
                _bufferFull = true;
                Log?.Invoke("plotter buffer full, pausing");
                break;
            case BufferClear:
                _bufferFull = false;
                Log?.Invoke("plotter buffer clear, resuming");
                break;
        }
    }
    
    /// <summary>Zusätzliche feste Pause (ms) nach jedem gesendeten Byte. 0 = aus.</summary>
    public int SafetyDelayMs { get; set; } = 2;

    /// <summary>
    /// Fragt BytesToRead SYNCHRON ab, statt nur auf das (unter Linux per
    /// Polling mit spürbarer Verzögerung implementierte) DataReceived-Event
    /// zu warten. Wird vor jedem einzelnen zu sendenden Byte aufgerufen.
    /// </summary>
    private void PollForStatusBytes()
    {
        try
        {
            while (_port.BytesToRead > 0)
            {
                int b = _port.ReadByte();
                if (b < 0) break;
                HandleStatusByte((byte) b);
            }
        }
        catch (Exception ex)
        {
            Log?.Invoke($"exception while polling: {ex.Message}");
        }
    }
    

    /// <summary>
    /// Sendet die Bytes einzeln, wartet dabei auf XON, falls der Plotter mit
    /// XOFF signalisiert, dass sein Puffer voll ist, und bricht bei einem
    /// zweiten (unwiederbringlichen) Plotterfehler ab.
    /// </summary>
    /// <param name="data">Die zu sendenden Bytes (bereits fertig konvertiertes HPGL).</param>
    /// <param name="onProgress">Wird nach jedem gesendeten Byte mit der Gesamtzahl bereits gesendeter Bytes aufgerufen.</param>
    /// <param name="cancellationToken">Zum Abbrechen von außen (z.B. Strg+C).</param>
    /// <returns>true, wenn die komplette Datei gesendet wurde; false bei Abbruch/Fehler.</returns>
    public async Task<bool> SendAsync(byte[] data, Action<int>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < data.Length; i++)
        {
            var waitStart = DateTime.UtcNow;
            while (true)
            {
                PollForStatusBytes();
                if (!_bufferFull || _abort) break;
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(5, cancellationToken);
                if ((DateTime.UtcNow - waitStart).TotalSeconds > 30)
                {
                    Log?.Invoke("timeout waiting for plotter (no XON received)");
                    return false;
                }
            }

            if (_abort)
            {
                Log?.Invoke("aborted after plotter error");
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            _port.Write(data, i, 1);
            onProgress?.Invoke(i + 1);
            if (SafetyDelayMs > 0) await Task.Delay(SafetyDelayMs, cancellationToken);
        }

        return true;
    }

    public void Dispose()
    {
        try
        {
            Close();
        }
        catch
        {
            // beim Aufräumen keine weiteren Exceptions nach außen geben
        }

        _port.Dispose();
    }
}
