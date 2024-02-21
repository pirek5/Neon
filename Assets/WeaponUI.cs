using UnityEngine;
using TMPro;
using Zenject;

public class WeaponUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _ammoCountText;
    [SerializeField] private TextMeshProUGUI _magazinesCountText;
    private WeaponController _weaponController;

    [Inject]
    private void Construct(WeaponController weaponController)
    {
        _weaponController = weaponController;
    }

    private void Start()
    {
        _weaponController.OnWeaponUIUpdate += HandleUpdateWeaponUI;
    }

    private void HandleUpdateWeaponUI(IWeapon weapon)
    {
        UpdateAmmoUI(weapon.GetAmmoCount()); // Aktualizacja ilo�ci amunicji
        UpdateMagazinesUI(weapon.GetMagazinesCount()); // Aktualizacja ilo�ci magazynk�w
    }

    private void UpdateAmmoUI(int ammoCount)
    {
        _ammoCountText.text = "Ammo: " + ammoCount;
    }

    private void UpdateMagazinesUI(int magazinesCount)
    {
        _magazinesCountText.text = "Magazines: " + magazinesCount;
    }

    private void OnDestroy()
    {
        _weaponController.OnWeaponUIUpdate -= HandleUpdateWeaponUI;
    }
}


