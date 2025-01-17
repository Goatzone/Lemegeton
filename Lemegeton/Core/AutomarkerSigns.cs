﻿using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Lemegeton.Core
{

    public class AutomarkerSigns
    {

        public class Preset
        {

            public string Name { get; set; } = "New preset";
            public bool Builtin { get; set; } = true;
            public Dictionary<string, SignEnum> Roles { get; set; } = new Dictionary<string, SignEnum>();

        }

        public enum SignEnum
        {
            None,
            Attack1, Attack2, Attack3, Attack4, Attack5,
            Bind1, Bind2, Bind3,
            Ignore1, Ignore2,
            Square, Circle, Plus, Triangle,
            AttackNext, BindNext, IgnoreNext,
        }

        public Dictionary<string, Preset> Presets { get; set; }
        public Dictionary<string, SignEnum> Roles { get; set; }
        public string SelectedPreset { get; set; } = null;
        
        public AutomarkerSigns()
        {
            Roles = new Dictionary<string, SignEnum>();
            Presets = new Dictionary<string, Preset>();
        }

        public void SetRole(string id, SignEnum sign, bool autoswap = true)
        {
            if (autoswap == true && sign != SignEnum.None && sign != SignEnum.AttackNext && sign != SignEnum.BindNext && sign != SignEnum.IgnoreNext)
            {
                var existing = (from ix in Roles where ix.Value == sign select ix.Key).FirstOrDefault();
                if (existing != null)
                {
                    if (Roles.ContainsKey(id) == true)
                    {
                        Roles[existing] = Roles[id];
                    }
                    else
                    {
                        Roles[existing] = SignEnum.None;
                    }
                }
            }
            Roles[id] = sign;
        }

        public void AddPreset(Preset preset)
        {
            if (Presets.ContainsKey(preset.Name) == true)
            {
                if (Presets[preset.Name].Builtin == true && preset.Builtin == false)
                {
                    return;
                }
            }
            Presets[preset.Name] = preset;
        }

        public void ApplyPreset(string id)
        {
            if (id == null || Presets.ContainsKey(id) == false)
            {
                SelectedPreset = null;
                return;
            }
            SelectedPreset = id;
            Preset pr = Presets[id];
            foreach (KeyValuePair<string, AutomarkerSigns.SignEnum> kp in pr.Roles)
            {
                SetRole(kp.Key, kp.Value, false);
            }
        }

        public string Serialize()
        {
            List<string> temp = new List<string>();
            temp.Add(string.Format("SelectedPreset={0}", SelectedPreset));
            foreach (KeyValuePair<string, AutomarkerSigns.SignEnum> kp in Roles)
            {
                temp.Add(string.Format("{0}={1}", kp.Key, kp.Value.ToString()));
            }
            List<Preset> userpresets = (from ix in Presets.Values where ix.Builtin == false select ix).ToList();
            // todo serialize user presets
            return string.Join(";", temp);
        }

        public void Deserialize(string data)
        {
            string[] items = data.Split(";");
            ApplyPreset(null);
            // todo deserialize user presets
            foreach (string item in items)
            {
                string[] kp = item.Split("=");
                if (kp[0] == "SelectedPreset" && kp[1] != "")
                {
                    ApplyPreset(kp[1]);
                    return;
                }
                else
                {
                    if (Roles.ContainsKey(kp[0]) == true)
                    {
                        AutomarkerSigns.SignEnum sign = (AutomarkerSigns.SignEnum)Enum.Parse(typeof(AutomarkerSigns.SignEnum), kp[1]);
                        SetRole(kp[0], sign, false);                        
                    }
                }
            }
        }

        internal AutomarkerPayload TestFunctionality(State st, AutomarkerPrio amp, AutomarkerTiming at, bool selfOnly, bool soft)
        {            
            Party pty = st.GetPartyMembers();
            if (amp != null)
            {
                amp.SortByPriority(pty.Members);
                int i = 1;
                foreach (Party.PartyMember pm in pty.Members)
                {
                    pm.Selection = i++;
                }
            }
            else
            {
                st.AssignRandomSelections(pty, Roles.Count);
            }
            int roleId = 1;
            AutomarkerPayload ap = new AutomarkerPayload(st, selfOnly, soft);
            foreach (KeyValuePair<string, AutomarkerSigns.SignEnum> kp in Roles)
            {
                var player = (from px in pty.Members where px.Selection == roleId select px).FirstOrDefault();
                if (player != null)
                {
                    st.Log(State.LogLevelEnum.Debug, null, "Assigning test role {0} with sign {1} to {2}", kp.Key, kp.Value, player.GameObject);
                    ap.Assign(kp.Value, player.GameObject);
                }
                roleId++;
            }            
            st.ExecuteAutomarkers(ap, at);
            return ap;
        }

    }

}
