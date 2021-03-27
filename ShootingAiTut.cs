using System.Collections.Generic;
using System.IO;   
using UnityEngine;
using UnityEngine.AI;
public class Dendrite
    {
        private float wt;
        public float Weight
        {
            get { return wt; }
            set { wt = value; }
        }

        //Provide a constructor for the class.
        //It is always better to provide a constructor instead of using
        //the compiler provided constructor
        public Dendrite()
        {
            wt = getRandom(0.00000001f, 1.0f);
        }

        private float getRandom(float MinValue, float MaxValue)
        {
            return UnityEngine.Random.Range(MinValue, MaxValue);
        }

    }

class Neuron
    {
        private List<Dendrite> dendrites;
        private float delta, _value;

        public float Delta
        {
            get
            { return delta; }
            set
            { delta = value; }
        }
        public float Value
        {
            get
            { return _value; }
            set
            { _value = value; }
        }

        public void AddDendrites(int nDendrites)
        {
            int i;
            //Dendrite d;
            for(i=0;i<nDendrites;i++)
            {
                //d = new Dendrite();
                dendrites.Add(new Dendrite());
            }
        }

        public int nDendrites()
        {
            return dendrites.Count;
        }

        public Dendrite getDendrite(int index)
        {
            return dendrites[index];
        }

        public Neuron(bool isInFirstLayer)
        {
            if (!isInFirstLayer) {
                dendrites = new List<Dendrite>();
            }
        }

    }

class Layer
   {
       private List<Neuron> neurons;

       public void Clear()
       {
           neurons.Clear();
       }
       public void Initialize(int nNeurons, bool isFirstLayer)
       {
           int i;
           for(i=0;i<nNeurons;i++)
           {
                neurons.Add(new Neuron(isFirstLayer));
           }
       }

       public Neuron getNeuron(int index)
       {
           return neurons[index];
       }
       public void setNeuron(int index, ref Neuron neuron)
       {
           neurons[index] = neuron;
       }
       public void setNeuron(int index, float value)
       {
           Neuron n = new Neuron(true);
           n.Value = value;
           neurons[index] = n;
       }

       public void AddDendritesToEachNeuron(int nDendrites)
       {
           int i;
           for(i=0;i<neurons.Count;i++)
           {
               neurons[i].AddDendrites(nDendrites);
           }
       }

       public int nNeurons()
       {
           return neurons.Count;
       }



       /// <summary>
       /// Constructor of the class
       /// </summary>
       public Layer()
       {
           neurons = new List<Neuron>();
       }
   }

class NeuralNetwork
    {

        public enum ActivationAlgorithms
        {
            Sigmoidal,
            BentIdentity,
            Identity,
            ReLU //Rectified linear unit
        };

        private ActivationAlgorithms _ActivationAlgorithm = ActivationAlgorithms.Identity;
        public ActivationAlgorithms ActivationAlgorithm
        {
            get { return _ActivationAlgorithm; }
            set { _ActivationAlgorithm = value; }
        }

        private List<Layer> layers;

        public int nLayers()
        {
            return layers.Count;
        }

        public Layer getLayer(int index)
        {
            return layers[index];
        }
        public void setLayer(int index, Layer layer)
        {
            layers[index] = layer;
        }

        //please note the nNeuronsInLayers[] arguement here.
        //it is an array which contains the number of neurons
        //in each layer
        public NeuralNetwork(List<int> nNeuronsInLayers)
        {
            layers = new List<Layer>();
            int nLayers = nNeuronsInLayers.Count; //we copy this to a variable
            if (nLayers > 1)
            {
                int lyr;
                Layer l;
                for (lyr = 0; lyr < nLayers; lyr++)
                {
                    // create a new layer
                    l = new Layer();

                    //now, in each layer, we have neurons
                    //so, we add neurons in the layer
                    //This has been simplified by
                    //Initialize function in the layer
                    l.Initialize(nNeuronsInLayers[lyr], (lyr == 0));
                    
                    if (lyr > 0)
                    {
                        //there are more than 1 layers in the NeuralNetwork..
                        //so we add dendrites to the neurons
                        //with a random bias (which has been already initialized)

                        //note that each neuron will have as many dendrites as the neurons
                        //in previous layer
                        if (lyr > 0)
                        {
                            l.AddDendritesToEachNeuron(nNeuronsInLayers[lyr - 1]);
                        }

                        layers.Add(l);
                    }
                }
            }
        }

        //The function which runs the net
        public List<float> Run(List<float> inputs)
        {
            int nOutputNeurons = layers[layers.Count - 1].nNeurons();
            int nLayers = layers.Count;
            float value;
            int i, j, iLayer, nNeuronsInLyr, nDendrites;
            List<float> output = new List<float>(nOutputNeurons);
            Neuron nrn;
            Dendrite dendrite;

            //first thing to do is copy the inputs on the first layer.
            //doing this will simply the solution of the NeuralNetwork.
            //hence, the first layer is dedicated to be the input layer,
            //last as the output layer and the rest are hidden layers
            nNeuronsInLyr = layers[0].nNeurons();
            for (i = 0; i < nNeuronsInLyr; i++)
            {
                //iterate over each neuron in the input layer to copy
                //the input supplied to this function in the list of floats
                layers[0].setNeuron(i, inputs[i]);
            }


            //move from left to the right layer and compute
            //the values of each neuron in each layer            
            //we start from layer 1 because layer 0 is the input layer
            for (iLayer = 1; iLayer < nLayers; iLayer++)
            {
                nNeuronsInLyr = layers[iLayer].nNeurons();
                nDendrites = layers[iLayer].getNeuron(0).nDendrites();
                for (i = 0; i < nNeuronsInLyr; i++)
                {
                    nrn = layers[iLayer].getNeuron(i);
                    value = 0.0f; //initialize the value of the neuron to 0

                    for (j = 0; j < nDendrites; j++)
                    {
                        dendrite = nrn.getDendrite(j);
                        value = value + dendrite.Weight * layers[iLayer - 1].getNeuron(j).Value;
                    }

                    nrn.Value = ActivationFunction(value);
                    layers[iLayer].setNeuron(i, ref nrn); //*** check if this is required.
                }
            }
            //now, copy the last layer to output list and return it
            for (i = 0; i < nOutputNeurons; i++)
            {
                output.Add(layers[nLayers - 1].getNeuron(i).Value);
            }
            return output;
        }

        //the activation function
        //we name it Activation so that changing the 
        //choice of function later on would not require
        //us to change the nomenclature of function and
        //function calls
        private float ActivationFunction(float x, bool returnDerivative = false)
        {
            //refer to https://en.wikipedia.org/wiki/Activation_function
            //for various activation functions

            float ret =x;
            if (returnDerivative == true) ret = 1.0f; //derivative of x
            switch (_ActivationAlgorithm)
            {
                case ActivationAlgorithms.BentIdentity:
                    //Bent Identity function
                    if (returnDerivative == true)
                    {
                        //derivative of the bent identity function
                        ret = x / (2.0f * ((float) System.Math.Sqrt(x * x + 1.0f))) + 1.0f;
                    }
                    else
                    {
                        ret = ((float) System.Math.Sqrt(x * x + 1) - 1.0f) / 2.0f + x;
                    }
                    break;
                case ActivationAlgorithms.Sigmoidal:
                    //sigmoidal function
                    if(returnDerivative==true)
                    {
                        ret = x * (1 - x); //derivative of the sigmoidal function
                    }else{                   
                        ret = 1.0f / (1.0f + (float) System.Math.Exp(x * -1));
                    }
                    break;
                case ActivationAlgorithms.Identity:
                    if (returnDerivative == true)
                    {
                        ret = 1.0f; //derivative of the identity function
                    }
                    else
                    {
                        ret = x;
                    }
                    break;
                case ActivationAlgorithms.ReLU:
                    if (returnDerivative == true)
                    {
                        if (x < 0)
                        {
                            ret = 0.0f;
                        }
                        else
                        {
                            ret = 1.0f;
                        }
                    }
                    else
                    {
                        if (x < 0)
                        {
                            ret = 0.0f;
                        }
                        else
                        {
                            ret = x;
                        }
                    }
                    break;
            }
            return ret;
        }
    }

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

    NeuralNetwork ann;

    private void Awake()
    {
        player = GameObject.Find("PlayerObj").transform;
        agent = GetComponent<NavMeshAgent>();

        string[] lines;
        lines = File.ReadAllLines(getPath() + "/weights.csv");
        // 4*4 weights, then 4*2 weights

        List<int> nNeuronsInEachLayer;
        int nInputNeurons, nOutputNeurons, NeuronsInEachLayer, nHiddenLayers;

        nInputNeurons = 4;          // x & z coordinates of bot & player
        nHiddenLayers = 1;
        NeuronsInEachLayer = 4;
        nOutputNeurons = 2;         // x & z coordinates of bullet target

        nNeuronsInEachLayer = new List<int>(nHiddenLayers+2);

        nNeuronsInEachLayer.Add(nInputNeurons);
        for(int i=0;i<nHiddenLayers;i++)
        {
            nNeuronsInEachLayer.Add(NeuronsInEachLayer);
        }
        nNeuronsInEachLayer.Add(nOutputNeurons);

        ann = new NeuralNetwork(nNeuronsInEachLayer);   // Identity activation function by default
        ann.ActivationAlgorithm = NeuralNetwork.ActivationAlgorithms.BentIdentity;
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

            rb.AddForce(executeNet(transform.position, player.position) * 32f, ForceMode.Impulse);
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

    // Get path for given CSV file
    private static string getPath(){
        #if UNITY_EDITOR
            return Application.dataPath;
        #elif UNITY_ANDROID
            return Application.persistentDataPath;// +fileName;
        #elif UNITY_IPHONE
            return Application.dataPath
                                .Substring(0, Application.dataPath.Length - 5)
                                .Substring(0, path.LastIndexOf('/'))
                                 + "/Documents";
        #else
            return Application.dataPath;// +"/"+ fileName;
        #endif
    }

    private Vector3 executeNet(Vector3 bot, Vector3 player)
    {
        List<float> inputs, outputs;

        inputs = new List<float>(ann.getLayer(0).nNeurons());
        outputs = new List<float>(ann.getLayer(ann.nLayers() - 1).nNeurons());

        inputs.Add(bot.x);
        inputs.Add(bot.z);
        inputs.Add(player.x);
        inputs.Add(player.z);

        outputs = ann.Run(inputs);

        Vector3 v = new Vector3(outputs[0], 0, outputs[1]);
        return v / v.magnitude;
    }
}
