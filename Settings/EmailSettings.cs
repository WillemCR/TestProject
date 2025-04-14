namespace TestProject.Settings // Pas de namespace eventueel aan
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "localhost"; // Standaardwaarden zijn goed voor fallback
        public int SmtpPort { get; set; } = 25;
        public bool EnableSsl { get; set; } = false;
        public string SenderAddress { get; set; } = "noreply@example.com";
    }
} 