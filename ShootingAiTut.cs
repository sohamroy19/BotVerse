using System.IO;   
using UnityEngine;
using UnityEngine.AI;
using Unity.Barracuda;

public class ShootingAiTut : MonoBehaviour
{
    public NavMeshAgent agent;

    public Transform player;
    public GameObject gun;

    //Stats
    public int health;

    //Check for Ground/Obstacles
    public LayerMask whatIsGround, whatIsPlayer;

    //Patroling
    public Vector3 walkPoint;
    public bool walkPointSet;
    public float walkPointRange;

    //Attack Player
    public float timeBetweenAttacks;
    bool alreadyAttacked;

    //States
    public bool isDead;
    public float sightRange, attackRange;
    public bool playerInSightRange, playerInAttackRange;

    //Special
    public Material green, red, yellow;
    public GameObject projectile;

    public NNModel modelAsset;
    private Model m_RuntimeModel;
    IWorker m_Worker;

    private void Awake()
    {
        player = GameObject.Find("PlayerObj").transform;
        agent = GetComponent<NavMeshAgent>();

        m_RuntimeModel = ModelLoader.Load(modelAsset);
        // <WorkerFactory.Type>, 
        m_Worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, m_RuntimeModel);
    }
    private void Update()
    {
        if (!isDead)
        {
            //Check if Player in sightrange
            playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);

            //Check if Player in attackrange
            playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

            if (!playerInSightRange && !playerInAttackRange) Patroling();
            if (playerInSightRange && !playerInAttackRange) ChasePlayer();
            if (playerInAttackRange && playerInSightRange) AttackPlayer();
        }
    }

    private void Patroling()
    {
        if (isDead) return;

        if (!walkPointSet) SearchWalkPoint();

        //Calculate direction and walk to Point
        if (walkPointSet){
            agent.SetDestination(walkPoint);
        }

        //Calculates DistanceToWalkPoint
        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        //Walkpoint reached
        if (distanceToWalkPoint.magnitude < 1f)
            walkPointSet = false;

        GetComponent<MeshRenderer>().material = green;
    }
    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint,-transform.up, 2,whatIsGround))
        walkPointSet = true;
    }
    private void ChasePlayer()
    {
        if (isDead) return;

        agent.SetDestination(player.position);

        GetComponent<MeshRenderer>().material = yellow;
    }
    private void AttackPlayer()
    {
        if (isDead) return;

        //Make sure enemy doesn't move
        agent.SetDestination(transform.position);

        transform.LookAt(player);

        if (!alreadyAttacked){

            //Attack
            Rigidbody rb = Instantiate(projectile, transform.position, Quaternion.identity).GetComponent<Rigidbody>();

            float[] inArr = {transform.position.x, transform.position.z, player.position.x, player.position.z};
            Tensor input = new Tensor(1, 1, 1, 4, inArr);
            m_Worker.Execute(input);

            Tensor output = m_Worker.PeekOutput();
            float[] outArr = output.readonlyArray;
            Vector3 direction = new Vector3(outArr[0], 0, outArr[1]);
            direction = direction / direction.magnitude;
            
            input.Dispose();
            output.Dispose();

            rb.AddForce(direction * 32f, ForceMode.Impulse);    
            rb.AddForce(transform.up * 12, ForceMode.Impulse);

            alreadyAttacked = true;
            Invoke("ResetAttack", timeBetweenAttacks);
        }

        GetComponent<MeshRenderer>().material = red;
    }
    private void ResetAttack()
    {
        if (isDead) return;

        alreadyAttacked = false;
    }
    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health < 0){
            isDead = true;
            Invoke("Destroyy", 2.8f);
        }
    }
    private void Destroyy()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
