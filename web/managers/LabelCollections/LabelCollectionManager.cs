using System.Globalization;
using web.Constants;
using web.Extensions;
using web.Infrastructure.Labels;
using web.Managers.LabelCollections.Dtos;
using web.Managers.LabelCollections.Interfaces;
using web.Repositories.Files.Dtos;
using web.Repositories.Files.Interfaces;
using web.Repositories.LabelCollections.Dtos;
using web.Repositories.LabelCollections.Interfaces;

namespace web.Managers.LabelCollections;

public class LabelCollectionManager(
    ILabelCollectionRepository labelCollectionRepository,
    IFileRepository fileRepository,
    ILogger<LabelCollectionManager> logger) : ILabelCollectionManager
{
    public async Task<UploadLabelMediaResultDto> UploadMediaAsync(int id, Stream content, string fileName, long length, string? ownerId, CancellationToken cancellationToken = default)
    {
        if (!await labelCollectionRepository.ExistsAsync(id, cancellationToken))
            return new UploadLabelMediaResultDto { Status = UploadLabelMediaStatus.NotFound };

        // The content type comes from our own list, never from the browser.
        if (!LabelDesignRules.ImageTypes.TryGetValue(Path.GetExtension(fileName), out var contentType))
            return Invalid("Filtypen understøttes ikke. Brug et billede i PNG, JPG eller WEBP.");
        if (length <= 0)
            return Invalid("Filen er tom.");
        if (length > LabelDesignRules.MaxImageBytes)
            return Invalid($"Filen er for stor. Billeder må højst være {LabelDesignRules.MaxImageBytes / (1024 * 1024)} MB.");

        var file = await fileRepository.CreateAsync(new CreateStoredFileDto
        {
            OriginalFileName = Path.GetFileName(fileName),
            ContentType = contentType,
            Category = FileCategories.Labels,
            SubFolder = id.ToString(CultureInfo.InvariantCulture),
            OwnerId = ownerId
        }, content, cancellationToken);

        var media = await labelCollectionRepository.AddMediaAsync(id, file.Id, cancellationToken);
        if (media is null)
        {
            // The collection was deleted while the image uploaded.
            await fileRepository.DeleteAsync(file.Id, cancellationToken);
            return new UploadLabelMediaResultDto { Status = UploadLabelMediaStatus.NotFound };
        }

        logger.LogInformation("Label collection {CollectionId} got image {FileId}", id, file.Id);
        return new UploadLabelMediaResultDto { Status = UploadLabelMediaStatus.Uploaded, Media = media };
    }

    public async Task<RemoveLabelCollectionMediaStatus> DeleteMediaAsync(int id, Guid mediaId, CancellationToken cancellationToken = default)
    {
        var result = await labelCollectionRepository.RemoveMediaAsync(id, mediaId, cancellationToken);
        if (result.Status == RemoveLabelCollectionMediaStatus.Removed)
            await fileRepository.DeleteAsync(result.FileId, cancellationToken);
        return result.Status;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var fileIds = await labelCollectionRepository.DeleteAsync(id, cancellationToken);
        if (fileIds is null)
            return false;

        foreach (var fileId in fileIds)
            await fileRepository.DeleteAsync(fileId, cancellationToken);

        logger.LogInformation("Label collection {CollectionId} deleted with {FileCount} images", id, fileIds.Count);
        return true;
    }

    public async Task<StoredFileDto?> GetMediaFileAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var fileId = await labelCollectionRepository.GetMediaFileIdAsync(mediaId, cancellationToken);
        return fileId is null ? null : await fileRepository.GetByIdAsync(fileId.Value, cancellationToken);
    }

    public async Task<LabelPdfResult?> BuildPdfAsync(int id, bool cutMarks, CancellationToken cancellationToken = default)
    {
        var print = await labelCollectionRepository.GetPrintAsync(id, cancellationToken);
        if (print is null)
            return null;

        var copies = print.Design.Labels.Sum(l => l.Quantity);
        if (copies == 0)
            return LabelPdfResult.Fail("Der er ingen labels i samlingen. Design en label først.");
        if (copies > LabelRules.MaxLabelsPerPdf)
            return LabelPdfResult.Fail($"Der er {copies} labels, men én PDF kan højst have {LabelRules.MaxLabelsPerPdf}. Sæt antallet ned på nogle af labelsene.");

        // Images whose file is missing on disk are simply left out of the PDF.
        var images = new Dictionary<Guid, string>();
        foreach (var (mediaId, fileId) in print.MediaFileIds)
        {
            var file = await fileRepository.GetByIdAsync(fileId, cancellationToken);
            if (file is not null)
                images[mediaId] = file.FullPath;
        }

        var title = $"Labels - {print.Name}";
        byte[] content;
        try
        {
            content = LabelDesignPdf.Build(title, print.Design, images, cutMarks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Designed labels for collection {CollectionId} could not be generated", id);
            return LabelPdfResult.Fail("PDF'en med labels kunne ikke dannes.");
        }

        return new LabelPdfResult
        {
            Success = true,
            Content = content,
            Title = title,
            FileName = FileNameHelper.Sanitize(title, "labels") + ".pdf"
        };
    }

    private static UploadLabelMediaResultDto Invalid(string error) =>
        new() { Status = UploadLabelMediaStatus.Invalid, ErrorMessage = error };
}
