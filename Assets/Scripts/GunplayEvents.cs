using System;
using UnityEngine;
namespace Fpp
{
    public enum GunplayEventKind { ShotFired, ShotTailRequest, MechanicalClick, ShellEject, ShellLand, ReloadFoley, Footstep, LandImpact, ADSIn, ADSOut, DryFire, WeaponSwap, Pickup, Hitmarker, Impact, Ricochet, Mount, Mantle, Slide, Dive, UI, Damage, Feed, StateChanged, ReloadProgress, PickupHighlight }
    public struct GunplayEvent
    {
        public GunplayEventKind Kind;
        public Vector3 Position, Normal;
        public WeaponAsset Weapon;
        public string Context, Address;
        public int Index;
        public float Value, Spread;
        public Vector2 Pattern;
        public bool ADS, Suppressed;
    }
    public static class GunplayEvents
    {
        private static readonly string[] Addresses=Enum.GetNames(typeof(GunplayEventKind));
        public static event Action<GunplayEvent> Raised;
        public static event Action<int,Vector2,bool,float> OnShotFired;
        public static event Action<Vector3,Vector3,string> OnImpact;
        public static void Emit(GunplayEvent e)
        {
            if(e.Kind==GunplayEventKind.ShotFired)OnShotFired?.Invoke(e.Index,e.Pattern,e.ADS,e.Spread);
            if(e.Kind==GunplayEventKind.Impact)OnImpact?.Invoke(e.Position,e.Normal,e.Context);
            Raised?.Invoke(e);
        }
        public static void Send(GunplayEventKind kind,Vector3 point,WeaponAsset weapon=null,string context=null,float value=0)
        {Emit(new GunplayEvent{Kind=kind,Position=point,Weapon=weapon,Context=context,Value=value,Address=Addresses[(int)kind]});}
    }
    public enum HitPart { Head, Chest, Stomach, Arms, Legs }
}
