using TerminalCommands.PromptCommand;

namespace NetworkServices.Interface {

    /// <summary>
    /// Responsável por alterar o ip da interface
    /// </summary>
    public class IpChanger {
        public async Task ChangeStaticIpv4Async(string interfaceName, string ip, string mask, string gateway) {
            using(CmdExecutor executor = new CmdExecutor()) {
                await executor.ExecuteCommandAsync($"netsh interface ipv4 set address \"{interfaceName}\" static {ip} {mask} {gateway} 1", new TimeSpan(0,0,3));
            }
        }
        public void ChangeStaticIpv4(string interfaceName, string ip, string mask, string gateway) {
            using(CmdExecutor executor = new CmdExecutor()) {
                executor.ExecuteCommand($"netsh interface ipv4 set address \"{interfaceName}\" static {ip} {mask} {gateway} 1", new TimeSpan(0,0,3));
            }
        }

        public async Task ChangeDhcpAsync(string interfaceName) {
            using(CmdExecutor executor = new CmdExecutor()) {
                await executor.ExecuteCommandAsync($"netsh interface ip set address \"{interfaceName}\" dhcp", new TimeSpan(0, 0, 1));
            }
        }
    }
}
