using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	// Generate Id:e3516c62-14a8-4ca6-8d6a-f39b1fe0ea65
	public partial class GMView
	{
		public const string Name = "GMView";
		
		
		private GMViewData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public GMViewData Data
		{
			get
			{
				return mData;
			}
		}
		
		GMViewData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GMViewData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
