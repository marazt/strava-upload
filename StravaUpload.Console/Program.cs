using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MovescountBackup.Lib.Services;
using Strava.Upload;
using StravaUpload.Lib;

namespace StravaUpload.Console
{
    static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        static async Task Main(string[] args)
        {
            var configuration = SetupConfiguration();
            var client = new Client(configuration, new ConsoleLogger<Client>());
            var storage = new FileSystemStorage();
            var downloader = new Downloader(configuration, client, storage, new ConsoleLogger<Downloader>());
            var moves = await downloader.DownloadLastUserMoves(configuration.MovescountMemberName);

            var uploader = new Uploader(configuration.StravaAccessToken, new ConsoleLogger<Uploader>());

            var fileFormat = DataFormat.Tcx;
            var movesData = moves.Select(move => (move,
                Path.Combine(configuration.BackupDir, move.MoveId.ToString(), Uploader.CreateGpsFileMapName(fileFormat)),
                fileFormat)).ToList();

            try
            {
                await uploader.AddOrUpdateMovescountMovesToStravaActivities(movesData);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Error while uploading activities");
                System.Console.WriteLine(ex.Message);
                System.Console.WriteLine(ex.StackTrace);
            }
        }


        private static Configuration SetupConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddEnvironmentVariables();
            var configuration = builder.Build();
            return new Configuration(configuration);
        }
    }
}