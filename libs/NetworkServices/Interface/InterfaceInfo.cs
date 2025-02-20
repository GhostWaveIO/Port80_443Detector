using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace NetworkServices.Interface {
    public static class InterfaceInfo {
        public static OperationalStatus? CollectOperationalStatus(string interfaceName) {
            NetworkInterface[] interfList = NetworkInterface.GetAllNetworkInterfaces();

            NetworkInterface? interf = null;

            foreach(NetworkInterface i in interfList) {
                if(i.Name == interfaceName) {
                    interf = i;
                    break;
                }
            }

            if(interf == null)
                return null;

            return interf.OperationalStatus;
        }

        public static string CollectMacFromIp(string ipAddress) {
            try {
                Process process = new Process {
                    StartInfo = new ProcessStartInfo {
                        FileName = "arp",
                        Arguments = "-a",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Regex para encontrar o endereço MAC correspondente ao IP fornecido
                string pattern = $@"{Regex.Escape(ipAddress)}\s+([\w-]+)";
                Match match = Regex.Match(output, pattern);

                if(match.Success) {
                    return match.Groups[1].Value.Replace('-', ':');
                }
            } catch(Exception ex) {
                throw new Exception($"Erro ao obter o endereço MAC: {ex.Message}");
            }

            return null;
        }

        public static NetworkInterface? CollectInterface(string interfaceName) {
            NetworkInterface? res = null;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach(NetworkInterface adapter in adapters) {
                if(adapter.Name == interfaceName) {
                    res = adapter;
                    break;
                }
            }

            return res;
        }

        #region Ip

        public static GatewayIPAddressInformationCollection? GetGatewaysIntarface(string interfaceName) {
            GatewayIPAddressInformationCollection res = null;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach(NetworkInterface adapter in adapters) {
                if(adapter.Name != interfaceName)
                    continue;
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                res = adapterProperties.GatewayAddresses;

                break;
                //if (addresses.Count > 0) {
                //    string gateway =
                //    Console.WriteLine(adapter.Description);
                //    foreach (GatewayIPAddressInformation address in addresses) {
                //        Console.WriteLine("  Gateway Address ......................... : {0}",
                //        address.Address.ToString());
                //    }
                //    Console.WriteLine();
                //}
            }

            return res;
        }

        //######################################################################################
        public static async Task<string> CollectFirstIpAsync(string[] ips, int[] ports) {
            string res = null;

            foreach(string i in ips) {
                if(string.IsNullOrWhiteSpace(i))
                    continue;
                if(await PortExistsAsync(i, ports)) {
                    res = i;
                    break;
                }
            }

            return res;
        }

        //######################################################################################
        public static async Task<string> CollectFirstIpAsync(string[] ips, int port) {
            string res = null;

            foreach(string i in ips) {
                if(string.IsNullOrWhiteSpace(i))
                    continue;
                if(await PortExistsAsync(i, port)) {
                    res = i;
                    break;
                }
            }

            return res;
        }

        #endregion


        #region Mac

        //##################################################################
        public static async Task<bool> PortExistsAsync(string ip, int[] ports, int timeout = 5000) {
            bool res = false;
            foreach(int p in ports) {
                if(await PortExistsAsync(ip, p, timeout)) {
                    res = true;
                }
            }
            return res;
        }

        //##################################################################
        public static async Task<bool> PortExistsAsync(string ip, int port, int timeout = 5000) {
            try {
                using(TcpClient tcpClient = new TcpClient()) {
                    var connectTask = tcpClient.ConnectAsync(ip, port);
                    var timeoutTask = Task.Delay(timeout);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if(completedTask == timeoutTask) {
                        // Timeout
                        return false;
                    }

                    // Conexão bem-sucedida
                    return tcpClient.Connected;
                }
            } catch {
                // Falha na conexão
                return false;
            }
        }

        //##################################################################
        public static bool PortExists(string ip, int[] ports, int timeout = 5000) {
            bool res = false;
            foreach(int p in ports) {
                if(PortExists(ip, p, timeout)) {
                    res = true;
                }
            }
            return res;
        }

        //##################################################################
        public static bool PortExists(string ip, int port, int timeout = 5000) {
            try {
                using(TcpClient tcpClient = new TcpClient()) {
                    Task connectTask = tcpClient.ConnectAsync(ip, port);
                    Task timeoutTask = Task.Delay(timeout);
                    Task? completedTask = Task.WhenAny(connectTask, timeoutTask).GetAwaiter().GetResult();

                    if(completedTask == timeoutTask) {
                        // Timeout
                        return false;
                    }

                    // Conexão bem-sucedida
                    return tcpClient.Connected;
                }
            } catch {
                // Falha na conexão
                return false;
            }
        }

        ////##################################################################
        //public static async Task<int?> FirstPortAsync(string ip, int[] ports, int timeout = 5000) {
        //    int? res = null;
        //    foreach (int p in ports) {
        //        if(await ExistsAsync(ip, p, timeout)) {
        //            res = p;
        //            break;
        //        }
        //    }

        //    return res;
        //}

        //##################################################################
        public static async Task<int?> FirstPortOrDefaultAsync(string ip, int[] ports, int timeout = 5000) {
            int? res = null;
            foreach(int p in ports) {
                if(await PortExistsAsync(ip, p, timeout)) {
                    res = p;
                    break;
                }
            }

            return res;
        }

        #endregion
    }
}
