using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	// Generate Id:faad3154-7ba4-4a98-9567-352b00fbe521
	public partial class BagDialog
	{
		public const string Name = "BagDialog";
		
		
		private BagDialogData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public BagDialogData Data
		{
			get
			{
				return mData;
			}
		}
		
		BagDialogData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BagDialogData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
