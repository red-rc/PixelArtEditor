using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PixelArtEditor.ViewModels;

namespace PixelArtEditor.UI;

public class Previewer : Control
{
    public static readonly StyledProperty<CreateParams> ParametersProperty =
        AvaloniaProperty.Register<Previewer, CreateParams>(nameof(Parameters));

    public CreateParams Parameters
    {
        get => GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }
    
    static Previewer()
    {
        ParametersProperty.Changed.AddClassHandler<Previewer>((sender, _) => sender.InvalidateVisual());
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (Parameters is not { Width: > 0, Height: > 0 }) return;

        var ratio = (double)Parameters.Width / Parameters.Height;
        if (double.IsInfinity(ratio) || double.IsNaN(ratio)) ratio = 1;

        var rect = 200 / ratio > 200 ? new Rect((200 - 200 * ratio) / 2, 0, 200 * ratio, 200) : 
            new Rect(0, (200 - 200 / ratio) / 2, 200, 200 / ratio);
        
        context.FillRectangle(new SolidColorBrush(Parameters.BackgroundColor), rect);
    }
}