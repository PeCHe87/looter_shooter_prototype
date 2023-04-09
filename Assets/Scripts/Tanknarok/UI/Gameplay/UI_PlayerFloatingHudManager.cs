
using System.Collections.Generic;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
    public class UI_PlayerFloatingHudManager : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Transform _parent;
        [SerializeField] private UI_PlayerFloatingHud _prefab;

        #endregion

        #region Private properties

        private Dictionary<int, UI_PlayerFloatingHud> _huds = new Dictionary<int, UI_PlayerFloatingHud>();

		#endregion

		#region Unity events

		private void Update()
		{
			if (GameManager.playState != GameManager.PlayState.LOBBY) return;

			var allPlayers = PlayerManager.allPlayers;

            for (int i = 0; i < allPlayers.Count; i++)
            {
				var player = allPlayers[i];

				if (!_huds.TryGetValue(player.playerID, out var indicator))
				{
					indicator = Instantiate(_prefab, _parent);

					_huds.Add(player.playerID, indicator);

					indicator.Refresh(player);

					player.InitFloatingHud(indicator);
				}
			}
		}

		#endregion
	}
}