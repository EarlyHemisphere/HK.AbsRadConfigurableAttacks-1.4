using UnityEngine;
using UnityEngine.Events;
using DebugMod;
using DebugMod.Canvas;

namespace AbsRadConfigurableAttacks {
    public class AttackConfigButton:TopMenuButton {
        public string ButtonText { get; }
        public string Name { get; }
        public AttackConfigButton(string name, string buttonText, UnityAction<string> clickedFunction) {
            Name = name;
            ButtonText = buttonText;
            ClickedFunction = clickedFunction;
        }
        public override void CreateButton(CanvasPanel panel){
            panel.AddButton(Name,
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