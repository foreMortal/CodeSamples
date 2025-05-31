using UnityEngine;

public class SetupWeapon : MonoBehaviour
{
    [SerializeField] private GetStatisticScriptableObject stats;
    [SerializeField] private SettingsScriptableObject settings;
    [SerializeField] private AllApexRecoilsScriptableObject recoils;
    [SerializeField] private GameObject[] weapons;
    [SerializeField] private Vector3[] weaponsPositions;
    [SerializeField] private int innerWeaponIndex;
    [SerializeField] private ApexRecoilScripatbleObject deafultWeapons;

    private UserRebinds rebinds;
    private Controlls controls;
    private PlayerMouvement move;
    private Camera fpsCam;
    private IRecoilProvider provider;
    private IShootable[] activeWeapon = new IShootable[2];
    private int[] activeWeaponsIndexes = new int[] {-1, -1};
    private CameraMoveParent[] cameraMove;
    private Animator[] animators = new Animator[2];
    private bool enableRecoil, inSwap, canSwap, awaked;
    private float swapTime;
    private RebindType type;
    private BulletsManager bulletsManager;
    private string[] gunsNames = new string[2];
    
    private void OnEnable()
    {
        controls = new();
        controls.GamepadControl.SwapGun.Enable();
    }
    private void OnDisable()
    {
        controls.GamepadControl.SwapGun.Disable();
    }

    private void SelfAwake()
    {
        fpsCam = GetComponent<Camera>();
        move = GetComponentInParent<PlayerMouvement>();
        controls = settings.controlls;
        bulletsManager = GetComponent<BulletsManager>();

        ApexApplySettings.ReuploadSettings.AddListener(Reupload);
        
        type = settings.rebinds.Type;
        if (type == RebindType.Apex)
        {
            rebinds = settings.rebinds;
            if ((int)rebinds.Data[AllSettingsKeys.RECOIL_ACTIVE] == 1)
                enableRecoil = true;
            else if((int)rebinds.Data[AllSettingsKeys.RECOIL_ACTIVE] == 2)
                enableRecoil = false;
            controls.GamepadControl.SwapGun.performed += ctx => SwapGun();
        }
    }

    private void Update()
    {
        if (inSwap)
        {
            if(swapTime > 0f)
            {
                swapTime -= Time.deltaTime;
                if (swapTime <= 0f)
                {
                    activeWeapon[1].SetActive(false);
                    activeWeapon[0].SetActive(true);
                    swapTime -= Time.deltaTime;
                }
            }
            else
            {
                swapTime -= Time.deltaTime;
                if (swapTime <= -0.15f)
                {
                    inSwap = false;
                }
            }
        }
    }

    public void ChangeWeapon(int recoilIndex)
    {
        if (enableRecoil)
        {
            ApexRecoilScripatbleObject recoil = recoils.Recoils[recoilIndex];
            if (activeWeapon[1] == null)
            {
                TakeWeapon(1, recoil);
                canSwap = true;
                SwapGun();
            }
            else if (activeWeaponsIndexes[0] != recoil.WeaponIndex)
            {
                foreach (var move in cameraMove)
                {
                    if (activeWeapon[0] != null)
                        move.PerformAds -= activeWeapon[0].PerformAds;
                }
                activeWeapon[0].SelfDestroy();
                TakeWeapon(0, recoil);
            }
            else
            {
                activeWeapon[0].GetGunRecoilable().ChangeWeapon(recoil);
                gunsNames[0] = recoil.WeaponName;
                bulletsManager.ChangeGun(gunsNames);
            }
        }
    }

    public void SwapGun()
    {
        if (!inSwap && canSwap)
        {
            inSwap = true;
            swapTime = 0.25f;
            animators[0].SetBool("Hide", true);
            IShootable hndlr = activeWeapon[0];
            activeWeapon[0] = activeWeapon[1];
            activeWeapon[1] = hndlr;

            int hndlrI = activeWeaponsIndexes[0];
            activeWeaponsIndexes[0] = activeWeaponsIndexes[1];
            activeWeaponsIndexes[1] = hndlrI;

            Animator hndlrA = animators[0];
            animators[0] = animators[1];
            animators[1] = hndlrA;
            move.Animator = animators[0];

            string h = gunsNames[0];
            gunsNames[0] = gunsNames[1];
            gunsNames[1] = h;
        }
    }

    public void RevealGun()
    {
        activeWeapon[0].SetActive(true);
    }
    public void GunRevealed() 
    {
        inSwap = false;
    }

    public void SetActive(bool state)
    {
        if (activeWeapon[0] != null)
        {
            activeWeapon[0].SetActive(state);
        }
    }

    public void Climb(bool state)
    {
        animators[0].SetBool("Climb", state);
    }

    public void ChangeRecoilProvider(IRecoilProvider provider)
    {
        if(cameraMove == null)//called once then camera setuped
        {
            cameraMove = GetComponentsInChildren<CameraMoveParent>();
            SelfAwake();
            if (enableRecoil)
            {
                ApexRecoilScripatbleObject recoil = recoils.Recoils[settings.RecoilIndex];
                TakeWeapon(0, recoil);
            }
            else
            {
                TakeWeapon(0, deafultWeapons);
            }
        }
        foreach(var weapon in activeWeapon)
        {
            if(weapon != null && enableRecoil)
                weapon.GetGunRecoilable().ChangeRecoilProvider(provider);
        }
        this.provider = provider;
    }

    private void TakeWeapon(int i, ApexRecoilScripatbleObject recoil)
    {
        GameObject gun = Instantiate(weapons[recoil.WeaponIndex], fpsCam.transform);
        gun.transform.SetLocalPositionAndRotation(weaponsPositions[recoil.WeaponIndex], Quaternion.identity);
        activeWeapon[i] = gun.GetComponent<IShootable>();
        activeWeapon[i].Setup(this, bulletsManager);
        activeWeaponsIndexes[i] = recoil.WeaponIndex;
        activeWeapon[i].GetGunRecoilable().ChangeWeapon(recoil);
        activeWeapon[i].GetGunRecoilable().ChangeRecoilProvider(provider);
        animators[i] = gun.GetComponent<Animator>();
        gunsNames[i] = recoil.WeaponName;
        move.Animator = animators[i];
        bulletsManager.ChangeGun(gunsNames);
        foreach (var move in cameraMove)
        {
            move.PerformAds += activeWeapon[i].PerformAds;
        }
    }

    private void Reupload()
    {
        if (type == RebindType.Apex)
        {
            if ((int)rebinds.Data[AllSettingsKeys.RECOIL_ACTIVE] == 2)//Not active
            {
                if (enableRecoil)
                {
                    canSwap = false;
                    enableRecoil = false;
                    for (int i = 0; i < 2; i++)
                    {
                        if (activeWeapon[i] != null)
                        {
                            foreach (var move in cameraMove)
                            {
                                move.PerformAds -= activeWeapon[i].PerformAds;
                            }
                            activeWeapon[i].SelfDestroy();
                            activeWeapon[i] = null;
                        }
                    }
                    TakeWeapon(0, deafultWeapons);
                }
            }
            else if ((int)rebinds.Data[AllSettingsKeys.RECOIL_ACTIVE] == 1)//active
            {
                if (!enableRecoil)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (activeWeapon[i] != null)
                        {
                            foreach (var move in cameraMove)
                            {
                                move.PerformAds -= activeWeapon[i].PerformAds;
                            }
                            activeWeapon[i].SelfDestroy();
                            activeWeapon[i] = null;
                        }
                    }
                    ApexRecoilScripatbleObject recoil = recoils.Recoils[settings.RecoilIndex];
                    TakeWeapon(0, recoil);
                    enableRecoil = true;
                }
            }
        }
    }

    public void RegisterHit()
    {
        activeWeapon[0].ShowHitMarker();
    }
}