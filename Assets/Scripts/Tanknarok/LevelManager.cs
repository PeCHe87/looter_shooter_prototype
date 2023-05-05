using System.Collections;
using System.Collections.Generic;
using Fusion;
using FusionExamples.FusionHelpers;
using FusionExamples.Tanknarok.Gameplay;
using FusionExamples.Tanknarok.Items;
using UnityEngine;
using UnityEngine.SceneManagement;
using static FusionExamples.Tanknarok.GameLauncher;
using Random = UnityEngine.Random;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// The LevelManager controls the map - keeps track of spawn points for players and powerups, and spawns powerups at regular intervals.
	/// </summary>
	public class LevelManager : NetworkSceneManagerBase
	{
        #region Inspector

        [SerializeField] private int _lobby;
		[SerializeField] private int[] _levels;
		[SerializeField] private LevelBehaviour _currentLevel;
		[SerializeField] private CameraScreenFXBehaviour _transitionEffect;
		[SerializeField] private AudioEmitter _audioEmitter;
		[SerializeField] private Camera _camera = default;
		[SerializeField] private CatalogData _catalog = default;
		[SerializeField] private PlayerDeathLoot _playerDeathLoot = default;
		[SerializeField] private EnemiesSpawnerService _enemiesSpawnerService = default;
		[SerializeField] private float _spawnRadius = 10;
		[SerializeField] private UI_PlayerKillPlayerPanel _playerKillsPanel = default;
		[SerializeField] private Material _materialBlueTeam = default;
		[SerializeField] private Material _materialRedTeam = default;

		#endregion

		#region Private properties

		private Scene _loadedScene;
		private ScoreManager _scoreManager;
		private ReadyupManager _readyupManager;
		private CountdownManager _countdownManager;

        #endregion

        #region Public properties

        public FusionLauncher launcher { get; set; }
		public Camera Camera => _camera;
		public CatalogData Catalog => _catalog;
		public LevelBehaviour LevelBehavior => _currentLevel;
		public PlayerDeathLoot PlayerDeathLoot => _playerDeathLoot;
		public EnemiesSpawnerService EnemiesSpawnerService => _enemiesSpawnerService;
		public NetworkRunner LevelRunner => Runner;

        #endregion

        #region Unity events

        private void Awake()
		{
			_scoreManager = FindObjectOfType<ScoreManager>(true);
			_readyupManager = FindObjectOfType<ReadyupManager>(true);
			_countdownManager = FindObjectOfType<CountdownManager>(true);

			_countdownManager.Reset();
			_scoreManager.HideLobbyScore();
			_readyupManager.HideUI();
		}

        #endregion

        #region Public methods

        // Get a random level
        public int GetRandomLevelIndex()
		{
			int idx = Random.Range(0, _levels.Length);
			// Make sure it's not the same level again. This is partially because it's more fun to try different levels and partially because scene handling breaks if trying to load the same scene again.
			if (_levels[idx] == _loadedScene.buildIndex)
				idx = (idx + 1) % _levels.Length;
			return idx;
		}

		public SpawnPoint GetPlayerSpawnPoint(int playerID)
		{
			if (_currentLevel!=null)
				return _currentLevel.GetPlayerSpawnPoint(playerID);
			return null;
		}

		public Vector3 GetPlayerSpawnPoint(TeamEnum team)
		{
			var point = (team == TeamEnum.BLUE) ? _currentLevel.SpawnPointBlue.position : _currentLevel.SpawnPointRed.position;

			var position = Utils.GetPositionAroundPoint(point, _spawnRadius);

			return new Vector3(position.x, 0, position.y);
		}

		public void LoadLevel(int nextLevelIndex)
		{
			Runner.SetActiveScene(nextLevelIndex < 0 ? _lobby:_levels[nextLevelIndex]);
		}

		public void RefreshPlayerKills(string killer, string killed, Color teamColor)
        {
			_playerKillsPanel.ShowMessage(killer, killed, teamColor);
        }

		public Material GetPlayerTeamMaterial(TeamEnum team)
        {
			return (team == TeamEnum.BLUE) ? _materialBlueTeam : _materialRedTeam;
		}

		#endregion

		#region Protected methods

		protected override void Shutdown(NetworkRunner runner)
		{
			base.Shutdown(runner);
			_currentLevel = null;
			if(_loadedScene!=default)
				SceneManager.UnloadSceneAsync(_loadedScene);
			_loadedScene = default;
			PlayerManager.ResetPlayerManager();
		}
		
		protected override IEnumerator SwitchScene(SceneRef prevScene, SceneRef newScene, FinishedLoadingDelegate finished)
		{
			Debug.Log($"Switching Scene from {prevScene} to {newScene}");
			if (newScene <= 0)
			{
				finished(new List<NetworkObject>());
				yield break;
			}

			if (Runner.IsServer || Runner.IsSharedModeMasterClient)
				GameManager.playState = GameManager.PlayState.TRANSITION;

			int winner = GameManager.WinningPlayerIndex;

			if (prevScene > 0)
			{
				yield return new WaitForSeconds(1.0f);

				InputController.fetchInput = false;

				// Despawn players with a small delay between each one
				Debug.Log("De-spawning all tanks");
				for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
				{
					Debug.Log($"De-spawning tank {i}:{PlayerManager.allPlayers[i]}");
					PlayerManager.allPlayers[i].DespawnTank();
					yield return new WaitForSeconds(0.1f);
				}

				yield return new WaitForSeconds(1.5f - PlayerManager.allPlayers.Count * 0.1f);

				Debug.Log("Despawned all tanks");
				// Players have despawned

				if (winner != -1)
				{
					_scoreManager.UpdateScore(winner, PlayerManager.GetPlayerFromID(winner).score);
					yield return new WaitForSeconds(1.5f);
					_scoreManager.HideUiScoreAndReset(false);
				}
			}

			_transitionEffect.ToggleGlitch(true);
			_audioEmitter.Play();
			
			launcher.SetConnectionStatus( FusionLauncher.ConnectionStatus.Loading, "");

			_scoreManager.HideLobbyScore();

			yield return null;
			Debug.Log($"Start loading scene {newScene} in single peer mode");

			if (_loadedScene != default)
			{
				Debug.Log($"Unloading Scene {_loadedScene.buildIndex}");
				yield return SceneManager.UnloadSceneAsync(_loadedScene);
			}

			_loadedScene = default;
			Debug.Log($"Loading scene {newScene}");

			List<NetworkObject> sceneObjects = new List<NetworkObject>();
			if (newScene >= 0)
			{
				yield return SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);
				_loadedScene = SceneManager.GetSceneByBuildIndex(newScene);
				Debug.Log($"Loaded scene {newScene}: {_loadedScene}");
				sceneObjects = FindNetworkObjects(_loadedScene, disable: false);
			}

			// Delay one frame
			yield return null;

			launcher.SetConnectionStatus(FusionLauncher.ConnectionStatus.Loaded, "");
			
			// Activate the next level
			_currentLevel = FindObjectOfType<LevelBehaviour>();
			if(_currentLevel!=null)
				_currentLevel.Activate();
			MusicPlayer.instance.SetLowPassTranstionDirection( newScene>_lobby ? 1f : -1f);

			Debug.Log($"Switched Scene from {prevScene} to {newScene} - loaded {sceneObjects.Count} scene objects");
			finished(sceneObjects);

			StartCoroutine(SwitchScenePostFadeIn(prevScene, newScene, winner));
		}

        #endregion

        #region Private methods

        IEnumerator SwitchScenePostFadeIn(SceneRef prevScene, SceneRef newScene, int winner)
		{
			Debug.Log("SwitchScene post effect");

			if(newScene==_lobby)
				_readyupManager.ShowUI();
			else
				_readyupManager.HideUI();

	    yield return new WaitForSeconds(0.3f);

	    Debug.Log($"Stop glitching");
	    _transitionEffect.ToggleGlitch(false);
	    _audioEmitter.Stop();

	    if (winner>=0 && newScene == _lobby)
	    {
		    // Show lobby scores and reset the score ui.
		    _scoreManager.ShowLobbyScore(winner);
		    _scoreManager.HideUiScoreAndReset(true);
	    }

	    // Respawn with slight delay between each player
	    Debug.Log($"Respawning All Players");
	    for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
	    {
		    Player player = PlayerManager.allPlayers[i];
		    Debug.Log($"Respawning Player {i}:{player}");
		    player.Respawn(0);
		    yield return new WaitForSeconds(0.3f);
	    }

	    // Set state to playing level
	    if (_loadedScene.buildIndex == _lobby)
	    {
		    if(Runner.IsServer || Runner.IsSharedModeMasterClient)
					GameManager.playState = GameManager.PlayState.LOBBY;
		    InputController.fetchInput = true;
		    Debug.Log($"Switched Scene from {prevScene} to {newScene}");
	    }
	    else
	    {
		    StartCoroutine(_countdownManager.Countdown(() =>
		    {
			    // Set state to playing level
			    if (Runner != null && (Runner.IsServer || Runner.IsSharedModeMasterClient))
			    {
				    GameManager.WinningPlayerIndex = -1;
				    GameManager.playState = GameManager.PlayState.LEVEL;
			    }
			    // Enable inputs after countdow finishes
			    InputController.fetchInput = true;
			    Debug.Log($"Switched Scene from {prevScene} to {newScene}");
		    }));
	    }
		}

        #endregion
    }
}