using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Modding;
using ModCommon.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using DebugMod;
using DebugMod.Canvas;

namespace AbsRadConfigurableAttacks {
    public class AbsRadConfigurableAttacks : Mod {
        public static AbsRadConfigurableAttacks instance;
        private PlayMakerFSM attackChoicesFSM = null;
        private CanvasPanel topMenuPanel;
        private static Dictionary<string, string> firstPhaseBtnNames = new Dictionary<string, string>() {
            { "nailSweepRight", "firstPhaseNailSweepRight" },
            { "nailSweepLeft", "firstPhaseNailSweepLeft" },
            { "nailSweepTop", "firstPhaseNailSweepTop" },
            { "eyeBeams", "firstPhaseEyeBeams" },
            { "beamSweepLeft", "firstPhaseBeamSweepLeft" },
            { "beamSweepRight", "firstPhaseBeamSweepRight" },
            { "nailFan", "firstPhaseNailFan" },
            { "orbs", "firstPhaseOrbs" },
        };
        private static Dictionary<string, string> btnLabels = new Dictionary<string, string>() {
            { "nailSweep", "Nail Sweep" },
            { "nailSweepRight", "Nail Sweep Right" },
            { "nailSweepLeft", "Nail Sweep Left" },
            { "nailSweepTop", "Sword Rain" },
            { "eyeBeams", "Beam Burst" },
            { "beamSweepLeft", "Beam Sweep Left" },
            { "beamSweepRight", "Beam Sweep Right" },
            { "nailFan", "Sword Burst" },
            { "orbs", "Orb Barrage" },
        };
        private static Dictionary<string, string> platformPhaseBtnNames = new Dictionary<string, string>() {
            { "nailSweep", "platformPhaseNailSweep" },
            { "eyeBeams", "platformPhaseEyeBeams" },
            { "beamSweepLeft", "platformPhaseBeamSweepLeft" },
            { "beamSweepRight", "platformPhaseBeamSweepRight" },
            { "nailFan", "platformPhaseNailFan" },
            { "orbs", "platformPhaseOrbs" },
        };
        private static Dictionary<string, float> firstPhaseDefaults = new Dictionary<string, float>() {
            { "nailSweepRight", 0.5f },
            { "nailSweepLeft", 0.5f },
            { "nailSweepTop", 0.75f },
            { "eyeBeams", 1f },
            { "beamSweepLeft", 0.75f },
            { "beamSweepRight", 0.5f },
            { "nailFan", 1f },
            { "orbs", 1f },
        };
        private static Dictionary<string, float> platformPhaseDefaults = new Dictionary<string, float>(){
            { "nailSweep", 0.5f },
            { "eyeBeams", 1f },
            { "beamSweepLeft", 0.75f },
            { "beamSweepRight", 0.5f },
            { "nailFan", 1f },
            { "orbs", 1f },
        };
        public Dictionary<string, float> firstPhaseWeights = new Dictionary<string, float>(firstPhaseDefaults);
        public Dictionary<string, float> platformPhaseWeights = new Dictionary<string, float>(platformPhaseDefaults);

        public AbsRadConfigurableAttacks() : base("AbsRad Configurable Attacks") { 
            instance = this;
        }

        public override void Initialize() {
            Log("Initializing");

            USceneManager.activeSceneChanged += InitiateRadiancePolling;
            topMenuPanel = ((DebugMod.Canvas.CanvasPanel) typeof(DebugMod.TopMenu).GetField("panel", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));

            DebugMod.DebugMod.AddTopMenuContent(
                MenuName: "AR Start",
                ButtonList: new List<TopMenuButton>(firstPhaseDefaults.Keys.ToList().Select(key =>
                    new AttackConfigButton(
                        name: firstPhaseBtnNames[key],
                        buttonText: string.Format("{0}: {1:0.##}", btnLabels[key], firstPhaseWeights[key]),
                        clickedFunction: _ => ApplyChange(key)
                    )
                ))
            );
            DebugMod.DebugMod.AddTopMenuContent(
                MenuName: "AR Plats",
                ButtonList: new List<TopMenuButton>(platformPhaseDefaults.Keys.ToList().Select(key =>
                    new AttackConfigButton(
                        name: platformPhaseBtnNames[key],
                        buttonText: string.Format("{0}: {1:0.##}", btnLabels[key], platformPhaseWeights[key]),
                        clickedFunction: _ => ApplyChange(key, false)
                    )
                ))
            );

            Log("Initialized");
        }

        private void InitiateRadiancePolling(Scene from, Scene to) {
            if (to.name == "GG_Radiance") {
                ModHooks.Instance.HeroUpdateHook += RadiancePoll;
            } else {
                ModHooks.Instance.HeroUpdateHook -= RadiancePoll;
            }
        }

        private void RadiancePoll() {
            if (this.attackChoicesFSM == null) {
                GameObject absRad = GameObject.Find("Absolute Radiance");
                if (absRad != null) {
                    this.attackChoicesFSM = absRad.LocateMyFSM("Attack Choices");
                    ModHooks.Instance.HeroUpdateHook -= RadiancePoll;
                    UpdateWeightsFSM();
                    CheckRepititionCap();
                }
            } else {
                ModHooks.Instance.HeroUpdateHook -= RadiancePoll;
            }
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public override int LoadPriority() => 2; // 1.4 modding api default is 1 instead of 0 :/

        private void ResetFirstPhases() {
            firstPhaseWeights = new Dictionary<string, float>(firstPhaseDefaults);
            UpdateWeightsFSM();
            RemoveFirstPhasesAttackRepititionCap();
        }

        private void ResetPlatsPhase() {
            platformPhaseWeights = new Dictionary<string, float>(platformPhaseDefaults);
            UpdateWeightsFSM();
            RemovePlatsAttackRepititionCap();
        }

        private void UpdateWeightsFSM() {
            if (attackChoicesFSM == null) return;

            attackChoicesFSM.GetAction<SendRandomEventV3>("A1 Choice", 1).weights = new FsmFloat[]{
                firstPhaseWeights["nailSweepRight"],
                firstPhaseWeights["nailSweepLeft"],
                firstPhaseWeights["nailSweepTop"],
                firstPhaseWeights["eyeBeams"],
                firstPhaseWeights["beamSweepLeft"],
                firstPhaseWeights["beamSweepRight"],
                firstPhaseWeights["nailFan"],
                firstPhaseWeights["orbs"]
            };
            attackChoicesFSM.GetAction<SendRandomEventV3>("A2 Choice", 1).weights = new FsmFloat[]{
                platformPhaseWeights["nailSweep"],
                platformPhaseWeights["nailFan"],
                platformPhaseWeights["orbs"],
                platformPhaseWeights["eyeBeams"],
                platformPhaseWeights["beamSweepLeft"],
                platformPhaseWeights["beamSweepRight"],
            };
        }

        public void ApplyChange(string key, bool firstPhase = true) {
            if (firstPhase) {
                firstPhaseWeights[key] += 0.25f;
                if (firstPhaseWeights[key] > 1f) {
                    firstPhaseWeights[key] = 0f;
                }
                topMenuPanel.GetButton(firstPhaseBtnNames[key], "AR Start");
            } else {
                platformPhaseWeights[key] += 0.25f;
                if (platformPhaseWeights[key] > 1f) {
                    platformPhaseWeights[key] = 0f;
                }
                topMenuPanel.GetButton(platformPhaseBtnNames[key], "AR Plats");
            }
            UpdateWeightsFSM();
            CheckRepititionCap();
            DebugMod.TopMenu.Update();
        }

        private bool FirstPhaseSettingsAreDefault() {
            foreach (var key in firstPhaseWeights.Keys) {
                if (!firstPhaseWeights[key].Equals(firstPhaseDefaults[key])) {
                    return false;
                }
            }
            return true;
        }

        private bool PlatsSettingsAreDefault() {
            foreach (var key in platformPhaseWeights.Keys) {
                if (!platformPhaseWeights[key].Equals(platformPhaseDefaults[key])) {
                    return false;
                }
            }
            return true;
        }

        private void CheckRepititionCap() {
            if (FirstPhaseSettingsAreDefault()) {
                AddFirstPhasesAttackRepititionCap();
            } else {
                RemoveFirstPhasesAttackRepititionCap();
            }

            if (PlatsSettingsAreDefault()) {
                AddPlatsAttackRepititionCap();
            } else {
                RemovePlatsAttackRepititionCap();
            }
        }

        private void RemoveFirstPhasesAttackRepititionCap() {
            if (attackChoicesFSM == null) return;
            SendRandomEventV3 action = attackChoicesFSM.GetAction<SendRandomEventV3>("A1 Choice", 1);
            action.eventMax = new FsmInt[]{10000, 10000, 10000, 10000, 10000, 10000, 10000, 10000};
            action.missedMax = new FsmInt[]{10000, 10000, 10000, 10000, 10000, 10000, 10000, 10000};
        }

        private void AddFirstPhasesAttackRepititionCap() {
            if (attackChoicesFSM == null) return;
            SendRandomEventV3 action = attackChoicesFSM.GetAction<SendRandomEventV3>("A1 Choice", 1);
            action.eventMax = new FsmInt[]{1, 1, 1, 2, 1, 1, 2, 1};
            action.missedMax = new FsmInt[]{12, 12, 12, 10, 12, 12, 10, 12};
        }

        private void RemovePlatsAttackRepititionCap() {
            if (attackChoicesFSM == null) return;
            SendRandomEventV3 action = attackChoicesFSM.GetAction<SendRandomEventV3>("A2 Choice", 1);
            action.eventMax = new FsmInt[]{10000, 10000, 10000, 10000, 10000, 10000};
            action.missedMax = new FsmInt[]{10000, 10000, 10000, 10000, 10000, 10000};
        }

        private void AddPlatsAttackRepititionCap() {
            if (attackChoicesFSM == null) return;
            SendRandomEventV3 action = attackChoicesFSM.GetAction<SendRandomEventV3>("A2 Choice", 1);
            action.eventMax = new FsmInt[]{1, 2, 1, 2, 1, 1};
            action.missedMax = new FsmInt[]{12, 10, 10, 10, 12, 12};
        }
    }

    public class AttackConfigButton:TopMenuButton {
        public string ButtonText { get; }
        public string Name { get; }
        public AttackConfigButton(string name, string buttonText, UnityAction<string> clickedFunction) {
            Name = name;
            ButtonText = buttonText;
            ClickedFunction = clickedFunction;
        }
        public override void CreateButton(CanvasPanel panel){
            panel.AddButton(ButtonText,
                GUIController.Instance.images["ButtonRectEmpty"],
                panel.GetNextPos(CanvasPanel.MenuItems.TextButton),
                Vector2.zero,
                ClickedFunction,
                new Rect(0f, 0f, 90f, 20f),
                GUIController.Instance.trajanNormal,
                ButtonText,
                9);
        }
    }
}