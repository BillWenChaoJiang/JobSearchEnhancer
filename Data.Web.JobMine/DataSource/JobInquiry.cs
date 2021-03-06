﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Common.Utility;
using Data.Contract.JobMine.Interface;
using Data.Web.JobMine.Common;
using HtmlAgilityPack;
using Model.Definition;
using Model.Entities;
using Model.Entities.JobMine;
using Model.Entities.Web;

namespace Data.Web.JobMine.DataSource
{
    //public class JobInquiry : IJobInquiry
    //{

    //    static CookieEnabledWebClient Client { get; set; }
    //    public JobInquiry(CookieEnabledWebClient client)
    //    {
    //        Client = client;
    //    }
    //    /// <summary>
    //    ///     Get all the JobOverViews from inquiry page
    //    /// </summary>
    //    public IEnumerable<JobOverView> GetJobOverViews(string term, string jobStatus) { return JobInquiryHelpper.GetJobInquiryPageObject(Client, term, jobStatus, GetJobOverView); }

    //    public IEnumerable<string> GetJobIds(string term, string jobStatus) { return JobInquiryHelpper.GetJobInquiryPageObject(Client, term, jobStatus, GetJobId); }

    //    private static JobOverView GetJobOverView(HtmlNode row, int count)
    //    {
    //        string jobId = GetConvertedNodeInnerHtml(row, ColumnPath.JobId, count);
    //        var jobOverView = new JobOverView
    //        {
    //            JobTitle = " ", //GetConvertedNodeInnerHtml(row, ColumnPath.JobTitle, count),
    //            Employer = new Employer
    //            {
    //                Name = GetConvertedNodeInnerHtml(row, ColumnPath.EmployerName, count),
    //                UnitName = GetConvertedNodeInnerHtml(row, ColumnPath.UnitName, count)
    //            },
    //            JobLocation = new JobLocation
    //            {
    //                Region = GetConvertedNodeInnerHtml(row, ColumnPath.Region, count)
    //            },
    //            Id = Convert.ToInt32(jobId),
    //        };
    //        if (Utility.IsCorrectJobId(jobId) && Utility.IsJobOverViewCompleted(jobOverView))
    //            return jobOverView;
    //        return null;
    //    }

    //    private static string GetJobId(HtmlNode row, int count) { return GetConvertedNodeInnerHtml(row, ColumnPath.JobId, count); }

    //    private static string GetConvertedNodeInnerHtml(HtmlNode row, string path, int count)
    //    {
    //        return
    //            row.SelectSingleNode(path + count + "']")
    //                .InnerHtml.Replace("&nbsp;", " ")
    //                .Replace("<br />", "\n")
    //                .Replace("&amp;", "&");
    //    }
    //}

    public class JobInquiry : IJobInquiry
    {
        /// <summary>
        ///     Because each real row are accompany by a row of empty text and first row is the title row, this is why the first
        ///     row index starts at 3
        /// </summary>
        private const int FirstRowIndex = 3;

        /// <summary>
        ///     Constant value that indicate it is the first time searching the job inquiry page, which means some extra operation
        ///     needs to be completed
        /// </summary>
        private const int FirstSearch = -1;

        public JobInquiry(ICookieEnabledWebClient client)
        {
            Client = client;
        }

        private static ICookieEnabledWebClient Client { get; set; }


        public bool AddJobToShortList(int jobId, string term, string jobStatus, string jobTitle, string employerName)
        {
            int numPages = FirstSearch;
            string iCAction = IcAction.Search;
            string iCsid = "";
            int iCStateNum = 1;

            for (int currentPageNum = 1; (numPages == FirstSearch) || currentPageNum <= numPages; currentPageNum++)
            {
                Trace.TraceInformation("GetJobOverViews Parsing Page " + currentPageNum);
                var doc = new HtmlDocument();
                SetInquiryData(Client, numPages, ref iCAction, ref iCsid, ref iCStateNum);

                string jobinfo = GetJobinfo(Client, iCAction, term, iCsid, iCStateNum, jobStatus, jobTitle, employerName);
                doc.LoadHtml(jobinfo);

                if (iCAction == IcAction.Search)
                    numPages = GetNumberOfPages(doc);

                if (FindJobAndAddToShortListInCurrentPage(doc, jobId, iCStateNum, iCsid))
                    return true;
            }
            return false;
        }

        private static bool FindJobAndAddToShortListInCurrentPage(HtmlDocument doc, int jobId, int iCStateNum, string iCsid)
        {
            HtmlNode thisTableNode = GetTableNode(doc);
            for (int childIndex = FirstRowIndex, count = 0; (childIndex < thisTableNode.ChildNodes.Count); childIndex++)
            {
                HtmlNode row = thisTableNode.ChildNodes[childIndex];
                if (row.Name == "tr")
                {
                    string thisJobId = GetConvertedNodeInnerHtml(row, ColumnPath.JobId, count);
                    if (Convert.ToInt32(thisJobId) == jobId)
                    {
                        JobOverView thisJobOverView = GetJobOverView(row, count);
                        if(!thisJobOverView.OnShortList)
                        {
                            var addSuccess = AddToShortList(count, iCStateNum, iCsid);
                            return addSuccess;
                        }
                    }
                    count++;
                }
            }
            return false;
        }

        private static bool AddToShortList(int count, int iCStateNum, string iCsid)
        {
            string iCAction = "UW_CO_SLIST_HL${0}".FormatString(count);
            iCStateNum++;
            const string url = JobMineDef.JobInquiryUrlShortpsc, method = "POST";
            NameValueCollection jobInquiryData = PostData.GetAddJobToShortListData(iCStateNum.ToString(CultureInfo.InvariantCulture),iCAction, iCsid);
            byte[] values = Client.UploadValues(url, method, jobInquiryData);

            var doc = new HtmlDocument();
            doc.LoadHtml(Encoding.UTF8.GetString(values));
            try
            {
                string innerHtml =
                    doc.DocumentNode.SelectNodes("//div[@id='alertmsg']/span[@class='popupText']")[0].InnerHtml;
                return innerHtml.Contains("The job has been added successfully.");
            }
            catch (Exception e)
            {
                Trace.TraceWarning(e.ToString());
            }
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="term"></param>
        /// <param name="jobStatus"></param>
        /// <returns></returns>
        public IEnumerable<JobOverView> GetJobOverViews(string term, string jobStatus)
        {
            int numPages = FirstSearch;
            string iCAction = IcAction.Search;
            string iCsid = "";
            int iCStateNum = 1;

            for (int currentPageNum = 1; (numPages == FirstSearch) || currentPageNum <= numPages; currentPageNum++)
            {
                Trace.TraceInformation("GetJobOverViews Parsing Page " + currentPageNum);
                var doc = new HtmlDocument();
                SetInquiryData(Client, numPages, ref iCAction, ref iCsid, ref iCStateNum);

                string jobinfo = GetJobinfo(Client, iCAction, term, iCsid, iCStateNum, jobStatus);
                doc.LoadHtml(jobinfo);

                if (iCAction == IcAction.Search)
                    numPages = GetNumberOfPages(doc);

                foreach (var jobOverView in GetCurrentPageJobOverViews(doc))
                    yield return jobOverView;
            }
            yield return null;
        }

        public IEnumerable<string> GetJobIds(string term, string jobStatus)
        {
            int numPages = FirstSearch;
            string iCAction = IcAction.Search;
            string iCsid = "";
            int iCStateNum = 1;
            var jobIds = new Queue<string>();

            for (int currentPageNum = 1; currentPageNum <= numPages || numPages == FirstSearch; currentPageNum++)
            {
                var doc = new HtmlDocument();
                SetInquiryData(Client, numPages, ref iCAction, ref iCsid, ref iCStateNum);

                string jobinfo = GetJobinfo(Client, iCAction, term, iCsid, iCStateNum, jobStatus);
                doc.LoadHtml(jobinfo);

                if (iCAction == IcAction.Search)
                    numPages = GetNumberOfPages(doc);

                GetCurrentPageJobIDs(doc, jobIds);
            }
            return jobIds;
        }

        private static void SetInquiryData(ICookieEnabledWebClient client, int numPages, ref string iCAction, ref string iCsid, ref int iCStateNum)
        {
            var doc = new HtmlDocument();
            if (iCAction != IcAction.Down && numPages != FirstSearch)
                iCAction = IcAction.Down;

            if (iCAction == IcAction.Search)
            {
                string srcUrl = GetIframeSrcUrl(client);
                string downloadString = client.DownloadString(srcUrl);
                doc.LoadHtml(downloadString);
                iCsid = GetIcsid(doc);
                iCStateNum = GetIcStateNum(doc);
            }
            else
                iCStateNum++;
        }

        private static IEnumerable<JobOverView> GetCurrentPageJobOverViews(HtmlDocument doc)
        {
            HtmlNode thisTableNode = GetTableNode(doc);
            for (int childIndex = FirstRowIndex, count = 0; (childIndex < thisTableNode.ChildNodes.Count); childIndex++)
            {
                HtmlNode row = thisTableNode.ChildNodes[childIndex];
                if (row.Name == "tr")
                {
                    JobOverView thisJobOverView = GetJobOverView(row, count);
                    yield return thisJobOverView;
                    count++;
                }
            }
        }

        private static void GetCurrentPageJobIDs(HtmlDocument doc, Queue<string> jobIds)
        {
            HtmlNode thisTableNode = GetTableNode(doc);
            for (int childIndex = FirstRowIndex, count = 0; childIndex < thisTableNode.ChildNodes.Count; childIndex++)
            {
                HtmlNode row = thisTableNode.ChildNodes[childIndex];
                if (row.Name != "tr") continue;
                string thisJobId = GetConvertedNodeInnerHtml(row, ColumnPath.JobId, count);
                if (!IsCorrectJobId(thisJobId)) continue;
                jobIds.Enqueue(thisJobId);
                count++;
            }
        }

        private static string GetJobinfo(ICookieEnabledWebClient client, string iCAction, string term, string iCsid,
            int iCStateNum, string jobStatus, string jobTitle = null, string empolyerName = null)
        {
            const string url = JobMineDef.JobInquiryUrlShortpsc, method = "POST";
            NameValueCollection jobInquiryData = PostData.GetJobInquiryData(iCStateNum.ToString(CultureInfo.InvariantCulture), iCAction, iCsid, term, jobStatus, jobTitle, empolyerName);
            byte[] values = client.UploadValues(url, method, jobInquiryData);
            return Encoding.UTF8.GetString(values);
        }

        private static int GetNumberOfPages(HtmlDocument doc)
        {
            string currentJobsDisplayString =
                doc.DocumentNode.SelectNodes("//span[@class='PSGRIDCOUNTER']")[1].InnerHtml;
            //var currentJobsDisplayString = doc.DocumentNode.SelectSingleNode("/page[1]/field[1]/tr[29]/td[2]/div[1]/table[1]/tr[2]/td[1]/table[1]/tr[2]/td[1]/div[1]/span[2]").InnerHtml;
            const string seperator = "of ";
            int numberOfJobs =
                Convert.ToInt32(
                    currentJobsDisplayString.Substring(
                        currentJobsDisplayString.IndexOf(seperator, StringComparison.Ordinal) + seperator.Length));


            return numberOfJobs/25 + ((numberOfJobs%25 == 0) ? 0 : 1);
        }

        public static bool IsJobOverViewCompleted(JobOverView jov)
        {
            return jov.Id > 0 && !string.IsNullOrEmpty(jov.JobTitle) &&
                   !string.IsNullOrEmpty(jov.Employer.Name) && !string.IsNullOrEmpty(jov.JobLocation.Region);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <remarks>Safe Extract: doc.DocumentNode.SelectSingleNode("//table[@id='UW_CO_JOBRES_VW$scroll$0']");</remarks>
        /// <returns></returns>
        private static HtmlNode GetTableNode(HtmlDocument doc)
        {
            return doc.DocumentNode.SelectSingleNode("//table[@class='PSLEVEL1GRID']");
                //doc.DocumentNode.SelectSingleNode(
                //    "/page[1]/field[1]/tr[29]/td[2]/div[1]/table[1]/tr[2]/td[1]/table[1]/tr[1]/td[1]/table[1]");
        }

        private static JobOverView GetJobOverView(HtmlNode row, int count)
        {
            string jobId = GetConvertedNodeInnerHtml(row, ColumnPath.JobId, count);
            if (IsCorrectJobId(jobId))
            {
                string numberOfOpeningString = GetConvertedNodeInnerHtml(row, ColumnPath.NumberOfOpening, count);
                string numberOfAppliedString = GetConvertedNodeInnerHtml(row, ColumnPath.NumberOfApplied, count);
                bool alreadyApplied = GetConvertedNodeInnerHtml(row, ColumnPath.AlreadyApplied, count) == "Already Applied";
                bool onShortList = GetConvertedNodeInnerHtml(row, ColumnPath.OnShortList, count) == "On Short List";
                string lastDateToApplyString = GetConvertedNodeInnerHtml(row, ColumnPath.LastDateToApply, count);
                DateTime? lastDateToApply = string.IsNullOrWhiteSpace(lastDateToApplyString) ? (DateTime?) null : DateTime.Parse(lastDateToApplyString);
                return new JobOverView
                {
                    JobTitle = GetConvertedNodeInnerHtml(row, ColumnPath.JobTitle, count),
                    Employer = new Employer
                    {
                        Name = GetConvertedNodeInnerHtml(row, ColumnPath.EmployerName, count),
                        UnitName = GetConvertedNodeInnerHtml(row, ColumnPath.UnitName, count)
                    },
                    JobLocation = new JobLocation {Region = GetConvertedNodeInnerHtml(row, ColumnPath.Region, count)},
                    Id = Convert.ToInt32(jobId),
                    NumberOfOpening = string.IsNullOrWhiteSpace(numberOfOpeningString) ? 0 : Convert.ToInt32(numberOfOpeningString),
                    NumberOfApplied = string.IsNullOrWhiteSpace(numberOfAppliedString) ? 0 : Convert.ToInt32(numberOfAppliedString),
                    AlreadyApplied = alreadyApplied,
                    OnShortList = onShortList,
                    LastDateToApply = lastDateToApply
                };
            }
            return new JobOverView();
        }

        private static string GetConvertedNodeInnerHtml(HtmlNode row, string path, int count)
        {
            string convertedNodeInnerHtml = row.SelectSingleNode(String.Format(path, count))
                .InnerHtml.Replace("&nbsp;", " ")
                .Replace("<br />", "\n")
                .Replace("&amp;", "&");
            return convertedNodeInnerHtml;
        }

        /// <summary>
        ///     Get the Iframe Url of the job search result table
        /// </summary>
        /// <param name="client">Loggedin WebClient</param>
        /// <returns>Iframe URL</returns>
        private static string GetIframeSrcUrl(ICookieEnabledWebClient client)
        {
            //var doc = new HtmlDocument();
            //string inquirypagehtml = client.DownloadString(JobMineDef.JobInquiryUrlpsp);
            //doc.LoadHtml(inquirypagehtml);
            //string src =
            //    doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[3]/div[1]/iframe[1]").Attributes["src"].Value;
            //src = doc.DocumentNode.SelectSingleNode("//iframe[@id='ptifrmtgtframe']").Attributes["src"].Value;
            return
                @"https://jobmine.ccol.uwaterloo.ca/psc/SS/EMPLOYEE/WORK/c/UW_CO_STUDENTS.UW_CO_JOBSRCH.GBL?pslnkid=UW_CO_JOBSRCH_LINK&amp;FolderPath=PORTAL_ROOT_OBJECT.UW_CO_JOBSRCH_LINK&amp;IsFolder=false&amp;IgnoreParamTempl=FolderPath%2cIsFolder&amp;PortalActualURL=https%3a%2f%2fjobmine.ccol.uwaterloo.ca%2fpsc%2fSS%2fEMPLOYEE%2fWORK%2fc%2fUW_CO_STUDENTS.UW_CO_JOBSRCH.GBL%3fpslnkid%3dUW_CO_JOBSRCH_LINK&amp;PortalContentURL=https%3a%2f%2fjobmine.ccol.uwaterloo.ca%2fpsc%2fSS%2fEMPLOYEE%2fWORK%2fc%2fUW_CO_STUDENTS.UW_CO_JOBSRCH.GBL%3fpslnkid%3dUW_CO_JOBSRCH_LINK&amp;PortalContentProvider=WORK&amp;PortalCRefLabel=Job%20Inquiry&amp;PortalRegistryName=EMPLOYEE&amp;PortalServletURI=https%3a%2f%2fjobmine.ccol.uwaterloo.ca%2fpsp%2fSS%2f&amp;PortalURI=https%3a%2f%2fjobmine.ccol.uwaterloo.ca%2fpsc%2fSS%2f&amp;PortalHostNode=WORK&amp;NoCrumbs=yes&amp;PortalKeyStruct=yes";
        }

        /// <summary>
        ///     Get the ICStateNum of the page in Html
        /// </summary>
        /// <param name="doc">Current page in Html</param>
        /// <returns>string ICStateNum</returns>
        /// <remarks>Safe Extraction: doc.DocumentNode.SelectSingleNode("//input[@id='ICStateNum']").Attributes["value"].Value;</remarks>
        /// <remarks>Lazy Extraction: doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/input[3]").Attributes["value"].Value;</remarks>
        private static int GetIcStateNum(HtmlDocument doc)
        {
            string iCStateNum = doc.DocumentNode.SelectSingleNode("//input[@id='ICStateNum']").Attributes["value"].Value;
            return Convert.ToInt32(iCStateNum);
        }

        /// <summary>
        ///     Get the ICSID of the page in Html
        /// </summary>
        /// <param name="doc">Current page in Html</param>
        /// <returns>string ICSID</returns>
        /// <remarks>Safe Extraction: doc.DocumentNode.SelectSingleNode("//input[@id='ICSID']").Attributes["value"].Value;</remarks>
        /// <remarks>Lazy Extraction: doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/input[13]").Attributes["value"].Value;</remarks>
        private static string GetIcsid(HtmlDocument doc)
        {
            string iCsid = doc.DocumentNode.SelectSingleNode("//input[@id='ICSID']").Attributes["value"].Value;
            return iCsid;
        }

        public static bool IsCorrectJobId(string jobId)
        {
            var regex = new Regex("[0-9]{8,8}");
            bool right = regex.IsMatch(jobId);
            return regex.IsMatch(jobId);
        }

        public struct ColumnPath
        {
            public const string JobId = "//span[@id='UW_CO_JOBRES_VW_UW_CO_JOB_ID${0}']";
            public const string JobTitle = "//div[@id='win0divUW_CO_JOBTITLE_HL${0}']/span[@title='Job Title (Hyper Link)']/a[@class='PSHYPERLINK']";
            public const string EmployerName = "//span[@id='UW_CO_JOBRES_VW_UW_CO_PARENT_NAME${0}']";
            public const string Region = "//span[@id='UW_CO_JOBRES_VW_UW_CO_WORK_LOCATN${0}']";
            public const string UnitName = "//span[@id='UW_CO_JOBRES_VW_UW_CO_EMPLYR_NAME1${0}']";
            public const string NumberOfOpening = "//span[@id='UW_CO_JOBRES_VW_UW_CO_OPENGS${0}']";
            public const string NumberOfApplied = "//span[@id='UW_CO_JOBAPP_CT_UW_CO_MAX_RESUMES${0}']";
            public const string AlreadyApplied = "//div[@id='win0divUW_CO_APPLY_HL${0}']/span[@title='Apply to Position']";
            public const string OnShortList = "//div[@id='win0divUW_CO_SLIST_HL${0}']/span[@title='Short List HyperLink']";
            public const string LastDateToApply = "//span[@id='UW_CO_JOBRES_VW_UW_CO_CHAR_DATE${0}']";
        }
    }
}