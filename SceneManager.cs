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
		static List<Scene> sceneList;
		static int playingScene;
		static Game gameContext;
		
		static SceneManager()
		{
			sceneList = new List<Scene>();
			playingScene = -1;
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
			get { return sceneList[playingScene]; }
		}
		
		public static bool LoadScene(int index)
		{
			if(sceneList.Count == 0) 
			{
				//ExitGame();
				return false;
			}
			if(playingScene != -1) {
				sceneList.FindLast(x => x.Index == playingScene).Stop();
			}
			playingScene = index;
			sceneList.FindLast(x => x.Index == playingScene).Start();
			return true;
		}
		
		public static void ExitGame()
		{
			gameContext.Exit();
		}
	}
}
