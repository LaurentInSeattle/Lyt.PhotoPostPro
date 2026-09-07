namespace Lyt.PhotoPostPro.Workflow.Tools.Watermarking;

using static Lyt.PhotoPostPro.Workflow.Tools.ToolsStatics;

public sealed partial class WatermarkEditViewModel :
    ViewModel<WatermarkEditView>, IEditor
{
    private readonly PhotoPostProModel model;
    private readonly EditorViewModel editorViewModel;
    /* 

    public string FriendlyName { get; set; } = string.Empty;

    public string FontFamily { get; set; } = "Arial";

    public int FontSize { get; set; } = 142;

    public PppFontStyle PppFontStyle { get; set; } = PppFontStyle.Bold;

    public string Text { get; set; } = "... ... Copyright © 2026 Laurent. All rights reserved. ... ...";

    public uint HexColorArgb { get; set; } = 0x80FFFFFF;

    */

    private int fontSize = 26;

    [ObservableProperty]
    public partial string FriendlyName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool FriendlyNameIsDisabled { get; set; }

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FontSizeString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PppFontStyle PppFontStyle { get; set; }

    [ObservableProperty]
    public partial List<FontFamily> SupportedFontFamilies { get; set; }

    [ObservableProperty]
    public partial int SelectedFontFamilyIndex { get; set; }

    [ObservableProperty]
    public partial Color ForegroundColor { get; set; } = Color.FromUInt32(0xFF_FF_FF_FF); // Pure White 

    [ObservableProperty]
    public partial ObservableCollection<string> SupportedFontStyles { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedFontStylesIndex { get; set; }

    [ObservableProperty]
    public partial string ValidationMessage { get; set; }

    public WatermarkEditViewModel(PhotoPostProModel model, EditorViewModel editorViewModel)
    {
        this.model = model;
        this.editorViewModel = editorViewModel;

        this.SetDefaults();
        this.SupportedFontFamilies = FontFamilies();
        this.ValidationMessage = string.Empty;
    }

    private void SetDefaults()
    {
        this.ForegroundColor = Color.FromUInt32(0xFF_FF_F8_F0);
        this.FriendlyName = Watermark.DefaultName;
        this.Text = "Coyrighted Work";
        this.fontSize = 140;
        this.FontSizeString = this.fontSize.ToString("D");
    }

    private void PopulateLocalizedComboBoxes()
    {
        // Same for the supported font styles
        var list = new List<string>();
        foreach (string item in SupportedFontStyleText)
        {
            list.Add(this.Localize(item));
        }

        this.SupportedFontStyles = new(list);

        // Enforce property changed
        this.SelectedFontStylesIndex = 0;
        this.SelectedFontStylesIndex = 2;
    }

    // Populate the form with defaults 
    public void BeginAdd()
    {
        this.FriendlyNameIsDisabled = false;
        this.PopulateLocalizedComboBoxes();
        this.SetDefaults();
    }

    // Populate the form with provided editable 
    public void BeginEdit(IEditable editable)
    {
        if (editable is not Watermark watermark)
        {
            return;
        }

        this.FriendlyNameIsDisabled = true;
        this.PopulateLocalizedComboBoxes();

        this.FriendlyName = watermark.FriendlyName;
        this.Text = watermark.Text;

        this.ForegroundColor = Color.FromUInt32(watermark.HexColorArgb);
        this.FontSizeString = watermark.FontSize.ToString("D");

        this.SelectedFontFamilyIndex = -1;
        for (int i = 0; i < this.SupportedFontFamilies.Count; ++i)
        {
            if (watermark.FontFamily.Equals(this.SupportedFontFamilies[i].Name, StringComparison.InvariantCultureIgnoreCase))
            {
                this.SelectedFontFamilyIndex = i;
                break;
            }
        }

        this.SelectedFontStylesIndex = -1;
        for (int i = 0; i < SupportedFontStyleValues.Count; ++i)
        {
            if (watermark.PppFontStyle == SupportedFontStyleValues[i])
            {
                this.SelectedFontStylesIndex = i;
                break;
            }
        }
    }

    partial void OnFriendlyNameChanged(string value) => this.ValidateAndMessage();

    partial void OnTextChanged(string value) => this.ValidateAndMessage();

    partial void OnFontSizeStringChanged(string value) => this.ValidateAndMessage();

    private void ValidateAndMessage()
    {
        if (!this.Validate(out string message))
        {
            this.ValidationMessage = this.Localize(message);
            this.editorViewModel.EnableButtons(enabled: false);
            return;
        }

        this.ValidationMessage = string.Empty;
        this.editorViewModel.EnableButtons(enabled: true);
    }

    private bool Validate(out string message)
    {
        message = string.Empty;
        string friendlyName = this.FriendlyName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(friendlyName) || friendlyName.Length < 3)
        {
            message = "Tools.Editor.Validation.FriendlyNameRequired";
            return false;
        }

        string text = this.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) || friendlyName.Length < 3)
        {
            message = "Tools.Editor.Validation.TextRequired";
            return false;
        }

        string fontSizeString = this.FontSizeString?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fontSizeString) || !int.TryParse(fontSizeString, out int maybeFontSize))
        {
            message = "Tools.Editor.Validation.FontSizeRequired";
            return false;
        }

        if (maybeFontSize < 6 || maybeFontSize > 200)
        {
            message = "Tools.Editor.Validation.FontSizeOutOfRange";
            return false;
        }

        this.fontSize = maybeFontSize;

        return true;
    }
    // Clicked "Add" button - add new editable to model, refresh master list,
    // and then select new item in master list
    public bool Add()
    {
        Watermark? existing = this.model.Watermarks.FromFriendlyName(this.FriendlyName.Trim());
        if (existing is not null)
        {
            this.ValidationMessage = this.Localize("Tools.Editor.Validation.FriendlyNameAlreadyExists");
            return false;
        }

        var newWatermark = this.CollectData();
        if (!this.model.AddWatermark(newWatermark, out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }

    // Clicked "Save" button - Save edits to model 
    public bool Save()
    {
        var editedWatermark = this.CollectData();
        if (!this.model.EditWatermark(editedWatermark, out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }

    // Clicked "Delete" button - Remove from model, refresh master list,
    // and then select new item in master list
    public bool Delete()
    {
        if (!this.model.DeleteWatermark(this.FriendlyName.Trim(), out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }

    private Watermark CollectData()
        => new()
        {
            FriendlyName = this.FriendlyName.Trim(),
            Text = this.Text.Trim(),
            FontSize = this.fontSize,
            FontFamily = this.SupportedFontFamilies[this.SelectedFontFamilyIndex].Name,
            PppFontStyle = SupportedFontStyleValues[this.SelectedFontStylesIndex],
            HexColorArgb = this.ForegroundColor.ToUInt32()
        };
}
