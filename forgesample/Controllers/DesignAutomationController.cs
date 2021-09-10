using Autodesk.Forge;
using Autodesk.Forge.DesignAutomation;
using Autodesk.Forge.DesignAutomation.Model;
using Autodesk.Forge.Model;
using forgesample;
using forgesample.Builders;
using forgesample.Hubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Activity = Autodesk.Forge.DesignAutomation.Model.Activity;
using Alias = Autodesk.Forge.DesignAutomation.Model.Alias;
using AppBundle = Autodesk.Forge.DesignAutomation.Model.AppBundle;
using Parameter = Autodesk.Forge.DesignAutomation.Model.Parameter;
using WorkItem = Autodesk.Forge.DesignAutomation.Model.WorkItem;
using WorkItemStatus = Autodesk.Forge.DesignAutomation.Model.WorkItemStatus;

namespace forgesample.Controllers
{
    [ApiController]
    public class DesignAutomationController : ControllerBase
    {
        //Naming
        private string _zipFileName = "DA4ShelfBuilderPlugin.bundle.zip";
        private string _bundleName = "WallShelfConfig";
        private string _activityName = "WallShelfConfig";
        private string _aliasName = "dev";
        private string _appBundleDescription = "Creates something ...";
        // Used to access the application folder (temp location for files & bundles)
        private IWebHostEnvironment _env;
        // used to access the SignalR Hub
        private IHubContext<DesignAutomationHub> _hubContext;
        // Local folder for bundles
        public string LocalBundlesFolder { get { return Path.Combine(_env.WebRootPath, "bundles"); } }
        public string LocalDataSetFolder { get { return Path.Combine(_env.WebRootPath, "inputFiles"); } }
        // Engine
        private string _engine;
        public string EngineName { get => _engine; set => _engine = value; }
        // Bundle file
        public string ZipFileName { get { return _zipFileName; } }
        // Bundle name
        public string AppBundleName { get { return _bundleName; } }
        // Activity name
        public string ActivityName { get { return _activityName; } }
        /// Prefix for AppBundles and Activities
        public string NickName { get { return OAuthController.GetAppSetting("FORGE_CLIENT_ID"); } }
        /// Alias for the app (e.g. DEV, STG, PROD). This value may come from an environment variable
        public string Alias { get { return _aliasName; } }
        // bucket name
        public string bucketKey => (NickName + "-" + AppBundleName).ToLower();
        // bucket region US or EMEA
        private BucketRegions _bucketRegion = BucketRegions.US;
        private BucketRegions BucketRegion { get => _bucketRegion; set => _bucketRegion = value; }

        // Design Automation v3 API
        DesignAutomationClient _designAutomation;

        // Constructor, where env and hubContext are specified
        public DesignAutomationController(IWebHostEnvironment env, IHubContext<DesignAutomationHub> hubContext, DesignAutomationClient api)
        {
            // DesignAutomation must be created as new instance.
            DesignAutomationClientBuilder da = new DesignAutomationClientBuilder(
                OAuthController.GetAppSetting("FORGE_CLIENT_ID"),
                OAuthController.GetAppSetting("FORGE_CLIENT_SECRET")
                );
            _designAutomation = da.Client;
            _env = env;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Get all Activities defined for this account
        /// </summary>
        [HttpGet]
        [Route("api/forge/designautomation/activities")]
        public async Task<List<string>> GetDefinedActivities()
        {
            // filter list of 
            Page<string> activities = await _designAutomation.GetActivitiesAsync();
            List<string> definedActivities = new List<string>();
            foreach (string activity in activities.Data)
                if (activity.StartsWith(NickName) && activity.IndexOf("$LATEST") == -1)
                    definedActivities.Add(activity.Replace(NickName + ".", String.Empty));

            return definedActivities;
        }

        /// <summary>
        /// Define a new activity
        /// </summary>
        [HttpPost]
        [Route("api/forge/designautomation/activities")]
        public async Task<IActionResult> CreateActivity([FromBody] JObject activitySpecs)
        {
            string engineName = activitySpecs["engine"].Value<string>();

            Page<string> activities = await _designAutomation.GetActivitiesAsync();
            string qualifiedActivityId = string.Format("{0}.{1}+{2}", NickName, ActivityName, Alias);
            if (!activities.Data.Contains(qualifiedActivityId))
            {
                // define the activity
                dynamic engineAttributes = EngineAttributes(engineName);
                string commandLine = string.Format(engineAttributes.commandLine, AppBundleName);
                Activity activitySpec = new Activity()
                {
                    Id = ActivityName,
                    Appbundles = new List<string>() { string.Format("{0}.{1}+{2}", NickName, AppBundleName, Alias) },
                    CommandLine = new List<string>() { commandLine },
                    Engine = engineName,
                    Parameters = new Dictionary<string, Parameter>()
                    {
                        { "inputFile", new Parameter()
                        {
                            Description = "input file",
                            LocalName = "ime fajla iz Data Set-a",
                            Verb = Verb.Get,
                            Zip = false
                        }
                        },
                        { "inputJson", new Parameter()
                        {
                            Description = "input json",
                            LocalName = "params.json",
                            Verb = Verb.Get,
                            Zip = false }
                        },
                        { "outputFile", new Parameter()
                        {
                            Description = "output file",
                            LocalName = "outputFile." + engineAttributes.extension,
                            Verb = Verb.Put,
                            Zip = false
                        }
                        }
                    }/*,
                    Settings = new Dictionary<string, ISetting>()
                    {
                        { "script", new StringSetting(){ Value = engineAttributes.script } }
                    }*/
                };
                Activity newActivity = await _designAutomation.CreateActivityAsync(activitySpec);

                // specify the alias for this Activity
                Alias aliasSpec = new Alias() { Id = Alias, Version = 1 };
                Alias newAlias = await _designAutomation.CreateActivityAliasAsync(ActivityName, aliasSpec);

                return Ok(new { Activity = qualifiedActivityId });
            }
            return Ok(new { Activity = "Activity already defined" });
        }

        /// <summary>
        /// Helps identify the engine
        /// </summary>
        private dynamic EngineAttributes(string engine)
        {
            if (engine.Contains("AutoCAD"))
            {
                return new
                {
                    commandLine = "$(engine.path)\\accoreconsole.exe /i \"$(args[inputFile].path)\" /al \"$(appbundles[{0}].path)\" /s $(settings[script].path)",
                    extension = "dwg",
                    script = "UpdateParam\n"
                };
            }
            if (engine.Contains("Inventor"))
            {
                return new
                {
                    commandLine = "$(engine.path)\\inventorcoreconsole.exe /i \"$(args[inputFile].path)\" /al \"$(appbundles[{0}].path)\"",
                    extension = "ipt",
                    script = string.Empty
                };
            }
            throw new Exception("Invalid engine");
        }

        /// <summary>
        /// Names of app bundles on this project
        /// </summary>
        [HttpGet]
        [Route("api/appbundles")]
        public string[] GetLocalBundles()
        {
            return Directory.GetFiles(LocalBundlesFolder, "*.zip").Select(Path.GetFileNameWithoutExtension).ToArray();
        }

        /// <summary>
        /// Define a new appbundle
        /// </summary>
        [HttpPost]
        [Route("api/forge/designautomation/appbundles")]
        public async Task<IActionResult> CreateAppBundle([FromBody] JObject appBundleSpecs)
        {
            //each call make new instance so every time i is nessessary to read Engine name
            string engineName = appBundleSpecs["engine"].Value<string>();

            // check if ZIP with bundle is here
            string packageZipPath = Path.Combine(LocalBundlesFolder, ZipFileName);
            if (!System.IO.File.Exists(packageZipPath)) throw new Exception("Appbundle not found at " + packageZipPath);

            // get defined app bundles
            Page<string> appBundles = await _designAutomation.GetAppBundlesAsync();

            // check if app bundle is already define
            dynamic newAppVersion;
            string qualifiedAppBundleId = string.Format("{0}.{1}+{2}", NickName, AppBundleName, Alias);
            if (!appBundles.Data.Contains(qualifiedAppBundleId))
            {
                // create an appbundle (version 1)
                AppBundle appBundleSpec = new AppBundle()
                {
                    Package = packageZipPath,
                    Engine = engineName,
                    Id = AppBundleName,
                    Description = _appBundleDescription
                };
                newAppVersion = await _designAutomation.CreateAppBundleAsync(appBundleSpec);
                if (newAppVersion == null) throw new Exception("Cannot create new app");

                // create alias pointing to v1
                Alias aliasSpec = new Alias() { Id = Alias, Version = 1 };
                Alias newAlias = await _designAutomation.CreateAppBundleAliasAsync(AppBundleName, aliasSpec);

                //upload the zip with .bundle
                RestClient uploadClient = new RestClient(newAppVersion.UploadParameters.EndpointURL);
                RestRequest request = new RestRequest(string.Empty, Method.POST);
                request.AlwaysMultipartFormData = true;
                foreach (KeyValuePair<string, string> x in newAppVersion.UploadParameters.FormData) request.AddParameter(x.Key, x.Value);
                request.AddFile("file", packageZipPath);
                request.AddHeader("Cache-Control", "no-cache");
                await uploadClient.ExecuteAsync(request);
            }/*
            else
            {
                // create new version
                AppBundle appBundleSpec = new AppBundle()
                {
                    Engine = engineName,
                    Description = appBundleName
                };
                newAppVersion = await _designAutomation.CreateAppBundleVersionAsync(appBundleName, appBundleSpec);
                if (newAppVersion == null) throw new Exception("Cannot create new version");

                // update alias pointing to v+1
                AliasPatch aliasSpec = new AliasPatch()
                {
                    Version = newAppVersion.Version
                };
                Alias newAlias = await _designAutomation.ModifyAppBundleAliasAsync(appBundleName, Alias, aliasSpec);
            }

            // upload the zip with .bundle
            RestClient uploadClient = new RestClient(newAppVersion.UploadParameters.EndpointURL);
            RestRequest request = new RestRequest(string.Empty, Method.POST);
            request.AlwaysMultipartFormData = true;
            foreach (KeyValuePair<string, string> x in newAppVersion.UploadParameters.FormData) request.AddParameter(x.Key, x.Value);
            request.AddFile("file", packageZipPath);
            request.AddHeader("Cache-Control", "no-cache");
            await uploadClient.ExecuteTaskAsync(request);

            return Ok(new { AppBundle = qualifiedAppBundleId, Version = newAppVersion.Version });*/
            return Ok(new { AppBundle = qualifiedAppBundleId, Version = "1" });
        }

        /// <summary>
        /// Input for StartWorkitem
        /// </summary>
        public class StartWorkitemInput
        {
            public IFormFile inputFile { get; set; }
            public string shelfData { get; set; }
            public string forgeData { get; set; }
        }

        /// <summary>
        /// Start a new workitem
        /// </summary>
        [HttpPost]
        [Route("api/forge/designautomation/workitems")]
        public async Task<IActionResult> StartWorkitem([FromForm] StartWorkitemInput input)
        {
            string zipedFileName = "file name that will be send. That could be ipt or zip";
            try
            {
                DataSetBuilder dataSetBuilder = new DataSetBuilder(LocalDataSetFolder, "DataSet");
                dataSetBuilder.SaveJsonData(input.shelfData, "params.json");
                dataSetBuilder.ZipFolder(zipedFileName);
            }
            catch (Exception ex)
            {
                return Ok(new { WorkItemId = ex.Message }); ;
            }
            // basic input validation
            JObject connItemData = JObject.Parse(input.forgeData);
            string uniqueActivityName = string.Format("{0}.{1}", NickName, ActivityName);
            string browserConnectionId = connItemData["browerConnectionId"].Value<string>();

            // save the file on the server
            var fileSavePath = Path.Combine(LocalDataSetFolder, zipedFileName);

            // OAuth token
            dynamic oauth = await OAuthController.GetInternalAsync();

            // upload file to OSS Bucket
            // 1. ensure bucket existis
            BucketsApi buckets = new BucketsApi();
            buckets.Configuration.AccessToken = oauth.access_token;
            try
            {
                PostBucketsPayload bucketPayload = new PostBucketsPayload(bucketKey, null, PostBucketsPayload.PolicyKeyEnum.Transient);
                await buckets.CreateBucketAsync(bucketPayload, BucketRegion.ToString());
            }
            catch { }; // in case bucket already exists
                       // 2. upload inputFile
            string inputFileNameOSS = string.Format("{0}_input_{1}", DateTime.Now.ToString("yyyyMMddhhmmss"), "File name for upload"); // avoid overriding
            ObjectsApi objects = new ObjectsApi();
            objects.Configuration.AccessToken = oauth.access_token;
            using (StreamReader streamReader = new StreamReader(fileSavePath))
                await objects.UploadObjectAsync(bucketKey, inputFileNameOSS, (int)streamReader.BaseStream.Length, streamReader.BaseStream, "application/octet-stream");

            // prepare workitem arguments
            // 1. input file
            XrefTreeArgument inputFileArgument = new XrefTreeArgument()
            {
                Verb = Verb.Get,
                LocalName = "Name using on server",
                Url = string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketKey, inputFileNameOSS),
                Headers = new Dictionary<string, string>()
                 {
                     { "Authorization", "Bearer " + oauth.access_token }
                 }
            };
            // 2. input json
            dynamic inputJson = new JObject();
            // TODO - create inputJson based on result from plugin (edges data)
            XrefTreeArgument inputJsonArgument = new XrefTreeArgument()
            {
                LocalName = "params.json",
                Verb = Verb.Get,
                Url = "data:application/json, " + ((JObject)inputJson).ToString(Formatting.None).Replace("\"", "'")
            };
            // 3. output file
            string outputFileNameOSS = string.Format("{0}_output_{1}", DateTime.Now.ToString("yyyyMMddhhmmss"), "ResultingPart.ipt"); // avoid overriding
            XrefTreeArgument outputFileArgument = new XrefTreeArgument()
            {
                Url = string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketKey, outputFileNameOSS),
                Verb = Verb.Put,
                Headers = new Dictionary<string, string>()
                   {
                       {"Authorization", "Bearer " + oauth.access_token }
                   }
            };

            // prepare & submit workitem
            // the callback contains the connectionId (used to identify the client) and the outputFileName of this workitem

            XrefTreeArgument completedArgument = new XrefTreeArgument()
            {
                Verb = Verb.Post,
                Url = string.Format(
                "{0}/api/forge/callback/designautomation?id={1}&outputFileName={2}",
                OAuthController.GetAppSetting("FORGE_WEBHOOK_URL"),
                //"https://webwallshelfbuilder.herokuapp.com",
                browserConnectionId,
                outputFileNameOSS)
            };

            XrefTreeArgument progressArgument = new XrefTreeArgument()
            {
                Verb = Verb.Post,
                Url = string.Format(
                    "{0}/api/forge/callback/designautomation/progress?id={1}",
                    OAuthController.GetAppSetting("FORGE_WEBHOOK_URL"),
                    //"https://webwallshelfbuilder.herokuapp.com",
                    browserConnectionId)
            };

            WorkItem workItemSpec = new WorkItem()
            {
                ActivityId = uniqueActivityName,
                Arguments = new Dictionary<string, IArgument>()
                {
                    { "inputFile", inputFileArgument },
                    { "outputFile", outputFileArgument },
                    { "onComplete", completedArgument },
                    { "onProgress", progressArgument }
                }
            };

            try
            {
                WorkItemStatus workItemStatus = await _designAutomation.CreateWorkItemAsync(workItemSpec);
                return Ok(new
                {
                    WorkItemId = workItemStatus.Id
                });
            }
            catch (Exception e)
            {
                return Ok(new { WorkItemId = e.Message });
            }
        }

        /// <summary>
        /// Callback from Design Automation Workitem (onProgress or onComplete)
        /// </summary>
        [HttpPost]
        [Route("/api/forge/callback/designautomation")]
        public async Task<IActionResult> OnCallback(string id, string outputFileName, [FromBody] dynamic body)
        {
            try
            {
                // your webhook should return immediately! we can use Hangfire to schedule a job
                JObject bodyJson = JObject.Parse((string)body.ToString());
                await _hubContext.Clients.Client(id).SendAsync("onComplete", bodyJson.ToString());

                var client = new RestClient(bodyJson["reportUrl"].Value<string>());
                var request = new RestRequest(string.Empty);

                byte[] bs = client.DownloadData(request);
                string report = System.Text.Encoding.Default.GetString(bs);
                await _hubContext.Clients.Client(id).SendAsync("onComplete", report);

                ObjectsApi objectsApi = new ObjectsApi();
                dynamic signedUrl = await objectsApi.CreateSignedResourceAsyncWithHttpInfo(bucketKey, outputFileName, new PostBucketsSigned(10), "read");
                await _hubContext.Clients.Client(id).SendAsync("downloadResult", (string)(signedUrl.Data.signedUrl));
            }
            catch (Exception e) { }

            // ALWAYS return ok (200)
            return Ok();
        }
        /// <summary>
        /// Callback from Design Automation Workitem onProgress
        /// </summary>
        [HttpPost]
        [Route("/api/forge/callback/designautomation/progress")]

        public async Task<IActionResult> OnCallback(string id, [FromBody] dynamic body)
        {
            try
            {
                // send the progress report
                JObject bodyJson = JObject.Parse((string)body.ToString());
                await _hubContext.Clients.Client(id).SendAsync("onProgress", bodyJson.ToString());
            }
            catch { }

            return Ok();
        }

        /// <summary>
        /// Return a list of available engines
        /// </summary>
        [HttpGet]
        [Route("api/forge/designautomation/engines")]
        public async Task<List<string>> GetAvailableEngines()
        {
            Page<string> engines = await _designAutomation.GetEnginesAsync();
            engines.Data.Sort();
            return engines.Data;
        }

        /// <summary>
        /// Clear the accounts (for debugging purposes)
        /// </summary>
        [HttpDelete]
        [Route("api/forge/designautomation/account")]
        public async Task<IActionResult> ClearAccount()
        {
            // clear account
            await _designAutomation.DeleteForgeAppAsync("me");
            return Ok();
        }
    }
}