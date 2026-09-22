namespace web.Constants
{
    /// <summary>
    /// Defines standard subcategories for files stored under App_files/.
    /// Each category corresponds to a subfolder: App_files/{category}/
    /// </summary>
    public static class FileCategories
    {
        /// <summary>User profile pictures — App_files/avatars/</summary>
        public const string Avatars = "avatars";

        /// <summary>General user uploads — App_files/uploads/</summary>
        public const string Uploads = "uploads";

        /// <summary>System-generated export files — App_files/exports/</summary>
        public const string Exports = "exports";

        /// <summary>Temporary files (can be cleaned up) — App_files/temp/</summary>
        public const string Temp = "temp";

        /// <summary>Meeting attachments and recordings — App_files/meetings/</summary>
        public const string Meetings = "meetings";

        /// <summary>Communication message attachments (email only) — App_files/communication/</summary>
        public const string Communication = "communication";
    }
}
