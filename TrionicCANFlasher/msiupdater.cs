using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;


namespace CommonSuite
{
    class msiupdater
    {
        private readonly Version m_currentversion;
        private string m_githubURL = "";
        private Version m_NewVersion;
        private bool m_blockauto_updates;

        public bool Blockauto_updates
        {
            get { return m_blockauto_updates; }
            set { m_blockauto_updates = value; }
        }

        public Version NewVersion
        {
            get { return m_NewVersion; }
            set { m_NewVersion = value; }
        }
        public delegate void DataPump(MSIUpdaterEventArgs e);
        public event msiupdater.DataPump onDataPump;

        public delegate void UpdateProgressChanged(MSIUpdateProgressEventArgs e);
        public event msiupdater.UpdateProgressChanged onUpdateProgressChanged;
        public class MSIUpdateProgressEventArgs : System.EventArgs
        {
            private Int32 _NoFiles;
            private Int32 _NoFilesDone;
            private Int32 _PercentageDone;
            private Int32 _NoBytes;
            private Int32 _NoBytesDone;

            public Int32 NoBytesDone
            {
                get
                {
                    return _NoBytesDone;
                }
            }

            public Int32 NoBytes
            {
                get
                {
                    return _NoBytes;
                }
            }


            public Int32 NoFiles
            {
                get
                {
                    return _NoFiles;
                }
            }
            public Int32 NoFilesDone
            {
                get
                {
                    return _NoFilesDone;
                }
            }
            public Int32 PercentageDone
            {
                get
                {
                    return _PercentageDone;
                }
            }


            public MSIUpdateProgressEventArgs(Int32 NoFiles, Int32 NoFilesDone, Int32 PercentageDone, Int32 NoBytes, Int32 NoBytesDone)
            {
                _NoFiles = NoFiles;
                _NoFilesDone = NoFilesDone;
                _PercentageDone = PercentageDone;
                _NoBytes = NoBytes;
                _NoBytesDone = NoBytesDone;
            }
        }


        public class MSIUpdaterEventArgs : System.EventArgs
        {
            private string _Data;
            private bool _UpdateAvailable;
            private bool _Version2High;
            private Version _Version;
            private string _xmlFile;


            private string _msiFile;

            public string MSIFile
            {
                get
                {
                    return _msiFile;
                }
            }
            public string Data
            {
                get
                {
                    return _Data;
                }
            }
            public bool UpdateAvailable
            {
                get
                {
                    return _UpdateAvailable;
                }
            }
            public bool Version2High
            {
                get
                {
                    return _Version2High;
                }
            }
            public Version Version
            {
                get
                {
                    return _Version;
                }
            }
            public MSIUpdaterEventArgs(string Data, bool Update, bool mVersion2High, Version version, string msiFile)
            {
                _Data = Data;
                _UpdateAvailable = Update;
                _Version2High = mVersion2High;
                _Version = version;
                _msiFile = msiFile;
            }
        }

        public msiupdater(Version CurrentVersion)
        {
            m_currentversion = CurrentVersion;
            m_NewVersion = new Version("0.0.0.0");
        }

        public void CheckForUpdates(string githubUrl)
        {
            m_githubURL = githubUrl;
            if (!m_blockauto_updates)
            {
                System.Threading.Thread t = new System.Threading.Thread(updatecheck);
                t.Start();
            }
        }

        public void ExecuteUpdate(string msiFile)
        {
            try
            {
                System.Diagnostics.Process.Start(msiFile);
            }
            catch (Exception E)
            {
                PumpString("Exception when checking new update(s): " + E.Message, false, false, new Version(), "");
            }
        }


        private void PumpString(string text, bool updateavailable, bool version2high, Version version, string msiFile)
        {
            onDataPump(new MSIUpdaterEventArgs(text, updateavailable, version2high, version, msiFile));
        }

        public string GetPageHTML(string pageUrl, int timeoutSeconds)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // framework 4.5 replace with SecurityProtocolType.Tls12;
            System.Net.WebResponse response = null;

            try
            {
                // Setup our Web request
                HttpWebRequest request = (HttpWebRequest)System.Net.WebRequest.Create(pageUrl);
                request.UserAgent = "Mozilla/5.0";
                request.Timeout = timeoutSeconds * 1000;

                try
                {
                    request.Proxy.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
                }
                catch (Exception proxyE)
                {
                    PumpString("Error setting proxy server: " + proxyE.Message, false, false, new Version(), "");
                }

                // Retrieve data from request
                response = request.GetResponse();

                System.IO.Stream streamReceive = response.GetResponseStream();
                System.Text.Encoding encoding = System.Text.Encoding.GetEncoding("utf-8");
                System.IO.StreamReader streamRead = new System.IO.StreamReader(streamReceive, encoding);

                return streamRead.ReadToEnd();
            }
            catch (Exception ex)
            {
                // Error occured grabbing data, return empty string.
                PumpString("An error occurred while retrieving the HTML content. " + ex.Message, false, false, new Version(), "");

                return "";
            }
            finally
            {
                // Check if exists, then close the response.
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        private void updatecheck()
        {
            string releaseInfo="";
            bool m_updateavailable = false;
            bool m_version_toohigh = false;
            Version maxversion = new Version("0.0.0.0");
            string msiFile = "";

            try
            {
                releaseInfo = GetPageHTML(m_githubURL, 10);
                JObject release = JObject.Parse(releaseInfo);

                string tag_name = (string)release["tag_name"]; // "TrionicCanFlasher_v0.1.72.0"
                int index = tag_name.IndexOf("_v", 0, tag_name.Length-1, StringComparison.CurrentCulture);
                Version v = new Version(tag_name.Substring(index + 2));
                if (v > m_currentversion)
                {
                    if (v > maxversion) maxversion = v;
                    m_updateavailable = true;
                    PumpString("Available version: " + tag_name, false, false, v, "");
                }
                else if (v.Major < m_currentversion.Major || (v.Major == m_currentversion.Major && v.Minor < m_currentversion.Minor) || (v.Major == m_currentversion.Major && v.Minor == m_currentversion.Minor && v.Build < m_currentversion.Build))
                {
                    // mmm .. gebruiker draait een versie die hoger is dan dat is vrijgegeven... 
                    if (v > maxversion) maxversion = v;
                    m_updateavailable = false;
                    m_version_toohigh = true;
                }

                IList<JToken> results = release["assets"].Children().ToList();

                IList<ReleaseAsset> searchResults = new List<ReleaseAsset>();
                foreach (JToken result in results)
                {
                    ReleaseAsset searchResult = result.ToObject<ReleaseAsset>();
                    if (searchResult.browser_download_url.Contains("msi")) {
                        msiFile = searchResult.browser_download_url;
                    }
                }
                
                if (m_updateavailable)
                {
                    PumpString("A newer version is available: " + maxversion.ToString(), m_updateavailable, m_version_toohigh, v, msiFile);
                    m_NewVersion = maxversion;
                }
                else if (m_version_toohigh)
                {
                    PumpString("Versionnumber is too high: " + maxversion.ToString(), m_updateavailable, m_version_toohigh, v, msiFile);
                    m_NewVersion = maxversion;
                }
                else
                {
                    PumpString("No new version(s) found...", false, false, new Version(), "");
                }
            }
            catch (Exception tuE)
            {
                PumpString(tuE.Message, false, false, new Version(), "");
            }
            
        }
    }
}
