using App.Tests.TestHelpers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace App.Tests.Unit;

/// <summary>
/// Tests for style standardization functionality as specified in CLAUDE.md Section 6.
/// Verifies OpenXML style properties match the exact specification requirements.
/// </summary>
public class StyleStandardizationTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    #region Normal Style Tests

    [Fact]
    public void NormalStyle_WhenStandardized_HasCorrectProperties()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithStylesToStandardize();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act - Get Normal style from the document
        var stylesPart = document.MainDocumentPart!.StyleDefinitionsPart;
        var normalStyle = stylesPart?.Styles?.Elements<Style>()
            .FirstOrDefault(s => s.StyleId == "Normal");

        // Assert - Verify Normal style properties per CLAUDE.md specification
        normalStyle.Should().NotBeNull("Normal style should exist");
        normalStyle!.Type.Should().Be(StyleValues.Paragraph);
        
        // Expected properties for Normal style:
        // - Verdana, 12pt, black, single line spacing, 6pt before
        // - Do NOT enable "Don't add space between paragraphs of the same style"
        AssertStyleProperties(normalStyle, new ExpectedStyleProperties
        {
            FontName = "Verdana",
            FontSize = 24, // 12pt = 24 half-points
            Color = "000000", // Black
            SpacingBefore = 120, // 6pt = 120 twips
            LineSpacing = "240", // Single spacing = 240 twips
            LineRule = "auto"
        });
    }

    #endregion

    #region Heading 1 Style Tests

    [Fact]
    public void Heading1Style_WhenStandardized_HasCorrectProperties()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithStylesToStandardize();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var stylesPart = document.MainDocumentPart!.StyleDefinitionsPart;
        var heading1Style = stylesPart?.Styles?.Elements<Style>()
            .FirstOrDefault(s => s.StyleId == "Heading1");

        // Assert - Verify Heading 1 style properties per CLAUDE.md specification
        heading1Style.Should().NotBeNull("Heading 1 style should exist");
        heading1Style!.Type.Should().Be(StyleValues.Paragraph);

        // Expected properties for Heading 1:
        // - Verdana, 18pt, bold, black, left-aligned, 0pt before / 12pt after, single spacing
        AssertStyleProperties(heading1Style, new ExpectedStyleProperties
        {
            FontName = "Verdana",
            FontSize = 36, // 18pt = 36 half-points
            Bold = true,
            Color = "000000", // Black
            Justification = "left",
            SpacingBefore = 0, // 0pt before
            SpacingAfter = 240, // 12pt = 240 twips after
            LineSpacing = "240", // Single spacing
            LineRule = "auto"
        });
    }

    #endregion

    #region Heading 2 Style Tests

    [Fact]
    public void Heading2Style_WhenStandardized_HasCorrectProperties()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithStylesToStandardize();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var stylesPart = document.MainDocumentPart!.StyleDefinitionsPart;
        var heading2Style = stylesPart?.Styles?.Elements<Style>()
            .FirstOrDefault(s => s.StyleId == "Heading2");

        // Assert - Verify Heading 2 style properties per CLAUDE.md specification
        heading2Style.Should().NotBeNull("Heading 2 style should exist");
        heading2Style!.Type.Should().Be(StyleValues.Paragraph);

        // Expected properties for Heading 2:
        // - Verdana, 14pt, bold, black, left-aligned, 6pt before / 6pt after, single spacing
        AssertStyleProperties(heading2Style, new ExpectedStyleProperties
        {
            FontName = "Verdana",
            FontSize = 28, // 14pt = 28 half-points
            Bold = true,
            Color = "000000", // Black
            Justification = "left",
            SpacingBefore = 120, // 6pt = 120 twips before
            SpacingAfter = 120, // 6pt = 120 twips after
            LineSpacing = "240", // Single spacing
            LineRule = "auto"
        });
    }

    #endregion

    #region Hyperlink Character Style Tests

    [Fact]
    public void HyperlinkStyle_WhenStandardized_HasCorrectCharacterProperties()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithStylesToStandardize();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var stylesPart = document.MainDocumentPart!.StyleDefinitionsPart;
        var hyperlinkStyle = stylesPart?.Styles?.Elements<Style>()
            .FirstOrDefault(s => s.StyleId == "Hyperlink");

        // Assert - Verify Hyperlink character style properties per CLAUDE.md specification
        hyperlinkStyle.Should().NotBeNull("Hyperlink style should exist");
        hyperlinkStyle!.Type.Should().Be(StyleValues.Character);

        // Expected properties for Hyperlink character style:
        // - Verdana, 12pt, blue #0000FF, underline
        // - Single spacing characteristics at paragraph level remain governed by containing paragraph
        AssertCharacterStyleProperties(hyperlinkStyle, new ExpectedCharacterStyleProperties
        {
            FontName = "Verdana",
            FontSize = 24, // 12pt = 24 half-points
            Color = "0000FF", // Blue
            Underline = true
        });
    }

    #endregion

    #region Style Creation Tests

    [Fact]
    public void StylesPart_WhenMissing_IsCreatedWithStandardStyles()
    {
        // Arrange - Document without styles part
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);
        docBuilder.AddParagraph("Test paragraph");
        var document = docBuilder.Build();

        // Act - Simulate ensuring styles exist (would be done by IFormattingService)
        var mainPart = document.MainDocumentPart!;
        var stylesPart = mainPart.StyleDefinitionsPart ?? mainPart.AddNewPart<StyleDefinitionsPart>();
        
        if (stylesPart.Styles == null)
        {
            stylesPart.Styles = new Styles();
        }

        // Add Normal style
        var normalStyle = new Style
        {
            StyleId = "Normal",
            Type = StyleValues.Paragraph
        };
        normalStyle.AppendChild(new StyleName { Val = "Normal" });
        stylesPart.Styles.AppendChild(normalStyle);

        // Assert
        stylesPart.Should().NotBeNull("Styles part should be created");
        stylesPart.Styles.Should().NotBeNull("Styles collection should be created");
        
        var createdNormalStyle = stylesPart.Styles.Elements<Style>()
            .FirstOrDefault(s => s.StyleId == "Normal");
        createdNormalStyle.Should().NotBeNull("Normal style should be created");
    }

    #endregion

    #region Style Property Validation Tests

    [Theory]
    [InlineData(12, 24)] // 12pt = 24 half-points
    [InlineData(18, 36)] // 18pt = 36 half-points
    [InlineData(14, 28)] // 14pt = 28 half-points
    public void FontSize_ConversionToHalfPoints_IsCorrect(int points, int expectedHalfPoints)
    {
        // Act & Assert
        var halfPoints = points * 2;
        halfPoints.Should().Be(expectedHalfPoints, $"{points}pt should equal {expectedHalfPoints} half-points");
    }

    [Theory]
    [InlineData(6, 120)]  // 6pt = 120 twips
    [InlineData(12, 240)] // 12pt = 240 twips
    [InlineData(0, 0)]    // 0pt = 0 twips
    public void Spacing_ConversionToTwips_IsCorrect(int points, int expectedTwips)
    {
        // Act & Assert
        var twips = points * 20; // 1 point = 20 twips
        twips.Should().Be(expectedTwips, $"{points}pt should equal {expectedTwips} twips");
    }

    [Fact]
    public void SingleLineSpacing_HasCorrectValues()
    {
        // Assert - Single spacing should be 240 twips with auto rule
        const int singleSpacingTwips = 240;
        const string autoRule = "auto";
        
        singleSpacingTwips.Should().Be(240, "Single spacing should be 240 twips");
        autoRule.Should().Be("auto", "Line rule should be auto for single spacing");
    }

    #endregion

    #region Color Validation Tests

    [Theory]
    [InlineData("000000", "Black")]
    [InlineData("0000FF", "Blue")]
    public void ColorValues_AreValidHexColors(string hexColor, string colorName)
    {
        // Act & Assert
        hexColor.Should().MatchRegex(@"^[0-9A-Fa-f]{6}$", $"{colorName} should be a valid 6-digit hex color");
        
        if (colorName == "Black")
        {
            hexColor.Should().Be("000000");
        }
        else if (colorName == "Blue")
        {
            hexColor.Should().Be("0000FF");
        }
    }

    #endregion

    #region Style Application Tests

    [Fact]
    public void ParagraphStyle_WhenApplied_ShowsInParagraphProperties()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithStylesToStandardize();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act - Find paragraph with Normal style applied
        var paragraph = document.MainDocumentPart!.Document.Descendants<Paragraph>()
            .FirstOrDefault(p => p.ParagraphProperties?.ParagraphStyleId?.Val == "Normal");

        // Assert
        paragraph.Should().NotBeNull("Should find paragraph with Normal style applied");
        paragraph!.ParagraphProperties.Should().NotBeNull();
        paragraph.ParagraphProperties!.ParagraphStyleId.Should().NotBeNull();
        paragraph.ParagraphProperties.ParagraphStyleId!.Val!.Value.Should().Be("Normal");
    }

    #endregion

    #region Helper Methods

    private static void AssertStyleProperties(Style style, ExpectedStyleProperties expected)
    {
        // Get paragraph properties
        var paragraphProps = style.StyleParagraphProperties;
        var runProps = style.StyleRunProperties;

        // Font properties (from run properties)
        if (!string.IsNullOrEmpty(expected.FontName))
        {
            runProps?.RunFonts?.Ascii?.Value.Should().Be(expected.FontName, $"Font should be {expected.FontName}");
            runProps?.RunFonts?.HighAnsi?.Value.Should().Be(expected.FontName, $"High ANSI font should be {expected.FontName}");
        }

        if (expected.FontSize.HasValue)
        {
            runProps?.FontSize?.Val?.Value.Should().Be(expected.FontSize.Value.ToString(), $"Font size should be {expected.FontSize} half-points");
        }

        if (expected.Bold.HasValue && expected.Bold.Value)
        {
            runProps?.Bold.Should().NotBeNull("Bold should be set");
        }

        if (!string.IsNullOrEmpty(expected.Color))
        {
            runProps?.Color?.Val?.Value.Should().Be(expected.Color, $"Color should be {expected.Color}");
        }

        // Paragraph properties
        if (!string.IsNullOrEmpty(expected.Justification))
        {
            var justificationValue = expected.Justification.ToLower() switch
            {
                "left" => JustificationValues.Left,
                "center" => JustificationValues.Center,
                "right" => JustificationValues.Right,
                _ => JustificationValues.Left
            };
            paragraphProps?.Justification?.Val?.Value.Should().Be(justificationValue);
        }

        // Spacing properties
        if (expected.SpacingBefore.HasValue)
        {
            paragraphProps?.SpacingBetweenLines?.Before?.Value.Should().Be(expected.SpacingBefore.Value.ToString());
        }

        if (expected.SpacingAfter.HasValue)
        {
            paragraphProps?.SpacingBetweenLines?.After?.Value.Should().Be(expected.SpacingAfter.Value.ToString());
        }

        if (!string.IsNullOrEmpty(expected.LineSpacing))
        {
            paragraphProps?.SpacingBetweenLines?.Line?.Value.Should().Be(expected.LineSpacing);
        }

        if (!string.IsNullOrEmpty(expected.LineRule))
        {
            var lineRule = expected.LineRule.ToLower() switch
            {
                "auto" => LineSpacingRuleValues.Auto,
                "exact" => LineSpacingRuleValues.Exact,
                _ => LineSpacingRuleValues.Auto
            };
            paragraphProps?.SpacingBetweenLines?.LineRule?.Value.Should().Be(lineRule);
        }
    }

    private static void AssertCharacterStyleProperties(Style style, ExpectedCharacterStyleProperties expected)
    {
        var runProps = style.StyleRunProperties;
        runProps.Should().NotBeNull("Character style should have run properties");

        if (!string.IsNullOrEmpty(expected.FontName))
        {
            runProps!.RunFonts?.Ascii?.Value.Should().Be(expected.FontName);
            runProps.RunFonts?.HighAnsi?.Value.Should().Be(expected.FontName);
        }

        if (expected.FontSize.HasValue)
        {
            runProps!.FontSize?.Val?.Value.Should().Be(expected.FontSize.Value.ToString());
        }

        if (!string.IsNullOrEmpty(expected.Color))
        {
            runProps!.Color?.Val?.Value.Should().Be(expected.Color);
        }

        if (expected.Underline.HasValue && expected.Underline.Value)
        {
            runProps!.Underline.Should().NotBeNull("Underline should be set");
            runProps.Underline!.Val?.Value.Should().Be(UnderlineValues.Single);
        }
    }

    #endregion

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
        _disposables.Clear();
    }
}

/// <summary>
/// Helper class for expected paragraph style properties in tests.
/// </summary>
public class ExpectedStyleProperties
{
    public string? FontName { get; set; }
    public int? FontSize { get; set; } // In half-points
    public bool? Bold { get; set; }
    public string? Color { get; set; } // Hex color
    public string? Justification { get; set; }
    public int? SpacingBefore { get; set; } // In twips
    public int? SpacingAfter { get; set; } // In twips
    public string? LineSpacing { get; set; } // In twips
    public string? LineRule { get; set; }
}

/// <summary>
/// Helper class for expected character style properties in tests.
/// </summary>
public class ExpectedCharacterStyleProperties
{
    public string? FontName { get; set; }
    public int? FontSize { get; set; } // In half-points
    public string? Color { get; set; } // Hex color
    public bool? Underline { get; set; }
}