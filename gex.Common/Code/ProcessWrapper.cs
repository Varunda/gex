using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace gex.Common.Code {

    public class ProcessWrapper {

        public string StdOut { get; private set; } = "";

        public string StdErr { get; private set; } = "";

        public ProcessStartInfo StartInfo { get; private set; }

        public int ExitCode { get; private set; }

        private ProcessWrapper(ProcessStartInfo startInfo) {
            StartInfo = startInfo;
        }

        public static ProcessWrapper Create(ProcessStartInfo parms, TimeSpan timeout) {

            using Process proc = new Process();
            proc.StartInfo = parms;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;

            Stopwatch timer = Stopwatch.StartNew();

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            using AutoResetEvent outputWaitHandle = new(false);
            using AutoResetEvent errorWaitHandle = new(false);
            proc.OutputDataReceived += (sender, e) => {
                if (e.Data == null) {
                    outputWaitHandle.Set();
                } else {
                    output.AppendLine(e.Data);
                }
            };
            proc.ErrorDataReceived += (sender, e) => {
                if (e.Data == null) {
                    errorWaitHandle.Set();
                } else {
                    error.AppendLine(e.Data);
                }
            };

            proc.Start();

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            // doing it this way prevents hangs due to not reading stdout or stderr
            // https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
            if (!(proc.WaitForExit(timeout) && outputWaitHandle.WaitOne(timeout) && errorWaitHandle.WaitOne(timeout))) {
                throw new TimeoutException($"failed to run process in timeout: {timeout}");
            }

            ProcessWrapper wrapper = new(parms);
            wrapper.ExitCode = proc.ExitCode;
            wrapper.StdOut = output.ToString();
            wrapper.StdErr = error.ToString();

            return wrapper;
        }

    }
}
