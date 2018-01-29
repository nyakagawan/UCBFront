using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityCloudBuildApi.IO.Swagger.Client;
using UnityCloudBuildApi.IO.Swagger.Model;

public class BuildDefinition
{
	class DefineSymbolSet
	{
		public string[] symbols;

		public DefineSymbolSet(params string[] symbols)
		{
			this.symbols = symbols;
		}
	}

	public const string NamePrefix = "gen_";

	static string[] Platforms = new string[] { "ios", "android" };
	static string[] Branches = new string[] { "develop" };
	static string[] UnityVersions = new string[] { "2017_2_0p2" };

	static LinkedList<DefineSymbolSet> DefineSymbolsList = new LinkedList<DefineSymbolSet>(
		new DefineSymbolSet[]
		{
			new DefineSymbolSet("RELEASE", ""),
			new DefineSymbolSet("REAL_STORE", ""),
			new DefineSymbolSet("RELEASEVER_0", "RELEASEVER_0;RELEASEVER_1", "RELEASEVER_0;RELEASEVER_1;RELEASEVER_2", "RELEASEVER_0;RELEASEVER_1;RELEASEVER_2;RELEASEVER_3"),
		});

	const string BundleId = "your.bundle.id";
	const string XcodeVersion = "latest";
	static readonly Dictionary<string, string> CredentialByPlatform = new Dictionary<string, string>()
	{
		{ "ios", "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" },//Your conditional id here
		{ "android", "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" },
	};

	public static List<Options2> MakeBuildTargetAPIOptions()
	{
		var optionList = new List<Options2>();
		foreach (var platform in Platforms)
		{
			foreach (var branch in Branches)
			{
				foreach (var unityVer in UnityVersions)
				{
					var symbolList = new List<string>();
					Recurse(DefineSymbolsList.First, symbolList, (symbols_) =>
					{
						var symbolsStr = string.Join(";", symbols_);
						var develop = symbols_.Contains("RELEASE") == false;

						var buildOptions = new List<string>();
						if (develop)
						{
							buildOptions = new List<string>
							{
								"BuildOptions.Development",
								"BuildOptions.AllowDebugging",
							};
						}

						var credentialId = CredentialByPlatform[platform];

						var option2 = new Options2
						{
							Platform = platform,
							Name = string.Format(NamePrefix + "{0}_{1}_{2}", develop ? "debug" : "release", platform, optionList.Count),
							Enabled = true,
							Settings = new OrgsorgidprojectsprojectidbuildtargetsSettings
							{
								AutoBuild = false,

								UnityVersion = unityVer,

								Platform = new OrgsorgidprojectsprojectidbuildtargetsSettingsPlatform
								{
									BundleId = BundleId,
									XcodeVersion = XcodeVersion,
								},

								Scm = new OrgsorgidprojectsprojectidbuildtargetsSettingsScm
								{
									Branch = branch,
									Type = "git"
								},

								Advanced = new OrgsorgidprojectsprojectidbuildtargetsSettingsAdvanced
								{
									Unity = new OrgsorgidprojectsprojectidbuildtargetsSettingsAdvancedUnity
									{
										ScriptingDefineSymbols = symbolsStr,

										PlayerExporter = new OrgsorgidprojectsprojectidbuildtargetsSettingsAdvancedUnityPlayerExporter()
										{
											BuildOptions = buildOptions,
										},
									}
								}
							},

							Credentials = new OrgsorgidprojectsprojectidbuildtargetsCredentials1
							{
								Signing = new OrgsorgidprojectsprojectidbuildtargetsCredentials1Signing
								{
									Credentialid = credentialId,
								}
							}
						};

						optionList.Add(option2);
					});
				}
			}
		}
		return optionList;
	}

	static void Recurse(LinkedListNode<DefineSymbolSet> node, List<string> symbolList, System.Action<List<string>> onMakeOpt)
	{
		if (node == null) return;

		foreach (var symbol in node.Value.symbols)
		{
			symbolList.Add(symbol);

			if (node.Next != null)
			{
				Recurse(node.Next, symbolList, onMakeOpt);
			}
			else
			{
				onMakeOpt(symbolList);
			}

			symbolList.RemoveAt(symbolList.Count - 1);
		}
	}

}
