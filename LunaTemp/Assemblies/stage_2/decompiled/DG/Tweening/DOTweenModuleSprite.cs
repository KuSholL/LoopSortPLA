using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening
{
	public static class DOTweenModuleSprite
	{
		public static TweenerCore<Color, Color, ColorOptions> DOColor(this SpriteRenderer target, Color endValue, float duration)
		{
			TweenerCore<Color, Color, ColorOptions> t = DOTween.To(() => target.color, delegate(Color x)
			{
				target.color = x;
			}, endValue, duration);
			t.SetTarget(target);
			return t;
		}

		public static TweenerCore<Color, Color, ColorOptions> DOFade(this SpriteRenderer target, float endValue, float duration)
		{
			TweenerCore<Color, Color, ColorOptions> t = DOTween.ToAlpha(() => target.color, delegate(Color x)
			{
				target.color = x;
			}, endValue, duration);
			t.SetTarget(target);
			return t;
		}

		public static Sequence DOGradientColor(this SpriteRenderer target, Gradient gradient, float duration)
		{
			Sequence s = DOTween.Sequence();
			GradientColorKey[] colors = gradient.colorKeys;
			int len = colors.Length;
			for (int i = 0; i < len; i++)
			{
				GradientColorKey c = colors[i];
				if (i == 0 && c.time <= 0f)
				{
					target.color = c.color;
					continue;
				}
				float colorDuration = ((i == len - 1) ? (duration - s.Duration(false)) : (duration * ((i == 0) ? c.time : (c.time - colors[i - 1].time))));
				s.Append(target.DOColor(c.color, colorDuration).SetEase(Ease.Linear));
			}
			s.SetTarget(target);
			return s;
		}

		public static Tweener DOBlendableColor(this SpriteRenderer target, Color endValue, float duration)
		{
			endValue -= target.color;
			Color to = new Color(0f, 0f, 0f, 0f);
			return DOTween.To(() => to, delegate(Color x)
			{
				Color color = x - to;
				to = x;
				target.color += color;
			}, endValue, duration).Blendable().SetTarget(target);
		}
	}
}
