using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MovescountBackup.Lib.Dto;
using MovescountBackup.Lib.Enums;
using MovescountBackup.Lib.Services;
using Newtonsoft.Json;
using Strava.Activities;
using Strava.Authentication;
using Strava.Clients;
using Strava.Upload;
using StravaUpload.Lib.Dto;

namespace StravaUpload.Lib
{
    public class Uploader
    {
        private readonly StravaClient client;
        private readonly string accessToken;
        private readonly ILogger logger;

        public Uploader(string accessToken, ILogger<Uploader> logger)
        {
            this.accessToken = accessToken;
            this.logger = logger;
            this.client = new StravaClient(new StaticAuthentication(this.accessToken));
        }

        private ActivityType MovescountTypeToStravaType(ActivityIdEnum activityType)
        {
            switch (activityType)
            {
                case ActivityIdEnum.CircuitTraining:
                    return ActivityType.Crossfit;
                case ActivityIdEnum.Climbing:
                    return ActivityType.Hike;
                case ActivityIdEnum.CrossFit:
                    return ActivityType.Crossfit;
                case ActivityIdEnum.Cycling:
                    return ActivityType.Ride;
                case ActivityIdEnum.MultiSport:
                    return ActivityType.Run;
                case ActivityIdEnum.NotSpecifiedSport:
                    return ActivityType.Workout;
                case ActivityIdEnum.Run:
                    return ActivityType.Run;
                case ActivityIdEnum.Walking:
                    return ActivityType.Walk;
                case ActivityIdEnum.NordicWalking:
                case ActivityIdEnum.Trekking:
                    return ActivityType.Hike;
                case ActivityIdEnum.Swimming:
                case ActivityIdEnum.OpenWaterSwimming:
                    return ActivityType.Swim;
                case ActivityIdEnum.Skating:
                    return ActivityType.InlineSkate;
                case ActivityIdEnum.IceSkating:
                    return ActivityType.Iceskate;
                case ActivityIdEnum.CrosscountrySkiing:
                    return ActivityType.CrossCountrySkiing;
                case ActivityIdEnum.AlpineSkiing:
                    return ActivityType.AlpineSki;
                case ActivityIdEnum.IndoorTraining:
                    return ActivityType.Crossfit;
                case ActivityIdEnum.TrailRunning:
                    return ActivityType.Run;
                default:
                    return ActivityType.Workout;
            }
        }

        // ReSharper disable once UnusedMember.Global
        public async Task FixActivityType()
        {
            var page = 1;
            var activities = await this.client.Activities.GetActivitiesAsync(page++, 30);

            while (activities.Any())
            {
                foreach (var activity in activities)
                {
                    try
                    {
                        if (activity.Type == ActivityType.Workout)
                        {
                            if (Enum.TryParse(activity.Name, out ActivityIdEnum moveActivity))
                            {
                                var newType = this.MovescountTypeToStravaType(moveActivity);

                                if (newType == ActivityType.Workout)
                                {
                                    this.logger.LogWarning(
                                        $"Type of activity {activity.Id} of name '{activity.Name}' must be resolved manually.");
                                    continue;
                                }

                                var activityData = this.client.Activities.GetActivity(activity.Id.ToString(), false);

                                var update = new UpdatableActivity
                                {
                                    Commute = activityData.IsCommute,
                                    Description = activityData.Description,
                                    Name = activityData.Name,
                                    Private = activityData.IsPrivate,
                                    Trainer = activityData.IsTrainer,
                                    Type = newType
                                };

                                using (var wc = new WebClient())
                                {
                                    wc.Headers.Add("Authorization",
                                        $"Bearer {this.accessToken}");
                                    wc.Headers.Add("Content-Type", "application/json");
                                    wc.UploadString($"https://www.strava.com/api/v3/activities/{activity.Id}",
                                        "PUT", JsonConvert.SerializeObject(update));
                                }
                            }
                            else
                            {
                                this.logger.LogWarning(
                                    $"Type of activity {activity.Id} of name '{activity.Name}' must be resolved manually.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError($"Error while updating type of activity {activity.Id}");
                        this.logger.LogError(ex.Message);
                        this.logger.LogError(ex.StackTrace);
                        throw;
                    }

                    activities = await this.client.Activities.GetActivitiesAsync(page++, 30);
                }
            }
        }

        // ReSharper disable once UnusedMember.Global
        public List<Move> LoadMovesFromFiles(string dirPath)
        {
            // Load moves
            var moves = new List<Move>();
            foreach (var dir in Directory.GetDirectories(dirPath))
            {
                var moveDataFilePath = Path.Combine(dir, Downloader.MoveDataFile);
                var move = JsonConvert.DeserializeObject<Move>(File.ReadAllText(moveDataFilePath),
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                if (move == null)
                {
                    this.logger.LogError($"MoveData in {dir} is null.");
                    continue;
                }

                moves.Add(move);
            }

            return moves;
        }

        private static string CreateDescription(Move move)
        {
            var notes = new StringBuilder(string.Empty);
            notes.AppendLine("Movescount data:");
            notes.AppendLine($"Move: http://www.movescount.com/moves/move{move.MoveId}");
            notes.AppendLine(
                $"Feeling: {(move.Feeling.HasValue ? Enum.Parse(typeof(FeelingEnum), move.Feeling.ToString()).ToString() : "-")}");
            notes.AppendLine(
                $"Weather: {(move.Weather.HasValue ? Enum.Parse(typeof(WeatherEnum), move.Weather.ToString()).ToString() : "-")}");
            notes.AppendLine($"Tags: {move.Tags}");
            notes.AppendLine(
                $"RecoveryTime: {(move.RecoveryTime.HasValue ? $"{Math.Round(move.RecoveryTime.Value / 3600, 1)}h" : "-")}");

            return $"{move.Notes}\n{notes}";
        }

        public async Task AddOrUpdateMovescountMovesToStravaActivities(
            IList<(Move move, string gpsFile, DataFormat gpsFormat)> moves)
        {
            if (!moves.Any())
            {
                this.logger.LogInformation("No moves to be processed.");
                return;
            }

            var activitiesToUpdate = new Dictionary<RangePair, ActivitySummary>();
            var uploadedActivitiesToUpdate = new Dictionary<long, string>();

            // Load Strava activities
            var page = 1;
            this.logger.LogInformation("Loading Strava activities.");
            var activities = await this.client.Activities.GetActivitiesAsync(page++, 30);
            while (activities.Count > 0)
            {
                foreach (var activity in activities)
                {
                    var key = new RangePair(activity.DateTimeStart, activity.DateTimeStart);
                    if (!activitiesToUpdate.ContainsKey(key))
                    {
                        activitiesToUpdate.Add(key, activity);
                    }
                }

                activities = await this.client.Activities.GetActivitiesAsync(page++, 30);
            }

            // Process moves
            this.logger.LogInformation("Processing moves.");
            foreach (var (move, gpsFile, gpsFormat) in moves)
            {
                DateTime? moveStartTime;

                moveStartTime = move.UTCStartTime ?? move.LocalStartTime;

                if (!moveStartTime.HasValue)
                {
                    this.logger.LogError($"Invalid UTCStartTime or StartTime {move.MoveId}.");
                    continue;
                }

                var key = new RangePair(moveStartTime.Value, moveStartTime.Value);
                var description = CreateDescription(move);
                var name = move.ActivityID.ToString();

                if (activitiesToUpdate.ContainsKey(key))
                {
                    // Update activity
                    var activity = activitiesToUpdate[key];
                    this.logger.LogInformation($"Activity {activity.Id} found. Updating description.");

                    try
                    {
                        await this.client.Activities.UpdateActivityAsync(activity.Id.ToString(),
                            ActivityParameter.Description, description);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError($"Error while updating activity {activity.Id}.");
                        this.logger.LogError(ex.Message);
                        this.logger.LogError(ex.StackTrace);
                        throw;
                    }
                }
                else
                {
                    // Create new activity
                    var activityType = this.MovescountTypeToStravaType(move.ActivityID);
                    try
                    {
                        var uploadStatus =
                            await this.client.Uploads.UploadActivityAsync(gpsFile, gpsFormat, activityType);

                        if (uploadStatus.Error != null)
                        {
                            if (uploadStatus.Error.Contains("duplicate of activity."))
                            {
                                var parts = uploadStatus.Error.Split(' ');
                                var activityId = parts[parts.Length - 1];
                                this.logger.LogWarning(
                                    $"Duplicate activity of MoveId {move.MoveId} and ActivityId {activityId}. File {gpsFile}.");
                                await this.client.Activities.UpdateActivityAsync(activityId,
                                    ActivityParameter.Description, description);
                            }
                            else if (uploadStatus.Error.Contains("empty"))
                            {
                                this.logger.LogWarning(
                                    $"Empty GPS file {gpsFile} of MoveId {move.MoveId}. Trying to create new activity.");
                                await this.client.Activities.CreateActivityAsync(name, activityType,
                                    moveStartTime.Value, (int)move.Duration, description,
                                    move.Distance ?? 0);
                            }
                        }
                        else
                        {
                            this.logger.LogWarning($"New activity of MoveId {move.MoveId} and file {gpsFile} created.");
                            if (uploadStatus.Id == 0)
                            {
                                this.logger.LogWarning($"Invalid upload status of uploaded move {move.MoveId}.");
                            }
                            else
                            {
                                this.logger.LogWarning(
                                    $"Saving upload status id {uploadStatus.Id} for description update.");
                                uploadedActivitiesToUpdate.Add(uploadStatus.Id, description);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError("Error while creating activity.");
                        this.logger.LogError(ex.Message);
                        this.logger.LogError(ex.StackTrace);
                        throw;
                    }
                }
            }

            // Update uploaded activities
            var activitiesToCheck = new HashSet<long>(uploadedActivitiesToUpdate.Keys);

            if (activitiesToCheck.Any())
            {
                this.logger.LogInformation("Updating newly created activities.");
            }

            while (activitiesToCheck.Any())
            {
                foreach (var activityStatus in activitiesToCheck)
                {
                    try
                    {
                        var status = await this.client.Uploads.CheckUploadStatusAsync(activityStatus.ToString());
                        if (status.CurrentStatus == CurrentUploadStatus.Ready)
                        {
                            var description = uploadedActivitiesToUpdate[activityStatus];
                            this.logger.LogInformation($"Updating description of newly created activity {status.ActivityId}.");
                            await this.client.Activities.UpdateActivityAsync(
                                status.ActivityId,
                                ActivityParameter.Description,
                                description);
                            uploadedActivitiesToUpdate.Remove(activityStatus);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError($"Error while updating newly created activity {activityStatus}.");
                        this.logger.LogError(ex.Message);
                        this.logger.LogError(ex.StackTrace);
                        throw;
                    }
                }

                activitiesToCheck = new HashSet<long>(uploadedActivitiesToUpdate.Keys);
            }
        }

        public static string CreateGpsFileMapName(DataFormat exportFormat) =>
            $"{Downloader.GpsDataFile}.{exportFormat.ToString().ToLower()}";
    }
}