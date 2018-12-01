using Microsoft.Extensions.Configuration;

namespace StravaUpload.Console
{
    public class Configuration : IConfiguration
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly IConfigurationRoot root;

        public Configuration()
        {
        }

        public Configuration(IConfigurationRoot root)
        {
            this.root = root;

            this.MovescountAppKey = this.root["AppConfig:MovescountAppKey"];
            this.MovescountUserEmail = this.root["AppConfig:MovescountUserEmail"];
            this.MovescountUserKey = this.root["AppConfig:MovescountUserKey"];
            this.MovescountMemberName = this.root["AppConfig:MovescountMemberName"];
            this.CookieValue = this.root["AppConfig:CookieValue"];
            this.StorageConnectionString = this.root["AppConfig:StorageConnectionString"];
            this.BackupDir = this.root["AppConfig:BackupDir"];
            this.ContainerName = this.root["AppConfig:ContainerName"];
            this.GarminConnectClientBackupDir = this.root["AppConfig:GarminConnectClientBackupDir"];
            this.GarminConnectClientContainerName = this.root["AppConfig:GarminConnectClientContainerName"];
            this.MovescountBackupBackupDir = this.root["AppConfig:MovescountBackupBackupDir"];
            this.MovescountBackupContainerName = this.root["AppConfig:MovescountBackupContainerName"];
            this.StravaAccessToken = this.root["AppConfig:StravaAccessToken"];
            this.SendGridApiKey = this.root["AppConfig:SendGridApiKey"];
            this.EmailFrom = this.root["AppConfig:EmailFrom"];
            this.EmailTo = this.root["AppConfig:EmailTo"];
        }

        public string MovescountAppKey { get; set; }

        public string MovescountUserEmail { get; set; }

        public string MovescountUserKey { get; set; }

        public string MovescountMemberName { get; set; }

        public string BackupDir { get; set; }

        public string CookieValue { get; set; }

        public string StorageConnectionString { get; set; }

        public string ContainerName { get; set; }

        public string StravaAccessToken { get; set; }

        public string SendGridApiKey { get; set; }

        public string EmailFrom { get; set; }

        public string EmailTo { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string MovescountBackupBackupDir { get; set; }

        public string MovescountBackupContainerName { get; set; }

        public string GarminConnectClientBackupDir { get; set; }

        public string GarminConnectClientContainerName { get; set; }
    }
}