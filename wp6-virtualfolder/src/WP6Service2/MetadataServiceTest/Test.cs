﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using MetadataService;
using MetadataService.Services.Files;
using MetadataService.Services.Settings;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using WP6Service2.Services.Cerif;
using WP6Service2.Services.Dataset;
using WP6Service2.Services.PerUserProcess;

namespace MetadataServiceTest
{
    [TestFixture]
    public class Test
    {
        private readonly string _baseUri = "http://localhost:8001/metadataservice/";
        //private readonly string _baseUri = "http://localhost:8002/metadataservice/";
        

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            System.Environment.SetEnvironmentVariable("VF_ALLOW_FILESYSTEM","true");
            try
            {
                Program.StartHost(_baseUri, null);
                Thread.Sleep(500);
            }
            catch (Exception)
            {
                Console.WriteLine("metadataservice already running by another process, testing ...");
            }
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            //Dispose it on TearDown
            Program.StopHost();
        }

        private static void PrepareTestProviderItems(out ProviderItem item, out ProviderItem item2,
            out string testmessage,
            out string testpassword)
        {
            item = new ProviderItem();
            item2 = new ProviderItem();
            testmessage = "secretmessage";
            item2.securetoken = item.securetoken = testmessage;
            item.loggeduser = item2.loggeduser = "testuser";
            testpassword = "testkey89012";
        }

        [Test]
        public void CheckDeletingDatasetWithForeignKeyDisabledTestCase()
        {
            //the same as dataset2TestCase - calls NoFK service. Warning released when cascade delete doesnt work =>
            //foreign key is very probably disabled
            var client = new JsonServiceClient(_baseUri);
            var entries = client.Get(new GetEntries());

            var datasetentries = client.Get(new GetDatasetEntries());
            var dErelations = datasetentries.Count;
            var myEntries = new List<DatasetEntry>();
            myEntries.Add(new DatasetEntry(){Name="2hh1",Url = "http://www.pdb.org/2hh1"});
            myEntries.Add(new DatasetEntry(){Name="3csa",Url = "http://www.pdb.org/3csa"});
            myEntries.Add(new DatasetEntry(){Name="4yg1",Url = "http://www.pdb.org/4yg1"});
            var mydto = new DatasetDTO
            {
                Name = "testdataset2",
                Entries = myEntries
            };
            var mydto2 = client.Post(mydto);

            entries = client.Get(new GetEntries());
            Assert.True(entries.Select(x => x.Name).Contains("2hh1"));
            Assert.True(entries.Select(x => x.Name).Contains("3csa"));
            Assert.True(entries.Select(x => x.Name).Contains("4yg1"));
            datasetentries = client.Get(new GetDatasetEntries());
            Assert.True(datasetentries.Count > dErelations);
            Assert.True(datasetentries.Count == dErelations + 3);

            client.Delete(new DeleteDatasetNoFk {Id = mydto2.Id});

            datasetentries = client.Get(new GetDatasetEntries());
            if (datasetentries.Count != dErelations)
            {
                Console.Error.WriteLine("Warning: connection foreign key not working - NO CASCADE DELETE.");
                Assert.Ignore();
            }

        }

        [Test]
        public void DatasetUpdateShouldAddOnlyNewEntriesTestCase()
        {
            //submit dataset, then update it, check whether only relevant entries were updated - not doubled
            var client = new JsonServiceClient(_baseUri);
            var myEntries = new List<DatasetEntry>();
            myEntries.Add(new DatasetEntry(){Name="2hh1",Url = "http://www.pdb.org/2hh1"});
            myEntries.Add(new DatasetEntry(){Name="3csa",Url = "http://www.pdb.org/3csa"});
            myEntries.Add(new DatasetEntry(){Name="4yg1",Url = "http://www.pdb.org/4yg1"});
            var mydto = new DatasetDTO
            {
                Name = "testdataset3",
                Entries = myEntries
            };
            var mydto2 = client.Post(mydto);
            Assert.True(mydto2.Entries.Count == 3);
            
            mydto2.Entries.Add(new DatasetEntry(){Name="2hhd",Url="http://www.pdb.org/2hhd"});
            
            client.Put(mydto2);
            var mydto4 = client.Get(new DatasetDTO {Id = mydto2.Id});

            Assert.True(mydto4.Entries.Count == 4);
            //Assert.True(mydto4.Urls.Count==4);
            Assert.True(mydto4.Entries.Select(x=> x.Name).Contains("2hhd"));
            //Assert.True(mydto4.Urls.Contains("http://www.pdb.org/2hhd"));
        }

        [Test]
        public void EncryptingAndDecriptingListOfItemsTestCase()
        {
            ProviderItem item, item2;
            string testmessage, testpassword;
            PrepareTestProviderItems(out item, out item2, out testmessage, out testpassword);

            SettingsStorageInDB.encrypt(ref item, testpassword);
            SettingsStorageInDB.encrypt(ref item2, testpassword);
            Assert.False(item2.securetoken.Equals(testmessage)); //item2 is encrypted not same as plaintext
            var items = new List<ProviderItem>();
            items.Add(item);
            items.Add(item2);
            Assert.False(items[0].securetoken.Equals(testmessage)); //item2 is encrypted not same as plaintext
            Assert.False(items[1].securetoken.Equals(testmessage)); //item2 is encrypted not same as plaintext
            SettingsStorageInDB.decrypt(ref items, testpassword);
            Assert.True(items[0].securetoken.Equals(testmessage));
            Assert.True(items[1].securetoken.Equals(testmessage));
        }

        [Test]
        public void EncryptingChangeContentOfSecureItemTestCase()
        {
            ProviderItem item;
            ProviderItem item2;
            string testmessage;
            string testpassword;
            PrepareTestProviderItems(out item, out item2, out testmessage, out testpassword);
            SettingsStorageInDB.encrypt(ref item, testpassword);
            Assert.False(item.securetoken.Equals(item2.securetoken)); //encrypted and nonencrypted are not same
            Assert.True(item.loggeduser.Equals(item2.loggeduser)); //nonencrypted items are same
            SettingsStorageInDB.decrypt(ref item, testpassword);
            Assert.True(item.securetoken.Equals(item2.securetoken)); //decrypted and nonencrypted are same
            Assert.True(item.loggeduser.Equals(item2.loggeduser)); //nonencrypted items are still same
        }

        [Test]
        public void PostingDeletingDatasetShouldIncreaseDecreaseEntriesTestCase()
        {
            //check no entries 2hh1,3csa,4yg1, get entries relation to dataset - put dataset, check whether entries present
            //check whether number of relation increased by 3, then delete
            var client = new JsonServiceClient(_baseUri);
            var entries = client.Get(new GetEntries());
            Assert.False(entries.Select(x => x.Name).Contains("2hh6"));
            Assert.False(entries.Select(x => x.Name).Contains("3cs6"));
            Assert.False(entries.Select(x => x.Name).Contains("4yg6"));

            var datasetentries = client.Get(new GetDatasetEntries());
            var dErelations = datasetentries.Count;
            var myEntries = new List<DatasetEntry>();
            myEntries.Add(new DatasetEntry(){Name="2hh6",Url = "http://www.pdb.org/2hh6"});
            myEntries.Add(new DatasetEntry(){Name="3cs6",Url = "http://www.pdb.org/3cs6"});
            myEntries.Add(new DatasetEntry(){Name="4yg6",Url = "http://www.pdb.org/4yg6"});

            var mydto = new DatasetDTO
            {
                Name = "testdataset2",
                Entries = myEntries
            };
            var mydto2 = client.Post(mydto);

            entries = client.Get(new GetEntries());
            Assert.True(entries.Select(x => x.Name).Contains("2hh6"));
            Assert.True(entries.Select(x => x.Name).Contains("3cs6"));
            Assert.True(entries.Select(x => x.Name).Contains("4yg6"));
            datasetentries = client.Get(new GetDatasetEntries());
            Assert.True(datasetentries.Count > dErelations);
            Assert.True(datasetentries.Count == dErelations + 3);

            client.Delete(new DeleteDataset {Id = mydto2.Id});

            datasetentries = client.Get(new GetDatasetEntries());
            Assert.True(datasetentries.Count == dErelations);
        }

        [Test]
        public void PostingDeletingDatasetTestCase()
        {
            var client = new JsonServiceClient(_baseUri);
            var all = client.Get(new GetDatasets());
            var firstcount = all.Count;
            Console.WriteLine(" DatasetTestCase() firstcount:" + firstcount);
            Assert.True(all.Count >= 0);
            //    Is.StringStarting("[")); // asserts that the json is array
            var myEntries = new List<DatasetEntry>();
            myEntries.Add(new DatasetEntry(){Name="2hhd",Url = "http://www.pdb.org/2hhd"});
            myEntries.Add(new DatasetEntry(){Name="3csb",Url = "http://www.pdb.org/3csb"});
            myEntries.Add(new DatasetEntry(){Name="4yg0",Url = "http://www.pdb.org/4yg0"});

            var mydto = new DatasetDTO
            {
                Name = "testdataset",
                Entries = myEntries
            };

            var mydto2 = client.Post(mydto);

            all = client.Get(new GetDatasets());
            Console.WriteLine(" DatasetTestCase() all.count:" + all.Count);
            Assert.True(all.Count == firstcount + 1);

            //var all2 =
            client.Get(new GetDatasets()); //gets all
            //var testdto = JsonSerializer.DeserializeFromString<DatasetsDTO>(all2.ToString());
            var all3 = client.Get(new DatasetDTO {Id = mydto2.Id});
            //var all3 = JsonSerializer.DeserializeFromString<DatasetDTO>(all3.ToString());
            Console.WriteLine(" DatasetTestCase() all3.name:" + all3.Name + " mydto.Name" + mydto.Name);
            Assert.True(all3.Name == mydto.Name);
            //Assert.True(all3.Entries.Count==mydto.Entries.Count);
            //Assert.True(all3.Entries[0]==mydto.Entries[0]);
            //Assert.True(all3.Urls.Count==mydto.Urls.Count);
            //Assert.True(all3.Urls[0]==mydto.Urls[0]);

            client.Delete(new DeleteDataset {Id = mydto2.Id});

            all = client.Get(new GetDatasets());
            Assert.True(all.Count == firstcount);
        }


        [Test]
        public void RawMetadataServiceTestCase()
        {
            var client = new JsonServiceClient(_baseUri);
            var response = client.Get<string>("");
            Assert.True(response.Length > 0);
        }

        [Test]
        public void HttpOptionsShouldReturn200OnFileProvidersDatasetTestCase()
        {
            var client = new JsonServiceClient(_baseUri);
            var response = client.Send<HttpWebResponse>("OPTIONS", "files", null);
            Assert.True(response.StatusCode == HttpStatusCode.OK);
            response = client.Send<HttpWebResponse>("OPTIONS", "files/filesystem", null);
            Assert.True(response.StatusCode == HttpStatusCode.OK);
            response = client.Send<HttpWebResponse>("OPTIONS", "dataset", null);
            Assert.True(response.StatusCode == HttpStatusCode.OK);
            
            try
            {
                response = client.Send<HttpWebResponse>("OPTIONS", "otherservice", null);
                Assert.Fail();
            }
            catch (WebServiceException)
            {
                //Assert.Pass();
            }
        }

        private ProviderItem createTestProviderItem()
        {
            var pi = new ProviderItem
            {
                alias = "b2drop_test",
                type = "B2Drop",
                securetoken =
                    "dmFncmFudFM2wAdx6KqWemSjD2Re38CZ9i0xMywQt8x71NwEoJZavTxbiOEga3te+q4cq4pK2gAKTtawu3k0REBz1pGiFiJR+Wnot8pS7d52o+w6x9tW",
                username = "tomas.kulhanek@stfc.ac.uk",
                loggeduser="vagrant"
            };
            return pi;
        }


        [Test]
        public void DecryptSecretsRegisterB2dropTestCase()
        {
            var pi = createTestProviderItem();
            try
            {                                                                
                SettingsStorageInDB.decrypt(ref pi);
            }
            catch (WarningException)
            {
                //ignore on machines, where pkey is different - cannot decrypt the securetoken 
                Assert.Ignore();                
            }
            //should decrypt secret token
            Assert.False(pi.securetoken.StartsWith("dmFncm"));
            
            var client = new JsonServiceClient(_baseUri);
            var providerlist = client.Get(new ProviderItem());            
            var providerlistwithnew = client.Put(pi);
            //should register - new providers is added
            Assert.True(providerlist.Count < providerlistwithnew.Count);
            Assert.True(providerlistwithnew.Last().alias=="b2drop_test");
            //test directory exists, it is mounted
            Assert.True(Directory.Exists("/home/vagrant/work/vagrant/b2drop_test"));
            //test directory is not empty - some dirs or files
            Assert.True(Directory.GetFiles("/home/vagrant/work/vagrant/b2drop_test").Length>0);

            var providerlistdeleted = client.Delete(pi);
            //should delete - no provider list is there
            Assert.True(providerlistdeleted.Count == providerlist.Count);
        }                       

        private ProviderItem createTestDropboxProviderItem()
        {
            var pi = new ProviderItem
            {
                alias = "dropbox_test",
                type = "Dropbox",
                securetoken =
                    "dmFncmFudEX+37yxPbbSDfjkeDjBmkbcWhZ47fNWLjQnpNnfAV38Gm/NeRZA7199SLzaJekuV//2tdyJRoR19Qc8EZAcvqbjwI6dtlXymSF5OlssD3e9XdYdXEM5b//D2dg6nuYOrMt/24iQ6KIasRSIn3wGNn12sawI73nc00KbwlX5NFJ5/urb/qgczBhO5h7v+0VFLQ==",                
                loggeduser="vagrant"
            };
            return pi;
        }

        [Test]
        public void DecryptSecretsRegisterDropboxTestCase()
        {
            var pi = createTestDropboxProviderItem();
            try
            {                                                                
                SettingsStorageInDB.decrypt(ref pi);
            }
            catch (WarningException)
            {
                //ignore on machines, where pkey is different - cannot decrypt the securetoken 
                Assert.Ignore();                
            }
            //should decrypt secret token
            Assert.False(pi.securetoken.StartsWith("dmFncm"));
            
            var client = new JsonServiceClient(_baseUri);
            var providerlist = client.Get(new ProviderItem());            
            var providerlistwithnew = client.Put(pi);
            //should register - new providers is added
            Assert.True(providerlist.Count < providerlistwithnew.Count);
            Assert.True(providerlistwithnew.Last().alias=="dropbox_test");

            var providerlistdeleted = client.Delete(pi);
            //should delete - no provider list is there
            Assert.True(providerlistdeleted.Count == providerlist.Count);
        }
        
        private ProviderItem createTestFilesystemProviderItem()
        {
            var pi = new ProviderItem
            {
                alias = "filesystem_test",
                type = "FileSystem",
                securetoken =
                    "/home/vagrant",                
                loggeduser="vagrant"
            };
            return pi;
        }
        
        [Test]
        public void RegisterFilesystemTestCase()
        {            
            var pi = createTestFilesystemProviderItem();            
            var client = new JsonServiceClient(_baseUri);
            try {
                var providerlist = client.Get(new ProviderItem());
                
                var providerlistwithnew = client.Put(pi);
                //should register - new providers is added
                Assert.True(providerlist.Count < providerlistwithnew.Count);
                Assert.True(providerlistwithnew.Last().alias=="filesystem_test");
                //test directory exists, it is mounted
                Assert.True(Directory.Exists("/home/vagrant/work/vagrant/filesystem_test"));
                //test directory is not empty - some dirs or files
                Assert.True(Directory.GetFiles("/home/vagrant/work/vagrant/filesystem_test").Length>0);

                var providerlistdeleted = client.Delete(pi);
                //should delete - no provider list is there
                Assert.True(providerlistdeleted.Count == providerlist.Count);
            }
            catch (WebServiceException e)
            {
        	if (e.Message == "UnauthorizedAccessException") Assert.Ignore();
                Console.WriteLine(e.Message);
                throw e;
            }
            catch (Exception e) {
                throw e;
            }
        }

        [Test]
        public void GetCerifProjectsReturnsNonZeroListTestCase()
        {
            var client = new JsonServiceClient(_baseUri);
            var gp = new GetProjects();
            var projects = client.Get(gp);
            Assert.True(projects.Count>0);            
        }

        [Test]
        public void GetCerifProjectsReturnsRequestedProjectTestCase()
        {
            var client = new JsonServiceClient(_baseUri);
            var gp = new GetProjects() {Name = "West-Life"};
            var projects = client.Get(gp);
            Assert.True(projects.Count>0);
            Assert.True(projects[0].cfTitle.StartsWith("West-Life")); 
            projects = client.Get(new GetProjects(){Name="Nonsense"});
            Assert.True(projects.Count==0);                        
        }        

        [Test]
        public void CreateAndDeleteUserJobServiceTestCase()
        {
            var client = new JsonServiceClient(_baseUri);
            var all = client.Get(new GetUserJobs());
            Assert.That(all.Count>=0);
            var jp = client.Post(new PostUserJob() {Name = "jupyter"});
            Assert.True(jp.jobType=="jupyter");
            Assert.True(jp.Id>=0);
            Assert.True(jp.Username.Equals("vagrant"));
            Thread.Sleep(10000);
            jp = client.Delete(
                new PostUserJob()
                {
                    Name = "jupyter"
                });
            //Assert.True(jp.);            
        }
        [Test]
        public void CreateTwiceCheckStartedOnceAndDeleteUserJobServiceTestCase()
        {
            var client = new JsonServiceClient(_baseUri);
            var all = client.Get(new GetUserJobs());
            //something is returned
            Assert.True(all.Count>=0);
            var runingjobs = all.Count;
            //create job
            var jp = client.Post(new PostUserJob() {Name = "jupyter"});
            Assert.True(jp.jobType=="jupyter");
            Assert.True(jp.Id>=0);
            Assert.True(jp.Username.Equals("vagrant"));
            all = client.Get(new GetUserJobs());
            //number of jobs increases by 1
            Assert.True(all.Count==(runingjobs+1));            
            Thread.Sleep(10000);
            //create 2nd job
            jp = client.Post(new PostUserJob() {Name = "jupyter"});
            Assert.True(jp.jobType=="jupyter");
            Assert.True(jp.Id>=0);
            Assert.True(jp.Username.Equals("vagrant"));
            all = client.Get(new GetUserJobs());
            //number of jobs don't increases, it is still same
            Assert.True(all.Count==(runingjobs+1));            
            
            jp = client.Delete(
                new PostUserJob()
                {
                    Name = "jupyter"
                });
            all = client.Get(new GetUserJobs());
            //number of jobs decreases by 1, to previous number of running jobs
            Assert.True(all.Count==runingjobs);            
        }
    }
}
