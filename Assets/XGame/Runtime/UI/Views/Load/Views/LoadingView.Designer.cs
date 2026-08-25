using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	// Generate Id:d648bca4-b0c8-4b65-848a-0f5563d92161
	public partial class LoadingView
	{
		public const string Name = "LoadingView";
		
		[SerializeField]
		public UnityEngine.UI.Slider SliderBar_Loading;
		
		private LoadingViewData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			SliderBar_Loading = null;
			
			mData = null;
		}
		
		public LoadingViewData Data
		{
			get
			{
				return mData;
			}
		}
		
		LoadingViewData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new LoadingViewData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
