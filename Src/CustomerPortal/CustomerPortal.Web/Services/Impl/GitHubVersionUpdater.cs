﻿using CustomerPortal.Web.Areas.Admin.Repository;
using CustomerPortal.Web.Entities;
using MongoRepository;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CustomerPortal.Web.Helpers;

namespace CustomerPortal.Web.Services.Impl
{
    public class GitHubVersionUpdater : ISourceControl
    {
        public async Task<bool> UpdateVersions()
        {
            var repository = new MongoRepository<Revision>();

            string result = null;

            

            var revisionsFromGitHub = (await GetTagsFromGitHub()).Select(x => new Revision
            {
                Id = Guid.NewGuid().ToString(),
                Commit = x.Commit.Sha,
                Tag = x.Name
            }).ToList();

            revisionsFromGitHub.Add(await GetDefaultRevision());
            
            //Existing Revisions, we ignore version number tags to prevent building a version from an alternate commit.
            //existing revisions => update
            var updatesRevisions = repository
                .AsEnumerable()
                .Where(revision => !IsVersionNumber(revision))
                .Join(revisionsFromGitHub, rev => rev.Tag, revBitbucket => revBitbucket.Tag, (rev, revBitbucket) => new Revision
                {
                    Id = rev.Id,
                    Tag = rev.Tag,
                    Commit = revBitbucket.Commit,
                    Hidden = rev.Hidden,
                    CustomerVisible = rev.CustomerVisible,
                    Inactive = rev.Inactive,
                })
                .ToArray();


            if (updatesRevisions.Any())
            {
                repository.Update(updatesRevisions);
            }

            var tagsFromRepository = repository.Select(x => x.Tag).ToList();
            //new revisions in bitbucket => add to repository
            var newRevisions = revisionsFromGitHub.Where(x => !tagsFromRepository.Contains(x.Tag)).ToList();
            if (newRevisions.Any())
            {
                repository.Add(newRevisions);
            }

            //var hasMoreThan100Tags = links != null ? true : false;
            return false;
        }

        private async Task<List<GitHubRepoResponse>> GetTagsFromGitHub()
        {
            var response = await GetClient().GetAsync("tags?per_page=100");
            var result = JsonConvert.DeserializeObject<GitHubRepoResponse[]>(await response.Content.ReadAsStringAsync()).ToList();

            IEnumerable<string> links;
            response.Headers.TryGetValues("link", out links);
            var nextPageUrl = GetNextPageLink(links.FirstOrDefault());

            while (nextPageUrl != null)
            {
                var nextPageResponse = await GetClient(true).GetAsync(nextPageUrl);
                var nextPageResult = JsonConvert.DeserializeObject<GitHubRepoResponse[]>(await nextPageResponse.Content.ReadAsStringAsync());
                result.AddRange(nextPageResult);

                nextPageResponse.Headers.TryGetValues("link", out links);
                nextPageUrl = GetNextPageLink(links.FirstOrDefault());
            }

            return result;
        }

        private string GetNextPageLink(string linkHeader)
        {
            return WebLinkParser.ParseAndGetNextLink(linkHeader);
        }

        public bool IsVersionNumber(Revision revision)
        {
            //Regex pattern to validate if revision is a version number.
            return Regex.IsMatch(revision.Tag, "^([0-9][0-9]*.[0-9][0-9]*.[0-9][0-9]*(.[0-9][0-9]*)?)$");
        }

        private async Task<Revision> GetDefaultRevision()
        {
            var repository = new MongoRepository<Revision>();

            string result = null;

            var response = await GetClient().GetAsync("branches/master");
            result = await response.Content.ReadAsStringAsync();

            var revisionsFromGitHub =
                JsonConvert.DeserializeObject<GitHubRepoResponse>(result);

            return new Revision
            {
                Id = Guid.NewGuid().ToString(),
                Commit = revisionsFromGitHub.Commit.Sha,
                Tag = "default.tip"
            };


        }
        private static HttpClient GetClient(bool noBaseUrl = false)
        {
            var client = new HttpClient() { BaseAddress = new Uri("https://api.github.com/repos/jddebruin/taxihail/") };

            var gitHubUsername = new AppSettingsReader().GetValue("GitHubUsername", typeof(string));
            var gitHubToken = new AppSettingsReader().GetValue("GitHubToken", typeof(string));

            var authInfo = string.Format("{0}:{1}", gitHubUsername, gitHubToken);
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));

            client.DefaultRequestHeaders.Add("Authorization", "Basic " + authInfo);
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10_4) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.125 Safari/537.36");

            return client;
        }
    }
}
