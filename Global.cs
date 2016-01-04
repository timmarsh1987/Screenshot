using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace TimMarsh.Screenshot
{
    public class Global
    {
        public static IWebDriver Driver;

        private static string _screenshotPath;
        public static string ScreenshotPath
        {
            get
            {
                if (!String.IsNullOrEmpty(_screenshotPath))
                {
                    return _screenshotPath;
                }
                if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["ScreenshotPath"]))
                {
                    return ConfigurationManager.AppSettings["ScreenshotPath"];
                }
                return AppDomain.CurrentDomain.BaseDirectory + "Screenshots";
            }
            set
            {
                _screenshotPath = value;
            }
        }

        public static string FilePath
        {
            get
            {
                return ScreenshotPath + "\\" + FileName;
            }
        }

        private static string _fileName;
        private static string FileName
        {
            get
            {
                if (String.IsNullOrEmpty(_fileName))
                {
                    string file = Driver.Url
                        .Replace(@"http://", "")
                        .Replace(@".", "")
                        .Replace("/", @"\");

                    _fileName = file.Substring(0, file.IndexOf("?") > 0 ? file.IndexOf("?") : file.Length);
                }
                return _fileName;
            }
        }
    }
}
