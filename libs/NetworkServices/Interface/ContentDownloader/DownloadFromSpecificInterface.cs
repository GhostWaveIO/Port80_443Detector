using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using NetworkServices.Interface.NetworkChecker;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NetworkServices.Interface.ContentDownloader {
    public class DownloadFromSpecificInterface {
        public async Task<string> DownloadContentAsync(Uri uri, string interfaceName) {
            string result = null;

            // Obter a interface de rede especificada
            NetworkInterface? networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.Name.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));

            if(networkInterface == null)
                throw new Exception($"Interface {interfaceName} não encontrada!");

            IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
            UnicastIPAddressInformation? ipv4Address = ipProperties.UnicastAddresses
                .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address));

            if(ipv4Address == null)
                throw new Exception($"Nenhum endereço IP encontrado na interface {interfaceName}");

            var localIpAddress = ipv4Address.Address;

            // Criar um SocketsHttpHandler com binding para o IP especificado
            var handler = new SocketsHttpHandler {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    try {
                        var endpoint = new DnsEndPoint(uri.Host, 443); // Porta HTTPS
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        // Bind ao endereço IP local
                        socket.Bind(new IPEndPoint(localIpAddress, 0));

                        // Tentar conectar ao endpoint
                        await socket.ConnectAsync(endpoint, cancellationToken);

                        // Atualizar para usar SSL
                        var sslStream = new SslStream(
                            new NetworkStream(socket, ownsSocket: true),
                            false,
                            (sender, certificate, chain, sslPolicyErrors) => true);

                        await sslStream.AuthenticateAsClientAsync(uri.Host);
                        return sslStream;
                    } catch(Exception ex) {
                        throw new Exception($"Erro ao conectar ao endpoint: {ex.Message}", ex);
                    }
                }
            };

            // Configuração do HttpClient com tempo limite ajustado
            using(var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) }) {
                try {
                    using(var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10))) {
                        HttpResponseMessage response = await client.GetAsync(uri, cts.Token);
                        if(!response.IsSuccessStatusCode) {
                            throw new Exception($"Falha ao acessar o domínio \"{uri.Host}\" através da interface {interfaceName}. StatusCode: {response.StatusCode}");
                        }
                        result = await response.Content.ReadAsStringAsync();
                    }
                } catch(OperationCanceledException ex) {
                    throw new Exception($"A operação foi cancelada (possível timeout). Detalhes: {ex.Message}", ex);
                } catch(Exception ex) {
                    throw new Exception($"Erro ao acessar o domínio \"{uri.Host}\" através da interface {interfaceName}. Detalhes: {ex.Message}", ex);
                }
            }

            return result;
        }







        //public async Task<string> DownloadContentAsync(Uri uri, string interfaceName) {
        //    string res = null;

        //    //StringBuilder contentToException = new StringBuilder();

        //    //contentToException.Append($"uri: {JsonSerializer.Serialize(uri)}\n");

        //    //Coleta as interfaces diponívels
        //    NetworkInterface? networkInterface = NetworkInterface.GetAllNetworkInterfaces()
        //        .FirstOrDefault(ni => ni.Name.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));

        //    //contentToException.AppendLine($"Interface Name: {interfaceName}");

        //    if(networkInterface == null)
        //        throw new Exception($"Interface {interfaceName} não encontrada!");

        //    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
        //    UnicastIPAddressInformation? ipv4Address = ipProperties.UnicastAddresses
        //        .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork &&
        //                              !IPAddress.IsLoopback(ip.Address));
        //    string ipAddress = ipv4Address.Address.ToString();
        //    //contentToException.AppendLine($"Ip Address: {JsonSerializer.Serialize(ipAddress)}");

        //    if(ipv4Address == null)
        //        throw new Exception($"Nenhum endereço de ip encontrado na interface {interfaceName}");

        //    var localIpAddress = ipv4Address.Address;

        //    // Cria um SocketsHttpHandler e faz um bind para o IP especificado
        //    var handler = new SocketsHttpHandler {
        //        ConnectCallback = async (context, token) => {
        //            var endpoint = new DnsEndPoint(uri.Host, 443); // Usa porta 443 para HTTPS
        //            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //            //contentToException.Append($"Socket Context: {JsonSerializer.Serialize(context)}");

        //            // Executa um bind através do IP local
        //            socket.Bind(new IPEndPoint(localIpAddress, 0));

        //            await socket.ConnectAsync(endpoint, token);

        //            // Atualiza o socket para SSL
        //            var sslStream = new SslStream(new NetworkStream(socket, ownsSocket: true), false, (sender, certificate, chain, sslPolicyErrors) => true);
        //            await sslStream.AuthenticateAsClientAsync(uri.Host);
        //            return sslStream;
        //        }
        //    };

        //    using(var client = new HttpClient(handler)) {
        //        try {
        //            HttpResponseMessage response = await client.GetAsync(uri.AbsoluteUri, new CancellationTokenSource(TimeSpan.FromSeconds(4)).Token);
        //            if(!response.IsSuccessStatusCode) {
        //                throw new NetworkInterfaceCheckerException($"Falha ao acessar o domínio \"{uri.Host}\" através da interface {interfaceName}", interfaceName);
        //            }
        //            res = await response.Content.ReadAsStringAsync();
        //        } catch(Exception err) {
        //            //using(StreamWriter fileW = new StreamWriter("log connection", true, Encoding.UTF8)) {
        //            //    await fileW.WriteAsync(contentToException.ToString());
        //            //}
        //            throw new NetworkInterfaceCheckerException($"Falha ao acessar o domínio \"{uri.Host}\" através da interface {interfaceName}.\nErro: {err.Message}", interfaceName, err);
        //        }
        //    }

        //    return res;
        //}

    }
}
