﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace MetadataService.Services.Files
{
    public class VreCookieRequestFilterAttribute : RequestFilterAttribute
    {
        private const string _API_URL_VARIABLE_NAME = "VF_VRE_API_URL";
        private const string _httpLocalhostApi = "http://localhost/api/";
        private const string _sessionserviceurl = "vfsession/";
        private const string _authproxyserviceurl = "authproxy/get_signed_url/";

        private static readonly Dictionary<string, string> sessionuser = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> sessionauthproxy = new Dictionary<string, string>();
        private static readonly object initlock = new object();

        private readonly string _vreapiurl = Environment.GetEnvironmentVariable(_API_URL_VARIABLE_NAME) != null
            ? Environment.GetEnvironmentVariable(_API_URL_VARIABLE_NAME)
            : _httpLocalhostApi;

        static VreCookieRequestFilterAttribute()
        {
           
            ServicePointManager.ServerCertificateValidationCallback +=
                ValidateRemoteCertificate;
        }


        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            //get sessionid from cookie
            Cookie mysession;
            try
            {
                mysession = req.Cookies["sessionid"];
                if (mysession == null) return; //no cookie set - return
            }
            catch (KeyNotFoundException) //no cookie set - either needs to log in or in single user deployment - it is vagrant user
            {
                mysession = new Cookie();
                mysession.Value = "west-life_vf_insecure_session_id";
            }

            //get user info related to session id fromVRE
            string loggeduser;
            string authproxy;
            lock (initlock)
            {
                loggeduser = GetAssociatedUser(mysession.Value);
                authproxy = GetAuthProxy(mysession.Value, req.GetUrlHostName());
            }
            //Console.WriteLine("Provider Service list"+loggeduser);
            //TODO get the providers associated to user
            req.Items.Add("userid", loggeduser);
            if (requestDto.GetType() == typeof(ProviderItem))
                ((ProviderItem) requestDto).loggeduser = loggeduser;
            req.Items.Add("authproxy", authproxy);
            //throw new NotImplementedException();
        }

        private string GetAssociatedUser(string sessionid)
        {
            if (sessionuser.ContainsKey(sessionid)) return sessionuser[sessionid];
            //fix check server certificate - certificates probably not installed for MONO environment
            var client = new JsonServiceClient(_vreapiurl);
            try
            {
                var response = client.Get<DjangoUserInfo>(_sessionserviceurl + sessionid);
                sessionuser[sessionid] = response.username;
                Console.WriteLine("sessionid " + sessionid + " belongs to " + response.username);
                return response.username;
            }
            catch (Exception e)
            {
                Console.WriteLine("error during getting user info of sessionid " + sessionid + " " + e.Message +
                                  e.StackTrace);
                return "";
            }
        }

        private string GetAuthProxy(string sessionid, string domain)
        {
            if (sessionauthproxy.ContainsKey(sessionid))
                return sessionauthproxy[sessionid];
            //fix check server certificate - certificates probably not installed for MONO environment
            var client = new JsonServiceClient(_vreapiurl);
            try
            {
                client.CookieContainer = new CookieContainer();
                client.CookieContainer.Add(new Cookie("sessionid", sessionid) {Domain = domain});
                var response = client.Get<DjangoAuthproxyInfo>(_authproxyserviceurl);
                sessionauthproxy[sessionid] = response.signed_url;
                Console.WriteLine("authproxy " + response.signed_url);
                return response.signed_url;
            }
            catch (Exception e)
            {
                Console.WriteLine("error during getting authproxy info of sessionid " + sessionid + " domain " +
                                  domain + " \n" + e.Message + e.StackTrace);
                return "";
            }
        }


        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain,
            SslPolicyErrors policyErrors)
        {
            //check subject for portal.west-life.eu on portal deployment it raises SslPolicyError
            if (cert.Subject.Contains("portal.west-life.eu"))
                //Console.WriteLine(policyErrors);
                return true;
            else
                return policyErrors == SslPolicyErrors.None; 
            
        }
    }
}