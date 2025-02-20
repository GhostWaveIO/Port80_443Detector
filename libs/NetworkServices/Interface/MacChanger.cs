using System.Text;
using TerminalCommands.PowerShell;

namespace NetworkServices.Interface {
    public class MacChanger {

        private string _interfaceName;

        public MacChanger(string interfaceName) {
            _interfaceName = interfaceName;
        }

        //################################################################################################
        public async Task ChangeAsync(string newMac) {
            //000729553557

            char[]? divMac = newMac?.Where(cm => Char.IsDigit(cm))?.ToArray();
            string? rawMac = null;
            if(divMac != null || divMac.Any())
            rawMac = new String(divMac);

            if (String.IsNullOrEmpty(rawMac)) {
                throw new Exception("Mac não informado.");
            }
            if (rawMac.Length != 12){
                throw new Exception("Formato de mac inválido.");
            }


            string cmdChangeMac = $"Set-NetAdapterAdvancedProperty -Name \"{_interfaceName.Trim()}\" -RegistryKeyword \"NetworkAddress\" -RegistryValue \"{rawMac}\"";
            string? output = null;

            using (var cmdExecutor = new PowerShellExecutor()) {
                output = await cmdExecutor.ExecuteCommandAsync(cmdChangeMac, new TimeSpan(0,0,4));
            }

            return;
        }

        //################################################################################################
        public void Change(string newMac) {
            //000729553557
            char[]? divMac = newMac?.Where(cm => Char.IsDigit(cm))?.ToArray();
            string? rawMac = null;
            if (divMac != null || divMac.Any())
                rawMac = Convert.ToString(divMac);

            if (String.IsNullOrEmpty(rawMac)) {
                throw new Exception("Mac não informado.");
            }
            if (rawMac.Length != 12) {
                throw new Exception("Formato de mac inválido.");
            }


            string cmdChangeMac = $"Set-NetAdapterAdvancedProperty -Name \"{_interfaceName.Trim()}\" -RegistryKeyword \"NetworkAddress\" -RegistryValue \"{rawMac}\"";
            string? output = null;

            using (var cmdExecutor = new PowerShellExecutor()) {
                output = cmdExecutor.ExecuteCommand(cmdChangeMac, new TimeSpan(300000));
            }

            return;
        }

        //################################################################################################
        public async Task ResetAsync() {
            string output = null;

            string cmd1 = $"Reset-NetAdapterAdvancedProperty -Name \"{_interfaceName.Trim()}\" -DisplayName \"*\"";

            using (var cmdExecutor = new PowerShellExecutor()) {
                output = await cmdExecutor.ExecuteCommandAsync(cmd1, 500000);
            }

            return;
        }

        //################################################################################################
        public void Reset() {
            string? output = null;

            string cmd1 = $"Reset-NetAdapterAdvancedProperty -Name \"{_interfaceName.Trim()}\" -DisplayName \"*\"";

            using (var cmdExecutor = new PowerShellExecutor()) {
                output = cmdExecutor.ExecuteCommand(cmd1, 100000);
            }

            return;
        }
    }
}
