﻿
using System.Drawing;
using ShapeEngine.Lib;
using System.Numerics;
using System.Text.RegularExpressions;
using ShapeEngine.Core.Structs;
using ShapeEngine.Core.Shapes;
using ShapeEngine.Input;
using ShapeEngine.Text;

namespace Examples.Scenes.ExampleScenes
{
    public class TextWrapEmphasisExample : TextExampleScene
    {

        private class MouseDetection : IMouseDetection
        {
            public Vector2 MousePos = new();
            private Emphasis mouseEmphasisBleed = new(new ED_Block(), new (System.Drawing.Color.Chartreuse), new (System.Drawing.Color.Black));
            private Emphasis mouseEmphasisRupture = new(new ED_Block(), new (System.Drawing.Color.Aqua), new (System.Drawing.Color.Black));
            private Emphasis mouseEmphasisIncreased = new(new ED_Block(), new (System.Drawing.Color.OrangeRed), new (System.Drawing.Color.Black));
            private Emphasis mouseEmphasisAdded = new(new ED_Block(), new (System.Drawing.Color.Violet), new (System.Drawing.Color.Black));
            private Emphasis mouseEmphasisTime = new(new ED_Block(), new (System.Drawing.Color.DeepSkyBlue), new (System.Drawing.Color.Black));
            private Emphasis mouseEmphasisCaps = new(new ED_Block(), new (System.Drawing.Color.Plum), new (System.Drawing.Color.Black));
            private Emphasis mouseEmphasisSpecial = new(new ED_Block(), new (System.Drawing.Color.Fuchsia), new (System.Drawing.Color.Black));
            private Regex specialRegex = new("[\" _ : ! ?]");
            private Regex ruptureRegex = new("(rupture)|(Rupture)");
            private Regex bleedRegex = new("(bleed)|(Bleed)");
            private Regex increaseRegex = new("(increased)|(Increased)");
            private Regex addedRegex = new("(added)|(Added)");
            private Regex timeRegex = new("(\\d)|(sec)|(seconds)");
            private Regex capsRegex = new("[A-Z]+$");
            public Vector2 GetMousePosition() => MousePos;
            public Emphasis? OnMouseEntered(string curWord, string completeWord, Rect rect)
            {
                if (ruptureRegex.IsMatch(completeWord)) return mouseEmphasisRupture;
                if (bleedRegex.IsMatch(completeWord)) return mouseEmphasisBleed;
                if (increaseRegex.IsMatch(completeWord)) return mouseEmphasisIncreased;
                if (addedRegex.IsMatch(completeWord)) return mouseEmphasisAdded;
                if (timeRegex.IsMatch(completeWord)) return mouseEmphasisTime;
                if (capsRegex.IsMatch(completeWord)) return mouseEmphasisCaps;
                if (specialRegex.IsMatch(completeWord)) return mouseEmphasisSpecial;
                return null;
            }
        }

        //string text = "Damaging an enemy with Rupture creates a pool that does Bleed damage over 6 seconds. Enemies in the pool take 10% increased Bleed damage.";
        int lineSpacing = 0;
        private const int lineSpacingIncrement = 1;
        private const int maxLineSpacing = 15;

        int fontSpacing = 0;
        private const int fontSpacingIncrement = 1;
        private const int maxFontSpacing = 15;

        // private const int fontSizeBase = 30;
        // private const int fontSizeIncrement = 5;
        // private const int maxFontSize = 90;
        // int fontSize = fontSizeBase + ((maxFontSize - fontSizeBase) / 2);
        
        
        bool wrapModeChar = true;
        // bool autoSize = false;

        private TextEmphasisBox textEmphasisBox;
        private MouseDetection mouseDetection = new();
        
        private readonly InputAction iaChangeFontSpacing;
        private readonly InputAction iaChangeLineSpacing;
        // private readonly InputAction iaChangeFontSize;
        
        // private readonly InputAction iaToggleAutoSize;
        private readonly InputAction iaToggleWrapMode;
        public TextWrapEmphasisExample() : base()
        {
            Title = "Text Wrap Multi Color Example";
            TextInputBox.EmptyText = "Enter Text...";
            TextInputBox.SetEnteredText("Damaging an enemy with Rupture creates a pool that does Bleed damage over 6 seconds. Enemies in the pool take 10% increased Bleed damage. ( Keywords: CAPS increased added 25% 5sec !a ?b _c d: )");
            
            var changeFontSpacingKB = new InputTypeKeyboardButton(ShapeKeyboardButton.S);
            var changeFontSpacingGP = new InputTypeGamepadButton(ShapeGamepadButton.LEFT_FACE_DOWN);
            iaChangeFontSpacing = new(accessTagTextBox,changeFontSpacingGP, changeFontSpacingKB);
            
            var changeLineSpacingKB = new InputTypeKeyboardButton(ShapeKeyboardButton.W);
            var changeLineSpacingGP = new InputTypeGamepadButton(ShapeGamepadButton.LEFT_FACE_UP);
            iaChangeLineSpacing = new(accessTagTextBox,changeLineSpacingGP, changeLineSpacingKB);
            
            // var changeFontSizeKB = new InputTypeKeyboardButton(ShapeKeyboardButton.D);
            // var changeFontSizeGP = new InputTypeGamepadButton(ShapeGamepadButton.LEFT_FACE_LEFT);
            // iaChangeFontSize = new(accessTagTextBox,changeFontSizeGP, changeFontSizeKB);
            //
            // var toggleAutoSizeKB = new InputTypeKeyboardButton(ShapeKeyboardButton.Q);
            // var toggleAutoSizeGP = new InputTypeGamepadButton(ShapeGamepadButton.LEFT_TRIGGER_TOP);
            // iaToggleAutoSize = new(accessTagTextBox,toggleAutoSizeKB, toggleAutoSizeGP);
            
            var toggleWrapModeKB = new InputTypeKeyboardButton(ShapeKeyboardButton.E);
            var toggleWrapModeGP = new InputTypeGamepadButton(ShapeGamepadButton.RIGHT_TRIGGER_TOP);
            iaToggleWrapMode = new(accessTagTextBox,toggleWrapModeKB, toggleWrapModeGP);
            
            inputActions.Add(iaChangeFontSpacing);
            inputActions.Add(iaChangeLineSpacing);
            // inputActions.Add(iaChangeFontSize);
            // inputActions.Add(iaToggleAutoSize);
            inputActions.Add(iaToggleWrapMode);

            // var tf = new TextFont(textFont.Font, 0f, 0f, WHITE);
            textEmphasisBox = new(textFont);
            textEmphasisBox.Caret = new(-1, new(Color.OrangeRed));
            var emphasis1 = new Emphasis(new ED_Block(), new(Color.IndianRed), new(Color.Black));
            var emphasis2 = new Emphasis(new ED_Block(), new(Color.CornflowerBlue), new(Color.Black));
            var emphasis3 = new Emphasis(new ED_Transparent(), new(Color.Black), new(Color.Goldenrod));
            var emphasis4 = new Emphasis(new ED_Transparent(), new(Color.Black), new(Color.ForestGreen));
            // var textEmphasis1 = new TextEmphasis(emphasis1, "(rupture)|(bleed)|(Rupture)|(Bleed)|[\" _ :]");
            var textEmphasis1 = new TextEmphasis(emphasis1, ShapeRegex.MatchWords("rupture", "Rupture", "bleed", "Bleed","\"", "_", ":"));
            var textEmphasis2 = new TextEmphasis(emphasis2, ShapeRegex.MatchWords("increased", "added"));
            var textEmphasis3 = new TextEmphasis(emphasis3, ShapeRegex.Combine(ShapeRegex.MatchAnyDigit(), ShapeRegex.MatchWords("sec", "seconds")));
            // var textEmphasis3 = new TextEmphasis(emphasis3, "(\\d)|(sec)|(seconds)");
            var textEmphasis4 = new TextEmphasis(emphasis4, "[A-Z]+$");
            textEmphasisBox.Emphases.Add(textEmphasis1);
            textEmphasisBox.Emphases.Add(textEmphasis2);
            textEmphasisBox.Emphases.Add(textEmphasis3);
            textEmphasisBox.Emphases.Add(textEmphasis4);

            mouseDetection = new MouseDetection();
            textEmphasisBox.TextFont.MouseDetection = mouseDetection;

            TextFont.EmphasisRectMargins = new(0.05f, 0f, 0.05f, 0f);
            // TextBlock.FontSizeModifier = 2f;

        }

        

        protected override void OnUpdateExample(GameTime time, ScreenInfo game, ScreenInfo ui)
        {
            mouseDetection.MousePos = ui.MousePos;
            
            base.OnUpdateExample(time, game, ui);
        }

        protected override void UpdateExampleTextEntryInactive(float dt, ScreenInfo game, ScreenInfo ui)
        {
            textEmphasisBox.TextFont.Font = textFont.Font;// font;// textBlock.TextFont.ChangeFont(font);
            textEmphasisBox.Caret.Index = -1;
        }

        protected override void UpdateExampleTextEntryActive(float dt, ScreenInfo game, ScreenInfo ui)
        {
            var index = TextInputBox.CaretVisible ? TextInputBox.CaretIndex : -1;
            textEmphasisBox.Caret.Index = index;
            // textBlock.Caret = new(index, 5f, RED);
        }

        protected override void HandleInputTextEntryInactive(float dt, Vector2 mousePosGame, Vector2 mousePosUI)
        {
            if (iaToggleWrapMode.State.Pressed) wrapModeChar = !wrapModeChar;

            // if (iaToggleAutoSize.State.Pressed) autoSize = !autoSize;
            
            // if (!autoSize && iaChangeFontSize.State.Pressed) ChangeFontSize();

            if (iaChangeFontSpacing.State.Pressed) ChangeFontSpacing();

            if (iaChangeLineSpacing.State.Pressed) ChangeLineSpacing();
        }

        protected override void DrawText(Rect rect)
        {
            var text = TextInputBox.Text;
            textEmphasisBox.Draw(text, rect, new(0), GAMELOOP.UIScreenInfo.MousePos, wrapModeChar ? TextWrapType.Char : TextWrapType.Word);
            // if (autoSize)
            // {
            //     if (wrapModeChar)
            //     {
            //         
            //         font.DrawTextWrappedChar(text, rect, fontSpacing, baseEmphasis, skill, bleed, numbers);
            //     }
            //     else
            //     {
            //         font.DrawTextWrappedWord(text, rect, fontSpacing, baseEmphasis, skill, bleed, numbers);
            //     }
            // }
            // else
            // {
            //     if (wrapModeChar)
            //     {
            //         font.DrawTextWrappedChar(text, rect, fontSize, fontSpacing, lineSpacing, baseEmphasis, skill, bleed, numbers);
            //     }
            //     else
            //     {
            //         font.DrawTextWrappedWord(text, rect, fontSize, fontSpacing, lineSpacing, baseEmphasis, skill, bleed, numbers);
            //     }
            // }
        }

        protected override void DrawTextEntry(Rect rect)
        {
            var text = TextInputBox.Text;
            textEmphasisBox.Draw(text, rect, new(0.5f, 0.5f),GAMELOOP.UIScreenInfo.MousePos, wrapModeChar ? TextWrapType.Char : TextWrapType.Word);
            // if (autoSize)
            // {
            //     if (wrapModeChar)
            //     {
            //         
            //         font.DrawTextWrappedChar(text, rect, fontSpacing, baseEmphasis, skill, bleed, numbers);
            //     }
            //     else
            //     {
            //         font.DrawTextWrappedWord(text, rect, fontSpacing, baseEmphasis, skill, bleed, numbers);
            //     }
            // }
            // else
            // {
            //     if (wrapModeChar)
            //     {
            //         font.DrawTextWrappedChar(text, rect, fontSize, fontSpacing, lineSpacing, baseEmphasis, skill, bleed, numbers);
            //     }
            //     else
            //     {
            //         font.DrawTextWrappedWord(text, rect, fontSize, fontSpacing, lineSpacing, baseEmphasis, skill, bleed, numbers);
            //     }
            // }
            
            // if(textBox.CaretVisible)
                // font.DrawCaret(textBox.Text, rect, fontSize, fontSpacing, new(), textBox.CaretIndex, 5f, ColorHighlight2);
        }

        protected override void DrawInputDescriptionBottom(Rect rect)
        {
            var curInputDeviceNoMouse = ShapeInput.CurrentInputDeviceTypeNoMouse;
            
            string fontSpacingText = iaChangeFontSpacing.GetInputTypeDescription(curInputDeviceNoMouse, true, 1, false);
            string lineSpacingText = iaChangeLineSpacing.GetInputTypeDescription(curInputDeviceNoMouse, true, 1, false);
            string wrapModeText = iaToggleWrapMode.GetInputTypeDescription(curInputDeviceNoMouse, true, 1, false);
            string textWrapMode = wrapModeChar ? "Char" : "Word";

            string text = $"Font Spacing {fontSpacingText} ({fontSpacing}) | Line Spacing {lineSpacingText} ({lineSpacing}) | Wrap Mode {wrapModeText} ({textWrapMode}))";
            
            textFont.FontSpacing = 4f;
            textFont.ColorRgba = Colors.Light;
            textFont.DrawTextWrapNone(text, rect, new(0.5f));
            // font.DrawText(text, rect, 4f, new Vector2(0.5f, 0.5f), ColorLight);
        }

        
       
        private void ChangeLineSpacing()
        {
            lineSpacing += lineSpacingIncrement;
            if (lineSpacing < 0) lineSpacing = maxLineSpacing;
            else if (lineSpacing > maxLineSpacing) lineSpacing = 0;
            textEmphasisBox.TextFont.LineSpacing = lineSpacing;
        }
        private void ChangeFontSpacing()
        {
            fontSpacing += fontSpacingIncrement;
            if (fontSpacing < 0) fontSpacing = maxFontSpacing;
            else if (fontSpacing > maxFontSpacing) fontSpacing = 0;
            textEmphasisBox.TextFont.FontSpacing = fontSpacing;
        }
        // private void ChangeFontSize()
        // {
        //     fontSize += fontSizeIncrement;
        //     if (fontSize < fontSizeBase) fontSize = maxFontSize;
        //     else if (fontSize > maxFontSize) fontSize = fontSizeBase;
        // }

        
    }

}
