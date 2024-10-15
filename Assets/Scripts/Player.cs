using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    private float tilt;
    private float side;
    private float forward;
    private float strafe;
    private Vector3 right;
    private Vector3 front;

    private bool paused;
    private int active;

    private int points;
    public TextMeshProUGUI bullets;
    public TextMeshProUGUI score;
    public TextMeshProUGUI health;
    public GameObject resume;

    public int HP;
    private float spd;
    private int chamber;
    private int magazine;
    private float reload;
    private bool reloading;
    private float spray;
    private float sense;
    private int reticleDis;

    public int selected;
    public GameObject[] weapons;
    public Material[] reticles;

    public GameObject from;
    public GameObject line;
    public Material laserColor;
    public ParticleSystem impact;
    public GameObject reticle;
    private GameObject shot;
    private Vector3[] laser;

    private GameObject manager;
    private GameManager manage;
    private Rigidbody rig;
    private Camera cam;

    void Start()
    {
        //Establish components
        cam = GetComponent<Camera>();
        rig = GetComponent<Rigidbody>();
        laser = new Vector3[2];

        //Check to see if the game started on the correct scene
        manager = GameObject.FindWithTag("GameController");
        manage = manager.GetComponent<GameManager>();

        //Copy values from the manager
        magazine = manage.magSize;
        spd = manage.maxSpeed / 2f;
        spray = manage.spread;
        sense = manage.sensitivity;
        HP = manage.playerHP;
        reticleDis = manage.reticle;
        reticle.GetComponent<Renderer>().material = reticles[manage.index];

        //Preset values
        selected = 0;
        reloading = false;
        chamber = magazine;
        reload = 0;
        points = 0;
        UnPause();
        UI();
        GunSwitch();
    }

    void Update()
    {
        //If manager is missing, exit to main scene
        if (manager == null) SceneManager.LoadScene("Main");

        //Switch weapons with scroll wheel
        selected = (int)Mathf.Abs((selected + Input.mouseScrollDelta.y) % 2);
        GunSwitch();

        //Death boundary
        if (transform.position.y < -5) HP = 0;

        //Pause menu
        active = paused ? 0 : 1;
        paused = manage.paused;
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Cursor.lockState = CursorLockMode.None;
            manage.paused = true;
            rig.useGravity = false;
        }

        //Sprint
        spd = (Input.GetKey(KeyCode.LeftShift)) ? manage.maxSpeed : manage.maxSpeed / 2f;

        //Fire bullet
        if (Input.GetMouseButton(0) && reload == 0 && !paused) {
            if (selected == 0) {
                ShotsFired(1);
            } else {
                BurstFire();
            }
            //Infinte ammo if magazine size is 21
            if (magazine < 21) chamber -= 1;
            reload = 0.1f;
        }

        //reload
        if ((chamber < 1 || Input.GetKeyDown(KeyCode.R)) && !reloading) {
            reload = 1;
            reloading = true;
        }

        //respawn when HP <= 0
        if (HP < 1) {
            HP = 5;
            reload = 1;
            reloading = true;
            transform.position = manage.respawns[Random.Range(0, manage.respawns.Length)].transform.position + Vector3.up;
            transform.rotation = Quaternion.identity;
        }

        //Get inputs from mouse and WASD/<^v>
        tilt = Input.GetAxis("Mouse Y") * sense * active;
        side = Input.GetAxis("Mouse X") * sense * active;
        forward = Input.GetAxis("Vertical") * active;
        strafe = Input.GetAxis("Horizontal") * active;
        
        //Camera move when mouse moves, can only look 45* up and down
        transform.localEulerAngles -= (Vector3.right * tilt);
        transform.localEulerAngles += (Vector3.up * side);
        if (transform.localEulerAngles.x > 45 && transform.localEulerAngles.x < 180) {
            transform.localEulerAngles = new Vector3(45, transform.localEulerAngles.y, 0);
        } else if (transform.localEulerAngles.x > 180 && transform.localEulerAngles.x < 315) {
            transform.localEulerAngles = new Vector3(315, transform.localEulerAngles.y, 0);
        }

        //Player move
        front = new Vector3(transform.forward.x, 0, transform.forward.z);
        right = new Vector3(transform.right.x, 0, transform.right.z);
        rig.AddForce(front * forward * Time.deltaTime * spd * 12, ForceMode.Impulse);
        rig.AddForce(right * strafe * Time.deltaTime * spd * 12, ForceMode.Impulse);

        //Clamp velocity based on max speed. Max speed increases when running
        float y = 0;
        if (rig.useGravity) {
            y = 100;
            rig.AddForce(Vector3.down * Time.deltaTime * 80, ForceMode.Impulse);
        }
        rig.velocity = new Vector3(Mathf.Clamp(rig.velocity.x, -spd, spd), Mathf.Clamp(rig.velocity.y, -y, y), Mathf.Clamp(rig.velocity.z, -spd, spd));

        //Prevent Z rotation;
        if (transform.localEulerAngles.z != 0) {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, 0);
        }

        //Keep the reticle at a set distance, unless it hits something
        ReticlePos();
    }

    void FixedUpdate()
    {
        //constantly decrease the reload clock to act as a cooldown
        reload = Mathf.Clamp01(reload - 0.01f);
        if (reload == 0 && reloading) {
            chamber = magazine;
            reloading = false;
        }

        UI();

        //Raycast down to detect ground, else move down like gravity
        if (Physics.Raycast(transform.position - Vector3.up, Vector3.up * -1.1f, out RaycastHit hit, 1.1f)) {
            transform.position = new Vector3(transform.position.x, hit.point.y + 2, transform.position.z);
            rig.useGravity = false;
        } else if (!paused) {
            rig.useGravity = true;
        }
    }

    private void GunSwitch()
    {
        for (int w = 0; w < weapons.Length; w++) {
            weapons[w].SetActive(w == selected);
        }
    }

    //Update UI display in game scene
    private void UI()
    {
        resume.SetActive(paused);
        bullets.text = chamber + " / " + magazine;
        if (magazine > 20) bullets.text = "\u221E" + " / " + "\u221E";
        score.text = "Score: " + points;
        health.text = HP + "";
    }

    //Button functions: Pause and Return to main menu
    public void UnPause()
    {
        manage.paused = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Load(string scene) { manage.Load(scene); }

    private void ShotsFired(float spread2)
    {
        float offX = Random.Range(-spray * spread2, spray * spread2);
        float offY = Random.Range(-spray * 0.5625f * spread2, spray * 0.5625f * spread2);
        Vector3 midscreen = new Vector3((cam.pixelWidth / 2) + offX, (cam.pixelHeight / 2) + offY, 0);
        Ray ray = cam.ScreenPointToRay(midscreen);
        if (Physics.Raycast(transform.position, ray.direction, out RaycastHit hit, 20000, 3)) {
            //Impact point
            laser[1] = hit.point;

            //Impact particle
            Instantiate(impact, hit.point, Quaternion.identity);

            //Damage enemies
            if (hit.collider.CompareTag("Enemy")) {
                EnemyAI target = hit.collider.gameObject.GetComponent<EnemyAI>();
                target.HP -= 1;
                if (target.HP < 1) {
                    Destroy(hit.collider.gameObject);
                    points += 1;
                }
            }
        } else {
            laser[1] = (ray.direction * 20000);
        }

        //Draw bullet line
        LineRender();
    }

    private void ReticlePos()
    {
        Vector3 midscreen = new Vector3((cam.pixelWidth / 2), (cam.pixelHeight / 2), 0);
        Ray ray = cam.ScreenPointToRay(midscreen);
        if (Physics.Raycast(transform.position, ray.direction, out RaycastHit hit, reticleDis, 3)) {
            reticle.transform.position = hit.point;
        } else {
            reticle.transform.localPosition = (Vector3.forward * reticleDis);
        }
    }

    private void BurstFire()
    {
        for (int i = 0; i < chamber; i++) {
            ShotsFired(chamber + 2);
        }
        if (magazine < 21) chamber = 1;
    }

    private void LineRender()
    {
        laser[0] = from.transform.position;
        shot = Instantiate(line, Vector3.zero, Quaternion.identity) as GameObject;
        LineRenderer rend = shot.GetComponent<LineRenderer>();
        rend.SetPositions(laser);
        rend.startWidth = 0.02f;
        rend.endWidth = 0.2f;
        rend.material = laserColor;
    }
}