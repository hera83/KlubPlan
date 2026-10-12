namespace web.Repositories.AppSettings.Interfaces;

/// <summary>The app's key/value settings (table AppSettings, keys in AppSettingKeys).</summary>
public interface IAppSettingRepository
{
    /// <summary>The stored value, or null when the key isn't set.</summary>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates the setting.</summary>
    Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default);
}
