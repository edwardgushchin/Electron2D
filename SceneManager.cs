/*
  Copyright (c) 2019-2020 Edward Gushchin.
  Licensed under the Apache License, Version 2.0
*/

using System;
using System.Collections.Generic;

namespace Electron2D
{
	public static class SceneManager
	{
		private static readonly List<Scene> sceneList;
		private static Game gameContext;

		static SceneManager()
		{
			sceneList = new List<Scene>();
			PlayingScene = -1;
			Debug.Log("Initialization of the scene manager subsystem completed successfully.", Debug.Sender.SceneManager);
		}

		internal static void SetGameContext(Game Game)
		{
			gameContext = Game;
		}

		public static void AddScene(Scene scene)
		{
			sceneList.Add(scene);
			scene.SetGame(gameContext);
		}

		public static Scene GetCurrentScene
		{
			get { return sceneList[PlayingScene]; }
		}

        public static int PlayingScene { get; set; }

        public static bool LoadScene(int index)
		{
			if(sceneList.Count == 0)
			{
				//ExitGame();
				return false;
			}
			if(PlayingScene != -1) {
				sceneList.FindLast(x => x.Index == PlayingScene).Stop();
			}
			PlayingScene = index;
			sceneList.FindLast(x => x.Index == PlayingScene).Start();
			return true;
		}

		public static void ExitGame()
		{
			gameContext.Exit();
		}
	}
}
