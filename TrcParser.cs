using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CANUDS_DTC_Report.Models;

namespace CANUDS_DTC_Report
{
    public class TrcParser
    {
        public static  List<CanFrame> Parse(string filePath)
        {
            var frames = new List<CanFrame>();

            foreach (var line in File.ReadLines(filePath))
            {
                if (!line.Contains("Rx") && !line.Contains("Tx"))
                    continue;

                try
                {
                    var parts = line.Split(' ');
                    var timestampStr = parts[1].Replace("(", "").Replace(")", "");
                    var time = DateTime.Now.AddSeconds(double.Parse(timestampStr, CultureInfo.InvariantCulture));

                    uint id = Convert.ToUInt32(parts[4], 16);

                    var dataList = new List<byte>();
                    for (int i = 5; i < parts.Length; i++)
                    {
                        if (byte.TryParse(parts[i], NumberStyles.HexNumber, null, out var b))
                            dataList.Add(b);
                    }

                    frames.Add(new CanFrame(id, dataList.ToArray(), time));
                }
                catch
                {
                    // Ignorar errores de línea inválida
                    continue;
                }
            }

            return frames;
        }
    }
}
