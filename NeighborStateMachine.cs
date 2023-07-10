using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;
using Random=UnityEngine.Random;
using random = System.Random;

public class NeighbourStateMachine : MonoBehaviour{
    public GameObject searchingAvatar;
    public float neighbourBreakInLength = 16f;
    public TimeSpan timeBreakingIn;
    public DateTime startNeighbourWaitInterval;
    public string attackType;
    public bool guilty;
    public random killRand;
    public State prevState;
    public Vector3 killPos;
    public Vector3 killAngle;
    public State currentState;
    public Neighbour neighbour;
    public DateTime LeavingWaitStart;
    public bool kill;
    public PlayerData.Locations attackLocation;
    public Neighbour killingNeighbour;

    public DateTime maxKillStart;
    private int maxKillGap=300;
    public NeighbourStateMachine(Neighbour neighbour){
        this.neighbour = neighbour;
        this.searchingAvatar = neighbour.searchingAvatar;
        killRand = new random();
        LeavingWaitStart = DateTime.Now;
        maxKillStart = DateTime.Now;
    }
    public void Update()
    {
        currentState!.Execute();
        if(neighbour.isGuilty&&DateTime.Now-LeavingWaitStart>=TimeSpan.FromSeconds(175f)&&!(currentState.GetType()!=State.States.Leaving.GetType()||currentState.GetType()!=State.States.Returning.GetType()||currentState.GetType()!=State.States.BreakingIn.GetType()||currentState.GetType()!=State.States.Attacking.GetType()||currentState.GetType()!=State.States.Searching.GetType()||currentState.GetType()!=State.States.Returning.GetType()||currentState.GetType()!=State.States.KillingNeighbour.GetType()||currentState.GetType()!=State.States.KillingPlayer.GetType())){
            ChangeState(State.States.Leaving);
        }
        if(DateTime.Now-maxKillStart>=TimeSpan.FromSeconds(maxKillGap)&&!kill&&neighbour.isGuilty){
            switch(neighbour.stateMachine.currentState.GetType().ToString()){
                case "BreakingIn":
                case "Searching":
                case "Leaving":
                case "Returning":
                case "Attacking":
                    break;
                case "Left":
                    ChangeState(State.States.Attacking);
                    break;
                default:
                    ChangeState(State.States.Leaving);
                    kill = true;
                    break;
            }
        }
    }
    public void ChangeState(State.States s){
        //if(neighbour.isGuilty){Debug.Log(s.ToString());}
        prevState = currentState;
        if(currentState!=null){
            currentState.Exit();
        }
        currentState = (State)Activator.CreateInstance(Type.GetType(s.ToString()));
        currentState.SetNeighbour(neighbour);
        currentState.Enter();
    }
}
public class State{
    public enum States{Roaming, GoingToSit, Sitting, Leaving, Attacking, Left, Returning, Chasing, BreakingIn, Searching, Dead, KillingNeighbour, WaitingToKillPlayer, KillingPlayer, GoingToMail, GoHome, GoingToGarbage, GettingGarbage, DisposingGarbage, GoingToPhone, AnsweringPhone};
    public GameObject NeighbourAttackerObject;
    public float yCoord = -3.517888f;
    public Neighbour neighbour;
    public string animationString = "isRoaming";
    public DateTime start;
    public float duration;
    public void SetNeighbour(Neighbour neighbour){
        this.neighbour = neighbour;
    }
    public virtual void Enter(){
        neighbour.neighbourAnimator.SetBool(animationString, true);
        StopFootstepsAudio();
    }
    public virtual void Execute(){}
    public virtual void Exit(){neighbour.neighbourAnimator.SetBool(animationString, false);}
    public void CheckChase(){
        if(IsPlayerInSphere()&&CheckPlayerInView()){
            neighbour.stateMachine.ChangeState(States.Chasing);
        }
    }
    public void PlayFootstepsAudio(){
        neighbour.audio.clip = GameObject.FindObjectOfType<AudioManager>().GetSound("NeighbourFootsteps").clip;
        neighbour.audio.Play();
    }
    public void StopFootstepsAudio(){
        neighbour.audio.Pause();
    }
    public bool IsPlayerInSphere(){
        return Vector3.Distance(neighbour.player.transform.position, neighbour.transform.position)<=neighbour.detectRadius;
    }
    public void CheckForDoorClosed(){
        RaycastHit hit;
        GameObject avatar = neighbour.stateMachine.currentState.GetType().ToString()=="Searching"?neighbour.stateMachine.currentState.NeighbourAttackerObject:neighbour.gameObject;
        if(Physics.Raycast(avatar.transform.position, avatar.transform.forward, out hit, 2.5f)){
            Door door = hit.collider.GetComponent<Door>();
            if(door!=null){
                door.Open();
            }
        }
    }
    public bool CheckPlayerInView(){
        RaycastHit hit;
        Vector3 rayStart = new Vector3(neighbour.transform.position.x, neighbour.transform.position.y+ neighbour.gameObject.GetComponent<CapsuleCollider>().height*2/3, neighbour.transform.position.z);
        Vector3 rayTarget = Vector3.zero;
        int startAngle = (int)(-neighbour.fov*0.5F);
        int finishAngle = (int)(neighbour.fov*0.5F);
        int inc = (int)(neighbour.fov/20);
        for(int i = startAngle;i<finishAngle;i+=inc){
            rayTarget = (Quaternion.Euler(0,i,0)*neighbour.transform.forward).normalized*5.0f;
            if(Physics.Linecast(rayStart, rayTarget, out hit)){
                PlayerMovement1stPerson player = hit.collider.GetComponent<PlayerMovement1stPerson>();
                if(player!=null){
                    return true;
                }
            }
            Debug.DrawLine(rayStart, rayTarget, Color.green);
        }
        return false;
    }
}
public class Roaming:State{
    private float walkRadius = 7f;
    private Vector3 destination;
    private random rand;
    public Roaming(){
        this.start = DateTime.Now;
        this.duration = 26f;
        rand = new random();
    }
    public override void Enter(){
        base.Enter();
        SetRoamLocation();
        PlayFootstepsAudio();
    }
    public override void Execute(){
        if(IsPlayerInSphere()){
            if(CheckPlayerInView()){
                neighbour.stateMachine.ChangeState(States.Chasing);
                return;
            }
        }
        CheckForDoorClosed();
        if(DateTime.Now-start<=TimeSpan.FromSeconds(duration)){
            if(Vector3.Distance(destination, neighbour.transform.position)<=1.7f)
                SetRoamLocation();
        }
        else{
            List<GameObject> neighbours = GameObject.FindObjectOfType<neighbourManager>().GetComponent<neighbourManager>().neighbours;
            int leavingCount=0;
            int mailCount=0;
            int garbageCount=0;
            foreach(GameObject obj in neighbours){
                string t = obj.GetComponent<Neighbour>().stateMachine.currentState.GetType().ToString();
                switch(t){
                    case "Leaving":
                    case "Left":
                    case "Returning":
                        leavingCount++;
                        break;
                    case "GoingToMail":
                        mailCount++;
                        break;
                    case "GettingGarbage":
                    case "GoingToGarbage":
                    case "DisposingGarbage":
                        garbageCount++;
                        break;
                }
            }
            int roll = rand.Next(0,5);
            if((roll==0||roll==4)&&leavingCount<=neighbours.Count-2&&(mailCount+garbageCount+leavingCount)<neighbours.Count-3){
                neighbour.stateMachine.ChangeState(States.Leaving);
            }
            else{
                roll = rand.Next(0, 6);
                if(roll==2&&mailCount<3&&(mailCount+garbageCount+leavingCount)<neighbours.Count-3) neighbour.stateMachine.ChangeState(States.GoingToMail);
                else if(roll==1&&garbageCount<3&&(mailCount+garbageCount+leavingCount)<neighbours.Count-3) neighbour.stateMachine.ChangeState(States.GettingGarbage);
                else if(roll==4||roll==5){neighbour.stateMachine.ChangeState(States.Roaming);}
                else{neighbour.stateMachine.ChangeState(States.GoingToSit);}
            }
        }
    }
    public void SetRoamLocation(){
            destination = RandomNavMeshLocation();
            if(destination!=new Vector3(3.14f,1.59f, 2.65f))
                neighbour.agent.SetDestination(destination);
    }
    public Vector3 RandomNavMeshLocation(){
        Vector3 finalPos = Vector3.zero;
        Vector3 randomPos;
        NavMeshHit hit;
        int loops = 0;
        do{
            loops++;
            if(loops>=100){
                neighbour.stateMachine.ChangeState(States.GoHome);
                return new Vector3(3.14f,1.59f, 2.65f);
            }
            randomPos = Random.insideUnitSphere*walkRadius;
            randomPos+= neighbour.transform.position;
        }while((!NavMesh.SamplePosition(randomPos, out hit, walkRadius, 1)||!PositionIsHome(randomPos)));
        finalPos = hit.position;
        return finalPos;
    }
    public bool PositionIsHome(Vector3 pos){
        return Physics.SphereCast(pos, 0.2f, -neighbour.transform.up, out RaycastHit hit, walkRadius)?hit.collider.gameObject.layer == LayerMask.NameToLayer("Home"):false;
    }
}
public class GoingToSit:State{
    public override void Enter(){
        base.Enter();
        neighbour.agent.SetDestination(neighbour.chairPos);
        PlayFootstepsAudio();
    }
    public override void Execute(){
        CheckChase();
        CheckForDoorClosed();
        if(Vector3.Distance(neighbour.transform.position, neighbour.chairPos)<=3f)
            neighbour.stateMachine.ChangeState(States.Sitting);
    }
}
public class Sitting:State{
    public Sitting(){
        start = DateTime.Now;
        animationString = "isSitting";
        duration = 22f;
        this.neighbour = neighbour;
        //Quaternion.Slerp(neighbour.transform.rotation, GameObject.Find(neighbour.address).transform.Find("Sofa").rotation, 1f*Time.deltaTime);
    }
    public override void Execute(){
        CheckChase();
        if(DateTime.Now-start>=TimeSpan.FromSeconds(duration))
            neighbour.stateMachine.ChangeState(States.Roaming);
    }
    public override void Enter(){
        base.Enter();
        neighbour.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
        neighbour.agent.baseOffset = -0.3f;
    }
    public override void Exit(){
        base.Exit();
        neighbour.agent.baseOffset = 0f;
    }
}
public class Leaving:State{
    private double attackStartWait = 300.0;
    private random rand = new random();
    public override void Enter(){
        base.Enter();
        rand = new random();
        neighbour.agent.SetDestination(neighbour.car.transform.position);
        neighbour.StartCoroutine(WaitForReachCar());
        PlayFootstepsAudio();
    }
    public override void Execute(){
        CheckForDoorClosed();
        if(neighbour.isGuilty&&IsPlayerInSphere()){
            neighbour.StopCoroutine(WaitForCar());
            neighbour.transform.position = neighbour.car.transform.position+ new Vector3(0f, 0f, 1f);
            neighbour.stateMachine.ChangeState(States.Chasing);
            neighbour.car.GetComponent<NeighbourCar>().Stop();
        }
    }
    IEnumerator WaitForReachCar(){
        yield return new WaitUntil(()=> Vector3.Distance(neighbour.transform.position, neighbour.car.transform.position)<=3f);
        StopFootstepsAudio();
        neighbour.ChangeVisibility(false);
        neighbour.agent.SetDestination(neighbour.transform.position);
        neighbour.GetComponent<CapsuleCollider>().enabled = false;
        neighbour.car.DriveAway();
        neighbour.StartCoroutine(WaitForCar());
    }
    IEnumerator WaitForCar(){
        yield return new WaitUntil(()=> neighbour.car.left);
        if(neighbour.isGuilty){
            if(!neighbour.stateMachine.kill){
                int roll = rand.Next(0, 11);
                switch(roll){
                    case 9: case 4:
                        neighbour.stateMachine.ChangeState(States.Left);
                        break;
                    default:
                        neighbour.stateMachine.ChangeState(States.Attacking);
                        break;
                }
            }
            else{
                neighbour.stateMachine.ChangeState(States.Attacking);
                neighbour.stateMachine.kill=false;
            }
        }
        else{
            neighbour.stateMachine.ChangeState(States.Left);
        }
    }
}
public class Attacking:State{
    private random rand = new random();
    private string location;
    GameObject house;
    private Room room;
    private Neighbour kneighbour;
    public override void Enter(){
        PlayerData.Locations playerLoc = GameObject.FindObjectOfType<PlayerMovement1stPerson>().playerData.location;
        int roll = rand.Next(0,3);
        if(!neighbour.stateMachine.kill&&roll==2&&playerLoc!=PlayerData.Locations.Outside&&playerLoc!=PlayerData.Locations.BusStop){
            location = playerLoc.ToString();
        }
        else{
            do{
                roll = rand.Next(0, Enum.GetNames(typeof(PlayerData.Locations)).Length-3);
                string[] locs = System.Enum.GetNames(typeof(PlayerData.Locations));
                location = locs[roll].Replace("House", "");
                kneighbour = new Neighbour();
                neighbourManager NM = GameObject.FindObjectOfType<neighbourManager>();
                bool livingNeighbourFound;
                foreach(GameObject obj in NM.neighbours){
                    Neighbour n = obj.GetComponent<Neighbour>();
                    if(n.address == location){
                        kneighbour = n;
                        livingNeighbourFound = true;
                        break;
                    }
                }
                Debug.Log(location);
                if(!livingNeighbourFound){
                    //all neighbours are dead -> GameOver
                    kneighbour.stateMachine.ChangeState(States.KillingPlayer);
                    return;
                }
            }
            while(!kneighbour.alive||location==neighbour.address);
        }
        if(location=="PlayerHouse"){
            neighbour.stateMachine.ChangeState(States.BreakingIn);
            return;
        }
        //get house gameobject
        GameObject houses = GameObject.Find("Houses");
        for(int i = 0; i<houses.transform.childCount; i++){
            if(houses.transform.GetChild(i).name==location){
                house = houses.transform.GetChild(i).gameObject;
            }
        }
        //get room in house to inhabit (trigger)
        GameObject t = house.transform.Find("Triggers").gameObject;
        room = t.transform.GetChild(rand.Next(0, t.transform.childCount)).GetComponent<Room>();
        Debug.Log(room.name);
    }
    public override void Execute(){
        //wait for player/neighbour to enter trigger
        if(room.inhabitant!=null){
            if(room.inhabitant.GetComponent<PlayerMovement1stPerson>()!=null){
                neighbour.stateMachine.ChangeState(States.KillingPlayer);
            }
            else{
                room.inhabitant = null;
                neighbour.stateMachine.ChangeState(States.KillingNeighbour);
            }
        }
    }
    public override void Exit(){
        neighbour.stateMachine.killingNeighbour = kneighbour;
    }
}
public class Left:State{
    public Left(){
        this.start = DateTime.Now;
        this.duration = 50f;
    }
    public override void Enter(){
        neighbour.car.gameObject.SetActive(false);
    }
    public override void Execute(){
        if(DateTime.Now-start>=TimeSpan.FromSeconds(duration))
            neighbour.stateMachine.ChangeState(States.Returning);
    }
}
public class Returning:State{
    public override void Enter(){
        neighbour.car.gameObject.SetActive(true);
        neighbour.car.DriveHome();
        neighbour.StartCoroutine(WaitForCarReachHouse());
    }
    public override void Execute(){
        //neighbour.transform.position = neighbour.car.transform.position;
        if(neighbour.isGuilty&&IsPlayerInSphere()){
            neighbour.transform.position = neighbour.car.transform.position+ new Vector3(0f, 0f, 1f);
            neighbour.stateMachine.ChangeState(States.Chasing);
            neighbour.StopCoroutine(WaitForCarReachHouse());
            neighbour.car.GetComponent<NeighbourCar>().Stop();
        }
    }
    public override void Exit(){
        neighbour.stateMachine.LeavingWaitStart = DateTime.Now;
    }
    IEnumerator WaitForCarReachHouse(){
        yield return new WaitUntil(()=>!neighbour.car.left);
        neighbour.car.GetComponent<NavMeshAgent>().ResetPath();
        neighbour.ChangeVisibility(true);
        neighbour.agent.GetComponent<CapsuleCollider>().enabled = true;
        neighbour.stateMachine.ChangeState(States.Roaming);
    }
}
public class Chasing:State{
    public override void Enter(){
        base.Enter();
        neighbour.ChangeVisibility(true);
        neighbour.car.Stop();
        PlayFootstepsAudio();
    }
    public override void Execute(){
        if(IsPlayerInSphere()&&!(neighbour.player.hidden||neighbour.player.inPlayerHouse))
            Chase();
        else{
            switch(neighbour.stateMachine.prevState.GetType().ToString()){
                case "Leaving":
                    neighbour.stateMachine.ChangeState(States.Leaving);
                    break;
                case "Returning":
                    neighbour.stateMachine.ChangeState(States.Returning);
                    break;
                default:
                    neighbour.stateMachine.ChangeState(States.GoHome);
                    break;
            }
        }
    }
    public void Chase(){
        CheckForDoorClosed();
        neighbour.agent.SetDestination(neighbour.player.transform.position);
    }
}
public class BreakingIn:State{
    public override void Enter(){
        animationString = "isBreakingIn";
        base.Enter();
        duration = 16f;
        start = DateTime.Now;
        BreakInHouse();
    }
    public override void Exit(){
        base.Exit();
        GameObject.Destroy(NeighbourAttackerObject);
    }
    public override void Execute(){
        if(DateTime.Now-start>=TimeSpan.FromSeconds(duration)){
            if(neighbour.player.inPlayerHouse&&!neighbour.player.hidden){
                neighbour.stateMachine.ChangeState(States.KillingPlayer);
                neighbour.stateMachine.killPos = new Vector3(-10.85213f, -3.96f, -9.79f);
            }
            else{
                neighbour.stateMachine.ChangeState(States.Searching);
            }
        }
    }
    void BreakInHouse(){
        GameObject.Find("AudioManager").GetComponent<AudioManager>().Play("EnemyLockpicking");
        NeighbourAttackerObject = GameObject.Instantiate(neighbour.NeighbourAttackerObjectPrefab, new Vector3(5.11f, -3.521974f, -10.22f), Quaternion.Euler(0f, 182.777f, 0f));
        //NeighbourAttackerObject.GetComponent<NavMeshAgent>().ResetPath();
        NeighbourAttackerObject.GetComponent<Animator>().SetBool("isBreakingIn",true);
    }
}
public class Searching:State{
    AudioManager audioManager;
    Vector3 destination;
    bool searching = true;
    List<Vector3> searchLocs;
    public Vector3 searchDestination;
    public DateTime start;
    public int cutoff=160;
    private bool cuttoffReached;
    public Searching(){
        start = DateTime.Now;
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        searchLocs = new List<Vector3>();
        searchLocs.Add(new Vector3(6.179663f, -3.517888f,-28.25553f));
        searchLocs.Add(new Vector3(15.8f, -3.517888f, -29.06f));
        searchLocs.Add(new Vector3(9.52f, -0.8027041f, -28.83f));
        searchLocs.Add(new Vector3(5.460865f, -0.5824052f, -28.78784f));
        searchLocs.Add(new Vector3(15.024f, -0.5824051f, -24.706f));
        searchLocs.Add(new Vector3(5.542762f, -3.517887f, -24.56398f));
        searchDestination = searchLocs[0];
    }
    public override void Enter(){
        GameObject.Find("AudioManager").GetComponent<AudioManager>().Play("NeighbourSearchingFootsteps");
        SearchHouse();
    }
    public override void Execute(){
        if(DateTime.Now-start>=TimeSpan.FromSeconds(cutoff)&&!cuttoffReached){
            cuttoffReached = true;
            searchDestination = searchLocs[searchLocs.Count-1];
        }
        else{
            CheckForDoorClosed();
            if(!neighbour.player.hidden&&neighbour.player.inPlayerHouse){
                neighbour.stateMachine.ChangeState(States.KillingPlayer);
            }
            else{
                GoToNewLocation(NeighbourAttackerObject.transform.position.x);
            }
        }
    }
    void SearchHouse(){
        GameObject.Find("AudioManager").GetComponent<AudioManager>().Stop("EnemyLockpicking");
        NeighbourAttackerObject = GameObject.Instantiate(neighbour.stateMachine.searchingAvatar, searchLocs[searchLocs.Count-1], Quaternion.Euler(0f, -180f, 0f));
        NeighbourAttackerObject.GetComponent<Animator>().SetBool("isSearching", true);
        NeighbourAttackerObject.GetComponent<NavMeshAgent>().SetDestination(searchDestination);
    }
    public override void Exit(){
        audioManager.Play("DoorClose");
        audioManager.Stop("NeighbourSearchingFootsteps");
        neighbour.ChangeVisibility(true);
        GameObject.Destroy(NeighbourAttackerObject);
    }
    void GoToNewLocation(float x){
        if(Vector3.Distance(NeighbourAttackerObject.transform.position, searchDestination)<=1f){
            if(searchLocs.IndexOf(searchDestination)!=searchLocs.Count-2&&!cuttoffReached){
                searchDestination = searchLocs[searchLocs.IndexOf(searchDestination)+1];
                NeighbourAttackerObject.GetComponent<NeighbourSearch>().searchDestination = searchDestination;
                //PlayLookAround();
                NeighbourAttackerObject.GetComponent<NavMeshAgent>().SetDestination(searchDestination);
            }
            else{
                neighbour.stateMachine.ChangeState(States.Returning);
            }
        }
    }
}
public class Dead:State{
    public Dead(){
        this.animationString = "isDead";
    }
    public override void Enter(){
        neighbour.agent.enabled=false;
        base.Enter();
        neighbour.alive = false;
        //neighbour.agent.ResetPath();
        neighbour.ChangeVisibility(true);
    }
}
public class KillingNeighbour:State{
    neighbourManager NM;
    random rand;
    GameObject Attacker;
    public KillingNeighbour(){
        NM = GameManager.FindObjectOfType<neighbourManager>();
        rand = new random();
    }
    public override void Enter(){
        Attacker = GameObject.Instantiate(neighbour.NeighbourAttackerObjectPrefab, neighbour.stateMachine.killingNeighbour.transform.position+(-3f*neighbour.stateMachine.killingNeighbour.transform.forward), Quaternion.Euler(-1*neighbour.stateMachine.killingNeighbour.transform.forward));
        Attacker.GetComponent<Animator>().SetBool("isAttacking", true);
        Neighbour n = neighbour.stateMachine.killingNeighbour;
        if(n!=null){
            //instantiate attacker behind neighbour
            KillNeighbour(n);
            neighbour.stateMachine.ChangeState(States.Left);
        }
    }
    public override void Exit(){
        GameObject.Destroy(Attacker);
        neighbour.stateMachine.maxKillStart = DateTime.Now;
    }
    public void KillNeighbour(Neighbour n){
        if(n!=null){
            Debug.Log("Killed "+n.address+" with "+NM.currentWeapon.evidenceType);
            n.Die(NM.currentWeapon);
            neighbour.murderWeaponsList.Add(NM.currentWeapon);
            neighbour.victims.Add(n);
            n.ChangeVisibility(true);
        }
    }
    public void GetNewMurderWeapon(){
        NM.currentWeapon = NM.murderWeapons[neighbour.stateMachine.killRand.Next(0, NM.murderWeapons.Count)];
    }
}
public class WaitingToKillPlayer:State{
    public override void Execute(){
        if(neighbour.stateMachine.attackType.Length>0){
            NeighbourAttackerObject.GetComponent<NavMeshAgent>().ResetPath();
            Vector3 killerRotation = new Vector3(0f, 0f, 0f);
            switch(neighbour.stateMachine.attackType){
                case "ExitCloset":
                    //set attacker position
                    neighbour.stateMachine.killPos = new Vector3(-11.69f, -3.67765f, -11.84f);
                    neighbour.stateMachine.killAngle = new Vector3(0f,-90f,0f);
                    break;
                case "ExitLaptop":
                    //set attacker position
                    neighbour.stateMachine.killPos = new Vector3(-9.25f, -3.67765f, -12.4472f);
                    neighbour.stateMachine.killAngle = new Vector3(0f,180f,0f);
                    break;
                case "ExitBoard":
                    //set attacker position
                    NeighbourAttackerObject.transform.position = new Vector3(-9.658434f, -3.67765f, -13.2f);
                    NeighbourAttackerObject.transform.localEulerAngles = new Vector3(0f,180f,0f);
                    break;
                case "EnterHouse":
                    //set attacker position
                    NeighbourAttackerObject.transform.position = new Vector3(-9.658434f, -3.67765f, -13.2f);
                    NeighbourAttackerObject.transform.localEulerAngles = new Vector3(0f,180f,0f);
                    break;
            }
            neighbour.stateMachine.ChangeState(States.KillingPlayer);
        }
    }
}
public class KillingPlayer:State{
    float distanceBehindPlayer = 2f;
    public override void Enter(){
        animationString = "isAttacking";
        base.Enter();
        GameObject attacker = GameObject.Instantiate(neighbour.NeighbourAttackerObjectPrefab, neighbour.stateMachine.killPos, Quaternion.identity);
        attacker.transform.localEulerAngles = neighbour.stateMachine.killAngle;
        neighbour.player.isBeingKilled = true;
        attacker.GetComponent<Animator>().SetBool("isAttacking", true);
        GameObject.Find("AudioManager").GetComponent<AudioManager>().Play("Neighbour_Jumpscare");
    }
}
public class GoingToMail:State{
    private Vector3 mailLoc;
    public override void Enter(){
        mailLoc = GameObject.Find("mailbox").transform.position;
        base.Enter();
        neighbour.agent.SetDestination(mailLoc);
        PlayFootstepsAudio();
    }
    public override void Execute(){
        CheckForDoorClosed();
        if(Vector3.Distance(neighbour.transform.position, mailLoc)<=1.6f){
            neighbour.stateMachine.ChangeState(States.GoHome);
            if(neighbour.isGuilty){
            foreach(Package p in neighbour.player.packages){
                GameObject.Destroy(p.gameObject);
            }
        }
        }
    }
}
public class GoHome:State{
    public override void Enter(){
        base.Enter();
        neighbour.agent.SetDestination(neighbour.homeLoc);
        PlayFootstepsAudio();
    }
    public override void Execute(){
        CheckForDoorClosed();
        if(Vector3.Distance(neighbour.transform.position, neighbour.homeLoc)<=0.5f)
            neighbour.stateMachine.ChangeState(States.Roaming);
    }
}
public class GettingGarbage:State{
    private GameObject Garbage;
    public override void Enter(){
        base.Enter();
        Garbage = GameObject.Find($"{neighbour.address}/TrashBag");
        if(Garbage!=null)
            neighbour.agent.SetDestination(Garbage.transform.position);
        else{
            neighbour.stateMachine.ChangeState(States.GoHome);
        }
        PlayFootstepsAudio();
    }
    public override void Execute(){
        CheckForDoorClosed();
        try{
            if(Vector3.Distance(neighbour.transform.position, Garbage.transform.position)<=1.5f){
                Garbage.transform.parent = neighbour.transform.GetChild(5).GetChild(2).GetChild(0).GetChild(0).GetChild(2).GetChild(0).GetChild(0).GetChild(0);
                Garbage.transform.SetAsLastSibling();
                Garbage.transform.localPosition = new Vector3(0.013f, 0.569f, 0.194f);
                neighbour.stateMachine.ChangeState(States.GoingToGarbage);
            }
        }catch(Exception e){
            neighbour.stateMachine.ChangeState(States.GoHome);
        }
    }
}
public class GoingToGarbage:State{
    private Vector3 destination;
    public override void Enter(){
        base.Enter();
        destination = new Vector3(26.02f, -3.93f, 12.076f);
        neighbour.agent.SetDestination(destination);
        PlayFootstepsAudio();
    }
    public override void Execute(){
        CheckForDoorClosed();
        if(Vector3.Distance(neighbour.transform.position, destination)<=0.5f){neighbour.stateMachine.ChangeState(States.DisposingGarbage);}
    }
}
public class DisposingGarbage:State{
    public override void Enter(){
        base.Enter();
        int index = neighbour.transform.GetChild(5).GetChild(2).GetChild(0).GetChild(0).GetChild(2).GetChild(0).GetChild(0).GetChild(0).childCount-1;
        GameObject.Destroy(neighbour.transform.GetChild(5).GetChild(2).GetChild(0).GetChild(0).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(index).gameObject);
        neighbour.stateMachine.ChangeState(States.GoHome);
    }
}
public class GoingToPhone:State{
    private Vector3 destination;
    public override void Enter(){
        base.Enter();
        destination = neighbour.phone.transform.position;
        neighbour.agent.SetDestination(destination);
        PlayPhoneSound();
        PlayFootstepsAudio();
    }
    public override void Execute(){
        CheckForDoorClosed();
        if(Vector3.Distance(neighbour.transform.position, destination)<=2f){neighbour.stateMachine.ChangeState(States.AnsweringPhone);}
    }
    private void PlayPhoneSound(){
        AudioSource.PlayClipAtPoint(GameObject.Find("AudioManager").GetComponent<AudioManager>().GetSound("Phone Vibrate").source.clip, neighbour.phone.transform.position);
    }
}
public class AnsweringPhone:State{
     public override void Enter(){
        neighbour.agent.ResetPath();
        duration = 60f;
        start = DateTime.Now;
    }
    public override void Execute(){
        if(DateTime.Now-start>=TimeSpan.FromSeconds(duration))
            neighbour.stateMachine.ChangeState(States.Roaming);
    }
    public override void Exit(){
        neighbour.phone.HangUp();
    }
}
