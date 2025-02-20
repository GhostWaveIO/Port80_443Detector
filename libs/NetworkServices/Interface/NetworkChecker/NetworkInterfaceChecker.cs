using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;

namespace NetworkServices.Interface.NetworkChecker {
    public class NetworkInterfaceChecker {
        private Uri _uri { get; set; }

        public NetworkInterfaceChecker(string url) {
            if(!url.Trim().StartsWith("http"))
                _uri = new Uri($"https://{url}");
            else
                _uri = new Uri(url);

        }


        #region Native
        /// <summary>
        /// Analisa se a interface especificada possui internet.
        /// Esta análise é feita usando o algoritmo nativo do Windows
        /// </summary>
        /// <param name="interfaceName">Nome da interface a ser verificada</param>
        /// <returns>Retorna true caso seja detectado internet na interface especificada</returns>
        public async Task<bool> NativeChecker(string interfaceName) {
            bool res = false;
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach(var netInterface in networkInterfaces) {

                if(!(netInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.Unknown
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.Wman
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.Wwanpp2
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.Wwanpp
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.AsymmetricDsl
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.Atm
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.BasicIsdn
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.GenericModem
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.TokenRing
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.IPOverAtm
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.Slip) ||
                        (netInterface.Description.Contains("VPN", StringComparison.InvariantCultureIgnoreCase) || netInterface.Description.Contains("Radmin", StringComparison.InvariantCultureIgnoreCase) || netInterface.Description.Contains("Hamachi", StringComparison.InvariantCultureIgnoreCase) || netInterface.Description.Contains("Virtual", StringComparison.InvariantCultureIgnoreCase) || netInterface.Description.Contains("Bluetooth", StringComparison.InvariantCultureIgnoreCase))
                    )
                    continue;

                if(netInterface.OperationalStatus == OperationalStatus.Up && netInterface.Name.Equals(interfaceName, StringComparison.InvariantCultureIgnoreCase)) {
                    res = await CheckInternetAccess();
                    break;
                }

            }

            return res;
        }


        /// <summary>
        /// Faz uma análise nativa para verificar se possui internet
        /// </summary>
        /// <returns>Retorna true caso possua internet</returns>
        private async Task<bool> CheckInternetAccess() {
            try {
                using(HttpClient client = new HttpClient()) {
                    // Testa na url especificada
                    HttpResponseMessage response = await client.GetAsync(_uri.AbsoluteUri);
                    return response.IsSuccessStatusCode;
                }
            } catch {
                return false;
            }
        }
        #endregion FIM | Native


        #region Advanced
        /// <summary>
        /// Analisa á partir de uma interface se por ela é acessível a internet.
        /// Essa é uma análise avançada, que faz uma verificação real se realmente possui internet
        /// </summary>
        /// <param name="interfaceName">Nome da inteface</param>
        /// <exception cref="Exception">Retorna uma exceção caso haja erro na conexão com a interface</exception>
        /// <exception cref="NetworkInterfaceBinderException">Retorna o tipo de exceção especificaso quando não foi possível acessar a url informada. (exibe nome da url na exceção gerada)</exception>
        public async Task AdvancedChecker(string interfaceName) {
            //Coleta as interfaces diponívels
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.Name.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));

            if(networkInterface == null)
                throw new Exception($"Interface {interfaceName} não encontrada!");

            IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
            UnicastIPAddressInformation? ipv4Address = ipProperties.UnicastAddresses
                .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                                      !IPAddress.IsLoopback(ip.Address));

            if(ipv4Address == null)
                throw new Exception($"Nenhum endereço de ip encontrado na interface {interfaceName}");

            var localIpAddress = ipv4Address.Address;

            // Cria um SocketsHttpHandler e faz um bind para o IP especificado
            var handler = new SocketsHttpHandler {
                ConnectCallback = async (context, token) => {
                    var endpoint = new DnsEndPoint(_uri.Host, 443); // Usa porta 443 para HTTPS
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Executa um bind através do IP local
                    socket.Bind(new IPEndPoint(localIpAddress, 0));

                    await socket.ConnectAsync(endpoint, token);

                    // Atualiza o socket para SSL
                    var sslStream = new SslStream(new NetworkStream(socket, ownsSocket: true), false, (sender, certificate, chain, sslPolicyErrors) => true);
                    await sslStream.AuthenticateAsClientAsync(_uri.Host);

                    return sslStream;
                }
            };

            using(var client = new HttpClient(handler)) {
                try {
                    HttpResponseMessage response = await client.GetAsync(_uri.AbsoluteUri, new CancellationTokenSource(TimeSpan.FromSeconds(4)).Token);
                    //string responseBody = await response.Content.ReadAsStringAsync();
                    if(!response.IsSuccessStatusCode) {
                        throw new NetworkInterfaceCheckerException($"Falha ao acessar o domínio \"{_uri.Host}\" através da interface {interfaceName}", interfaceName);
                    }
                } catch(Exception err) {
                    throw new NetworkInterfaceCheckerException($"Falha ao acessar o domínio \"{_uri.Host}\" através da interface {interfaceName}.\nErro: {err.Message}", interfaceName, err);
                }
            }
        }
        #endregion FIM | Advanced

        #region Main Interface
        public static string GetPrimaryInternetInterface() {
            string res = null;

            // Obtém todas as interfaces de rede ativas com endereço IP
            var activeInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                                                  .Where(i => i.OperationalStatus == OperationalStatus.Up && i.GetIPProperties().UnicastAddresses.Any());

            // Encontra a interface com a menor métrica
            NetworkInterface? primaryInterface = activeInterfaces.OrderByDescending(i => i.GetIPProperties().UnicastAddresses.First().IPv4Mask.Address).FirstOrDefault();

            if(primaryInterface != null) {
                res = primaryInterface.Name;
            }

            return res;
        }
        #endregion FIM | Main Interface
    }
}
