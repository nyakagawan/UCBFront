using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace XamarinWPF.ViewModels
{
	public class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (Object.Equals(storage, value))
				return false;

			storage = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class ReleaseVerViewModel : ViewModelBase
	{
		string _name;
		public string Name
		{
			get { return _name; }
			set { SetProperty(ref _name, value); }
		}

		public ReleaseVerViewModel(string name)
		{
			_name = name;
		}
	}

	class MainPageViewModel : ViewModelBase
	{
		public ICommand BuildIOSCommand { private set; get; }
		public ICommand BuildAndroidCommand { private set; get; }
		public ICommand AdminCommand { private set; get; }

		public ObservableCollection<ReleaseVerViewModel> ReleaseVerList { get; } = new ObservableCollection<ReleaseVerViewModel>();

		ReleaseVerViewModel _SelectedReleaseVersion;
		public ReleaseVerViewModel SelectedReleaseVersion
		{
			get { return _SelectedReleaseVersion; }
			set { SetProperty(ref _SelectedReleaseVersion, value); }
		}

		bool _IsReleaseBuild;
		public bool IsReleaseBuild
		{
			get { return _IsReleaseBuild; }
			set { SetProperty(ref _IsReleaseBuild, value); }
		}

		bool _IsRealStore;
		public bool IsRealStore
		{
			get { return _IsRealStore; }
			set { SetProperty(ref _IsRealStore, value); }
		}

		bool _IsCleanBuild;
		public bool IsCleanBuild
		{
			get { return _IsCleanBuild; }
			set { SetProperty(ref _IsCleanBuild, value); }
		}

		string _StatusLine;
		public string StatusLine
		{
			get { return _StatusLine; }
			set { SetProperty(ref _StatusLine, value); }
		}

		bool _NowBuilding = false;

		static readonly List<string> ReleaseVerNames = new List<string>
		{
			"RELEASEVER_0",
			"RELEASEVER_1",
			"RELEASEVER_2",
			"RELEASEVER_3",
		};

		public MainPageViewModel()
		{
			StatusLine = "Initializing";

			AdminCommand = new Command(
				execute: async (parameter_) =>
				{
					Debug.WriteLine("Admin Operation");
					StatusLine = "Start Admin Operation";
					_NowBuilding = true;
					await Models.UCBApi.RunAdminOperation();
					_NowBuilding = false;
					RefreshCanExecutes();
					StatusLine = "Finish";
				},
				canExecute: (parameter_) =>
				{
					return Debugger.IsAttached;
				});

			BuildIOSCommand = new Command(
				execute: async (parameter_) =>
				{
					Debug.WriteLine("Build iOS");
					await BuildCommand("ios");
				},
				canExecute: (parameter_) =>
				{
					return !_NowBuilding;
				});

			BuildAndroidCommand = new Command(
				execute: async (parameter_) =>
				{
					Debug.WriteLine("Build Android");
					await BuildCommand("android");
				},
				canExecute: (parameter_) =>
				{
					return !_NowBuilding;
				});

			foreach (var name in ReleaseVerNames)
			{
				ReleaseVerList.Add(new ReleaseVerViewModel(name));
			}

			SelectedReleaseVersion = ReleaseVerList[0];

			StatusLine = "Ready";
		}

		async Task BuildCommand(string platform)
		{
			StatusLine = "Start Build Command";
			_NowBuilding = true;
			RefreshCanExecutes();

			var param = new Models.UCBApi.StartaBuildParam
			{
				Platform = platform,
				CleanBuild = IsCleanBuild,
				ReleaseBuild = IsReleaseBuild,
				RealStore = IsRealStore,
				ReleaseVer = ReleaseVerNames.IndexOf(SelectedReleaseVersion.Name),
			};

			StatusLine = "StartBuild: " + param;

			var targetNames= await Models.UCBApi.StartBuild(param);

			_NowBuilding = false;
			RefreshCanExecutes();

			if(targetNames.Length == 0)
			{
				StatusLine = "開始出来るビルドが見つかりませんでした";
			}
			else
			{
				StatusLine = "ビルド開始: " + string.Join(" ", targetNames);
			}
		}

		void RefreshCanExecutes()
		{
			(BuildIOSCommand as Command).ChangeCanExecute();
			(BuildAndroidCommand as Command).ChangeCanExecute();
		}
	}
}
