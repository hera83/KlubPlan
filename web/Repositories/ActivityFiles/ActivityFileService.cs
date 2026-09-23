using System.Globalization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.ActivityFiles.Dtos;
using web.Repositories.ActivityFiles.Interfaces;
using web.ViewModels;

namespace web.Repositories.ActivityFiles
{
    public class ActivityFileService : IActivityFileService
    {
        private static readonly FileExtensionContentTypeProvider ContentTypes = new();
        private static readonly StringComparer NameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("da-DK"), ignoreCase: true);

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<ActivityFileService> _logger;

        public ActivityFileService(ApplicationDbContext context, IWebHostEnvironment env, IConfiguration config, ILogger<ActivityFileService> logger)
        {
            _context = context;
            _env = env;
            _config = config;
            _logger = logger;
        }

        private sealed record FolderNode(int Id, int? ParentId, string Name, DateTime ModifiedAtUtc);

        // ─── Read ────────────────────────────────────────────────────────────────

        public async Task<ActivityFilesViewModel?> GetFolderAsync(int activityId, int? folderId, string? searchText, CancellationToken ct = default)
        {
            if (!await _context.Activities.AnyAsync(a => a.Id == activityId, ct))
                return null;

            var folders = await LoadFoldersAsync(activityId, ct);
            if (folderId.HasValue && !folders.ContainsKey(folderId.Value))
                folderId = null;

            var model = new ActivityFilesViewModel
            {
                ActivityId = activityId,
                CurrentFolderId = folderId,
                SearchText = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim()
            };

            model.Breadcrumbs.Add(new ActivityFolderBreadcrumbViewModel { Id = null, Name = "Filer" });
            model.Breadcrumbs.AddRange(GetPath(folders, folderId).Select(f => new ActivityFolderBreadcrumbViewModel { Id = f.Id, Name = f.Name }));

            var fileCounts = await _context.ActivityFiles
                .Where(f => f.ActivityId == activityId)
                .GroupBy(f => f.FolderId)
                .Select(g => new { FolderId = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            var countsByFolder = fileCounts.Where(c => c.FolderId.HasValue).ToDictionary(c => c.FolderId!.Value, c => c.Count);

            model.TotalFileCount = fileCounts.Sum(c => c.Count);
            model.TotalSizeBytes = await _context.ActivityFileVersions
                .Where(v => v.ActivityFile.ActivityId == activityId)
                .SumAsync(v => (long?)v.FileMetadata.FileSizeBytes, ct) ?? 0;

            var term = model.SearchText;
            var folderRows = term is null
                ? folders.Values.Where(f => f.ParentId == folderId)
                : folders.Values.Where(f => f.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

            model.Folders = folderRows
                .OrderBy(f => f.Name, NameComparer)
                .Select(f => new ActivityFolderItemViewModel
                {
                    Id = f.Id,
                    Name = f.Name,
                    ItemCount = folders.Values.Count(c => c.ParentId == f.Id) + countsByFolder.GetValueOrDefault(f.Id),
                    ModifiedAtUtc = f.ModifiedAtUtc,
                    ParentPath = term is null ? null : PathText(folders, f.ParentId)
                })
                .ToList();

            var fileQuery = _context.ActivityFiles.AsNoTracking().Where(f => f.ActivityId == activityId);
            if (term is null)
                fileQuery = fileQuery.Where(f => f.FolderId == folderId);

            var fileRows = await fileQuery
                .Select(f => new
                {
                    f.Id,
                    f.FileName,
                    f.FolderId,
                    ModifiedAtUtc = f.UpdatedAtUtc ?? f.CreatedAtUtc,
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
                .ToListAsync(ct);

            model.Files = fileRows
                .Where(f => f.Current is not null)
                .Where(f => term is null || f.FileName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.FileName, NameComparer)
                .Select(f => new ActivityFileItemViewModel
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    IconClass = FileIconHelper.GetIconClass(f.FileName),
                    FolderId = f.FolderId,
                    FolderPath = term is null ? null : PathText(folders, f.FolderId),
                    CurrentVersionId = f.Current!.Id,
                    CurrentVersionNumber = f.Current.VersionNumber,
                    VersionCount = f.VersionCount,
                    SizeBytes = f.Current.FileSizeBytes,
                    ContentType = f.Current.ContentType,
                    CanPreview = FileIconHelper.CanPreview(f.Current.ContentType),
                    ModifiedAtUtc = f.ModifiedAtUtc,
                    ModifiedByName = f.Current.UploadedBy
                })
                .ToList();

            return model;
        }

        public Task<int> CountFilesAsync(int activityId, CancellationToken ct = default)
            => _context.ActivityFiles.CountAsync(f => f.ActivityId == activityId, ct);

        public async Task<List<ActivityFolderTreeItemViewModel>> GetFolderTreeAsync(int activityId, CancellationToken ct = default)
        {
            var folders = await LoadFoldersAsync(activityId, ct);
            var result = new List<ActivityFolderTreeItemViewModel> { new() { Id = null, Name = "Filer", Depth = 0 } };

            void Walk(int? parentId, int depth)
            {
                foreach (var child in folders.Values.Where(f => f.ParentId == parentId).OrderBy(f => f.Name, NameComparer))
                {
                    result.Add(new ActivityFolderTreeItemViewModel { Id = child.Id, Name = child.Name, Depth = depth });
                    Walk(child.Id, depth + 1);
                }
            }

            Walk(null, 1);
            return result;
        }

        public async Task<ActivityFileVersionsViewModel?> GetVersionsAsync(int activityId, int fileId, CancellationToken ct = default)
        {
            var file = await _context.ActivityFiles
                .AsNoTracking()
                .Where(f => f.Id == fileId && f.ActivityId == activityId)
                .Select(f => new
                {
                    f.Id,
                    f.FileName,
                    Versions = f.Versions
                        .OrderByDescending(v => v.VersionNumber)
                        .Select(v => new
                        {
                            v.Id,
                            v.VersionNumber,
                            v.FileMetadata.FileSizeBytes,
                            v.FileMetadata.ContentType,
                            v.CreatedAtUtc,
                            UploadedBy = v.UploadedByUser != null ? v.UploadedByUser.DisplayName : null
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (file is null)
                return null;

            return new ActivityFileVersionsViewModel
            {
                ActivityId = activityId,
                FileId = file.Id,
                FileName = file.FileName,
                IconClass = FileIconHelper.GetIconClass(file.FileName),
                CanPreview = file.Versions.Count > 0 && FileIconHelper.CanPreview(file.Versions[0].ContentType),
                Versions = file.Versions.Select((v, i) => new ActivityFileVersionItemViewModel
                {
                    Id = v.Id,
                    VersionNumber = v.VersionNumber,
                    SizeBytes = v.FileSizeBytes,
                    UploadedByName = v.UploadedBy,
                    CreatedAtUtc = v.CreatedAtUtc,
                    IsCurrent = i == 0
                }).ToList()
            };
        }

        public async Task<ActivityFileDownloadDto?> GetDownloadAsync(int activityId, int fileId, int? versionId, CancellationToken ct = default)
        {
            var versions = _context.ActivityFileVersions
                .AsNoTracking()
                .Where(v => v.ActivityFileId == fileId && v.ActivityFile.ActivityId == activityId);

            var version = await (versionId.HasValue
                    ? versions.Where(v => v.Id == versionId.Value)
                    : versions.OrderByDescending(v => v.VersionNumber))
                .Select(v => new
                {
                    v.VersionNumber,
                    v.ActivityFile.FileName,
                    v.FileMetadata.StoredPath,
                    v.FileMetadata.ContentType,
                    MaxVersion = v.ActivityFile.Versions.Max(x => x.VersionNumber)
                })
                .FirstOrDefaultAsync(ct);

            if (version is null)
                return null;

            var fullPath = ToFullPath(version.StoredPath);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Activity file {FileId} version {Version} is missing on disk", fileId, version.VersionNumber);
                return null;
            }

            var fileName = version.VersionNumber == version.MaxVersion
                ? version.FileName
                : $"{Path.GetFileNameWithoutExtension(version.FileName)} (v{version.VersionNumber}){Path.GetExtension(version.FileName)}";

            return new ActivityFileDownloadDto
            {
                FullPath = fullPath,
                ContentType = string.IsNullOrEmpty(version.ContentType) ? "application/octet-stream" : version.ContentType,
                FileName = fileName,
                CanPreview = FileIconHelper.CanPreview(version.ContentType)
            };
        }

        public async Task<ActivityFileZipDto?> GetZipAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken ct = default)
        {
            var activityTitle = await _context.Activities.Where(a => a.Id == activityId).Select(a => a.Title).FirstOrDefaultAsync(ct);
            if (activityTitle is null)
                return null;

            var folders = await LoadFoldersAsync(activityId, ct);

            // Drop selected folders that sit inside another selected folder — they're included via their ancestor.
            var selectedRoots = folderIds.Where(folders.ContainsKey).Distinct().ToHashSet();
            selectedRoots.RemoveWhere(id => GetPath(folders, folders[id].ParentId).Any(a => selectedRoots.Contains(a.Id)));

            var coveredFolders = GetDescendants(folders, selectedRoots);
            var coveredList = coveredFolders.ToList();
            var fileIdList = fileIds.Distinct().ToList();

            var files = await _context.ActivityFiles
                .AsNoTracking()
                .Where(f => f.ActivityId == activityId && (fileIdList.Contains(f.Id) || (f.FolderId != null && coveredList.Contains(f.FolderId.Value))))
                .Select(f => new
                {
                    f.FileName,
                    f.FolderId,
                    StoredPath = f.Versions.OrderByDescending(v => v.VersionNumber).Select(v => v.FileMetadata.StoredPath).FirstOrDefault()
                })
                .ToListAsync(ct);

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

            var result = new ActivityFileZipDto();
            var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folderId in coveredFolders)
            {
                var path = RelativeFolderPath(folderId) + "/";
                if (usedPaths.Add(path))
                    result.EmptyFolders.Add(path);
            }

            foreach (var file in files.Where(f => f.StoredPath is not null))
            {
                var inCoveredFolder = file.FolderId.HasValue && coveredFolders.Contains(file.FolderId.Value);
                var entryPath = inCoveredFolder ? $"{RelativeFolderPath(file.FolderId!.Value)}/{file.FileName}" : file.FileName;

                // Loose files from different folders (e.g. picked in search results) can share a name.
                var unique = entryPath;
                for (var n = 2; !usedPaths.Add(unique); n++)
                {
                    unique = Path.ChangeExtension(entryPath, null) + $" ({n})" + Path.GetExtension(entryPath);
                }

                var fullPath = ToFullPath(file.StoredPath!);
                if (File.Exists(fullPath))
                    result.Entries.Add(new ActivityFileZipEntryDto { EntryPath = unique, FullPath = fullPath });
            }

            if (result.Entries.Count == 0 && result.EmptyFolders.Count == 0)
                return null;

            var zipBaseName = selectedRoots.Count == 1 && fileIdList.Count == 0
                ? folders[selectedRoots.First()].Name
                : $"{activityTitle} - filer";
            result.ZipFileName = SanitizeForFileName(zipBaseName) + ".zip";

            return result;
        }

        // ─── Upload / versions ───────────────────────────────────────────────────

        public async Task<UploadActivityFileResponseDto> UploadAsync(UploadActivityFileRequestDto request, CancellationToken ct = default)
        {
            UploadActivityFileResponseDto Fail(string message) => new() { Success = false, ErrorMessage = message };

            if (!await _context.Activities.AnyAsync(a => a.Id == request.ActivityId, ct))
                return Fail("Aktiviteten blev ikke fundet.");

            // Browsers may send a full path in the file name — only the last segment is the name.
            var rawName = request.FileName.Replace('\\', '/').Split('/').Last();
            var name = NormalizeName(rawName, out var nameError);
            if (name is null)
                return Fail($"{rawName}: {nameError}");

            if (ActivityFileRules.IsBlocked(name))
                return Fail($"{name}: programfiler og scripts kan ikke uploades.");

            if (request.Length > ActivityFileRules.MaxFileSizeBytes)
                return Fail($"{name} er større end {ByteSizeFormatter.FormatDanish(ActivityFileRules.MaxFileSizeBytes)}.");

            var now = DateTime.UtcNow;
            ActivityFile? file;

            if (request.TargetFileId.HasValue)
            {
                file = await _context.ActivityFiles.FirstOrDefaultAsync(f => f.Id == request.TargetFileId.Value && f.ActivityId == request.ActivityId, ct);
                if (file is null)
                    return Fail("Filen blev ikke fundet.");
            }
            else
            {
                var folderId = request.FolderId;
                if (folderId.HasValue && !await _context.ActivityFolders.AnyAsync(f => f.Id == folderId.Value && f.ActivityId == request.ActivityId, ct))
                    return Fail("Mappen blev ikke fundet.");

                // "Billeder/2026/foto.jpg" from a dropped folder → create "Billeder" and "2026" as needed.
                var segments = (request.RelativePath ?? string.Empty).Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                foreach (var segment in segments.Take(Math.Max(0, segments.Length - 1)))
                {
                    var folderName = NormalizeName(segment, out var folderError);
                    if (folderName is null)
                        return Fail($"{segment}: {folderError}");

                    var folderResult = await GetOrCreateFolderAsync(request.ActivityId, folderId, folderName, request.UserId, ct);
                    if (!folderResult.Success)
                        return Fail(folderResult.ErrorMessage!);
                    folderId = folderResult.Id;
                }

                file = await FindFileByNameAsync(request.ActivityId, folderId, name, ct);
                if (file is null)
                {
                    if (await FindFolderByNameAsync(request.ActivityId, folderId, name, ct) is not null)
                        return Fail($"Der findes allerede en mappe med navnet \"{name}\".");

                    file = new ActivityFile { ActivityId = request.ActivityId, FolderId = folderId, FileName = name, CreatedAtUtc = now };
                    _context.ActivityFiles.Add(file);
                }
            }

            var nextVersion = file.Id == 0
                ? 1
                : (await _context.ActivityFileVersions.Where(v => v.ActivityFileId == file.Id).MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0) + 1;

            var metadata = await StorePhysicalFileAsync(request.ActivityId, request.Content, name, request.UserId, ct);
            try
            {
                file.UpdatedAtUtc = now;
                _context.FileMetadata.Add(metadata);
                _context.ActivityFileVersions.Add(new ActivityFileVersion
                {
                    ActivityFile = file,
                    FileMetadata = metadata,
                    VersionNumber = nextVersion,
                    UploadedByUserId = request.UserId,
                    CreatedAtUtc = now
                });
                await _context.SaveChangesAsync(ct);
            }
            catch
            {
                TryDeletePhysicalFile(metadata.StoredPath);
                throw;
            }

            _logger.LogInformation("Activity {ActivityId}: uploaded {FileName} as version {Version} (file {FileId})", request.ActivityId, file.FileName, nextVersion, file.Id);

            return new UploadActivityFileResponseDto { Success = true, FileId = file.Id, FileName = file.FileName, VersionNumber = nextVersion };
        }

        public async Task<ActivityFileActionResultDto> RestoreVersionAsync(int activityId, int versionId, string? userId, CancellationToken ct = default)
        {
            var version = await _context.ActivityFileVersions
                .Include(v => v.ActivityFile)
                .Include(v => v.FileMetadata)
                .FirstOrDefaultAsync(v => v.Id == versionId && v.ActivityFile.ActivityId == activityId, ct);
            if (version is null)
                return ActivityFileActionResultDto.Fail("Versionen blev ikke fundet.");

            var sourcePath = ToFullPath(version.FileMetadata.StoredPath);
            if (!File.Exists(sourcePath))
                return ActivityFileActionResultDto.Fail("Filen til versionen findes ikke længere på disken.");

            var file = version.ActivityFile;
            var maxVersion = await _context.ActivityFileVersions.Where(v => v.ActivityFileId == file.Id).MaxAsync(v => v.VersionNumber, ct);
            if (version.VersionNumber == maxVersion)
                return ActivityFileActionResultDto.Fail("Versionen er allerede den nyeste.");

            FileMetadata metadata;
            await using (var source = File.OpenRead(sourcePath))
            {
                metadata = await StorePhysicalFileAsync(activityId, source, file.FileName, userId, ct);
            }

            try
            {
                var now = DateTime.UtcNow;
                file.UpdatedAtUtc = now;
                _context.FileMetadata.Add(metadata);
                _context.ActivityFileVersions.Add(new ActivityFileVersion
                {
                    ActivityFileId = file.Id,
                    FileMetadata = metadata,
                    VersionNumber = maxVersion + 1,
                    UploadedByUserId = userId,
                    CreatedAtUtc = now
                });
                await _context.SaveChangesAsync(ct);
            }
            catch
            {
                TryDeletePhysicalFile(metadata.StoredPath);
                throw;
            }

            _logger.LogInformation("Activity {ActivityId}: restored {FileName} version {Old} as version {New}", activityId, file.FileName, version.VersionNumber, maxVersion + 1);
            return ActivityFileActionResultDto.Ok(maxVersion + 1);
        }

        // ─── Folders / rename / move ─────────────────────────────────────────────

        public async Task<ActivityFileActionResultDto> CreateFolderAsync(int activityId, int? parentFolderId, string name, string? userId, CancellationToken ct = default)
        {
            if (!await _context.Activities.AnyAsync(a => a.Id == activityId, ct))
                return ActivityFileActionResultDto.Fail("Aktiviteten blev ikke fundet.");

            var cleaned = NormalizeName(name, out var error);
            if (cleaned is null)
                return ActivityFileActionResultDto.Fail(error!);

            if (parentFolderId.HasValue && !await _context.ActivityFolders.AnyAsync(f => f.Id == parentFolderId.Value && f.ActivityId == activityId, ct))
                return ActivityFileActionResultDto.Fail("Mappen blev ikke fundet.");

            if (await IsNameTakenAsync(activityId, parentFolderId, cleaned, null, null, ct))
                return ActivityFileActionResultDto.Fail($"Der findes allerede en fil eller mappe med navnet \"{cleaned}\".");

            var folder = new ActivityFolder { ActivityId = activityId, ParentFolderId = parentFolderId, Name = cleaned, CreatedByUserId = userId, CreatedAtUtc = DateTime.UtcNow };
            _context.ActivityFolders.Add(folder);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Activity {ActivityId}: created folder {FolderName} ({FolderId})", activityId, folder.Name, folder.Id);
            return ActivityFileActionResultDto.Ok(folder.Id);
        }

        public async Task<ActivityFileActionResultDto> RenameFolderAsync(int activityId, int folderId, string name, CancellationToken ct = default)
        {
            var folder = await _context.ActivityFolders.FirstOrDefaultAsync(f => f.Id == folderId && f.ActivityId == activityId, ct);
            if (folder is null)
                return ActivityFileActionResultDto.Fail("Mappen blev ikke fundet.");

            var cleaned = NormalizeName(name, out var error);
            if (cleaned is null)
                return ActivityFileActionResultDto.Fail(error!);

            if (await IsNameTakenAsync(activityId, folder.ParentFolderId, cleaned, null, folder.Id, ct))
                return ActivityFileActionResultDto.Fail($"Der findes allerede en fil eller mappe med navnet \"{cleaned}\".");

            folder.Name = cleaned;
            folder.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return ActivityFileActionResultDto.Ok(folder.Id);
        }

        public async Task<ActivityFileActionResultDto> RenameFileAsync(int activityId, int fileId, string name, CancellationToken ct = default)
        {
            var file = await _context.ActivityFiles.FirstOrDefaultAsync(f => f.Id == fileId && f.ActivityId == activityId, ct);
            if (file is null)
                return ActivityFileActionResultDto.Fail("Filen blev ikke fundet.");

            var cleaned = NormalizeName(name, out var error);
            if (cleaned is null)
                return ActivityFileActionResultDto.Fail(error!);

            if (ActivityFileRules.IsBlocked(cleaned))
                return ActivityFileActionResultDto.Fail("Filtypen er ikke tilladt.");

            if (await IsNameTakenAsync(activityId, file.FolderId, cleaned, file.Id, null, ct))
                return ActivityFileActionResultDto.Fail($"Der findes allerede en fil eller mappe med navnet \"{cleaned}\".");

            file.FileName = cleaned;
            file.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return ActivityFileActionResultDto.Ok(file.Id);
        }

        public async Task<MoveActivityFilesResponseDto> MoveAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, int? targetFolderId, CancellationToken ct = default)
        {
            var result = new MoveActivityFilesResponseDto();
            var folders = await LoadFoldersAsync(activityId, ct);

            if (targetFolderId.HasValue && !folders.ContainsKey(targetFolderId.Value))
            {
                result.Skipped.Add("Destinationsmappen blev ikke fundet");
                return result;
            }

            // A folder can't be moved into itself or anywhere below itself.
            var targetAndAncestors = GetPath(folders, targetFolderId).Select(f => f.Id).ToHashSet();

            var takenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            takenNames.UnionWith(folders.Values.Where(f => f.ParentId == targetFolderId).Select(f => f.Name));
            takenNames.UnionWith(await _context.ActivityFiles.Where(f => f.ActivityId == activityId && f.FolderId == targetFolderId).Select(f => f.FileName).ToListAsync(ct));

            var now = DateTime.UtcNow;
            var folderIdList = folderIds.Distinct().ToList();
            var folderEntities = await _context.ActivityFolders.Where(f => f.ActivityId == activityId && folderIdList.Contains(f.Id)).ToListAsync(ct);
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
                folder.UpdatedAtUtc = now;
                result.MovedCount++;
            }

            var fileIdList = fileIds.Distinct().ToList();
            var fileEntities = await _context.ActivityFiles.Where(f => f.ActivityId == activityId && fileIdList.Contains(f.Id)).ToListAsync(ct);
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
                file.UpdatedAtUtc = now;
                result.MovedCount++;
            }

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Activity {ActivityId}: moved {Count} item(s) to folder {TargetFolderId}", activityId, result.MovedCount, targetFolderId);
            return result;
        }

        // ─── Delete ──────────────────────────────────────────────────────────────

        public async Task<DeleteActivityFilesSummaryDto> GetDeleteSummaryAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken ct = default)
        {
            var (folderList, fileList) = await ResolveSelectionAsync(activityId, fileIds, folderIds, ct);
            var versions = await _context.ActivityFileVersions
                .Where(v => fileList.Contains(v.ActivityFileId))
                .Select(v => v.FileMetadata.FileSizeBytes)
                .ToListAsync(ct);

            return new DeleteActivityFilesSummaryDto
            {
                FolderCount = folderList.Count,
                FileCount = fileList.Count,
                VersionCount = versions.Count,
                TotalBytes = versions.Sum()
            };
        }

        public async Task<DeleteActivityFilesSummaryDto> DeleteAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken ct = default)
        {
            var (folderList, fileList) = await ResolveSelectionAsync(activityId, fileIds, folderIds, ct);
            var versions = await _context.ActivityFileVersions
                .Where(v => fileList.Contains(v.ActivityFileId))
                .Select(v => new { v.FileMetadataId, v.FileMetadata.FileSizeBytes, v.FileMetadata.StoredPath })
                .ToListAsync(ct);
            var metadataIds = versions.Select(v => v.FileMetadataId).ToList();

            await using (var tx = await _context.Database.BeginTransactionAsync(ct))
            {
                await _context.ActivityFileVersions.Where(v => fileList.Contains(v.ActivityFileId)).ExecuteDeleteAsync(ct);
                await _context.FileMetadata.Where(m => metadataIds.Contains(m.Id)).ExecuteDeleteAsync(ct);
                await _context.ActivityFiles.Where(f => fileList.Contains(f.Id)).ExecuteDeleteAsync(ct);
                await _context.ActivityFolders.Where(f => folderList.Contains(f.Id)).ExecuteDeleteAsync(ct);
                await tx.CommitAsync(ct);
            }

            // Physical files go only after the DB commit, so a failed commit never leaves rows pointing at nothing.
            foreach (var version in versions)
            {
                TryDeletePhysicalFile(version.StoredPath);
            }

            _logger.LogInformation("Activity {ActivityId}: deleted {FolderCount} folder(s), {FileCount} file(s), {VersionCount} version(s)",
                activityId, folderList.Count, fileList.Count, versions.Count);

            return new DeleteActivityFilesSummaryDto
            {
                FolderCount = folderList.Count,
                FileCount = fileList.Count,
                VersionCount = versions.Count,
                TotalBytes = versions.Sum(v => v.FileSizeBytes)
            };
        }

        public async Task DeleteAllForActivityAsync(int activityId, CancellationToken ct = default)
        {
            var folderIds = await _context.ActivityFolders.Where(f => f.ActivityId == activityId).Select(f => f.Id).ToListAsync(ct);
            var fileIds = await _context.ActivityFiles.Where(f => f.ActivityId == activityId).Select(f => f.Id).ToListAsync(ct);
            if (folderIds.Count > 0 || fileIds.Count > 0)
            {
                await DeleteAsync(activityId, fileIds, folderIds, ct);
            }

            try
            {
                var dir = Path.Combine(_env.ContentRootPath, FilesRoot, FileCategories.Activities, activityId.ToString(CultureInfo.InvariantCulture));
                if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not remove file directory for activity {ActivityId}", activityId);
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private string FilesRoot => _config["AppSettings:FilesPath"] ?? "App_files";

        private string ToFullPath(string storedPath) => Path.Combine(_env.ContentRootPath, storedPath);

        private async Task<Dictionary<int, FolderNode>> LoadFoldersAsync(int activityId, CancellationToken ct)
        {
            var rows = await _context.ActivityFolders
                .AsNoTracking()
                .Where(f => f.ActivityId == activityId)
                .Select(f => new { f.Id, f.ParentFolderId, f.Name, Modified = f.UpdatedAtUtc ?? f.CreatedAtUtc })
                .ToListAsync(ct);

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
        private async Task<(List<int> FolderIds, List<int> FileIds)> ResolveSelectionAsync(int activityId, IReadOnlyCollection<int> fileIds, IReadOnlyCollection<int> folderIds, CancellationToken ct)
        {
            var folders = await LoadFoldersAsync(activityId, ct);
            var folderList = GetDescendants(folders, folderIds).ToList();
            var selectedFiles = fileIds.Distinct().ToList();

            var fileList = await _context.ActivityFiles
                .Where(f => f.ActivityId == activityId && (selectedFiles.Contains(f.Id) || (f.FolderId != null && folderList.Contains(f.FolderId.Value))))
                .Select(f => f.Id)
                .ToListAsync(ct);

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

        private async Task<bool> IsNameTakenAsync(int activityId, int? folderId, string name, int? exceptFileId, int? exceptFolderId, CancellationToken ct)
        {
            var fileNames = await _context.ActivityFiles
                .Where(f => f.ActivityId == activityId && f.FolderId == folderId && (exceptFileId == null || f.Id != exceptFileId))
                .Select(f => f.FileName)
                .ToListAsync(ct);
            var folderNames = await _context.ActivityFolders
                .Where(f => f.ActivityId == activityId && f.ParentFolderId == folderId && (exceptFolderId == null || f.Id != exceptFolderId))
                .Select(f => f.Name)
                .ToListAsync(ct);

            return fileNames.Concat(folderNames).Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        // SQLite's lower()/LIKE only fold ASCII, so names are compared in memory to also treat "Æ"/"æ" as equal.
        private async Task<ActivityFile?> FindFileByNameAsync(int activityId, int? folderId, string name, CancellationToken ct)
        {
            var candidates = await _context.ActivityFiles
                .Where(f => f.ActivityId == activityId && f.FolderId == folderId)
                .Select(f => new { f.Id, f.FileName })
                .ToListAsync(ct);
            var match = candidates.FirstOrDefault(f => string.Equals(f.FileName, name, StringComparison.OrdinalIgnoreCase));
            return match is null ? null : await _context.ActivityFiles.FirstAsync(f => f.Id == match.Id, ct);
        }

        private async Task<int?> FindFolderByNameAsync(int activityId, int? parentFolderId, string name, CancellationToken ct)
        {
            var candidates = await _context.ActivityFolders
                .Where(f => f.ActivityId == activityId && f.ParentFolderId == parentFolderId)
                .Select(f => new { f.Id, f.Name })
                .ToListAsync(ct);
            return candidates.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))?.Id;
        }

        private async Task<ActivityFileActionResultDto> GetOrCreateFolderAsync(int activityId, int? parentFolderId, string name, string? userId, CancellationToken ct)
        {
            var existing = await FindFolderByNameAsync(activityId, parentFolderId, name, ct);
            if (existing.HasValue)
                return ActivityFileActionResultDto.Ok(existing);

            return await CreateFolderAsync(activityId, parentFolderId, name, userId, ct);
        }

        private async Task<FileMetadata> StorePhysicalFileAsync(int activityId, Stream content, string fileName, string? userId, CancellationToken ct)
        {
            var relativeDir = Path.Combine(FilesRoot, FileCategories.Activities, activityId.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Path.Combine(_env.ContentRootPath, relativeDir));

            var extension = Path.GetExtension(fileName);
            if (extension.Length > 20)
                extension = string.Empty;

            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var storedRelativePath = Path.Combine(relativeDir, storedFileName);
            var fullPath = ToFullPath(storedRelativePath);

            await using (var target = File.Create(fullPath))
            {
                await content.CopyToAsync(target, ct);
            }

            return new FileMetadata
            {
                OriginalFileName = fileName,
                StoredFileName = storedFileName,
                StoredPath = storedRelativePath,
                // The browser-supplied type isn't trusted — derive it from the extension.
                ContentType = ContentTypes.TryGetContentType(fileName, out var contentType) ? contentType : "application/octet-stream",
                FileSizeBytes = new FileInfo(fullPath).Length,
                OwnerId = userId,
                Category = FileCategories.Activities,
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        private void TryDeletePhysicalFile(string storedPath)
        {
            try
            {
                var fullPath = ToFullPath(storedPath);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not delete physical file {StoredPath}", storedPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Could not delete physical file {StoredPath}", storedPath);
            }
        }

        private static string SanitizeForFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars().Concat(ActivityFileRules.InvalidNameChars).ToHashSet();
            var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            return cleaned.Length == 0 ? "filer" : cleaned;
        }
    }
}
