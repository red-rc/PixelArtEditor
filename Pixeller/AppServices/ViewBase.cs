using Pixeller.AppServices.Shell;
using Pixeller.Models.Canvas;
using ReactiveUI.SourceGenerators;
using System.Threading.Tasks;

namespace Pixeller.AppServices;

public abstract partial class ViewBase : ReactiveObject
{
    [Reactive] public partial bool IsLoading { get; set; } = false;

    public async Task<PixelModel?> RunLoadAsync(Task<PixelModel?> load)
    {
        IsLoading = true;

        try
        {
            var minVisible = Task.Delay(300);
            var model = await load;
            await minVisible;

            if (model is null)
            {
                await ActionService.ShowError(LocalizationService.Get("ImageLoadingError"));
                return null;
            }

            return model;
        }
        finally { IsLoading = false; }
    }
}