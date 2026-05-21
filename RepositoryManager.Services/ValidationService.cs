using System.Text.Json;
using System.Xml;
using RepositoryManager.Models;

namespace RepositoryManager.Services;

/// <summary>
/// Validates raw file content before it is uploaded to the repository.
/// All methods are pure / stateless and safe to call from any thread.
/// </summary>
public static class ValidationService
{
    /// <summary>
    /// Detect the item type from the file extension, then validate the content.
    /// Returns (ItemType, errorMessage). errorMessage is null on success.
    /// </summary>
    public static (ItemType Type, string? Error) ValidateContent(string fileName, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (ItemType.Unknown, "File is empty.");

        string ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext switch
        {
            ".json" => ValidateJson(content),
            ".xml"  => ValidateXml(content),
            _       => (ItemType.Unknown, $"Unsupported file type: '{ext}'. Only .json and .xml are accepted.")
        };
    }

    private static (ItemType, string?) ValidateJson(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content, new JsonDocumentOptions { AllowTrailingCommas = true });
            return (ItemType.Json, null);
        }
        catch (JsonException ex)
        {
            return (ItemType.Json, $"Invalid JSON: {ex.Message}");
        }
    }

    private static (ItemType, string?) ValidateXml(string content)
    {
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };
            using var reader = XmlReader.Create(new System.IO.StringReader(content), settings);
            while (reader.Read()) { }
            return (ItemType.Xml, null);
        }
        catch (XmlException ex)
        {
            return (ItemType.Xml, $"Invalid XML: {ex.Message}");
        }
    }
}
