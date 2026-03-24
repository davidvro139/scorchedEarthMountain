using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using Win32 = Microsoft.Win32;

namespace ScorchedEarthMountain.App;

public partial class MainWindow : Window
{
    private BitmapDocument? _loadedBitmapDocument;
    private MountainDocument? _loadedMountainDocument;
    private BitmapSource? _originalImageSource;
    private BitmapSource? _preparedImageSource;
    private RgbColor _selectedBackgroundColor = new(0, 0, 0);
    private string? _imageInputPath;
    private string? _mountainInputPath;

    public MainWindow()
    {
        InitializeComponent();
        UpdateBackgroundColorUi(_selectedBackgroundColor);
    }

    private void BrowseImageInput_Click(object sender, RoutedEventArgs e)
    {
        Win32.OpenFileDialog dialog = new()
        {
            Filter = "Images|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff|All files|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _imageInputPath = dialog.FileName;
            _originalImageSource = BitmapPreviewFactory.FromFile(_imageInputPath);

            ImageInputPathTextBox.Text = _imageInputPath;
            MountainOutputPathTextBox.Text = BuildDefaultOutputPath(_imageInputPath, ".MTN");
            RebuildImagePreparation();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
        }
    }

    private void ChooseMountainOutput_Click(object sender, RoutedEventArgs e)
    {
        Win32.SaveFileDialog dialog = new()
        {
            Filter = "Scorched Earth Mountain|*.MTN",
            FileName = _imageInputPath is null ? "OUTPUT.MTN" : Path.GetFileNameWithoutExtension(_imageInputPath).ToUpperInvariant() + ".MTN"
        };

        if (dialog.ShowDialog() == true)
        {
            MountainOutputPathTextBox.Text = dialog.FileName;
        }
    }

    private void ConvertToMountain_Click(object sender, RoutedEventArgs e)
    {
        if (_loadedMountainDocument is null)
        {
            StatusTextBlock.Text = "Load an image before converting.";
            return;
        }

        string outputPath = ResolveOutputPath(MountainOutputPathTextBox.Text, _imageInputPath, ".MTN", upperCaseName: true);
        File.WriteAllBytes(outputPath, _loadedMountainDocument.ToBytes());
        MountainOutputPathTextBox.Text = outputPath;
        StatusTextBlock.Text = $"Wrote MTN file to {outputPath}";
    }

    private void BrowseMountainInput_Click(object sender, RoutedEventArgs e)
    {
        Win32.OpenFileDialog dialog = new()
        {
            Filter = "Scorched Earth Mountain|*.mtn|All files|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _mountainInputPath = dialog.FileName;
            _loadedMountainDocument = MountainDocument.FromBytes(File.ReadAllBytes(_mountainInputPath), Path.GetFileNameWithoutExtension(_mountainInputPath));
            _loadedBitmapDocument = _loadedMountainDocument.ToBitmapDocument();

            MountainInputPathTextBox.Text = _mountainInputPath;
            BitmapOutputPathTextBox.Text = BuildDefaultOutputPath(_mountainInputPath, ".bmp");
            BitmapSource preview = BitmapPreviewFactory.FromBytes(_loadedBitmapDocument.ToBytes());
            MountainModeInputPreview.Source = preview;
            MountainModeOutputPreview.Source = preview;
            MountainModeInfoTextBlock.Text = $"Loaded {_loadedMountainDocument.Width} x {_loadedMountainDocument.Height} mountain with {_loadedMountainDocument.Palette.Length} palette entries.";
            StatusTextBlock.Text = "MTN file loaded and decoded preview generated.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
        }
    }

    private void ChooseBitmapOutput_Click(object sender, RoutedEventArgs e)
    {
        Win32.SaveFileDialog dialog = new()
        {
            Filter = "Bitmap|*.bmp",
            FileName = _mountainInputPath is null ? "OUTPUT.bmp" : Path.GetFileNameWithoutExtension(_mountainInputPath) + ".bmp"
        };

        if (dialog.ShowDialog() == true)
        {
            BitmapOutputPathTextBox.Text = dialog.FileName;
        }
    }

    private void ConvertToBitmap_Click(object sender, RoutedEventArgs e)
    {
        if (_loadedBitmapDocument is null || _mountainInputPath is null)
        {
            StatusTextBlock.Text = "Load an MTN file before converting.";
            return;
        }

        string outputPath = ResolveOutputPath(BitmapOutputPathTextBox.Text, _mountainInputPath, ".bmp", upperCaseName: false);
        byte[] bmpBytes = _loadedBitmapDocument.ToBytes();
        File.WriteAllBytes(outputPath, bmpBytes);
        BitmapOutputPathTextBox.Text = outputPath;
        MountainModeOutputPreview.Source = BitmapPreviewFactory.FromBytes(bmpBytes);
        StatusTextBlock.Text = $"Wrote bitmap to {outputPath}";
    }

    private static string BuildDefaultOutputPath(string inputPath, string extension)
    {
        string directory = Path.GetDirectoryName(inputPath)!;
        string fileName = Path.GetFileNameWithoutExtension(inputPath);
        if (extension.Equals(".MTN", StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName.ToUpperInvariant();
        }

        return Path.Combine(directory, fileName + extension);
    }

    private static string ResolveOutputPath(string currentValue, string? inputPath, string extension, bool upperCaseName)
    {
        if (!string.IsNullOrWhiteSpace(currentValue))
        {
            return currentValue;
        }

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new InvalidOperationException("No input file selected.");
        }

        string directory = Path.GetDirectoryName(inputPath)!;
        string fileName = Path.GetFileNameWithoutExtension(inputPath);
        if (upperCaseName)
        {
            fileName = fileName.ToUpperInvariant();
        }

        return Path.Combine(directory, fileName + extension);
    }

    private void BackgroundSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (BackgroundToleranceValueTextBlock is null || BackgroundToleranceSlider is null)
        {
            return;
        }

        if (_originalImageSource is null)
        {
            BackgroundToleranceValueTextBlock.Text = ((int)BackgroundToleranceSlider.Value).ToString();
            return;
        }

        RebuildImagePreparation();
    }

    private void ResetBackgroundPreview_Click(object sender, RoutedEventArgs e)
    {
        RemoveBackgroundCheckBox.IsChecked = false;
        UseCustomBackgroundColorCheckBox.IsChecked = false;
        BackgroundToleranceSlider.Value = 24;
        if (_originalImageSource is not null)
        {
            _selectedBackgroundColor = BitmapDocument.SampleTopLeftColor(_originalImageSource);
            UpdateBackgroundColorUi(_selectedBackgroundColor);
            RebuildImagePreparation();
        }
    }

    private void PickBackgroundColor_Click(object sender, RoutedEventArgs e)
    {
        Forms.ColorDialog dialog = new()
        {
            AllowFullOpen = true,
            FullOpen = true,
            Color = System.Drawing.Color.FromArgb(_selectedBackgroundColor.R, _selectedBackgroundColor.G, _selectedBackgroundColor.B)
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        _selectedBackgroundColor = new RgbColor(dialog.Color.R, dialog.Color.G, dialog.Color.B);
        UseCustomBackgroundColorCheckBox.IsChecked = true;
        UpdateBackgroundColorUi(_selectedBackgroundColor);

        if (_originalImageSource is not null)
        {
            RebuildImagePreparation();
        }
    }

    private void RebuildImagePreparation()
    {
        if (_originalImageSource is null ||
            string.IsNullOrWhiteSpace(_imageInputPath) ||
            BackgroundToleranceSlider is null ||
            BackgroundToleranceValueTextBlock is null ||
            RemoveBackgroundCheckBox is null ||
            UseCustomBackgroundColorCheckBox is null ||
            SourceImagePreview is null ||
            ImageModeOutputPreview is null ||
            ImageModeInfoTextBlock is null ||
            StatusTextBlock is null)
        {
            return;
        }

        byte tolerance = (byte)Math.Round(BackgroundToleranceSlider.Value);
        BackgroundToleranceValueTextBlock.Text = tolerance.ToString();
        RgbColor topLeftColor = BitmapDocument.SampleTopLeftColor(_originalImageSource);
        RgbColor activeBackgroundColor = UseCustomBackgroundColorCheckBox.IsChecked == true ? _selectedBackgroundColor : topLeftColor;

        if (UseCustomBackgroundColorCheckBox.IsChecked != true)
        {
            UpdateBackgroundColorUi(activeBackgroundColor);
        }

        int removedPixelCount = 0;
        BitmapSource preparedPreview = _originalImageSource;

        if (RemoveBackgroundCheckBox.IsChecked == true)
        {
            BackgroundRemovalResult backgroundRemoval = BackgroundRemovalProcessor.RemoveBackground(_originalImageSource, tolerance, activeBackgroundColor);
            _preparedImageSource = backgroundRemoval.ExportImage;
            preparedPreview = backgroundRemoval.PreviewImage;
            removedPixelCount = backgroundRemoval.RemovedPixelCount;
        }
        else
        {
            _preparedImageSource = _originalImageSource;
        }

        _loadedBitmapDocument = BitmapDocument.FromBitmapSource(_preparedImageSource);
        _loadedMountainDocument = MountainDocument.FromBitmap(_loadedBitmapDocument, Path.GetFileNameWithoutExtension(_imageInputPath));

        SourceImagePreview.Source = preparedPreview;
        ImageModeOutputPreview.Source = BitmapPreviewFactory.FromBytes(_loadedBitmapDocument.ToBytes());

        string colorMode = UseCustomBackgroundColorCheckBox.IsChecked == true
            ? $"Manual detection color {FormatColor(activeBackgroundColor)} selected."
            : $"Using top-left detection color {FormatColor(activeBackgroundColor)}.";

        string backgroundMode = RemoveBackgroundCheckBox.IsChecked == true
            ? $"Background removal enabled with tolerance {tolerance}. Removed {removedPixelCount} pixels. Export sky is forced to white. {colorMode}"
            : "Background removal disabled.";

        ImageModeInfoTextBlock.Text = $"Loaded {_loadedBitmapDocument.Width} x {_loadedBitmapDocument.Height} image. {backgroundMode} Preview shows the prepared image and generated 16-color bitmap used for MTN export.";
        StatusTextBlock.Text = "Prepared image and MTN preview refreshed.";
    }

    private void UpdateBackgroundColorUi(RgbColor color)
    {
        _selectedBackgroundColor = color;
        if (BackgroundColorSwatch is null || BackgroundColorValueTextBlock is null)
        {
            return;
        }

        BackgroundColorSwatch.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
        BackgroundColorValueTextBlock.Text = FormatColor(color);
    }

    private static string FormatColor(RgbColor color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
