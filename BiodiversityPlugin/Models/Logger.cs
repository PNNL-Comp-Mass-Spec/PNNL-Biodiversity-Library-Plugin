﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using KeggDataLibrary.Models;

namespace BiodiversityPlugin.Models
{
    public class Logger
    {
        private static Logger _logger;
        
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
            body += "Selected Tab Index : " + SelectedTabIndex + linebreak;
            body += "Selected Organism  : " + orgText + linebreak;
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

                //mailClient.UseDefaultCredentials = true;
                MailAddress to = new MailAddress("michael.degan@pnnl.gov");

                MailAddress f = new MailAddress("michael.degan@pnnl.gov");
                MailMessage message = new MailMessage(f, to);
                //message.To = to;
                message.To.Add(new MailAddress("grant.fujimoto@pnnl.gov"));
                message.Subject = subject;
                message.Body = body;
                mailClient.Credentials = new NetworkCredential("BioDiversityErrorLogger@gmail.com", "Amt23Data");
                mailClient.EnableSsl = true;
                mailClient.Send(message);

            }
            catch (Exception)
            {

                var address = "mailto:michael.degan@pnnl.gov;grant.fujimoto@pnnl.gov?subject=" + subject + "&body=" + body;
                try
                {
                    System.Diagnostics.Process.Start(address);
                }
                catch (Exception)
                {
                    MessageBox.Show("That e-mail address is invalid.", "E-mail error");
                }

            }
            

        }
    }
}
