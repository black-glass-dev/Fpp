using UnityEngine;
namespace Fpp
{
    // Original geometric glyphs, one shared texture. No runtime download or per-frame generation.
    public static class TouchIconAtlas
    {
        private static readonly string[] ids={"fire","aim","reload","pickup","sidearm","jump","sprint","crouch","prone","move","mount"};
        private static Sprite[] sprites;
        public static Sprite Get(string id)
        {
            if(sprites==null)
            {
                var texture=Resources.Load<Texture2D>("Presentation/TouchIcons");if(texture==null)return null;
                sprites=new Sprite[ids.Length];
                for(int i=0;i<ids.Length;i++)sprites[i]=Sprite.Create(texture,new Rect((i%4)*64,texture.height-(i/4+1)*64,64,64),Vector2.one*.5f,64);
            }
            for(int i=0;i<ids.Length;i++)if(ids[i]==id)return sprites[i];return null;
        }
    }
}
