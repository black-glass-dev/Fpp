using Fpp;
namespace Fpp.Tests
{
    public sealed class ScriptedInputProvider : IInputProvider
    {
        public InputFrame Value;
        public int LastPolledFrame;
        public InputFrame Poll(GunplayConfig c){LastPolledFrame=UnityEngine.Time.frameCount;var result=Value;Value.FirePressed=Value.Reload=Value.Jump=Value.Pickup=Value.Swap=Value.CrouchPressed=Value.Prone=Value.Mount=false;return result;}
        public void Reset(){Value=default(InputFrame);}
    }
}
