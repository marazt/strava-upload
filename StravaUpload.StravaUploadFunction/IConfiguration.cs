namespace StravaUpload.StravaUploadFunction
{
    public interface IConfiguration : MovescountBackup.Lib.IConfiguration, Lib.IConfiguration
    {
        string EmailFrom { get; }

        string EmailTo { get; }

        string SendGridApiKey { get; }
    }
}