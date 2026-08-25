using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	// Generate Id:22d18631-b963-4bb6-bb6f-e77a28bb821a
	public partial class StartGameView
	{
		public const string Name = "StartGameView";
		
		[SerializeField]
		public UnityEngine.UI.Button StartBtn;
		[SerializeField]
		public UnityEngine.UI.Button SettingBtn;
		
		private StartGameViewData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			StartBtn = null;
			SettingBtn = null;
			
			mData = null;
		}
		
		public StartGameViewData Data
		{
			get
			{
				return mData;
			}
		}
		
		StartGameViewData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new StartGameViewData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
