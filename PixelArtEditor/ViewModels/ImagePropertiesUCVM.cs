using Avalonia;
using ReactiveUI;

namespace PixelArtEditor.ViewModels;

public class ImagePropertiesUCVM : ReactiveObject
{
    private float _imageProportion = 0f;
    private bool _isUpdating = false;

    private short _selectedWidth = 32;
    public short SelectedWidth
    {
        get => _selectedWidth;
        set
        {
            if (_isUpdating) 
            { 
                this.RaiseAndSetIfChanged(ref _selectedWidth, value); 
                return; 
            }

            _isUpdating = true;

            if (EnableProportion && _imageProportion != 0f)
            {
                var newHeight = (short)(value / _imageProportion);
                if (newHeight > 0) SelectedHeight = newHeight;
            }

            this.RaiseAndSetIfChanged(ref _selectedWidth, value);
            _isUpdating = false;
        }
    }

    private short _selectedHeight = 32;
    public short SelectedHeight
    {
        get => _selectedHeight;
        set
        {
            if (_isUpdating) 
            { 
                this.RaiseAndSetIfChanged(ref _selectedHeight, value); 
                return; 
            }

            _isUpdating = true;

            if (EnableProportion && _imageProportion != 0f)
            {
                var newWidth = (short)(value * _imageProportion);
                if (newWidth > 0) SelectedWidth = newWidth;
            }

            this.RaiseAndSetIfChanged(ref _selectedHeight, value);
            _isUpdating = false;
        }
    }

    private bool _enableProportion = false;
    public bool EnableProportion
    {
        get => _enableProportion;
        set
        {
            if (SelectedWidth == 0 || SelectedHeight == 0) return;
            else if (value) _imageProportion = (float)SelectedWidth / SelectedHeight;
            this.RaiseAndSetIfChanged(ref _enableProportion, value);
        }
    }

    private Vector _selectedDpi = new(96, 96);
    public Vector SelectedDPI
    {
        get => _selectedDpi;
        set => this.RaiseAndSetIfChanged(ref _selectedDpi, value);
    }

    public double SelectedDpiX
    {
        get => _selectedDpi.X;
        set
        {
            var newVec = new Vector(value, _selectedDpi.Y);
            this.RaiseAndSetIfChanged(ref _selectedDpi, newVec);
            this.RaisePropertyChanged(nameof(SelectedDpiX));
            this.RaisePropertyChanged(nameof(SelectedDPI));
        }
    }

    public double SelectedDpiY
    {
        get => _selectedDpi.Y;
        set
        {
            var newVec = new Vector(_selectedDpi.X, value);
            this.RaiseAndSetIfChanged(ref _selectedDpi, newVec);
            this.RaisePropertyChanged(nameof(SelectedDpiY));
            this.RaisePropertyChanged(nameof(SelectedDPI));
        }
    }
}
