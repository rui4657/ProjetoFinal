using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;


public class GunSystem : MonoBehaviour
{
    [Header("Info da Arma")]
    public string gunName = "AK";

    [Header("Referências")]
    public Camera playerCam;
    public Transform muzzlePoint;
    public PlayerHealth player;

    [Header("Tiro")]
    public float fireRate = 8f;
    public float maxDistance = 100f;
    public int damage = 20;
    public LayerMask hitMask;

    [Header("Munição")]
    public int magSize = 30;
    public int reserveAmmo = 120;
    public int currentAmmo;
    public float reloadTime = 2f;

    private bool reloading = false;
    private float nextTimeToFire = 0f;

    [Header("Efeitos")]
    public ParticleSystem muzzleFlash;
    public LineRenderer bulletTrail;
    public GameObject impactEffect;
    public float trailTime = 0.05f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip shotClip;
    public AudioClip reloadClip;
    [Range(0f, 1f)] public float shotVolume = 1f;
    [Range(0f, 1f)] public float reloadVolume = 1f;

    void Start()
    {
        if (bulletTrail != null)
        {
            bulletTrail.positionCount = 2;
            bulletTrail.enabled = false;
        }

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (reloading) return;

        // RELOAD manual (R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Só recarrega se houver reserva e se o carregador não estiver cheio
            if (reserveAmmo > 0 && currentAmmo < magSize)
                StartCoroutine(Reload());

            return;
        }

        // Se o carregador ficou vazio, só tenta recarregar se houver reserva.
        if (currentAmmo <= 0)
        {
            if (reserveAmmo > 0)
                StartCoroutine(Reload());

            return; // se não houver reserva, não faz nada (não entra em loop)
        }

        // AK: segurar = rajada, click = um tiro (porque depende do fireRate)
        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            ShootIfPossible();
        }
    }

    void ShootIfPossible()
    {
        if (currentAmmo <= 0) return;

        currentAmmo--;
        nextTimeToFire = Time.time + 1f / fireRate;

        if (sfxSource && shotClip)
            sfxSource.PlayOneShot(shotClip, shotVolume);

        SaveCurrentState();
        Shoot();
    }

    IEnumerator Reload()
    {
        // anti-loop
        if (reloading) yield break;
        if (reserveAmmo <= 0) yield break;
        if (currentAmmo >= magSize) yield break;

        reloading = true;

        if (sfxSource && reloadClip)
            sfxSource.PlayOneShot(reloadClip, reloadVolume);

        yield return new WaitForSeconds(reloadTime);

        int missing = magSize - currentAmmo;
        int toLoad = Mathf.Min(missing, reserveAmmo);

        // Se por algum motivo não houver nada para carregar, sai sem loop
        if (toLoad <= 0)
        {
            reloading = false;
            yield break;
        }

        currentAmmo += toLoad;
        reserveAmmo -= toLoad;

        reloading = false;

        SaveCurrentState();
    }

    void Shoot()
    {
        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        RaycastHit hit;
        Vector3 hitPoint;

        if (Physics.Raycast(ray, out hit, maxDistance, hitMask))
        {
            hitPoint = hit.point;

            ZombieAI zombie = hit.collider.GetComponent<ZombieAI>();
            if (zombie != null)
                zombie.TakeDamage(damage);

            HealthDummy dummy = hit.collider.GetComponent<HealthDummy>();
            if (dummy != null)
                dummy.TakeDamage(damage);

            if (impactEffect != null)
                Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
        }
        else
        {
            hitPoint = playerCam.transform.position + playerCam.transform.forward * maxDistance;
        }

        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (bulletTrail != null && muzzlePoint != null)
            StartCoroutine(ShowTrail(hitPoint));
    }

    IEnumerator ShowTrail(Vector3 hitPoint)
    {
        bulletTrail.enabled = true;
        bulletTrail.SetPosition(0, muzzlePoint.position);
        bulletTrail.SetPosition(1, hitPoint);

        yield return new WaitForSeconds(trailTime);

        bulletTrail.enabled = false;
    }

    void SaveCurrentState()
    {
        if (player == null || SaveSystem.instance == null) return;

        int round = 1;
        RoundManager rm = Object.FindFirstObjectByType<RoundManager>();
        if (rm != null) round = rm.currentRound;

        Vector3 pos = player.transform.position;

        SaveSystem.instance.Save(
            player.CurrentHP,
            gunName,
            currentAmmo,
            reserveAmmo,
            round,
            pos
        );
    }

    internal void InitNewGame()
    {
        currentAmmo = magSize;
        reserveAmmo = 120;
    }

    public void ApplyLoadedGun(string armaNome, int ammoMag, int ammoReserva)
    {
        gunName = armaNome;
        currentAmmo = ammoMag;
        reserveAmmo = ammoReserva;
    }
}
