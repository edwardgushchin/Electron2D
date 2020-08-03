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

		public static void NewLine()
		{
			Console.WriteLine();
		}
	}
}