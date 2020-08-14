/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

using Electron2D.Graphics;

namespace Electron2D
{
	public static class Debug
	{
		public enum MessageStatus {
			Success,
			Warning,
			Error,
			Log
		}

		public enum Sender {
			Main,
			Render,
			Physics,
			Event,
			SceneManager,
			Scene,
			Entity,
			GUIObject,
			ResourceManager,
			FontCache
		}

		public static void Message(string Message)
		{
			Console.WriteLine(Message);
		}

		private static string GetCurrentTime
		{
			get { return string.Format("[{0:hh:mm:ss:ff}]", DateTime.Now); }
		}

		public static void Log(string Message)
		{
            Console.Write($"{GetCurrentTime} ");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(Message);
			Console.ResetColor();
		}

		public static void Log(string Message, Sender Sender)
		{
			Console.Write($"{GetCurrentTime} [{Sender}] ");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(Message);
			Console.ResetColor();
		}

		public static void Log(string Message, MessageStatus Status)
		{
			Console.Write($"{GetCurrentTime} ");
			switch (Status) {
				case MessageStatus.Success:
					Console.ForegroundColor = ConsoleColor.Green;
					break;
				case MessageStatus.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
				case MessageStatus.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
			}
			Console.WriteLine(Message);
			Console.ResetColor();
		}

		public static void Log(string Message, Sender Sender, MessageStatus Status)
		{
			Console.Write($"{GetCurrentTime} [{Sender}] ");
			switch (Status) {
				case MessageStatus.Success:
					Console.ForegroundColor = ConsoleColor.Green;
					break;
				case MessageStatus.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
				case MessageStatus.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
			}
			Console.WriteLine(Message);
			Console.ResetColor();
		}

		public static void DrawText()
		{
		}

		public static void DrawPoint()
		{
		}

		public static void DrawPoints()
		{
		}

		public static void DrawLine()
		{
		}

		public static void DrawLines()
		{
		}

		public static void DrawRectangle()
		{
		}

		public static void DrawRectangles()
		{
		}

		public static void DrawGrid()
		{
			var color = SceneManager.GetCurrentScene.ClearColor;
            const int size = 100;

			Binding.SDL.SDL.SDL_SetRenderDrawColor(Game.RenderContext, 100, 100, 100, 0);

			for (int i = -size; i <= size; i++)
			{
				var px1 = Camera.MainCamera.ConvertWorldToScreen(new Point(i - Camera.MainCamera.Transform.Position.X, size - Camera.MainCamera.Transform.Position.Y));
				var py1 = Camera.MainCamera.ConvertWorldToScreen(new Point(i - Camera.MainCamera.Transform.Position.X, -size - Camera.MainCamera.Transform.Position.Y));

				var px2 = Camera.MainCamera.ConvertWorldToScreen(new Point(size - Camera.MainCamera.Transform.Position.X, i - Camera.MainCamera.Transform.Position.Y));
				var py2 = Camera.MainCamera.ConvertWorldToScreen(new Point(-size - Camera.MainCamera.Transform.Position.X, i - Camera.MainCamera.Transform.Position.Y));

				Binding.SDL.SDL.SDL_RenderDrawLineF(Game.RenderContext, (float)px1.X, (float)px1.Y, (float)py1.X, (float)py1.Y);
				Binding.SDL.SDL.SDL_RenderDrawLineF(Game.RenderContext, (float)px2.X, (float)px2.Y, (float)py2.X, (float)py2.Y);
			}

			var x1 = Camera.MainCamera.ConvertWorldToScreen(new Point(0 - Camera.MainCamera.Transform.Position.X, size - Camera.MainCamera.Transform.Position.Y));
			var y1 = Camera.MainCamera.ConvertWorldToScreen(new Point(0 - Camera.MainCamera.Transform.Position.X, -size - Camera.MainCamera.Transform.Position.Y));

			var x2 = Camera.MainCamera.ConvertWorldToScreen(new Point(size - Camera.MainCamera.Transform.Position.X, 0 - Camera.MainCamera.Transform.Position.Y));
			var y2 = Camera.MainCamera.ConvertWorldToScreen(new Point(-size - Camera.MainCamera.Transform.Position.X, 0 - Camera.MainCamera.Transform.Position.Y));

			Binding.SDL.SDL.SDL_SetRenderDrawColor(Game.RenderContext, 200, 200, 200, 0);
			Binding.SDL.SDL.SDL_RenderDrawLineF(Game.RenderContext, (float)x1.X, (float)x1.Y, (float)y1.X, (float)y1.Y);
			Binding.SDL.SDL.SDL_SetRenderDrawColor(Game.RenderContext, 200, 200, 200, 0);
			Binding.SDL.SDL.SDL_RenderDrawLineF(Game.RenderContext, (float)x2.X, (float)x2.Y, (float)y2.X, (float)y2.Y);
			Binding.SDL.SDL.SDL_SetRenderDrawColor(Game.RenderContext, color.R, color.G, color.B, color.A);
		}

		public static void NewLine()
		{
			Console.WriteLine();
		}
	}
}