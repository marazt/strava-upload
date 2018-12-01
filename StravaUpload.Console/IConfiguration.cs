namespace StravaUpload.Console
{
    public interface IConfiguration : MovescountBackup.Lib.IConfiguration, Lib.IConfiguration, GarminConnectClient.Lib.IConfiguration
    {
        string EmailFrom { get; }

        string EmailTo { get; }

        string SendGridApiKey { get; }

        string MovescountBackupBackupDir { get; }

        string MovescountBackupContainerName { get; }

        string GarminConnectClientBackupDir { get; }

        string GarminConnectClientContainerName { get; }
    }
}