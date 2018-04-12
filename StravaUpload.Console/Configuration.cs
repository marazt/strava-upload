using Microsoft.Extensions.Configuration;

namespace StravaUpload.Console
{

    public class Configuration : MovescountBackup.Lib.IConfiguration, Lib.IConfiguration
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly IConfigurationRoot root;

        public Configuration() { }

        public Configuration(IConfigurationRoot root)
        {
            this.root = root;

            this.MovescountAppKey = this.root["AppConfig:MovescountAppKey"];
            this.MovescountUserEmail = this.root["AppConfig:MovescountUserEmail"];
            this.MovescountUserKey = this.root["AppConfig:MovescountUserKey"];
            this.MovescountMemberName = this.root["AppConfig:MovescountMemberName"];
            this.BackupDir = this.root["AppConfig:BackupDir"];
            this.CookieValue = this.root["AppConfig:CookieValue"];
            this.StorageConnectionString = this.root["AppConfig:StorageConnectionString"];
            this.ContainerName = this.root["AppConfig:ContainerName"];
            this.StravaAccessToken = this.root["AppConfig:StravaAccessToken"];
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
    }
}
