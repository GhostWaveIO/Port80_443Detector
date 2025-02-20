using System.Net.Sockets;
using System.Text;

namespace NetworkServices.Telnet {
    public class TelnetCommands {
        private string _host;
        private int _port;
        private string _username;
        private string _password;

        public TelnetCommands(string host, int port, string user, string password) {
            _host = host;
            _port = port;
            _username = user;
            _password = password;
        }
        public TelnetCommands(string host, string user, string password) {
            _host = host;
            _port = 23;
            _username = user;
            _password = password;
        }



        //##########################################################################################
        public async Task ExecuteAsync(List<Tuple<string, TimeSpan?>> commands) {


            string ip = _host;
            int port = _port; // Porta padrão para Telnet
            string username = _username;
            string password = _password;

            try {
                using (TcpClient tcpClient = new TcpClient(ip, port))
                using (NetworkStream networkStream = tcpClient.GetStream())
                using (StreamReader reader = new StreamReader(networkStream, Encoding.ASCII))
                using (StreamWriter writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true }) {
                    // Aguarda o prompt inicial do Telnet
                    await Task.Delay(1000);

                    // Envia o nome de usuário
                    await writer.WriteLineAsync(username);
                    await writer.FlushAsync();

                    // Aguarda o prompt de senha
                    await Task.Delay(1000);

                    // Envia a senha
                    await writer.WriteLineAsync(password);
                    await writer.FlushAsync();

                    // Aguarda a autenticação
                    await Task.Delay(2000);

                    foreach (Tuple<string, TimeSpan?> cmd in commands) {
                        // Envia o comando
                        await writer.WriteLineAsync(cmd.Item1);
                        await Task.Delay(200);
                        await writer.FlushAsync();

                        await Task.Delay(cmd.Item2 ?? new TimeSpan(200000));

                        // Lê a resposta do comando
                        string response = await ReadResponseAsync(networkStream, reader);
                        Console.WriteLine("Resposta do comando:");
                        Console.WriteLine(response);
                    }


                    Console.WriteLine("--FIM--");
                    Console.WriteLine("");
                    Console.WriteLine("");
                }
            } catch (Exception ex) {
                Console.WriteLine($"Erro: {ex.Message}");
            }
        }

        private static async Task<string> ReadResponseAsync(NetworkStream networkStream, StreamReader reader) {
            StringBuilder sb = new StringBuilder();

            while (networkStream.DataAvailable && !reader.EndOfStream) {
                if (networkStream.DataAvailable) {
                    string line = await reader.ReadLineAsync();
                    if (line != null) {
                        sb.AppendLine(line);
                    }
                } else {
                    await Task.Delay(100); // Aguarda um pouco antes de verificar novamente
                }
            }

            return sb.ToString();
        }
    }
}
