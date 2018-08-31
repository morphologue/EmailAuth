namespace EmailAuth.Models
{
    // An element of the ProxyDestinations appsettings section
    public class ProxyDestination
    {
        public string Protocol { get; set; }
        public bool Authenticated { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }
}
