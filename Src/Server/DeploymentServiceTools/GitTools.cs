﻿using System;
using System.Diagnostics;
using System.Threading;

namespace DeploymentServiceTools
{
    public class GitTools : IVersionControlTools
    {
        private readonly string _sourceDirectory;

        public GitTools(string gitPath, string sourceDirectory)
        {
            GitPath = gitPath;
            _sourceDirectory = sourceDirectory;
        }

        private string GitPath { get; set; }

        public void Pull()
        {
            var gitPull = ProcessEx.GetProcess(GitPath,
                "fetch",
                _sourceDirectory);
            using (var exeProcess = Process.Start(gitPull))
            {
                var output = ProcessEx.GetOutput(exeProcess);
                if (exeProcess.ExitCode > 0)
                {
                    throw new Exception("Error during pull source code step" + output);
                }
            }

            SleepFor(10);
        }

        private static string GetRevisionString(string revisionNumber)
        {
            var revision = string.IsNullOrEmpty(revisionNumber) ? string.Empty : "-r " + revisionNumber;
            return revision;
        }

        public void Clone(string revisionNumber)
        {
            var revision = GetRevisionString(revisionNumber);
            var gitClone = ProcessEx.GetProcess(GitPath,
                string.Format(
                    "clone https://jddebruin:Jdd_1501!@github.com/jddebruin/taxihail.git  {0}",
                    _sourceDirectory));
            
            using (var exeProcess = Process.Start(gitClone))
            {
                var output = ProcessEx.GetOutput(exeProcess);
                if (exeProcess.ExitCode > 0)
                {
                    throw new Exception("Error during clone source code step" + output);
                }
            }

            SleepFor(10);
            Update(revisionNumber);

        }

        public void Revert()
        {
            var hgRevert = ProcessEx.GetProcess(GitPath, "reset --hard", _sourceDirectory);
            using (var exeProcess = Process.Start(hgRevert))
            {
                var output = ProcessEx.GetOutput(exeProcess);
                if (exeProcess.ExitCode > 0)
                {
                    throw new Exception("Error during revert source code step" + output);
                }
            }

            SleepFor(10);
        }


        public void Purge()
        {
            var gitPurge = ProcessEx.GetProcess(GitPath, "clean -f -d", _sourceDirectory);
            using (var exeProcess = Process.Start(gitPurge))
            {
                var output = ProcessEx.GetOutput(exeProcess);
                if (exeProcess.ExitCode > 0)
                {
                    throw new Exception("Error during purge source code step" + output);
                }
            }

            SleepFor(10);
        }

        public void Update(string revisionNumber)
        {
            var gitUpdate = ProcessEx.GetProcess(GitPath,
                string.Format(
                    "checkout {0}",
                    revisionNumber),
                _sourceDirectory);
            
            using (var exeProcess = Process.Start(gitUpdate))
            {
                var output = ProcessEx.GetOutput(exeProcess);
                if (exeProcess.ExitCode > 0)
                {
                    throw new Exception("Error during checkout source code step" + output);
                }
            }

            SleepFor(10);
        }

        private void SleepFor(int seconds)
        {
            Thread.Sleep(seconds * 10);
        }
    }
}

