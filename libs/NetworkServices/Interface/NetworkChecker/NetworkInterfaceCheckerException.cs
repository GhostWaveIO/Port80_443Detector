namespace NetworkServices.Interface.NetworkChecker {
    public class NetworkInterfaceCheckerException : Exception {

        public string InterfaceName { get; private set; }

        public NetworkInterfaceCheckerException(string message, string interfaceName) : base(message) {
            InterfaceName = interfaceName;
        }
        public NetworkInterfaceCheckerException(string message, string interfaceName, Exception innerException) : base(message, innerException) {
            InterfaceName = interfaceName;
        }
    }
}
