using UnityEngine;
using Fusion;

namespace FusionExamples.Tanknarok
{
	public class WeaponManager : NetworkBehaviour
	{
		public enum WeaponInstallationType
		{
			PRIMARY,
			SECONDARY,
			BUFF
		};

        #region Inspector

        [SerializeField] private Weapon[] _weapons;
		[SerializeField] private Player _player;
		[SerializeField] private Transform _pivot = default;

        #endregion

        #region Networked properties

        [Networked]
		public byte selectedPrimaryWeapon { get; set; }

		[Networked]
		public byte selectedSecondaryWeapon { get; set; }

		[Networked]
		public TickTimer primaryFireDelay { get; set; }

		[Networked]
		public TickTimer secondaryFireDelay { get; set; }

		[Networked]
		public byte primaryAmmo { get; set; }

		[Networked]
		public byte secondaryAmmo { get; set; }

		[Networked] public NetworkBool isReloading { get; set; }
		[Networked] public TickTimer reloadingTime { get; set; }

        #endregion

        #region Private properties

        private byte _activePrimaryWeapon;
		private byte _activeSecondaryWeapon;

        #endregion

        #region Networked methods

        public override void Render()
		{
			ShowAndHideWeapons();
		}

        public override void Spawned()
        {
			var weaponIndex = 0;
			var weapon = _weapons[weaponIndex];

			primaryAmmo = weapon.ammo;
			_player.RefreshWeaponInformation(primaryAmmo, weapon.InitialAmmo);
		}

        public override void FixedUpdateNetwork()
        {
            // Check if reloading should finish
			if (this.isReloading)
            {
				if (this.reloadingTime.ExpiredOrNotRunning(Runner))
                {
					StopReloading();
                }
			}
        }

        #endregion

        #region Public methods
		
		/// <summary>
		/// Activate a new weapon when picked up
		/// </summary>
		/// <param name="weaponType">Type of weapon that should be activated</param>
		/// <param name="weaponIndex">Index of weapon the _Weapons list for the player</param>
		public void ActivateWeapon(WeaponInstallationType weaponType, int weaponIndex)
		{
			byte selectedWeapon = weaponType == WeaponInstallationType.PRIMARY ? selectedPrimaryWeapon : selectedSecondaryWeapon;
			byte activeWeapon = weaponType == WeaponInstallationType.PRIMARY ? _activePrimaryWeapon : _activeSecondaryWeapon;

			if (!_player.isActivated || selectedWeapon != activeWeapon)
				return;

			// Fail safe, clamp the weapon index within weapons list bounds
			weaponIndex = Mathf.Clamp(weaponIndex, 0, _weapons.Length - 1);

			if (weaponType == WeaponInstallationType.PRIMARY)
			{
				selectedPrimaryWeapon = (byte)weaponIndex;
				primaryAmmo = _weapons[(byte) weaponIndex].ammo;

				_player.RefreshWeaponInformation(primaryAmmo, _weapons[(byte)weaponIndex].InitialAmmo);
			}
			else
			{
				selectedSecondaryWeapon = (byte)weaponIndex;
				secondaryAmmo = _weapons[(byte) weaponIndex].ammo;
			}
		}

		/// <summary>
		/// Fire the current weapon. This is called from the Input Auth Client and on the Server in
		/// response to player input. Input Auth Client spawns a dummy shot that gets replaced by the networked shot
		/// whenever it arrives
		/// </summary>
		public void FireWeapon(WeaponInstallationType weaponType)
		{
			// Avoid action if it is reloading
			if (this.isReloading) return;

			if (!IsWeaponFireAllowed(weaponType)) return;

			byte ammo = weaponType == WeaponInstallationType.PRIMARY ? primaryAmmo : secondaryAmmo;

			TickTimer tickTimer = weaponType==WeaponInstallationType.PRIMARY ? primaryFireDelay : secondaryFireDelay;
			if (tickTimer.ExpiredOrNotRunning(Runner) && ammo > 0)
			{
				byte weaponIndex = weaponType == WeaponInstallationType.PRIMARY ? _activePrimaryWeapon : _activeSecondaryWeapon;
				Weapon weapon = _weapons[weaponIndex];

				var aimDirection = GetAimingDirection();

				weapon.Fire(Runner,Object.InputAuthority, aimDirection, _player);

				if (!weapon.infiniteAmmo)
					ammo--;

				if (weaponType == WeaponInstallationType.PRIMARY)
				{
					primaryFireDelay = TickTimer.CreateFromSeconds(Runner, weapon.delay);
					primaryAmmo = ammo;
				}
				else
				{
					secondaryFireDelay = TickTimer.CreateFromSeconds(Runner, weapon.delay);
					secondaryAmmo = ammo;
				}
					
				//if (/*Object.HasStateAuthority &&*/ ammo == 0)
				//{
				//	ResetWeapon(weaponType);
				//}

				if (ammo == 0 && !this.isReloading)
                {
					StartReloading(weapon);
                }
				else
                {
					_player.RefreshWeaponInformation(ammo, weapon.InitialAmmo);
                }
			}
		}
		
		public void ResetAllWeapons()
		{
			ResetWeapon(WeaponInstallationType.PRIMARY);
			ResetWeapon(WeaponInstallationType.SECONDARY);
		}

		public void InstallWeapon(PowerupElement powerup)
		{
			int weaponIndex = GetWeaponIndex(powerup.powerupType);
			ActivateWeapon(powerup.weaponInstallationType, weaponIndex);
		}

		public Items.ItemWeaponType GetWeaponType()
		{
			if (_activePrimaryWeapon < 0 || _activePrimaryWeapon >= _weapons.Length) return Items.ItemWeaponType.NONE;

			return _weapons[_activePrimaryWeapon].WeaponType;
		}

		public void MeleeAttack()
        {
			Debug.LogError("<color=magenta>WeaponManager</color>::MeleeAttack");

			byte weaponIndex =_activePrimaryWeapon;
			
			Weapon weapon = _weapons[weaponIndex];

			var aimDirection = GetAimingDirection();

			weapon.MeleeAttack(Runner, Object.InputAuthority, aimDirection, _player);
		}

		#endregion

		#region Private methods

		private void ShowAndHideWeapons()
		{
			// Animates the scale of the weapon based on its active status
			for (int i = 0; i < _weapons.Length; i++)
			{
				_weapons[i].Show(i == selectedPrimaryWeapon || i == selectedSecondaryWeapon);
			}

			// Whenever the weapon visual is fully visible, set the weapon to be active - prevents shooting when changing weapon
			SetWeaponActive(selectedPrimaryWeapon, ref _activePrimaryWeapon);
			SetWeaponActive(selectedSecondaryWeapon, ref _activeSecondaryWeapon);
		}

		private void SetWeaponActive(byte selectedWeapon, ref byte _activeWeapon)
		{
			if (_weapons[selectedWeapon].isShowing)
				_activeWeapon = selectedWeapon;
		}

		private Vector3 GetAimingDirection()
        {
			var dir = _player.velocity;

			if (_player.IsTargetDetected())
            {
				dir = _player.GetTargetDirection();
            }

			return dir;
        }

		private bool IsWeaponFireAllowed(WeaponInstallationType weaponType)
		{
			if (!_player.isActivated)
				return false;

			// Has the selected weapon become fully visible yet? If not, don't allow shooting
			if (weaponType == WeaponInstallationType.PRIMARY && _activePrimaryWeapon != selectedPrimaryWeapon)
				return false;
			else if (weaponType == WeaponInstallationType.SECONDARY && _activeSecondaryWeapon != selectedSecondaryWeapon)
				return false;
			return true;
		}

		void ResetWeapon(WeaponInstallationType weaponType)
		{
			if (weaponType == WeaponInstallationType.PRIMARY)
			{
				ActivateWeapon(weaponType, 0);
			}
			else if (weaponType == WeaponInstallationType.SECONDARY)
			{
				ActivateWeapon(weaponType, 4);
			}
		}

		private int GetWeaponIndex(PowerupType powerupType)
		{
			for (int i = 0; i < _weapons.Length; i++)
			{
				if (_weapons[i].powerupType == powerupType)
					return i;
			}

			Debug.LogError($"Weapon {powerupType} was not found in the weapon list, returning <color=red>0 </color>");
			return 0;
		}

		private void RefreshVisualWeapon(string displayName, GameObject visualRepresentation)
        {
			Debug.LogError($"Refresh visual weapon for <color=yellow>{visualRepresentation.name}</color>");

			if (_pivot.childCount > 0)
            {
				var previousWeapon = _pivot.GetChild(0);

				Destroy(previousWeapon.gameObject);
            }

			var newWeapon = Instantiate(visualRepresentation, Vector3.zero, Quaternion.identity, _pivot);
			newWeapon.transform.localPosition = Vector3.zero;
			newWeapon.transform.localRotation = Quaternion.identity;

			newWeapon.name = $"visualWeapon_{displayName}";
        }

		#endregion

		#region Reloading actions

		public void StartReloadingWeapon(WeaponManager.WeaponInstallationType weaponType)
        {
			if (this.isReloading) return;

			byte weaponIndex = weaponType == WeaponInstallationType.PRIMARY ? _activePrimaryWeapon : _activeSecondaryWeapon;
			Weapon weapon = _weapons[weaponIndex];

			// Check if weapon is full magazine
			if (this.primaryAmmo >= weapon.InitialAmmo) return;

			StartReloading(weapon);
		}

		private void StartReloading(Weapon weapon)
        {
			this.reloadingTime = TickTimer.CreateFromSeconds(Runner, weapon.ReloadingTime);
			this.isReloading = true;

			_player.StartReloadingWeapon(weapon.ReloadingTime, this.reloadingTime);
        }

		private void StopReloading()
        {
			Weapon weapon = _weapons[_activePrimaryWeapon];

			this.primaryAmmo = weapon.InitialAmmo;

			this.isReloading = false;

			_player.StopReloadingWeapon(this.primaryAmmo, weapon.InitialAmmo);
        }

        #endregion

        #region Equip

		public void Equip(int itemId, Items.EquipableItemCatalogData itemCatalogData, out int previousWeaponId)
        {
			previousWeaponId = -1;

			// Remove previous weapon
			if (_weapons[0] != null)
			{
				var oldWeapon = _weapons[0];

				previousWeaponId = oldWeapon.ItemId;
			}

			var weapon = _weapons[0];

			weapon.OverrideConfiguration(itemId, itemCatalogData);

			RefreshVisualWeapon(itemCatalogData.displayName, itemCatalogData.WeaponData.Visual);
			
			if (weapon.WeaponType != Items.ItemWeaponType.ASSAULT) return;

			primaryAmmo = 0;

			StartReloading(weapon);
        }

        #endregion
    }
}