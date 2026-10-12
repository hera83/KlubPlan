namespace web.Constants;

/// <summary>
/// Defines standard subcategories for files stored under app_files/.
/// Each category corresponds to a subfolder: app_files/{category}/
/// </summary>
public static class FileCategories
{
    /// <summary>User profile pictures — app_files/avatars/</summary>
    public const string Avatars = "avatars";

    /// <summary>General user uploads — app_files/uploads/</summary>
    public const string Uploads = "uploads";

    /// <summary>System-generated export files — app_files/exports/</summary>
    public const string Exports = "exports";

    /// <summary>Temporary files (can be cleaned up) — app_files/temp/</summary>
    public const string Temp = "temp";

    /// <summary>Meeting attachments and recordings — app_files/meetings/</summary>
    public const string Meetings = "meetings";

    /// <summary>Communication message attachments (email only) — app_files/communication/</summary>
    public const string Communication = "communication";

    /// <summary>Activity document hotel (Filer-fanen på en aktivitet) — app_files/activities/</summary>
    public const string Activities = "activities";

    /// <summary>Images and videos shown on info screens (Værktøjer → Infoskærme) — app_files/infoscreens/</summary>
    public const string InfoScreens = "infoscreens";

    /// <summary>Images placed on designed labels (Værktøjer → Labels) — app_files/labels/</summary>
    public const string Labels = "labels";
}
