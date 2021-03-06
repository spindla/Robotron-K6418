# Usage:

_**you need the latest dot.NET 6 preview!**_

Description of the commandline program:

```
Send and convert files to your ROBOTRON K6418 / CM6415

OPTIONS:
   -h      Display this help message
   -c      Convert *.hpgl
   -cs     Convert *.hpgl and save a copy of the converted file
   -s      send hpgl file to plotter, connection string:
                                          [serial-port;baud-rate;parity;data-bits;stop-bit]

USAGE:
   PlotterHelper   [-h]
                   [-c -s [connection] [file path] | -s -c [connection] [file path]]
                   [-s [connection] [file path]]
                   [-cs [file path]]

EXAMPLE:
   "PlotterHelper -cs -s COM1;9600;N;8;1 C:\example.hpgl"
   This will convert, save and send the example.hpgl from C:\ to the
   plotter on serialconnection COM1 with 9600 baud, no parity bit, 8 data-bits and one stop-bit.
```

## How to get an *.hpgl-file?

You can use Inkscape version 0.92.4 (newer version dosen't work for me!) and save your file as *.hpgl. Before you must convert any object to path.

Settings for *.hpgl-file:

max. document size 370mm x 270mm

![alt text](Inkscape_Settings_1.jpg)

![alt text](Inkscape_Settings_2.jpg)
