using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PolyMod 
{
    public static class Api
    {
        public static string GetJTokenName(JToken token, int n = 1)
		{
			return token.Path.Split('.')[^n];
		}

        public static Sprite BuildSprite(byte[] data, Vector2 pivot)
		{
			Texture2D texture = new(1, 1);
			texture.filterMode = FilterMode.Trilinear;
			texture.LoadImage(data);
			Console.Write(texture.filterMode);
			return Sprite.Create(texture, new(0, 0, texture.width, texture.height), pivot, 2112);
		}
    }
}