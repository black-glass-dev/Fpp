using UnityEngine;
namespace Fpp
{
    public sealed class TestTarget : MonoBehaviour
    {
        public float MaxHealth=100;
        public bool Friendly;
        public float Health {get;private set;}
        private Renderer targetRenderer;
        private float restoreAt;
        private Color original;
        private MaterialPropertyBlock block;
        private static readonly int ColorId=Shader.PropertyToID("_Color");
        private void Awake()
        {
            Health=MaxHealth;targetRenderer=GetComponent<Renderer>();block=new MaterialPropertyBlock();
            if(targetRenderer!=null)original=targetRenderer.sharedMaterial.color;
        }
        public void ResetTarget(){Health=MaxHealth;restoreAt=0;Tint(original);}
        private void Tint(Color color){if(targetRenderer==null)return;block.SetColor(ColorId,color);targetRenderer.SetPropertyBlock(block);}
        public void Hit(){Tint(Color.red);restoreAt=Time.time+GunplayConfig.Current.hitmarkerSeconds;}
        public bool Damage(float damage,HitPart part,Vector3 point,WeaponAsset weapon)
        {
            if(Friendly||Health<=0)return false;
            var c=GunplayConfig.Current;float multiplier=part==HitPart.Head?c.headMultiplier:part==HitPart.Stomach?c.stomachMultiplier:part==HitPart.Arms?c.armMultiplier:part==HitPart.Legs?c.legMultiplier:c.chestMultiplier;
            damage*=multiplier;Health=Mathf.Max(0,Health-damage);Hit();
            string kind=Health<=0?"kill":part==HitPart.Head?"head":"hit";
            GunplayEvents.Send(GunplayEventKind.Hitmarker,point,weapon,kind,damage);
            GunplayEvents.Send(GunplayEventKind.Feed,point,weapon,kind,damage);return true;
        }
        private void Update(){if(restoreAt>0&&Time.time>=restoreAt){Tint(Health<=0?Color.gray:original);restoreAt=0;}}
    }
}
