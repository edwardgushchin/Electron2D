/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;

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
			var camera = SceneManager.GetCurrentScene.Camera;
			var size = new Graphics.Rect(100, 100);

			Binding.SDL.SDL.SDL_SetRenderDrawColor(Game.RenderContext, 100, 100, 100, 0);

			for (int i = -100; i <= 100; i++)
			{
				var p1 = camera.ConvertWorldToScreen(new Graphics.Point(i - camera.Transform.Position.X, size.Width - camera.Transform.Position.Y));
				var p2 = camera.ConvertWorldToScreen(new Graphics.Point(i - camera.Transform.Position.X, -size.Width - camera.Transform.Position.Y));
				var p3 = camera.ConvertWorldToScreen(new Graphics.Point(size.Width - camera.Transform.Position.X, i - camera.Transform.Position.Y));
				var p4 = camera.ConvertWorldToScreen(new Graphics.Point(-size.Width - camera.Transform.Position.X, i - camera.Transform.Position.Y));

				Binding.SDL.SDL.SDL_RenderDrawLineF(Game.RenderContext, (float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
				Binding.SDL.SDL.SDL_RenderDrawLineF(Game.RenderContext, (float)p3.X, (float)p3.Y, (float)p4.X, (float)p4.Y);
			}

			var cx1 = camera.ConvertWorldToScreen(new Graphics.Point(0 - camera.Transform.Position.X, size.Width - camera.Transform.Position.Y));
			var cy1 = camera.ConvertWorldToScreen(new Graphics.Point(0 - camera.Transform.Position.X, -size.Width - camera.Transform.Position.Y));
			var cx2 = camera.ConvertWorldToScreen(new Graphics.Point(size.Width - camera.Transform.Position.X, 0 - camera.Transform.Position.Y));
			var cy2 = camera.ConvertWorldToScreen(new Graphics.Point(-size.Width - camera.Transform.Position.X, 0 - camera.Transform.Position.Y));

			Binding.SDL.SDL.SDL_SetRenderDrawColor(Game.RenderContext, 200, 200, 200, 0);
			Binding.SDL.SDL.SDL_RenderDrawLineF(Game.RenderContext, (float)cx1.X, (float)cx1.Y, (float)cy1.X, (float)cy1.Y);
			Binding.SDL.SDL.SDL_SetRenderDrawColor(Game.RenderContext, 200, 200, 200, 0);
			Binding.SDL.SDL.SDL_RenderDrawLineF(Game.RenderContext, (float)cx2.X, (float)cx2.Y, (float)cy2.X, (float)cy2.Y);
			Binding.SDL.SDL.SDL_SetRenderDrawColor(Game.RenderContext, color.R, color.G, color.B, color.A);
		}

		public static void NewLine()
		{
			Console.WriteLine();
		}
	}
}