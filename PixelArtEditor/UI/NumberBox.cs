using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace PixelArtEditor.UI
{
    public class NumberBox : NumericUpDown
    {
        protected override Type StyleKeyOverride => typeof(NumericUpDown);
        private bool _suppressLastValidUpdate = false;
        private decimal _lastValidValue = 0m;

        public NumberBox()
        {
            ShowButtonSpinner = false;

            if (Value.HasValue) _lastValidValue = Value.Value;

            this.GetObservable(ValueProperty).Subscribe(val =>
            {
                if (_suppressLastValidUpdate) return;
                if (val.HasValue) _lastValidValue = val.Value;
            });
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            var textBox = e.NameScope.Find<TextBox>("PART_TextBox");
            if (textBox is not null)
            {
                textBox.TextChanged += (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        _suppressLastValidUpdate = true;
                        Value = 0m;
                        _suppressLastValidUpdate = false;

                        Dispatcher.UIThread.Post(() =>
                        {
                            try { textBox.SelectAll(); } catch { }
                        }, DispatcherPriority.Background);
                    }
                };
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (!Value.HasValue || Value == 0) Value = _lastValidValue;
        }
    }
}