using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityCloudBuildApi.IO.Swagger.Client;
using UnityCloudBuildApi.IO.Swagger.Model;

namespace XamarinWPF.Models
{
	public static class UCBApi
	{
		const string ApiKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"; // You may find from https://build.cloud.unity3d.com/login/me/
		const string OrgId = "your oganaization id";
		const string ProjectId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"; //your projet id

		static Configuration config = null;

		static UCBApi()
		{
			var client = new ApiClient("https://build-api.cloud.unity3d.com/api/v1");
			config = new Configuration(client, accessToken: ApiKey);
		}

		static async Task<T> ConsideringRateLimitWith<T>(System.Func<Task<T>> act)
		{
			T result;
			while (true)
			{
				try
				{
					result = await act();
				}
				catch (ApiException ex)
				{
					if (ex.ErrorCode == 429)
					{
						Debug.WriteLine("Rate Limiting. " + (string)ex.ErrorContent);
						await Task.Delay(3000);
						continue;
					}
					throw ex;
				}
				break;
			}
			return result;
		}

		public static async Task RunAdminOperation()
		{
			var optionList = BuildDefinition.MakeBuildTargetAPIOptions();
			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < optionList.Count; i++)
			{
				sb.AppendLine(string.Format("-------- option[{0}] -------", i));
				sb.AppendLine(optionList[i].ToString());
			}
			Debug.WriteLine(sb.ToString());

#if false
			optionList.RemoveRange(1, optionList.Count - 1);//test
#endif
			//await RemoveBuildTarget();

			await CreateBuildTarget(optionList);

			//await DumpCredentials("../../DumpCredential.txt");
		}

		static async Task RemoveBuildTarget()
		{
			var buildTargetsApi = new UnityCloudBuildApi.IO.Swagger.Api.BuildtargetsApi(config);

			// Get BuildTargets for list BuildTargetId
			var buildTargets = await ConsideringRateLimitWith(async () => await buildTargetsApi.GetBuildTargetsAsync(OrgId, ProjectId));

			for (int i = 0; i < buildTargets.Count;)
			{
				var buildTarget = buildTargets[i];
				if (buildTarget.Name.IndexOf(BuildDefinition.NamePrefix) >= 0)
				{
					Debug.WriteLine("Delete build target: " + buildTarget.Name);
					await ConsideringRateLimitWith(
						async () => await buildTargetsApi.DeleteBuildTargetAsync(OrgId, ProjectId, buildTarget.Buildtargetid)
						);
				}
				i++;
			}
		}

		static async Task CreateBuildTarget(List<Options2> optionList)
		{
			var buildTargetsApi = new UnityCloudBuildApi.IO.Swagger.Api.BuildtargetsApi(config);

			// Get BuildTargets for list BuildTargetId
			var buildTargets = await ConsideringRateLimitWith(async () => await buildTargetsApi.GetBuildTargetsAsync(OrgId, ProjectId));
			var existsTargetIdByName = buildTargets.ToDictionary(x => x.Name);

			for (int i = 0; i < optionList.Count;)
			{
				var opt2 = optionList[i];
				if (existsTargetIdByName.ContainsKey(opt2.Name))
				{
					//Update build target
					var target = existsTargetIdByName[opt2.Name];
					Debug.WriteLine("Update build target: " + opt2.Name + ", " + target.Buildtargetid);
					var opt3 = new Options3();
					opt3.Name = opt2.Name;
					opt3.Platform = opt2.Platform;
					opt3.Enabled = opt2.Enabled;
					opt3.Settings = opt2.Settings;
					opt3.Credentials = opt2.Credentials;
					await ConsideringRateLimitWith(async () => await buildTargetsApi.UpdateBuildTargetAsync(OrgId, ProjectId, target.Buildtargetid, opt3));
				}
				else
				{
					// Add Build Target
					Debug.WriteLine("Add build target: " + opt2.Name);
					await ConsideringRateLimitWith(async () => await buildTargetsApi.AddBuildTargetAsync(OrgId, ProjectId, opt2));
				}
				i++;
			}
		}

		static async Task DumpCredentials(string path)
		{
			var credentialsApi = new UnityCloudBuildApi.IO.Swagger.Api.CredentialsApi(config);

			var sb = new System.Text.StringBuilder();
			{
				var list = await credentialsApi.GetAllIosAsync(OrgId, ProjectId);
				sb.AppendLine("---- iOS ----");
				foreach (var response in list)
				{
					sb.AppendFormat("{0},{1},{2}\n", response.Label, response.Credentialid, response.LastMod);
				}
			}
			{
				var list = await credentialsApi.GetAllAndroidAsync(OrgId, ProjectId);
				sb.AppendLine("---- Android ----");
				foreach (var response in list)
				{
					sb.AppendFormat("{0},{1},{2}\n", response.Label, response.Credentialid, response.LastMod);
				}
			}

			System.IO.File.WriteAllText(path, sb.ToString());
		}

		public class StartaBuildParam
		{
			public string Platform = "ios";//ios or android
			public bool ReleaseBuild = false;
			public bool CleanBuild = false;
			public bool RealStore = false;
			public int ReleaseVer = 0;

			public override string ToString()
			{
				return string.Format("{0}, Rel:{1}, Cln:{2}, Str:{3}, Ver:{4}", Platform, ReleaseBuild, CleanBuild, RealStore, ReleaseVer);
			}
		}

		public static async Task<string[]> StartBuild(StartaBuildParam param)
		{
			Debug.WriteLine("StartBuild with: " + param);

			var buildTargetsApi = new UnityCloudBuildApi.IO.Swagger.Api.BuildtargetsApi(config);

			Debug.WriteLine("StartBuild 1");
			// Get BuildTargets for list BuildTargetId
			var buildTargets = await buildTargetsApi.GetBuildTargetsAsync(OrgId, ProjectId, "settings");
			Debug.WriteLine("StartBuild 2");

			var targets = from x in buildTargets
						  where x.Platform == param.Platform
						  where x.Settings != null && x.Settings.Advanced != null && x.Settings.Advanced.Unity != null
						  let symbolsStr = x.Settings.Advanced.Unity.ScriptingDefineSymbols
						  let symbolList = symbolsStr.Split(';')
						  let release = symbolList.Contains("RELEASE")
						  let realStore = symbolList.Contains("REAL_STORE")
						  where release == param.ReleaseBuild
						  where realStore == param.RealStore
						  let relVer0 = symbolList.Contains("RELEASEVER_0")
						  let relVer1 = symbolList.Contains("RELEASEVER_1")
						  let relVer2 = symbolList.Contains("RELEASEVER_2")
						  let relVer3 = symbolList.Contains("RELEASEVER_3")
						  where
							(param.ReleaseVer == 3 && (relVer0 && relVer1 && relVer2 && relVer3)) ||
							(param.ReleaseVer == 2 && (relVer0 && relVer1 && relVer2) && (!relVer3)) ||
							(param.ReleaseVer == 1 && (relVer0 && relVer1) && (!relVer2 && !relVer3)) ||
							(param.ReleaseVer == 0 && (relVer0) && (!relVer1 && !relVer2 && !relVer3))
						  select x;
			Debug.WriteLine("StartBuild 3: " + targets.Count());

			var buildApi = new UnityCloudBuildApi.IO.Swagger.Api.BuildsApi(config);
			foreach (var target in targets)
			{
				var opt4 = new Options4
				{
					Clean = param.CleanBuild,
					Delay = 0,
				};
				Debug.WriteLine("Build: " + target.Buildtargetid);
				await buildApi.StartBuildsAsync(OrgId, ProjectId, target.Buildtargetid, opt4);
			}

			Debug.WriteLine("StartBuild 4: ");

			return targets.Select(x => x.Name).ToArray();
		}
	}
}
