using Microsoft.Extensions.Logging;
using MovescountBackup.Lib.Services;
using Strava.Activities;
using Strava.Authentication;
using Strava.Clients;
using Strava.Upload;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StravaUpload.Lib
{
    public class GaminConnectUploader
    {
        private readonly StravaClient client;
        private readonly string accessToken;
        private readonly ILogger logger;

        public GaminConnectUploader(string accessToken, ILogger<GaminConnectUploader> logger)
        {
            this.accessToken = accessToken;
            this.logger = logger;
            this.client = new StravaClient(new StaticAuthentication(this.accessToken));
        }

        private ActivityType GarminConnectTypeToStravaType(GarminConnectClient.Lib.Enum.ActivityTypeEnum activityType)
        {
            switch (activityType)
            {
                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.IndoorCardio:
                    return ActivityType.Crossfit;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.Hiking:
                    return ActivityType.Hike;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.StrengthTraining:
                    return ActivityType.Crossfit;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.Cycling:
                    return ActivityType.Ride;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.MultiSport:
                    return ActivityType.Run;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.Uncategorized:
                    return ActivityType.Workout;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.Running:
                    return ActivityType.Run;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.Walking:
                    return ActivityType.Walk;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.OpenWaterSwimming:
                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.Swimming:
                    return ActivityType.Swim;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.InlineSkating:
                    return ActivityType.InlineSkate;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.Skating:
                    return ActivityType.Iceskate;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.CrossCountrySkiing:
                    return ActivityType.CrossCountrySkiing;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.BackCountrySkiingSnowboarding:
                    return ActivityType.AlpineSki;

                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.TrackRunning:
                case GarminConnectClient.Lib.Enum.ActivityTypeEnum.TrailRunning:
                    return ActivityType.Run;

                default:
                    return ActivityType.Workout;
            }
        }

        private static string CreateDescription(GarminConnectClient.Lib.Dto.Activity activity)
        {
            var notes = new StringBuilder(string.Empty);
            notes.AppendLine("Garmin Connect data:");
            notes.AppendLine($"Activity: https://connect.garmin.com/modern/activity/{activity.ActivityId}");
            notes.AppendLine($"Training effect: {Math.Round(activity.Summary.TrainingEffect, 1)}");
            notes.AppendLine($"Average HR: {(activity.Summary.AverageHr)} bps");
            notes.AppendLine($"Location: {activity.LocationName}");
            notes.AppendLine($"Average temperature: {Math.Round(activity.Summary.AverageTemperature, 1)} °C");

            return $"{activity.Description?.Replace(" + ", " plus ")}\n{notes}";
        }

        public async Task AddOrUpdateGarminConnectActivitiesToStravaActivities(
            IList<(GarminConnectClient.Lib.Dto.Activity activity, string gpsFile, DataFormat gpsFormat)> garminActivities)
        {
            if (!garminActivities.Any())
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
            foreach (var (garminActivity, gpsFile, gpsFormat) in garminActivities)
            {
                var moveStartTime = garminActivity.Summary.StartTimeGmt.DateTime;

                var key = new RangePair(moveStartTime, moveStartTime);
                var description = CreateDescription(garminActivity);
                var name = garminActivity.ActivityName;

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
                    if (!File.Exists(gpsFile))
                    {
                        this.logger.LogWarning($"Gps data file {gpsFile} was not found. Activity could not be created.");
                    }
                    else
                    {
                        // Create new activity
                        var activityType = this.GarminConnectTypeToStravaType((GarminConnectClient.Lib.Enum.ActivityTypeEnum)garminActivity.ActivityType.TypeId);
                        try
                        {
                            var uploadStatus =
                                await this.client.Uploads.UploadActivityAsync(gpsFile, gpsFormat, activityType);

                            if (uploadStatus.Error != null)
                            {
                                if (uploadStatus.Error.ToLower().Contains("duplicate of activity"))
                                {
                                    var parts = uploadStatus.Error.Split(' ');
                                    var activityId = parts[parts.Length - 1];
                                    this.logger.LogWarning(
                                        $"Duplicate activity of Garmin Connect ActivityId {garminActivity.ActivityId} and Strava ActivityId {activityId}. File {gpsFile}.");
                                    await this.client.Activities.UpdateActivityAsync(activityId,
                                        ActivityParameter.Description, description);
                                    await this.client.Activities.UpdateActivityAsync(activityId,
                                        ActivityParameter.Name, name);
                                    // TODO: Gear mapping
                                    //await this.client.Activities.UpdateActivityAsync(activityId,
                                    //    ActivityParameter.GearId, name);
                                }
                                else if (uploadStatus.Error.Contains("empty"))
                                {
                                    this.logger.LogWarning(
                                        $"Empty GPS file {gpsFile} of Garmin Connect ActivityId {garminActivity.ActivityId}. Trying to create new activity.");
                                    await this.client.Activities.CreateActivityAsync(name, activityType,
                                        moveStartTime, (int)garminActivity.Summary.Duration, description, garminActivity.Summary.Distance);
                                }
                            }
                            else
                            {
                                this.logger.LogWarning($"New activity of Garmin Connect ActivityId {garminActivity.ActivityId} and file {gpsFile} created.");
                                if (uploadStatus.Id == 0)
                                {
                                    this.logger.LogWarning($"Invalid upload status of uploaded Garmin Connect ActivityId {garminActivity.ActivityId}.");
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