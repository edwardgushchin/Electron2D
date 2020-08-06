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
		private static readonly List<Scene> _sceneList;
		private static Game _gameContext;

		static SceneManager()
		{
			_sceneList = new List<Scene>();
			PlayingScene = -1;
			Debug.Log("Initialization of the scene manager subsystem completed successfully.", Debug.Sender.SceneManager);
		}

		internal static void SetGameContext(Game Game)
		{
			_gameContext = Game;
		}

		public static void AddScene(Scene scene)
		{
			_sceneList.Add(scene);
			scene.SetGame(_gameContext);
		}

        public static Scene GetCurrentScene
        {
            get
            {
                return _sceneList[PlayingScene];
            }
        }

        public static int PlayingScene { get; set; }

        public static bool LoadScene(int index)
		{
			if(_sceneList.Count == 0)
			{
				return false;
			}
			if(PlayingScene != -1) {
				_sceneList.FindLast(x => x.Index == PlayingScene).Stop();
			}
			PlayingScene = index;
			_sceneList.FindLast(x => x.Index == PlayingScene).Start();
			return true;
		}

		public static void ExitGame()
		{
			_gameContext.Exit();
		}
	}
}
