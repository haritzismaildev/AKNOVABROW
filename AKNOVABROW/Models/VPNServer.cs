namespace AKNOVABROW.Models
{
    public class VPNServer
    {
        public string Country { get; set; } = "";
        public string Flag { get; set; } = "";
        public string ProxyHost { get; set; } = "";
        public int ProxyPort { get; set; }
        public string Speed { get; set; } = "";

        public override string ToString() => $"{Flag} {Country} ({Speed})";
    }
}
