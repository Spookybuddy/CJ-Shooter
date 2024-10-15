using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public int HP;
    public int lvl;
    private bool rayHit;
    private bool spotted;
    private Vector3 target;
    private Vector3 direction;
    private Quaternion lookAt;
    private GameObject player;

    private int chamber;
    private float reload;
    private bool shooting;

    public GameObject from;
    public GameObject line;
    public Material laserColor;
    public ParticleSystem impact;
    private GameObject shot;
    private Vector3[] laser;

    public GameObject[] interest;
    public int index;
    public int prev;

    public GameObject exclaim;

    public NavMeshAgent agent;
    private int pastHP;
    private bool damaged;
    private float aggro;
    private bool wasSpotted;
    private float forgetting;
    private float distance;
    private Vector3 destination;

    private bool paused;
    private int active;
    private GameManager manage;
    private Rigidbody rig;
    private Player script;

    private Vector3 front;
    private int diff;

    void Start()
    {
        manage = GameObject.FindWithTag("GameController").GetComponent<GameManager>();
        player = GameObject.FindWithTag("MainCamera");
        script = player.GetComponent<Player>();
        interest = GameObject.FindGameObjectsWithTag("Finish");
        prev = -1;
        rig = GetComponent<Rigidbody>();
        laser = new Vector3[2];

        lvl = manage.AILevel;
        HP = Mathf.Clamp(lvl / 2, 1, 5);
        chamber = 5;
        target = Vector3.zero;
        pastHP = HP;
        damaged = false;
        forgetting = 0;
        wasSpotted = false;
        exclaim.SetActive(false);

        agent.speed = lvl;
        diff = lvl * -2 + 25;

        NewPoint();
    }

    void Update()
    {
        paused = manage.paused;
        active = paused ? 1 : 0;
        front = transform.position + (transform.forward * 0.5f);

        //Only shoot if not paused, there's bullets in the chamber, not reloading, and can see target
        if (shooting && reload == 0 && chamber > 0 && !paused) {
            Shoot();
            chamber -= 1;
            reload = (12 - lvl) / 20f;
        }

        //Chamber empty, reload
        if (chamber < 1) {
            reload = 1;
            chamber = 5;
        }

        //Raycast down to detect ground, else move down like gravity
        if (Physics.Raycast(transform.position - Vector3.up, Vector3.up * -0.5f, out RaycastHit hit, 0.5f)) {
            transform.position = new Vector3(transform.position.x, hit.point.y + 1.1f, transform.position.z);
            rig.useGravity = false;
        } else if (!paused) {
            rig.useGravity = true;
        }

        //Stop movement on pause
        if (paused) {
            agent.SetDestination(transform.position);
        } else {
            agent.SetDestination(destination);
        }

        //Wander until player is spotted
        if (!spotted) {
            if (Vector3.Distance(transform.position, destination) < 2) {
                shooting = false;
                NewPoint();
            }
        } else {
            distance = Vector3.Distance(transform.position, target);
            if (HP == 1 || reload > 0) {
                //Back off if reloading or low HP
                MaintainDistance(30);
            } else if (distance > 21 || distance < 19) {
                //Circle around player, maintaining a distance of 20 units
                MaintainDistance(20);
            } else {
                //Stand still and shoot
                destination = transform.position;
                shooting = true;
            }
        }

        //Instantly notice player when shot, and then calm down over time
        if (pastHP != HP) {
            pastHP = HP;
            damaged = true;
            aggro = lvl / 2f;
        } else if (!paused) {
            aggro = Mathf.Clamp(aggro - 0.01f, 0, lvl / 2f);
        }

        //If the player has been spotted, set was spotted to true and reset the forget cooldown. Otherwise reduce the cooldown
        if (spotted) {
            wasSpotted = true;
            forgetting = lvl / 2f;
        } else if (!paused) {
            forgetting = Mathf.Clamp(forgetting - 0.01f, 0, lvl / 2f);
        }

        //If they completely forget/unaggro, they no longer become aggro
        if (aggro == 0) damaged = false;
        if (forgetting == 0) wasSpotted = false;
        exclaim.SetActive(wasSpotted);

        //Unagrro when player respawns
        if (script.HP < 1) {
            spotted = false;
            wasSpotted = false;
            forgetting = 0;
            damaged = false;
            aggro = 0;
        }
    }

    void FixedUpdate()
    {
        reload = Mathf.Clamp01(reload - 0.01f);

        //Raycast from in front of enemy to player, check to see if collider hit is tagged player
        Debug.DrawRay(front, player.transform.position - front, Color.red, 0.1f);
        rayHit = false;
        if (Physics.Raycast(front, player.transform.position - front, out RaycastHit hit, 50, 3)) {
            if (hit.collider.CompareTag("MainCamera")) rayHit = true;
        }
        spotted = (rayHit || damaged);

        //If damaged or having seen the player, record their position
        if (spotted || wasSpotted) target = player.transform.position;

        //Use the hit point as the target to rotate towards over time
        if (spotted) {
            direction = (target - transform.position).normalized;
            lookAt = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAt, Time.deltaTime * lvl);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }
    }

    //Keep at a distance
    private void MaintainDistance(float stayAt)
    {
        //Use trig to determine a set distance along the ray between target
        float X = target.x - transform.position.x;
        float Z = target.z - transform.position.z;
        float H = Mathf.Acos(X / distance) * Mathf.Sign(Z);
        X = Mathf.Cos(H) * -stayAt + target.x;
        Z = Mathf.Sin(H) * -stayAt + target.z;
        destination = new Vector3(X, transform.position.y, Z);
    }

    //Find new point of interest that is not the current one
    private void NewPoint()
    {
        index = Random.Range(0, interest.Length);
        if (index != prev) {
            destination = interest[index].transform.position;
            prev = index;
        }
    }

    //Shoot at player
    private void Shoot()
    {
        float offset = Random.Range(-diff, diff + 1);
        Ray fire = new Ray(transform.position + (Vector3.up * 0.1f), (player.transform.position - transform.position).normalized + (transform.right * offset * 0.02f));
        if (Physics.Raycast(fire, out RaycastHit hit, 2000)) {
            laser[1] = hit.point;
            Instantiate(impact, hit.point, Quaternion.identity);
            if (hit.collider.CompareTag("MainCamera")) script.HP -= 1;
        } else {
            laser[1] = (fire.direction * 2000);
        }

        laser[0] = from.transform.position;
        shot = Instantiate(line, Vector3.zero, Quaternion.identity) as GameObject;
        LineRenderer rend = shot.GetComponent<LineRenderer>();
        rend.SetPositions(laser);
        rend.startWidth = 0.03f;
        rend.endWidth = 0.3f;
        rend.material = laserColor;
    }
}