using System.Collections.Generic;
using Modding;
using ModCommon.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using DebugMod;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AbsRadConfigurableAttacks {
    public class AbsRadConfigurableAttacks : Mod {
        public static AbsRadConfigurableAttacks instance;
        private PlayMakerFSM attackChoicesFSM = null;
        public static Dictionary<string, float> firstPhaseDefaults = new Dictionary<string, float>() {
            { "nailSweepRight", 0.5f },
            { "nailSweepLeft", 0.5f },
            { "nailSweepTop", 0.75f },
            { "eyeBeams", 1f },
            { "beamSweepLeft", 0.75f },
            { "beamSweepRight", 0.5f },
            { "nailFan", 1f },
            { "orbs", 1f },
        };
        public static Dictionary<string, float> platformPhaseDefaults = new Dictionary<string, float>(){
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

            DebugMod.DebugMod.AddTopMenuContent(
                MenuName: "AbsRad First Phases",
                ButtonList: new List<TopMenuButton>() {
                    new TextButton(
                        buttonText: $"Sword Rain: {firstPhaseWeights["nailSweepTop"]}",
                        clickedFunction: _ => ApplyChange("nailSweepTop")
                    ),
                    new TextButton(
                        buttonText: $"Nail Sweep Left: {firstPhaseWeights["nailSweepLeft"]}",
                        clickedFunction: _ => ApplyChange("nailSweepLeft")
                    ),
                    new TextButton(
                        buttonText: $"Nail Sweep Right: {firstPhaseWeights["nailSweepRight"]}",
                        clickedFunction: _ => ApplyChange("nailSweepRight")
                    ),
                    new TextButton(
                        buttonText: $"Beam Burst: {firstPhaseWeights["eyeBeams"]}",
                        clickedFunction: _ => ApplyChange("eyeBeams")
                    ),
                    new TextButton(
                        buttonText: $"Beam Sweep Left: {firstPhaseWeights["beamSweepLeft"]}",
                        clickedFunction: _ => ApplyChange("beamSweepLeft")
                    ),
                    new TextButton(
                        buttonText: $"Beam Sweep Right: {firstPhaseWeights["beamSweepRight"]}",
                        clickedFunction: _ => ApplyChange("beamSweepRight")
                    ),
                    new TextButton(
                        buttonText: $"Sword Burst: {firstPhaseWeights["nailFan"]}",
                        clickedFunction: _ => ApplyChange("nailFan")
                    ),
                    new TextButton(
                        buttonText: $"Orb Barrage: {firstPhaseWeights["orbs"]}",
                        clickedFunction: _ => ApplyChange("orbs")
                    ),
                    new TextButton(
                        buttonText: "Reset To Defaults",
                        clickedFunction: _ => ResetFirstPhases()
                    ),
                }
            );

            DebugMod.DebugMod.AddTopMenuContent(
                MenuName: "AbsRad Platform Phase",
                ButtonList: new List<TopMenuButton>() {
                    new TextButton(
                        buttonText: $"Nail Sweep: {platformPhaseWeights["nailSweep"]}",
                        clickedFunction: _ => ApplyChange("nailSweep", false)
                    ),
                    new TextButton(
                        buttonText: $"Beam Burst: {platformPhaseWeights["eyeBeams"]}",
                        clickedFunction: _ => ApplyChange("eyeBeams", false)
                    ),
                    new TextButton(
                        buttonText: $"Beam Sweep Left: {platformPhaseWeights["beamSweepLeft"]}",
                        clickedFunction: _ => ApplyChange("beamSweepLeft", false)
                    ),
                    new TextButton(
                        buttonText: $"Beam Sweep Right: {platformPhaseWeights["beamSweepRight"]}",
                        clickedFunction: _ => ApplyChange("beamSweepRight", false)
                    ),
                    new TextButton(
                        buttonText: $"Sword Burst: {platformPhaseWeights["nailFan"]}",
                        clickedFunction: _ => ApplyChange("nailFan", false)
                    ),
                    new TextButton(
                        buttonText: $"Orb Barrage: {platformPhaseWeights["orbs"]}",
                        clickedFunction: _ => ApplyChange("orbs", false)
                    ),
                    new TextButton(
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
            } else {
                ModHooks.Instance.HeroUpdateHook -= RadiancePoll;
            }
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

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

        private void ApplyChange(string key, bool firstPhase = true) {
            if (firstPhase) {
                firstPhaseWeights[key] += 0.25f;
                if (firstPhaseWeights[key] > 1f) {
                    firstPhaseWeights[key] = 0f;
                }
            } else {
                platformPhaseWeights[key] += 0.25f;
                if (platformPhaseWeights[key] > 1f) {
                    platformPhaseWeights[key] = 0f;
                }
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