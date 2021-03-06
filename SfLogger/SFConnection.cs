﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SfLogger.tooling32;
using System.Diagnostics;
using System.Windows;
using Salesforce.Force;
using Salesforce.Common.Attributes;
using Salesforce.Common.Models;
using Salesforce.Common.Serializer;
using Salesforce.Common;
using  System.Net.Http;

namespace SfLogger
{
    public class SFConnection
    {
        public event Action Connected;
        
        public string Username { get; set; }
        public string Password { get; set; }
        public string OrgUrl { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
        public string Api { get; set; }
        public bool IsSandboxUser { get; set; }
        public StringBuilder buffer = new StringBuilder();


        private HttpClient httpClient;
        private AuthenticationClient auth;
        private tooling32.SforceServiceService sforce = new SforceServiceService();
        private ServiceHttpClient serviceClient;
        private List<ApexLog> logsData = new List<ApexLog>();

        //ctor
        public SFConnection(string login, string password, string url, string key, string secret, string api, bool sandbox)
        {
            Username = login;
            Password = password;
            OrgUrl = url;
            Key = key;
            Secret = secret;
            api = Api;
            IsSandboxUser = sandbox;

        }

        public async void Initialize()
        {
            LoginSoap();
            await LoginRest();
        }

        private void LoginSoap()
        {
            try
            {
                sforce.Url = OrgUrl + "/services/Soap/T/32.0";
                var res = sforce.login(Username, Password);
                sforce.SessionHeaderValue = new SessionHeader();
                sforce.SessionHeaderValue.sessionId = res.sessionId;
                Debug.WriteLine("success login: " + res.userInfo.userFullName);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "SOAP error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async Task LoginRest()
        {

            var url = IsSandboxUser == true
                ? "https://test.salesforce.com/services/oauth2/token"
                : "https://login.salesforce.com/services/oauth2/token";

            httpClient = new HttpClient();
            auth = new AuthenticationClient();

            try
            {
                await auth.UsernamePasswordAsync(Key, Secret, Username, Password, url);
                //TODO: verify if needed
                serviceClient = new ServiceHttpClient(auth.InstanceUrl, auth.ApiVersion, auth.AccessToken, httpClient);
                Connected();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "REST authentication error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public void RegisterUser()
        {

            TraceFlag traceFlag = new TraceFlag();
            traceFlag.ApexCode = "Finest";
            traceFlag.ApexProfiling = "Info";
            traceFlag.Callout = "Info";
            traceFlag.Database = "Info";
            traceFlag.System = "Debug";
            traceFlag.Validation = "Info";
            traceFlag.Visualforce = "Info";
            traceFlag.Workflow = "Info";
            var queryRes = sforce.query("select id from user where username = '" + Username + "'");
            var currentUserId = queryRes.records.First().Id;
            traceFlag.TracedEntityId = currentUserId;
            //call the create method
            TraceFlag[] traceFlags = { traceFlag };

            SaveResult[] traceResults = sforce.create(traceFlags);
            for (int i = 0; i < traceResults.Length; i++)
            {

                if (traceResults[i].success)
                {
                    Debug.WriteLine("Successfully created trace flag: " +
                    traceResults[i].id);
                }
                else
                {
                    Debug.WriteLine("Error: could not create trace flag ");
                    Debug.WriteLine(" The error reported was: " +
                    traceResults[i].errors[0].message + "\n");
                }
            }

            Connected();
        }


        private void GetLogsIds()
        {
            var res = sforce.query("select id, operation from apexLog order by starttime desc limit 20");
            logsData = res.records.Cast<ApexLog>().ToList();
        }





        public async Task<IEnumerable<string>> QueryLogs()
        {
            GetLogsIds();
            StringBuilder logs = new StringBuilder();
            foreach (var logData in logsData)
            {
                logs.AppendLine("*** " + logData.Operation + " ***");
                logs.Append(await httpClient.GetStringAsync(
                    OrgUrl + "/services/data/v29.0/sobjects/ApexLog/" + logData.Id + "/Body/"));
            }

            var logsLines = logs.ToString().Split(new[] { '\r', '\n' });

            Connected();
            return logsLines.Where(x => x.Contains("|USER_DEBUG|") || x.Contains("***"));

        }


    }




}
