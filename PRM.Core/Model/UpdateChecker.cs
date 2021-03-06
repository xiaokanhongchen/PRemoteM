﻿using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shawn.Utils;

namespace PRM.Core.Model
{
    public class UpdateChecker
    {
        private readonly string[] urls =
        {
            "https://github.com/VShawn/PRemoteM/wiki",
            "https://github.com/VShawn/PRemoteM",
#if DEV
            "https://github.com/VShawn/PRemoteM-Test/wiki",
#endif
        };


        private string _ignoreVersion;

        /// <summary>
        /// Invoke to notify a newer version of te software was released
        /// while new version code = arg1, download url = arg2
        /// </summary>
        protected readonly Action<string, string> OnNewVersionRelease;

        public UpdateChecker(Action<string, string> onNewVersionRelease, string ignoreVersion = "")
        {
            OnNewVersionRelease = onNewVersionRelease;
            this._ignoreVersion = ignoreVersion;
        }

        /// <summary>
        /// if newer, return true
        /// </summary>
        /// <param name="versionString"></param>
        /// <returns></returns>
        private bool Compare(string versionString)
        {
            var x = FromVersionString(versionString);
            if (x.Item1 > PRMVersion.Major)
                return true;
            if (x.Item1 == PRMVersion.Major
                && x.Item2 > PRMVersion.Minor)
                return true;
            if (x.Item1 == PRMVersion.Major
                && x.Item2 == PRMVersion.Minor
                && x.Item3 > PRMVersion.Build)
                return true;
            if (x.Item1 == PRMVersion.Major
                && x.Item2 == PRMVersion.Minor
                && x.Item3 == PRMVersion.Build
                && x.Item4 > PRMVersion.ReleaseDate)
                return true;
            return false;
        }

        

        /// <summary>
        /// if versionString2 is newer, return true
        /// </summary>
        /// <param name="versionString"></param>
        /// <returns></returns>
        private bool Compare(string versionString1,string versionString2)
        {
            var x1 = FromVersionString(versionString1);
            var x2 = FromVersionString(versionString2);
            if (x2.Item1 > x1.Item1)
                return true;
            if (x2.Item1 == x1.Item1
                && x2.Item2 > x1.Item2)
                return true;
            if (x2.Item1 == x1.Item1
                && x2.Item2 == x1.Item2
                && x2.Item3 > x1.Item3)
                return true;
            if (x2.Item1 == x1.Item1
                && x2.Item2 == x1.Item2
                && x2.Item3 == x1.Item3
                && x2.Item4 > x1.Item4)
                return true;
            return false;
        }


        private Tuple<int, int, int, int> FromVersionString(string versionString)
        {
            var splits = versionString?.Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries);
            if (splits?.Length == 4)
            {
                if (int.TryParse(splits[0], out var major)
                    && int.TryParse(splits[1], out var minor)
                    && int.TryParse(splits[2], out var build)
                    && int.TryParse(splits[3], out var releaseDate)
                )
                {
                    return new Tuple<int, int, int, int>(major, minor, build, releaseDate);
                }
            }
            return new Tuple<int, int, int, int>(-1, -1, -1, -1);
        }

        public Tuple<bool, string, string> CheckUpdate(string url)
        {
            try
            {
                var html = HttpHelper.Get(url).ToLower();
                var vs = Regex.Match(html, @"latest\sversion:\s*([\d|.]*)");
                if (vs.Success)
                {
                    var tmp = vs.ToString();
                    var version = tmp.Substring(tmp.IndexOf("version:") + "version:".Length + 1).Trim();
                    if (Compare(_ignoreVersion, version))
                        if (Compare(version))
                        {
                            return new Tuple<bool, string, string>(true, version, url);
                        }
                }
            }
            catch (Exception e)
            {
                SimpleLogHelper.Warning(e);
            }
            return new Tuple<bool, string, string>(false, "", url);
        }

        /// <summary>
        /// Check if new release, return true + url.
        /// </summary>
        /// <returns></returns>
        public Tuple<bool, string, string> CheckUpdate()
        {
            foreach (var url in urls)
            {
                var tuple = CheckUpdate(url);
                if (tuple.Item1)
                    return tuple;
            }
            return new Tuple<bool, string, string>(false, "", "");
        }

        /// <summary>
        /// Check if new release, invoke OnNewRelease with new version & url.
        /// </summary>
        /// <returns></returns>
        public void CheckUpdateAsync()
        {
            var t = new Task(() =>
            {
                var r = CheckUpdate();
                if (r.Item1)
                {
                    OnNewVersionRelease?.Invoke(r.Item2, r.Item3);
                }
            });
            t.Start();
        }
    }
}
