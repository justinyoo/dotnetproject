namespace dotnetproject.Models
{
    public class SpeechToTextModel
    {
        public string? SpeechText { get; set; }
        public string? Text { get; set; }
        public string? Language { get; set; }
        public List<string>? Languages { get; set; }
    }

}
