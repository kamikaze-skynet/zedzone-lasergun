using System;
using System.IO;
using UnityEngine;

namespace LaserGun
{
    // The rifle's crack. Loads fire.wav from the mod's folder (next to LaserGun.dll, or its "sound"
    // sub-folder) once, builds an AudioClip from the samples and plays it at the muzzle on every shot.
    // Any 8/16/24-bit PCM or 32-bit float WAV works, so players can swap in their own sound.
    internal static class Sound
    {
        static AudioClip _clip;
        static bool _tried;
        static float _last;

        static string ModDir()
        {
            try { return Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? ""; } catch { return ""; }
        }

        static AudioClip Clip()
        {
            if (_clip != null) { try { if (_clip.samples > 0) return _clip; } catch { } _clip = null; }
            if (_tried) return null;
            _tried = true;
            string name = (Settings.SoundFile.Value ?? "").Trim();
            if (name.Length == 0) name = "fire.wav";
            string dir = ModDir();
            string path = Path.IsPathRooted(name) ? name : Path.Combine(dir, name);
            if (!File.Exists(path)) path = Path.Combine(dir, "sound", name);
            if (!File.Exists(path)) { PLog.Warn($"Sound file '{name}' not found next to LaserGun.dll; the game's own shot sound plays instead."); return null; }
            try
            {
                _clip = Wav.Load(path);
                UnityEngine.Object.DontDestroyOnLoad(_clip);
                _clip.hideFlags = HideFlags.HideAndDontSave;
                PLog.Info($"Laser sound: {path} ({_clip.length:0.00} s, {_clip.frequency} Hz, {_clip.channels} ch)");
            }
            catch (Exception e) { PLog.Error("Reading " + path + " failed", e); _clip = null; }
            return _clip;
        }

        // Your own shots play on one dedicated 2D channel: every shot stops the sound and starts it again from the
        // beginning, so each shot is the whole clip from its start and fast fire repeats it shot after shot.
        // Other players' shots play at their gun from a few 3D channels, each restarted the same way.
        static AudioSource _own;
        static readonly AudioSource[] _remote = new AudioSource[4];
        static int _nextRemote;
        static bool _warned;

        static AudioSource MakeSource(string name, bool spatial)
        {
            var go = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.loop = false; src.priority = 32;
            src.spatialBlend = spatial ? 1f : 0f;
            if (spatial) { src.rolloffMode = AudioRolloffMode.Linear; src.minDistance = 4f; src.maxDistance = 40f; src.dopplerLevel = 0f; }
            return src;
        }

        static void Volume(AudioSource src, float vol)
        {
            try { AudioController.ApplySoundVolume(src, vol); } catch { src.volume = Mathf.Clamp01(vol); }
        }

        internal static void Shot(Vector3 at, bool remote)
        {
            float vol = Settings.SoundVolume.Value;
            if (vol <= 0f) return;
            if (Time.time - _last < 0.03f && !remote) return;
            var clip = Clip();
            if (clip == null) return;
            try
            {
                AudioSource src;
                if (!remote)
                {
                    if (_own == null) _own = MakeSource("LaserGun.Sound", false);
                    src = _own;
                    _last = Time.time;
                }
                else
                {
                    int k = _nextRemote; _nextRemote = (_nextRemote + 1) % _remote.Length;
                    if (_remote[k] == null) _remote[k] = MakeSource("LaserGun.Sound" + k, true);
                    src = _remote[k];
                    src.transform.position = new Vector3(at.x, at.y, 0f);
                }
                src.Stop();
                if (src.clip != clip) src.clip = clip;
                Volume(src, vol);
                src.time = 0f;
                src.Play();
            }
            catch (Exception e)
            {
                if (!_warned) { _warned = true; PLog.Warn("Laser sound channel failed, using the game's sound player: " + e.Message); }
                try { AudioController.instance?.PlayAt(clip, at, vol); } catch { }
            }
        }
    }

    internal static class Wav
    {
        internal static AudioClip Load(string path)
        {
            byte[] b = File.ReadAllBytes(path);
            if (b.Length < 44 || b[0] != 'R' || b[1] != 'I' || b[2] != 'F' || b[3] != 'F') throw new Exception("not a RIFF/WAV file");
            int pos = 12, fmt = 0, channels = 0, rate = 0, bits = 0, dataPos = -1, dataLen = 0;
            while (pos + 8 <= b.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(b, pos, 4);
                int len = BitConverter.ToInt32(b, pos + 4);
                int body = pos + 8;
                if (id == "fmt ")
                {
                    fmt = BitConverter.ToInt16(b, body);
                    channels = BitConverter.ToInt16(b, body + 2);
                    rate = BitConverter.ToInt32(b, body + 4);
                    bits = BitConverter.ToInt16(b, body + 14);
                    if (fmt == unchecked((short)0xFFFE) || fmt == -2) fmt = BitConverter.ToInt16(b, body + 24); // WAVE_FORMAT_EXTENSIBLE
                }
                else if (id == "data") { dataPos = body; dataLen = Math.Min(len, b.Length - body); break; }
                pos = body + len + (len & 1);
            }
            if (dataPos < 0 || channels <= 0 || rate <= 0) throw new Exception("missing fmt or data chunk");
            int bytesPer = bits / 8;
            if (bytesPer <= 0) throw new Exception("bad bit depth " + bits);
            int count = dataLen / bytesPer;
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                int o = dataPos + i * bytesPer;
                if (fmt == 3 && bits == 32) data[i] = BitConverter.ToSingle(b, o);
                else if (bits == 16) data[i] = BitConverter.ToInt16(b, o) / 32768f;
                else if (bits == 8) data[i] = (b[o] - 128) / 128f;
                else if (bits == 24) data[i] = ((b[o] << 8 | b[o + 1] << 16 | b[o + 2] << 24) >> 8) / 8388608f;
                else if (bits == 32) data[i] = BitConverter.ToInt32(b, o) / 2147483648f;
                else throw new Exception("unsupported WAV format " + fmt + "/" + bits + " bit");
            }
            var clip = AudioClip.Create("LaserGun.Fire", count / channels, channels, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
