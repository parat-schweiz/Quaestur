using System;
using System.IO;
using System.Diagnostics;

namespace Publicus
{
    public static class PdfUnite
    {
        private const string PdfUniteBinary = "/usr/bin/pdfunite";

        public static byte[] Work(byte[] a, byte[] b)
        {
            var tempFolder = Path.Combine("/tmp", DateTime.Now.Ticks.ToString());

            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }

            try
            {
                var fileA = Path.Combine(tempFolder, "a.pdf");
                var fileB = Path.Combine(tempFolder, "b.pdf");
                var fileC = Path.Combine(tempFolder, "c.pdf");

                File.WriteAllBytes(fileA, a);
                File.WriteAllBytes(fileB, b);

                var start = new ProcessStartInfo(PdfUniteBinary, 
                    string.Format("{0} {1} {2}", fileA, fileB, fileC));
                start.RedirectStandardError = true;
                start.RedirectStandardInput = true;
                start.RedirectStandardOutput = true;
                start.UseShellExecute = false;

                var process = Process.Start(start);
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception(process.StandardError.ReadToEnd());
                }

                if (File.Exists(fileC))
                {
                    return File.ReadAllBytes(fileC);
                }
                else
                {
                    throw new Exception("Output file not found"); 
                }
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true); 
                } 
            }
        }
    }
}
