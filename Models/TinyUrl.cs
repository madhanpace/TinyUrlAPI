namespace TinyUrlAPI.Models
{
    public class TinyUrl
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ShortURL { get; set; } = string.Empty;
        public string OriginalURL { get; set; } = string.Empty;
        public int TotalClicks { get; set; }
        public bool IsPrivate { get; set; }
    }

}
