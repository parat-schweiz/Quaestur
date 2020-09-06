using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SiteLibrary
{
    public abstract class LatexDocument
    {
        private const string XelatexBinary = "xelatex";

        public abstract string TexDocument { get; }

        public string ErrorText { get; private set; }

        public virtual bool Prepare() { return true; }

        public LatexDocument()
        {
            ErrorText = string.Empty; 
        }

        public virtual IEnumerable<Tuple<string, byte[]>> Files
        {
            get { return new Tuple<string, byte[]>[0];  }
        }

        public byte[] Compile()
        {
            const string documentName = "document.tex";
            const string pdfName = "document.pdf";
            string tempFolder = Path.Combine("/tmp", DateTime.Now.Ticks.ToString());

            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }

            try
            {
                foreach (var file in Files)
                {
                    File.WriteAllBytes(Path.Combine(tempFolder, file.Item1), file.Item2);
                }

                File.WriteAllText(Path.Combine(tempFolder, documentName), TexDocument);

                var start = new ProcessStartInfo(XelatexBinary, documentName);
                start.UseShellExecute = false;
                start.WorkingDirectory = tempFolder;
                start.RedirectStandardError = true;
                start.RedirectStandardOutput = true;

                var process = Process.Start(start);
                var startTime = DateTime.Now;
                ErrorText = string.Format("Compiling document {0}", documentName);

                while (!process.HasExited)
                {
                    if (DateTime.Now.Subtract(startTime).TotalSeconds > 30d)
                    {
                        ErrorText += string.Format("Timout exeeded: {0:0.000}s", DateTime.Now.Subtract(startTime).TotalSeconds);
                        break;
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }

                if (!process.HasExited)
                {
                    process.Kill();
                    ErrorText += "Process killed";
                    ErrorText += process.StandardOutput.ReadToEnd() + "\n" + process.StandardError.ReadToEnd();
                    return null;
                }

                if (process.ExitCode != 0)
                {
                    ErrorText += process.StandardOutput.ReadToEnd() + "\n" + process.StandardError.ReadToEnd();
                    return null;
                }

                ErrorText += process.StandardOutput.ReadToEnd() + "\n" + process.StandardError.ReadToEnd();
                var pdfPath = Path.Combine(tempFolder, pdfName);

                if (!File.Exists(pdfPath))
                {
                    ErrorText += "PDF not created";
                    return null;
                }

                return File.ReadAllBytes(pdfPath);
            }
            catch (Exception exception)
            {
                ErrorText += string.Format("{0} when creating PDF\n{1}", exception.GetType().FullName, exception.ToString());
                return null;
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
