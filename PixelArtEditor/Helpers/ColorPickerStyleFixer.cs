using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;

namespace PixelArtEditor.Helpers;

public class ColorPickerStyleFixer
{
    public static void Fix(ColorPicker colorPicker)
    {
        colorPicker.TemplateApplied += (_, _) =>
        {
            var dropDownButton = colorPicker.GetVisualDescendants().OfType<DropDownButton>().FirstOrDefault();

            if (dropDownButton is not null)
                FixDropDownButtonClip(dropDownButton);
        };
    }
    public static void FixDropDownButtonClip(DropDownButton dropDownButton)
    {
        dropDownButton.TemplateApplied += (_, _) =>
        {
            var border = dropDownButton.GetVisualDescendants()
                .OfType<Border>()
                .FirstOrDefault(b => b.Name == "RootBorder");

            border?.ClipToBounds = false;
        };
    }
}
