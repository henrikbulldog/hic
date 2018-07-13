# HIC - GE Proficy Historian Command Line Query Tool
hic.exe is a command line tool to query sensor data from the GE Proficy Historian.

# Directory Structure

## /deps
This folder contains the GE Proficy Historian Client SDK assemblies.
The version of the client SDK assembly must match that of the GE Historian server.

## /src
This folder contains the C# hic.exe source files.

## /exe
This folder contains hic executables for different versions of the GE Proficy Historian.

# Building the Module
To build the module, open up the solution in Visual Studio and run the build command. 

# Running the module
Usage: hic <options>
Options:
        --server <server dns or environment variable>
        --user <user name or environment variable>
        --psw <password or environment variable>
        --tags <tag names>
        --start <start time>
        --end <end time>
        --samplingMode <CurrentValue | Interpolated | Trend | RawByTime | RawByNumber | Calculated | Lab | InterpolatedToRaw | TrendToRaw | LabToRaw | RawByFilterToggling | Trend2 | TrendToRaw2>
        --calculationMode <Average | StandardDeviation | Total | Minimum | Maximum | Count | RawAverage | RawStandardDeviation | RawTotal | MinimumTime | MaximumTime | TimeGood | StateCount | StateTime | OPCAnd | OPCOr | FirstRawValue | FirstRawTime | LastRawValue | LastRawTime | TagStats>
        --intervalMicroseconds <sample interval in 1/1000000 seconds>
        [--numberOfSamples <number of samples>]
        [--out <output csv file>]