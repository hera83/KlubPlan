using web.Repositories.Files.Dtos;

namespace web.Repositories.Files.Interfaces;

public interface IFileRepository
{
    /// <summary>
    /// Writes the stream to app_files/&lt;category&gt;[/&lt;sub folder&gt;]/&lt;guid&gt;.&lt;ext&gt; and saves the FileMetadata row.
    /// If the row can't be saved, the file is deleted again.
    /// </summary>
    Task<StoredFileDto> CreateAsync(CreateStoredFileDto dto, Stream content, CancellationToken cancellationToken = default);

    /// <summary>The file's metadata, or null if the row doesn't exist, is marked deleted or the physical file is missing.</summary>
    Task<StoredFileDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Deletes the FileMetadata row and the physical file. False if the row doesn't exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Removes app_files/&lt;category&gt;/&lt;sub folder&gt;/ when it exists and is empty (e.g. after an activity's files are deleted).</summary>
    Task DeleteEmptyFolderAsync(string category, string subFolder, CancellationToken cancellationToken = default);
}
