﻿using System;
using System.Collections.Specialized;
using Model.Definition;
using Model.Entities.Web;

namespace Data.Web.JobMine.Common
{
    public static class Login
    {
        /// <summary>
        ///     Login to JobMine and return wether it is logged in
        /// </summary>
        public static bool LoginToJobMine(ICookieEnabledWebClient client, string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                throw new ArgumentException("UserName and/or PassWord must not be empty or null");
            NameValueCollection logindata = PostData.GetLoginData(userName, password);
            string result = client.UploadValues(JobMineDef.LogInUrl, "POST", logindata).ToString();
            return !String.IsNullOrEmpty(result) && IsLoggedIn(client);
        }

        public static bool IsLoggedIn(ICookieEnabledWebClient client)
        {
            return client.CookieContainer.Count > 5;
        }

        /// <summary>
        ///     Get a new CookieEnabledWebClient that has logged into JobMine
        /// </summary>
        public static ICookieEnabledWebClient GetJobMineLoggedInWebClient(string userName, string password)
        {
            var client = new CookieEnabledWebClient();
            if (LoginToJobMine(client, userName, password))
                return client;
            return null;
        }
    }
}