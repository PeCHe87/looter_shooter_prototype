using Fusion;
using FusionExamples.FusionHelpers;
using FusionExamples.UIHelpers;
using Tanknarok.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// App entry point and main UI flow management.
	/// </summary>
	public class GameLauncher : MonoBehaviour
	{
		[System.Serializable]
		public enum TeamEnum { NONE = 0, BLUE = 1, RED = 2 }

		[SerializeField] private GameManager _gameManagerPrefab;
		[SerializeField] private Player _playerPrefab;
		[SerializeField] private TMP_InputField _room;
		[SerializeField] private TextMeshProUGUI _progress;
		[SerializeField] private Panel _uiCurtain;
		[SerializeField] private Panel _uiStart;
		[SerializeField] private Panel _uiProgress;
		[SerializeField] private Panel _uiRoom;
		[SerializeField] private GameObject _uiGame;
		[SerializeField] private GameObject _uiLevel;
		[SerializeField] private TMP_InputField _txtDisplayName;
		[SerializeField] private Button _btnTeamBlue;
		[SerializeField] private Image _iconBlue;
		[SerializeField] private Button _btnTeamRed;
		[SerializeField] private Image _iconRed;
		[SerializeField] private TextMeshProUGUI _txtError;

		private FusionLauncher.ConnectionStatus _status = FusionLauncher.ConnectionStatus.Disconnected;
		private GameMode _gameMode;
		private TeamEnum _team = TeamEnum.NONE;

		private void Awake()
		{
			DontDestroyOnLoad(this);

			_iconRed.enabled = false;
			_iconBlue.enabled = false;
			_btnTeamBlue.onClick.AddListener(() => { _team = TeamEnum.BLUE; _iconBlue.enabled = true; _iconRed.enabled = false; });
			_btnTeamRed.onClick.AddListener( () => { _team = TeamEnum.RED; _iconBlue.enabled = false; _iconRed.enabled = true; } );
		}

        private void OnDestroy()
        {
			_btnTeamBlue.onClick.RemoveAllListeners();
			_btnTeamRed.onClick.RemoveAllListeners();
		}

        private void Start()
		{
			OnConnectionStatusUpdate(null, FusionLauncher.ConnectionStatus.Disconnected, "");
		}

		private void Update()
		{
			if (_uiProgress.isShowing)
			{
				if (Input.GetKeyUp(KeyCode.Escape))
				{
					NetworkRunner runner = FindObjectOfType<NetworkRunner>();
					if (runner != null && !runner.IsShutdown)
					{
						// Calling with destroyGameObject false because we do this in the OnShutdown callback on FusionLauncher
						runner.Shutdown(false);
					}
				}
				UpdateUI();
			}
		}

		// What mode to play - Called from the start menu
		public void OnHostOptions()
		{
			SetGameMode(GameMode.Host);
		}

		public void OnJoinOptions()
		{
			SetGameMode(GameMode.Client);
		}

		public void OnSharedOptions()
		{
			SetGameMode(GameMode.Shared);
		}

		private void SetGameMode(GameMode gamemode)
		{
			_gameMode = gamemode;
			if (GateUI(_uiStart))
				_uiRoom.SetVisible(true);
		}

		public void OnEnterRoom()
		{
			var passControl = CheckRequiredInputs();

			if (!passControl) return;

			PlayerPrefs.SetString("playerDisplayName", _txtDisplayName.text.ToUpperInvariant());
			PlayerPrefs.SetInt("playerTeam", (int)_team);
			PlayerPrefs.Save();

			if (GateUI(_uiRoom))
			{
		    FusionLauncher launcher = FindObjectOfType<FusionLauncher>();
				if (launcher == null)
					launcher = new GameObject("Launcher").AddComponent<FusionLauncher>();

				LevelManager lm = FindObjectOfType<LevelManager>();
				lm.launcher = launcher;

				launcher.Launch(_gameMode, _room.text, lm, OnConnectionStatusUpdate, OnSpawnWorld, OnSpawnPlayer, OnDespawnPlayer);
			}
		}

		/// <summary>
		/// Call this method from button events to close the current UI panel and check the return value to decide
		/// if it's ok to proceed with handling the button events. Prevents double-actions and makes sure UI panels are closed. 
		/// </summary>
		/// <param name="ui">Currently visible UI that should be closed</param>
		/// <returns>True if UI is in fact visible and action should proceed</returns>
		private bool GateUI(Panel ui)
		{
			if (!ui.isShowing)
				return false;
			ui.SetVisible(false);
			return true;
		}

		private void OnConnectionStatusUpdate(NetworkRunner runner, FusionLauncher.ConnectionStatus status, string reason)
		{
			if (!this)
				return;

			Debug.Log(status);

			if (status != _status)
			{
				switch (status)
				{
					case FusionLauncher.ConnectionStatus.Disconnected:
						ErrorBox.Show("Disconnected!", reason, () => { });
						break;
					case FusionLauncher.ConnectionStatus.Failed:
						ErrorBox.Show("Error!", reason, () => { });
						break;
				}
			}

			_status = status;
			UpdateUI();
		}

		private void OnSpawnWorld(NetworkRunner runner)
		{
			Debug.Log("Spawning GameManager");
			runner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity, null, InitNetworkState);
			void InitNetworkState(NetworkRunner runner, NetworkObject world)
			{
				world.transform.parent = transform;
			}
		}

		private void OnSpawnPlayer(NetworkRunner runner, PlayerRef playerref)
		{
			if (GameManager.playState != GameManager.PlayState.LOBBY)
			{
				Debug.Log("Not Spawning Player - game has already started");
				return;
			}
			Debug.Log($"Spawning tank for player {playerref}");
			runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, playerref, InitNetworkState);
			void InitNetworkState(NetworkRunner runner, NetworkObject networkObject)
			{
				Player player = networkObject.gameObject.GetComponent<Player>();

				player.InitNetworkState(GameManager.MAX_LIVES);
			}
		}

		private void OnDespawnPlayer(NetworkRunner runner, PlayerRef playerref)
		{
			Debug.Log($"Despawning Player {playerref}");
			Player player = PlayerManager.Get(playerref);
			player.TriggerDespawn();
		}

		private void UpdateUI()
		{
			bool intro = false;
			bool progress = false;
			bool running = false;

			switch (_status)
			{
				case FusionLauncher.ConnectionStatus.Disconnected:
					_progress.text = "Disconnected!";
					intro = true;
					break;
				case FusionLauncher.ConnectionStatus.Failed:
					_progress.text = "Failed!";
					intro = true;
					break;
				case FusionLauncher.ConnectionStatus.Connecting:
					_progress.text = "Connecting";
					progress = true;
					break;
				case FusionLauncher.ConnectionStatus.Connected:
					_progress.text = "Connected";
					progress = true;
					break;
				case FusionLauncher.ConnectionStatus.Loading:
					_progress.text = "Loading";
					progress = true;
					break;
				case FusionLauncher.ConnectionStatus.Loaded:
					running = true;
					break;
			}

			_uiCurtain.SetVisible(!running);
			_uiStart.SetVisible(intro);
			_uiProgress.SetVisible(progress);
			_uiGame.SetActive(running);
			_uiLevel.Toggle(running);

			if (intro)
				MusicPlayer.instance.SetLowPassTranstionDirection( -1f);
		}

		private bool CheckRequiredInputs()
        {
			if (string.IsNullOrEmpty(_txtDisplayName.text))
            {
				_txtError.text = "Fill display name";
				_txtError.enabled = true;
				return false;
            }

			if (_team == TeamEnum.NONE)
			{
				_txtError.text = "Select a Team";
				_txtError.enabled = true;
				return false;
			}

			return true;
		}
	}
}