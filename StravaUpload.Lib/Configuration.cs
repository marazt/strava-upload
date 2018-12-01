using Microsoft.Extensions.Configuration;

namespace StravaUpload.Lib
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

            this.StravaAccessToken = this.root["AppConfig:StravaAccessToken"];
        }

        public string StravaAccessToken { get; set; }
    }
}