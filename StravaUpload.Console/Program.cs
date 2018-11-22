using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MovescountBackup.Lib.Dto;
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
            var log = new ConsoleLogger();
            log.Information("Running.");
            var configuration = SetupConfiguration();
            var backupFullPath = Path.Combine(Environment.CurrentDirectory, configuration.BackupDir);
            if (!Directory.Exists(backupFullPath))
            {
                Directory.CreateDirectory(backupFullPath);
            }

            var mailService = new MailService(configuration);

            var client = new Client(configuration, new ConsoleLogger<Client>());
            var storage = new CloudStorage(configuration.StorageConnectionString, configuration.ContainerName);
            var downloader = new Downloader(configuration, client, storage, new ConsoleLogger<Downloader>());

            IList<(Move move, string filePath, DataFormat fileFormat)> movesData = null;

            try
            {
                log.Information("Trying to backup new Movescount moves and upload them to Strava.");
                var moves = await downloader.DownloadLastUserMoves(configuration.MovescountMemberName);

                var uploader = new Uploader(configuration.StravaAccessToken, new ConsoleLogger<Uploader>());

                const DataFormat fileFormat = DataFormat.Tcx;
                movesData = moves.Select(move => (move,
                                 filePath: Path.Combine(backupFullPath, move.MoveId.ToString(), Uploader.CreateGpsFileMapName(fileFormat)),
                                 fileFormat))
                                 .ToList();

                // Load data from Cloud storage and store them locally
                foreach (var moveItem in movesData)
                {
                    Directory.CreateDirectory(Path.Combine(backupFullPath, moveItem.move.MoveId.ToString()));
                    var blobStorageFilePath = Path.Combine(configuration.BackupDir, moveItem.move.MoveId.ToString(), Uploader.CreateGpsFileMapName(fileFormat));
                    if (!File.Exists(moveItem.filePath))
                    {
                        log.Information($"Storing gps data file in {moveItem.filePath}.");
                        File.WriteAllText(moveItem.filePath, await storage.LoadData(blobStorageFilePath));
                    }
                }

                await uploader.AddOrUpdateMovescountMovesToStravaActivities(movesData);

                var processedMoves = moves.Any()
                    ? string.Join("<br />", moves.Select(move =>
                    {
                        var link = $"http://www.movescount.com/moves/move{move.MoveId}";
                        return $"<p><a href=\"{link}\" target=\"_blank\">{link}</a></p>";
                    }))
                    : "No moves";

                await mailService.SendEmail(
                    configuration.EmailFrom,
                    configuration.EmailTo,
                    $"<div><div><strong>Following moves were uploaded or updated: </strong></div>{processedMoves}</div>"
                );
            }
            catch (Exception ex)
            {
                log.Error("Error while downloading Movescount data and uploading them to Strava.");
                log.Error(ex.Message);
                log.Error(ex.StackTrace);
                await mailService.SendEmail(
                    configuration.EmailFrom,
                    configuration.EmailTo,
                    $"<div><div><strong>Error while downloading Movescount data and uploading them to Strava:</strong></div><p>{ex.StackTrace}</p></div>"
                    );
            }
            finally
            {
                // Clean backup directory
                Directory.Delete(backupFullPath, true);
            }
            System.Console.ReadKey();
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