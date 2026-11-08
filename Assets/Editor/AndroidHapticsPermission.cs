#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;
namespace Fpp.Editor
{
    public sealed class AndroidHapticsPermission:IPostGenerateGradleAndroidProject
    {
        public int callbackOrder=>10;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifest=Path.Combine(path,"src/main/AndroidManifest.xml");var document=new XmlDocument();document.Load(manifest);
            const string android="http://schemas.android.com/apk/res/android";
            foreach(XmlNode node in document.DocumentElement.ChildNodes)if(node.Name=="uses-permission"&&node.Attributes?["name",android]?.Value=="android.permission.VIBRATE")return;
            var permission=document.CreateElement("uses-permission");var name=document.CreateAttribute("android","name",android);name.Value="android.permission.VIBRATE";permission.Attributes.Append(name);document.DocumentElement.AppendChild(permission);document.Save(manifest);
        }
    }
}
#endif
