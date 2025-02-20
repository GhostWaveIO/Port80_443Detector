using System.Diagnostics;
using System.Text;

namespace TerminalCommands.PromptCommand {
    public class CmdExecutor : IDisposable {
        private Process cmdProcess;
        private StreamWriter cmdStreamWriter;

        string? _content = null;
        string _error = null;
        TimeSpan _timingToEnds = new TimeSpan(0,0,2);

        public CmdExecutor() {
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
        public async Task<string?> ExecuteCommandAsync(string command, TimeSpan timeToWait) {
            if (cmdProcess == null || cmdProcess.HasExited) {
                StartCmdProcess();
            }
            _content = null;

            await cmdStreamWriter.WriteLineAsync(command);
            await cmdStreamWriter.FlushAsync();
            if (timeToWait.Equals(TimeSpan.Zero)) {
                cmdProcess.WaitForExit();
            } else {
                cmdProcess.WaitForExit(timeToWait);
            }

            await Task.Delay(500);


            while (_timingToEnds > TimeSpan.Zero) {
                _timingToEnds -= TimeSpan.FromMilliseconds(100);
                await Task.Delay(100);
            }

            if (!string.IsNullOrWhiteSpace(_error)) {
                throw new Exception(_error);
            }

            return _content;
        }

        //##########################################################################################
        public string? ExecuteCommand(string command, TimeSpan timeToWait) {
            if (cmdProcess == null || cmdProcess.HasExited) {
                StartCmdProcess();
            }
            _content = null;

            cmdStreamWriter.WriteLine(command);
            cmdStreamWriter.Flush();
            if (timeToWait.Equals(TimeSpan.Zero)) {
                cmdProcess.WaitForExit();
            } else {
                cmdProcess.WaitForExit(timeToWait);
            }

            Thread.Sleep(500);


            while (_timingToEnds > TimeSpan.Zero) {
                _timingToEnds -= TimeSpan.FromMilliseconds(100);
                Thread.Sleep(100);
            }

            if (!string.IsNullOrWhiteSpace(_error)) {
                throw new Exception(_error);
            }

            return _content;
        }

        //##########################################################################################
        private void StartCmdProcess() {
            _timingToEnds = new TimeSpan(0, 0, 2);
            cmdProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "cmd.exe",
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
            _timingToEnds = new TimeSpan(0, 0, 0, 2, 200);
            _content += e.Data + '\n';
        }

        ////##########################################################################################
        private void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
            _timingToEnds = new TimeSpan(0, 0, 0, 2, 200);
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
                cmdStreamWriter.Dispose();
                await cmdProcess.WaitForExitAsync();
                cmdProcess.Dispose();
            }
        }
    }
}
