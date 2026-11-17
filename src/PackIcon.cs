using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LaserGun
{
    // Draws the battery pack on the rifle's inventory picture while a pack is loaded. The game only shows the bare
    // gun there, so a small image is slipped in behind the gun's icon and kept lined up with it.
    static class PackIcon
    {
        const string HolderName = "LaserGun_PackIcon";
        static readonly List<BasicItemUI> _tracked = new List<BasicItemUI>();
        static Sprite _sprite;
        static bool _warned, _logged;

        internal static void Refreshed(BasicItemUI ui)
        {
            try
            {
                if (ui == null) return;
                var d = ui.itemdata;
                if (d == null || !Weapons.IsLaser(d.itemId)) return;
                for (int i = 0; i < _tracked.Count; i++) if (_tracked[i].Pointer == ui.Pointer) { Update(ui); return; }
                _tracked.Add(ui);
                Update(ui);
            }
            catch (Exception e) { Warn(e); }
        }

        internal static void Tick()
        {
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                try
                {
                    var ui = _tracked[i];
                    if (ui == null) { _tracked.RemoveAt(i); continue; }
                    if (!Update(ui)) _tracked.RemoveAt(i);
                }
                catch (Exception e) { _tracked.RemoveAt(i); Warn(e); }
            }
        }

        static void Warn(Exception e) { if (_warned) return; _warned = true; PLog.Warn("Inventory pack picture failed: " + e); }

        // false = this slot no longer shows the rifle
        static bool Update(BasicItemUI ui)
        {
            var icon = ui.iconImage;
            if (icon == null) return false;
            var iconRt = icon.rectTransform;
            var parent = iconRt.parent;
            if (parent == null) return false;
            var ht = parent.Find(HolderName);
            var d = ui.itemdata;
            bool mine = d != null && Weapons.IsLaser(d.itemId);
            bool show = mine && Settings.ShowPack.Value && Battery.HasBattery(d) && icon.enabled && icon.sprite != null;
            if (!show)
            {
                if (ht != null && ht.gameObject.activeSelf) ht.gameObject.SetActive(false);
                return mine;
            }
            var sp = Sprite(); if (sp == null) return true;

            RectTransform holder; Image img;
            if (ht == null)
            {
                var go = new GameObject(HolderName);
                holder = go.AddComponent<RectTransform>();
                holder.SetParent(parent, false);
                try { go.AddComponent<LayoutElement>().ignoreLayout = true; } catch { }
                var child = new GameObject("pack");
                img = child.AddComponent<Image>();
                img.raycastTarget = false;
                img.rectTransform.SetParent(holder, false);
            }
            else
            {
                holder = ht.Cast<RectTransform>();
                img = holder.GetChild(0).GetComponent<Image>();
            }
            if (!holder.gameObject.activeSelf) holder.gameObject.SetActive(true);
            // behind the gun picture: just before it among its siblings
            int want = iconRt.GetSiblingIndex();
            int have = holder.GetSiblingIndex();
            if (have > want) holder.SetSiblingIndex(want);
            else if (have < want - 1) holder.SetSiblingIndex(want - 1);

            // same box as the gun picture
            holder.anchorMin = iconRt.anchorMin; holder.anchorMax = iconRt.anchorMax; holder.pivot = iconRt.pivot;
            holder.anchoredPosition = iconRt.anchoredPosition; holder.sizeDelta = iconRt.sizeDelta;
            holder.localRotation = iconRt.localRotation; holder.localScale = iconRt.localScale;

            // how big the gun is drawn inside that box
            var r = iconRt.rect; var gs = icon.sprite.rect;
            float w = Mathf.Abs(r.width), h = Mathf.Abs(r.height);
            if (icon.preserveAspect && gs.height > 0.5f && h > 0.5f)
            {
                float a = gs.width / gs.height;
                if (w / h > a) w = h * a; else h = w / a;
            }
            float sx = w / 299f, sy = h / 118f; // rifle picture is 299 x 118 px
            var px = Battery.PackPx();
            float size = Settings.PackSize.Value;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(px.x * sx, px.y * sy);
            rt.sizeDelta = new Vector2(Battery.PackWidth * sx * size, Battery.PackWidth * 140f / 112f * sy * size);
            if (img.sprite != sp) img.sprite = sp;
            img.color = icon.color;
            if (!_logged) { _logged = true; PLog.Info($"Inventory pack picture: gun icon {w:0}x{h:0} (sprite {gs.width:0}x{gs.height:0}, keep aspect {icon.preserveAspect}, rotation {iconRt.localEulerAngles.z:0})"); }
            return true;
        }

        static Sprite Sprite()
        {
            if (_sprite != null) return _sprite;
            var tex = Battery.PackTexture(); if (tex == null) return null;
            _sprite = UnityEngine.Sprite.Create(tex, new Rect(8, tex.height - 2 - 140, 112, 140), new Vector2(0.5f, 0.5f), 100f);
            _sprite.hideFlags = HideFlags.HideAndDontSave;
            return _sprite;
        }
    }
}
