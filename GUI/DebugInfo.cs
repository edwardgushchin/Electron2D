/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using System.Timers;
using Electron2D.Graphics;
using System.Collections.Generic;
using Electron2D.Binding.SDL;

namespace Electron2D.GUI 
{
    internal class DebugInfo
    {
        Label titleLabel, resolutionLabel, vsyncLabel, smoothLabel,
		resizeLabel, fpsLabel, sceneNameLabel, 
		fullscreenLabel, splitLabel, fontObjectsLabel,
		guiObjectsLabel, gameObjectsLabel, allObjectsLabel,
		mposLabel, split2Label;

        List<GUIObject> obj;

        internal DebugInfo()
        {
            Debug.Log("Initializing the debug information Interface...", Debug.Sender.Main);
            obj = new List<GUIObject>();       
            
            CreateLabels();
			PositionLabels();

            Debug.Log("Debug information interface initialized.", Debug.Sender.Main);
        }

        internal List<GUIObject> ObjectsList
        {
            get { return obj; }
        }

        void CreateLabels()
		{
			var font = Kernel.ResourceManager.LoadFont(@"Resources\Fonts\consola.ttf", 12);
			font.Color = new Color(0, 0, 0);
			
			titleLabel = new Label(font, "Title");
			obj.Add(titleLabel);
			
			resolutionLabel = new Label(font, "Resolution");
			obj.Add(resolutionLabel);

			fullscreenLabel = new Label(font, "Fullscreen");
			obj.Add(fullscreenLabel);

			vsyncLabel = new Label(font, "V-Sync");
			obj.Add(vsyncLabel);

			smoothLabel = new Label(font, "Smoothing");
			obj.Add(smoothLabel);

			fpsLabel = new Label(font, "Frame Per Second");
			obj.Add(fpsLabel);

			mposLabel = new Label(font, "Mouse Position");
			obj.Add(mposLabel);

			resizeLabel = new Label(font, "Resizeble");
			obj.Add(resizeLabel);

			splitLabel = new Label(font, "Split");
			obj.Add(splitLabel);

			split2Label = new Label(font, "Split");
			obj.Add(split2Label);

			sceneNameLabel = new Label(font, "Scene");
			obj.Add(sceneNameLabel);

			fontObjectsLabel = new Label(font, "Font cache");
			obj.Add(fontObjectsLabel);

			guiObjectsLabel = new Label(font, "GUI objects");
			obj.Add(guiObjectsLabel);

			gameObjectsLabel = new Label(font, "Game objects");
			obj.Add(gameObjectsLabel);

			allObjectsLabel = new Label(font, "All objects");
			obj.Add(allObjectsLabel);
		}


        void PositionLabels()
		{
			int padding = 10;
			int padding_bottom = 20;

			titleLabel.RectTransform.Top = padding;
			titleLabel.RectTransform.Left = padding;

			resolutionLabel.RectTransform.Top = titleLabel.RectTransform.Top + padding_bottom;
			resolutionLabel.RectTransform.Left = padding;

			fullscreenLabel.RectTransform.Top = resolutionLabel.RectTransform.Top + padding_bottom;
			fullscreenLabel.RectTransform.Left = padding;

			vsyncLabel.RectTransform.Top = fullscreenLabel.RectTransform.Top + padding_bottom;
			vsyncLabel.RectTransform.Left = padding;

			smoothLabel.RectTransform.Top = vsyncLabel.RectTransform.Top + padding_bottom;
			smoothLabel.RectTransform.Left = padding;

			resizeLabel.RectTransform.Top = smoothLabel.RectTransform.Top + padding_bottom;
			resizeLabel.RectTransform.Left = padding;

			splitLabel.RectTransform.Top = resizeLabel.RectTransform.Top + padding_bottom;
			splitLabel.RectTransform.Left = padding;

			sceneNameLabel.RectTransform.Top = splitLabel.RectTransform.Top + padding_bottom;
			sceneNameLabel.RectTransform.Left = padding;

			fpsLabel.RectTransform.Top = sceneNameLabel.RectTransform.Top + padding_bottom;
			fpsLabel.RectTransform.Left = padding;

			mposLabel.RectTransform.Top = fpsLabel.RectTransform.Top + padding_bottom;
			mposLabel.RectTransform.Left = padding;

			split2Label.RectTransform.Top = mposLabel.RectTransform.Top + padding_bottom;
			split2Label.RectTransform.Left = padding;
			
			fontObjectsLabel.RectTransform.Top = split2Label.RectTransform.Top + padding_bottom;
			fontObjectsLabel.RectTransform.Left = padding;

			guiObjectsLabel.RectTransform.Top = fontObjectsLabel.RectTransform.Top + padding_bottom;
			guiObjectsLabel.RectTransform.Left = padding;

			gameObjectsLabel.RectTransform.Top = guiObjectsLabel.RectTransform.Top + padding_bottom;
			gameObjectsLabel.RectTransform.Left = padding;

			allObjectsLabel.RectTransform.Top = gameObjectsLabel.RectTransform.Top + padding_bottom;
			allObjectsLabel.RectTransform.Left = padding;
		}

        internal void Update(double deltaTime, int guiObjectsCount, int gameObjectsCount, int fontCacheCount)
		{
			titleLabel.Text= $"Title: {Kernel.Settings.Title}";
			resolutionLabel.Text = $"Resolution: {Kernel.Settings.Resolution.ToString()}";
			fullscreenLabel.Text = $"Fullscreen: {Kernel.Settings.Fullscreen}";
			vsyncLabel.Text = $"V-Sync: {Kernel.Settings.VSinc}";
			smoothLabel.Text = $"Smoothing: {Kernel.Settings.Smoothing}";
			resizeLabel.Text = $"Resizeble: {Kernel.Settings.Resizeble}";

			splitLabel.Text = "===============================";

			sceneNameLabel.Text = $"Scene: {SceneManager.GetCurrentScene.Name}";
			fpsLabel.Text = $"Frame Per Second: {((int)(1.0f/deltaTime)).ToString()}";
			mposLabel.Text = $"Mouse position: ({Input.MousePosition.X}; {Input.MousePosition.Y})";
			
			split2Label.Text = "===============================";

			fontObjectsLabel.Text = $"Font cache: {fontCacheCount} in memory";
			guiObjectsLabel.Text = $"GUI objects: {guiObjectsCount} in scene";
			gameObjectsLabel.Text = $"Game objects: {gameObjectsCount} in scene";
			allObjectsLabel.Text = $"All objects: {guiObjectsCount + gameObjectsCount} in scene";
		}
    }
}