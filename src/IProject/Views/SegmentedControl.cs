namespace IProject.Views;

/// <summary>
/// A row of mutually exclusive options. <see cref="SelectedIndex"/> binds two-way;
/// <see cref="OptionColors"/> optionally gives each option its own selected color.
/// </summary>
public class SegmentedControl : Grid
{
    public static readonly BindableProperty OptionsProperty =
        BindableProperty.Create(nameof(Options), typeof(IList<string>), typeof(SegmentedControl),
            propertyChanged: (b, _, _) => ((SegmentedControl)b).Build());

    public static readonly BindableProperty OptionColorsProperty =
        BindableProperty.Create(nameof(OptionColors), typeof(IList<Color>), typeof(SegmentedControl),
            propertyChanged: (b, _, _) => ((SegmentedControl)b).UpdateSelection());

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(SegmentedControl), 0, BindingMode.TwoWay,
            propertyChanged: (b, _, _) => ((SegmentedControl)b).UpdateSelection());

    static readonly Color Unselected = Color.FromArgb("#2A2A2A");

    readonly List<(Border Chip, Label Text)> segments = [];

    public SegmentedControl()
    {
        ColumnSpacing = 6;
    }

    public IList<string>? Options
    {
        get => (IList<string>?)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public IList<Color>? OptionColors
    {
        get => (IList<Color>?)GetValue(OptionColorsProperty);
        set => SetValue(OptionColorsProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    void Build()
    {
        Children.Clear();
        ColumnDefinitions.Clear();
        segments.Clear();

        var options = Options ?? [];
        for (var i = 0; i < options.Count; i++)
        {
            var index = i;
            var text = new Label
            {
                Text = options[i],
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };
            var chip = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                HeightRequest = 38,
                Content = text,
            };
            chip.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SelectedIndex = index) });

            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            this.Add(chip, i);
            segments.Add((chip, text));
        }
        UpdateSelection();
    }

    void UpdateSelection()
    {
        for (var i = 0; i < segments.Count; i++)
        {
            var selected = i == SelectedIndex;
            var color = OptionColors is { } colors && i < colors.Count ? colors[i] : Palette.Accent;
            segments[i].Chip.BackgroundColor = selected ? color : Unselected;
            segments[i].Text.TextColor = selected ? Colors.White : Palette.TextSecondary;
            segments[i].Text.FontFamily = selected ? "OpenSansSemibold" : "OpenSansRegular";
        }
    }
}
