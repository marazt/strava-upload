using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace StravaUpload.StravaUploadFunction
{
    public class MailService
    {
        private readonly IConfiguration configuration;

        public MailService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendEmail(string fromEmail, string toEmail, string htmlContent)
        {
            var apiKey = this.configuration.SendGridApiKey;
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail);
            const string subject = "MovescountBackup Information";
            var to = new EmailAddress(toEmail);
            var plainTextContent = string.Empty;
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            await client.SendEmailAsync(msg);
        }
    }
}