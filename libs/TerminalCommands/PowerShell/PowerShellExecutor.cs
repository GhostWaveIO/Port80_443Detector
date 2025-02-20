using System.Diagnostics;
using System.Text;

namespace TerminalCommands.PowerShell {
    public class PowerShellExecutor : IDisposable {
        private Process cmdProcess;
        private StreamWriter cmdStreamWriter;
        private StreamReader cmdStreamReader;

        string _content = null;
        string _error = null;
        int _secondsToEnds = 2;

        public PowerShellExecutor() {
            StartCmdProcess();
        }


        //##########################################################################################
        public async Task<string> ExecuteCommandAsync(string command) {
            return await ExecuteCommandAsync(command, new TimeSpan(0));
        }

        //##########################################################################################
        public async Task<string> ExecuteCommandAsync(string command, int ticks) {
            return await ExecuteCommandAsync(command, new TimeSpan(ticks));
        }

        //##########################################################################################
        public async Task<string> ExecuteCommandAsync(string command, TimeSpan timeToWait) {
            if (cmdProcess == null || cmdProcess.HasExited) {
                StartCmdProcess();
            }


            await cmdStreamWriter.WriteLineAsync(command);
            await cmdStreamWriter.FlushAsync();
            if (timeToWait.Equals(TimeSpan.Zero)) {
                cmdProcess.WaitForExit();
            } else {
                cmdProcess.WaitForExit(timeToWait);
            }

            while (_secondsToEnds > 0) {
                _secondsToEnds--;
                await Task.Delay(1000);
            }

            if (!string.IsNullOrWhiteSpace(_error)) {
                throw new Exception(_error);
            }


            return _content;
        }

        //##########################################################################################
        public string ExecuteCommand(string command) {
            return ExecuteCommand(command, new TimeSpan(0));
        }

        //##########################################################################################
        public string ExecuteCommand(string command, int ticks) {
            return ExecuteCommand(command, new TimeSpan(ticks));
        }

        //##########################################################################################
        public string ExecuteCommand(string command, TimeSpan timeToWait) {
            if (cmdProcess == null || cmdProcess.HasExited) {
                StartCmdProcess();
            }


            cmdStreamWriter.WriteLine(command);
            cmdStreamWriter.Flush();
            if (timeToWait.Equals(TimeSpan.Zero)) {
                cmdProcess.WaitForExit();
            } else {
                cmdProcess.WaitForExit(timeToWait);
            }

            Thread.Sleep(4000);


            while (_secondsToEnds > 0) {
                _secondsToEnds--;
                Thread.Sleep(1000);
            }

            if (!string.IsNullOrWhiteSpace(_error)) {
                throw new Exception(_error);
            }


            return _content;
        }

        //##########################################################################################
        private void StartCmdProcess() {
            _secondsToEnds = 2;
            cmdProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "powershell.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            cmdProcess.OutputDataReceived += proc_OutputDataReceived;
            cmdProcess.ErrorDataReceived += proc_ErrorDataReceived;

            cmdProcess.Start();
            cmdProcess.BeginOutputReadLine();
            cmdStreamWriter = cmdProcess.StandardInput;


            //cmdStreamReader = cmdProcess.StandardOutput;
        }

        ////##########################################################################################
        private void proc_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            _secondsToEnds = 2;
            _content += e.Data + '\n';
        }

        ////##########################################################################################
        private void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
            _secondsToEnds = 2;
            _error += e.Data + '\n';
        }

        //##########################################################################################
        public void Dispose() {
            if (cmdProcess != null && !cmdProcess.HasExited) {
                cmdStreamWriter.WriteLine("exit");
                cmdStreamWriter.Flush();
                cmdProcess.WaitForExit();
                cmdProcess.Dispose();
            }
        }

        //##########################################################################################
        public async Task DisposeAsync() {
            if (cmdProcess != null && !cmdProcess.HasExited) {
                await cmdStreamWriter.WriteLineAsync("exit");
                await cmdStreamWriter.FlushAsync();
                await cmdProcess.WaitForExitAsync();
                cmdProcess.Dispose();
            }
        }
    }
}
