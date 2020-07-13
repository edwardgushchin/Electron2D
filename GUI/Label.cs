/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using Electron2D.Graphics;

namespace Electron2D.GUI
{
    public class Label : GUIObject
    {
        private readonly Font labelFont;
        public Label(Font font, string text)
        {
            labelFont = font;
            Text = text;
            Color = font.Color;
            Size = font.Size;
            labelFont.Color = Color;

            if(labelFont == null)
            {
                Debug.Log($"Label \"{Text}\" failed loaded.", Debug.Sender.GUIObject, Debug.MessageStatus.Error);
            }

            Debug.Log($"Label \"{Text}\" loaded.", Debug.Sender.GUIObject);
        }

        public string Text { set; get; }

        public Color Color { set; get; }

        public int Size { set; get; }

        public override void Update(double deltaTime)
        {
            labelFont.Draw(Text, RectTransform, Color, Size);
        }
    }
}