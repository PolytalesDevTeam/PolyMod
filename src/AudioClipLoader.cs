using UnityEngine;
using UnityEngine.Networking;

namespace PolyMod
{
    public static class AudioClipLoader
    {
        public static AudioClip BuildAudioClip(byte[] data)
        {
            string path = Path.Combine(Application.persistentDataPath, "temp.wav");
            File.WriteAllBytes(path, data);
            WWW www = new("file://" + path);
            while (!www.isDone) {}
            AudioClip audioClip = www.GetAudioClip(false);
            File.Delete(path);
            return audioClip;
        }
    }
}