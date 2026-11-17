using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaserGun
{
    // The visible beam: two short-lived trails (a white-hot core and a coloured glow) that follow each
    // shot. At laser speed a trail that lives ~0.07 s stretches from the muzzle to the target, so it reads
    // as a beam. The trails are not children of the bullet, so they fade out on their own after it is gone.
    internal static class Beam
    {
        sealed class Shot { public BasicBullet B; public GameObject Core, Glow; public float Born; public Vector3 Start, Last; public int Layer, Order; public Linger Line; }
        sealed class Linger { public GameObject Go; public LineRenderer Core, Glow; public float Born, Life; public Color C; }
        static readonly List<Linger> _lingers = new List<Linger>();
        static readonly List<Shot> _shots = new List<Shot>();
        static readonly HashSet<IntPtr> _tracked = new HashSet<IntPtr>();
        static Material _mat;
        static bool _matMissing;

        internal static bool Tracked(BasicBullet b) { try { return b != null && _tracked.Contains(b.Pointer); } catch { return false; } }

        internal static Color Colour()
        {
            string s = (Settings.BeamColor.Value ?? "").Trim().TrimStart('#');
            if (s.Length == 6 && int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out int v))
                return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f);
            return new Color(1f, 0.23f, 0.12f, 1f);
        }

        internal static void Fire(BasicBullet b)
        {
            if (b == null) return;
            try { _tracked.Add(b.Pointer); } catch { }
            if (!Settings.Beam.Value) return;
            var mat = Mat(b);
            if (mat == null) return;
            int layer = 0, order = 0;
            try
            {
                var r = b.GetComponentInChildren<Renderer>();
                if (r != null) { layer = r.sortingLayerID; order = r.sortingOrder; }
                // Hide the game's bullet sprite and tracer: the beam replaces them.
                foreach (var rr in b.GetComponentsInChildren<Renderer>()) if (rr != null) rr.enabled = false;
            }
            catch { }
            Vector3 p = b.transform.position;
            // Local shots start at the tip of the barrel, not where the game spawns the bullet.
            bool muzzle = Battery.MuzzleFrame >= Time.frameCount - 1;
            if (muzzle) { var m = Battery.Muzzle; m.z = p.z; p = m; }
            Color c = Colour();
            float w = Settings.BeamWidth.Value, t = Settings.BeamFade.Value;
            var s = new Shot { B = b, Born = Time.time, Start = p, Last = p, Layer = layer, Order = order };
            if (!muzzle) try { var sp = b.startPos; if (sp != Vector3.zero) { sp.z = p.z; s.Start = sp; } } catch { }
            s.Glow = Make("LaserGun.Glow", p, mat, layer, order + 1, t, w * 2.4f, new Color(c.r, c.g, c.b, 0.55f), new Color(c.r, c.g, c.b, 0f));
            s.Core = Make("LaserGun.Core", p, mat, layer, order + 2, t * 0.8f, w, Color.Lerp(c, Color.white, 0.75f), new Color(c.r, c.g, c.b, 0f));
            try { s.Line = StartLine(s); } catch (Exception e) { PLog.Debug("Trail: " + e.Message); }
            _shots.Add(s);
        }

        static GameObject Make(string name, Vector3 p, Material mat, int layer, int order, float time, float width, Color head, Color tail)
        {
            var go = new GameObject(name);
            go.transform.position = p;
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = mat;
            tr.time = time;
            tr.widthMultiplier = width;
            tr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.6f);
            tr.startColor = head;
            tr.endColor = tail;
            tr.minVertexDistance = 0.05f;
            tr.numCapVertices = 2;
            tr.sortingLayerID = layer;
            tr.sortingOrder = order;
            tr.emitting = true;
            return go;
        }

        static Material Mat(BasicBullet b)
        {
            if (_mat != null) return _mat;
            if (_matMissing) return null;
            try { var sh = Shader.Find("Sprites/Default"); if (sh != null) _mat = new Material(sh); } catch { }
            if (_mat == null) { try { var t = b.trailRenderer; if (t != null) _mat = t.sharedMaterial; } catch { } }
            if (_mat == null) { _matMissing = true; PLog.Warn("No material for the laser beam; beams are off."); return null; }
            UnityEngine.Object.DontDestroyOnLoad(_mat);
            return _mat;
        }

        // A hit: snap the beam's head to the impact point so it ends exactly on the target.
        internal static void HitAt(BasicBullet b, Vector3 at)
        {
            foreach (var s in _shots)
                if (s.B != null && s.B.Pointer == b.Pointer) { Move(s, at); break; }
        }

        static void Move(Shot s, Vector3 p)
        {
            s.Last = p;
            try { if (s.Line != null) { s.Line.Glow.SetPosition(1, p); s.Line.Core.SetPosition(1, p); } } catch { }
            try { if (s.Core != null) s.Core.transform.position = p; if (s.Glow != null) s.Glow.transform.position = p; } catch { }
        }

        // The lingering trail: a soft line along the whole shot path that hangs in the air and fades out.
        // A solid line from the barrel to the shot, drawn from the first frame so the beam visibly leaves the muzzle.
        // When the shot ends it stays behind as the lingering trail and fades.
        static Linger StartLine(Shot s)
        {
            if (_mat == null || !Settings.Beam.Value) return null;
            if (_lingers.Count > 80) return null;
            var go = new GameObject("LaserGun.Trail");
            var l = new Linger { Go = go, Born = -1f, Life = Mathf.Max(0.05f, Settings.TrailLinger.Value), C = Colour() };
            float w = Settings.BeamWidth.Value;
            l.Glow = Line(go, s, w * 3.2f, s.Order);
            var core = new GameObject("Core"); core.transform.SetParent(go.transform, false);
            l.Core = Line(core, s, w * 0.9f, s.Order + 1);
            Paint(l, 0f);
            _lingers.Add(l);
            return l;
        }

        static void LeaveTrail(Shot s)
        {
            var l = s.Line; if (l == null) return;
            l.Born = Time.time;   // start fading now
        }

        static LineRenderer Line(GameObject go, Shot s, float width, int order)
        {
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = _mat;
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, s.Start); lr.SetPosition(1, s.Last);
            lr.widthMultiplier = width;
            lr.numCapVertices = 2;
            lr.sortingLayerID = s.Layer;
            lr.sortingOrder = order;
            return lr;
        }

        static void Paint(Linger l, float k)
        {
            // k: 0 fresh .. 1 gone. The hot core cools quickly; the coloured glow hangs on and thins out.
            float glow = 0.45f * (1f - k) * (1f - k), core = 0.8f * Mathf.Clamp01(1f - k * 2.5f);
            var g = new Color(l.C.r, l.C.g, l.C.b, glow);
            var c = Color.Lerp(l.C, Color.white, 0.6f); c.a = core;
            l.Glow.startColor = g; l.Glow.endColor = g;
            l.Core.startColor = c; l.Core.endColor = c;
            float w = Settings.BeamWidth.Value;
            l.Glow.widthMultiplier = w * (3.2f + 2f * k);
        }

        internal static void Tick()
        {
            for (int i = _lingers.Count - 1; i >= 0; i--)
            {
                var l = _lingers[i];
                if (l.Born < 0f) continue;   // still attached to a live shot
                float k = (Time.time - l.Born) / l.Life;
                bool dead = k >= 1f;
                try { if (!dead) Paint(l, k); } catch { dead = true; }
                if (dead) { try { if (l.Go != null) UnityEngine.Object.Destroy(l.Go); } catch { } _lingers.RemoveAt(i); }
            }
            for (int i = _shots.Count - 1; i >= 0; i--)
            {
                var s = _shots[i];
                bool alive = false;
                try { alive = s.B != null && s.B.gameObject != null && s.B.gameObject.activeInHierarchy; } catch { }
                if (alive && Time.time - s.Born < 3f) { Move(s, s.B.transform.position); continue; }
                Release(s);
                _shots.RemoveAt(i);
            }
        }

        static void Release(Shot s)
        {
            try { if (s.B != null) _tracked.Remove(s.B.Pointer); } catch { }
            try { LeaveTrail(s); } catch (Exception e) { PLog.Debug("Trail: " + e.Message); }
            float t = Settings.BeamFade.Value + 0.1f;
            try { if (s.Core != null) { s.Core.GetComponent<TrailRenderer>().emitting = false; UnityEngine.Object.Destroy(s.Core, t); } } catch { }
            try { if (s.Glow != null) { s.Glow.GetComponent<TrailRenderer>().emitting = false; UnityEngine.Object.Destroy(s.Glow, t); } } catch { }
        }

        internal static void Clear()
        {
            foreach (var s in _shots) Release(s);
            _shots.Clear();
            _tracked.Clear();
            foreach (var l in _lingers) { try { if (l.Go != null) UnityEngine.Object.Destroy(l.Go); } catch { } }
            _lingers.Clear();
        }
    }
}
