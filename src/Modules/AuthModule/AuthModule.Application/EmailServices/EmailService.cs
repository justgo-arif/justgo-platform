using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace AuthModule.Application.EmailServices
{
    public class EmailService
    {
        private readonly LazyService<IReadRepository<MailItem>> _readRepository;
        private readonly LazyService<IReadRepository<SystemSettings>> _readRepositorySystemSettings;
        private readonly LazyService<IWriteRepository<MailItem>> _writeRepository;
        private readonly IUtilityService _utilityService;
        public EmailService(LazyService<IReadRepository<MailItem>> readRepository,LazyService<IReadRepository<SystemSettings>> readRepositorySystemSettings
            , LazyService<IWriteRepository<MailItem>> writeRepository
            , IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _readRepositorySystemSettings= readRepositorySystemSettings;
            _writeRepository = writeRepository;
            _utilityService = utilityService;
        }

        private IDictionary<int, Dictionary<string, object>> MessageTemplates = new Dictionary<int, Dictionary<string, object>>();
        private int batchSize = 10;
        private const int Max_Fail_Count = 5;
        private const string MAILQUEUE_SELECT = @"select top {0} * from MailQueue where status={1} and FailCount<={2}";

        private const string MAILQUEUE_STUTUS_UPDATE = "update MailQueue set status={0} where id in({1})";
        private const string MAILQUEUE_UPDATE_TO_COMPLETED = "update MailQueue set status=@status,SentDate=@getdate(),ErrorMessage=@ErrorMessage where id=@id";
        private const string MAILQUEUE_UPDATE_TO_FAILED = "update MailQueue set status=@status,SentDate=getdate(),FailCount=FailCount+1,ErrorMessage=@ErrorMessage where id=@id";
        private string SENDGRID_APIKEY = "";


        public async Task Execute()
        {
            var selectedMail = await GetMailItemsByStatus(EmailServiceEnum.SendImmediately);

            if (selectedMail.Count > 0)
            {
                _ = UpdateStatusToInProgress(selectedMail, EmailServiceEnum.InProgress);
                #pragma warning disable CS0612 // Type or member is obsolete
                _ = ProcessMail(selectedMail);
                #pragma warning restore CS0612 // Type or member is obsolete
            }
        }

        private async Task<List<MailItem>> GetMailItemsByStatus(EmailServiceEnum status)
        {
            //some pending task
            var items = new List<MailItem>();
            MailQueueItemDataReaderService _readerService = new MailQueueItemDataReaderService();
           
                string sql = string.Format(MAILQUEUE_SELECT, batchSize, (int)status, (status == EmailServiceEnum.Pending ? 0 : Max_Fail_Count));

            // Execute the query and fetch results
            var mailItems = await _readRepository.Value.GetListAsync(sql, null, null, "text");


                if (mailItems.Count() == 0) return items;

                items = _readerService.ResolveOrdinal(mailItems.ToList());

          

            return items;
        }

        private async Task UpdateStatusToInProgress(List<MailItem> selectedMail, EmailServiceEnum mailStatus)
        {
            var id = string.Empty;
            var ids = new List<string>();
            for (int i = 0; i < selectedMail.Count; i++)
            {
                ids.Add(selectedMail[i].Id.ToString());
            }
            id = string.Join(",", ids.ToArray());
            try
            {
                string sql = string.Format(MAILQUEUE_STUTUS_UPDATE, (int)mailStatus, id);
                // Execute the query and fetch results
                var mailItems = await _writeRepository.Value.ExecuteAsync(sql, null, null, "text");

            }
            catch
            {

            }

        }

        private async Task ProcessMail(List<MailItem> selectedMail)
        {
            await GetSendGridAPIKey();
            var errorInfo = string.Empty;
            foreach (var email in selectedMail)
            {
                try
                {

                        string query = @"select SystemSettings.ItemKey,SystemSettings.Value,isnull(Restricted,0) Restricted  from     SystemSettings  where SystemSettings.ItemKey in (select s from dbo.SplitString(@Key,','))";

                        var queryParameters = new DynamicParameters();
                        queryParameters.Add("@Key", "SYSTEM.MAIL.SENDGRID");


                    // Execute the query and return a single MailItem
                    var sendGrid = await _readRepositorySystemSettings.Value.GetAsync(query, queryParameters, null, "text");

                        if (sendGrid != null && !string.IsNullOrEmpty(sendGrid.Value))
                        {
                            if (sendGrid.Restricted)
                                sendGrid.Value = _utilityService.DecryptData(sendGrid.Value);

                        }

                        if (sendGrid.Value.ToLower() == "yes")
                            await SendingMailForSendgrid(email);
                        else
                            await SendingMail(email);

                        await MailqueueUpadate(email, EmailServiceEnum.Complete, errorInfo, MAILQUEUE_UPDATE_TO_COMPLETED);
                    
                }
                catch (Exception exception)
                {
                    await MailqueueUpadate(email, EmailServiceEnum.Failed, exception.Message + exception, MAILQUEUE_UPDATE_TO_FAILED);
                }
            }
        }

        private async Task GetSendGridAPIKey()
        {
            var apiKey = string.Empty;
            try
            {
                var reader = await _readRepositorySystemSettings.Value.GetAsync("select [Value] from systemsettings where itemkey='SYSTEM.SENDGRIDAPIKEY'", null, null, "text");
                SENDGRID_APIKEY = reader?.Value;
            }
            catch { }

        }


        private async Task SendingMailForSendgrid(MailItem email)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://sendgrid.com");
                client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", SENDGRID_APIKEY);
                using (var multicontent = new MultipartFormDataContent())
                {


                    var mailAddressAndName = ExtractEmailAddressAndName(email.Sender);

                    if (string.IsNullOrWhiteSpace(email.ReplyToEmailAddress))
                    {
                        multicontent.Add(new StringContent(mailAddressAndName.Key), "from");
                        multicontent.Add(new StringContent(mailAddressAndName.Value), "fromname");
                    }

                    else
                    {
                        multicontent.Add(new StringContent(mailAddressAndName.Key), "from");
                        multicontent.Add(new StringContent(email.ReplyToName), "fromname");
                        multicontent.Add(new StringContent(email.ReplyToEmailAddress), "replyto");
                        multicontent.Add(new StringContent(email.ReplyToName), "reply_toname");
                    }

                    multicontent.Add(new StringContent(email.Subject), "subject");
                    multicontent.Add(new StringContent(email.Mailbody.Replace("<a href =", "<a href=").Replace("<a href= ", "<a href=")), "html");
                    string Temp = email.Mailbody.Replace("<a href =", "<a href=").Replace("<a href= ", "<a href=");

                    multicontent.Add(new StringContent(HTMLToText(Temp.Replace(@"<a href=""", "").Replace(@"</a>", "")).Replace(@""">", "  ")), "text");


                    string[] listOfTo = !string.IsNullOrEmpty(email.To) ? email.To.Split(';') : new string[] { };
                    string[] listOfBcc = !string.IsNullOrEmpty(email.Bcc) ? email.Bcc.Split(';') : new string[] { };
                    string[] listOfCC = !string.IsNullOrEmpty(email.CC) ? email.CC.Split(';') : new string[] { };


                    foreach (var address in listOfTo)
                        multicontent.Add(new StringContent(address), "to[]");

                    foreach (var address in listOfCC)
                        multicontent.Add(new StringContent(address), "cc[]");

                    foreach (var address in listOfBcc)
                        multicontent.Add(new StringContent(address), "bcc[]");

                    var hostId = await _readRepositorySystemSettings.Value.GetAsync("select [Value] from systemsettings where itemkey='CLUBPLUS.HOSTSYSTEMID'", null, null, "text");
                    var itemList = JsonConvert.SerializeObject(new { category = string.Format("[{0},{1},{2}]", hostId?.Value, email.Subject, email.Tag) });
                    multicontent.Add(new StringContent(itemList), "x-smtpapi");


                    var result = await client.PostAsync("/api/mail.send.json", multicontent);

                    var resultContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(await result.Content.ReadAsStringAsync());
                    if (resultContent["message"].ToString() == "success")
                    {
                        await MailqueueUpadate(email, EmailServiceEnum.Complete, string.Empty, MAILQUEUE_UPDATE_TO_COMPLETED);
                    }
                    else await MailqueueUpadate(email, EmailServiceEnum.Failed, JsonConvert.SerializeObject(resultContent), MAILQUEUE_UPDATE_TO_FAILED);
                }

            }
        }
        private KeyValuePair<string, string> ExtractEmailAddressAndName(string val)
        {
            if (val.IndexOf("<") != -1 && val.IndexOf(">") != -1)
            {
                return new KeyValuePair<string, string>(val.Substring(val.IndexOf(">") + 1).Trim(), val.Substring(val.IndexOf("<") + 1, val.IndexOf(">") - 1).Trim());
            }
            else
            {
                return new KeyValuePair<string, string>(val.Trim(), string.Empty);
            }
        }
        private string HTMLToText(string htmlCode)
        {
            // Remove new lines since they are not visible in HTML
            htmlCode = htmlCode.Replace("\n", " ");

            // Remove tab spaces
            htmlCode = htmlCode.Replace("\t", " ");

            // Remove multiple white spaces from HTML
            htmlCode = Regex.Replace(htmlCode, "\\s+", " ");

            // Remove HEAD tag
            htmlCode = Regex.Replace(htmlCode, "<head.*?</head>", ""
                                , RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Remove any JavaScript
            htmlCode = Regex.Replace(htmlCode, "<script.*?</script>", ""
              , RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Replace special characters like &, <, >, " etc.
            StringBuilder sbHTML = new StringBuilder(htmlCode);
            // Note: There are many more special characters, these are just
            // most common. You can add new characters in this arrays if needed
            string[] OldWords = { "&nbsp;", "&amp;", "&quot;", "&lt;", "&gt;", "&reg;", "&copy;", "&bull;", "&trade;" };
            string[] NewWords = { " ", "&", "\"", "<", ">", "Â®", "Â©", "â€¢", "â„¢" };
            for (int i = 0; i < OldWords.Length; i++)
            {
                sbHTML.Replace(OldWords[i], NewWords[i]);
            }

            // Check if there are line breaks (<br>) or paragraph (<p>)
            sbHTML.Replace("<br>", "\n<br>");
            sbHTML.Replace("<br ", "\n<br ");
            sbHTML.Replace("<p ", "\n<p ");

            // Finally, remove all HTML tags and return plain text


            return System.Text.RegularExpressions.Regex.Replace(
              sbHTML.ToString(), "<[^>]*>", "");
        }

        private async Task MailqueueUpadate(MailItem email, EmailServiceEnum complete, string errorInfo, string updateQuery)
        {
            var param = new DynamicParameters();
            param.Add("@status", complete.GetHashCode());
            param.Add("@id", email.Id);
            param.Add("@ErrorMessage", errorInfo);
            param.Add("@ErrorMessage", errorInfo);
            try
            {
                // Execute the update query
                await _writeRepository.Value.ExecuteAsync(updateQuery, param, null, "text");

            }
            catch
            {
            }

        }

        private async Task SendingMail(MailItem email)
        {

            SmtpClient smtpServer = null;

            string query = @"select SystemSettings.ItemKey,SystemSettings.Value,isnull(Restricted,0) Restricted  from     SystemSettings  where SystemSettings.ItemKey in (select s from dbo.SplitString(@Key,','))";

            var queryMailServerPass = new DynamicParameters();
            queryMailServerPass.Add("@Key", "SYSTEM.MAIL.MAILSERVERUSERPASSWORD");

            var queryMailServerIp = new DynamicParameters();
            queryMailServerIp.Add("@Key", "SYSTEM.MAIL.MAILSERVERIP");

            var querySmtpPort = new DynamicParameters();
            querySmtpPort.Add("@Key", "SYSTEM.MAIL.SMTPPORT");

            var queryServerName = new DynamicParameters();
            queryServerName.Add("@Key", "SYSTEM.MAIL.MAILSERVERUSERNAME");

            var mailServerPass = await _readRepositorySystemSettings.Value.GetAsync(query, queryMailServerPass, null, "text");
            var mailServerIp = await _readRepositorySystemSettings.Value.GetAsync(query, queryMailServerIp, null, "text");
            var smtpPort = await _readRepositorySystemSettings.Value.GetAsync(query, querySmtpPort, null, "text");
            var mailServerName = await _readRepositorySystemSettings.Value.GetAsync(query, queryServerName, null, "text");
            // Execute the update query
            smtpServer = !string.IsNullOrEmpty(mailServerPass.Value)
                            ? new SmtpClient(mailServerIp.Value, int.Parse(smtpPort.Value))
                            {
                                Credentials = new System.Net.NetworkCredential(mailServerName.Value, mailServerPass.Value)
                            }
                            : new SmtpClient(mailServerIp.Value);
           



            var mail = new MailMessage
            {
                Sender = new MailAddress(email.Sender),
                From = new MailAddress(email.Sender),
                IsBodyHtml = true,
                Subject = email.Subject,
                #pragma warning disable CS0618 // Type or member is obsolete
                ReplyTo = new MailAddress(email.Sender),
                #pragma warning restore CS0618 // Type or member is obsolete
                BodyEncoding = System.Text.Encoding.GetEncoding("utf-8")
            };

            var plainView = AlternateView.CreateAlternateViewFromString
                            (System.Text.RegularExpressions.Regex.Replace(email.Mailbody, @"<(.|\n)*?>", string.Empty), null, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(email.Mailbody, null, "text/html");

            mail.AlternateViews.Add(plainView);
            mail.AlternateViews.Add(htmlView);

            string[] listOfTo = new string[] { };
            string[] listOfBcc = new string[] { };
            string[] listOfCC = new string[] { };
            if (!string.IsNullOrEmpty(email.To))
            {
                listOfTo = email.To.Split(';');
            }

            if (!string.IsNullOrEmpty(email.Bcc))
            {
                listOfBcc = email.Bcc.Split(';');
            }
            if (!string.IsNullOrEmpty(email.CC))
            {
                listOfCC = email.CC.Split(';');
            }
            foreach (var recipient in listOfTo) // assuming recipients is a List<string>
            {

                mail.To.Add(new MailAddress(recipient));
            }
            foreach (var recipient in listOfBcc) // assuming recipients is a List<string>
            {

                mail.Bcc.Add(new MailAddress(recipient));
            }
            foreach (var recipient in listOfCC) // assuming recipients is a List<string>
            {

                mail.CC.Add(new MailAddress(recipient));
            }

            try
            {
                var queryEnavleSsl = new DynamicParameters();
                queryEnavleSsl.Add("@Key", "SYSTEM.ENABLESSL");

                var enableSsl = await _readRepositorySystemSettings.Value.GetAsync(query, queryEnavleSsl, null, "text");


                if (enableSsl.Value.ToLower() == "true") smtpServer.EnableSsl = true;
               

            }
            catch (Exception ex)
            {
                string errMsg = "EmailService" + "," + "EmailSending error: " + ex.ToString();
                throw new Exception(errMsg);
            }



            smtpServer.Send(mail);
            await MailqueueUpadate(email, EmailServiceEnum.Complete, string.Empty, MAILQUEUE_UPDATE_TO_COMPLETED);
            mail.Attachments.Dispose();
            smtpServer.Dispose();

        }


    }
}
