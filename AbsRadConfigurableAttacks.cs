using System.Collections.Generic;
using System.Reflection;
using Modding;
using ModCommon.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
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
        private static Dictionary<string, string> platformPhaseBtnNames = new Dictionary<string, string>() {
            { "nailSweep", "platformPhaseNailSweep" },
            { "eyeBeams", "platformPhaseEyeBeams" },
            { "beamSweepLeft", "platformPhaseBeamSweepLeft" },
            { "beamSweepRight", "platformPhaseBeamSweepRight" },
            { "nailFan", "platformPhaseNailFan" },
            { "orbs", "platformPhaseOrbs" },
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
        private static Dictionary<string, float> firstPhaseDefaults = new Dictionary<string, float>() {
            { "nailSweepRight", 0.5f },
            { "nailSweepLeft", 0.5f },
            { "nailSweepTop", 0.75f },
            { "eyeBeams", 1f },
            { "beamSweepLeft", 0.75f },
            { "beamSweepRight", 0.75f },
            { "nailFan", 1f },
            { "orbs", 1f },
        };
        private static Dictionary<string, float> platformPhaseDefaults = new Dictionary<string, float>(){
            { "nailSweep", 0.5f },
            { "eyeBeams", 1f },
            { "beamSweepLeft", 0.75f },
            { "beamSweepRight", 0.75f },
            { "nailFan", 1f },
            { "orbs", 1f },
        };
        public Dictionary<string, float> firstPhaseWeights = new Dictionary<string, float>(firstPhaseDefaults);
        public Dictionary<string, float> platformPhaseWeights = new Dictionary<string, float>(platformPhaseDefaults);

        public AbsRadConfigurableAttacks() : base("AbsRad Configurable Attacks") { 
            instance = this;
        }

        public override void Initialize() {
            USceneManager.activeSceneChanged += InitiateRadiancePolling;

            topMenuPanel = ((DebugMod.Canvas.CanvasPanel) typeof(DebugMod.TopMenu).GetField("panel", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));

            DebugMod.DebugMod.AddTopMenuContent(
                MenuName: "",
                ButtonList: new List<TopMenuButton>()
            ); // Accounts for top menu display glitch

            DebugMod.DebugMod.AddTopMenuContent(
                MenuName: "AR Start",
                ButtonList: new List<TopMenuButton>() {
                    new AttackConfigButton(
                        name: firstPhaseBtnNames["nailSweepTop"],
                        buttonText: GetButtonText("nailSweepTop"),
                        clickedFunction: _ => ApplyChange("nailSweepTop")
                    ),
                    new AttackConfigButton(
                        name: firstPhaseBtnNames["nailSweepLeft"],
                        buttonText: GetButtonText("nailSweepLeft"),
                        clickedFunction: _ => ApplyChange("nailSweepLeft")
                    ),
                    new AttackConfigButton(
                        name: firstPhaseBtnNames["nailSweepRight"],
                        buttonText: GetButtonText("nailSweepRight"),
                        clickedFunction: _ => ApplyChange("nailSweepRight")
                    ),
                    new AttackConfigButton(
                        name: firstPhaseBtnNames["eyeBeams"],
                        buttonText: GetButtonText("eyeBeams"),
                        clickedFunction: _ => ApplyChange("eyeBeams")
                    ),
                    new AttackConfigButton(
                        name: firstPhaseBtnNames["beamSweepLeft"],
                        buttonText: GetButtonText("beamSweepLeft"),
                        clickedFunction: _ => ApplyChange("beamSweepLeft")
                    ),
                    new AttackConfigButton(
                        name: firstPhaseBtnNames["beamSweepRight"],
                        buttonText: GetButtonText("beamSweepRight"),
                        clickedFunction: _ => ApplyChange("beamSweepRight")
                    ),
                    new AttackConfigButton(
                        name: firstPhaseBtnNames["nailFan"],
                        buttonText: GetButtonText("nailFan"),
                        clickedFunction: _ => ApplyChange("nailFan")
                    ),
                    new AttackConfigButton(
                        name: firstPhaseBtnNames["orbs"],
                        buttonText: GetButtonText("orbs"),
                        clickedFunction: _ => ApplyChange("orbs")
                    ),
                    new AttackConfigButton(
                        name: "firstPhaseReset",
                        buttonText: "Reset To Defaults",
                        clickedFunction: _ => ResetFirstPhases()
                    ),
                }
            );

            DebugMod.DebugMod.AddTopMenuContent(
                MenuName: "AR Plats",
                ButtonList: new List<TopMenuButton>() {
                    new AttackConfigButton(
                        name: platformPhaseBtnNames["nailSweep"],
                        buttonText: GetButtonText("nailSweep", false),
                        clickedFunction: _ => ApplyChange("nailSweep", false)
                    ),
                    new AttackConfigButton(
                        name: platformPhaseBtnNames["eyeBeams"],
                        buttonText: GetButtonText("eyeBeams", false),
                        clickedFunction: _ => ApplyChange("eyeBeams", false)
                    ),
                    new AttackConfigButton(
                        name: platformPhaseBtnNames["beamSweepLeft"],
                        buttonText: GetButtonText("beamSweepLeft", false),
                        clickedFunction: _ => ApplyChange("beamSweepLeft", false)
                    ),
                    new AttackConfigButton(
                        name: platformPhaseBtnNames["beamSweepRight"],
                        buttonText: GetButtonText("beamSweepRight", false),
                        clickedFunction: _ => ApplyChange("beamSweepRight", false)
                    ),
                    new AttackConfigButton(
                        name: platformPhaseBtnNames["nailFan"],
                        buttonText: GetButtonText("nailFan", false),
                        clickedFunction: _ => ApplyChange("nailFan", false)
                    ),
                    new AttackConfigButton(
                        name: platformPhaseBtnNames["orbs"],
                        buttonText: GetButtonText("orbs", false),
                        clickedFunction: _ => ApplyChange("orbs", false)
                    ),
                    new AttackConfigButton(
                        name: "platformPhaseReset",
                        buttonText: "Reset To Defaults",
                        clickedFunction: _ => ResetPlatsPhase()
                    ),
                }
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
            }
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public override int LoadPriority() => 2; // 1.4 modding api default is 1 instead of 0 :/

        private string GetButtonText(string key, bool firstPhase = true) {
            return string.Format("{0}: {1:0.##}", btnLabels[key], firstPhase ? firstPhaseWeights[key] : platformPhaseWeights[key]);
        }

        private void ResetFirstPhases() {
            firstPhaseWeights = new Dictionary<string, float>(firstPhaseDefaults);
            foreach (var key in firstPhaseDefaults.Keys) {
                topMenuPanel.GetButton(firstPhaseBtnNames[key], "AR Start").UpdateText(GetButtonText(key));
            }
            UpdateWeightsFSM();
            RemoveFirstPhasesAttackRepititionCap();
        }

        private void ResetPlatsPhase() {
            platformPhaseWeights = new Dictionary<string, float>(platformPhaseDefaults);
            foreach (var key in platformPhaseDefaults.Keys) {
                topMenuPanel.GetButton(platformPhaseBtnNames[key], "AR Plats").UpdateText(GetButtonText(key, false));
            }
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
                topMenuPanel.GetButton(firstPhaseBtnNames[key], "AR Start").UpdateText(GetButtonText(key));
            } else {
                platformPhaseWeights[key] += 0.25f;
                if (platformPhaseWeights[key] > 1f) {
                    platformPhaseWeights[key] = 0f;
                }
                topMenuPanel.GetButton(platformPhaseBtnNames[key], "AR Plats").UpdateText(GetButtonText(key, false));
            }
            UpdateWeightsFSM();
            CheckRepititionCap();
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
}