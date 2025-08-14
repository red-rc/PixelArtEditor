namespace PixelArtEditor.Other;

public interface ISettings
{
   string Language { get; set; }
   string Theme { get; set; }
   int GridMaxSize { get; set; }
   string GridColor { get; set; }
   bool EnableGrid { get; set; }
   bool EnableAutosave { get; set; }
   int AutosaveFrequency { get; set; }
}