using System.Globalization;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Models;
using web.Extensions;
using web.Repositories.ActivityFiles.Dtos;
using web.Repositories.ActivityFiles.Interfaces;
using web.Repositories.ActivityFiles.Mappers;

namespace web.Repositories.ActivityFiles;

public class ActivityFileRepository(WebDbContext dbContext, ILogger<ActivityFileRepository> logger) : IActivityFileRepository
{
    private static readonly StringComparer NameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("da-DK"), ignoreCase: true);

    private sealed record FolderNode(int Id, int? ParentId, string Name, DateTime ModifiedAtUtc);

    // ─── Read ────────────────────────────────────────────────────────────────

    public async Task<ActivityFolderContentDto?> GetFolderAsync(int activityId, int? folderId, string? searchText, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Activities.AnyAsync(a => a.Id == activityId, cancellationToken))
            return null;

        var folders = await LoadFoldersAsync(activityId, cancellationToken);
        if (folderId.HasValue && !folders.ContainsKey(folderId.Value))
            folderId = null;

        var content = new ActivityFolderContentDto
        {
            ActivityId = activityId,
            CurrentFolderId = folderId,
            SearchText = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim()
        };

        content.Breadcrumbs.Add(new ActivityFolderBreadcrumbDto { Id = null, Name = "Filer" });
        content.Breadcrumbs.AddRange(GetPath(folders, folderId).Select(f => new ActivityFolderBreadcrumbDto { Id = f.Id, Name = f.Name }));

        var fileCounts = await dbContext.ActivityFiles.AsNoTracking()
            .Where(f => f.ActivityId == activityId)
            .GroupBy(f => f.FolderId)
            .Select(g => new { FolderId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var countsByFolder = fileCounts.Where(c => c.FolderId.HasValue).ToDictionary(c => c.FolderId!.Value, c => c.Count);

        content.TotalFileCount = fileCounts.Sum(c => c.Count);
        content.TotalSizeBytes = await dbContext.ActivityFileVersions.AsNoTracking()
            .Where(v => v.ActivityFile.ActivityId == activityId)
            .SumAsync(v => (long?)v.FileMetadata.FileSizeBytes, cancellationToken) ?? 0;

        var term = content.SearchText;
        var folderRows = term is null
            ? folders.Values.Where(f => f.ParentId == folderId)
            : folders.Values.Where(f => f.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

        content.Folders = folderRows
            .OrderBy(f => f.Name, NameComparer)
            .Select(f => new ActivityFolderItemDto
            {
                Id = f.Id,
                Name = f.Name,
                ItemCount = folders.Values.Count(c => c.ParentId == f.Id) + countsByFolder.GetValueOrDefault(f.Id),
                ModifiedAtUtc = f.ModifiedAtUtc,
                ParentPath = term is null ? null : PathText(folders, f.ParentId)
            })
            .ToList();

        var fileQuery = dbContext.ActivityFiles.AsNoTracking().Where(f => f.ActivityId == activityId);
        if (term is null)
            fileQuery = fileQuery.Where(f => f.FolderId == folderId);

        var fileRows = await fileQuery
            .Select(f => new
            {
                f.Id,
                f.FileName,
                f.FolderId,
                ModifiedAtUtc = f.UpdatedAt,
                VersionCount = f.Versions.Count,
                Current = f.Versions
                    .OrderByDescending(v => v.VersionNumber)
                    .Select(v => new
                    {
                        v.Id,
                        v.VersionNumber,
                        v.FileMetadata.FileSizeBytes,
                        v.FileMetadata.ContentType,
                        UploadedBy = v.UploadedByUser != null ? v.UploadedByUser.DisplayName : null
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        content.Files = fileRows
            .Where(f => f.Current is not null)
            .Where(f => term is null || f.FileName.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.FileName, NameComparer)
            .Select(f => new ActivityFileItemDto
            {
                Id = f.Id,
                FileName = f.FileName,
                FolderId = f.FolderId,
                FolderPath = term is null ? null : PathText(folders, f.FolderId),
                CurrentVersionId = f.Current!.Id,
                CurrentVersionNumber = f.Current.VersionNumber,
                VersionCount = f.VersionCount,
                SizeBytes = f.Current.FileSizeBytes,
                ContentType = f.Current.ContentType,
                ModifiedAtUtc = f.ModifiedAtUtc,
                ModifiedByName = f.Current.UploadedBy
            })
            .ToList();

        return content;
    }

    public async Task<List<ActivityFolderTreeItemDto>> GetFolderTreeAsync(int activityId, CancellationToken cancellationToken = default)
    {
        var folders = await LoadFoldersAsync(activityId, cancellationToken);
        var result = new List<ActivityFolderTreeItemDto> { new() { Id = null, Name = "Filer", Depth = 0 } };

        void Walk(int? parentId, int depth)
        {
            foreach (var child in folders.Values.Where(f => f.ParentId == parentId).OrderBy(f => f.Name, NameComparer))
            {
                result.Add(new ActivityFolderTreeItemDto { Id = child.Id, Name = child.Name, Depth = depth });
                Walk(child.Id, depth + 1);
            }
        }

        Walk(null, 1);
        return result;
    }

    public async Task<ActivityFileVersionsDto?> GetVersionsAsync(int activityId, int fileId, CancellationToken cancellationToken = default)
    {
        var file = await dbContext.ActivityFiles.AsNoTracking()
            .Where(f => f.Id == fileId && f.ActivityId == activityId)
            .Select(f => new
            {
                f.Id,
                f.FileName,
                Versions = f.Versions.AsQueryable()
                    .OrderByDescending(v => v.VersionNumber)
                    .Select(ActivityFileMapper.ToVersionItemDtoExpression)
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (file is null)
            return null;

        if (file.Versions.Count > 0)
            file.Versions[0].IsCurrent = true;

        return new ActivityFileVersionsDto { ActivityId = activityId, FileId = file.Id, FileName = file.FileName, Versions = file.Versions };
    }

    public async Task<ActivityFileVersionRefDto?> GetVersionRefAsync(int activityId, int fileId, int? versionId, CancellationToken cancellationToken = default)
    {
        var versions = dbContext.ActivityFileVersions.AsNoTracking()
            .Where(v => v.ActivityFileId == fileId && v.ActivityFile.ActivityId == activityId);

        var version = await (versionId.HasValue
                ? versions.Where(v => v.Id == versionId.Value)
                : versions.OrderByDescending(v => v.VersionNumber))
            .Select(v => new
            {
                v.VersionNumber,
                v.FileMetadataId,
                v.ActivityFile.FileName,
                v.FileMetadata.ContentType,
                MaxVersion = v.ActivityFile.Versions.Max(x => x.VersionNumber)
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (version is null)
            return null;

        return new ActivityFileVersionRefDto
        {
            StoredFileId = version.FileMetadataId,
            VersionNumber = version.VersionNumber,
            DownloadName = version.VersionNumber == version.MaxVersion
                ? version.FileName
                : $"{Path.GetFileNameWithoutExtension(version.FileName)} (v{version.VersionNumber}){Path.GetExtension(version.FileName)}",
            ContentType = version.ContentType
        };
    }

    public async Task<ActivityFileZipPlanDto?> GetZipPlanAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken = default)
    {
        var activityTitle = await dbContext.Activities.Where(a => a.Id == activityId).Select(a => a.Title).FirstOrDefaultAsync(cancellationToken);
        if (activityTitle is null)
            return null;

        var folders = await LoadFoldersAsync(activityId, cancellationToken);

        // Drop selected folders that sit inside another selected folder — they're included via their ancestor.
        var selectedRoots = folderIds.Where(folders.ContainsKey).Distinct().ToHashSet();
        selectedRoots.RemoveWhere(id => GetPath(folders, folders[id].ParentId).Any(a => selectedRoots.Contains(a.Id)));

        var coveredFolders = GetDescendants(folders, selectedRoots);
        var coveredList = coveredFolders.ToList();
        var fileIdList = fileIds.Distinct().ToList();

        var files = await dbContext.ActivityFiles.AsNoTracking()
            .Where(f => f.ActivityId == activityId && (fileIdList.Contains(f.Id) || (f.FolderId != null && coveredList.Contains(f.FolderId.Value))))
            .Select(f => new
            {
                f.FileName,
                f.FolderId,
                StoredFileId = f.Versions.OrderByDescending(v => v.VersionNumber).Select(v => (int?)v.FileMetadataId).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        // Path of a folder relative to the selected root it belongs to, e.g. "Planlægning/Budget".
        string RelativeFolderPath(int folderId)
        {
            var names = new List<string>();
            int? current = folderId;
            while (current.HasValue)
            {
                var node = folders[current.Value];
                names.Insert(0, node.Name);
                if (selectedRoots.Contains(node.Id))
                    break;
                current = node.ParentId;
            }
            return string.Join('/', names);
        }

        var plan = new ActivityFileZipPlanDto();
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var folderId in coveredFolders)
        {
            var path = RelativeFolderPath(folderId) + "/";
            if (usedPaths.Add(path))
                plan.EmptyFolders.Add(path);
        }

        foreach (var file in files.Where(f => f.StoredFileId is not null))
        {
            var inCoveredFolder = file.FolderId.HasValue && coveredFolders.Contains(file.FolderId.Value);
            var entryPath = inCoveredFolder ? $"{RelativeFolderPath(file.FolderId!.Value)}/{file.FileName}" : file.FileName;

            // Loose files from different folders (e.g. picked in search results) can share a name.
            var unique = entryPath;
            for (var n = 2; !usedPaths.Add(unique); n++)
                unique = Path.ChangeExtension(entryPath, null) + $" ({n})" + Path.GetExtension(entryPath);

            plan.Entries.Add(new ActivityFileZipPlanEntryDto { EntryPath = unique, StoredFileId = file.StoredFileId!.Value });
        }

        var zipBaseName = selectedRoots.Count == 1 && fileIdList.Count == 0
            ? folders[selectedRoots.First()].Name
            : $"{activityTitle} - filer";
        plan.ZipFileName = SanitizeForFileName(zipBaseName) + ".zip";
        return plan;
    }

    // ─── Upload / versions ───────────────────────────────────────────────────

    public async Task<UploadTargetDto> ResolveUploadTargetAsync(int activityId, int? folderId, string? relativePath, int? targetFileId, string fileName, long length, string? userId, CancellationToken cancellationToken = default)
    {
        static UploadTargetDto Fail(string message) => new() { ErrorMessage = message };

        if (!await dbContext.Activities.AnyAsync(a => a.Id == activityId, cancellationToken))
            return Fail("Aktiviteten blev ikke fundet.");

        // Browsers may send a full path in the file name — only the last segment is the name.
        var rawName = fileName.Replace('\\', '/').Split('/').Last();
        var name = NormalizeName(rawName, out var nameError);
        if (name is null)
            return Fail($"{rawName}: {nameError}");
        if (ActivityFileRules.IsBlocked(name))
            return Fail($"{name}: programfiler og scripts kan ikke uploades.");
        if (length > ActivityFileRules.MaxFileSizeBytes)
            return Fail($"{name} er større end {ByteSizeHelper.FormatDanish(ActivityFileRules.MaxFileSizeBytes)}.");

        if (targetFileId.HasValue)
        {
            var target = await dbContext.ActivityFiles.AsNoTracking()
                .Where(f => f.Id == targetFileId.Value && f.ActivityId == activityId)
                .Select(f => new { f.Id, f.FolderId })
                .FirstOrDefaultAsync(cancellationToken);
            return target is null
                ? Fail("Filen blev ikke fundet.")
                : new UploadTargetDto { Name = name, FolderId = target.FolderId, ExistingFileId = target.Id };
        }

        if (folderId.HasValue && !await dbContext.ActivityFolders.AnyAsync(f => f.Id == folderId.Value && f.ActivityId == activityId, cancellationToken))
            return Fail("Mappen blev ikke fundet.");

        // "Billeder/2026/foto.jpg" from a dropped folder → create "Billeder" and "2026" as needed.
        var segments = (relativePath ?? string.Empty).Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments.Take(Math.Max(0, segments.Length - 1)))
        {
            var folderName = NormalizeName(segment, out var folderError);
            if (folderName is null)
                return Fail($"{segment}: {folderError}");

            var folderResult = await GetOrCreateFolderAsync(activityId, folderId, folderName, userId, cancellationToken);
            if (!folderResult.Success)
                return Fail(folderResult.ErrorMessage!);
            folderId = folderResult.Id;
        }

        var existingFileId = await FindFileIdByNameAsync(activityId, folderId, name, cancellationToken);
        if (existingFileId is null && await FindFolderByNameAsync(activityId, folderId, name, cancellationToken) is not null)
            return Fail($"Der findes allerede en mappe med navnet \"{name}\".");

        return new UploadTargetDto { Name = name, FolderId = folderId, ExistingFileId = existingFileId };
    }

    public async Task<ActivityFileVersionAddedDto> AddVersionAsync(int activityId, int? existingFileId, int? folderId, string name, int storedFileId, string? userId, CancellationToken cancellationToken = default)
    {
        // The same name may have been uploaded in the meantime (e.g. several files of a dropped folder at once).
        existingFileId ??= await FindFileIdByNameAsync(activityId, folderId, name, cancellationToken);

        var file = existingFileId.HasValue
            ? await dbContext.ActivityFiles.FirstAsync(f => f.Id == existingFileId.Value && f.ActivityId == activityId, cancellationToken)
            : dbContext.ActivityFiles.Add(new ActivityFile { ActivityId = activityId, FolderId = folderId, FileName = name }).Entity;

        var nextVersion = file.Id == 0
            ? 1
            : (await dbContext.ActivityFileVersions.Where(v => v.ActivityFileId == file.Id).MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0) + 1;

        dbContext.Entry(file).State = file.Id == 0 ? EntityState.Added : EntityState.Modified;
        dbContext.ActivityFileVersions.Add(new ActivityFileVersion
        {
            ActivityFile = file,
            FileMetadataId = storedFileId,
            VersionNumber = nextVersion,
            UploadedByUserId = userId
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Activity {ActivityId}: {FileName} got version {Version} (file {FileId})", activityId, file.FileName, nextVersion, file.Id);
        return new ActivityFileVersionAddedDto { FileId = file.Id, FileName = file.FileName, VersionNumber = nextVersion };
    }

    public async Task<RestoreSourceDto?> GetRestoreSourceAsync(int activityId, int versionId, CancellationToken cancellationToken = default)
    {
        var version = await dbContext.ActivityFileVersions.AsNoTracking()
            .Where(v => v.Id == versionId && v.ActivityFile.ActivityId == activityId)
            .Select(v => new
            {
                v.ActivityFileId,
                v.ActivityFile.FileName,
                v.FileMetadataId,
                v.VersionNumber,
                MaxVersion = v.ActivityFile.Versions.Max(x => x.VersionNumber)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return version is null ? null : new RestoreSourceDto
        {
            FileId = version.ActivityFileId,
            FileName = version.FileName,
            StoredFileId = version.FileMetadataId,
            IsLatest = version.VersionNumber == version.MaxVersion
        };
    }

    // ─── Folders / rename / move ─────────────────────────────────────────────

    public async Task<ActivityFileActionResultDto> CreateFolderAsync(int activityId, int? parentFolderId, string name, string? userId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Activities.AnyAsync(a => a.Id == activityId, cancellationToken))
            return ActivityFileActionResultDto.Fail("Aktiviteten blev ikke fundet.");

        var cleaned = NormalizeName(name, out var error);
        if (cleaned is null)
            return ActivityFileActionResultDto.Fail(error!);

        if (parentFolderId.HasValue && !await dbContext.ActivityFolders.AnyAsync(f => f.Id == parentFolderId.Value && f.ActivityId == activityId, cancellationToken))
            return ActivityFileActionResultDto.Fail("Mappen blev ikke fundet.");

        if (await IsNameTakenAsync(activityId, parentFolderId, cleaned, null, null, cancellationToken))
            return ActivityFileActionResultDto.Fail($"Der findes allerede en fil eller mappe med navnet \"{cleaned}\".");

        var folder = new ActivityFolder { ActivityId = activityId, ParentFolderId = parentFolderId, Name = cleaned, CreatedByUserId = userId };
        dbContext.ActivityFolders.Add(folder);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Activity {ActivityId}: created folder {FolderName} ({FolderId})", activityId, folder.Name, folder.Id);
        return ActivityFileActionResultDto.Ok(folder.Id);
    }

    public async Task<ActivityFileActionResultDto> RenameFolderAsync(int activityId, int folderId, string name, CancellationToken cancellationToken = default)
    {
        var folder = await dbContext.ActivityFolders.FirstOrDefaultAsync(f => f.Id == folderId && f.ActivityId == activityId, cancellationToken);
        if (folder is null)
            return ActivityFileActionResultDto.Fail("Mappen blev ikke fundet.");

        var cleaned = NormalizeName(name, out var error);
        if (cleaned is null)
            return ActivityFileActionResultDto.Fail(error!);

        if (await IsNameTakenAsync(activityId, folder.ParentFolderId, cleaned, null, folder.Id, cancellationToken))
            return ActivityFileActionResultDto.Fail($"Der findes allerede en fil eller mappe med navnet \"{cleaned}\".");

        folder.Name = cleaned;
        dbContext.Entry(folder).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ActivityFileActionResultDto.Ok(folder.Id);
    }

    public async Task<ActivityFileActionResultDto> RenameFileAsync(int activityId, int fileId, string name, CancellationToken cancellationToken = default)
    {
        var file = await dbContext.ActivityFiles.FirstOrDefaultAsync(f => f.Id == fileId && f.ActivityId == activityId, cancellationToken);
        if (file is null)
            return ActivityFileActionResultDto.Fail("Filen blev ikke fundet.");

        var cleaned = NormalizeName(name, out var error);
        if (cleaned is null)
            return ActivityFileActionResultDto.Fail(error!);

        if (ActivityFileRules.IsBlocked(cleaned))
            return ActivityFileActionResultDto.Fail("Filtypen er ikke tilladt.");

        if (await IsNameTakenAsync(activityId, file.FolderId, cleaned, file.Id, null, cancellationToken))
            return ActivityFileActionResultDto.Fail($"Der findes allerede en fil eller mappe med navnet \"{cleaned}\".");

        file.FileName = cleaned;
        dbContext.Entry(file).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ActivityFileActionResultDto.Ok(file.Id);
    }

    public async Task<MoveActivityFilesResultDto> MoveAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, int? targetFolderId, CancellationToken cancellationToken = default)
    {
        var result = new MoveActivityFilesResultDto();
        var folders = await LoadFoldersAsync(activityId, cancellationToken);

        if (targetFolderId.HasValue && !folders.ContainsKey(targetFolderId.Value))
        {
            result.Skipped.Add("Destinationsmappen blev ikke fundet");
            return result;
        }

        // A folder can't be moved into itself or anywhere below itself.
        var targetAndAncestors = GetPath(folders, targetFolderId).Select(f => f.Id).ToHashSet();

        var takenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        takenNames.UnionWith(folders.Values.Where(f => f.ParentId == targetFolderId).Select(f => f.Name));
        takenNames.UnionWith(await dbContext.ActivityFiles.Where(f => f.ActivityId == activityId && f.FolderId == targetFolderId).Select(f => f.FileName).ToListAsync(cancellationToken));

        var folderIdList = folderIds.Distinct().ToList();
        var folderEntities = await dbContext.ActivityFolders.Where(f => f.ActivityId == activityId && folderIdList.Contains(f.Id)).ToListAsync(cancellationToken);
        foreach (var folder in folderEntities)
        {
            if (folder.ParentFolderId == targetFolderId)
                continue;
            if (targetAndAncestors.Contains(folder.Id))
            {
                result.Skipped.Add($"{folder.Name} (en mappe kan ikke flyttes ind i sig selv)");
                continue;
            }
            if (!takenNames.Add(folder.Name))
            {
                result.Skipped.Add($"{folder.Name} (navnet findes allerede i destinationen)");
                continue;
            }

            folder.ParentFolderId = targetFolderId;
            result.MovedCount++;
        }

        var fileIdList = fileIds.Distinct().ToList();
        var fileEntities = await dbContext.ActivityFiles.Where(f => f.ActivityId == activityId && fileIdList.Contains(f.Id)).ToListAsync(cancellationToken);
        foreach (var file in fileEntities)
        {
            if (file.FolderId == targetFolderId)
                continue;
            if (!takenNames.Add(file.FileName))
            {
                result.Skipped.Add($"{file.FileName} (navnet findes allerede i destinationen)");
                continue;
            }

            file.FolderId = targetFolderId;
            result.MovedCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Activity {ActivityId}: moved {Count} item(s) to folder {TargetFolderId}", activityId, result.MovedCount, targetFolderId);
        return result;
    }

    // ─── Delete ──────────────────────────────────────────────────────────────

    public async Task<DeleteActivityFilesSummaryDto> GetDeleteSummaryAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken = default)
    {
        var (folderList, fileList) = await ResolveSelectionAsync(activityId, fileIds, folderIds, cancellationToken);
        var sizes = await dbContext.ActivityFileVersions.AsNoTracking()
            .Where(v => fileList.Contains(v.ActivityFileId))
            .Select(v => v.FileMetadata.FileSizeBytes)
            .ToListAsync(cancellationToken);

        return new DeleteActivityFilesSummaryDto { FolderCount = folderList.Count, FileCount = fileList.Count, VersionCount = sizes.Count, TotalBytes = sizes.Sum() };
    }

    public async Task<DeleteActivityFilesResultDto> DeleteAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken = default)
    {
        var (folderList, fileList) = await ResolveSelectionAsync(activityId, fileIds, folderIds, cancellationToken);
        var versions = await dbContext.ActivityFileVersions.AsNoTracking()
            .Where(v => fileList.Contains(v.ActivityFileId))
            .Select(v => new { v.FileMetadataId, v.FileMetadata.FileSizeBytes })
            .ToListAsync(cancellationToken);

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            await dbContext.ActivityFileVersions.Where(v => fileList.Contains(v.ActivityFileId)).ExecuteDeleteAsync(cancellationToken);
            await dbContext.ActivityFiles.Where(f => fileList.Contains(f.Id)).ExecuteDeleteAsync(cancellationToken);
            await dbContext.ActivityFolders.Where(f => folderList.Contains(f.Id)).ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        logger.LogInformation("Activity {ActivityId}: deleted {FolderCount} folder(s), {FileCount} file(s), {VersionCount} version(s)",
            activityId, folderList.Count, fileList.Count, versions.Count);

        return new DeleteActivityFilesResultDto
        {
            Summary = new DeleteActivityFilesSummaryDto
            {
                FolderCount = folderList.Count,
                FileCount = fileList.Count,
                VersionCount = versions.Count,
                TotalBytes = versions.Sum(v => v.FileSizeBytes)
            },
            StoredFileIds = versions.Select(v => v.FileMetadataId).ToList()
        };
    }

    public async Task<List<int>> DeleteAllForActivityAsync(int activityId, CancellationToken cancellationToken = default)
    {
        var folderIds = await dbContext.ActivityFolders.Where(f => f.ActivityId == activityId).Select(f => f.Id).ToListAsync(cancellationToken);
        var fileIds = await dbContext.ActivityFiles.Where(f => f.ActivityId == activityId).Select(f => f.Id).ToListAsync(cancellationToken);
        if (folderIds.Count == 0 && fileIds.Count == 0)
            return [];

        return (await DeleteAsync(activityId, fileIds, folderIds, cancellationToken)).StoredFileIds;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Dictionary<int, FolderNode>> LoadFoldersAsync(int activityId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.ActivityFolders.AsNoTracking()
            .Where(f => f.ActivityId == activityId)
            .Select(f => new { f.Id, f.ParentFolderId, f.Name, Modified = f.UpdatedAt })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(f => f.Id, f => new FolderNode(f.Id, f.ParentFolderId, f.Name, f.Modified));
    }

    /// <summary>Folders from the root down to (and including) folderId. Empty for the root itself.</summary>
    private static List<FolderNode> GetPath(Dictionary<int, FolderNode> folders, int? folderId)
    {
        var path = new List<FolderNode>();
        var guard = 0;
        while (folderId.HasValue && folders.TryGetValue(folderId.Value, out var node) && guard++ < 256)
        {
            path.Insert(0, node);
            folderId = node.ParentId;
        }
        return path;
    }

    private static string PathText(Dictionary<int, FolderNode> folders, int? folderId)
        => string.Join(" / ", GetPath(folders, folderId).Select(f => f.Name));

    /// <summary>The given folders plus everything below them.</summary>
    private static HashSet<int> GetDescendants(Dictionary<int, FolderNode> folders, IEnumerable<int> roots)
    {
        var result = new HashSet<int>();
        var queue = new Queue<int>(roots.Where(folders.ContainsKey));
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!result.Add(id))
                continue;
            foreach (var child in folders.Values.Where(f => f.ParentId == id))
                queue.Enqueue(child.Id);
        }
        return result;
    }

    /// <summary>Expands a selection to every folder and file it covers (sub folders included), scoped to the activity.</summary>
    private async Task<(List<int> FolderIds, List<int> FileIds)> ResolveSelectionAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken cancellationToken)
    {
        var folders = await LoadFoldersAsync(activityId, cancellationToken);
        var folderList = GetDescendants(folders, folderIds).ToList();
        var selectedFiles = fileIds.Distinct().ToList();

        var fileList = await dbContext.ActivityFiles
            .Where(f => f.ActivityId == activityId && (selectedFiles.Contains(f.Id) || (f.FolderId != null && folderList.Contains(f.FolderId.Value))))
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        return (folderList, fileList);
    }

    /// <summary>Trims and validates a file/folder name. Returns null (and an error text) when it can't be used.</summary>
    private static string? NormalizeName(string? raw, out string? error)
    {
        error = null;
        var name = (raw ?? string.Empty).Trim().TrimEnd('.', ' ');

        if (name.Length == 0)
        {
            error = "Angiv et navn.";
            return null;
        }
        if (name.Length > ActivityFileRules.MaxNameLength)
        {
            error = $"Navnet må højst være {ActivityFileRules.MaxNameLength} tegn.";
            return null;
        }
        if (name.IndexOfAny(ActivityFileRules.InvalidNameChars) >= 0 || name.Any(char.IsControl))
        {
            error = "Navnet må ikke indeholde \\ / : * ? \" < > |";
            return null;
        }
        return name;
    }

    private async Task<bool> IsNameTakenAsync(int activityId, int? folderId, string name, int? exceptFileId, int? exceptFolderId, CancellationToken cancellationToken)
    {
        var fileNames = await dbContext.ActivityFiles
            .Where(f => f.ActivityId == activityId && f.FolderId == folderId && (exceptFileId == null || f.Id != exceptFileId))
            .Select(f => f.FileName)
            .ToListAsync(cancellationToken);
        var folderNames = await dbContext.ActivityFolders
            .Where(f => f.ActivityId == activityId && f.ParentFolderId == folderId && (exceptFolderId == null || f.Id != exceptFolderId))
            .Select(f => f.Name)
            .ToListAsync(cancellationToken);

        return fileNames.Concat(folderNames).Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    // SQLite's lower()/LIKE only fold ASCII, so names are compared in memory to also treat "Æ"/"æ" as equal.
    private async Task<int?> FindFileIdByNameAsync(int activityId, int? folderId, string name, CancellationToken cancellationToken)
    {
        var candidates = await dbContext.ActivityFiles
            .Where(f => f.ActivityId == activityId && f.FolderId == folderId)
            .Select(f => new { f.Id, f.FileName })
            .ToListAsync(cancellationToken);
        return candidates.FirstOrDefault(f => string.Equals(f.FileName, name, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private async Task<int?> FindFolderByNameAsync(int activityId, int? parentFolderId, string name, CancellationToken cancellationToken)
    {
        var candidates = await dbContext.ActivityFolders
            .Where(f => f.ActivityId == activityId && f.ParentFolderId == parentFolderId)
            .Select(f => new { f.Id, f.Name })
            .ToListAsync(cancellationToken);
        return candidates.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private async Task<ActivityFileActionResultDto> GetOrCreateFolderAsync(int activityId, int? parentFolderId, string name, string? userId, CancellationToken cancellationToken)
    {
        var existing = await FindFolderByNameAsync(activityId, parentFolderId, name, cancellationToken);
        return existing.HasValue
            ? ActivityFileActionResultDto.Ok(existing)
            : await CreateFolderAsync(activityId, parentFolderId, name, userId, cancellationToken);
    }

    private static string SanitizeForFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(ActivityFileRules.InvalidNameChars).ToHashSet();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return cleaned.Length == 0 ? "filer" : cleaned;
    }
}
