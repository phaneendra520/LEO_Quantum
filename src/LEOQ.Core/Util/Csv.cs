using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LEOQ.Core.Util;

public static class Csv
{
    public static void WriteRows(string path, IEnumerable<string> header, IEnumerable<IEnumerable<object>> rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var sw = new StreamWriter(path);
        sw.WriteLine(string.Join(",", header));
        foreach (var row in rows)
        {
            sw.WriteLine(string.Join(",", row.Select(Format)));
        }
    }

    private static string Format(object o)
    {
        return o switch
        {
            null => "",
            double d => d.ToString("G17", CultureInfo.InvariantCulture),
            float f => f.ToString("G9", CultureInfo.InvariantCulture),
            _ => o.ToString()?.Replace(",", " ") ?? "",
        };
    }
}
