using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using MovescountBackup.Lib.Dto;
using MovescountBackup.Lib.Services;
using Strava.Upload;
using StravaUpload.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StravaUpload.StravaUploadFunction
{
    // ReSharper disable once UnusedMember.Global
    public static class Function
    {
        [FunctionName("StravaUploadFunction")]
        // ReSharper disable once UnusedParameter.Global
        // ReSharper disable once UnusedMember.Global
        public static async Task Run([TimerTrigger("0 0 3 * * *", RunOnStartup = true, UseMonitor = true)] TimerInfo myTimer, TraceWriter log, ExecutionContext context)
        {
            log.Info($"C# Timer trigger MovescountBackup function executed at: {DateTime.UtcNow.ToIsoString()}.");
            await Execute(log, context);
            log.Info($"Done at: {DateTime.UtcNow.ToIsoString()}.");
        }

        private static async Task Execute(TraceWriter log, ExecutionContext context)
        {
            log.Info("Running.");
            var configuration = SetupConfiguration(context);
            var backupFullPath = Path.Combine(context.FunctionAppDirectory, configuration.BackupDir);
            if (!Directory.Exists(backupFullPath))
            {
                Directory.CreateDirectory(backupFullPath);
            }

            var mailService = new MailService(configuration);

            var client = new Client(configuration, new TraceWriterLogger<Client>(log));
            var storage = new CloudStorage(configuration.StorageConnectionString, configuration.ContainerName);
            var downloader = new Downloader(configuration, client, storage, new TraceWriterLogger<Downloader>(log));

            IList<(Move move, string filePath, DataFormat fileFormat)> movesData = null;

            try
            {
                log.Info("Trying to backup new Movescount moves and upload them to Strava.");
                var moves = await downloader.DownloadLastUserMoves(configuration.MovescountMemberName);

                var uploader = new MovescountUploader(configuration.StravaAccessToken, new TraceWriterLogger<MovescountUploader>(log));

                const DataFormat fileFormat = DataFormat.Tcx;
                movesData = moves.Select(move => (move,
                                 filePath: Path.Combine(backupFullPath, move.MoveId.ToString(), MovescountUploader.CreateGpsFileMapName(fileFormat)),
                                 fileFormat))
                                 .ToList();

                // Load data from Cloud storage and store them locally
                foreach (var moveItem in movesData)
                {
                    Directory.CreateDirectory(Path.Combine(backupFullPath, moveItem.move.MoveId.ToString()));
                    var blobStorageFilePath = Path.Combine(configuration.BackupDir, moveItem.move.MoveId.ToString(), MovescountUploader.CreateGpsFileMapName(fileFormat));
                    if (!File.Exists(moveItem.filePath))
                    {
                        log.Info($"Storing gps data file in {moveItem.filePath}.");
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
        }

        private static IConfiguration SetupConfiguration(ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return new Configuration
            {
                MovescountAppKey = config["MovescountAppKey"],
                MovescountUserEmail = config["MovescountUserEmail"],
                MovescountUserKey = config["MovescountUserKey"],
                BackupDir = config["BackupDir"],
                CookieValue = config["CookieValue"],
                StorageConnectionString = config["StorageConnectionString"],
                ContainerName = config["ContainerName"],
                MovescountMemberName = config["MovescountMemberName"],

                SendGridApiKey = config["SendGridApiKey"],
                EmailFrom = config["EmailFrom"],
                EmailTo = config["EmailTo"],

                StravaAccessToken = config["StravaAccessToken"],
            };
        }
    }
}