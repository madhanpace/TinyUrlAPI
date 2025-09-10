namespace TinyUrlAPI.Models
{
    public class TinyUrlAddDto
    {
        public string OriginalURL { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
    }
}
