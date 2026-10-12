using System.Linq.Expressions;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using web.Constants;
using web.Data.Models;
using web.Repositories.LabelCollections.Dtos;

namespace web.Repositories.LabelCollections.Mappers;

public static class LabelCollectionMapper
{
    /// <summary>camelCase both ways — the designer reads and writes the same JSON. The default encoder escapes &lt;, &gt; and &amp;, so it is safe inside a &lt;script&gt; block.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// How ElementsJson is stored: unused fields left out, and æ/ø/å kept as they are (not escaped),
    /// so the table's search can find a label's text.
    /// </summary>
    private static readonly JsonSerializerOptions StorageJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly Expression<Func<LabelCollection, LabelCollectionListItemDto>> ToListItemDtoExpression = x => new LabelCollectionListItemDto
    {
        Id = x.Id,
        Name = x.Name,
        LabelCount = x.Items.Count,
        Copies = x.Items.Sum(i => (int?)i.Quantity) ?? 0,
        Across = x.Across,
        Down = x.Down,
        Landscape = x.Landscape,
        UpdatedAt = x.UpdatedAt
    };

    /// <summary>A new collection with one starter label showing the name, so the designer opens on something to edit.</summary>
    public static LabelCollection ToEntity(this CreateLabelCollectionDto dto, string name)
    {
        var collection = new LabelCollection
        {
            Name = name,
            Across = Math.Clamp(dto.Across, 1, LabelRules.MaxAcross),
            Down = Math.Clamp(dto.Down, 1, LabelRules.MaxDown),
            Landscape = dto.Landscape
        };

        collection.Items.Add(new LabelCollectionItem
        {
            Order = 0,
            Quantity = 1,
            Background = LabelDesignRules.DefaultBackground,
            ElementsJson = JsonSerializer.Serialize(new[]
            {
                new LabelElementDto
                {
                    Id = "e1", Type = LabelDesignRules.ElementText, X = 5, Y = 20, W = 90, H = 60,
                    Text = name, FontSize = 24, Bold = true, Italic = false,
                    Align = "center", VAlign = "middle", Color = LabelDesignRules.DefaultTextColor, Background = ""
                }
            }, StorageJsonOptions)
        });

        return collection;
    }

    public static LabelCollectionDesignDto ToDesignDto(this LabelCollection collection) => new()
    {
        Name = collection.Name,
        Across = collection.Across,
        Down = collection.Down,
        Landscape = collection.Landscape,
        Labels = collection.Items.OrderBy(i => i.Order).Select(i => new LabelDesignDto
        {
            Quantity = i.Quantity,
            Background = i.Background,
            Elements = DeserializeElements(i.ElementsJson)
        }).ToList()
    };

    public static LabelCollectionItem ToEntity(this LabelDesignDto label, int order) => new()
    {
        Order = order,
        Quantity = label.Quantity,
        Background = label.Background,
        ElementsJson = JsonSerializer.Serialize(label.Elements, StorageJsonOptions)
    };

    public static LabelMediaDto ToDto(this LabelCollectionMedia media) => new()
    {
        Id = media.PublicId,
        Name = media.FileMetadata.OriginalFileName,
        SizeBytes = media.FileMetadata.FileSizeBytes
    };

    private static List<LabelElementDto> DeserializeElements(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<LabelElementDto>>(json, JsonOptions) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}
