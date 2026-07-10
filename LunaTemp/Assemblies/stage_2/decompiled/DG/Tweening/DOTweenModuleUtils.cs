using System;
using System.Reflection;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.Scripting;

namespace DG.Tweening
{
	public static class DOTweenModuleUtils
	{
		public static class Physics
		{
			public static void SetOrientationOnPath(PathOptions options, Tween t, Quaternion newRot, Transform trans)
			{
				if (options.isRigidbody)
				{
					((Rigidbody)t.target).rotation = newRot;
				}
				else
				{
					trans.rotation = newRot;
				}
			}

			public static bool HasRigidbody2D(Component target)
			{
				return target.GetComponent<Rigidbody2D>() != null;
			}

			
			public static bool HasRigidbody(Component target)
			{
				return target.GetComponent<Rigidbody>() != null;
			}

			
			public static TweenerCore<Vector3, Path, PathOptions> CreateDOTweenPathTween(MonoBehaviour target, bool tweenRigidbody, bool isLocal, Path path, float duration, PathMode pathMode)
			{
				TweenerCore<Vector3, Path, PathOptions> t = null;
				bool rBodyFoundAndTweened = false;
				if (tweenRigidbody)
				{
					Rigidbody rBody = target.GetComponent<Rigidbody>();
					if (rBody != null)
					{
						rBodyFoundAndTweened = true;
						t = (isLocal ? rBody.DOLocalPath(path, duration, pathMode) : rBody.DOPath(path, duration, pathMode));
					}
				}
				if (!rBodyFoundAndTweened && tweenRigidbody)
				{
					Rigidbody2D rBody2D = target.GetComponent<Rigidbody2D>();
					if (rBody2D != null)
					{
						rBodyFoundAndTweened = true;
						t = (isLocal ? rBody2D.DOLocalPath(path, duration, pathMode) : rBody2D.DOPath(path, duration, pathMode));
					}
				}
				if (!rBodyFoundAndTweened)
				{
					t = (isLocal ? target.transform.DOLocalPath(path, duration, pathMode) : target.transform.DOPath(path, duration, pathMode));
				}
				return t;
			}
		}

		private static bool _initialized;

		
		public static void Init()
		{
			if (!_initialized)
			{
				_initialized = true;
				DOTweenExternalCommand.SetOrientationOnPath += Physics.SetOrientationOnPath;
			}
		}

		
		private static void Preserver()
		{
			Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			MethodInfo mi = typeof(MonoBehaviour).GetMethod("Stub");
		}
	}
}
