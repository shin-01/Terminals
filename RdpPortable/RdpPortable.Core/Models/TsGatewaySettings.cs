namespace RdpPortable.Core.Models
{
    public class TsGatewaySettings
    {
        public GatewayUsageMode UsageMethod { get; set; } = GatewayUsageMode.Never;

        public string HostName { get; set; } = string.Empty;

        public GatewayCredentialSource CredentialSource { get; set; } = GatewayCredentialSource.Ask;

        public bool SeparateLogin { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Domain { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    public enum GatewayUsageMode
    {
        Never = 0,
        Always = 1,
        Detect = 2,
    }

    public enum GatewayCredentialSource
    {
        Ask = 0,
        SameAsTarget = 1,
        Custom = 2,
        SmartCard = 3,
    }
}
