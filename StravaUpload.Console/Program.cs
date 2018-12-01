using GarminConnectClient.Lib.Dto;
using Microsoft.Extensions.Configuration;
using MovescountBackup.Lib.Dto;
using Strava.Upload;
using StravaUpload.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StravaUpload.Console
{
    internal static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static async Task Main(string[] args)
        {
            var log = new ConsoleLogger();
            log.Information("Running.");
            var configuration = SetupConfiguration();
            var mailService = new MailService(configuration);

            await ProcessGarminConnectActivities(configuration, log, mailService);

            System.Console.ReadKey();
        }

        private static async Task ProcessGarminConnectActivities(Configuration configuration, ConsoleLogger log, MailService mailService)
        {
            configuration.ContainerName = configuration.GarminConnectClientContainerName;
            configuration.BackupDir = configuration.GarminConnectClientBackupDir;

            var backupFullPath = Path.Combine(Environment.CurrentDirectory, configuration.BackupDir);
            if (!Directory.Exists(backupFullPath))
            {
                Directory.CreateDirectory(backupFullPath);
            }

            var client = new GarminConnectClient.Lib.Services.Client(configuration, new ConsoleLogger<GarminConnectClient.Lib.Services.Client>());
            var storage = new GarminConnectClient.Lib.Services.CloudStorage(configuration.StorageConnectionString, configuration.ContainerName);
            var downloader = new GarminConnectClient.Lib.Services.Downloader(configuration, client, storage, new ConsoleLogger<GarminConnectClient.Lib.Services.Downloader>());

            IList<(Activity activity, string filePath, DataFormat fileFormat)> activitiesData = null;

            try
            {
                log.Information("Trying to backup new Garmin Connect activities and upload them to Strava.");
                var activities = await downloader.DownloadLastUserActivities();

                const DataFormat fileFormat = DataFormat.Gpx;
                activitiesData = activities.Select(activity => (activity,
                                 filePath: Path.Combine(backupFullPath, activity.ActivityId.ToString(), GaminConnectUploader.CreateGpsFileMapName(fileFormat)),
                                 fileFormat))
                                 .ToList();

                // Load data from Cloud storage and store them locally
                foreach (var activityItem in activitiesData)
                {
                    Directory.CreateDirectory(Path.Combine(backupFullPath, activityItem.activity.ActivityId.ToString()));
                    var blobStorageFilePath = Path.Combine(configuration.BackupDir, activityItem.activity.ActivityId.ToString(), GaminConnectUploader.CreateGpsFileMapName(fileFormat));
                    if (!File.Exists(activityItem.filePath))
                    {
                        log.Information($"Storing gps data file in {activityItem.filePath}.");
                        File.WriteAllText(activityItem.filePath, await storage.LoadData(blobStorageFilePath));
                    }
                }

                var uploader = new GaminConnectUploader(configuration.StravaAccessToken, new ConsoleLogger<GaminConnectUploader>());
                await uploader.AddOrUpdateGarminConnectActivitiesToStravaActivities(activitiesData);

                var processedActivities = activities.Any()
                    ? string.Join("<br />", activities.Select(activity =>
                    {
                        var link = $"https://connect.garmin.com/modern/activity/{activity.ActivityId}";
                        return $"<p><a href=\"{link}\" target=\"_blank\">{link}</a></p>";
                    }))
                    : "No moves";

                await mailService.SendEmail(
                    configuration.EmailFrom,
                    configuration.EmailTo,
                    $"<div><div><strong>Following activities were uploaded or updated: </strong></div>{processedActivities}</div>"
                );
            }
            catch (Exception ex)
            {
                log.Error("Error while downloading Garmin Connect activities data and uploading them to Strava.");
                log.Error(ex.Message);
                log.Error(ex.StackTrace);
                await mailService.SendEmail(
                    configuration.EmailFrom,
                    configuration.EmailTo,
                    $"<div><div><strong>Error while downloading Garmin Connect activities data and uploading them to Strava:</strong></div><p>{ex.StackTrace}</p></div>"
                    );
            }
            finally
            {
                // Clean backup directory
                Directory.Delete(backupFullPath, true);
            }
        }

        private static async Task ProcessMovescountMoves(Configuration configuration, ConsoleLogger log, MailService mailService)
        {
            var backupFullPath = Path.Combine(Environment.CurrentDirectory, configuration.MovescountBackupBackupDir);
            if (!Directory.Exists(backupFullPath))
            {
                Directory.CreateDirectory(backupFullPath);
            }

            var client = new MovescountBackup.Lib.Services.Client(configuration, new ConsoleLogger<MovescountBackup.Lib.Services.Client>());
            var storage = new MovescountBackup.Lib.Services.CloudStorage(configuration.StorageConnectionString, configuration.MovescountBackupContainerName);
            var downloader = new MovescountBackup.Lib.Services.Downloader(configuration, client, storage, new ConsoleLogger<MovescountBackup.Lib.Services.Downloader>());

            IList<(Move move, string filePath, DataFormat fileFormat)> movesData = null;

            try
            {
                log.Information("Trying to backup new Movescount moves and upload them to Strava.");
                var moves = await downloader.DownloadLastUserMoves(configuration.MovescountMemberName);

                var uploader = new MovescountUploader(configuration.StravaAccessToken, new ConsoleLogger<MovescountUploader>());

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
        }

        private static Configuration SetupConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.appsettings.json", false, true)
                .AddEnvironmentVariables();
            var configuration = builder.Build();
            return new Configuration(configuration);
        }
    }
}