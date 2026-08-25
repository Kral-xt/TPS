using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	// Generate Id:565f0d79-7f53-47fb-b4c6-a273a6c858fa
	public partial class BattleView
	{
		public const string Name = "BattleView";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI FPS;
		[SerializeField]
		public TMPro.TextMeshProUGUI Speed;
		[SerializeField]
		public UnityEngine.UI.Slider Slider_HP;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text_Hp;
		[SerializeField]
		public UnityEngine.UI.Slider Slider_EXP;
		[SerializeField]
		public UnityEngine.UI.Image Image_UserProfile;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text_Level;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text_Name;
		[SerializeField]
		public RectTransform BulletTimeSlider;
		[SerializeField]
		public UnityEngine.UI.Image crosshair;
		[SerializeField]
		public RectTransform EmojiBar;
		
		private BattleViewData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			FPS = null;
			Speed = null;
			Slider_HP = null;
			Text_Hp = null;
			Slider_EXP = null;
			Image_UserProfile = null;
			Text_Level = null;
			Text_Name = null;
			BulletTimeSlider = null;
			crosshair = null;
			EmojiBar = null;
			
			mData = null;
		}
		
		public BattleViewData Data
		{
			get
			{
				return mData;
			}
		}
		
		BattleViewData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BattleViewData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
