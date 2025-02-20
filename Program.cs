using NetworkServices.Interface;

namespace Port80_443Detector {
    class Program {
        static async Task Main(string[] args) {
            if(args.Length != 1) return;

            string ip = args[0];
            if (await PortDetector.ExistsAsync(ip, 80, 500)) {
                Console.WriteLine("porta 80 detectada");
            } else if (await PortDetector.ExistsAsync(ip, 443, 500)) {
                Console.WriteLine("porta 443 detectada");
            } else {
                Console.WriteLine("porta não detectada");
            }

            Console.ReadKey();
        }
    }
}