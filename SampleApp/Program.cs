using NtfsIndexer;
using System.Diagnostics;

var start = Stopwatch.GetTimestamp();
var longestFileName = "";
Console.WriteLine("Scanning C drive...");
var found = new MftScanner().EnumerateFiles("C:").Count(name =>
{
    if (name.Length > longestFileName.Length) longestFileName = name;
    return true;
});
Console.WriteLine($"Found {found} files, longest file name: {longestFileName}, took: {Stopwatch.GetElapsedTime(start)}");