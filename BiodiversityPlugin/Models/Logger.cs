using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows;
using BiodiversityPlugin.ViewModels;
using BiodiversityPlugin.Views;
using KeggDataLibrary.Models;
using Microsoft.Win32;

namespace BiodiversityPlugin.Models
{
    public class Logger
    {
        private static Logger _logger;

        public string ErrorMessage { get; set; }
        public Organism SelectedOrganism { get; set; }
        public List<string> AllOrgs { get; set; } 
        public int SelectedTabIndex { get; set; }
        public List<Pathway> SelectedPathways { get; set; } 
        public DownloadLocationEnum DownloadLocation { get; set; }
        public bool ContainsToolClient { get; set; }
        public string DatabasePath { get; set; }
        public string DatabaseVersion { get; set; }
        public string DatabaseCreationDate { get; set; }
        public bool ValidSkylineVersion { get; set; }
        public ErrorTypeEnum ErrorType { get; set; }
        public string UserEmail { get; set; }

        public static Logger Instance
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new Logger();
                }
                return _logger;
            }
        }

        private Logger()
        {
            SelectedPathways = new List<Pathway>();
            AllOrgs = new List<string>();
            AllOrgs.Add("No Orgs selected for export");
            SelectedOrganism = null;
            SelectedTabIndex = -1;
            DownloadLocation = DownloadLocationEnum.NONE;
            ErrorType = ErrorTypeEnum.None;

        }

        public void SendEmailLog()
        {

            //var hyperlink = sender as Hyperlink;
            //var address = "mailto:" + hyperlink.NavigateUri.ToString();\

            var orgText = SelectedOrganism != null ? SelectedOrganism.Name : "null";
            var linebreak = "%0D%0A";
            const string subject = "BioDiversity Library Error Log";
            var body = "";
            body += "User Message : " + ErrorMessage + linebreak + linebreak;
            if (UserEmail != "")
            {
                body += "Email to reply to : " + UserEmail + linebreak + linebreak;
            }
            body += "Selected Tab Index : " + SelectedTabIndex + linebreak;
            body += "Selected Organism : " + orgText + linebreak;
            body += "All Organisms Selected : " + linebreak;
            foreach (var org in AllOrgs)
            {
                body += "      -> " + org + linebreak;
            }
            body += linebreak;
            body += "Selected Pathway(s) : " + linebreak;
            foreach (var pathway in SelectedPathways)
            {
                body += "      -> " + pathway.Name + linebreak;
            }
            body += linebreak;
            body += "Download Location Origin : " + DownloadLocation + linebreak;
            body += "Error Type Encountered : " + ErrorType + linebreak;
            body += "Using Skyline Tool Client : " + ContainsToolClient + linebreak;
            body += "Has Valid Skyline version : " + ValidSkylineVersion + linebreak;
            body += "Database Path : " + DatabasePath + linebreak;
            body += "Database Version : " + DatabaseVersion + linebreak;
            body += "Database Creation Date : " + DatabaseCreationDate + linebreak;
            

            try
            {

                SmtpClient mailClient = new SmtpClient("smtp.gmail.com", 587);
                MailMessage message = new MailMessage();
                message.From = new MailAddress("BioDiversityErrorLogger@gmail.com");
                message.To.Add(new MailAddress("lillian.ryadinskiy@pnnl.gov"));
                message.To.Add(new MailAddress("michael.degan@pnnl.gov"));
                message.To.Add(new MailAddress("grant.fujimoto@pnnl.gov"));
                message.Subject = subject;
                message.Body = body;
                mailClient.Credentials = new NetworkCredential("BioDiversityErrorLogger@gmail.com", "Amt23Data");
                mailClient.EnableSsl = true;
                mailClient.Send(message);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                object mailClient = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\Mail", "", "none");
                var mail = mailClient.ToString();
                if (string.IsNullOrEmpty(mail))
                {
                    var address = "mailto:michael.degan@pnnl.gov;lillian.ryadinskiy@pnnl.gov;grant.fujimoto@pnnl.gov?subject=" + subject + "&body=" +
                                  body;
                    try
                    {
                        System.Diagnostics.Process.Start(address);
                    }
                    catch (Exception ey)
                    {
                        Console.WriteLine(ey.Message);
                        MessageBox.Show("That e-mail address is invalid.", "E-mail error");
                    }
                }
                else
                {
                    var tempPath = Path.GetTempPath();
                    Directory.CreateDirectory(Path.Combine(tempPath, "BioDiversityLogger"));
                    tempPath = Path.Combine(tempPath, "BioDiversityLogger");
                    tempPath = Path.Combine(tempPath, "bioDiversityLog.txt");

                    body = body.Replace(linebreak, "\n\r\n\r");

                    using (var tempWriter = new StreamWriter(tempPath))
                    {
                        tempWriter.Write(body);
                    }

                    var navBoxVM = new ClickableErrorMessageBox(Path.GetDirectoryName(tempPath));
                    var navBox = new ClickableErrorMessageBoxView();
                    navBox.DataContext = navBoxVM;
                    navBox.Show();
                }

            }
            

        }
    }
}
