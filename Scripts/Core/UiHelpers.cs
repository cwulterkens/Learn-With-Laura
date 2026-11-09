using Godot;

public static class UiHelpers
{
    private static readonly Color LightText = new(0.98f, 0.91f, 0.78f);
    private static readonly Color ButtonText = new(0.99f, 0.91f, 0.73f);
    private static readonly Color DarkText = new(0.13f, 0.08f, 0.05f);
    private static Font _uiFont = null!;
    private static Font _titleFont = null!;

    public static void ApplyConfiguredFont(Control root)
    {
        if (root == null)
        {
            return;
        }

        ApplyFixedWindow();

        EnsureUiFont();

        NormalizeControlTree(root);
    }

    public static void ApplyFixedWindow()
    {
        AppState.ApplyWindowSettings();
    }

    public static void StyleNewControl(Control control)
    {
        if (control == null)
        {
            return;
        }

        EnsureUiFont();
        NormalizeControlTree(control);
    }

    private static void EnsureUiFont()
    {
        string path = AppState.Settings.UiFontPath;
        if (!string.IsNullOrWhiteSpace(path) && ResourceLoader.Exists(path))
        {
            FontFile font = GD.Load<FontFile>(path);
            if (font != null)
            {
                _uiFont = font;
            }
        }

        _uiFont ??= new SystemFont
        {
            FontNames = new[] { "Avenir", "Helvetica Neue", "Arial" },
            AllowSystemFallback = true
        };

        const string titleFontPath = "res://assets/fonts/AtkinsonHyperlegible-Bold.otf";
        if (_titleFont == null && ResourceLoader.Exists(titleFontPath))
        {
            _titleFont = GD.Load<FontFile>(titleFontPath);
        }

        _titleFont ??= _uiFont;
    }

    private static StyleBox BuildButtonBox(string texturePath, Color fallbackColor)
    {
        if (ResourceLoader.Exists(texturePath))
        {
            var textureBox = new StyleBoxTexture
            {
                Texture = GD.Load<Texture2D>(texturePath),
                TextureMarginLeft = 24,
                TextureMarginTop = 18,
                TextureMarginRight = 24,
                TextureMarginBottom = 18,
                ContentMarginLeft = 20,
                ContentMarginTop = 12,
                ContentMarginRight = 20,
                ContentMarginBottom = 12
            };
            return textureBox;
        }

        return new StyleBoxFlat
        {
            BgColor = fallbackColor,
            BorderColor = new Color(0.25f, 0.16f, 0.10f),
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
    }

    private static void NormalizeControlTree(Control root)
    {
        if (root.GetParent() == root.GetTree()?.Root)
        {
            root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            root.OffsetLeft = 0;
            root.OffsetTop = 0;
            root.OffsetRight = 0;
            root.OffsetBottom = 0;
        }

        NormalizeControl(root);

        foreach (Node child in root.GetChildren())
        {
            if (child is Control control)
            {
                NormalizeControlTree(control);
            }
        }
    }

    private static void NormalizeControl(Control control)
    {
        Color lightText = AppState.Settings.HighContrast ? Colors.White : LightText;
        Color buttonText = AppState.Settings.HighContrast ? new Color(1f, 0.96f, 0.62f) : ButtonText;
        int textBoost = AppState.Settings.LargeText ? 4 : 0;

        if (control is Button button)
        {
            bool isAnswerButton = button.Name.ToString().StartsWith("Option");
            bool isFooterButton = button.GetParent()?.Name.ToString() == "Footer";
            float minimumWidth = isAnswerButton ? 760 : isFooterButton ? 280 : 560;
            button.CustomMinimumSize = new Vector2(
                Mathf.Max(button.CustomMinimumSize.X, minimumWidth),
                Mathf.Max(button.CustomMinimumSize.Y, isAnswerButton ? 76 : 64));
            button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            button.Alignment = HorizontalAlignment.Center;
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            button.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            button.FocusMode = Control.FocusModeEnum.All;
            button.AddThemeColorOverride("font_color", buttonText);
            button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.96f, 0.82f));
            button.AddThemeColorOverride("font_pressed_color", new Color(0.98f, 0.86f, 0.62f));
            button.AddThemeColorOverride("font_disabled_color", new Color(0.62f, 0.58f, 0.52f));
            button.AddThemeFontOverride("font", _uiFont);
            button.AddThemeFontSizeOverride("font_size", 26 + textBoost);
            button.AddThemeStyleboxOverride("normal", BuildButtonBox("res://assets/ui/button_normal.png", new Color(0.60f, 0.35f, 0.18f)));
            button.AddThemeStyleboxOverride("hover", BuildButtonBox("res://assets/ui/button_hover.png", new Color(0.70f, 0.43f, 0.21f)));
            button.AddThemeStyleboxOverride("pressed", BuildButtonBox("res://assets/ui/button_pressed.png", new Color(0.42f, 0.25f, 0.15f)));
            button.AddThemeStyleboxOverride("disabled", BuildButtonBox("res://assets/ui/button_disabled.png", new Color(0.32f, 0.30f, 0.27f)));
            button.AddThemeStyleboxOverride("focus", BuildFocusBox());
        }

        if (control is OptionButton optionButton)
        {
            optionButton.CustomMinimumSize = new Vector2(
                Mathf.Max(optionButton.CustomMinimumSize.X, 560),
                Mathf.Max(optionButton.CustomMinimumSize.Y, 64));
            optionButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            optionButton.AddThemeColorOverride("font_color", buttonText);
            optionButton.AddThemeColorOverride("font_hover_color", new Color(1f, 0.96f, 0.82f));
            optionButton.AddThemeColorOverride("font_pressed_color", new Color(0.98f, 0.86f, 0.62f));
            optionButton.AddThemeFontOverride("font", _uiFont);
            optionButton.AddThemeFontSizeOverride("font_size", 24 + textBoost);
            optionButton.AddThemeStyleboxOverride("normal", BuildButtonBox("res://assets/ui/button_normal.png", new Color(0.60f, 0.35f, 0.18f)));
            optionButton.AddThemeStyleboxOverride("hover", BuildButtonBox("res://assets/ui/button_hover.png", new Color(0.70f, 0.43f, 0.21f)));
            optionButton.AddThemeStyleboxOverride("pressed", BuildButtonBox("res://assets/ui/button_pressed.png", new Color(0.42f, 0.25f, 0.15f)));
            optionButton.AddThemeStyleboxOverride("focus", BuildFocusBox());

            PopupMenu popup = optionButton.GetPopup();
            popup.AddThemeColorOverride("font_color", DarkText);
            popup.AddThemeColorOverride("font_hover_color", buttonText);
            popup.AddThemeFontOverride("font", _uiFont);
            popup.AddThemeFontSizeOverride("font_size", 22 + textBoost);
        }

        if (control is TextureButton textureButton)
        {
            textureButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        }

        if (control is Label label)
        {
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.SizeFlagsHorizontal = Control.SizeFlags.Fill;
            label.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            label.AddThemeColorOverride("font_color", lightText);
            label.AddThemeColorOverride("font_shadow_color", new Color(0.05f, 0.03f, 0.02f, 0.95f));
            label.AddThemeConstantOverride("shadow_offset_x", 2);
            label.AddThemeConstantOverride("shadow_offset_y", 2);
            bool isTitleLabel = label.Name.ToString().Contains("Title");
            label.AddThemeFontOverride("font", isTitleLabel ? _titleFont : _uiFont);
            bool isQuestionLabel = label.Name.ToString().Contains("Question");
            label.CustomMinimumSize = new Vector2(
                label.CustomMinimumSize.X,
                Mathf.Max(label.CustomMinimumSize.Y, isQuestionLabel ? 120 : 36));
            label.AddThemeFontSizeOverride(
                "font_size",
                (isTitleLabel ? 34 : isQuestionLabel ? 30 : 24) + textBoost);
        }

        if (control is CheckBox checkBox)
        {
            checkBox.AddThemeColorOverride("font_color", lightText);
            checkBox.AddThemeColorOverride("font_hover_color", new Color(1f, 0.96f, 0.82f));
            checkBox.AddThemeFontOverride("font", _uiFont);
            checkBox.AddThemeFontSizeOverride("font_size", 22 + textBoost);
        }

        if (control is LineEdit lineEdit)
        {
            lineEdit.AddThemeColorOverride("font_color", DarkText);
            lineEdit.AddThemeColorOverride("font_placeholder_color", new Color(0.38f, 0.30f, 0.24f));
            lineEdit.AddThemeFontOverride("font", _uiFont);
            lineEdit.AddThemeFontSizeOverride("font_size", 22 + textBoost);
            lineEdit.AddThemeStyleboxOverride("normal", BuildInputBox());
            lineEdit.AddThemeStyleboxOverride("read_only", BuildInputBox());
        }

        if (control is SpinBox spinBox)
        {
            spinBox.CustomMinimumSize = new Vector2(
                Mathf.Max(spinBox.CustomMinimumSize.X, 560),
                Mathf.Max(spinBox.CustomMinimumSize.Y, 64));
            spinBox.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            spinBox.AddThemeColorOverride("font_color", DarkText);
            spinBox.AddThemeFontOverride("font", _uiFont);
            spinBox.AddThemeFontSizeOverride("font_size", 22 + textBoost);
            spinBox.AddThemeStyleboxOverride("normal", BuildInputBox());
        }

        if (control is ItemList itemList)
        {
            itemList.AddThemeColorOverride("font_color", DarkText);
            itemList.AddThemeColorOverride("font_selected_color", buttonText);
            itemList.AddThemeFontOverride("font", _uiFont);
            itemList.AddThemeFontSizeOverride("font_size", 22 + textBoost);
            itemList.AddThemeStyleboxOverride("normal", BuildInputBox());
        }

        if (control is Panel)
        {
            control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            control.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            control.AddThemeStyleboxOverride("panel", BuildPanelBox());
        }
    }

    private static StyleBoxFlat BuildPanelBox()
    {
        return new StyleBoxFlat
        {
            BgColor = AppState.Settings.HighContrast
                ? new Color(0.035f, 0.025f, 0.02f, 0.96f)
                : new Color(0.12f, 0.08f, 0.055f, 0.82f),
            BorderColor = AppState.Settings.HighContrast
                ? new Color(1f, 0.86f, 0.34f)
                : new Color(0.67f, 0.45f, 0.23f),
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            ContentMarginLeft = 22,
            ContentMarginTop = 22,
            ContentMarginRight = 22,
            ContentMarginBottom = 22
        };
    }

    private static StyleBoxFlat BuildInputBox()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.94f, 0.83f, 0.62f, 0.95f),
            BorderColor = new Color(0.48f, 0.29f, 0.15f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 10,
            ContentMarginTop = 8,
            ContentMarginRight = 10,
            ContentMarginBottom = 8
        };
    }

    private static StyleBoxFlat BuildFocusBox()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0f),
            BorderColor = new Color(1f, 0.86f, 0.34f, 1f),
            BorderWidthLeft = 5,
            BorderWidthTop = 5,
            BorderWidthRight = 5,
            BorderWidthBottom = 5,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ExpandMarginLeft = 4,
            ExpandMarginTop = 4,
            ExpandMarginRight = 4,
            ExpandMarginBottom = 4
        };
    }
}
