# Strava upload

Strava Upload is s simple library to synchronize (create, update) activities on **Strava** with other activities. 
Currently it provides examples for synchronization with:
- **Suunto Movescount** moves that are provided by [**MovescountBackup** library](https://github.com/marazt/movescount-backup).
- **Garmin Connect** activities that are provided by [**GarminConnectClient** library](https://github.com/marazt/garmin-connect-client).
 
## Version

- **Version 1.0.1** - 2018-04-24

### Getting Started/Installing

```ps
PM> Install-Package StravaUpload -Version 1.0.1
```

## Project Description

The solution consists of the following projects:

- **StravaUpload.Lib** is the main library containing `Uploader` class.

- **StravaUpload.Console** is just a sample that downloads *Movescount* data, store them on local dist and sync them with *Strava*.

- **StravaUpload.StravaUploadFunction** Azure function that periodically downloads data from *Movescount*, store them in *Azure Blob Storage* and sync them with *Strava*.

- **StravaUpload.Lib.Spec** TODO.


### Prerequisites

- .NET Core 2.0.
- For more details about *MovescountUpload* check the [project site](https://github.com/marazt/movescount-backup)

### Configuration

#### StravaUpload.Lib

- **StravaAccessToken** - Name of the member whose data we want to get.

#### StravaUpload.Console

##### MovescountUploader

- **MovescountAppKey** - App key to be able to query Movescount API.
- **MovescountUserKey** - User key to be able to query Movescount API.
- **MovescountUserEmail** - User email to be able to query Movescount API.
- **MovescountMemberName** - Name of the member whose data we want to get.
- **CookieValue** - A cookie value that is needed to export GPX, TCX and other move files.
    This value can be get by the following steps:
    1. Open console in your browser to se network requests.
    1. Login into Movescount.
    1. Select a requeset to `http://www.movescount.com/api/members/private/messages`.
    1. Copy value of `Cookie` key in request header. It should start with `ASP.NET ...`.
- **BackupDir** - Directory where moves should be stored. Required for `FileSystemStorage`.
- **StorageConnectionString** - Connection string to Azure Blob Storage. Required for `CloudStorate`.
- **ContainerName** - Container name on Azure Blob Storage. Required for `CloudStorate`.
- **StravaAccessToken** - Strava access token wit write rights.

##### GarminConnectUploader

- **Username** - Garmin Conect username.
- **Password** - Garmin Conect password.
- **BackupDir** - Directory where moves should be stored. Required for `FileSystemStorage`.
- **StorageConnectionString** - Connection string to Azure Blob Storage. Required for `CloudStorate`.
- **ContainerName** - Container name on Azure Blob Storage. Required for `CloudStorate`.

#### StravaUpload.StravaUploadFunction

- **MovescountAppKey** - App key to be able to query Movescount API.
- **MovescountUserKey** - User key to be able to query Movescount API.
- **MovescountUserEmail** - User email to be able to query Movescount API.
- **MovescountMemberName** - Name of the member whose data we want to get.
- **CookieValue** - A cookie value that is needed to export GPX, TCX and other move files.
    This value can be get by the following steps:
    1. Open console in your browser to se network requests.
    1. Login into Movescount.
    1. Select a requeset to `http://www.movescount.com/api/members/private/messages`.
    1. Copy value of `Cookie` key in request header. It should start with `ASP.NET ...`.
- **BackupDir** - Directory where moves should be stored. Required for `FileSystemStorage`.
- **StorageConnectionString** - Connection string to Azure Blob Storage. Required for `CloudStorate`.
- **ContainerName** - Container name on Azure Blob Storage. Required for `CloudStorate`.
- **StravaAccessToken** - Strava access token wit write rights.
- **SendGridApiKey** - API key for *SendGrid* mail service to be able to send status messages.
- **EmailFrom** - Email from.
- **EmailTo** - Email to.

### StravaUpload.Lib.Uploader.cs

Uploader is the main class for activity synchronization.
It downloads activities list from **Strava**, compare them with list of moves that should be added/updated.
The first comparison is done by activity/move start/end datetime. If an acitvity is found in **Strava**, it is updated. If the activity is
not found in **Strava**,  *fit*, *gpx* or other activity data file is uploaded. 
It can happen that newly uploaded activity is already in **Strava**. This state is evaluated from the upload response. In this
case, the activity is just updated.

## Deployment

You can use this libraly to be run manually (`StravaUpload.Console.Program.cs`) or you can create, e.g., a *Azure function* (`StravaUpload.StravaUploadFunction.Function.cs`)  or *Lambda function on AWS*, that can run periodically.

## Contributing

Any contribution is welcomed.

## Authors

- **Marek Polak** - *Initial work* - [marazt](https://github.com/marazt)

## License

© 2018 Marek Polak. This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

- Enjoy it!
- If you want, you can support this project too.